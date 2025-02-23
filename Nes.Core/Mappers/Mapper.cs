namespace Nes.Core.Mappers;

public abstract class Mapper(Emulator emulator)
{
    protected readonly Emulator m_emulator = emulator;

    public abstract byte ReadByte(ushort address);

    public abstract void WriteByte(ushort address, byte value);

    public virtual void IrqTick( ) { }
}
