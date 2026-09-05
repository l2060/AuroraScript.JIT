using System.Runtime.CompilerServices;
using System;
using AuroraScript.Hosting;

namespace AuroraScript.Runtime.Types
{
    /// <summary>Immutable object view of an exact unsigned 64-bit integer datum.</summary>
    [AuroraNativeType("UInt64", NativeReceiverType = typeof(ulong))]
    public sealed partial class UInt64Value : ScriptImmutable
    {
        /// <summary>Creates an immutable wrapper for an exact unsigned 64-bit value.</summary>
        public UInt64Value(ulong value) : base(Prototypes.UInt64ValuePrototype)
        {
            Value = value;
        }

        /// <summary>Gets the wrapped value.</summary>
        public ulong Value { get; }

        /// <inheritdoc />
        protected internal override ScriptDatum TypeOfValue => TypeNames.UInt64;

        /// <inheritdoc />
        public override string ToString() => FormatString(Value);

        /// <inheritdoc />
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override bool IsTrue() => Value != 0;

        /// <summary>Formats the exact integer without a Number conversion.</summary>
        [AuroraExport("toString", DynamicAdapter = nameof(TOSTRING), Target = AuroraExportTarget.Instance)]
        public static string FormatString(ulong value) => NumberValue.FormatString(value);

        /// <summary>Formats the exact integer with an integer radix.</summary>
        [AuroraExport("toString", DynamicAdapter = nameof(TOSTRING), Target = AuroraExportTarget.Instance)]
        public static string FormatString(ulong value, int radix) => NumberValue.FormatString(value, radix);

        internal new static void TOSTRING(ScriptContext ctx, ScriptObject receiver, Span<ScriptDatum> args, ref ScriptDatum result)
        {
            if (receiver is not UInt64Value integer) { ScriptDatum.MarkAsNull(ref result); return; }
            ScriptDatum.WriteAsString(ref result, args.Length == 1 && args[0].Kind == ValueKind.Number
                ? FormatString(integer.Value, (int)args[0].Number) : FormatString(integer.Value));
        }
    }
}
