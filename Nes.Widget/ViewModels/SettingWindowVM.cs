using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.Xaml.Behaviors;
using Nes.Console.Models;
using System.Windows.Controls.Primitives;
using System.Windows.Input;

namespace Nes.Widget.ViewModels;

internal partial class SettingWindowVM : ObservableObject
{
    public static SettingWindowVM Instance { get; } = new( );

    [ObservableProperty]
    private string m_P1Up = "按键W";

    [ObservableProperty]
    private string m_P1Down = "按键S";

    [ObservableProperty]
    private string m_P1Left = "按键A";

    [ObservableProperty]
    private string m_P1Right = "按键D";

    [ObservableProperty]
    private string m_P1A = "按键K";

    [ObservableProperty]
    private string m_P1B = "按键J";

    [ObservableProperty]
    private string m_P1Start = "按键B";

    [ObservableProperty]
    private string m_P1Select = "按键N";

    [ObservableProperty]
    private string m_P2Up = "按键↑";

    [ObservableProperty]
    private string m_P2Down = "按键↓";

    [ObservableProperty]
    private string m_P2Left = "按键←";

    [ObservableProperty]
    private string m_P2Right = "按键→";

    [ObservableProperty]
    private string m_P2A = "按键NumPad1";

    [ObservableProperty]
    private string m_P2B = "按键NumPad2";

    [ObservableProperty]
    private string m_P2Start = "按键NumPad3";

    [ObservableProperty]
    private string m_P2Select = "按键NumPad4";
}

internal class SelectKeyBehavior : Behavior<ToggleButton>
{
    private static bool m_haveButtonClicked = false;   // 是否有按钮已经被点击

    protected override void OnAttached( )
    {
        AssociatedObject.Click += static (sender, _) =>
        {
            if(sender is ToggleButton obj)
            {
                m_haveButtonClicked = true;
                obj.Content = "按键?";
            }
        };
        AssociatedObject.PreviewMouseDown += PreviewMouseDownHandle;
        AssociatedObject.PreviewKeyDown += PreviewKeyDownHandle;
    }

    private void PreviewKeyDownHandle(object sender, KeyEventArgs e)
    {
        if(sender is ToggleButton obj)
        {
            e.Handled = true;// 取消按键的默认行为
            if(obj.IsChecked == false)
                return;
            obj.Content = ControlKey.KeyTypeToString(ControlKey.ToKeyType(e.Key));
            obj.IsChecked = false;
            m_haveButtonClicked = false;
        }
    }

    private void PreviewMouseDownHandle(object sender, MouseButtonEventArgs e)
    {
        if(sender is ToggleButton obj)
        {
            if(obj.IsChecked == false)
            {
                if(m_haveButtonClicked)
                    e.Handled = true;   // 已经有按钮处于点击状态, 则取消当前按钮的点击行为
                return;
            }
            e.Handled = true;   // 取消默认行为
            obj.IsChecked = false;
            obj.Content = ControlKey.KeyTypeToString(ControlKey.ToKeyType(e.ChangedButton));
            m_haveButtonClicked = false;
        }
    }
}