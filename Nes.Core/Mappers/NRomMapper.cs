﻿namespace Nes.Core.Mappers;

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
            < 0x2000 => m_emulator.InstalledCartridge?.ChrData[address] ?? default,
            >= 0x8000 => m_emulator.InstalledCartridge?.PrgRom[MapAddress(m_emulator.InstalledCartridge, address)]
                         ??
                         default,
            _ => 0
        };
    }

    public override void WriteByte(ushort address, byte value)
    {
        if(address < 0x2000)
        {
            m_emulator.InstalledCartridge!.ChrData[address] = value;
        }
    }

    #endregion Public Methods
}