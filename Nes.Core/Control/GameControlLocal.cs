using Nes.Core.Control.Palettes;
using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Nes.Core.Control;

/// <summary>
/// 本地游戏控制器
/// </summary>
public partial class GameControlLocal : GameControl
{
    public override event EventHandler? GameOpened;
    public override event EventHandler? GameStopped;
    public override event EventHandler? GamePaused;
    public override event EventHandler? GameResumed;
    public override event EventHandler? GameReseted;
    public override event EventHandler<byte[]>? GameDrawFrame;
    public override event EventHandler<float[]>? GameAudioOut;
    public override event EventHandler<string>? ErrorEventOccurred;

    public override string? GameName { get; protected set; }
    public override string? GameFilePath { get; protected set; }
    public override int GameStatus { get; protected set; } = 0;
    public override GameControlType Type => GameControlType.Local;
    public override ColorPalette SelectedColorPalette { get; set; }
    public override bool IsP1Enabled => true;
    public override bool IsP2Enabled => true;

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

    private readonly byte[] m_Pixels = new byte[256 * 240 * 4]; // 画面像素
    private TaskCompletionSource? m_TcsPause = null; // 控制暂停任务的返回
    private TaskCompletionSource? m_TcsStop = null; // 控制停止任务的返回
    private readonly Emulator m_emulator = new( );   // 模拟器
    private Thread? m_GameThread;    // 游戏线程
    private int m_GameRunPeriod = 17; // 游戏运行周期(ms)

    public GameControlLocal( )
    {
        SelectedColorPalette = ColorPalette.GetColorPaletteByName("Default");

        float[] outputBuffer = new float[128];
        int writeIndex = 0;
        m_emulator.Apu.WriteSample = (sampleValue) =>
        {
            outputBuffer[writeIndex++] = sampleValue;
            if(writeIndex == 128)
            {
                writeIndex = 0;
                GameAudioOut?.Invoke(this, outputBuffer);
            }
        };
        m_emulator.DrawFrame += DrawFrameHandle; // 画帧事件

        GameStopped += (_, _) =>
        {
            m_GameThread = null;    // 清空游戏线程
            GameStatus = 0; // 设置游戏状态为未打开
            GameName = null; // 清空游戏名称
            GameFilePath = null; // 清空游戏文件路径
            m_emulator.Reset( ); // 重置模拟器
            m_emulator.RemoveCartridge( ); // 移除游戏卡带
        };
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
                case 1: // 运行中
                        //try
                        //{
                    m_emulator.ExecuteStep( ); // 进行一帧画面的模拟
                    //}
                    //catch(Exception ex)
                    //{
                    //    ErrorEventOccurred?.Invoke(this, ex.Message);
                    //    GameStopped?.Invoke(this, EventArgs.Empty);
                    //    return;
                    //}
                    break;
                case 2: // 暂停
                    if(m_TcsPause is not null)
                    {
                        m_TcsPause.SetResult( );    // 设置暂停任务的返回
                        m_TcsPause = null;
                        // 触发游戏停止事件
                        GamePaused?.Invoke(this, EventArgs.Empty);
                    }
                    break;
                case 3: // 停止
                    if(m_TcsStop is not null)
                    {
                        m_TcsStop.SetResult( ); // 设置停止任务的返回
                        m_TcsStop = null;
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

    public override void OpenGame(string nesFilePath)
    {
        if(!File.Exists(nesFilePath)) return; // 文件不存在
        GameName = Path.GetFileNameWithoutExtension(nesFilePath); // 获取游戏名称
        GameFilePath = nesFilePath; // 保存游戏文件路径
        m_emulator.Open(nesFilePath);
        GameStatus = 1; // 设置游戏状态为运行中
        m_GameThread = new(GameRunHandle)
        {
            IsBackground = true,
            Name = "GameThread",
        };
        m_GameThread.Start( );  // 启动游戏线程
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

    public override void SetButtonState(int Px, Controller.Buttons btn, bool state)
    {
        if(Px != 1 && Px != 2) return;  // 玩家编号错误
        m_emulator.Controller.SetButtonState(Px, btn, state);
    }

    /// <summary>
    /// 画帧事件处理函数
    /// </summary>
    private void DrawFrameHandle(object? sender, byte[] bitmapData)
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

        m_emulator.Save(writer);    // 保存游戏
    }

    public override void Load(BinaryReader reader)
    {
        if(GameStatus != 1 && GameStatus != 2) return; // 没有打开的游戏

        m_emulator.Load(reader);    // 读档
    }

    public override void Dispose( )
    {
        GameStatus = 3; // 设置游戏为停止状态
        m_GameThread?.Join( );
        GC.SuppressFinalize(this); // 防止对象被终止
    }
}

