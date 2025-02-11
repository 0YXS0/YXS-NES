using CommunityToolkit.Mvvm.ComponentModel;
using Nes.Widget.Models;
using System.Collections.ObjectModel;

namespace Nes.Widget.ViewModels;

internal partial class SelectSaveFileWindowVM : ObservableObject
{
    /// <summary>
    /// 已选择存档事件
    /// </summary>
    public event EventHandler<int>? SelectedSaveFileEvent;
    public event EventHandler<int>? SelectedLoadFileEvent;

    public ObservableCollection<SaveFileInfo> SaveInfos { get; } = [];

    /// <summary>
    /// 当前操作是存档还是读档(用来触发不同的事件)--true:存档 false:读档
    /// </summary>
    public bool IsSave { get; set; } = true;

    public int SelectedSaveInfoIndex
    {
        get;
        set
        {
            field = value;
            if(IsSave)
                SelectedSaveFileEvent?.Invoke(this, value);
            else
                SelectedLoadFileEvent?.Invoke(this, value);
        }
    }

    public SelectSaveFileWindowVM( )
    {
        foreach(var _ in Enumerable.Range(0, 6))
        {
            SaveInfos.Add(new( ));
        }
    }

    [ObservableProperty]
    private string m_Title = "存档选择";

    [ObservableProperty]
    private string m_GameName = "无运行游戏";
}
