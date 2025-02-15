using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Nes.Core.Control;
using Nes.Widget.Models;
using System.IO;

namespace Nes.Widget.ViewModels;

internal partial class SelectNesFileWindowVM : ObservableObject
{
    public event EventHandler<NesFileInfo>? SelectedNesFileEvent;

    [ObservableProperty]
    private NesFileInfo[] m_Infos = [];

    [ObservableProperty]
    private string m_SearchStr = "";

    [RelayCommand]
    public void DataGridDoubleClick(object parameter)
    {
        if(parameter is NesFileInfo info)
        {
            SelectedNesFileEvent?.Invoke(this, info);
        }
    }

    /// <summary>
    /// 对文件夹内的nes文件进行分析
    /// </summary>
    /// <param name="path">文件夹路径</param>
    public async Task SelectnesFile(string path)
    {
        var nesFiles = Directory.GetFiles(path, "*.nes");
        int i = 0;
        var tasks = nesFiles.Select(async (nesFilePath) =>
        {
            var (res, mapperNum, isSupported) = await GameControl.GetNesFileInfoAsync(nesFilePath);
            return new NesFileInfo
            {
                Index = i++,
                Name = Path.GetFileNameWithoutExtension(nesFilePath),
                Path = nesFilePath,
                MapperNumber = mapperNum,
                IsSupported = isSupported,
            };
        });
        Infos = [.. (await Task.WhenAll(tasks)).OrderByDescending(info => info.IsSupported)];
    }
}


