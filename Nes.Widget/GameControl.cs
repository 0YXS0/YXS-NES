using NAudio.Wave;
using Nes.Core;
using Nes.Widget.Models;
using Nes.Widget.Palettes;
using System.IO;
using Color = System.Windows.Media.Color;

namespace Nes.Widget.Control;

internal class GameControl
{
    public const string DefaultNesFilePath = "D:\\YXS\\C#_Project\\SimpleFC\\NesFile";
    public const string DefaultSaveFilePath = "SaveFile";
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
    /// 正在运行的NES文件信息
    /// </summary>
    public NesFileInfo? NesFileInfo { get; set; }

    /// <summary>
    /// 游戏是否正在运行
    /// </summary>
    public bool IsGameRunning { get => m_emulator.Running; }

    /// <summary>
    /// 游戏画面颜色
    /// </summary>
    public byte[] Pixels { get; } = new byte[256 * 240 * 4];

    private string m_SelectedColorPaletteName = "Default";
    private ColorPalette m_selectedColorPalette;
    /// <summary>
    /// 选择的颜色调色板
    /// </summary>
    public ColorPalette SelectedColorPalette
    {
        get => m_selectedColorPalette;
        set
        {
            m_selectedColorPalette = value;
            m_SelectedColorPaletteName = ColorPalette.Palettes.First(p => p.Value == value).Key;
        }
    }

    private readonly Emulator m_emulator = new( );   // 模拟器
    private Thread m_gameThread;   // 游戏线程
    private readonly WaveOut m_waveOut;    // 音频输出
    private readonly WriteLine m_apuAudioProvider = new( );  // 音频提供器

    public GameControl( )
    {
        // 检查默认NES文件夹是否存在
        if(!Directory.Exists(DefaultNesFilePath))
            Directory.CreateDirectory(DefaultNesFilePath);

        m_waveOut = new WaveOut
        {
            DesiredLatency = 50,
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
        m_selectedColorPalette = ColorPalette.GetColorPaletteByName(m_SelectedColorPaletteName);   // 选择默认颜色调色板
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
        m_emulator.Pause( );    // 暂停模拟器
        m_waveOut.Pause( ); // 暂停音频
    }

    /// <summary>
    /// 恢复游戏
    /// </summary>
    public void ResumeGame( )
    {
        m_emulator.Resume( );   // 恢复模拟器
        m_waveOut.Play( );  // 播放音频
    }

    /// <summary>
    /// 获取NES文件信息
    /// </summary>
    /// <param name="nesFilePath">文件路径</param>
    /// <returns>（获取信息是否成功，Mapper号，是否支持）</returns>
    public static async Task<(bool, int, bool)> GetNesFileInfoAsync(string nesFilePath)
    {
        using FileStream stream = new(nesFilePath, FileMode.Open, FileAccess.Read, FileShare.Read, 4096, useAsync: true);
        using var binaryReader = new BinaryReader(stream);
        var fileName = Path.GetFileNameWithoutExtension(nesFilePath);
        byte[] raw = new byte[10];
        await stream.ReadAsync(raw); // 异步读取前10个字节

        if(BitConverter.ToInt32(raw, 0) != 0x1A53454E)
        {
            Console.WriteLine("不是有效的NES文件:" + fileName);
            return (false, -1, false);
        }
        if(((raw[7] >> 2) & 0b0000_0011) != 0)
        {
            Console.WriteLine("不支持的NES文件版本:" + fileName);
            return (false, -1, false);
        }
        if((raw[7] & 1) != 0 || (raw[7] & 2) != 0)
        {
            Console.WriteLine("文件不是有效的NES 1.0格式:" + fileName);
            return (false, -1, false);
        }
        var Num = (raw[7] & 0b1111_0000) | (raw[6] >> 4); // Mapper编号

        return (true, Num, Emulator.IsMapperSupported(Num));
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
    public void Save(BinaryWriter writer)
    {
        if(NesFileInfo is null) return; // 没有打开的游戏

        m_emulator.Save(writer);    // 保存游戏
    }

    /// <summary>
    /// 读档
    /// </summary>
    public void Load(BinaryReader reader)
    {
        if(NesFileInfo is null) return; // 没有打开的游戏

        m_emulator.Load(reader);    // 读档
    }
}

internal class WriteLine : WaveProvider32
{
    private readonly float[] cyclicBuffer = [];
    private int readIndex;
    private int writeIndex;
    private int size;
    private readonly object queueLock = new( );

    public bool Enabled { get; set; }

    public WriteLine( )
    {
        cyclicBuffer = new float[4096];
        readIndex = writeIndex = 0;
        Enabled = true;
    }

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
}

