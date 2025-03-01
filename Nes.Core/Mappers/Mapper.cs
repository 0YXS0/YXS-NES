using System.IO;

namespace Nes.Core.Mappers;

public abstract class Mapper(Emulator emulator)
{
    protected readonly Emulator m_emulator = emulator;

    public abstract byte ReadByte(ushort address);

    public abstract void WriteByte(ushort address, byte value);

    public virtual void IrqTick( ) { }

    /// <summary>
    /// 存档
    /// </summary>
    public virtual void Save(BinaryWriter writer) { }

    /// <summary>
    /// 读档
    /// </summary>
    public virtual void Load(BinaryReader reader) { }
}
