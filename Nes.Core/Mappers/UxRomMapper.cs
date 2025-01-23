// ============================================================================
//  _ __   ___  ___  ___ _ __ ___  _   _
// | '_ \ / _ \/ __|/ _ \ '_ ` _ \| | | |
// | | | |  __/\__ \  __/ | | | | | |_| |
// |_| |_|\___||___/\___|_| |_| |_|\__,_|
//
// NES Emulator by daxnet, 2024
// MIT License
// ============================================================================

using System;

namespace NesEmu.Core.Mappers;

[Mapper(0x02, "UxROM")]
internal sealed class UxRomMapper : Mapper
{
    #region Public Constructors

    public UxRomMapper(Emulator emulator)
        : base(emulator)
    {
        if(emulator.InstalledCartridge is null)
            throw new InvalidOperationException( );

        _bank0 = 0;
        _bank1 = (emulator.InstalledCartridge.PrgRomBanks - 1) * 0x4000;
    }

    #endregion Public Constructors

    #region Private Fields

    private readonly int _bank1;

    private int _bank0;

    #endregion Private Fields

    #region Public Methods

    public override byte ReadByte(ushort address)
    {
        return address switch
        {
            < 0x2000 => _emulator.InstalledCartridge?.ReadChr(address) ?? default,
            >= 0x8000 and <= 0xbfff => _emulator.InstalledCartridge?.ReadPrgRom(_bank0 + (address - 0x8000)) ?? default,
            >= 0xc000 and <= 0xffff => _emulator.InstalledCartridge?.ReadPrgRom(_bank1 + (address - 0xc000)) ?? default,
            _ => 0
        };
    }

    public override void WriteByte(ushort address, byte value)
    {
        switch(address)
        {
            case < 0x2000:
                _emulator.InstalledCartridge?.WriteChr(address, value);
                break;

            case >= 0x8000:
                // Bank select
                // 7  bit  0
                // ---- ----
                // xxxx pPPP
                //      ||||
                //      ++++- Select 16 KB PRG ROM bank for CPU $8000-$BFFF
                //           (UNROM uses bits 2-0; UOROM uses bits 3-0)
                _bank0 = (value & 0xf) * 0x4000;
                break;
        }
    }

    #endregion Public Methods
}