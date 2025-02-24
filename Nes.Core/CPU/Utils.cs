namespace Nes.Core;

internal static class Utils
{
    #region Public Methods

    public static string FormatAddressByMode(AddressingMode mode, ushort address)
    {
        return mode switch
        {
            AddressingMode.Accumulator => "A",
            AddressingMode.Immediate or AddressingMode.Relative or AddressingMode.ZeroPage => $"#${address:x2}",
            AddressingMode.ZeroPageX => $"${address:x2},X",
            AddressingMode.ZeroPageY => $"${address:x2},Y",
            AddressingMode.Absolute or AddressingMode.Indirect => $"${address:x4}",
            AddressingMode.AbsoluteX => $"${address:x4},X",
            AddressingMode.AbsoluteY => $"${address:x4},Y",
            AddressingMode.IndexedIndirect => (address & 0xff) == address
                ? $"(${address:x2},X)"
                : $"(${address:x4},X)",
            AddressingMode.IndirectIndexed => (address & 0xff) == address
                ? $"(${address:x2}),Y"
                : $"(${address:x4}),Y",
            _ => string.Empty
        };
    }

    public static byte WrapToByte(this int value)
    {
        return (byte)(value % (byte.MaxValue + 1));
    }

    public static ushort WrapToWord(this int value)
    {
        return (ushort)(value % (ushort.MaxValue + 1));
    }

    #endregion Public Methods
}
