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

    /// <summary>
    /// 选择的控制器设置
    /// </summary>
    //public ControllerSetting SelectedControllerSetting { get; set; }

    public Thread GameThread { get => m_gameThread; }

    private readonly Emulator m_emulator = new( );   // 模拟器
    private readonly Thread m_gameThread;   // 游戏线程
    private readonly byte[] m_Pixels = new byte[256 * 240 * 4];   // 游戏画面颜色

    public GameControl( )
    {
        m_gameThread = new Thread(m_emulator.Run)
        {
            IsBackground = true // 后台线程
        };
        m_emulator.DrawFrame += Emulator_DrawFrame; // 画帧事件

        SelectedColorPalette = ColorPalette.GetColorPaletteByName("Default");   // 选择默认颜色调色板
        //SelectedControllerSetting = ControllerSetting.Default;  // 选择默认控制器设置
    }

    /// <summary>
    /// 打开游戏
    /// </summary>
    /// <param name="fileName"></param>
    public void OpenGame(string fileName)
    {
        m_emulator.Open(fileName);
        m_gameThread.Start( );
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
        // 等待游戏线程结束
        if(m_gameThread?.ThreadState == ThreadState.Running) m_gameThread.Join( );
        GameStopped?.Invoke(this, EventArgs.Empty); // 触发游戏停止事件
    }


    /// <summary>
    /// 暂停游戏
    /// </summary>
    public void PauseGame( )
    {
        m_emulator.Pause( );
    }

    /// <summary>
    /// 恢复游戏
    /// </summary>
    public void ResumeGame( )
    {
        m_emulator.Resume( );
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
