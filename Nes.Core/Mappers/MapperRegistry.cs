using System;

namespace NesEmu.Core.Mappers
{
    public record MapperRegistry(int Number, string Name, Func<Emulator, Mapper> Factory)
    {
    }
}
