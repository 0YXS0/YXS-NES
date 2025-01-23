// ============================================================================
//  _ __   ___  ___  ___ _ __ ___  _   _
// | '_ \ / _ \/ __|/ _ \ '_ ` _ \| | | |
// | | | |  __/\__ \  __/ | | | | | |_| |
// |_| |_|\___||___/\___|_| |_| |_|\__,_|
//
// NES Emulator by daxnet, 2024
// MIT License
// ============================================================================

namespace NesEmu.Core;

/// <summary>
///     Represents the PPU Mask register (PPUMASK).
/// </summary>
/// <remarks>
///     7  bit  0
///     ---- ----
///     BGRs bMmG
///     |||| ||||
///     |||| |||+- Greyscale (0: normal color, 1: produce a greyscale display)
///     |||| ||+-- 1: Show background in leftmost 8 pixels of screen, 0: Hide
///     |||| |+--- 1: Show sprites in leftmost 8 pixels of screen, 0: Hide
///     |||| +---- 1: Show background
///     |||+------ 1: Show sprites
///     ||+------- Emphasize red (green on PAL/Dendy)
///     |+-------- Emphasize green (red on PAL/Dendy)
///     +--------- Emphasize blue
/// </remarks>
internal struct PpuMaskRegister
{
    #region Public Properties

    /// <summary>
    ///     Emphasize blue.
    /// </summary>
    public byte EmphasizeBlue { get; private set; }

    /// <summary>
    ///     Emphasize green (red on PAL/Dendy).
    /// </summary>
    public byte EmphasizeGreen { get; private set; }

    /// <summary>
    ///     Emphasize red (green on PAL/Dendy).
    /// </summary>
    public byte EmphasizeRed { get; private set; }

    public byte IsGreyScale { get; private set; }
    public byte ShowBackground { get; private set; }
    public byte ShowBackgroundInLeftmost8PixelsOfScreen { get; private set; }
    public byte ShowSprites { get; private set; }
    public byte ShowSpritesInLeftmost8PixelsOfScreen { get; private set; }

    public byte Value { get; private set; }

    #endregion Public Properties

    #region Public Methods

    /// <summary>
    ///     Resets the value of the mask register.
    /// </summary>
    public void Reset( )
    {
        EmphasizeBlue = 0;
        EmphasizeGreen = 0;
        EmphasizeRed = 0;
        IsGreyScale = 0;
        ShowBackground = 0;
        ShowSprites = 0;
        ShowBackgroundInLeftmost8PixelsOfScreen = 0;
        ShowSpritesInLeftmost8PixelsOfScreen = 0;
    }

    /// <summary>
    ///     Sets the value of the mask register.
    /// </summary>
    /// <param name="value">The value to be set into the register.</param>
    public void Set(byte value)
    {
        Value = value;
        IsGreyScale = (byte)(value & 1);
        ShowBackgroundInLeftmost8PixelsOfScreen = (byte)((value >> 1) & 1);
        ShowSpritesInLeftmost8PixelsOfScreen = (byte)((value >> 2) & 1);
        ShowBackground = (byte)((value >> 3) & 1);
        ShowSprites = (byte)((value >> 4) & 1);
        EmphasizeRed = (byte)((value >> 5) & 1);
        EmphasizeGreen = (byte)((value >> 6) & 1);
        EmphasizeBlue = (byte)((value >> 7) & 1);
    }

    #endregion Public Methods
}