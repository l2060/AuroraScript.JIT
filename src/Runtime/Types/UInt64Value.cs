using System.Globalization;
using System.Runtime.CompilerServices;

namespace AuroraScript.Runtime.Types
{
    /// <summary>Immutable object view of an exact unsigned 64-bit integer datum.</summary>
    public sealed class UInt64Value : ScriptImmutable
    {
        /// <summary>Creates an immutable wrapper for an exact unsigned 64-bit value.</summary>
        public UInt64Value(ulong value) : base(Prototypes.ObjectPrototype)
        {
            Value = value;
        }

        /// <summary>Gets the wrapped value.</summary>
        public ulong Value { get; }

        /// <inheritdoc />
        protected internal override ScriptDatum TypeOfValue => TypeNames.UInt64;

        /// <inheritdoc />
        public override string ToString() => Value.ToString(CultureInfo.InvariantCulture);

        /// <inheritdoc />
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override bool IsTrue() => Value != 0;
    }
}
