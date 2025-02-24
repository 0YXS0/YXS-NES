using System;

namespace Nes.Core
{
    public struct CpuFlags : IEquatable<CpuFlags>
    {
        #region Private Fields

        private byte _value;

        #endregion Private Fields

        #region Public Constructors

        public CpuFlags( ) : this(0)
        {
        }

        public CpuFlags(byte value) => _value = value;

        #endregion Public Constructors

        #region Public Properties

        public Bit Bit4
        {
            readonly get => Bit.Get(_value, 4);
            set => _value = value ? Bit.Set(_value, 4) : Bit.Unset(_value, 4);
        }

        public Bit Bit5
        {
            readonly get => Bit.Get(_value, 5);
            set => _value = value ? Bit.Set(_value, 5) : Bit.Unset(_value, 5);
        }

        /// <summary>
        /// Gets or sets the Carry (C) flag.
        /// </summary>
        public Bit C
        {
            readonly get => Bit.Get(_value, 0);
            set => _value = value ? Bit.Set(_value, 0) : Bit.Unset(_value, 0);
        }

        /// <summary>
        /// Gets or sets the Decimal (D) flag.
        /// </summary>
        public Bit D
        {
            readonly get => Bit.Get(_value, 3);
            set => _value = value ? Bit.Set(_value, 3) : Bit.Unset(_value, 3);
        }

        /// <summary>
        /// Gets or sets the Interrupt Disable (I) flag.
        /// </summary>
        public Bit I
        {
            readonly get => Bit.Get(_value, 2);
            set => _value = value ? Bit.Set(_value, 2) : Bit.Unset(_value, 2);
        }

        /// <summary>
        /// Gets or sets the Negative (N) flag.
        /// </summary>
        public Bit N
        {
            readonly get => Bit.Get(_value, 7);
            set => _value = value ? Bit.Set(_value, 7) : Bit.Unset(_value, 7);
        }

        /// <summary>
        /// Gets or sets the Overflow (V) flag.
        /// </summary>
        public Bit V
        {
            readonly get => Bit.Get(_value, 6);
            set => _value = value ? Bit.Set(_value, 6) : Bit.Unset(_value, 6);
        }

        public byte Value
        {
            readonly get => _value;
            internal set => _value = value;
        }

        /// <summary>
        /// Gets or sets the Zero (Z) flag.
        /// </summary>
        public Bit Z
        {
            readonly get => Bit.Get(_value, 1);
            set => _value = value ? Bit.Set(_value, 1) : Bit.Unset(_value, 1);
        }

        #endregion Public Properties

        #region Public Methods

        public static bool operator !=(CpuFlags left, CpuFlags right)
        {
            return !(left == right);
        }

        public static bool operator ==(CpuFlags left, CpuFlags right)
        {
            return left.Equals(right);
        }

        public override readonly bool Equals(object? obj)
        {
            return obj is CpuFlags flags && Equals(flags);
        }

        public readonly bool Equals(CpuFlags other)
        {
            return _value == other._value;
        }

        public override readonly int GetHashCode( ) => _value.GetHashCode( );

        public override readonly string ToString( ) => Convert.ToString(_value, 2).PadLeft(8, '0');

        #endregion Public Methods
    }
}