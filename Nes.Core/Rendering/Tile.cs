using System;
using System.Collections.Generic;

namespace Nes.Core.Rendering
{
    public sealed class Tile
    {
        private const int TileWidth = 8;
        private const int TileHeight = 8;

        private readonly byte[] _data = new byte[TileWidth * TileHeight];
        private readonly byte[] _block;

        /// <summary>
        /// Initializes a new instance of the <c>Tile</c> class.
        /// </summary>
        /// <param name="block">A byte array with 16 bytes that contains the original bytes in the CHR ROM space which
        /// represents a tile.</param>
        /// <exception cref="ArgumentException"></exception>
        public Tile(byte[] block)
        {
            _block = block;
        }

        public IEnumerable<byte> Data => _data;

        public byte this[int x, int y]
        {
            get => _data[y * TileWidth + x];
            private set => _data[y * TileWidth + x] = value;
        }

        public void Render(Palette palette, PaletteIndexer paletteIndexer, RenderPixelCallback renderPixelCallback)
        {
            if(_block.Length != 16)
            {
                throw new ArgumentException("Size of the tile block should be 16 bytes length.");
            }

            // for each row in the tile
            for(var y = 0; y < TileHeight; y++)
            {
                var hi = _block[y + TileHeight];
                var lo = _block[y];

                // for each bit (column) in the tile
                for(var x = 0; x < TileWidth; x++)
                {
                    var value = (hi >> 7 - x & 1) << 1 | lo >> 7 - x & 1;
                    this[x, y] = (byte)value;
                    var colorRgb = palette.PaletteDefinition[paletteIndexer(this[x, y])];
                    renderPixelCallback(x, y, colorRgb);
                }
            }

        }
    }
}
