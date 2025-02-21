using Nes.Core.Control.Agreement;
using Nes.Core.Control.Palettes;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.IO.Compression;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using ThreadState = System.Threading.ThreadState;

namespace Nes.Core.Control;

/// <summary>
/// 本地游戏控制器
/// </summary>
public partial class GameControlSlave : GameControl
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
    /// 游戏连接事件
    /// </summary>
    public event EventHandler<int>? ConnectedEvent;

    public override string? GameName { get; protected set; }
    public override string? GameFilePath { get; protected set; }
    public override int GameStatus
    {
        get;
        protected set
        {
            Interlocked.Exchange(ref field, value);
        }
    } = 0;
    public override GameControlType Type => GameControlType.Salve;
    public override ColorPalette SelectedColorPalette { get; set; }
    public override bool IsP1Enabled => false;
    public override bool IsP2Enabled => true;

    /// <summary>
    /// 游戏帧率
    /// </summary>
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


    /// <summary>
    /// 主机支持的所有Nes游戏文件名
    /// </summary>
    public (int MapperNum, string Name)[] NesfileNames
    {
        get;
        private set;
    } = [];

    private readonly SortedList<uint, byte[]> m_ImageCompressedDataList = [];
    private readonly object m_ImageCompressedDataLock = new( );
    private readonly byte[] m_bitmapData = new byte[256 * 240]; // 画面像素
    private readonly float[] m_audioData = new float[128]; // 音频数据
    private readonly byte[] m_Pixels = new byte[256 * 240 * 4]; // 画面像素
    private readonly Thread? m_GameThread;    // 游戏线程
    private int m_GameRunPeriod = 17; // 定时器周期(ms)

    private string m_Address = "127.0.0.1";   // IP地址
    private int m_Port = 55666;  // 端口号
    private readonly Thread m_SendDataThread;    // 发送数据线程
    private readonly Thread m_RecvDataThread;    // 接收数据线程
    private UdpClient? m_UdpClient;
    private readonly ReaderWriterLockSlim m_ClientLock = new( );    // 读写锁
    private readonly Timer m_HeartbeatTimer;    // 心跳定时器
    private bool m_IsHeartbeatTimerEnabled = false;    // 是否启用心跳定时器
    private volatile uint m_HeartbeatCount = 0; // 心跳计数
    private readonly BlockingCollection<DataFrame> m_SendDataQueue = [];    // 发送数据队列
    private int m_SequenceNumber;   // 数据帧序列号
    private readonly byte[] m_ButtonState = new byte[8]; // 按键状态

    /// <summary>
    /// 连接状态---0:未连接, 1:连接中, 2:已连接, 3:连接失败
    /// </summary>
    private int ConnectionState
    {
        get;
        set
        {
            if(field == value) return;
            Interlocked.Exchange(ref field, value);
            ConnectedEvent?.Invoke(this, value);
        }
    } = 0; // 是否有从机连接
    private volatile bool m_IsOver = false;  // 是否结束

    public GameControlSlave( )
    {
        m_IsOver = false;
        SelectedColorPalette = ColorPalette.GetColorPaletteByName("Default");

        GameStopped += (_, _) =>
        {
            GameStatus = 0; // 设置游戏状态为未打开
            GameName = null; // 清空游戏名称
            GameFilePath = null; // 清空游戏文件路径
        };

        m_HeartbeatTimer = new((_) =>
        {
            m_HeartbeatTimer!.Change(Timeout.Infinite, 1000);    // 暂停定时器
            if(m_HeartbeatCount > 10)
            {
                ConnectionState = 1;    // 设置连接状态为连接中
                Interlocked.Exchange(ref m_HeartbeatCount, 0);    // 重置心跳计数
            }
            if(ConnectionState == 1)
            {/// 连接中持续发送连接请求
                var buffer = new byte[10];
                MemoryStream ms = new(buffer);
                ms.Write(Encoding.UTF8.GetBytes("123456"));
                ms.Write(BitConverter.GetBytes(1));
                DataFrame dataFrame = new(DataFrameType.ConnectionRequest,
                    Interlocked.Increment(ref m_SequenceNumber), buffer);
                m_SendDataQueue.Add(dataFrame);    // 将数据帧加入发送数据队列
            }
            if(m_IsHeartbeatTimerEnabled)
                m_HeartbeatTimer?.Change(1000, 1000);
        }, null, Timeout.Infinite, 1000); // 创建心跳定时器

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

        m_GameThread = new Thread(GameRunHandle)
        {
            IsBackground = true,
            Name = "GameThread",
        };  // 初始化游戏线程
        m_GameThread.Start( );  // 启动游戏线程
    }

    ~GameControlSlave( )
    {
        m_IsOver = true;
        ConnectionState = 3;    // 设置连接失败
        GameStatus = 3; // 设置游戏状态为停止
        m_HeartbeatTimer.Change(Timeout.Infinite, 1000);    // 停止心跳定时器
        m_RecvDataThread.Join( );    // 等待接收数据线程结束
        m_SendDataThread.Join( );    // 等待发送数据线程结束
        m_UdpClient?.Close( );    // 关闭UDP客户端
        m_UdpClient = null;
        m_HeartbeatTimer.Dispose( );    // 释放心跳定时器
    }

    /// <summary>
    /// 游戏运行处理函数
    /// </summary>
    private void GameRunHandle( )
    {
        long RunCount = 0;  // 运行总次数
        long RunTime = 0;   // 运行总时间
        byte[] buffer;
        var Watch = new Stopwatch( );
        while(true)
        {
            Watch.Restart( );

            if(m_ImageCompressedDataList.Count > 0)
            {
                lock(m_ImageCompressedDataLock)
                {
                    buffer = m_ImageCompressedDataList.Values[0];
                    m_ImageCompressedDataList.RemoveAt(0);
                }
                MemoryStream ms = new(buffer);
                //解压画面数据
                using var deflateStream = new DeflateStream(ms, CompressionMode.Decompress);
                deflateStream.Read(m_bitmapData);
                deflateStream.Close( );
                DrawFrameHandle(m_bitmapData);
            }

            var sendframe = new DataFrame(DataFrameType.OperationSyncRequest,
                Interlocked.Increment(ref m_SequenceNumber),
                m_ButtonState);  // 创建数据帧
            m_SendDataQueue.Add(sendframe);    // 将数据帧加入发送数据队列

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
                DataFrame dataFrame = m_SendDataQueue.Take( ); // 从发送数据队列中取出数据帧
                if(dataFrame.Type != DataFrameType.None)
                    m_UdpClient.Send(dataFrame.GetBuffer( ), m_Address, m_Port); // 发送数据
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
            case DataFrameType.ConnectionResponse:  // 连接响应
                ConnectionState = 2;    // 设置连接状态为已连接
                break;

            case DataFrameType.OpenGameResponse:    /// 打开游戏响应
                var obj = dataFrame.Analyze( );
                if(obj is not (bool, string)) break;
                (var isSuccess, var Name) = ((bool, string))obj;
                if(isSuccess)
                {
                    GameName = Name;    // 保存游戏名称
                    GameFilePath = Name;    // 保存游戏文件路径
                    GameStatus = 1; // 设置游戏状态为运行中
                }
                GameOpened?.Invoke(this, EventArgs.Empty);  // 触发游戏打开事件
                break;

            case DataFrameType.PauseGameRequest:    // 暂停游戏请求
                sendframe = new(DataFrameType.PauseGameResponse,
                    Interlocked.Increment(ref m_SequenceNumber),
                    BitConverter.GetBytes(true));
                m_SendDataQueue.Add(sendframe);    // 将数据帧加入发送数据队列
                await PauseGame( ); // 暂停游戏
                break;

            case DataFrameType.PauseGameResponse:   // 暂停游戏响应
                break;

            case DataFrameType.ResumeGameRequest:   // 恢复游戏请求
                sendframe = new(DataFrameType.ResumeGameResponse,
                    Interlocked.Increment(ref m_SequenceNumber),
                    BitConverter.GetBytes(true));
                m_SendDataQueue.Add(sendframe);    // 将数据帧加入发送数据队列
                ResumeGame( );  // 恢复游戏
                break;

            case DataFrameType.ResumeGameResponse:  // 恢复游戏响应
                break;

            case DataFrameType.StopGameRequest: // 关闭游戏请求
                await StopGame( );
                break;

            case DataFrameType.StopGameResponse:    // 关闭游戏响应
                break;

            case DataFrameType.ImageDataRequest:
                obj = dataFrame.Analyze( );
                if(obj is not (uint, byte[])) break;
                (var num, var buffer) = ((uint, byte[]))obj;
                lock(m_ImageCompressedDataLock)
                {
                    m_ImageCompressedDataList.Add(num, buffer);
                }
                sendframe = new(DataFrameType.ImageDataResponse,
                    Interlocked.Increment(ref m_SequenceNumber),
                    BitConverter.GetBytes(true));
                m_SendDataQueue.Add(sendframe);    // 将数据帧加入发送数据队列
                break;

            case DataFrameType.AudioDataRequest:
                obj = dataFrame.Analyze( );
                if(obj is not (byte[], int, int)) break;
                (buffer, var offset, var length) = ((byte[], int, int))obj;
                Buffer.BlockCopy(buffer, offset, m_audioData, 0, length);
                GameAudioOut?.Invoke(this, m_audioData);

                sendframe = new(DataFrameType.AudioDataResponse,
                    Interlocked.Increment(ref m_SequenceNumber),
                    BitConverter.GetBytes(true));
                m_SendDataQueue.Add(sendframe);    // 将数据帧加入发送数据队列
                break;

            case DataFrameType.HeartbeatRequest:    // 心跳请求
                sendframe = new(DataFrameType.HeartbeatResponse,
                    Interlocked.Increment(ref m_SequenceNumber), []);
                m_SendDataQueue.Add(sendframe);    // 将数据帧加入发送数据队列
                if(m_HeartbeatCount > 0)
                    Interlocked.Decrement(ref m_HeartbeatCount);
                break;

            case DataFrameType.HeartbeatResponse:   // 心跳响应
                break;

            case DataFrameType.NesFileInfosRequest:
                obj = dataFrame.Analyze( );
                if(obj is not (int, string)[]) break;
                NesfileNames = ((int, string)[])obj;
                sendframe = new(DataFrameType.NesFileInfosResponse,
                    Interlocked.Increment(ref m_SequenceNumber),
                    BitConverter.GetBytes(true));
                m_SendDataQueue.Add(sendframe);    // 将数据帧加入发送数据队列
                break;

            default:
                break;
        }
    }

    public override void Connect(string ip = "127.0.0.1", int port = 55666)
    {
        m_Address = ip;  // 设置IP地址
        m_Port = port;  // 设置端口号
        m_SequenceNumber = 0;   // 重置数据帧序列号
        m_IsHeartbeatTimerEnabled = true;   // 启用心跳定时器
        m_HeartbeatTimer.Change(0, 1000);   // 启动心跳定时器
        m_UdpClient = new UdpClient( );    // 创建UDP客户端
        ConnectionState = 1;    // 设置连接状态为连接中

        if(((byte)m_RecvDataThread.ThreadState & (byte)ThreadState.Unstarted) != 0) // 接收数据线程未启动
            m_RecvDataThread.Start( );  // 启动接收数据线程
        if(((byte)m_SendDataThread.ThreadState & (byte)ThreadState.Unstarted) != 0) // 发送数据线程未启动
            m_SendDataThread.Start( );  // 启动发送数据线程
    }

    public override void DisConnect( )
    {
        ConnectionState = 3;    // 设置连接失败
        m_SequenceNumber = 0;   // 重置数据帧序列号
        m_IsHeartbeatTimerEnabled = false;   // 禁用心跳定时器
        m_HeartbeatTimer.Change(Timeout.Infinite, 1000);    // 停止心跳定时器
        foreach(var _ in m_SendDataQueue)
        {
            m_SendDataQueue.Take( );
        }
        m_ClientLock.EnterWriteLock( );  // 获取写锁
        m_UdpClient?.Close( );    // 关闭UDP客户端
        m_UdpClient = null;
        m_ClientLock.ExitWriteLock( );  // 释放写锁
    }

    public override void OpenGame(string nesFilePath)
    {
        if(ConnectionState == 2)
        {/// 已连接且非对方请求打开游戏
            MemoryStream ms = new(50);
            ms.Write(Encoding.UTF8.GetBytes(nesFilePath));
            DataFrame sendframe = new(DataFrameType.OpenGameRequest,
                Interlocked.Increment(ref m_SequenceNumber),
                ms);
            m_SendDataQueue.Add(sendframe);    // 将数据帧加入发送数据队列
        }
    }

    public override void ResetGame( )
    {
        GameReseted?.Invoke(this, EventArgs.Empty); // 触发游戏重置事件
    }

    public override Task StopGame( )
    {
        GameStatus = 3; // 设置游戏状态为停止
        DataFrame sendframe = new(DataFrameType.StopGameRequest,
            Interlocked.Increment(ref m_SequenceNumber),
            []);
        m_SendDataQueue.Add(sendframe);    // 将数据帧加入发送数据队列
        GameStopped?.Invoke(this, EventArgs.Empty); // 触发游戏停止事件
        return Task.Delay(0);
    }

    public override Task PauseGame( )
    {
        GameStatus = 2; // 设置游戏状态为暂停
        DataFrame sendframe = new(DataFrameType.PauseGameRequest,
            Interlocked.Increment(ref m_SequenceNumber),
            []);
        m_SendDataQueue.Add(sendframe);    // 将数据帧加入发送数据队列
        GamePaused?.Invoke(this, EventArgs.Empty); // 触发游戏暂停事件
        return Task.Delay(0);
    }

    public override void ResumeGame( )
    {
        GameStatus = 1; // 设置游戏状态为运行中
        DataFrame sendframe = new(DataFrameType.ResumeGameRequest,
            Interlocked.Increment(ref m_SequenceNumber),
            []);
        m_SendDataQueue.Add(sendframe);    // 将数据帧加入发送数据队列
        GameResumed?.Invoke(this, EventArgs.Empty); // 触发游戏恢复事件
    }

    public override void SetButtonState(int Px, Controller.Buttons btn, bool state)
    {
        m_ButtonState[(byte)btn] = state ? (byte)1 : (byte)0;
    }

    private void DrawFrameHandle(byte[] bitmapData)
    {
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
    }

    public override void Load(BinaryReader reader)
    {
        if(GameStatus != 1 && GameStatus != 2) return; // 没有打开的游戏
    }

    public override void Dispose( )
    {
        m_IsOver = true;
        ConnectionState = 3;    // 设置连接失败
        GameStatus = 3; // 设置游戏状态为停止
        m_HeartbeatTimer.Change(Timeout.Infinite, 1000);    // 停止心跳定时器
        m_RecvDataThread.Join( );    // 等待接收数据线程结束
        m_SendDataThread.Join( );    // 等待发送数据线程结束
        m_HeartbeatTimer.Dispose( );    // 释放心跳定时器
        GC.SuppressFinalize(this); // 防止对象被终止
    }
}

