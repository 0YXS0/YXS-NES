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

    public ObservableCollection<SaveFileInfo> SaveInfos { get; } = [];

    public int SelectedSaveInfoIndex
    {
        get;
        set
        {
            field = value;
            SelectedSaveFileEvent?.Invoke(this, value);
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
