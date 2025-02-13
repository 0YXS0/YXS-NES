using NAudio.Wave;
using Nes.Core;
using Nes.Widget.Control.Palettes;
using Nes.Widget.Models;
using System.IO;
using Color = System.Windows.Media.Color;

namespace Nes.Widget.Control;

/// <summary>
/// 本地游戏控制器
/// </summary>
internal class GameControlLocal : GameControl
{
    /// <summary>
    /// 游戏打开事件
    /// </summary>
    public override event EventHandler? GameOpened;

    /// <summary>
    /// 游戏停止事件
    /// </summary>
    public override event EventHandler? GameStopped;

    /// <summary>
    /// 游戏画帧事件
    /// </summary>
    public override event EventHandler? GameDrawFrame;

    /// <summary>
    /// 正在运行的NES文件信息
    /// </summary>
    public override NesFileInfo? NesFileInfo { get; set; }

    /// <summary>
    /// 游戏是否正在运行
    /// </summary>
    public override bool IsGameRunning { get => m_emulator.Running; }

    /// <summary>
    /// 是否为游戏主机--true:主机，false:从机
    /// </summary>
    public override bool IsHost { get; } = true;

    /// <summary>
    /// 游戏画面像素数组
    /// </summary>
    public override byte[] Pixels { get; }

    /// <summary>
    /// 选择的颜色调色板
    /// </summary>
    public override ColorPalette SelectedColorPalette { get; set; }

    private readonly Emulator m_emulator = new( );   // 模拟器
    private Thread m_gameThread;   // 游戏线程
    private readonly WaveOut m_waveOut;    // 音频输出
    private readonly WriteLine m_apuAudioProvider = new( );  // 音频提供器

    public GameControlLocal( )
    {
        Pixels = new byte[256 * 240 * 4];
        SelectedColorPalette = ColorPalette.GetColorPaletteByName("Default");

        m_waveOut = new WaveOut
        {
            DesiredLatency = 100,
        };
        m_waveOut.Init(m_apuAudioProvider);
        float[] outputBuffer = new float[128];
        int writeIndex = 0;
        m_emulator.Apu.WriteSample = (sampleValue) =>
        {
            outputBuffer[writeIndex++] = sampleValue;
            writeIndex %= outputBuffer.Length;
            if(writeIndex == 0)
                m_apuAudioProvider.Queue(outputBuffer);
        };

        m_gameThread = new Thread(( ) => { Thread.Sleep(1000); });
        m_emulator.DrawFrame += DrawFrameHandle; // 画帧事件
    }

    /// <summary>
    /// 打开游戏
    /// </summary>
    /// <param name="fileName"></param>
    public override void OpenGame(string fileName)
    {
        m_emulator.Open(fileName);
        m_gameThread = new Thread(m_emulator.Run)
        {
            IsBackground = true, // 后台线程
            Name = "GameThread"
        };
        m_gameThread.Start( );
        m_waveOut.Play( );  // 播放音频
        GameOpened?.Invoke(this, EventArgs.Empty);  // 触发游戏打开事件
    }

    /// <summary>
    /// 重置游戏
    /// </summary>
    public override void ResetGame( )
    {
        m_emulator.Reset( );
    }

    /// <summary>
    /// 结束游戏
    /// </summary>
    public override void StopGame( )
    {
        m_emulator.Stop( );
        m_waveOut.Stop( );  // 停止音频
        // 等待游戏线程结束
        if(m_gameThread.IsAlive)
            m_gameThread.Join( );
        GameStopped?.Invoke(this, EventArgs.Empty); // 触发游戏停止事件
    }

    /// <summary>
    /// 暂停游戏
    /// </summary>
    public override void PauseGame( )
    {
        m_emulator.Pause( );    // 暂停模拟器
        m_waveOut.Pause( ); // 暂停音频
    }

    /// <summary>
    /// 恢复游戏
    /// </summary>
    public override void ResumeGame( )
    {
        m_emulator.Resume( );   // 恢复模拟器
        m_waveOut.Play( );  // 播放音频
    }

    /// <summary>
    /// 设置按钮状态
    /// </summary>
    /// <param name="Px">玩家编号:1--P1，2--P2</param>
    /// <param name="btn">按钮</param>
    /// <param name="state">状态</param>
    public override void SetButtonState(int Px, Controller.Buttons btn, bool state)
    {
        if(Px != 1 && Px != 2) return;  // 玩家编号错误
        m_emulator.Controller.SetButtonState(Px, btn, state);
    }

    /// <summary>
    /// 画帧事件处理函数
    /// </summary>
    private void DrawFrameHandle(object? sender, DrawFrameEventArgs e)
    {
        var colorBytes = e.BitmapData;
        Parallel.For(0, 256 * 240, i =>
        {
            Color color = SelectedColorPalette.Colors[colorBytes[i]];
            Pixels[i * 4 + 0] = color.B;
            Pixels[i * 4 + 1] = color.G;
            Pixels[i * 4 + 2] = color.R;
            Pixels[i * 4 + 3] = color.A;
        });
        GameDrawFrame?.Invoke(this, EventArgs.Empty); // 触发游戏画帧事件
    }

    /// <summary>
    /// 存档
    /// </summary>
    public override void Save(BinaryWriter writer)
    {
        if(NesFileInfo is null) return; // 没有打开的游戏

        m_emulator.Save(writer);    // 保存游戏
    }

    /// <summary>
    /// 读档
    /// </summary>
    public override void Load(BinaryReader reader)
    {
        if(NesFileInfo is null) return; // 没有打开的游戏

        m_emulator.Load(reader);    // 读档
    }
}

