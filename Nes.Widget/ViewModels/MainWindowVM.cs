using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Win32;
using System.IO;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace Nes.Widget.ViewModels;

partial class MainWindowVM : ObservableObject
{
    public static MainWindowVM Instance { get; internal set; } = new MainWindowVM( );

    public event EventHandler<string>? GameStartEvent;
    public event EventHandler? GamePauseEvent;
    public event EventHandler? GameSettingEvent;

    private const string m_OriginTitle = "YXS-NES · 本地";   // 原始标题
    [ObservableProperty]
    private string m_title = m_OriginTitle;

    [ObservableProperty]
    private bool m_IsPauseBtnClicked = false;

    [ObservableProperty]
    private WriteableBitmap m_BitImage = new(256, 240, 96, 96, PixelFormats.Bgra32, null);

    [RelayCommand]
    private void OpenFile( )
    {
        OpenFileDialog openFileDialog = new( )
        {
            Filter = "NES文件|*.NES|所有文件|*.*", // 设置文件类型过滤器
            InitialDirectory = "D:\\YXS\\C#_Project\\SimpleFC\\NesFile", // 设置初始目录
            Title = "打开文件", // 设置对话框标题
            Multiselect = false // 是否允许选择多个文件
        };
        if(openFileDialog.ShowDialog( ) == true)
        {
            Title = m_OriginTitle + " · " +
                Path.GetFileNameWithoutExtension(openFileDialog.FileName);
            GameStartEvent?.Invoke(this, openFileDialog.FileName);
        }
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


