using System;

namespace Nes.Core.Mappers;

[Mapper(0x02, "UxROM")]
internal sealed class UxRomMapper : Mapper
{
    public UxRomMapper(Emulator emulator)
        : base(emulator)
    {
        if(emulator.InstalledCartridge is null)
            throw new InvalidOperationException("模拟器为空。");

        _bank0 = 0;
        if(emulator.InstalledCartridge.PrgRomBanks > 1)
            _bank1 = (emulator.InstalledCartridge.PrgRomBanks - 1) * 0x4000;
        else
            _bank1 = 0;
    }

    private readonly int _bank1;

    private int _bank0;

    public override byte ReadByte(ushort address)
    {
        return address switch
        {
            < 0x2000 => m_emulator.InstalledCartridge?.ChrData[address] ?? default,
            >= 0x8000 and <= 0xbfff => m_emulator.InstalledCartridge?.PrgRom[_bank0 + (address - 0x8000)] ?? default,
            >= 0xc000 and <= 0xffff => m_emulator.InstalledCartridge?.PrgRom[_bank1 + (address - 0xC000)] ?? default,
            _ => 0
        };
    }

    public override void WriteByte(ushort address, byte value)
    {
        switch(address)
        {
            case < 0x2000:
                m_emulator.InstalledCartridge!.ChrData[address] = value;
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
}