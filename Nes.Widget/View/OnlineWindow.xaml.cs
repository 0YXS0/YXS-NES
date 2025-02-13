using iNKORE.UI.WPF.Modern.Controls;
using Nes.Widget.ViewModels;
using System.Windows;
using MessageBox = iNKORE.UI.WPF.Modern.Controls.MessageBox;

namespace Nes.Widget.View;

/// <summary>
/// OnlineWindow.xaml 的交互逻辑
/// </summary>
public partial class OnlineWindow : ContentDialog
{
    private readonly OnlineWindowVM m_VM = new( );
    public OnlineWindow( )
    {
        InitializeComponent( );
        this.DataContext = m_VM;
        this.PrimaryButtonClick += (sender, e) =>
        {
            e.Cancel = true;    // 取消关闭对话框
            if(m_VM.HasErrors)
            {
                // 获取错误集合
                var errors = m_VM.GetErrors( );
                // 提取每个 ValidationResult 的 ErrorMessage 属性，并将其转换为字符串集合
                var errorMessages = errors.Select(error => error.ErrorMessage);
                // 使用换行符将错误消息拼接成一个字符串
                string message = string.Join("\n", errorMessages);
                // 显示 MessageBox
                MessageBox.Show(message, "错误的连接信息", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            else
            {
                m_VM.IsConnecting = true;
            }
        };

        this.SecondaryButtonClick += (sender, e) =>
        {
            e.Cancel = true;    // 取消关闭对话框
            m_VM.IsConnecting = false;
        };
    }
}
