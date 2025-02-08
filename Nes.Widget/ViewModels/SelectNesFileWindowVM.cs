using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Nes.Widget.Models;
using NesEmu.Control;
using System.Collections.ObjectModel;
using System.IO;

namespace Nes.Widget.ViewModels;

internal partial class SelectNesFileWindowVM : ObservableObject
{
    public ObservableCollection<NesFileInfo> Infos { get; set; } = [];
    public EventHandler<NesFileInfo>? GameStartEvent;

    [ObservableProperty]
    private string m_SearchStr = "";

    [RelayCommand]
    public void DataGridDoubleClick(object parameter)
    {
        if(parameter is NesFileInfo info)
        {
            GameStartEvent?.Invoke(this, info);
        }
    }

    public async void SelectnesFile(string path)
    {
        Infos.Clear( ); // 清除原有数据
        var nesFiles = Directory.GetFiles(path, "*.nes");
        int i = 0;
        foreach(var nesFile in nesFiles)
        {
            var (res, mapperNum, isSupported) = await GameControl.GetNesFileInfoAsync(nesFile);
            if(res)
            {
                var nesFileInfo = new NesFileInfo
                {
                    Index = i++,
                    Name = Path.GetFileNameWithoutExtension(nesFile),
                    Path = nesFile,
                    MapperNumber = mapperNum,
                    IsSupported = isSupported,
                };
                Infos.Add(nesFileInfo);
            }
        }
    }
}


