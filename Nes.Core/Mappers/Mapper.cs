namespace NesEmu.Core.Mappers
{
    public abstract class Mapper(Emulator emulator)
    {
        protected readonly Emulator _emulator = emulator;

        public abstract byte ReadByte(ushort address);

        public abstract void WriteByte(ushort address, byte value);
    }
}
