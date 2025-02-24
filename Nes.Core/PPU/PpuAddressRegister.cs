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
    /// 在 PPU 寄存器写入期间设置 VRAM 地址的值。
    /// </summary>
    /// <param name="value">要设置到 VRAM 地址的值。</param>
    /// <param name="firstWriteFlag">一个标志，用于指示当前写入是否是首次写入，此值与 PpuScrollRegister 共享。</param>
    /// <param name="tempVramAddress">临时 VRAM 地址。</param>
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