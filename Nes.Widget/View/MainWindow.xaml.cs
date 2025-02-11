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
    private readonly SelectSaveFileWindow m_SelectSaveFileWindow = new( );
    private readonly SelectSaveFileWindowVM m_SelectSaveFileWindowVM;
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
        m_SelectSaveFileWindowVM = (SelectSaveFileWindowVM)m_SelectSaveFileWindow.DataContext;
        this.KeyDown += KeyDownHandle;
        this.KeyUp += KeyUphandle;
        this.MouseDown += MouseDownHandle;
        this.MouseUp += MouseUpHandle;
        m_GameControl.GameDrawFrame += DrawFrame; // 画帧事件
        m_SelectSaveFileWindowVM.SelectedSaveFileEvent += GameSave; // 存档事件
        m_SelectSaveFileWindowVM.SelectedLoadFileEvent += GameLoad; // 读档事件
        // 进行存档信息的读取以及信息显示的初始化
        m_SelectSaveFileWindow.Opened += SelectSaveFileWindow_OpenEventHandle;

#if DEBUG   // 调试时让窗口始终在最上层, 方便调试
        //this.Topmost = true;
#endif

        m_SelectNesFileWindowVM.SelectedNesFileEvent += (object? _, NesFileInfo info) =>
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

        m_MainWindowVM.GameOpenButtonClickedEvent += async (_, _) =>
        {
            await m_SelectNesFileWindowVM.SelectnesFile(GameControl.DefaultNesFilePath);
            await m_SelectNesFileWindow.ShowAsync( );
        };

        m_MainWindowVM.GamePauseButtonClickedEvent += (_, _) =>
        {
            if(m_GameControl.IsGameRunning)
            {
                if(m_MainWindowVM.IsPauseBtnClicked)
                    m_GameControl.PauseGame( );
                else
                    m_GameControl.ResumeGame( );
            }
        };

        m_MainWindowVM.SettingButtonClickedEvent += async (_, _) =>
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

        m_MainWindowVM.GameSaveButtonClickedEvent += async (_, _) =>
        {
            m_SelectSaveFileWindowVM.Title = "保存游戏";
            m_SelectSaveFileWindowVM.IsSave = true;
            if(m_GameControl.IsGameRunning && m_GameControl.NesFileInfo is not null)
                m_SelectSaveFileWindowVM.GameName = m_GameControl.NesFileInfo.Name;
            else
                m_SelectSaveFileWindowVM.GameName = "无运行游戏";
            await m_SelectSaveFileWindow.ShowAsync( );
        };

        m_MainWindowVM.GameLoadButtonClickedEvent += async (_, _) =>
        {
            m_SelectSaveFileWindowVM.Title = "加载游戏";
            m_SelectSaveFileWindowVM.IsSave = false;
            if(m_GameControl.IsGameRunning && m_GameControl.NesFileInfo is not null)
                m_SelectSaveFileWindowVM.GameName = m_GameControl.NesFileInfo.Name;
            else
                m_SelectSaveFileWindowVM.GameName = "无运行游戏";
            await m_SelectSaveFileWindow.ShowAsync( );
        };

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

    private void SelectSaveFileWindow_OpenEventHandle(ContentDialog sender, ContentDialogOpenedEventArgs args)
    {
        // 如果游戏没有运行, 则不读取
        if(!m_GameControl.IsGameRunning) return;
        if(m_GameControl.NesFileInfo is null) return;

        // 当前游戏的存档文件夹
        var gamePath = GameControl.DefaultSaveFilePath + @"\" + m_GameControl.NesFileInfo.Name;
        var fileInfoName = m_GameControl.NesFileInfo.Name + ".info";    // 存档信息文件名

        for(var index = 0; index < 6; index++)
        {
            // 当前序号的存档文件夹
            var saveFilePath = gamePath + @"\" + $"{m_GameControl.NesFileInfo.Name}_{index}";
            // 如果当前序号的存档文件夹不存在, 则跳过
            if(!Directory.Exists(saveFilePath)) continue;
            if(!File.Exists(saveFilePath + @"\" + fileInfoName)) continue;  // 如果存档信息文件不存在, 则跳过

            /// 存档信息
            SaveFileInfo info = new( );
            using BinaryReader infoReader = new(File.Open(saveFilePath + @"\" + fileInfoName, FileMode.Open));
            info.Load(infoReader);  // 读取存档信息
            if(!File.Exists(info.Path)) continue;  // 如果存档文件不存在, 则跳过
            // 如果封面图片存在, 则加载封面图片
            if(File.Exists(info.FrontCoverPath))
            {
                BitmapImage bitmapImage = new( );   // 创建一个BitmapImage对象
                bitmapImage.BeginInit( );
                bitmapImage.CacheOption = BitmapCacheOption.OnLoad; // 确保图像数据在加载后缓存到内存中
                bitmapImage.UriSource = new Uri(AppDomain.CurrentDomain.BaseDirectory + info.FrontCoverPath, UriKind.Absolute);
                bitmapImage.CreateOptions = BitmapCreateOptions.IgnoreImageCache; // 忽略图片缓存
                bitmapImage.EndInit( );
                info.FrontCover = bitmapImage;
            }
            m_SelectSaveFileWindowVM.SaveInfos[index] = info;
        }
    }

    private void GameSave(object? sender, int index)
    {
        // 如果游戏没有运行, 则不保存
        if(!m_GameControl.IsGameRunning) goto end;
        if(m_GameControl.NesFileInfo is null) goto end;
        var time = DateTime.Now;

        // 当前游戏的存档文件夹
        var gamePath = GameControl.DefaultSaveFilePath + @"\" + m_GameControl.NesFileInfo.Name;
        // 当前序号的存档文件夹
        var saveFilePath = gamePath + @"\" + $"{m_GameControl.NesFileInfo.Name}_{index}";
        Directory.CreateDirectory(saveFilePath);    // 创建当前序号的存档文件夹

        var fileInfoName = m_GameControl.NesFileInfo.Name + ".info";    // 存档信息文件名
        var fileName = m_GameControl.NesFileInfo.Name + ".save";    // 存档文件名
        var frontCoverName = m_GameControl.NesFileInfo.Name + ".png";   // 封面图片名

        /// 保存封面图片
        using(FileStream fileStream = new(saveFilePath + @"\" + frontCoverName, FileMode.Create))
        {
            PngBitmapEncoder encoder = new( );
            encoder.Frames.Add(BitmapFrame.Create(m_MainWindowVM.BitImage));    // 将WriteableBitmap转换为BitmapFrame
            encoder.Save(fileStream);   // 将编码后的数据写入文件
        }

        /// 保存存档
        using(BinaryWriter fileWriter = new(File.Open(saveFilePath + @"\" + fileName, FileMode.Create)))
        {
            m_GameControl.Save(fileWriter);
        }

        /// 保存存档信息
        SaveFileInfo info = new( )
        {
            GameName = m_GameControl.NesFileInfo.Name,
            NesFilePath = m_GameControl.NesFileInfo.Path,
            Path = saveFilePath + @"\" + fileName,
            FrontCoverPath = saveFilePath + @"\" + frontCoverName,
            Date = time,
        };
        BitmapImage bitmapImage = new( );   // 创建一个BitmapImage对象
        bitmapImage.BeginInit( );
        bitmapImage.CacheOption = BitmapCacheOption.OnLoad; // 确保图像数据在加载后缓存到内存中
        bitmapImage.UriSource = new Uri(AppDomain.CurrentDomain.BaseDirectory + info.FrontCoverPath, UriKind.Absolute);
        bitmapImage.CreateOptions = BitmapCreateOptions.IgnoreImageCache; // 忽略图片缓存
        bitmapImage.EndInit( );
        info.FrontCover = bitmapImage;

        using(var infoWriter = new BinaryWriter(File.Open(saveFilePath + @"\" + fileInfoName, FileMode.Create)))
        {
            info.Save(infoWriter);
        }
        m_SelectSaveFileWindowVM.SaveInfos[index] = info;
    end:
        m_SelectSaveFileWindow.Hide( );
    }

    private void GameLoad(object? sender, int index)
    {
        var info = m_SelectSaveFileWindowVM.SaveInfos[index];

        using BinaryReader reader = new(File.Open(info.Path, FileMode.Open));
        m_GameControl.Load(reader);

        m_SelectSaveFileWindow.Hide( ); // 隐藏选择存档窗口
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


