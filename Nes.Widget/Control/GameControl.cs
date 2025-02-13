using NAudio.Wave;
using Nes.Core;
using Nes.Widget.Control.Palettes;
using Nes.Widget.Models;
using System.IO;

namespace Nes.Widget.Control;

/// <summary>
/// 游戏控制器基类
/// </summary>
internal abstract class GameControl
{
    public const string DefaultNesFilePath = "D:\\YXS\\C#_Project\\SimpleFC\\NesFile";
    public const string DefaultSaveFilePath = "SaveFile";

    /// <summary>
    /// 游戏打开事件
    /// </summary>
    public abstract event EventHandler? GameOpened;

    /// <summary>
    /// 游戏停止事件
    /// </summary>
    public abstract event EventHandler? GameStopped;

    /// <summary>
    /// 游戏画帧事件
    /// </summary>
    public abstract event EventHandler? GameDrawFrame;

    /// <summary>
    /// 正在运行的NES文件信息
    /// </summary>
    public abstract NesFileInfo? NesFileInfo { get; set; }

    /// <summary>
    /// 游戏是否正在运行
    /// </summary>
    public abstract bool IsGameRunning { get; }

    /// <summary>
    /// 是否为游戏主机--true:主机，false:从机
    /// </summary>
    public abstract bool IsHost { get; }

    /// <summary>
    /// 游戏画面像素数组
    /// </summary>
    public abstract byte[] Pixels { get; }

    /// <summary>
    /// 选择的颜色调色板
    /// </summary>
    public abstract ColorPalette SelectedColorPalette { get; set; }

    public GameControl( )
    {
        // 创建Nes文件目录
        Directory.CreateDirectory(DefaultNesFilePath);
    }

    /// <summary>
    /// 打开游戏
    /// </summary>
    /// <param name="fileName"></param>
    public abstract void OpenGame(string fileName);

    /// <summary>
    /// 重置游戏
    /// </summary>
    public abstract void ResetGame( );

    /// <summary>
    /// 结束游戏
    /// </summary>
    public abstract void StopGame( );

    /// <summary>
    /// 暂停游戏
    /// </summary>
    public abstract void PauseGame( );

    /// <summary>
    /// 从暂停中恢复游戏
    /// </summary>
    public abstract void ResumeGame( );

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
    public abstract void SetButtonState(int Px, Controller.Buttons btn, bool state);

    /// <summary>
    /// 存档
    /// </summary>
    public abstract void Save(BinaryWriter writer);

    /// <summary>
    /// 读档
    /// </summary>
    public abstract void Load(BinaryReader reader);
}

/// <summary>
/// 音频提供器
/// </summary>
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
