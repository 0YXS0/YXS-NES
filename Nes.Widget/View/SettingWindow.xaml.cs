using iNKORE.UI.WPF.Modern.Controls;
using Nes.Widget.ViewModels;

namespace Nes.Widget.View;

/// <summary>
/// SettingWindow.xaml 的交互逻辑
/// </summary>
public partial class SettingWindow : ContentDialog
{
    public SettingWindow( )
    {
        InitializeComponent( );
        DataContext = new SettingWindowVM( );
    }
}
