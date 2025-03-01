using System;

namespace Nes.Core.Mappers;

[Mapper(0x02, "UxROM")]
internal sealed class UxRomMapper : Mapper
{
    private readonly byte[] m_ChrRom;
    private readonly byte[] m_PrgRom;
    private readonly int m_PrgRomSize;
    private readonly int m_PrgRomBankNum;
    private readonly byte[] m_PrgRam = new byte[0x2000];

    private readonly int m_bankOffset1;
    private int m_bankOffset0;

    public UxRomMapper(Emulator emulator) : base(emulator)
    {
        if(emulator.InstalledCartridge == null)
            throw new InvalidOperationException("UxRom未安装卡带。");
        m_ChrRom = emulator.InstalledCartridge.ChrData;
        m_PrgRom = emulator.InstalledCartridge.PrgRom;
        m_PrgRomSize = emulator.InstalledCartridge.PrgRomSize;
        m_PrgRomBankNum = emulator.InstalledCartridge.PrgRomBanks;

        m_bankOffset0 = 0;
        if(m_PrgRomBankNum > 1)
            m_bankOffset1 = (m_PrgRomBankNum - 1) * 0x4000;
        else
            m_bankOffset1 = 0;
    }

    public override byte ReadByte(ushort address)
    {
        return address switch
        {
            < 0x2000 => m_ChrRom[address],
            < 0x6000 => throw new InvalidOperationException("UxRom意料之外的读取操作。"),
            < 0x8000 => m_PrgRam[address - 0x6000],
            <= 0xBFFF => m_PrgRom[m_bankOffset0 + (address - 0x8000)],
            <= 0xFFFF => m_PrgRom[m_bankOffset1 + (address - 0xC000)],
        };
    }

    public override void WriteByte(ushort address, byte value)
    {
        switch(address)
        {
            case < 0x2000:
                m_ChrRom[address] = value;
                break;

            case < 0x6000:
                break;

            case < 0x8000:
                m_PrgRam[address - 0x6000] = value;
                break;

            case <= 0xFFFF:
                m_bankOffset0 = (value & 0x0F) * 0x4000;
                if(m_bankOffset0 > m_PrgRomSize)
                    m_bankOffset0 %= m_PrgRomSize;
                break;
        }
    }

    public override void Save(System.IO.BinaryWriter writer)
    {
        writer.Write(m_PrgRam);
        writer.Write(m_bankOffset0);
    }

    public override void Load(System.IO.BinaryReader reader)
    {
        reader.Read(m_PrgRam);
        m_bankOffset0 = reader.ReadInt32( );
    }
}