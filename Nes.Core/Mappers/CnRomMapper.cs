namespace Nes.Core.Mappers;

[Mapper(0x03, "CnROM")]
internal sealed class CnRomMapper(Emulator emulator) : Mapper(emulator)
{
    private int m_ChrBank = 0;

    private static ushort MapAddress(Cartridge cartridge, ushort address)
    {
        var mappedAddress = (ushort)(address - 0x8000);
        mappedAddress = cartridge.PrgRomBanks == 1
            ? (ushort)(mappedAddress % 0x4000) // Mirrors 0x8000 - 0xBFFF for NROM-128 (PRG ROM BANKS = 1)
            : mappedAddress;

        return mappedAddress;
    }

    public override byte ReadByte(ushort address)
    {
        return address switch
        {
            < 0x2000 => _emulator.InstalledCartridge?.ChrData[m_ChrBank + address] ?? default,
            >= 0x8000 => _emulator.InstalledCartridge?.PrgRom[MapAddress(_emulator.InstalledCartridge, address)]
                         ??
                         default,
            _ => 0,
        };
    }

    public override void WriteByte(ushort address, byte value)
    {
        switch(address)
        {
            case < 0x2000:
                _emulator.InstalledCartridge!.ChrData[m_ChrBank + address] = value;
                break;

            case >= 0x8000:
                m_ChrBank = value * 0x2000;
                m_ChrBank %= _emulator.InstalledCartridge!.ChrRomSize;
                break;
        }
    }
}
