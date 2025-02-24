using System.IO;

namespace Nes.Core;

/// <summary>
///     Represents the PPU control register (PPUCTRL) which includes various flags controlling PPU operation.
/// </summary>
/// <remarks>
///     7  bit  0
///     ---- ----
///     VPHB SINN
///     |||| ||||
///     |||| ||++- Base nametable address
///     |||| ||    (0 = $2000; 1 = $2400; 2 = $2800; 3 = $2C00)
///     |||| |+--- VRAM address increment per CPU read/write of PPUDATA
///     |||| |     (0: add 1, going across; 1: add 32, going down)
///     |||| +---- Sprite pattern table address for 8x8 sprites
///     ||||       (0: $0000; 1: $1000; ignored in 8x16 mode)
///     |||+------ Background pattern table address (0: $0000; 1: $1000)
///     ||+------- Sprite size (0: 8x8 pixels; 1: 8x16 pixels – see PPU OAM#Byte 1)
///     |+-------- PPU master/slave select
///     |          (0: read backdrop from EXT pins; 1: output color on EXT pins)
///     +--------- Generate an NMI at the start of the
///     vertical blanking interval (0: off; 1: on)
/// </remarks>
internal struct PpuControlRegister
{

    #region Public Constructors

    /// <summary>
    ///     Initializes the values of <see cref="PpuControlRegister" />.
    /// </summary>
    /// <param name="value">The value</param>
    public PpuControlRegister(byte value)
        : this( )
    {
        Set(value);
    }

    #endregion Public Constructors

    #region Public Properties

    public ushort BackgroundPatternTableAddress { get; private set; }

    public ushort BaseNametableAddress { get; private set; }

    /// <summary>
    /// 在垂直消隐（VBlank）间隔开始时，该值指示是否应生成NMI。
    /// True：打开；False：关闭。
    /// </summary>
    public bool GenerateNmiWhenVBlankBegins { get; private set; }

    /// <summary>
    ///     Gets the PPU master/slave select flag. 0: Read backdrop from EXT pins; 1: output color on EXT pins.
    /// </summary>
    public int PpuMasterSlaveSelectFlag { get; private set; }

    public ushort SpritePatternTableAddress { get; private set; }

    /// <summary>
    /// 获取角色大小，0:8x8像素；1:8x16像素。
    /// </summary>
    public int SpriteSizeFlag { get; private set; }

    public byte Value { get; private set; }
    public int VramAddressIncrement { get; private set; }

    #endregion Public Properties

    #region Public Methods

    /// <summary>
    ///     Resets the value of the control register.
    /// </summary>
    public void Reset( )
    {
        BackgroundPatternTableAddress = 0;
        BaseNametableAddress = 0;
        GenerateNmiWhenVBlankBegins = false;
        PpuMasterSlaveSelectFlag = 0;
        SpritePatternTableAddress = 0;
        SpriteSizeFlag = 0;
        VramAddressIncrement = 0;
    }

    /// <summary>
    ///     Sets the value of the control register.
    /// </summary>
    /// <param name="data">The value to be set into the register.</param>
    public void Set(byte data)
    {
        Value = data;
        BackgroundPatternTableAddress = (ushort)((data & 16) == 0 ? 0 : 0x1000);
        BaseNametableAddress = (ushort)(0x2000 + (data & 3) * 0x400);
        GenerateNmiWhenVBlankBegins = (data & 128) != 0;
        PpuMasterSlaveSelectFlag = (data & 64) >> 6;
        SpritePatternTableAddress = (ushort)((data & 8) == 0 ? 0 : 0x1000);
        SpriteSizeFlag = (data & 32) >> 5;
        VramAddressIncrement = (data & 4) == 0 ? 1 : 32;
    }

    public readonly void Save(BinaryWriter writer)
    {
        writer.Write(BackgroundPatternTableAddress);
        writer.Write(BaseNametableAddress);
        writer.Write(GenerateNmiWhenVBlankBegins);
        writer.Write(PpuMasterSlaveSelectFlag);
        writer.Write(SpritePatternTableAddress);
        writer.Write(SpriteSizeFlag);
        writer.Write(Value);
        writer.Write(VramAddressIncrement);
    }

    public void Load(BinaryReader reader)
    {
        BackgroundPatternTableAddress = reader.ReadUInt16( );
        BaseNametableAddress = reader.ReadUInt16( );
        GenerateNmiWhenVBlankBegins = reader.ReadBoolean( );
        PpuMasterSlaveSelectFlag = reader.ReadInt32( );
        SpritePatternTableAddress = reader.ReadUInt16( );
        SpriteSizeFlag = reader.ReadInt32( );
        Value = reader.ReadByte( );
        VramAddressIncrement = reader.ReadInt32( );
    }

    #endregion Public Methods

}