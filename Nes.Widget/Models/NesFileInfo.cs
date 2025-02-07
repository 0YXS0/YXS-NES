namespace Nes.Widget.Models;

internal record NesFileInfo
{
    public required int Index { get; init; }
    public required string Name { get; init; }
    public required string Path { get; init; }
    public required int MapperNumber { get; init; }
    public required bool IsSupported { get; init; }
}

