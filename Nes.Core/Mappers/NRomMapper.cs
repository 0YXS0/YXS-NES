// ============================================================================
//  _ __   ___  ___  ___ _ __ ___  _   _
// | '_ \ / _ \/ __|/ _ \ '_ ` _ \| | | |
// | | | |  __/\__ \  __/ | | | | | |_| |
// |_| |_|\___||___/\___|_| |_| |_|\__,_|
//
// NES Emulator by daxnet, 2024
// MIT License
// ============================================================================

namespace Nes.Core.Mappers;

[Mapper(0, "NROM")]
internal sealed class NRomMapper(Emulator emulator) : Mapper(emulator)
{
    #region Private Methods

    private static ushort MapAddress(Cartridge cartridge, ushort address)
    {
        var mappedAddress = (ushort)(address - 0x8000);
        mappedAddress = cartridge.PrgRomBanks == 1
            ? (ushort)(mappedAddress % 0x4000) // Mirrors 0x8000 - 0xBFFF for NROM-128 (PRG ROM BANKS = 1)
            : mappedAddress;

        return mappedAddress;
    }

    #endregion Private Methods

    #region Public Methods

    public override byte ReadByte(ushort address)
    {
        return address switch
        {
            < 0x2000 => _emulator.InstalledCartridge?.ReadChr(address) ?? default,
            >= 0x8000 => _emulator.InstalledCartridge?.PrgRom[MapAddress(_emulator.InstalledCartridge, address)]
                         ??
                         default,
            _ => 0
        };
    }

    public override void WriteByte(ushort address, byte value)
    {
        if(address < 0x2000)
        {
            _emulator.InstalledCartridge?.WriteChr(address, value);
        }
    }

    #endregion Public Methods
}