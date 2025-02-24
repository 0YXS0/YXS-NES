namespace Nes.Core.Mappers;

[Mapper(0, "NROM")]
internal sealed class NRomMapper(Emulator emulator) : Mapper(emulator)
{
    private readonly byte[] m_ChrRom = emulator.InstalledCartridge!.ChrData;
    private readonly byte[] m_PrgRom = emulator.InstalledCartridge!.PrgRom;
    private readonly int m_PrgRomBankNum = emulator.InstalledCartridge!.PrgRomBanks;
    private readonly byte[] m_PrgRam = new byte[0x2000];

    private static ushort MapAddress(int bankNum, ushort address)
    {
        var mappedAddress = (ushort)(address - 0x8000);
        mappedAddress = bankNum == 1
            ? (ushort)(mappedAddress % 0x4000) // Mirrors 0x8000 - 0xBFFF for NROM-128 (PRG ROM BANKS = 1)
            : mappedAddress;

        return mappedAddress;
    }

    public override byte ReadByte(ushort address)
    {
        return address switch
        {
            < 0x2000 => m_ChrRom[address],
            < 0x6000 => 0,
            < 0x8000 => m_PrgRam[address - 0x6000],
            <= 0xFFFF => m_PrgRom[MapAddress(m_PrgRomBankNum, address)],
        };
    }

    public override void WriteByte(ushort address, byte value)
    {
        if(address < 0x2000)
        {
            m_ChrRom[address] = value;
        }
        else if(address < 0x6000)
        { }
        else if(address < 0x8000)
        {
            m_PrgRam[address - 0x6000] = value;
        }
    }
}