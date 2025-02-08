using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace Nes.Widget.ViewModels;

partial class MainWindowVM : ObservableObject
{
    public static MainWindowVM Instance { get; } = new( ); // 单例
    public event EventHandler? GameOpenButtonClickedEvent;  // 打开游戏按钮点击事件
    public event EventHandler? GamePauseButtonClickedEvent; // 暂停游戏按钮点击事件
    public event EventHandler? GameSaveButtonClickedEvent;  // 保存游戏按钮点击事件
    public event EventHandler? GameLoadButtonClickedEvent;  // 加载游戏按钮点击事件
    public event EventHandler? GameOnlineButtonClickedEvent;  // 在线游戏按钮点击事件
    public event EventHandler? SettingButtonClickedEvent;   // 设置按钮点击事件

    public const string OriginTitle = "YXS-NES · 本地";   // 原始标题
    [ObservableProperty]
    private string m_title = OriginTitle;

    [ObservableProperty]
    private bool m_IsPauseBtnClicked = false;

    [ObservableProperty]
    private bool m_IsOnlineBtnClicked = false;

    [ObservableProperty]
    private WriteableBitmap m_BitImage = new(256, 240, 96, 96, PixelFormats.Bgra32, null);

    [RelayCommand]
    private void OpenFile( )
    {
        GameOpenButtonClickedEvent?.Invoke(this, EventArgs.Empty);
    }

    [RelayCommand]
    private void PauseGame( )
    {
        GamePauseButtonClickedEvent?.Invoke(this, EventArgs.Empty);
    }

    [RelayCommand]
    private void Setting( )
    {
        SettingButtonClickedEvent?.Invoke(this, EventArgs.Empty);
    }

    [RelayCommand]
    private void SaveGame( )
    {
        GameSaveButtonClickedEvent?.Invoke(this, EventArgs.Empty);
    }

    [RelayCommand]
    private void LoadGame( )
    {
        GameLoadButtonClickedEvent?.Invoke(this, EventArgs.Empty);
    }

    [RelayCommand]
    private void OnlineGame( )
    {
        GameOnlineButtonClickedEvent?.Invoke(this, EventArgs.Empty);
    }
}


