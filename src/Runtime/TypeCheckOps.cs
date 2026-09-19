using AuroraScript.Runtime.Types;
using System.Collections.Generic;
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
        /// <summary>Single-precision packed array.</summary>
        Float32Array,
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
        UInt64Array,
        /// <summary>
        /// Number constrained to an exact signed 32-bit integer.
        /// This remains a script number at runtime.
        /// </summary>
        Int32,
        /// <summary>
        /// Number constrained to an exact unsigned 32-bit integer.
        /// This remains a script number at runtime.
        /// </summary>
        UInt32,
        /// <summary>Exact signed 64-bit integer primitive.</summary>
        Int64,
        /// <summary>Exact unsigned 64-bit integer primitive.</summary>
        UInt64
    }

    /// <summary>
    /// Exact runtime assertions for native builtin types on typed parameters
    /// and <c>value as Number</c>-style checks, including structural boundaries.
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
                CheckedType.Int32 => CheckInt32(value),
                CheckedType.UInt32 => CheckUInt32(value),
                CheckedType.Int64 => CheckInt64(value),
                CheckedType.UInt64 => CheckUInt64(value),
                CheckedType.String => CheckString(value),
                CheckedType.Object => CheckObject(value),
                CheckedType.Array => CheckArray(value),
                CheckedType.Int32Array => CheckInt32Array(value),
                CheckedType.Int8Array => CheckInt8Array(value),
                CheckedType.Float32Array => CheckFloat32Array(value),
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

        /// <summary>
        /// Validates a Number whose value is an exact signed 32-bit integer.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ScriptDatum CheckInt32(ScriptDatum value)
        {
            if (value.Kind == ValueKind.Number && IsInt32(value.Number))
            {
                return value;
            }
            return Mismatch(CheckedType.Int32, value);
        }

        /// <summary>
        /// Validates a dynamic value as an exact signed 32-bit integer and
        /// returns it as System.Int32. Callers that need the integer skip the
        /// general numeric coercion, whose string and object cases cannot
        /// apply once the check has passed.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int CheckInt32Value(ScriptDatum value)
        {
            if (value.Kind == ValueKind.Number)
            {
                var number = value.Number;
                var truncated = (int)number;
                if (truncated == number && (truncated != 0 || IsPositiveZero(number)))
                {
                    return truncated;
                }
            }
            Mismatch(CheckedType.Int32, value);
            return 0;
        }

        /// <summary>
        /// Validates a Number that is already native double storage and returns
        /// it as System.Int32, without a ScriptDatum round trip.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int CheckInt32Number(double value)
        {
            var truncated = (int)value;
            if (truncated == value && (truncated != 0 || IsPositiveZero(value)))
            {
                return truncated;
            }
            return MismatchNumber(value);
        }

        /// <summary>Explicit numeric cast: truncate toward zero, rejecting non-finite or out-of-range results.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int CastInt32Number(double value)
        {
            var integer = System.Math.Truncate(value);
            if (integer >= int.MinValue && integer <= int.MaxValue)
                return (int)integer;
            throw new AuroraRuntimeException("Cannot convert number to int32: value is not finite or is out of range.");
        }

        /// <summary>Explicitly casts a numeric datum to int32, truncating toward zero.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int CastInt32Value(ScriptDatum value)
        {
            return value.Kind switch
            {
                ValueKind.Number => CastInt32Number(value.Number),
                ValueKind.Int64 => CastInt32Number(value.Int64),
                ValueKind.UInt64 => CastInt32Number(value.UInt64),
                _ => throw new AuroraRuntimeException("An int32 cast requires a numeric value.")
            };
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static int MismatchNumber(double value)
        {
            Mismatch(CheckedType.Int32, ScriptDatum.FromNumber(value));
            return 0;
        }

        /// <summary>
        /// Returns whether a Number is represented exactly by System.Int32.
        /// Negative zero is excluded because native integer storage cannot
        /// preserve its observable sign.
        /// </summary>
        /// <remarks>
        /// The round trip covers range and integrality in one conversion:
        /// .NET saturates out-of-range double to int and maps NaN to zero, so
        /// no saturated or fractional value can compare equal to its source.
        /// </remarks>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsInt32(double number)
        {
            var truncated = (int)number;
            return truncated == number &&
                (truncated != 0 || IsPositiveZero(number));
        }

        /// <summary>
        /// Validates a Number whose value is an exact unsigned 32-bit integer.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ScriptDatum CheckUInt32(ScriptDatum value)
        {
            if (value.Kind == ValueKind.Number && IsUInt32(value.Number))
            {
                return value;
            }
            return Mismatch(CheckedType.UInt32, value);
        }

        /// <summary>
        /// Validates a dynamic value as an exact unsigned 32-bit integer and
        /// returns it as System.UInt32.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static uint CheckUInt32Value(ScriptDatum value)
        {
            if (value.Kind == ValueKind.Number)
            {
                var number = value.Number;
                if (IsUInt32(number))
                {
                    return (uint)number;
                }
            }
            Mismatch(CheckedType.UInt32, value);
            return 0;
        }

        /// <summary>
        /// Validates a Number that is already native double storage and returns
        /// it as System.UInt32, without a ScriptDatum round trip.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static uint CheckUInt32Number(double value)
        {
            if (IsUInt32(value))
            {
                return (uint)value;
            }
            return MismatchUInt32Number(value);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static uint MismatchUInt32Number(double value)
        {
            Mismatch(CheckedType.UInt32, ScriptDatum.FromNumber(value));
            return 0;
        }

        /// <summary>
        /// Returns whether a Number is represented exactly by System.UInt32.
        /// Negative zero is excluded because native integer storage cannot
        /// preserve its observable sign.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsUInt32(double number)
        {
            return number >= uint.MinValue &&
                number <= uint.MaxValue &&
                number == System.Math.Truncate(number) &&
                (number != 0d || IsPositiveZero(number));
        }

        /// <summary>Validates and normalizes an exact signed 64-bit integer.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ScriptDatum CheckInt64(ScriptDatum value)
        {
            switch (value.Kind)
            {
                case ValueKind.Int64:
                    return value;
                case ValueKind.UInt64 when value.UInt64 <= long.MaxValue:
                    return ScriptDatum.FromInt64((long)value.UInt64);
                case ValueKind.Number:
                    var number = value.Number;
                    if (IsInt64(number)) return ScriptDatum.FromInt64((long)number);
                    break;
            }
            return Mismatch(CheckedType.Int64, value);
        }

        /// <summary>Returns an exact signed 64-bit integer from a dynamic value.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static long CheckInt64Value(ScriptDatum value)
        {
            switch (value.Kind)
            {
                case ValueKind.Int64:
                    return value.Int64;
                case ValueKind.UInt64 when value.UInt64 <= long.MaxValue:
                    return (long)value.UInt64;
                case ValueKind.Number:
                    var number = value.Number;
                    if (IsInt64(number)) return (long)number;
                    break;
            }
            Mismatch(CheckedType.Int64, value);
            return 0;
        }

        /// <summary>Validates a native Number and returns it as System.Int64.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static long CheckInt64Number(double value)
        {
            if (IsInt64(value)) return (long)value;
            return MismatchInt64Number(value);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static long MismatchInt64Number(double value)
        {
            Mismatch(CheckedType.Int64, ScriptDatum.FromNumber(value));
            return 0;
        }

        /// <summary>Returns whether a Number can be represented exactly as System.Int64.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsInt64(double number)
        {
            return number >= -9223372036854775808d &&
                number < 9223372036854775808d &&
                number == System.Math.Truncate(number) &&
                (number != 0d || IsPositiveZero(number));
        }

        /// <summary>Validates and normalizes an exact unsigned 64-bit integer.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ScriptDatum CheckUInt64(ScriptDatum value)
        {
            switch (value.Kind)
            {
                case ValueKind.UInt64:
                    return value;
                case ValueKind.Int64 when value.Int64 >= 0:
                    return ScriptDatum.FromUInt64((ulong)value.Int64);
                case ValueKind.Number:
                    var number = value.Number;
                    if (IsUInt64(number)) return ScriptDatum.FromUInt64((ulong)number);
                    break;
            }
            return Mismatch(CheckedType.UInt64, value);
        }

        /// <summary>Returns an exact unsigned 64-bit integer from a dynamic value.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ulong CheckUInt64Value(ScriptDatum value)
        {
            switch (value.Kind)
            {
                case ValueKind.UInt64:
                    return value.UInt64;
                case ValueKind.Int64 when value.Int64 >= 0:
                    return (ulong)value.Int64;
                case ValueKind.Number:
                    var number = value.Number;
                    if (IsUInt64(number)) return (ulong)number;
                    break;
            }
            Mismatch(CheckedType.UInt64, value);
            return 0;
        }

        /// <summary>Validates a native Number and returns it as System.UInt64.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ulong CheckUInt64Number(double value)
        {
            if (IsUInt64(value)) return (ulong)value;
            return MismatchUInt64Number(value);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static ulong MismatchUInt64Number(double value)
        {
            Mismatch(CheckedType.UInt64, ScriptDatum.FromNumber(value));
            return 0;
        }

        /// <summary>Returns whether a Number can be represented exactly as System.UInt64.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsUInt64(double number)
        {
            return number >= 0d &&
                number < 18446744073709551616d &&
                number == System.Math.Truncate(number) &&
                (number != 0d || IsPositiveZero(number));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool IsPositiveZero(double number) =>
            System.BitConverter.DoubleToInt64Bits(number) >= 0;

        /// <summary>Validates an exact String value.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ScriptDatum CheckString(ScriptDatum value) =>
            value.Kind is ValueKind.String or ValueKind.Null
                ? value
                : Mismatch(CheckedType.String, value);

        /// <summary>Validates an exact Object value.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ScriptDatum CheckObject(ScriptDatum value) =>
            value.Kind == ValueKind.Null || value.Kind == ValueKind.Object && value.Reference is ScriptObject
                ? value
                : Mismatch(CheckedType.Object, value);

        /// <summary>
        /// Converts a nullable object-backed callable argument to its native
        /// representation while preserving script null as a CLR null reference.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static T GetNullableNativeObject<T>(ScriptDatum value)
            where T : ScriptObject
        {
            if (value.Kind == ValueKind.Null)
            {
                return null;
            }
            if (value.Reference is T result)
            {
                return result;
            }
            throw new AuroraRuntimeException(
                $"Type check failed: expected {typeof(T).Name}, actual {value.Kind.ToString().ToLowerInvariant()}.");
        }

        /// <summary>Validates an exact Array value.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ScriptDatum CheckArray(ScriptDatum value) =>
            value.Kind == ValueKind.Null || value.Kind == ValueKind.Array && value.Reference is ScriptArray
                ? value
                : Mismatch(CheckedType.Array, value);

        /// <summary>Validates an exact Int32Array value.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ScriptDatum CheckInt32Array(ScriptDatum value) =>
            value.Kind == ValueKind.Null ||
            value.Reference is ScriptInt32Array
                ? value
                : Mismatch(CheckedType.Int32Array, value);

        /// <summary>Validates an exact Int8Array value.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ScriptDatum CheckInt8Array(ScriptDatum value) =>
            value.Kind == ValueKind.Null ||
            value.Reference is ScriptInt8Array
                ? value
                : Mismatch(CheckedType.Int8Array, value);

        /// <summary>Validates an exact Float32Array value.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ScriptDatum CheckFloat32Array(ScriptDatum value) =>
            value.Kind == ValueKind.Null ||
            value.Reference is ScriptFloat32Array
                ? value
                : Mismatch(CheckedType.Float32Array, value);

        /// <summary>Validates an exact Float64Array value.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ScriptDatum CheckFloat64Array(ScriptDatum value) =>
            value.Kind == ValueKind.Null ||
            value.Reference is ScriptFloat64Array
                ? value
                : Mismatch(CheckedType.Float64Array, value);

        /// <summary>Validates an exact BooleanArray value.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ScriptDatum CheckBooleanArray(ScriptDatum value) =>
            value.Kind == ValueKind.Null ||
            value.Reference is ScriptBooleanArray
                ? value
                : Mismatch(CheckedType.BooleanArray, value);

        /// <summary>Validates an exact UInt8Array value.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ScriptDatum CheckUInt8Array(ScriptDatum value) =>
            value.Kind == ValueKind.Null ||
            value.Reference is ScriptUInt8Array
                ? value
                : Mismatch(CheckedType.UInt8Array, value);

        /// <summary>Validates an exact Int16Array value.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ScriptDatum CheckInt16Array(ScriptDatum value) =>
            value.Kind == ValueKind.Null ||
            value.Reference is ScriptInt16Array
                ? value
                : Mismatch(CheckedType.Int16Array, value);

        /// <summary>Validates an exact UInt16Array value.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ScriptDatum CheckUInt16Array(ScriptDatum value) =>
            value.Kind == ValueKind.Null ||
            value.Reference is ScriptUInt16Array
                ? value
                : Mismatch(CheckedType.UInt16Array, value);

        /// <summary>Validates an exact UInt32Array value.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ScriptDatum CheckUInt32Array(ScriptDatum value) =>
            value.Kind == ValueKind.Null ||
            value.Reference is ScriptUInt32Array
                ? value
                : Mismatch(CheckedType.UInt32Array, value);

        /// <summary>Validates an exact Int64Array value.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ScriptDatum CheckInt64Array(ScriptDatum value) =>
            value.Kind == ValueKind.Null ||
            value.Reference is ScriptInt64Array
                ? value
                : Mismatch(CheckedType.Int64Array, value);

        /// <summary>Validates an exact UInt64Array value.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ScriptDatum CheckUInt64Array(ScriptDatum value) =>
            value.Kind == ValueKind.Null ||
            value.Reference is ScriptUInt64Array
                ? value
                : Mismatch(CheckedType.UInt64Array, value);

        /// <summary>Validates a nullable native object contract.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ScriptDatum CheckNativeObject<T>(ScriptDatum value) where T : ScriptObject
        {
            _ = GetNullableNativeObject<T>(value);
            return value;
        }

        /// <summary>Validates a nullable callable reference.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ScriptDatum CheckCallable(ScriptDatum value) =>
            value.Kind is ValueKind.Null or ValueKind.Function or ValueKind.ClrFunction or ValueKind.ClrBonding
                ? value : throw new AuroraRuntimeException("Type check failed: expected callable.");

        /// <summary>Validates the exact arity of a strong callable invocation.</summary>
        public static void CheckArgumentCount(int actual, int expected)
        {
            if (actual != expected)
                throw new AuroraRuntimeException($"Callable requires {expected} arguments, actual {actual}.");
        }

        /// <summary>Extracts a checked String value while preserving null.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static string GetStringValue(ScriptDatum value) => value.Kind == ValueKind.Null ? null : value.StringText;

        /// <summary>Wraps a native string while preserving null.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ScriptDatum FromNullableString(string value) => value == null ? ScriptDatum.Null : ScriptDatum.FromString(value);

        /// <summary>Formats a nullable native string for script concatenation.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static string FormatString(string value) => value ?? "null";

        /// <summary>Rejects null before calling a native string member.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static string RequireStringReceiver(string value) => value ??
            throw new AuroraRuntimeException("Cannot access a member of null.");

        /// <summary>Checks a structure receiver and detects repeated visits in recursive shapes.</summary>
        public static bool EnterStructure(ScriptDatum value, string type,
            ref HashSet<(ScriptObject, string)> visited, bool recursive)
        {
            if (value.Kind == ValueKind.Null) return false;
            if (value.Kind != ValueKind.Object)
                throw new AuroraRuntimeException($"Type check failed: expected {type}, actual {ScriptDatum.GetTypeName(value)}.");
            if (!recursive) return true;
            visited ??= new();
            return visited.Add((value.Object, type));
        }

        /// <summary>Reads a structural field and records whether validation invoked user code.</summary>
        public static ScriptDatum ReadStructuralField(ScriptDatum owner, ScriptContext context,
            string name, ref bool pure)
        {
            if (owner.Reference is ScriptObject obj && obj.GetType() == typeof(ScriptObject) &&
                obj.TryGetOwnDataProperty(name, out var value)) return value;
            pure = false;
            return ObjectOps.GetProperty(owner, context, name);
        }

        /// <summary>Reads a structural field, omitting its type check while a plain data proof is valid.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ScriptDatum GetStructuralField(ScriptContext context, ScriptDatum owner,
            string name, CheckedType expected, bool proven)
        {
            // Only an ordinary own data slot can be read without invoking user code.
            if (proven && owner.Reference is ScriptObject obj && obj.GetType() == typeof(ScriptObject) &&
                obj.TryGetOwnDataProperty(name, out var value)) return value;
            return Check(ObjectOps.GetProperty(owner, context, name), expected);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static ScriptDatum Mismatch(
            CheckedType expected,
            ScriptDatum actual)
        {
            throw new AuroraRuntimeException(
                "Type check failed: expected " +
                (expected == CheckedType.Int32
                    ? "int32"
                    : expected == CheckedType.UInt32
                        ? "uint32"
                        : expected == CheckedType.Int64
                            ? "int64"
                            : expected == CheckedType.UInt64 ? "uint64" : expected) +
                ", actual " + ScriptDatum.GetTypeName(actual) + ".");
        }
    }
}
