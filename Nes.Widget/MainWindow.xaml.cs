using Nes.Widget.ViewModels;
using NesEmu.Console;
using System.Windows;
using System.Windows.Media.Imaging;

namespace Nes.Widget;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    private readonly GameControl m_GameControl = new( );
    private readonly MainWindowVM m_MainWindowVM = MainWindowVM.Instance;

    public MainWindow( )
    {
        InitializeComponent( );
        DataContext = m_MainWindowVM;
        m_GameControl.GameDrawFrame += DrawFrame; // 画帧事件

        m_MainWindowVM.GameStartEvent += (object? sender, string fileName) =>
        {
            if(m_GameControl.IsGameRunning)
                m_GameControl.StopGame( );
            m_GameControl.OpenGame(fileName);
        };

        m_MainWindowVM.GamePauseEvent += (object? sender, EventArgs e) =>
        {
            if(m_GameControl.IsGameRunning)
            {
                if(m_MainWindowVM.IsPauseBtnClicked)
                    m_GameControl.PauseGame( );
                else
                    m_GameControl.ResumeGame( );
            }
        };

        m_MainWindowVM.GameSettingEvent += async (object? sender, EventArgs e) =>
        {
            SettingWindow settingWindow = new( );
            var res = await settingWindow.ShowAsync( );
        };
    }

    private void DrawFrame(object? sender, EventArgs e)
    {
        Dispatcher.Invoke(( ) =>
        {
            WriteableBitmap bitmap = MainWindowVM.Instance.BitImage;
            bitmap.WritePixels(new Int32Rect(0, 0, 256, 240), m_GameControl.Pixels, 256 * 4, 0);
        });
    }
}