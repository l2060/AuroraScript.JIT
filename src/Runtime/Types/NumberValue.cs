using System.Runtime.CompilerServices;

namespace AuroraScript.Runtime.Types
{
    /// <summary>
    /// Represents a double-precision floating-point number in AuroraScript.
    /// This is an immutable object wrapping a CLI <see cref="double"/>.
    /// </summary>
    public sealed partial class NumberValue : ScriptImmutable
    {
        private readonly double _value;

        /// <summary>
        /// Initializes a new instance of the <see cref="NumberValue"/> class.
        /// </summary>
        /// <param name="dValue">The double precision value.</param>
        public NumberValue(double dValue = 0) : base(Prototypes.NumberValuePrototype)
        {
            _value = dValue;
        }

        /// <summary> Gets the underlying double value. </summary>
        public double DoubleValue => _value;

        /// <inheritdoc />
        protected internal override ScriptDatum TypeOfValue => TypeNames.Number;

        /// <summary> Gets the value cast to a 32-bit integer. </summary>
        public int Int32Value => (int)_value;

        /// <summary> Gets the value cast to a 64-bit integer. </summary>
        public long Int64Value => (long)_value;

        /// <summary>
        /// Returns the string representation of the numeric value.
        /// </summary>
        /// <returns>A string representing the number.</returns>
        public override string ToString()
        {
            return _value.ToString();
        }

        /// <summary>
        /// Checks if the numeric value represents a "truthy" value (anything other than 0).
        /// </summary>
        /// <returns>True if the value is not 0; otherwise, false.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override bool IsTrue()
        {
            return _value != 0;
        }

        /// <summary>
        /// Returns a <see cref="NumberValue"/> instance for the given double value.
        /// Attempts to use cached singletons for common values like 0 or NaN.
        /// </summary>
        /// <param name="value">The double value.</param>
        /// <returns>A corresponding <see cref="NumberValue"/> instance.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static NumberValue Of(double value)
        {
            if (double.IsNaN(value))
            {
                return NaN;
            }
            if (value == 0d)
            {
                return Zero;
            }
            if (value == 1d) return Num1;
            if (value == 2d) return Num2;
            if (value == 3d) return Num3;
            if (value == 4d) return Num4;
            if (value == 5d) return Num5;
            if (value == 6d) return Num6;
            if (value == 7d) return Num7;
            if (value == 8d) return Num8;
            if (value == 9d) return Num9;
            if (value == -1d) return Negative1;
            return new NumberValue(value);
        }
    }
}
