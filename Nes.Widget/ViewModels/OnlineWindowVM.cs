using CommunityToolkit.Mvvm.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace Nes.Widget.ViewModels;

internal partial class OnlineWindowVM : ObservableValidator
{
    public event EventHandler<bool>? ConnectEvent;

    [Required(ErrorMessage = "IP地址不能为空")]
    [RegularExpression(@"^((25[0-5]|2[0-4][0-9]|[01]?[0-9][0-9]?)\.){3}(25[0-5]|2[0-4][0-9]|[01]?[0-9][0-9]?)$", ErrorMessage = "无效的IP地址")]
    [NotifyDataErrorInfo]
    [ObservableProperty]
    private string m_ServerAddr = "127.0.0.1";   // 服务器地址

    [Required(ErrorMessage = "端口不能为空")]
    [RegularExpression(@"^([0-9]|[1-9][0-9]{1,3}|[1-5][0-9]{4}|6[0-4][0-9]{3}|65[0-4][0-9]{2}|655[0-2][0-9]|6553[0-5])$", ErrorMessage = "端口号必须在0到65535之间")]
    [NotifyDataErrorInfo]
    [ObservableProperty]
    private string m_ServerPort = "55666";   // 服务器端口

    [RegularExpression(@"^[0-9]{6}$", ErrorMessage = "约定码必须是6位数字")]
    [NotifyDataErrorInfo]
    [ObservableProperty]
    private string m_AgreementCode = "123456";    // 约定码

    [ObservableProperty]
    private bool m_IsAgreementCodeEnabled = false;  // 是否启用约定码输入框

    [ObservableProperty]
    private int m_SelectedIndex = 0;    // 选中的模式索引
    partial void OnSelectedIndexChanged(int value)
    {
        IsAgreementCodeEnabled = m_SelectedIndex == 2;
    }

    [ObservableProperty]
    private bool m_IsConnecting = false; // 是否正在连接
    partial void OnIsConnectingChanged(bool value)
    {
        IsConnected = false;
        if(value)
        {
            ConnectEvent?.Invoke(this, true);
        }
    }

    [ObservableProperty]
    private bool m_IsConnected = false; // 是否连接成功
}
