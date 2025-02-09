using CommunityToolkit.Mvvm.ComponentModel;
using System.IO;
using System.Windows.Media.Imaging;

namespace Nes.Widget.Models;

/// <summary>
/// 存档文件信息
/// </summary>
internal class SaveFileInfo : ObservableObject
{
    public string GameName { get; set; } = string.Empty;
    public string NesFilePath { get; set; } = string.Empty;
    public string Path { get; set; } = string.Empty;
    public dynamic FrontCoverPath { get; set; } = string.Empty;
    public BitmapImage? FrontCover { get; set; }
    public DateTime Date { get; set; } = default;
    public string DateStr
    {
        get
        {
            if(Date == default) return string.Empty;
            return Date.ToString("MM-dd HH:mm");
        }
    }

    public void Save(BinaryWriter writer)
    {
        writer.Write(GameName);
        writer.Write(NesFilePath);
        writer.Write(Path);
        writer.Write(FrontCoverPath);
        writer.Write(Date.ToBinary( ));
    }

    public void Load(BinaryReader reader)
    {
        GameName = reader.ReadString( );
        NesFilePath = reader.ReadString( );
        Path = reader.ReadString( );
        FrontCoverPath = reader.ReadString( );
        Date = DateTime.FromBinary(reader.ReadInt64( ));
    }
}
