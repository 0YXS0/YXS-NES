using Nes.Core.Control.Palettes;
using System;
using System.IO;
using System.Threading.Tasks;

namespace Nes.Core.Control;

public enum GameControlType
{
    Local,  // 本地主机
    LANHost,    // 局域网主机
    INTEHost,   // 互联网主机
    Salve,  // 从机
}

/// <summary>
/// 游戏控制器基类
/// </summary>
public abstract class GameControl : IDisposable
{
    /// <summary>
    /// 游戏打开事件
    /// </summary>
    public abstract event EventHandler? GameOpened;

    /// <summary>
    /// 游戏停止事件
    /// </summary>
    public abstract event EventHandler? GameStopped;

    /// <summary>
    /// 游戏暂停事件
    /// </summary>
    public abstract event EventHandler? GamePaused;

    /// <summary>
    /// 游戏从暂停中恢复事件
    /// </summary>
    public abstract event EventHandler? GameResumed;

    /// <summary>
    /// 游戏重置事件
    /// </summary>
    public abstract event EventHandler? GameReseted;

    /// <summary>
    /// 游戏画帧事件
    /// </summary>
    public abstract event EventHandler<byte[]>? GameDrawFrame;

    /// <summary>
    /// 游戏音频输出事件
    /// </summary>
    public abstract event EventHandler<float[]>? GameAudioOut;

    /// <summary>
    /// 发生错误事件---错误信息
    /// </summary>
    public abstract event EventHandler<string>? ErrorEventOccurred;

    /// <summary>
    /// 正在运行的游戏名称
    /// </summary>
    public abstract string? GameName { get; protected set; }

    /// <summary>
    /// 正在运行的游戏文件路径
    /// </summary>
    public abstract string? GameFilePath { get; protected set; }

    /// <summary>
    /// 游戏状态---0:未打开, 1:运行中, 2:暂停, 3:停止, other:未知
    /// </summary>
    public abstract int GameStatus { get; protected set; }

    /// <summary>
    /// 控制器类型
    /// </summary>
    public abstract GameControlType Type { get; }

    /// <summary>
    /// 选择的颜色调色板
    /// </summary>
    public abstract ColorPalette SelectedColorPalette { get; set; }

    /// <summary>
    /// 是否启用P1
    /// </summary>
    public abstract bool IsP1Enabled { get; }

    /// <summary>
    /// 是否启用P2
    /// </summary>
    public abstract bool IsP2Enabled { get; }

    /// <summary>
    /// 打开游戏
    /// </summary>
    /// <param name="fileName">游戏文件路径</param>
    public abstract void OpenGame(string fileName);

    /// <summary>
    /// 重置游戏
    /// </summary>
    public abstract void ResetGame( );

    /// <summary>
    /// 结束游戏
    /// </summary>
    public abstract Task StopGame( );

    /// <summary>
    /// 暂停游戏
    /// </summary>
    public abstract Task PauseGame( );

    /// <summary>
    /// 从暂停中恢复游戏
    /// </summary>
    public abstract void ResumeGame( );

    /// <summary>
    /// 开启连接服务
    /// </summary>
    public virtual void Connect(string ip = "127.0.0.1", int port = 55666) { }

    /// <summary>
    /// 断开连接
    /// </summary>
    public virtual void DisConnect( ) { }

    /// <summary>
    /// 释放资源
    /// </summary>
    public abstract void Dispose( );

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
            return (false, -1, false);
        }
        if(((raw[7] >> 2) & 0b0000_0011) != 0)
        {
            return (false, -1, false);
        }
        if((raw[7] & 1) != 0 || (raw[7] & 2) != 0)
        {
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