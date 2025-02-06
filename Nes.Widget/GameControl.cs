using NAudio.Wave;
using NesEmu.Console.Palettes;
using NesEmu.Core;

using Color = System.Windows.Media.Color;

namespace NesEmu.Console;

internal class GameControl
{
    /// <summary>
    /// 游戏打开事件
    /// </summary>
    public event EventHandler? GameOpened;

    /// <summary>
    /// 游戏停止事件
    /// </summary>
    public event EventHandler? GameStopped;

    /// <summary>
    /// 游戏画帧事件
    /// </summary>
    public event EventHandler? GameDrawFrame;

    /// <summary>
    /// 游戏是否正在运行
    /// </summary>
    public bool IsGameRunning { get => m_emulator.Running; }

    /// <summary>
    /// 游戏画面颜色
    /// </summary>
    public byte[] Pixels { get => m_Pixels; }

    /// <summary>
    /// 选择的颜色调色板
    /// </summary>
    public ColorPalette SelectedColorPalette { get; set; }

    public Thread GameThread { get => m_gameThread; }

    private readonly Emulator m_emulator = new( );   // 模拟器
    private Thread m_gameThread;   // 游戏线程
    private readonly byte[] m_Pixels = new byte[256 * 240 * 4];   // 游戏画面颜色
    private readonly WaveOut m_waveOut;    // 音频输出
    private readonly ApuAudioProvider m_apuAudioProvider = new( );  // 音频提供器

    public GameControl( )
    {
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

        m_gameThread = new Thread(( ) => { });
        m_emulator.DrawFrame += Emulator_DrawFrame; // 画帧事件
        SelectedColorPalette = ColorPalette.GetColorPaletteByName("Default");   // 选择默认颜色调色板
    }

    /// <summary>
    /// 打开游戏
    /// </summary>
    /// <param name="fileName"></param>
    public void OpenGame(string fileName)
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
    public void ResetGame( )
    {
        m_emulator.Reset( );
    }

    /// <summary>
    /// 结束游戏
    /// </summary>
    public void StopGame( )
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
    public void PauseGame( )
    {
        m_emulator.Pause( );
        m_waveOut.Pause( ); // 暂停音频
    }

    /// <summary>
    /// 恢复游戏
    /// </summary>
    public void ResumeGame( )
    {
        m_emulator.Resume( );
        m_waveOut.Play( );  // 播放音频
    }

    /// <summary>
    /// 设置按钮状态
    /// </summary>
    /// <param name="Px">玩家编号:1--P1，2--P2</param>
    /// <param name="btn">按钮</param>
    /// <param name="state">状态</param>
    public void SetButtonState(int Px, Controller.Buttons btn, bool state)
    {
        if(Px != 1 && Px != 2) return;  // 玩家编号错误
        m_emulator.Controller.SetButtonState(Px, btn, state);
    }

    private void Emulator_DrawFrame(object? sender, DrawFrameEventArgs e)
    {
        var colorBytes = e.BitmapData;
        Parallel.For(0, 256 * 240, i =>
        {
            Color color = SelectedColorPalette.Colors[colorBytes[i]];
            m_Pixels[i * 4 + 0] = color.B;
            m_Pixels[i * 4 + 1] = color.G;
            m_Pixels[i * 4 + 2] = color.R;
            m_Pixels[i * 4 + 3] = color.A;
        });
        GameDrawFrame?.Invoke(this, EventArgs.Empty); // 触发游戏画帧事件
    }
}

public class ApuAudioProvider : WaveProvider32
{
    public ApuAudioProvider( )
    {
        cyclicBuffer = new float[4096];
        readIndex = writeIndex = 0;
        Enabled = true;
    }

    public bool Enabled { get; set; }

    public override int Read(float[] buffer, int offset, int sampleCount)
    {
        lock(queueLock)
        {
            if(!Enabled || size == 0)
            {
                buffer[offset] = 0;
                return 1;
            }

            sampleCount = Math.Min(sampleCount, size);

            for(int n = 0; n < sampleCount; n++)
            {
                buffer[n + offset] = cyclicBuffer[readIndex++];
                readIndex %= cyclicBuffer.Length;
                --size;
            }
            return sampleCount;
        }
    }

    public void Queue(float[] sampleValues)
    {
        lock(queueLock)
        {
            for(int index = 0; index < sampleValues.Length; index++)
            {
                if(size >= cyclicBuffer.Length)
                    return;

                cyclicBuffer[writeIndex] = sampleValues[index];
                ++writeIndex;
                writeIndex %= cyclicBuffer.Length;
                ++size;
            }
        }
    }

    private readonly float[] cyclicBuffer = new float[8192];
    private int readIndex;
    private int writeIndex;
    private int size;
    private readonly object queueLock = new object( );
}

