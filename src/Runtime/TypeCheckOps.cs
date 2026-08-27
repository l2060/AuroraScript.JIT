using AuroraScript.Runtime.Types;
using System.Runtime.CompilerServices;

namespace AuroraScript.Runtime
{
    /// <summary>Exact value types supported by script type assertions.</summary>
    public enum CheckedType : byte
    {
        /// <summary>Null value.</summary>
        Null,
        /// <summary>Boolean primitive.</summary>
        Boolean,
        /// <summary>Number primitive.</summary>
        Number,
        /// <summary>String primitive.</summary>
        String,
        /// <summary>Plain script object.</summary>
        Object,
        /// <summary>Script array.</summary>
        Array,
        /// <summary>Signed 32-bit packed array.</summary>
        Int32Array,
        /// <summary>Signed 8-bit packed array.</summary>
        Int8Array,
        /// <summary>Double-precision packed array.</summary>
        Float64Array,
        /// <summary>Boolean packed array.</summary>
        BooleanArray,
        /// <summary>Unsigned 8-bit packed array.</summary>
        UInt8Array,
        /// <summary>Signed 16-bit packed array.</summary>
        Int16Array,
        /// <summary>Unsigned 16-bit packed array.</summary>
        UInt16Array,
        /// <summary>Unsigned 32-bit packed array.</summary>
        UInt32Array,
        /// <summary>Signed 64-bit packed array.</summary>
        Int64Array,
        /// <summary>Unsigned 64-bit packed array.</summary>
        UInt64Array
    }

    /// <summary>
    /// Exact runtime assertions for native builtin types on typed parameters
    /// and <c>value as Number</c>-style checks. Custom <c>type</c> names are
    /// compile-time grants and never reach this helper.
    /// </summary>
    public static class TypeCheckOps
    {
        /// <summary>
        /// Validates <paramref name="value"/> against an exact script type and
        /// returns the unchanged value.
        /// </summary>
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static ScriptDatum Check(ScriptDatum value, CheckedType expected)
        {
            return expected switch
            {
                CheckedType.Null => CheckNull(value),
                CheckedType.Boolean => CheckBoolean(value),
                CheckedType.Number => CheckNumber(value),
                CheckedType.String => CheckString(value),
                CheckedType.Object => CheckObject(value),
                CheckedType.Array => CheckArray(value),
                CheckedType.Int32Array => CheckInt32Array(value),
                CheckedType.Int8Array => CheckInt8Array(value),
                CheckedType.Float64Array => CheckFloat64Array(value),
                CheckedType.BooleanArray => CheckBooleanArray(value),
                CheckedType.UInt8Array => CheckUInt8Array(value),
                CheckedType.Int16Array => CheckInt16Array(value),
                CheckedType.UInt16Array => CheckUInt16Array(value),
                CheckedType.UInt32Array => CheckUInt32Array(value),
                CheckedType.Int64Array => CheckInt64Array(value),
                CheckedType.UInt64Array => CheckUInt64Array(value),
                _ => Mismatch(expected, value)
            };
        }

        /// <summary>Validates an exact null value.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ScriptDatum CheckNull(ScriptDatum value) =>
            value.Kind == ValueKind.Null
                ? value
                : Mismatch(CheckedType.Null, value);

        /// <summary>Validates an exact Boolean value.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ScriptDatum CheckBoolean(ScriptDatum value) =>
            value.Kind == ValueKind.Boolean
                ? value
                : Mismatch(CheckedType.Boolean, value);

        /// <summary>Validates an exact Number value.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ScriptDatum CheckNumber(ScriptDatum value) =>
            value.Kind == ValueKind.Number
                ? value
                : Mismatch(CheckedType.Number, value);

        /// <summary>Validates an exact String value.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ScriptDatum CheckString(ScriptDatum value) =>
            value.Kind == ValueKind.String
                ? value
                : Mismatch(CheckedType.String, value);

        /// <summary>Validates an exact Object value.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ScriptDatum CheckObject(ScriptDatum value) =>
            value.Kind == ValueKind.Object && value.Reference is ScriptObject
                ? value
                : Mismatch(CheckedType.Object, value);

        /// <summary>Validates an exact Array value.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ScriptDatum CheckArray(ScriptDatum value) =>
            value.Kind == ValueKind.Array && value.Reference is ScriptArray
                ? value
                : Mismatch(CheckedType.Array, value);

        /// <summary>Validates an exact Int32Array value.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ScriptDatum CheckInt32Array(ScriptDatum value) =>
            value.Reference is ScriptInt32Array
                ? value
                : Mismatch(CheckedType.Int32Array, value);

        /// <summary>Validates an exact Int8Array value.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ScriptDatum CheckInt8Array(ScriptDatum value) =>
            value.Reference is ScriptInt8Array
                ? value
                : Mismatch(CheckedType.Int8Array, value);

        /// <summary>Validates an exact Float64Array value.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ScriptDatum CheckFloat64Array(ScriptDatum value) =>
            value.Reference is ScriptFloat64Array
                ? value
                : Mismatch(CheckedType.Float64Array, value);

        /// <summary>Validates an exact BooleanArray value.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ScriptDatum CheckBooleanArray(ScriptDatum value) =>
            value.Reference is ScriptBooleanArray
                ? value
                : Mismatch(CheckedType.BooleanArray, value);

        /// <summary>Validates an exact UInt8Array value.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ScriptDatum CheckUInt8Array(ScriptDatum value) =>
            value.Reference is ScriptUInt8Array
                ? value
                : Mismatch(CheckedType.UInt8Array, value);

        /// <summary>Validates an exact Int16Array value.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ScriptDatum CheckInt16Array(ScriptDatum value) =>
            value.Reference is ScriptInt16Array
                ? value
                : Mismatch(CheckedType.Int16Array, value);

        /// <summary>Validates an exact UInt16Array value.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ScriptDatum CheckUInt16Array(ScriptDatum value) =>
            value.Reference is ScriptUInt16Array
                ? value
                : Mismatch(CheckedType.UInt16Array, value);

        /// <summary>Validates an exact UInt32Array value.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ScriptDatum CheckUInt32Array(ScriptDatum value) =>
            value.Reference is ScriptUInt32Array
                ? value
                : Mismatch(CheckedType.UInt32Array, value);

        /// <summary>Validates an exact Int64Array value.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ScriptDatum CheckInt64Array(ScriptDatum value) =>
            value.Reference is ScriptInt64Array
                ? value
                : Mismatch(CheckedType.Int64Array, value);

        /// <summary>Validates an exact UInt64Array value.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ScriptDatum CheckUInt64Array(ScriptDatum value) =>
            value.Reference is ScriptUInt64Array
                ? value
                : Mismatch(CheckedType.UInt64Array, value);

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static ScriptDatum Mismatch(
            CheckedType expected,
            ScriptDatum actual)
        {
            throw new AuroraRuntimeException(
                "Type check failed: expected " + expected +
                ", actual " + ScriptDatum.GetTypeName(actual) + ".");
        }
    }
}
