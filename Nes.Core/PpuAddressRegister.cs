// ============================================================================
//  _ __   ___  ___  ___ _ __ ___  _   _
// | '_ \ / _ \/ __|/ _ \ '_ ` _ \| | | |
// | | | |  __/\__ \  __/ | | | | | |_| |
// |_| |_|\___||___/\___|_| |_| |_|\__,_|
//
// NES Emulator by daxnet, 2024
// MIT License
// ============================================================================

using System.IO;

namespace Nes.Core;

/// <summary>
///     Represents the PPU address register.
/// </summary>
internal struct PpuAddressRegister
{
    #region Public Properties

    /// <summary>
    ///     Gets or sets the current VRam address value.
    /// </summary>
    public ushort Value { get; set; }

    #endregion Public Properties

    #region Public Methods

    public void Reset( ) => Value = 0;

    /// <summary>
    ///     Sets the value of the VRam address during the write of the PPU register.
    /// </summary>
    /// <param name="value">The value to be set into the VRam address.</param>
    /// <param name="firstWriteFlag">
    ///     The flag which indicates if the current write is a first write, this
    ///     value is shared with PpuScrollRegister.
    /// </param>
    /// <param name="tempVramAddress">The temporary VRam address.</param>
    public void Set(byte value, ref byte firstWriteFlag, ref ushort tempVramAddress)
    {
        if(firstWriteFlag == 0)
        {
            tempVramAddress = (ushort)((tempVramAddress & 0xff) | (value << 8));
            firstWriteFlag = 1;
        }
        else
        {
            tempVramAddress = (ushort)((tempVramAddress & 0xff00) | value);
            Value = tempVramAddress;
            firstWriteFlag = 0;
        }
    }

    public readonly void Save(BinaryWriter writer) => writer.Write(Value);

    public void Load(BinaryReader reader) => Value = reader.ReadUInt16( );

    #endregion Public Methods
}