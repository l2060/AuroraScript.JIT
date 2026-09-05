using AuroraScript.Runtime.Types;
using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace AuroraScript.Runtime
{
    /// <summary>
    /// Represents the compact dynamic value crossing AuroraScript runtime boundaries.
    /// Primitive payloads are encoded in 64 bits while managed references stay in a
    /// normal GC-tracked field.
    /// </summary>
    [DebuggerTypeProxy(typeof(Debugging.ScriptDatumDebugView))]
    [DebuggerDisplay("{DebuggerDisplayValue,nq}", Type = "{DebuggerDisplayType,nq}")]
    [StructLayout(LayoutKind.Sequential)]
    public partial struct ScriptDatum : IEquatable<ScriptDatum>
    {
        private const ulong NullPayload = 0;
        private const ulong FalsePayload = 1;
        private const ulong TruePayload = 2;
        private const ulong EncodedPositiveZero = 0x7ff8_0000_0000_0001UL;
        private const ulong EncodedSubnormalOne = 0x7ff8_0000_0000_0002UL;
        private const ulong EncodedSubnormalTwo = 0x7ff8_0000_0000_0003UL;
        private const ulong EncodedNaN = 0x7ff8_0000_0000_0004UL;
        private static readonly object s_kindMarker = new();
        private static readonly object s_int64Marker = new();
        private static readonly object s_uint64Marker = new();

        private object reference;
        private ulong payload;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private ScriptDatum(object reference, ulong payload)
        {
            this.reference = reference;
            this.payload = payload;
        }

        /// <summary>Gets the kind of value stored in this datum.</summary>
        public ValueKind Kind
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            readonly get
            {
                if (reference != null)
                {
                    if (ReferenceEquals(reference, s_int64Marker)) return ValueKind.Int64;
                    if (ReferenceEquals(reference, s_uint64Marker)) return ValueKind.UInt64;
                    return (ValueKind)(short)payload;
                }

                return payload switch
                {
                    NullPayload => ValueKind.Null,
                    FalsePayload or TruePayload => ValueKind.Boolean,
                    _ => ValueKind.Number,
                };
            }
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            set
            {
                var currentKind = Kind;
                switch (value)
                {
                    case ValueKind.Null:
                        SetNull();
                        return;
                    case ValueKind.Boolean:
                        SetBoolean(currentKind == ValueKind.Boolean && Boolean);
                        return;
                    case ValueKind.Number:
                        SetNumber(currentKind == ValueKind.Number ? Number : 0d);
                        return;
                    case ValueKind.Int64:
                        SetInt64(currentKind == ValueKind.Int64 ? Int64 : 0L);
                        return;
                    case ValueKind.UInt64:
                        SetUInt64(currentKind == ValueKind.UInt64 ? UInt64 : 0UL);
                        return;
                    case ValueKind.String:
                        SetString(reference switch
                        {
                            string text => text,
                            StringValue text => text.Value,
                            _ => string.Empty
                        });
                        return;
                    default:
                        payload = (ulong)(short)value;
                        if (reference is not ScriptObject) reference = s_kindMarker;
                        return;
                }
            }
        }

        /// <summary>Gets the double-precision numeric payload.</summary>
        public double Number
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            readonly get => DecodeNumber(payload);
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            set => SetNumber(value);
        }

        /// <summary>Gets the Boolean payload.</summary>
        public bool Boolean
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            readonly get => payload == TruePayload;
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            set => SetBoolean(value);
        }

        /// <summary>
        /// Gets the referenced script object. String primitives are materialized only
        /// when this compatibility view is explicitly requested.
        /// </summary>
        public ScriptObject Object
        {
            readonly get
            {
                if (reference is ScriptObject scriptObject)
                {
                    return scriptObject;
                }

                return reference is string text ? StringValue.Of(text) : null;
            }
            set => WriteObject(ref this, value);
        }

        /// <summary>
        /// Gets the legacy string object view. Runtime code should use the allocation-free
        /// <see cref="StringText"/> view instead.
        /// </summary>
        public StringValue String
        {
            readonly get => reference is string text ? StringValue.Of(text) : reference as StringValue;
            set => SetString(value?.Value);
        }

        /// <summary>Gets the raw CLR string without allocating a compatibility wrapper.</summary>
        internal readonly string StringText => reference as string;

        /// <summary>Gets the raw managed reference for internal kind-specialized code.</summary>
        internal readonly object Reference => reference;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static ScriptDatum CreateBoolean(bool value)
        {
            return new ScriptDatum(null, value ? TruePayload : FalsePayload);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static ScriptDatum CreateNumber(double value)
        {
            return new ScriptDatum(null, EncodeNumber(value));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static ScriptDatum CreateNumber(int value)
        {
            return new ScriptDatum(null, EncodeNumber(value));
        }

        /// <summary>Gets the exact signed 64-bit integer payload.</summary>
        public long Int64
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            readonly get => unchecked((long)payload);
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            set => SetInt64(value);
        }

        /// <summary>Gets the exact unsigned 64-bit integer payload.</summary>
        public ulong UInt64
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            readonly get => payload;
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            set => SetUInt64(value);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static ScriptDatum CreateNumber(uint value)
        {
            return new ScriptDatum(null, EncodeNumber(value));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static ScriptDatum CreateNumber(long value)
        {
            return new ScriptDatum(null, EncodeNumber(value));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static ScriptDatum CreateInt64(long value)
        {
            return new ScriptDatum(s_int64Marker, unchecked((ulong)value));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static ScriptDatum CreateUInt64(ulong value)
        {
            return new ScriptDatum(s_uint64Marker, value);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static ScriptDatum CreateString(string value)
        {
            return new ScriptDatum(value ?? string.Empty, (ulong)(short)ValueKind.String);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static ScriptDatum CreateReference(ValueKind kind, ScriptObject value)
        {
            return value == null
                ? default
                : new ScriptDatum(value, (ulong)(short)kind);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void SetNull()
        {
            reference = null;
            payload = NullPayload;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void SetBoolean(bool value)
        {
            reference = null;
            payload = value ? TruePayload : FalsePayload;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void SetNumber(double value)
        {
            reference = null;
            payload = EncodeNumber(value);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void SetInt64(long value)
        {
            reference = s_int64Marker;
            payload = unchecked((ulong)value);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void SetUInt64(ulong value)
        {
            reference = s_uint64Marker;
            payload = value;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void SetString(string value)
        {
            reference = value ?? string.Empty;
            payload = (ulong)(short)ValueKind.String;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void SetReference(ValueKind kind, ScriptObject value)
        {
            if (value == null)
            {
                SetNull();
                return;
            }

            reference = value;
            payload = (ulong)(short)kind;
        }

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        internal readonly string DebuggerDisplayValue => Debugging.ScriptDebugView.FormatValue(this);

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        internal readonly string DebuggerDisplayType => Debugging.ScriptDebugView.GetTypeName(this);

        /// <summary>Returns the script value's string representation.</summary>
        public override readonly string ToString()
        {
            return ScriptDatum.ToString(this);
        }

        /// <summary>Returns the script value's semantic hash code.</summary>
        public override readonly int GetHashCode()
        {
            return Kind switch
            {
                ValueKind.Null => ScriptObject.Null.GetHashCode(),
                ValueKind.Boolean => (Boolean ? 1d : 0d).GetHashCode(),
                ValueKind.Number => Number.GetHashCode(),
                // Cross-kind equality compares through Number. Hash through the
                // same representation; collisions between exact integers are OK.
                ValueKind.Int64 => ((double)Int64).GetHashCode(),
                ValueKind.UInt64 => ((double)UInt64).GetHashCode(),
                ValueKind.String => TryToNumber(this, out var number)
                    ? number.GetHashCode()
                    : StringText.GetHashCode(StringComparison.Ordinal),
                _ => reference.GetHashCode(),
            };
        }

        /// <summary>Compares two values using AuroraScript equality semantics.</summary>
        public readonly bool Equals(ScriptDatum other)
        {
            var a = other;
            var b = this;
            if (a.Kind == b.Kind)
            {
                return a.Kind switch
                {
                    ValueKind.Null => true,
                    ValueKind.Boolean => a.Boolean == b.Boolean,
                    ValueKind.Number => a.Number == b.Number,
                    ValueKind.Int64 => a.Int64 == b.Int64,
                    ValueKind.UInt64 => a.UInt64 == b.UInt64,
                    ValueKind.String => a.StringText == b.StringText,
                    _ => ReferenceEquals(a.reference, b.reference),
                };
            }

            if (TryToNumber(a, out var na) && TryToNumber(b, out var nb))
            {
                if (a.Kind == ValueKind.Int64 && b.Kind == ValueKind.UInt64)
                {
                    return a.Int64 >= 0 && (ulong)a.Int64 == b.UInt64;
                }
                if (a.Kind == ValueKind.UInt64 && b.Kind == ValueKind.Int64)
                {
                    return b.Int64 >= 0 && a.UInt64 == (ulong)b.Int64;
                }
                return na == nb;
            }
            return false;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static ulong EncodeNumber(double value)
        {
            if (double.IsNaN(value))
            {
                return EncodedNaN;
            }

            var bits = BitConverter.DoubleToUInt64Bits(value);
            return bits switch
            {
                NullPayload => EncodedPositiveZero,
                FalsePayload => EncodedSubnormalOne,
                TruePayload => EncodedSubnormalTwo,
                _ => bits,
            };
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static ulong EncodeNumber(int value)
        {
            return EncodeNumber((double)value);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static ulong EncodeNumber(uint value)
        {
            return EncodeNumber((double)value);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static ulong EncodeNumber(long value)
        {
            return EncodeNumber((double)value);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static double DecodeNumber(ulong encoded)
        {
            var bits = encoded switch
            {
                EncodedPositiveZero => NullPayload,
                EncodedSubnormalOne => FalsePayload,
                EncodedSubnormalTwo => TruePayload,
                EncodedNaN => BitConverter.DoubleToUInt64Bits(double.NaN),
                _ => encoded,
            };
            return BitConverter.UInt64BitsToDouble(bits);
        }
    }
}
