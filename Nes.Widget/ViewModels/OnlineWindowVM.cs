using CommunityToolkit.Mvvm.ComponentModel;
using Nes.Core.Control;
using System.ComponentModel.DataAnnotations;

namespace Nes.Widget.ViewModels;

internal partial class OnlineWindowVM : ObservableValidator
{
    public event EventHandler<GameControlType>? DisConnectButtonClickedEvent;
    public event EventHandler<GameControlType>? ConnectButtonClickedEvent;

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
    private bool m_IsAddressEnabled = false;    // 是否启用地址输入框

    [ObservableProperty]
    private bool m_IsPortEnabled = false;    // 是否启用端口输入框

    [ObservableProperty]
    public bool m_IsWindowEnabled = true;

    [ObservableProperty]
    private int m_SelectedIndex = -1;    // 选中的模式索引
    partial void OnSelectedIndexChanged(int value)
    {
        IsPortEnabled = true;
        IsAgreementCodeEnabled = m_SelectedIndex == 2;
        IsAddressEnabled = m_SelectedIndex != 0;
    }

    /// <summary>
    /// 连接状态---0:未连接, 1:连接中, 2:已连接, 3:连接失败
    /// </summary>
    public int ConnectionState
    {
        get;
        set
        {
            SetProperty(ref field, value);
        }
    } = 0;

    internal void OnDisConnectButtonClicked( )
    {
        DisConnectButtonClickedEvent?.Invoke(this, SelectedIndex switch
        {
            0 => GameControlType.LANHost,
            1 => GameControlType.INTEHost,
            2 => GameControlType.Salve,
            _ => GameControlType.Local
        });
    }

    internal void OnConnectButtonClicked( )
    {
        ConnectButtonClickedEvent?.Invoke(this, SelectedIndex switch
        {
            0 => GameControlType.LANHost,
            1 => GameControlType.INTEHost,
            2 => GameControlType.Salve,
            _ => GameControlType.Local
        });
    }
}
