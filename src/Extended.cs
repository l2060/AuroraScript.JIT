using AuroraScript.Runtime;
using AuroraScript.Runtime.Types;
using System;
using System.Globalization;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;

namespace AuroraScript
{
    /// <summary>
    /// Provides extension methods for various types within the AuroraScript runtime, 
    /// including IL generation utilities and script datum manipulation.
    /// </summary>
    public static class Extended
    {
        /// <summary>
        /// Retrieves the <see cref="RuntimeMethodHandle"/> associated with a <see cref="DynamicMethod"/>.
        /// This is achieved by generating a dynamic invoker that retreives the token via IL.
        /// </summary>
        /// <param name="dynamicMethod">The dynamic method to get the handle for.</param>
        /// <returns>The <see cref="RuntimeMethodHandle"/> for the dynamic method.</returns>
        public static RuntimeMethodHandle GetMethodHandle(this DynamicMethod dynamicMethod)
        {
            // https://github.com/dotnet/runtime/issues/118238
            DynamicMethod _method = new("", typeof(RuntimeMethodHandle), []);
            ILGenerator il = _method.GetILGenerator();
            int dynamicMethodMetadataToken = 0x6000002;

            // Emit a call to the dynamic method to force JIT compilation
            Label actualMethodBody = il.DefineLabel();
            il.Emit(OpCodes.Br_S, actualMethodBody);
            il.Emit(OpCodes.Ldc_I4_0);
            il.Emit(OpCodes.Ldc_I4_0);
            il.Emit(OpCodes.Call, dynamicMethod);
            il.Emit(OpCodes.Pop);

            // Actual method body
            il.MarkLabel(actualMethodBody);
            var local = il.DeclareLocal(typeof(RuntimeMethodHandle));
            il.Emit(OpCodes.Ldloca, local);
            il.Emit(OpCodes.Ldtoken, dynamicMethodMetadataToken);
            il.Emit(OpCodes.Stobj, typeof(RuntimeMethodHandle));
            il.Emit(OpCodes.Ldloc, local);
            il.Emit(OpCodes.Ret);

            Func<RuntimeMethodHandle> getMethodPointerAndHandle = (Func<RuntimeMethodHandle>)_method.CreateDelegate(typeof(Func<RuntimeMethodHandle>));
            return getMethodPointerAndHandle();
        }

        /// <summary>
        /// Retrieves the native method address (function pointer) associated with a <see cref="DynamicMethod"/>.
        /// </summary>
        /// <param name="dynamicMethod">The dynamic method to get the address for.</param>
        /// <returns>The native function pointer as an <see cref="nint"/>.</returns>
        public static nint GetMethodAddress(this DynamicMethod dynamicMethod)
        {
            // https://github.com/dotnet/runtime/issues/118238
            DynamicMethod _method = new("", typeof(nint), []);
            ILGenerator il = _method.GetILGenerator();
            Label actualMethodBody = il.DefineLabel();
            il.Emit(OpCodes.Br_S, actualMethodBody);
            il.Emit(OpCodes.Ldc_I4_0);
            il.Emit(OpCodes.Ldc_I4_0);
            il.Emit(OpCodes.Call, dynamicMethod);
            il.Emit(OpCodes.Pop);

            int dynamicMethodMetadataToken = 0x6000002;
            il.MarkLabel(actualMethodBody);
            il.Emit(OpCodes.Ldftn, dynamicMethodMetadataToken);
            il.Emit(OpCodes.Ret);

            Func<nint> invoker = (Func<nint>)_method.CreateDelegate(typeof(Func<nint>));
            return invoker();
        }

        /// <summary>
        /// Checks if the specified <see cref="ValueKind"/> includes the given flag.
        /// </summary>
        /// <param name="valueKind">The source value kind.</param>
        /// <param name="flag">The flag to check for.</param>
        /// <returns>True if the flag is present; otherwise, false.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool Include(this ValueKind valueKind, ValueKind flag)
        {
            return (valueKind & flag) != 0;
        }

        /// <summary>
        /// Attempts to retrieve a double-precision number from the <see cref="ScriptDatum"/> at the specified index.
        /// Supports conversion from Numbers, Booleans, and properly formatted Strings.
        /// </summary>
        /// <param name="source">The span of script data.</param>
        /// <param name="index">The index of the datum to retrieve.</param>
        /// <param name="value">The retrieved double value, or NaN if retrieval fails.</param>
        /// <returns>True if the value was successfully retrieved and converted; otherwise, false.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool TryGetNumber(this Span<ScriptDatum> source, int index, out double value)
        {
            if ((uint)index < (uint)source.Length)
            {
                ref readonly var d = ref source[index];
                switch (d.Kind)
                {
                    case ValueKind.Number:
                        value = d.Number;
                        return true;

                    case ValueKind.Int64:
                        value = d.Int64;
                        return true;

                    case ValueKind.UInt64:
                        value = d.UInt64;
                        return true;

                    case ValueKind.Boolean:
                        value = d.Boolean ? 1.0 : 0.0;
                        return true;

                    case ValueKind.String:
                        return double.TryParse(
                            d.StringText,
                            NumberStyles.AllowLeadingSign | NumberStyles.AllowDecimalPoint,
                            CultureInfo.InvariantCulture,
                            out value);
                }
            }
            value = double.NaN;
            return false;
        }

        /// <summary>
        /// Attempts to retrieve a 64-bit integer from the <see cref="ScriptDatum"/> at the specified index.
        /// Supports conversion from Numbers, Booleans, and properly formatted Strings.
        /// </summary>
        /// <param name="source">The span of script data.</param>
        /// <param name="index">The index of the datum to retrieve.</param>
        /// <param name="value">The retrieved integer value, or 0 if retrieval fails.</param>
        /// <returns>True if the value was successfully retrieved and converted; otherwise, false.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool TryGetInteger(this Span<ScriptDatum> source, int index, out long value)
        {
            if ((uint)index < (uint)source.Length)
            {
                ref readonly var d = ref source[index];
                switch (d.Kind)
                {
                    case ValueKind.Number:
                        value = (long)d.Number;
                        return true;

                    case ValueKind.Int64:
                        value = d.Int64;
                        return true;

                    case ValueKind.UInt64 when d.UInt64 <= long.MaxValue:
                        value = (long)d.UInt64;
                        return true;

                    case ValueKind.Boolean:
                        value = d.Boolean ? 1 : 0;
                        return true;

                    case ValueKind.String:
                        return long.TryParse(
                            d.StringText,
                            NumberStyles.AllowLeadingSign | NumberStyles.AllowDecimalPoint,
                            CultureInfo.InvariantCulture,
                            out value);
                }
            }
            value = 0;
            return false;
        }


        /// <summary>
        /// Attempts to retrieve a 64-bit integer from the <see cref="ScriptDatum"/> at the specified index.
        /// Supports conversion from Numbers, Booleans, and properly formatted Strings.
        /// </summary>
        /// <param name="source">The span of script data.</param>
        /// <param name="index">The index of the datum to retrieve.</param>
        /// <param name="value">The retrieved integer value, or 0 if retrieval fails.</param>
        /// <returns>True if the value was successfully retrieved and converted; otherwise, false.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool TryGetInt32(this Span<ScriptDatum> source, int index, out int value)
        {
            if ((uint)index < (uint)source.Length)
            {
                ref readonly var d = ref source[index];
                switch (d.Kind)
                {
                    case ValueKind.Number:
                        value = (int)d.Number;
                        return true;

                    case ValueKind.Int64:
                        value = (int)d.Int64;
                        return true;

                    case ValueKind.UInt64:
                        value = (int)d.UInt64;
                        return true;

                    case ValueKind.Boolean:
                        value = d.Boolean ? 1 : 0;
                        return true;

                    case ValueKind.String:
                        return int.TryParse(
                            d.StringText,
                            NumberStyles.AllowLeadingSign | NumberStyles.AllowDecimalPoint,
                            CultureInfo.InvariantCulture,
                            out value);
                }
            }
            value = 0;
            return false;
        }



        /// <summary>
        /// Attempts to retrieve a number from the <see cref="ScriptDatum"/> at the specified index strictly if it is already a Number.
        /// </summary>
        /// <param name="source">The span of script data.</param>
        /// <param name="index">The index of the datum to retrieve.</param>
        /// <param name="value">The retrieved number value, or 0 if retrieval fails.</param>
        /// <returns>True if the datum is a Number; otherwise, false.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool TryGetStrictNumber(this Span<ScriptDatum> source, int index, out double value)
        {
            if ((uint)index < (uint)source.Length)
            {
                ref readonly var d = ref source[index];
                if (d.Kind == ValueKind.Number)
                {
                    value = d.Number;
                    return true;
                }
            }
            value = 0;
            return false;
        }

        /// <summary>
        /// Attempts to retrieve a string from the <see cref="ScriptDatum"/> strictly if it is already a String.
        /// </summary>
        /// <param name="source">The span of script data.</param>
        /// <param name="index">The index of the datum to retrieve.</param>
        /// <param name="value">The retrieved string value, or empty if retrieval fails.</param>
        /// <returns>True if the datum is a String; otherwise, false.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool TryGetStrictString(this Span<ScriptDatum> source, int index, out string value)
        {
            if ((uint)index < (uint)source.Length)
            {
                ref readonly var d = ref source[index];
                if (d.Kind == ValueKind.String)
                {
                    value = d.StringText;
                    return true;
                }
            }
            value = string.Empty;
            return false;
        }

        /// <summary>
        /// Attempts to retrieve a string representation of the <see cref="ScriptDatum"/> at the specified index.
        /// Converts existing types (String, Null, Number, Boolean) into their string equivalents.
        /// </summary>
        /// <param name="source">The span of script data.</param>
        /// <param name="index">The index of the datum to retrieve.</param>
        /// <param name="value">The retrieved string value, or empty if retrieval fails.</param>
        /// <returns>True if the datum was successfully converted to a string; otherwise, false.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool TryGetString(this Span<ScriptDatum> source, int index, out string value)
        {
            if ((uint)index < (uint)source.Length)
            {
                ref readonly var d = ref source[index];
                switch (d.Kind)
                {
                    case ValueKind.String:
                        value = d.StringText;
                        return true;

                    case ValueKind.Null:
                        value = "null";
                        return true;

                    case ValueKind.Number:
                        value = d.Number.ToString(CultureInfo.InvariantCulture);
                        return true;

                    case ValueKind.Int64:
                        value = d.Int64.ToString(CultureInfo.InvariantCulture);
                        return true;

                    case ValueKind.UInt64:
                        value = d.UInt64.ToString(CultureInfo.InvariantCulture);
                        return true;

                    case ValueKind.Boolean:
                        value = d.Boolean ? "true" : "false";
                        return true;
                }
            }
            value = string.Empty;
            return false;
        }

        /// <summary>
        /// Attempts to retrieve a <see cref="ScriptObject"/> from the <see cref="ScriptDatum"/> at the specified index.
        /// </summary>
        /// <param name="source">The span of script data.</param>
        /// <param name="index">The index of the datum to retrieve.</param>
        /// <param name="value">The retrieved script object, or null if retrieval fails.</param>
        /// <returns>True if the datum kind is Object or higher; otherwise, false.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool TryGetObject(this Span<ScriptDatum> source, int index, out ScriptObject value)
        {
            if ((uint)index < (uint)source.Length)
            {
                ref readonly var d = ref source[index];
                if (d.Kind.Include(ValueKind.Object))
                {
                    value = d.Object;
                    return true;
                }
            }
            value = null;
            return false;
        }

        /// <summary>
        /// Attempts to retrieve a <see cref="ScriptEnumerator"/> from the script object at the specified index.
        /// </summary>
        /// <param name="source">The span of script data.</param>
        /// <param name="index">The index of the datum to retrieve.</param>
        /// <param name="value">The retrieved enumerator, or null if retrieval fails.</param>
        /// <returns>True if the object exists and supports enumeration; otherwise, false.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool TryGetEnumerator(this Span<ScriptDatum> source, int index, out ScriptEnumerator value)
        {
            if ((uint)index < (uint)source.Length)
            {
                ref readonly var d = ref source[index];
                if (d.Object != null)
                {
                    value = d.Object.GetEnumerator();
                    return true;
                }
            }
            value = null;
            return false;
        }

        /// <summary>
        /// Returns a read-only reference to the <see cref="ScriptDatum"/> at the specified index without safety checks.
        /// </summary>
        /// <param name="source">The span of script data.</param>
        /// <param name="index">The index to look up.</param>
        /// <returns>A read-only reference to the datum.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ref readonly ScriptDatum GetRefUnchecked(this Span<ScriptDatum> source, int index)
        {
            return ref source[index];
        }

        /// <summary>
        /// Attempts to retrieve a copy of the <see cref="ScriptDatum"/> by reference from the source span.
        /// </summary>
        /// <param name="source">The span of script data.</param>
        /// <param name="index">The index of the datum to retrieve.</param>
        /// <param name="value">The output datum variable.</param>
        /// <returns>True if the index is within bounds; otherwise, false.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool TryGetRef(this Span<ScriptDatum> source, int index, ref ScriptDatum value)
        {
            if ((uint)index < (uint)source.Length)
            {
                value = source[index];
                return true;
            }
            return false;
        }

        /// <summary>
        /// Attempts to retrieve a <see cref="ScriptRegex"/> from the <see cref="ScriptDatum"/> at the specified index.
        /// </summary>
        /// <param name="source">The span of script data.</param>
        /// <param name="index">The index of the datum to retrieve.</param>
        /// <param name="value">The retrieved script regex, or null if retrieval fails.</param>
        /// <returns>True if the datum is a Regex kind and the underlying object is a regex; otherwise, false.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool TryGetRegex(this Span<ScriptDatum> source, int index, out ScriptRegex value)
        {
            if ((uint)index < (uint)source.Length)
            {
                ref readonly var d = ref source[index];
                if (d.Kind == ValueKind.Regex)
                {
                    value = d.Object as ScriptRegex;
                    return value != null;
                }
            }
            value = default;
            return false;
        }

        /// <summary>
        /// Attempts to retrieve a <see cref="ClosureFunction"/> from the <see cref="ScriptDatum"/> at the specified index.
        /// </summary>
        /// <param name="source">The span of script data.</param>
        /// <param name="index">The index of the datum to retrieve.</param>
        /// <param name="value">The retrieved closure function, or null if retrieval fails.</param>
        /// <returns>True if the datum is a Function kind and the underlying object is a closure; otherwise, false.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool TryGetFunction(this Span<ScriptDatum> source, int index, out ClosureFunction value)
        {
            if ((uint)index < (uint)source.Length)
            {
                ref readonly var d = ref source[index];
                if (d.Kind == ValueKind.Function)
                {
                    value = d.Object as ClosureFunction;
                    return value != null;
                }
            }
            value = null;
            return false;
        }

        /// <summary>
        /// Attempts to retrieve a boolean value from the <see cref="ScriptDatum"/> at the specified index.
        /// Implements truthy/falsy logic for Numbers, Strings, and Objects.
        /// </summary>
        /// <param name="source">The span of script data.</param>
        /// <param name="index">The index of the datum to retrieve.</param>
        /// <param name="value">The retrieved boolean value, or false if retrieval fails.</param>
        /// <returns>True if the datum was successfully evaluated as a boolean; otherwise, false.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool TryGetBoolean(this Span<ScriptDatum> source, int index, out bool value)
        {
            if ((uint)index < (uint)source.Length)
            {
                ref readonly var d = ref source[index];
                switch (d.Kind)
                {
                    case ValueKind.Boolean:
                        value = d.Boolean;
                        return true;

                    case ValueKind.Number:
                        value = d.Number != 0;
                        return true;

                    case ValueKind.Int64:
                        value = d.Int64 != 0;
                        return true;

                    case ValueKind.UInt64:
                        value = d.UInt64 != 0;
                        return true;

                    case ValueKind.String:
                        value = d.StringText.Length != 0;
                        return true;

                    default:
                        if (d.Object != null)
                        {
                            value = d.Object != ScriptObject.Null;
                            return true;
                        }
                        break;
                }
            }
            value = false;
            return false;
        }
    }
}
