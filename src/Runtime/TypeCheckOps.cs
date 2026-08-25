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

    /// <summary>Exact runtime assertions used by typed parameters and <c>value as Type</c>.</summary>
    public static class TypeCheckOps
    {
        /// <summary>
        /// Validates <paramref name="value"/> against an exact script type and
        /// returns the unchanged value.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ScriptDatum Check(ScriptDatum value, CheckedType expected)
        {
            var reference = value.Reference;
            var valid = expected switch
            {
                CheckedType.Null => value.Kind == ValueKind.Null,
                CheckedType.Boolean => value.Kind == ValueKind.Boolean,
                CheckedType.Number => value.Kind == ValueKind.Number,
                CheckedType.String => value.Kind == ValueKind.String,
                CheckedType.Object => value.Kind == ValueKind.Object &&
                    reference is ScriptObject,
                CheckedType.Array => value.Kind == ValueKind.Array &&
                    reference is ScriptArray,
                CheckedType.Int32Array => reference is ScriptInt32Array,
                CheckedType.Int8Array => reference is ScriptInt8Array,
                CheckedType.Float64Array => reference is ScriptFloat64Array,
                CheckedType.BooleanArray => reference is ScriptBooleanArray,
                CheckedType.UInt8Array => reference is ScriptUInt8Array,
                CheckedType.Int16Array => reference is ScriptInt16Array,
                CheckedType.UInt16Array => reference is ScriptUInt16Array,
                CheckedType.UInt32Array => reference is ScriptUInt32Array,
                CheckedType.Int64Array => reference is ScriptInt64Array,
                CheckedType.UInt64Array => reference is ScriptUInt64Array,
                _ => false
            };
            if (valid) return value;
            throw Mismatch(expected.ToString(), value);
        }

        private static AuroraRuntimeException Mismatch(
            string expected,
            ScriptDatum actual)
        {
            return new AuroraRuntimeException(
                "Type check failed: expected " + expected +
                ", actual " + ScriptDatum.GetTypeName(actual) + ".");
        }
    }
}
