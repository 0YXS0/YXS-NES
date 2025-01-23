using System;

namespace NesEmu.Core.Rendering
{
    public sealed class Renderer
    {
        public void RenderCartridgeChrRomBank(Cartridge cartridge, int bank, int tileNumber, Palette palette,
            PaletteIndexer paletteIndexer, RenderPixelCallback renderPixelCallback)
        {
            if(cartridge.ChrRomSize == 0)
            {
                return;
            }

            var startIndex = bank * 0x1000 + tileNumber * 0x10;
            var tileBlock = new byte[16];
            Array.Copy(cartridge.ChrData, startIndex, tileBlock, 0, 16);
            var tile = new Tile(tileBlock);
            tile.Render(palette, paletteIndexer, renderPixelCallback);
        }
    }
}
