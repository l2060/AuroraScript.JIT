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
            set
            {
                var kind = Kind;
                if (kind == ValueKind.String && value is StringValue text)
                {
                    SetString(text.Value);
                    return;
                }

                reference = value ?? (kind is ValueKind.Null or ValueKind.Boolean or ValueKind.Number
                    ? null
                    : s_kindMarker);
                payload = (ulong)(short)kind;
            }
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
                ValueKind.Boolean => Boolean.GetHashCode(),
                ValueKind.Number => Number.GetHashCode(),
                ValueKind.String => StringText.GetHashCode(StringComparison.Ordinal),
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
                    ValueKind.String => a.StringText == b.StringText,
                    _ => ReferenceEquals(a.reference, b.reference),
                };
            }

            if (TryToNumber(a, out var na) && TryToNumber(b, out var nb))
            {
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
