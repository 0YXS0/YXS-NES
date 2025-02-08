// ============================================================================
//  _ __   ___  ___  ___ _ __ ___  _   _
// | '_ \ / _ \/ __|/ _ \ '_ ` _ \| | | |
// | | | |  __/\__ \  __/ | | | | | |_| |
// |_| |_|\___||___/\___|_| |_| |_|\__,_|
//
// NES Emulator by daxnet, 2023
// MIT License
// ============================================================================

namespace Nes.Core
{
    /// <summary>
    /// Represents a bit in a byte.
    /// </summary>
    public readonly struct Bit
    {
        #region Public Fields

        public static readonly Bit BitClear = new(0);
        public static readonly Bit BitSet = new(1);

        #endregion Public Fields

        #region Private Fields

        private readonly int _value;

        #endregion Private Fields

        #region Public Constructors

        /// <summary>
        /// Initializes a new instance of the <c>Bit</c> struct.
        /// </summary>
        public Bit( ) : this(0)
        { }

        /// <summary>
        /// Initializes a new instance of the <c>Bit</c> struct.
        /// </summary>
        /// <param name="value">A integer value that is used to initialize the bit value.
        /// If this integer value equals to zero, the bit is cleared, otherwise, the bit
        /// is set.</param>
        public Bit(int value) => _value = value == 0 ? 0 : 1;

        /// <summary>
        /// Initializes a new instance of the <c>Bit</c> struct.
        /// </summary>
        /// <param name="value">A boolean value that is used to initialize the bit value.
        /// If the boolean value is true, the bit is set, otherwise the bit is cleared.</param>
        public Bit(bool value)
        {
            _value = value ? 1 : 0;
        }

        #endregion Public Constructors

        #region Public Methods

        /// <summary>
        /// Gets a bit from the given byte.
        /// </summary>
        /// <param name="src">The byte.</param>
        /// <param name="bitPos">The position of the bit to return.</param>
        /// <returns>The bit value.</returns>
        public static Bit Get(byte src, int bitPos) => (src >> bitPos) & 1;

        /// <summary>
        /// Checks whether the bit of the given byte has been set.
        /// </summary>
        /// <param name="src">The byte to check.</param>
        /// <param name="bitPos">The position of the bit.</param>
        /// <returns>True if the bit has been set, otherwise, false.</returns>
        public static bool HasSet(byte src, int bitPos) => Get(src, bitPos) != 0;

        /// <summary>
        /// Implicitly converts a <see cref="int"/> value to a <c>Bit</c> struct.
        /// </summary>
        /// <param name="value">The integer value to be converted. If the <paramref name="value"/> is zero,
        /// then the bit will be cleared, otherwise, the bit will be set.</param>
        public static implicit operator Bit(int value) => new(value);

        /// <summary>
        /// Implicitly converts a <see cref="bool"/> value to a <c>Bit</c> struct.
        /// </summary>
        /// <param name="value">The boolean value to be converted. If the <paramref name="value"/> is false,
        /// then the bit will be cleared, otherwise, the bit will be set.</param>
        public static implicit operator Bit(bool value) => new(value);

        /// <summary>
        /// Implicitly converts a <c>Bit</c> struct to a <see cref="bool"/> value.
        /// </summary>
        /// <param name="bit">The <c>Bit</c> value to be converted.</param>
        public static implicit operator bool(Bit bit) => bit._value == 1;

        /// <summary>
        /// Implicitly converts a <c>Bit</c> struct to a <see cref="int"/> value.
        /// </summary>
        /// <param name="bit">The <c>Bit</c> value to be converted.</param>
        public static implicit operator int(Bit bit) => bit._value;

        /// <summary>
        /// Compares two <c>Bit</c> values and returns true if they are not equal, otherwise, returns false.
        /// </summary>
        /// <param name="a">The first Bit value to be compared with.</param>
        /// <param name="b">The second Bit value to compare.</param>
        /// <returns>True if the two are not equal, otherwise, false.</returns>
        public static bool operator !=(Bit a, Bit b) => !(a == b);

        /// <summary>
        /// Performs the arithmetic AND operation on the two given <c>Bit</c>s.
        /// </summary>
        /// <param name="a">The first operand.</param>
        /// <param name="b">The second operand.</param>
        /// <returns>The result.</returns>
        public static Bit operator &(Bit a, Bit b) => a._value & b._value;

        /// <summary>
        ///
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <returns></returns>
        public static Bit operator ^(Bit a, Bit b) => a._value ^ b._value;

        public static Bit operator |(Bit a, Bit b) => a._value | b._value;

        public static Bit operator ~(Bit a) => !a;

        public static bool operator ==(Bit a, Bit b) => a._value == b._value;

        /// <summary>
        /// Sets a bit in a given byte on the given bit position.
        /// </summary>
        /// <param name="src">The byte to be operated on.</param>
        /// <param name="bitPos">The position in the byte to be set.</param>
        /// <returns>A byte with the specified position being set.</returns>
        public static byte Set(byte src, int bitPos) => (byte)(src | (1 << bitPos));

        /// <summary>
        /// Unsets (clears) a bit in a given byte on the given bit position.
        /// </summary>
        /// <param name="src">The byte to be operated on.</param>
        /// <param name="bitPos">The position in the byte to be cleared.</param>
        /// <returns>A byte with the specified position being cleared.</returns>
        public static byte Unset(byte src, int bitPos) => (byte)(src & ~(1 << bitPos));

        /// <inheritdoc/>
        public override bool Equals(object? obj) => obj is Bit bit && _value == bit._value;

        /// <inheritdoc/>
        public override int GetHashCode( ) => _value.GetHashCode( );

        /// <inheritdoc/>
        public override string ToString( ) => _value.ToString( );

        #endregion Public Methods
    }
}