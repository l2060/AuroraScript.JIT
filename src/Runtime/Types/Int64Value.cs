using System.Globalization;
using System.Runtime.CompilerServices;

namespace AuroraScript.Runtime.Types
{
    /// <summary>Immutable object view of an exact signed 64-bit integer datum.</summary>
    public sealed class Int64Value : ScriptImmutable
    {
        /// <summary>Creates an immutable wrapper for an exact signed 64-bit value.</summary>
        public Int64Value(long value) : base(Prototypes.ObjectPrototype)
        {
            Value = value;
        }

        /// <summary>Gets the wrapped value.</summary>
        public long Value { get; }

        /// <inheritdoc />
        protected internal override ScriptDatum TypeOfValue => TypeNames.Int64;

        /// <inheritdoc />
        public override string ToString() => Value.ToString(CultureInfo.InvariantCulture);

        /// <inheritdoc />
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override bool IsTrue() => Value != 0;
    }
}
