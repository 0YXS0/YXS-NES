using System.IO;

namespace Nes.Core;

/// <summary>
///     Represents the PPU status register.
/// </summary>
/// <remarks>
///     7  bit  0
///     ---- ----
///     VSO. ....
///     |||| ||||
///     |||+-++++- PPU open bus. Returns stale PPU bus contents.
///     ||+------- Sprite overflow. The intent was for this flag to be set
///     ||         whenever more than eight sprites appear on a scanline, but a
///     ||         hardware bug causes the actual behavior to be more complicated
///     ||         and generate false positives as well as false negatives; see
///     ||         PPU sprite evaluation. This flag is set during sprite
///     ||         evaluation and cleared at dot 1 (the second dot) of the
///     ||         pre-render line.
///     |+-------- Sprite 0 Hit.  Set when a nonzero pixel of sprite 0 overlaps
///     |          a nonzero background pixel; cleared at dot 1 of the pre-render
///     |          line.  Used for raster timing.
///     +--------- Vertical blank has started (0: not in vblank; 1: in vblank).
///                Set at dot 1 of line 241 (the line *after* the post-render
///                line); cleared after reading $2002 and at dot 1 of the
///                pre-render line.
/// </remarks>

internal struct PpuStatusRegister
{

    #region Public Properties

    public byte LastRegisterWrite { get; set; }

    /// <summary>
    /// 精灵溢出标志(扫描线上的精灵数量超过8个)
    /// </summary>
    public byte SpriteOverflow { get; set; }
    public byte SpriteZeroHit { get; set; }
    public readonly byte Value
    {
        get
        {
            var ret = (byte)(LastRegisterWrite & 0x1f);
            ret |= (byte)(SpriteOverflow << 5);
            ret |= (byte)(SpriteZeroHit << 6);
            ret |= (byte)(VBlankStarted << 7);
            return ret;
        }
    }

    public byte VBlankStarted { get; set; }

    #endregion Public Properties

    #region Public Methods

    public void Reset( )
    {
        LastRegisterWrite = 0;
        SpriteOverflow = 0;
        SpriteZeroHit = 0;
        VBlankStarted = 0;
    }

    public readonly void Save(BinaryWriter writer)
    {
        writer.Write(LastRegisterWrite);
        writer.Write(SpriteOverflow);
        writer.Write(SpriteZeroHit);
        writer.Write(VBlankStarted);
    }

    public void Load(BinaryReader reader)
    {
        LastRegisterWrite = reader.ReadByte( );
        SpriteOverflow = reader.ReadByte( );
        SpriteZeroHit = reader.ReadByte( );
        VBlankStarted = reader.ReadByte( );
    }

    #endregion Public Methods

}
