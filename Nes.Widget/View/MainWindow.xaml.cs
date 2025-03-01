using iNKORE.UI.WPF.Modern.Controls;
using NAudio.Wave;
using Nes.Core;
using Nes.Core.Control;
using Nes.Widget.Models;
using Nes.Widget.ViewModels;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using KeyEventArgs = System.Windows.Input.KeyEventArgs;
using MessageBox = iNKORE.UI.WPF.Modern.Controls.MessageBox;

namespace Nes.Widget.View;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    public const string DefaultNesFilePath = "NesFile";
    public const string DefaultSaveFilePath = "SaveFile";

    private GameControl m_GameControl = new GameControlLocal( );
    private readonly MainWindowVM m_MainWindowVM = MainWindowVM.Instance;
    private readonly SettingWindow m_SettingWindow = new( );
    private readonly SettingWindowVM m_SettingWindowVM;
    private readonly SelectNesFileWindow m_SelectNesFileWindow = new( );
    private readonly SelectNesFileWindowVM m_SelectNesFileWindowVM;
    private readonly SelectSaveFileWindow m_SelectSaveFileWindow = new( );
    private readonly SelectSaveFileWindowVM m_SelectSaveFileWindowVM;
    private readonly OnlineWindow m_OnlineWindow = new( );
    private readonly OnlineWindowVM m_OnlineWindowVM;
    private readonly WaveOut m_waveOut = new( ) { DesiredLatency = 100 };    // 音频输出
    private readonly WriteLine m_apuAudioProvider = new( );  // 音频提供器
    private static readonly JsonSerializerOptions JsonSerializerOptions = new( )
    {
        WriteIndented = true,   // 缩进
        Converters = { new JsonStringEnumConverter( ) } // 使用数字表示枚举
    };

    public MainWindow( )
    {
        InitializeComponent( );
        DataContext = m_MainWindowVM;
        m_waveOut.Init(m_apuAudioProvider);
        m_SettingWindowVM = (SettingWindowVM)m_SettingWindow.DataContext;
        m_SelectNesFileWindowVM = (SelectNesFileWindowVM)m_SelectNesFileWindow.DataContext;
        m_SelectSaveFileWindowVM = (SelectSaveFileWindowVM)m_SelectSaveFileWindow.DataContext;
        m_OnlineWindowVM = (OnlineWindowVM)m_OnlineWindow.DataContext;

        this.KeyDown += KeyDownHandle;
        this.KeyUp += KeyUphandle;
        this.MouseDown += MouseDownHandle;
        this.MouseUp += MouseUpHandle;
        m_SelectSaveFileWindowVM.SelectedSaveFileEvent += GameSaveHandle; // 存档事件
        m_SelectSaveFileWindowVM.SelectedLoadFileEvent += GameLoadHandle; // 读档事件
        // 进行存档信息的读取以及信息显示的初始化
        m_SelectSaveFileWindow.Opened += SelectSaveFileWindow_OpenEventHandle;

        Directory.CreateDirectory(DefaultNesFilePath);  // 创建默认的Nes游戏文件夹
        Directory.CreateDirectory(DefaultSaveFilePath); // 创建默认的存档文件夹

#if DEBUG   // 调试时让窗口始终在最上层, 方便调试
        //this.Topmost = true;
#endif
        InitGameControl( ); // 初始化游戏控制器

        m_SelectNesFileWindowVM.SelectedNesFileEvent += async (object? _, NesFileInfo info) =>
        {
            if(!info.IsSupported)
            {
                MessageBox.Show("不支持的文件格式");
                return;
            }

            if(m_GameControl.GameStatus != 0)
                await m_GameControl.StopGame( );  // 会阻塞调用线程, 所以要放在新线程中,防止阻塞主线程
            m_GameControl.OpenGame(info.Path);

            m_SelectNesFileWindow.Hide( );  // 隐藏选择文件窗口
        };

        m_MainWindowVM.GameOpenButtonClickedEvent += async (_, _) =>
        {
            if(m_GameControl is not GameControlSlave)
                await m_SelectNesFileWindowVM.SelectNesFile(DefaultNesFilePath);
            else
            {
                var gameControl = (GameControlSlave)m_GameControl;
                int index = 0;
                m_SelectNesFileWindowVM.Infos = gameControl.NesfileNames.Select(info => new NesFileInfo
                {
                    Index = index++,
                    Name = info.Name,
                    Path = info.Name,
                    MapperNumber = info.MapperNum,
                    IsSupported = true,
                }).ToArray( );
            }
            await m_SelectNesFileWindow.ShowAsync( );
        };

        m_MainWindowVM.GamePauseButtonClickedEvent += async (_, _) =>
        {
            if(m_GameControl.GameStatus != 0)
            {
                if(m_MainWindowVM.IsPauseBtnClicked)
                    await m_GameControl.PauseGame( );
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
            if(m_GameControl.GameStatus != 0)
                m_SelectSaveFileWindowVM.GameName = m_GameControl.GameName!;
            else
                m_SelectSaveFileWindowVM.GameName = "无运行游戏";
            await m_SelectSaveFileWindow.ShowAsync( );
        };

        m_MainWindowVM.GameLoadButtonClickedEvent += async (_, _) =>
        {
            m_SelectSaveFileWindowVM.Title = "加载游戏";
            m_SelectSaveFileWindowVM.IsSave = false;
            if(m_GameControl.GameStatus != 0)
                m_SelectSaveFileWindowVM.GameName = m_GameControl.GameName!;
            else
                m_SelectSaveFileWindowVM.GameName = "无运行游戏";
            await m_SelectSaveFileWindow.ShowAsync( );
        };

        m_OnlineWindowVM.ConnectButtonClickedEvent += (_, type) =>
        {
            if(type == GameControlType.LANHost && m_GameControl is not GameControlLANHost)
            {
                m_GameControl?.Dispose( );
                GameControlLANHost gameControl = new(DefaultNesFilePath);
                m_GameControl = gameControl;
                InitGameControl( );
                m_MainWindowVM.IsSaveBtnEnabled = true;    // 启用存档按钮
                m_MainWindowVM.IsLoadBtnEnabled = true;    // 启用读档按钮
                ClearFrame( );  // 清空画面
                gameControl.ConnectedEvent += async (_, value) =>
                {
                    await Dispatcher.BeginInvoke(( ) =>
                    {
                        m_OnlineWindowVM.ConnectionState = value;
                    });
                };
            }
            else if(type == GameControlType.Salve && m_GameControl is not GameControlSlave)
            {
                m_GameControl?.Dispose( );
                GameControlSlave gameControl = new( );
                m_GameControl = gameControl;
                InitGameControl( );
                m_MainWindowVM.IsSaveBtnEnabled = false;    // 禁用存档按钮
                m_MainWindowVM.IsLoadBtnEnabled = false;    // 禁用读档按钮
                ClearFrame( );  // 清空画面
                gameControl.ConnectedEvent += async (_, value) =>
                {
                    await Dispatcher.BeginInvoke(( ) =>
                    {
                        m_OnlineWindowVM.ConnectionState = value;
                    });
                };
            }
            if(type == GameControlType.Salve)
            {
                var gameControl = (GameControlSlave)m_GameControl;
                gameControl.AgreementCode = m_OnlineWindowVM.AgreementCode;
            }
            m_GameControl.Connect(m_OnlineWindowVM.ServerAddr, int.Parse(m_OnlineWindowVM.ServerPort));
        };

        m_OnlineWindowVM.DisConnectButtonClickedEvent += (_, _) =>
        {
            m_GameControl.DisConnect( );
        };

        m_MainWindowVM.GameOnlineButtonClickedEvent += async (_, _) =>
        {
            if(m_GameControl.Type is GameControlType.Local)
            {
                m_OnlineWindowVM.IsPortEnabled = false;
                m_OnlineWindowVM.IsAgreementCodeEnabled = false;
                m_OnlineWindowVM.IsAddressEnabled = false;
                m_MainWindowVM.IsOnlineBtnClicked = false;
            }
            else
                m_MainWindowVM.IsOnlineBtnClicked = true;
            /// 获取本地IPv4地址
            using Socket socket = new(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.IP);
            socket.Connect("8.8.8.8", 65530);
            IPEndPoint? endPoint = socket.LocalEndPoint as IPEndPoint;
            m_OnlineWindowVM.ServerAddr = endPoint?.Address.ToString( ) ?? "127.0.0.1";
            var res = await m_OnlineWindow.ShowAsync( );
            switch(m_GameControl.Type)
            {
                case GameControlType.LANHost:
                    m_OnlineWindowVM.SelectedIndex = 0;
                    m_MainWindowVM.IsOnlineBtnClicked = true;
                    break;
                case GameControlType.INTEHost:
                    m_OnlineWindowVM.SelectedIndex = 1;
                    m_MainWindowVM.IsOnlineBtnClicked = true;
                    break;
                case GameControlType.Salve:
                    m_OnlineWindowVM.SelectedIndex = 2;
                    m_MainWindowVM.IsOnlineBtnClicked = true;
                    break;
                case GameControlType.Local:
                    m_OnlineWindowVM.SelectedIndex = -1;
                    m_MainWindowVM.IsOnlineBtnClicked = false;
                    break;
            }
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

    /// <summary>
    /// 初始化游戏控制器
    /// </summary>
    private void InitGameControl( )
    {
        m_MainWindowVM.Title = MainWindowVM.OriginTitle;
        m_GameControl.GameDrawFrame += async (_, pixels) =>
        {
            await Dispatcher.BeginInvoke(( ) =>
            {
                WriteableBitmap bitmap = m_MainWindowVM.BitImage;
                bitmap.WritePixels(new Int32Rect(0, 0, 256, 240), pixels, 256 * 4, 0);
            });
        };  // 绘制一帧画面事件

        m_GameControl.GameAudioOut += (_, sampleValue) =>
        {
            m_apuAudioProvider.Queue(sampleValue);
        };
        m_waveOut.Play( );  // 开始播放音频

        m_GameControl.ErrorEventOccurred += async (_, ErrorMsg) =>
        {
            await Dispatcher.BeginInvoke(( ) =>
            {
                MessageBox.Show(ErrorMsg, "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            });
        };

        m_GameControl.GameOpened += async (_, _) =>
        {
            string title = MainWindowVM.OriginTitle;
            if(m_GameControl is GameControlLocal) title += " · 本地";
            else if(m_GameControl is GameControlLANHost) title += " · 局域网主机";
            //else if(m_GameControl is GameControlINTEHost) title += " · 互联网主机";
            else if(m_GameControl is GameControlSlave) title += " · 从机";
            title += " · " + m_GameControl.GameName;
            await Dispatcher.BeginInvoke(( ) =>
            {
                m_MainWindowVM.Title = title;
            });
        }; // 游戏打开事件
        m_GameControl.GamePaused += (_, _) => { m_MainWindowVM.IsPauseBtnClicked = true; }; // 游戏暂停事件
        m_GameControl.GameResumed += (_, _) => { m_MainWindowVM.IsPauseBtnClicked = false; };    // 游戏恢复事件
        m_GameControl.GameStopped += async (_, _) =>
        {
            await Dispatcher.BeginInvoke(( ) =>
            {
                m_MainWindowVM.Title = MainWindowVM.OriginTitle;
                ClearFrame( );
            });
        }; // 游戏停止事件
    }

    /// <summary>
    /// 清空画面
    /// </summary>
    private void ClearFrame( )
    {
        Dispatcher.BeginInvoke(( ) =>
        {
            byte[] pixels = new byte[256 * 240 * 4];
            Color color = Color.FromArgb(0, 0, 0, 0);   // 透明
            Parallel.For(0, 256 * 240, i =>
            {
                pixels[i * 4 + 0] = color.B;
                pixels[i * 4 + 1] = color.G;
                pixels[i * 4 + 2] = color.R;
                pixels[i * 4 + 3] = color.A;
            });
            WriteableBitmap bitmap = m_MainWindowVM.BitImage;
            bitmap.WritePixels(new Int32Rect(0, 0, 256, 240), pixels, 256 * 4, 0);
        });
    }

    private void SelectSaveFileWindow_OpenEventHandle(ContentDialog sender, ContentDialogOpenedEventArgs args)
    {
        // 清空存档信息
        for(var index = 0; index < 6; index++)
        {
            m_SelectSaveFileWindowVM.SaveInfos[index] = new( );
        }

        // 如果游戏没有运行, 则不读取
        if(m_GameControl.GameStatus == 0) return;
        if(m_GameControl.GameName is null) return;

        // 当前游戏的存档文件夹
        var gamePath = DefaultSaveFilePath + @"\" + m_GameControl.GameName;
        var fileInfoName = m_GameControl.GameName + ".info";    // 存档信息文件名

        for(var index = 0; index < 6; index++)
        {
            // 当前序号的存档文件夹
            var saveFilePath = gamePath + @"\" + $"{m_GameControl.GameName}_{index}";
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

    private void GameSaveHandle(object? sender, int index)
    {
        // 如果游戏没有运行, 则不保存
        if(m_GameControl.GameStatus == 0) return;
        if(m_GameControl.GameName is null) return;
        var time = DateTime.Now;

        // 当前游戏的存档文件夹
        var gamePath = DefaultSaveFilePath + @"\" + m_GameControl.GameName;
        // 当前序号的存档文件夹
        var saveFilePath = gamePath + @"\" + $"{m_GameControl.GameName}_{index}";
        Directory.CreateDirectory(saveFilePath);    // 创建当前序号的存档文件夹

        var fileInfoName = m_GameControl.GameName + ".info";    // 存档信息文件名
        var fileName = m_GameControl.GameName + ".save";    // 存档文件名
        var frontCoverName = m_GameControl.GameName + ".png";   // 封面图片名

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
            GameName = m_GameControl.GameName,
            NesFilePath = m_GameControl.GameFilePath!,
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
        // 保存成功后隐藏窗口
        m_SelectSaveFileWindow.Hide( );
    }

    private void GameLoadHandle(object? sender, int index)
    {
        // 如果游戏没有运行, 则不读取
        if(m_GameControl.GameStatus == 0) return;
        if(m_GameControl.GameName is null) return;

        var info = m_SelectSaveFileWindowVM.SaveInfos[index];

        if(!File.Exists(info.Path)) return;  // 如果存档文件不存在, 则不读取

        using BinaryReader reader = new(File.Open(info.Path, FileMode.Open));
        m_GameControl.Load(reader);

        m_SelectSaveFileWindow.Hide( ); // 隐藏选择存档窗口
    }

    private void MouseUpHandle(object sender, MouseButtonEventArgs e)
    {
        var key = ControlKey.ToKeyType(e.ChangedButton);
        if(m_GameControl.IsP1Enabled)
            ProcessKey(1, key, false);
        if(m_GameControl.IsP2Enabled)
            ProcessKey(2, key, false);
    }

    private void MouseDownHandle(object sender, MouseButtonEventArgs e)
    {
        var key = ControlKey.ToKeyType(e.ChangedButton);
        if(m_GameControl.IsP1Enabled)
            ProcessKey(1, key, true);
        if(m_GameControl.IsP2Enabled)
            ProcessKey(2, key, true);
    }

    private void KeyUphandle(object sender, KeyEventArgs e)
    {
        var key = ControlKey.ToKeyType(e.Key);
        if(m_GameControl.IsP1Enabled)
            ProcessKey(1, key, false);
        if(m_GameControl.IsP2Enabled)
            ProcessKey(2, key, false);
    }

    private void KeyDownHandle(object sender, KeyEventArgs e)
    {
        var key = ControlKey.ToKeyType(e.Key);
        if(m_GameControl.IsP1Enabled)
            ProcessKey(1, key, true);
        if(m_GameControl.IsP2Enabled)
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
}


