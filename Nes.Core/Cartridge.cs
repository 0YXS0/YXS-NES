using Nes.Core.Rendering;
using System;
using System.IO;

namespace Nes.Core;

/// <summary>
///     Represents a Cartridge in the Nintendo Entertainment System (NES).
/// </summary>
public class Cartridge
{
    #region Private Fields

    // ReSharper disable once InconsistentNaming
    private const int CHR_ROM_UNIT_SIZE = 0x2000;

    // ReSharper disable once InconsistentNaming
    private const int NES_HEADER_VALUE = 0x1A53454E;

    // ReSharper disable once InconsistentNaming
    private const int PRG_ROM_UNIT_SIZE = 0x4000;

    #endregion Private Fields

    #region Public Constructors

    public Cartridge(string fileName)
        : this(File.OpenRead(fileName))
    {
    }

    public Cartridge(Stream stream)
    {
        using var binaryReader = new BinaryReader(stream);
        var raw = binaryReader.ReadBytes((int)stream.Length);
        if(BitConverter.ToInt32(raw, 0) != NES_HEADER_VALUE)
            throw new FormatException("Nes文件格式不正确。");

        if(((raw[7] >> 2) & 0b0000_0011) != 0) throw new NotSupportedException("不支持Nes文件版本。");

        if((raw[7] & 1) != 0 || (raw[7] & 2) != 0)
            throw new FormatException("该文件不是有效的Nes 1.0文件格式。");

        if(Bit.HasSet(raw[6], 3))
        {
            //_mirroring = Mirroring.FourScreen;
        }
        else
        {
            Mirroring = Bit.HasSet(raw[6], 0) ?
                VramMirroring.Vertical : VramMirroring.Horizontal;
        }

        IsBatteryBacked = Bit.HasSet(raw[6], 1);
        Mapper = (raw[7] & 0b1111_0000) | (raw[6] >> 4);
        TvSystem = (TvSystem)(raw[9] & 1);

        PrgRomBanks = raw[4];
        ChrRomBanks = raw[5];
        UseChrRam = raw[5] == 0;

        PrgRomSize = PrgRomBanks * PRG_ROM_UNIT_SIZE;
        PrgRom = new byte[PrgRomSize];
        ChrRomSize = ChrRomBanks * CHR_ROM_UNIT_SIZE;
        HasTrainer = Bit.HasSet(raw[6], 2);
        var prgRomStartIdx = 16 + (HasTrainer ? 512 : 0);
        var chrRomStartIdx = prgRomStartIdx + PrgRomSize;

        // Load PRG ROM
        Array.Copy(raw, prgRomStartIdx, PrgRom, 0, PrgRomSize);

        // Load CHR ROM
        if(UseChrRam)
        {
            // at this point, number of CHR ROM Banks is zero.
            ChrData = new byte[0x2000]; // 8kb
        }
        else
        {
            ChrData = new byte[ChrRomSize];
            Array.Copy(raw, chrRomStartIdx, ChrData, 0, ChrRomSize);
        }
    }

    public Cartridge(byte[] raw)
        : this(new MemoryStream(raw))
    {
    }

    #endregion Public Constructors

    #region Public Properties

    public byte[] ChrData { get; }

    public int ChrRomBanks { get; }

    public int ChrRomSize { get; }

    public bool HasTrainer { get; }

    public bool IsBatteryBacked { get; }

    public int Mapper { get; }

    public VramMirroring Mirroring { get; private set; }

    public byte[] PrgRom { get; }

    public int PrgRomBanks { get; }

    public int PrgRomSize { get; }

    public TvSystem TvSystem { get; }

    public bool UseChrRam { get; }

    #endregion Public Properties

    #region Public Methods

    public Tile? GetTile(int bank, int tileNumber)
    {
        if(ChrRomSize == 0) return null;

        // each bank has 4096 bytes and within the bank, each tile has 16 bytes.
        var startIndex = bank * 0x1000 + tileNumber * 0x10;
        var tileBlock = new byte[16];
        Array.Copy(ChrData, startIndex, tileBlock, 0, 16);
        return new Tile(tileBlock);
    }

    /// <summary>
    /// 存档
    /// </summary>
    public void Save(BinaryWriter writer)
    {
        writer.Write(ChrData);
    }

    /// <summary>
    /// 读档
    /// </summary>
    public void Load(BinaryReader reader)
    {
        reader.Read(ChrData);
    }

    #endregion Public Methods
}