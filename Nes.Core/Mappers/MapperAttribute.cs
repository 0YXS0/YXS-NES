using System;

namespace Nes.Core.Mappers;

[AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
public sealed class MapperAttribute : Attribute
{
    public MapperAttribute(int number, string name)
    {
        Number = number;
        Name = name;
    }

    public string Name { get; }
    public int Number { get; }
}