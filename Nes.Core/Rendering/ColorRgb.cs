namespace Nes.Core.Rendering
{
    public struct ColorRgb(int r, int g, int b)
    {

        #region Public Properties

        public int B { get; set; } = b;

        public int G { get; set; } = g;

        public int R { get; set; } = r;

        #endregion Public Properties

        #region Public Methods

        public static implicit operator (int, int, int)(ColorRgb value) => (value.R, value.G, value.B);

        public static implicit operator ColorRgb((int r, int g, int b) value) =>
            new ColorRgb(value.r, value.g, value.b);

        public static bool operator !=(ColorRgb left, ColorRgb right)
        {
            return !(left == right);
        }

        public static bool operator ==(ColorRgb left, ColorRgb right)
        {
            return left.R == right.R &&
                   left.G == right.G &&
                   left.B == right.B;
        }

        public readonly void Deconstruct(out int r, out int g, out int b)
        {
            r = R;
            g = G;
            b = B;
        }

        public readonly override bool Equals(object? obj)
        {
            return obj is ColorRgb other && Equals(other);
        }

        /// <inheritdoc />
        public readonly override int GetHashCode( )
        {
            unchecked
            {
                var hashCode = R;
                hashCode = hashCode * 397 ^ G;
                hashCode = hashCode * 397 ^ B;
                return hashCode;
            }
        }

        #endregion Public Methods

        #region Private Methods

        private readonly bool Equals(ColorRgb other) => R == other.R && G == other.G && B == other.B;

        #endregion Private Methods

    }
}
