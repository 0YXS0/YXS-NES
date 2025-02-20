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
                //m_VM.ConnectionState = 1;   // 设置连接状态为连接中
                m_VM.IsWindowEnabled = false;
                m_VM.OnConnectButtonClicked( ); // 触发连接按钮点击事件
            }
        };

        this.SecondaryButtonClick += (sender, e) =>
        {
            e.Cancel = true;    // 取消关闭对话框
            //m_VM.ConnectionState = 0;   // 设置连接状态为未连接
            m_VM.IsWindowEnabled = true;
            m_VM.OnDisConnectButtonClicked( ); // 触发断开连接按钮点击事件
        };
    }
}
