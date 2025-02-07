using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace Nes.Widget.ViewModels;

partial class MainWindowVM : ObservableObject
{
    public static MainWindowVM Instance { get; } = new( ); // 单例
    public event EventHandler? GameOpenEvent;
    public event EventHandler? GamePauseEvent;
    public event EventHandler? GameSettingEvent;

    public const string OriginTitle = "YXS-NES · 本地";   // 原始标题
    [ObservableProperty]
    private string m_title = OriginTitle;

    [ObservableProperty]
    private bool m_IsPauseBtnClicked = false;

    [ObservableProperty]
    private WriteableBitmap m_BitImage = new(256, 240, 96, 96, PixelFormats.Bgra32, null);

    [RelayCommand]
    private void OpenFile( )
    {
        GameOpenEvent?.Invoke(this, EventArgs.Empty);
    }

    [RelayCommand]
    private void PauseGame( )
    {
        GamePauseEvent?.Invoke(this, EventArgs.Empty);
    }

    [RelayCommand]
    private void Setting( )
    {
        GameSettingEvent?.Invoke(this, EventArgs.Empty);
    }
}


