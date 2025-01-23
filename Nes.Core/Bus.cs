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

namespace NesEmu.Core;

// ----------------------------------------------------------------------------------------------------------|
// |  $0000-$07FF  |  $0800  |  2KB internal RAM                                                             |
// |---------------+---------+-------------------------------------------------------------------------------|
// |  $0800-$0FFF  |  $0800  |                                                                               |
// |---------------+---------|                                                                               |
// |  $1000-$17FF  |  $0800  |  Mirrors of $0000-$07FF                                                       |
// |---------------+---------|                                                                               |
// |  $1800-$1FFF  |  $0800  |                                                                               |
// |---------------+---------+-------------------------------------------------------------------------------|
// |  $2000-$2007  |  $0008  |  NES PPU registers                                                            |
// |---------------+---------+-------------------------------------------------------------------------------|
// |  $2008-$3FFF  |  $1FF8  |  Mirrors of $2000-2007 (repeats every 8 bytes)                                |
// |---------------+---------+-------------------------------------------------------------------------------|
// |  $4000-$4017  |  $0018  |  NES APU and I/O registers                                                    |
// |---------------+---------+-------------------------------------------------------------------------------|
// |  $4018-$401F  |  $0008  |  APU and I/O functionality that is normally disabled. See CPU Test Mode.      |
// |---------------+---------+-------------------------------------------------------------------------------|
// |  $4020-$FFFF  |  $BFE0  |  Cartridge space: PRG ROM, PRG RAM, and mapper registers.                     |
// |---------------------------------------------------------------------------------------------------------|

/// <summary>
///     Represents the Bus concept in the NES emulator.
/// </summary>
public class Bus(Emulator emulator)
{
    #region Protected Fields

    protected readonly byte[] _ram = new byte[0x2000];

    #endregion Protected Fields

    #region Internal Methods

    internal void DirectMemoryRead(byte[] buffer, int start, ushort address, int size)
    {
        var index = start;
        var bytesRead = 0;
        var addr = address;
        while(bytesRead < size)
        {
            if(index >= buffer.Length) index = 0;
            buffer[index] = ReadByte(addr);

            addr++;
            bytesRead++;
            index++;
        }
    }

    #endregion Internal Methods

    #region Private Methods

    private static ushort MapPpuRegisterAddress(ushort address)
    {
        if(address == 0x4014) return address;
        return (ushort)(0x2000 + (address - 0x2000) % 8);
    }

    #endregion Private Methods

    #region Public Methods

    /// <summary>
    /// 根据给出的地址读取一个字节数据
    /// </summary>
    /// <param name="address">地址</param>
    /// <returns>对应数据</returns>
    public virtual byte ReadByte(ushort address) =>
        address switch
        {
            < 0x2000 => _ram[address & 0x07FF],
            >= 0x2000 and <= 0x3fff => emulator.Ppu.ReadRegister(MapPpuRegisterAddress(address)),
            <= 0x401f and not 0x4016 => 0,
            0x4016 => emulator.Controller.ReadControllerInput( ),
            _ => emulator.Mapper.ReadByte(address) // In case for addresses >= 0x4020
        };

    public virtual ushort ReadWord(ushort address)
    {
        var hi = ReadByte((ushort)(address + 1));
        var lo = ReadByte(address);
        return (ushort)((hi << 8) | lo);
    }

    /// <summary>
    ///     Resets the internal memory.
    /// </summary>
    public void Reset( ) => Array.Clear(_ram, 0, _ram.Length);

    public virtual void WriteByte(ushort address, byte data)
    {
        switch(address)
        {
            case < 0x2000:
                _ram[address % 0x800] = data;
                break;

            case >= 0x2000 and <= 0x3fff or 0x4014:
                emulator.Ppu.WriteRegister(MapPpuRegisterAddress(address), data);
                break;

            case 0x4016:
                emulator.Controller.WriteControllerInput(data);
                break;

            case <= 0x401f:
                break;

            default:
                emulator.Mapper.WriteByte(address, data);
                break;
        }
    }

    public virtual void WriteWord(ushort address, ushort value)
    {
        var hi = (byte)(value >> 8);
        var lo = (byte)(value & 0xff);
        WriteByte(address, lo);
        WriteByte((ushort)(address + 1), hi);
    }

    #endregion Public Methods
}