﻿using iNKORE.UI.WPF.Modern.Controls;
using Nes.Console.Models;
using Nes.Widget.ViewModels;
using NesEmu.Console;
using NesEmu.Core;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media.Imaging;

namespace Nes.Widget;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    private readonly GameControl m_GameControl = new( );
    private readonly MainWindowVM m_MainWindowVM = MainWindowVM.Instance;
    private readonly SettingWindowVM m_SettingWindowVM = SettingWindowVM.Instance;
    private readonly SettingWindow m_SettingWindow = new( );
    private static readonly JsonSerializerOptions JsonSerializerOptions = new( )
    {
        WriteIndented = true,   // 缩进
        Converters = { new JsonStringEnumConverter( ) } // 使用数字表示枚举
    };

    public MainWindow( )
    {
        InitializeComponent( );
        DataContext = m_MainWindowVM;
        this.KeyDown += KeyDownHandle;
        this.KeyUp += KeyUphandle;
        this.MouseDown += MouseDownHandle;
        this.MouseUp += MouseUpHandle;
        m_GameControl.GameDrawFrame += DrawFrame; // 画帧事件

        m_MainWindowVM.GameStartEvent += (object? sender, string fileName) =>
        {
            if(m_GameControl.IsGameRunning)
                m_GameControl.StopGame( );
            m_GameControl.OpenGame(fileName);
        };

        m_MainWindowVM.GamePauseEvent += (object? sender, EventArgs e) =>
        {
            if(m_GameControl.IsGameRunning)
            {
                if(m_MainWindowVM.IsPauseBtnClicked)
                    m_GameControl.PauseGame( );
                else
                    m_GameControl.ResumeGame( );
            }
        };

        m_MainWindowVM.GameSettingEvent += async (object? sender, EventArgs e) =>
        {
            m_SettingWindow.DataContext = m_SettingWindowVM;
            var res = await m_SettingWindow.ShowAsync( );
            if(res == ContentDialogResult.Primary)
            {
                // 序列化为 JSON
                string str = JsonSerializer.Serialize(m_SettingWindowVM, JsonSerializerOptions);
                File.WriteAllText("setting.json", str);
            }
        };

        // 反序列化
        if(File.Exists("setting.json"))
        {
            string str = File.ReadAllText("setting.json");
            m_SettingWindowVM = JsonSerializer.Deserialize<SettingWindowVM>(str, JsonSerializerOptions)
                ?? SettingWindowVM.Instance;
        }
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

    private void DrawFrame(object? sender, EventArgs e)
    {
        Dispatcher.Invoke(( ) =>
        {
            WriteableBitmap bitmap = MainWindowVM.Instance.BitImage;
            bitmap.WritePixels(new Int32Rect(0, 0, 256, 240), m_GameControl.Pixels, 256 * 4, 0);
        });
    }
}