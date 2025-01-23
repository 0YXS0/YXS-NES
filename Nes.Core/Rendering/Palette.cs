namespace NesEmu.Core.Rendering
{
    public abstract class Palette
    {
        public abstract ColorRgb[] PaletteDefinition { get; }

        public static readonly Palette Default = new DefaultPalette( );
    }
}
