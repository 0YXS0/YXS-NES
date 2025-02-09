using iNKORE.UI.WPF.Modern.Controls;
using Nes.Core;
using Nes.Widget.Control;
using Nes.Widget.Models;
using Nes.Widget.ViewModels;
using System.IO;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using MessageBox = iNKORE.UI.WPF.Modern.Controls.MessageBox;

namespace Nes.Widget.View;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    private readonly GameControl m_GameControl = new( );
    private readonly MainWindowVM m_MainWindowVM = MainWindowVM.Instance;
    private readonly SettingWindow m_SettingWindow = new( );
    private readonly SettingWindowVM m_SettingWindowVM;
    private readonly SelectNesFileWindow m_SelectNesFileWindow = new( );
    private readonly SelectNesFileWindowVM m_SelectNesFileWindowVM;
    private static readonly JsonSerializerOptions JsonSerializerOptions = new( )
    {
        WriteIndented = true,   // 缩进
        Converters = { new JsonStringEnumConverter( ) } // 使用数字表示枚举
    };

    public MainWindow( )
    {
        InitializeComponent( );
        DataContext = m_MainWindowVM;
        m_SettingWindowVM = (SettingWindowVM)m_SettingWindow.DataContext;
        m_SelectNesFileWindowVM = (SelectNesFileWindowVM)m_SelectNesFileWindow.DataContext;
        this.KeyDown += KeyDownHandle;
        this.KeyUp += KeyUphandle;
        this.MouseDown += MouseDownHandle;
        this.MouseUp += MouseUpHandle;
        m_GameControl.GameDrawFrame += DrawFrame; // 画帧事件

#if DEBUG   // 调试时让窗口始终在最上层, 方便调试
        //this.Topmost = true;
#endif

        m_SelectNesFileWindowVM.SelectedNesFileEvent += (object? sender, NesFileInfo info) =>
        {
            if(!info.IsSupported)
            {
                MessageBox.Show("不支持的文件格式");
                return;
            }
            new Thread(( ) =>
            {
                if(m_GameControl.IsGameRunning)
                    m_GameControl.StopGame( );  // 会阻塞调用线程, 所以要放在新线程中,防止阻塞主线程
                m_GameControl.OpenGame(info.Path);
            })
            {
                IsBackground = true,
                Name = "OpenGameThread",
            }.Start( );
            m_MainWindowVM.Title = MainWindowVM.OriginTitle + " · " + info.Name;
            m_GameControl.NesFileInfo = info;   // 保存文件信息
            m_SelectNesFileWindow.Hide( );  // 隐藏选择文件窗口
        };

        m_MainWindowVM.GameOpenButtonClickedEvent += async (object? sender, EventArgs e) =>
        {
            await m_SelectNesFileWindowVM.SelectnesFile(GameControl.DefaultNesFilePath);
            await m_SelectNesFileWindow.ShowAsync( );
        };

        m_MainWindowVM.GamePauseButtonClickedEvent += (object? sender, EventArgs e) =>
        {
            if(m_GameControl.IsGameRunning)
            {
                if(m_MainWindowVM.IsPauseBtnClicked)
                    m_GameControl.PauseGame( );
                else
                    m_GameControl.ResumeGame( );
            }
        };

        m_MainWindowVM.SettingButtonClickedEvent += async (object? sender, EventArgs e) =>
        {
            var res = await m_SettingWindow.ShowAsync( );
            if(res == ContentDialogResult.Primary)
            {// 将设置序列化到文件
                string str = JsonSerializer.Serialize(m_SettingWindowVM, JsonSerializerOptions);
                File.WriteAllText("setting.json", str);
            }
            else
            {// 取消设置, 从文件中重新加载设置
                if(File.Exists("setting.json"))
                {
                    string str = File.ReadAllText("setting.json");
                    SettingWindowVM VM = JsonSerializer.Deserialize<SettingWindowVM>(str, JsonSerializerOptions)
                        ?? m_SettingWindowVM;

                    var props = m_SettingWindowVM.GetType( ).GetProperties(BindingFlags.Public | BindingFlags.Instance);
                    foreach(var prop in props)
                    {
                        prop.SetValue(m_SettingWindowVM, prop.GetValue(VM));
                    }
                }
            }
        };

        m_MainWindowVM.GameSaveButtonClickedEvent += GameSave;
        m_MainWindowVM.GameLoadButtonClickedEvent += GameLoad;

        // 从文件中加载设置
        if(File.Exists("setting.json"))
        {
            m_SettingWindow.DataContext = null;
            string str = File.ReadAllText("setting.json");
            var VM = JsonSerializer.Deserialize<SettingWindowVM>(str, JsonSerializerOptions);
            m_SettingWindowVM = VM ?? m_SettingWindowVM;
            m_SettingWindow.DataContext = m_SettingWindowVM;
            if(VM == null)
            {
                Console.WriteLine("读取setting.json设置文件失败。");
            }
        }
    }

    private void GameLoad(object? sender, EventArgs e)
    {
        var fileName = m_GameControl.NesFileInfo?.Name + ".save";
        using BinaryReader reader = new(File.Open(fileName, FileMode.Open));
        m_GameControl.Load(reader);
    }

    private void GameSave(object? sender, EventArgs e)
    {
        var fileName = m_GameControl.NesFileInfo?.Name + ".save";
        using BinaryWriter writer = new(File.Open(fileName, FileMode.Create));
        m_GameControl.Save(writer);
    }

    private void MouseUpHandle(object sender, MouseButtonEventArgs e)
    {
        var key = ControlKey.ToKeyType(e.ChangedButton);
        ProcessKey(1, key, false);
        ProcessKey(2, key, false);
    }

    private void MouseDownHandle(object sender, MouseButtonEventArgs e)
    {
        var key = ControlKey.ToKeyType(e.ChangedButton);
        ProcessKey(1, key, true);
        ProcessKey(2, key, true);
    }

    private void KeyUphandle(object sender, KeyEventArgs e)
    {
        var key = ControlKey.ToKeyType(e.Key);
        ProcessKey(1, key, false);
        ProcessKey(2, key, false);
    }

    private void KeyDownHandle(object sender, KeyEventArgs e)
    {
        var key = ControlKey.ToKeyType(e.Key);
        ProcessKey(1, key, true);
        ProcessKey(2, key, true);
    }

    private void ProcessKey(int Px, ControlKey.KeyType key, bool state)
    {
        if(key == ControlKey.ToKeyType(m_SettingWindowVM.P1Up))
            m_GameControl.SetButtonState(Px, Controller.Buttons.Up, state);
        else if(key == ControlKey.ToKeyType(m_SettingWindowVM.P1Down))
            m_GameControl.SetButtonState(Px, Controller.Buttons.Down, state);
        else if(key == ControlKey.ToKeyType(m_SettingWindowVM.P1Left))
            m_GameControl.SetButtonState(Px, Controller.Buttons.Left, state);
        else if(key == ControlKey.ToKeyType(m_SettingWindowVM.P1Right))
            m_GameControl.SetButtonState(Px, Controller.Buttons.Right, state);
        else if(key == ControlKey.ToKeyType(m_SettingWindowVM.P1A))
            m_GameControl.SetButtonState(Px, Controller.Buttons.A, state);
        else if(key == ControlKey.ToKeyType(m_SettingWindowVM.P1B))
            m_GameControl.SetButtonState(Px, Controller.Buttons.B, state);
        else if(key == ControlKey.ToKeyType(m_SettingWindowVM.P1Start))
            m_GameControl.SetButtonState(Px, Controller.Buttons.Start, state);
        else if(key == ControlKey.ToKeyType(m_SettingWindowVM.P1Select))
            m_GameControl.SetButtonState(Px, Controller.Buttons.Select, state);
    }

    /// <summary>
    /// 显示出一帧画面
    /// </summary>
    private void DrawFrame(object? sender, EventArgs e)
    {
        Dispatcher.BeginInvoke(( ) =>
        {
            WriteableBitmap bitmap = m_MainWindowVM.BitImage;
            bitmap.WritePixels(new Int32Rect(0, 0, 256, 240), m_GameControl.Pixels, 256 * 4, 0);
        });
    }
}


