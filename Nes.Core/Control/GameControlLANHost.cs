using Nes.Core.Control.Agreement;
using Nes.Core.Control.Palettes;
using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using ThreadState = System.Threading.ThreadState;

namespace Nes.Core.Control;

/// <summary>
/// 本地游戏控制器
/// </summary>
public partial class GameControlLANHost : GameControl
{
    public override event EventHandler? GameOpened;
    public override event EventHandler? GameStopped;
    public override event EventHandler? GamePaused;
    public override event EventHandler? GameResumed;
    public override event EventHandler? GameReseted;
    public override event EventHandler<byte[]>? GameDrawFrame;
    public override event EventHandler<float[]>? GameAudioOut;
    public override event EventHandler<string>? ErrorEventOccurred;

    /// <summary>
    /// 游戏连接事件---0:未连接, 1:连接中, 2:已连接, 3:连接失败
    /// </summary>
    public event EventHandler<int>? ConnectedEvent;

    public override string? GameName { get; protected set; }
    public override string? GameFilePath { get; protected set; }
    public override int GameStatus
    {
        get;
        protected set
        {
            if(field == value) return;
            Interlocked.Exchange(ref field, value);
        }
    } = 0;
    public override GameControlType Type => GameControlType.LANHost;
    public override ColorPalette SelectedColorPalette { get; set; }
    public override bool IsP1Enabled => true;
    public override bool IsP2Enabled => false;

    /// <summary>
    /// 游戏帧率
    /// </summary>
    public Byte GameFrameRate
    {
        get;
        set
        {
            if(value > 200) value = 60;
            m_GameRunPeriod = (int)Math.Round(1000.0 / value);
            field = value;
        }
    } = 60;

    private uint m_ScreenNumber = 0;   // 画面编号
    private readonly byte[] m_Pixels = new byte[256 * 240 * 4]; // 画面像素
    private TaskCompletionSource? m_TcsPause = null; // 控制暂停任务的返回
    private TaskCompletionSource? m_TcsStop = null; // 控制停止任务的返回
    private readonly Emulator m_emulator = new( );   // 模拟器
    private Thread? m_GameThread;    // 游戏线程
    private int m_GameRunPeriod = 17; // 定时器周期(ms)

    private readonly string m_NesFileDirPath; // NES文件目录路径
    private readonly Thread m_SendDataThread;    // 发送数据线程
    private readonly Thread m_RecvDataThread;    // 接收数据线程
    private volatile UdpClient? m_UdpClient;    // UDP客户端
    private IPEndPoint m_RemoteEndPoint = new(IPAddress.Any, 0);    // 远程端点
    private readonly ReaderWriterLockSlim m_ClientLock = new( );    // TCP客户端锁对象
    private readonly BlockingCollection<DataFrame> m_SendDataQueue = [];    // 发送数据队列
    private int m_SequenceNumber;   // 数据帧序列号
    private readonly Timer m_HeartbeatTimer;    // 心跳定时器
    private bool m_IsHeartbeatTimerEnabled = false;    // 是否启用心跳定时器
    private volatile uint m_HearbeatCount = 0;   // 心跳计数

    /// <summary>
    /// 连接状态---0:未连接, 1:连接中, 2:已连接, 3:连接失败
    /// </summary>
    public int ConnectionState
    {
        get;
        set
        {
            if(field == value) return;
            Interlocked.Exchange(ref field, value);
            ConnectedEvent?.Invoke(this, value);
        }
    } = 0;
    private volatile bool m_IsRequestOpenGame = false;  // 是否请求打开游戏
    private volatile bool m_IsRequestPauseGame = false;  // 是否请求暂停游戏
    private volatile bool m_IsRequestResumeGame = false;  // 是否请求恢复游戏
    private volatile bool m_IsRequestStopGame = false;  // 是否请求停止游戏
    private volatile bool m_IsSendNesInfos = false;  // 是否成功发送NES信息
    private volatile bool m_IsOver = false;  // 是否结束

    public GameControlLANHost(string nesFileDirPath)
    {
        m_IsOver = false;
        m_NesFileDirPath = nesFileDirPath;
        SelectedColorPalette = ColorPalette.GetColorPaletteByName("Default");

        float[] outputBuffer = new float[128];
        int writeIndex = 0;
        m_emulator.Apu.WriteSample = (sampleValue) =>
        {
            outputBuffer[writeIndex++] = sampleValue;
            if(writeIndex == 128)
            {
                writeIndex = 0;
                if(ConnectionState == 2)
                {
                    byte[] buffer = new byte[128 * 4];
                    Buffer.BlockCopy(outputBuffer, 0, buffer, 0, 128 * 4);
                    var sendFrame = new DataFrame(DataFrameType.AudioDataRequest,
                        Interlocked.Increment(ref m_SequenceNumber),
                        buffer);  // 创建数据帧
                    m_SendDataQueue.Add(sendFrame);    // 将数据帧加入发送数据队列
                }
                GameAudioOut?.Invoke(this, outputBuffer);
            }
        };
        m_emulator.DrawFrame += DrawFrameHandle; // 画帧事件

        GameOpened += (_, _) =>
        {
            if(ConnectionState == 2 && m_IsRequestOpenGame)
            {/// 有从机连接且请求打开游戏
                MemoryStream stream = new( );
                stream.Write(BitConverter.GetBytes(true));
                stream.Write(Encoding.UTF8.GetBytes(GameName!));
                DataFrame sendframe = new(DataFrameType.OpenGameResponse,
                                Interlocked.Increment(ref m_SequenceNumber),
                                stream);
                m_SendDataQueue.Add(sendframe);    // 将数据帧加入发送数据队列
                m_IsRequestOpenGame = false;    // 重置请求打开游戏
            }
        };

        GamePaused += (_, _) =>
        {
            if(ConnectionState == 2 && m_IsRequestPauseGame)
            {/// 有从机连接
                DataFrame sendframe = new(DataFrameType.PauseGameResponse,
                    Interlocked.Increment(ref m_SequenceNumber),
                    BitConverter.GetBytes(true));
                m_SendDataQueue.Add(sendframe);    // 将数据帧加入发送数据队列
                m_IsRequestPauseGame = false;    // 重置请求暂停游戏
            }
        };

        GameResumed += (_, _) =>
        {
            if(ConnectionState == 2 && m_IsRequestResumeGame)
            {/// 有从机连接
                DataFrame sendframe = new(DataFrameType.ResumeGameResponse,
                    Interlocked.Increment(ref m_SequenceNumber),
                    BitConverter.GetBytes(true));
                m_SendDataQueue.Add(sendframe);    // 将数据帧加入发送数据队列
                m_IsRequestResumeGame = false;    // 重置请求恢复游戏
            }
        };

        GameStopped += (_, _) =>
        {
            m_GameThread = null;    // 清空游戏线程
            GameStatus = 0; // 设置游戏状态为未打开
            GameName = null; // 清空游戏名称
            GameFilePath = null; // 清空游戏文件路径
            m_emulator.Reset( ); // 重置模拟器
            m_emulator.RemoveCartridge( ); // 移除游戏卡带
            if(ConnectionState == 2 && m_IsRequestStopGame)
            {/// 有从机连接
                DataFrame sendframe = new(DataFrameType.StopGameResponse,
                    Interlocked.Increment(ref m_SequenceNumber),
                    BitConverter.GetBytes(true));
                m_SendDataQueue.Add(sendframe);    // 将数据帧加入发送数据队列
                m_IsRequestStopGame = false;    // 重置请求停止游戏
            }
        };

        m_HeartbeatTimer = new(async (_) =>
        {
            m_HeartbeatTimer!.Change(Timeout.Infinite, 1000);    // 停止心跳定时器
            if(m_UdpClient is null) return;
            if(ConnectionState == 2)
            {
                DataFrame sendframe = new(DataFrameType.HeartbeatRequest,
                            Interlocked.Increment(ref m_SequenceNumber), []);
                m_SendDataQueue.Add(sendframe);    // 将数据帧加入发送数据队列
                Interlocked.Increment(ref m_HearbeatCount); // 心跳计数加1

                if(!m_IsSendNesInfos)
                {
                    // 获取文件夹内的所有文件（不包括子文件夹中的文件）
                    using MemoryStream stream = new( );
                    using BinaryWriter writer = new(stream);
                    var fullPath = Path.GetFullPath(m_NesFileDirPath);
                    string[] files = Directory.GetFiles(fullPath, "*.nes");
                    var tasks = files.Select(async (nesFilePath) =>
                    {
                        var (res, mapperNum, isSupported) = await GameControl.GetNesFileInfoAsync(nesFilePath);
                        var nesFileName = Path.GetFileNameWithoutExtension(nesFilePath);
                        return (nesFileName, mapperNum, isSupported);
                    });
                    var Infos = await Task.WhenAll(tasks);
                    writer.Write(Infos.Count(info => info.isSupported));
                    foreach(var (nesFileName, mapperNum, isSupported) in Infos)
                    {
                        if(isSupported)
                        {
                            writer.Write(mapperNum);    // Mapper号
                            writer.Write(Path.GetFileNameWithoutExtension(nesFileName));    // 游戏名称
                        }
                    }
                    sendframe = new(DataFrameType.NesFileInfosRequest,
                       Interlocked.Increment(ref m_SequenceNumber),
                       stream);
                    m_SendDataQueue.Add(sendframe);    // 将数据帧加入发送数据队列
                }
            }
            if(m_HearbeatCount > 10)
            {/// 心跳计数大于10或者未连接
                ConnectionState = 1;    // 设置连接状态为连接中
                Interlocked.Exchange(ref m_HearbeatCount, 0);   // 重置心跳计数
            }
            if(m_IsHeartbeatTimerEnabled)
                m_HeartbeatTimer?.Change(1000, 1000);
        }, null, Timeout.Infinite, 1000);  // 创建心跳定时器

        m_RecvDataThread = new Thread(RecvDataHandle)
        {
            IsBackground = true,
            Name = "RecvDataThread",
        };  // 初始化接收数据线程

        m_SendDataThread = new Thread(SendDataHandle)
        {
            IsBackground = true,
            Name = "SendDataThread",
        };  // 初始化发送数据线程
    }

    ~GameControlLANHost( )
    {
        m_IsOver = true;
        ConnectionState = 3;    // 设置连接状态为连接失败
        GameStatus = 3; // 设置游戏状态为停止
        m_RecvDataThread.Join( );  // 等待接收数据线程结束
        m_SendDataThread.Join( );  // 等待发送数据线程结束
        m_GameThread?.Join( );  // 等待游戏线程结束
        m_HeartbeatTimer.Change(Timeout.Infinite, 1000);    // 停止心跳定时器
        m_HeartbeatTimer.Dispose( );    // 释放心跳定时器
        m_ClientLock.EnterWriteLock( );
        m_UdpClient?.Close( );   // 关闭UDP客户端
        m_UdpClient = null; // 清空UDP客户端
        m_ClientLock.ExitWriteLock( );
        m_ClientLock.Dispose( ); // 释放锁对象
    }

    /// <summary>
    /// 游戏运行处理函数
    /// </summary>
    private void GameRunHandle( )
    {
        long RunCount = 0;  // 运行总次数
        long RunTime = 0;   // 运行总时间
        var Watch = new Stopwatch( );
        while(true)
        {
            Watch.Restart( );
            switch(GameStatus)
            {
                case 0: // 未打开
                    break;
                case 1: // 运行中
                    m_emulator.ExecuteStep( ); // 进行一帧画面的模拟
                    break;
                case 2: // 暂停
                    if(m_TcsPause is not null)
                    {
                        m_TcsPause.SetResult( );    // 设置暂停任务的返回
                        m_TcsPause = null;
                        if(ConnectionState == 2 && !m_IsRequestPauseGame)
                        {/// 非对方请求导致的暂停游戏
                            var sendframe = new DataFrame(DataFrameType.PauseGameRequest,
                                Interlocked.Increment(ref m_SequenceNumber),
                                []);  // 创建数据帧
                            m_SendDataQueue.Add(sendframe);    // 将数据帧加入发送数据队列
                        }
                        // 触发游戏暂停事件
                        GamePaused?.Invoke(this, EventArgs.Empty);
                    }
                    break;
                case 3: // 停止
                    if(m_TcsStop is not null)
                    {
                        m_TcsStop.SetResult( ); // 设置停止任务的返回
                        m_TcsStop = null;
                        if(ConnectionState == 2 && !m_IsRequestStopGame)
                        {/// 非对方请求导致的停止游戏
                            var sendframe = new DataFrame(DataFrameType.StopGameRequest,
                                Interlocked.Increment(ref m_SequenceNumber),
                                []);  // 创建数据帧
                            m_SendDataQueue.Add(sendframe);    // 将数据帧加入发送数据队列
                        }
                        // 触发游戏停止事件
                        GameStopped?.Invoke(this, EventArgs.Empty);
                    }
                    return;
                default:
                    GameStopped?.Invoke(this, EventArgs.Empty);
                    return;
            }
            var diff = (int)(RunCount * m_GameRunPeriod - RunTime);
            if(diff > 0)
            {
                Thread.Sleep(Math.Min(diff, m_GameRunPeriod));
            }

            Watch.Stop( );
            RunCount++; // 记录总运行次数
            RunTime += Watch.ElapsedMilliseconds;   // 记录总运行时间
        }
    }

    /// <summary>
    /// 发送数据线程处理函数
    /// </summary>
    private void SendDataHandle( )
    {
        while(!m_IsOver)
        {
            m_ClientLock.EnterReadLock( );
            if(m_UdpClient is null)
            {// TCP客户端未连接
                m_ClientLock.ExitReadLock( );
                Thread.Sleep(100);
                continue;
            }
            try
            {
                DataFrame? dataFrame = m_SendDataQueue.Take( ); // 从发送数据队列中取出数据帧
                if(dataFrame.Type != DataFrameType.None)
                    m_UdpClient.Send(dataFrame.GetBuffer( ), m_RemoteEndPoint); // 发送数据
            }
            catch(Exception)
            { }
            m_ClientLock.ExitReadLock( );
        }
    }
    /// <summary>
    /// 接收数据线程处理函数
    /// </summary>
    private void RecvDataHandle( )
    {
        byte[] buffer = [];
        IPEndPoint clientEndPoint = new(IPAddress.Any, 0);
        while(!m_IsOver)
        {
            m_ClientLock.EnterReadLock( );
            if(m_UdpClient is null)
            {// TCP客户端未连接
                m_ClientLock.ExitReadLock( );
                Thread.Sleep(100);
                goto end;
            }
            try
            {
                buffer = m_UdpClient.Receive(ref clientEndPoint); // 读取数据
                var dataFrame = new DataFrame(buffer); // 创建数据帧

                if(dataFrame.Type == DataFrameType.None) goto end;
                if(dataFrame.Type == DataFrameType.ConnectionRequest)
                    m_RemoteEndPoint = new(clientEndPoint.Address, clientEndPoint.Port);
                // 创建处理数据任务
                Task.Factory.StartNew(async ( ) => await ProcessorDataHandle(dataFrame));
            }
            catch(Exception)
            { }
        end:
            m_ClientLock.ExitReadLock( );
        }
    }

    /// <summary>
    /// 处理数据线程处理函数
    /// </summary>
    private async Task ProcessorDataHandle(DataFrame dataFrame)
    {
        DataFrame? sendframe;
        Interlocked.Exchange(ref m_SequenceNumber, dataFrame.SequenceNumber);   // 更新数据帧序列号
        switch(dataFrame.Type)
        {
            case DataFrameType.ConnectionRequest:   // 连接请求
                if(ConnectionState == 2) break; // 已连接
                ConnectionState = 2;    // 设置已连接
                var obj = dataFrame.Analyze( );
                if(obj is not (string, int)) break;
                var (_, connectCount) = ((string, int))obj;
                var buffer = BitConverter.GetBytes(connectCount);
                sendframe = new(DataFrameType.ConnectionResponse,
                    Interlocked.Increment(ref m_SequenceNumber), buffer);
                m_SendDataQueue.Add(sendframe); // 将数据帧加入发送数据队列
                if(GameStatus == 1 || GameStatus == 2)
                {
                    MemoryStream stream = new( );
                    stream.Write(BitConverter.GetBytes(true));
                    stream.Write(Encoding.UTF8.GetBytes(GameName!));
                    sendframe = new(DataFrameType.OpenGameResponse,
                                    Interlocked.Increment(ref m_SequenceNumber),
                                    stream);
                    m_SendDataQueue.Add(sendframe);    // 将数据帧加入发送数据队列
                }
                break;

            case DataFrameType.OpenGameRequest: // 打开游戏请求
                if(m_IsRequestOpenGame) break;
                m_IsRequestOpenGame = true; // 设置请求打开游戏
                obj = dataFrame.Analyze( );
                if(obj is not string) break;
                var gameName = (string)obj;
                if(!File.Exists(Path.Combine(m_NesFileDirPath, gameName + ".nes")))
                {
                    MemoryStream stream = new( );
                    stream.Write(BitConverter.GetBytes(false));
                    stream.Write(Encoding.UTF8.GetBytes(gameName));
                    sendframe = new(DataFrameType.OpenGameResponse,
                                    Interlocked.Increment(ref m_SequenceNumber),
                                    stream);
                    m_SendDataQueue.Add(sendframe);    // 将数据帧加入发送数据队列
                }
                else
                {
                    if(GameStatus != 0)
                        await StopGame( );  // 停止游戏
                    OpenGame(Path.Combine(m_NesFileDirPath, gameName + ".nes"));
                }
                break;

            case DataFrameType.PauseGameRequest:    // 暂停游戏请求
                m_IsRequestPauseGame = true;    // 设置暂停游戏请求
                await PauseGame( ); // 暂停游戏
                break;

            case DataFrameType.PauseGameResponse:   // 暂停游戏响应
                break;

            case DataFrameType.ResumeGameRequest:   // 恢复游戏请求
                m_IsRequestResumeGame = true;   // 设置恢复游戏请求
                ResumeGame( );  // 恢复游戏
                break;

            case DataFrameType.ResumeGameResponse:  // 恢复游戏响应
                break;

            case DataFrameType.StopGameRequest: // 关闭游戏请求
                m_IsRequestStopGame = true;  // 设置请求停止游戏
                await StopGame( );  // 停止游戏
                break;

            case DataFrameType.StopGameResponse:    // 关闭游戏响应
                break;

            case DataFrameType.OperationSyncRequest:   // 操作数据同步
                var operationData = dataFrame.Analyze( );
                if(operationData is byte[] buttonStates)
                    for(var i = 0; i < 8; i++)
                    {
                        SetButtonState(2, (Controller.Buttons)i, buttonStates[i] != 0);
                    }
                break;

            case DataFrameType.OperationSyncResponse:
                break;

            case DataFrameType.ImageDataResponse:
                break;

            case DataFrameType.NesFileInfosResponse:
                obj = dataFrame.Analyze( );
                if(obj is not bool) break;
                m_IsSendNesInfos = (bool)obj;
                break;

            case DataFrameType.HeartbeatRequest:    // 心跳请求
                break;

            case DataFrameType.HeartbeatResponse:   // 心跳响应
                if(m_HearbeatCount > 0)
                    Interlocked.Decrement(ref m_HearbeatCount);    // 心跳计数减1
                break;

            default:
                break;
        }
    }

    public override void Connect(string ip = "127.0.0.1", int port = 55666)
    {
        try
        {
            m_UdpClient = new UdpClient(port);    // 创建UDP客户端
            ConnectionState = 1;    // 设置连接状态为连接中
        }
        catch(SocketException)
        {
            ErrorEventOccurred?.Invoke(this, "端口号被已被占用");
            ConnectionState = 3;    // 设置连接状态为连接失败
            return;
        }
        m_SequenceNumber = 0;   // 重置数据帧序列号
        m_IsRequestOpenGame = false;    // 重置请求打开游戏
        m_IsHeartbeatTimerEnabled = true;   // 启用心跳定时器
        Interlocked.Exchange(ref m_HearbeatCount, 0);   // 重置心跳计数
        m_HeartbeatTimer.Change(100, 1000);    // 启动心跳定时器
        if(((byte)m_RecvDataThread.ThreadState & (byte)ThreadState.Unstarted) != 0) // 接收数据线程未启动
            m_RecvDataThread.Start( );  // 启动接收数据线程
        if(((byte)m_SendDataThread.ThreadState & (byte)ThreadState.Unstarted) != 0) // 发送数据线程未启动
            m_SendDataThread.Start( );  // 启动发送数据线程
    }

    public override void DisConnect( )
    {
        ConnectionState = 3;    // 设置为连接失败
        m_SequenceNumber = 0;   // 重置数据帧序列号
        m_IsRequestOpenGame = false;    // 重置请求打开游戏
        m_IsHeartbeatTimerEnabled = false;  // 关闭心跳定时器
        m_HeartbeatTimer.Change(Timeout.Infinite, Timeout.Infinite);    // 启动心跳定时器
        Interlocked.Exchange(ref m_HearbeatCount, 0);   // 重置心跳计数
        foreach(var _ in m_SendDataQueue)
        {
            m_SendDataQueue.Take( );
        }
        m_ClientLock.EnterWriteLock( );
        m_UdpClient?.Close( );   // 关闭UDP客户端
        m_UdpClient = null; // 清空UDP客户端
        m_ClientLock.ExitWriteLock( );
    }

    public override void OpenGame(string nesFilePath)
    {
        if(!File.Exists(nesFilePath)) return; // 文件不存在
        GameName = Path.GetFileNameWithoutExtension(nesFilePath); // 获取游戏名称
        GameFilePath = nesFilePath; // 保存游戏文件路径
        m_emulator.Open(nesFilePath);
        m_GameThread = new(GameRunHandle)
        {
            IsBackground = true,
            Name = "GameThread",
        };
        m_GameThread.Start( );  // 启动游戏线程
        GameStatus = 1; // 设置游戏状态为运行中

        if(ConnectionState == 2 && !m_IsRequestOpenGame)
        {
            MemoryStream stream = new( );
            stream.Write(BitConverter.GetBytes(true));
            stream.Write(Encoding.UTF8.GetBytes(GameName));
            DataFrame sendframe = new(DataFrameType.OpenGameResponse,
                            Interlocked.Increment(ref m_SequenceNumber),
                            stream);
            m_SendDataQueue.Add(sendframe);    // 将数据帧加入发送数据队列
        }

        GameOpened?.Invoke(this, EventArgs.Empty);  // 触发游戏打开事件
    }

    public override async void ResetGame( )
    {
        await PauseGame( ); // 暂停游戏
        m_emulator.Reset( );
        ResetGame( );  // 恢复游戏
        GameReseted?.Invoke(this, EventArgs.Empty); // 触发游戏重置事件
    }

    public override Task StopGame( )
    {
        m_TcsStop = new( ); // 创建一个新的任务
        GameStatus = 3; // 设置游戏状态为停止
        return m_TcsStop.Task;
    }

    public override Task PauseGame( )
    {
        m_TcsPause = new( ); // 创建一个新的任务
        GameStatus = 2; // 设置游戏状态为暂停
        return m_TcsPause.Task;
    }

    public override void ResumeGame( )
    {
        GameStatus = 1; // 设置游戏状态为运行中
        GameResumed?.Invoke(this, EventArgs.Empty); // 触发游戏恢复事件
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public override void SetButtonState(int Px, Controller.Buttons btn, bool state)
    {
        m_emulator.Controller.SetButtonState(Px, btn, state);
    }

    private void DrawFrameHandle(object? sender, byte[] bitmapData)
    {
        if(ConnectionState == 2)
        {
            // 压缩画面数据
            using var outputStream = new MemoryStream( );
            outputStream.Write(BitConverter.GetBytes(m_ScreenNumber++));
            using DeflateStream deflateStream = new(outputStream, CompressionMode.Compress, true);
            deflateStream.Write(bitmapData);
            deflateStream.Close( );
            var sendFrame = new DataFrame(DataFrameType.ImageDataRequest,
                Interlocked.Increment(ref m_SequenceNumber),
                outputStream);  // 创建数据帧
            m_SendDataQueue.Add(sendFrame);    // 将数据帧加入发送数据队列
        }

        var colors = SelectedColorPalette.Colors;
        Parallel.For(0, 256 * 240, i =>
        {
            Color color = colors[bitmapData[i]];
            m_Pixels[i * 4 + 0] = color.B;
            m_Pixels[i * 4 + 1] = color.G;
            m_Pixels[i * 4 + 2] = color.R;
            m_Pixels[i * 4 + 3] = color.A;
        });
        GameDrawFrame?.Invoke(this, m_Pixels); // 触发游戏画帧事件
    }

    public override void Save(BinaryWriter writer)
    {
        if(GameStatus != 1 && GameStatus != 2) return; // 没有打开的游戏

        m_emulator.Save(writer);    // 保存游戏
    }

    public override void Load(BinaryReader reader)
    {
        if(GameStatus != 1 && GameStatus != 2) return; // 没有打开的游戏

        m_emulator.Load(reader);    // 读档
    }

    public override void Dispose( )
    {
        m_IsOver = true;
        ConnectionState = 3;    // 设置连接状态为连接失败
        GameStatus = 3; // 设置游戏状态为停止
        m_RecvDataThread.Join( );  // 等待接收数据线程结束
        m_SendDataThread.Join( );  // 等待发送数据线程结束
        m_GameThread?.Join( );  // 等待游戏线程结束
        m_HeartbeatTimer.Change(Timeout.Infinite, 1000);    // 停止心跳定时器
        m_HeartbeatTimer.Dispose( );    // 释放心跳定时器
        m_ClientLock.EnterWriteLock( );
        m_UdpClient?.Close( );   // 关闭UDP客户端
        m_UdpClient = null; // 清空UDP客户端
        m_ClientLock.ExitWriteLock( );
        m_ClientLock.Dispose( ); // 释放锁对象
        GC.SuppressFinalize(this); // 防止对象被终止
    }
}

