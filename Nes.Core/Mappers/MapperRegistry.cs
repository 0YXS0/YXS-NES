using System;

namespace Nes.Core.Mappers
{
    public record MapperRegistry(int Number, string Name, Func<Emulator, Mapper> Factory)
    {
    }
}
