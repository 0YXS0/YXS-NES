// ============================================================================
//  _ __   ___  ___  ___ _ __ ___  _   _
// | '_ \ / _ \/ __|/ _ \ '_ ` _ \| | | |
// | | | |  __/\__ \  __/ | | | | | |_| |
// |_| |_|\___||___/\___|_| |_| |_|\__,_|
//
// NES Emulator by daxnet, 2024
// MIT License
// ============================================================================

namespace Nes.Core;

/// <summary>
///     Represents the PPU scroll register.
/// </summary>
internal struct PpuScrollRegister
{

    #region Public Properties

    /// <summary>
    /// Gets the value of the Scroll register.
    /// </summary>
    public byte Value { get; private set; }

    #endregion Public Properties

    #region Public Methods

    /// <summary>
    /// Resets the value of the Scroll register.
    /// </summary>
    public void Reset( ) => Value = 0;

    /// <summary>
    ///     Sets the value of the Scroll register.
    /// </summary>
    /// <param name="value">The value to be set into the register.</param>
    /// <param name="firstWriteFlag">
    ///     The flag which indicates if the current write is a first write, this
    ///     value is shared with PpuAddressRegister.
    /// </param>
    /// <param name="tempVramAddress">The temporary VRam address.</param>
    public void Set(byte value, ref byte firstWriteFlag, ref ushort tempVramAddress)
    {
        if(firstWriteFlag == 0)
        {
            tempVramAddress = (ushort)((tempVramAddress & 0xffe0) | (value >> 3));
            Value = (byte)(value & 0x7);
            firstWriteFlag = 1;
        }
        else
        {
            tempVramAddress = (ushort)(tempVramAddress & 0xc1f);
            tempVramAddress |= (ushort)((value & 0x7) << 12);
            tempVramAddress |= (ushort)((value & 0xf8) << 2);
            firstWriteFlag = 0;
        }
    }

    #endregion Public Methods
}