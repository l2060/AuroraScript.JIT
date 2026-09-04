using AuroraScript.Runtime;
using AuroraScript.Runtime.Interop;
using AuroraScript.Runtime.Types;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;


namespace AuroraScript.Runtime
{
    /// <summary>
    /// Partial implementation of <see cref="ScriptDatum"/> providing conversion and type-checking utilities.
    /// This fragment focuses on "TryGet" patterns and low-level coercions.
    /// </summary>
    public partial struct ScriptDatum
    {
        /// <summary>
        /// Attempts to get the underlying <see cref="ScriptObject"/> if it represents any object-like type.
        /// </summary>
        /// <param name="d">The datum to check.</param>
        /// <param name="value">The resulting object if successful.</param>
        /// <returns>True if the datum contains a non-null object reference.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Boolean TryGetAnyObject(in ScriptDatum d, out ScriptObject value)
        {
            value = d.Object;
            return value != null;
        }

        /// <summary>
        /// Attempts to get the underlying <see cref="ScriptObject"/> if the datum is explicitly an object.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Boolean TryGetObject(in ScriptDatum d, out ScriptObject value)
        {
            if (d.Kind == ValueKind.Object)
            {
                value = d.Object;
                return true;
            }
            value = null;
            return false;
        }



        /// <summary>
        /// Attempts to get the underlying <see cref="ScriptError"/> if the datum is explicitly an Error.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Boolean TryGetError(in ScriptDatum d, out ScriptError value)
        {
            if (d.Kind == ValueKind.Error)
            {
                value = (ScriptError)d.Object;
                return true;
            }
            value = null;
            return false;
        }

        /// <summary>
        /// Attempts to get the underlying <see cref="ScriptArray"/> if the datum is an array.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Boolean TryGetArray(in ScriptDatum d, out ScriptArray value)
        {
            if (d.Kind == ValueKind.Array)
            {
                value = (ScriptArray)d.Object;
                return true;
            }
            value = null;
            return false;
        }

        /// <summary>
        /// Attempts to get the underlying <see cref="ScriptRegex"/> if the datum is a regex.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Boolean TryGetRegex(in ScriptDatum d, out ScriptRegex value)
        {
            if (d.Kind == ValueKind.Regex)
            {
                value = (ScriptRegex)d.Object;
                return true;
            }
            value = null;
            return false;
        }

        /// <summary>
        /// Attempts to get the underlying <see cref="ScriptType"/> if the datum represents a type.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Boolean TryGetType(in ScriptDatum d, out ScriptType value)
        {
            if (d.Kind == ValueKind.Type)
            {
                value = (ScriptType)d.Object;
                return true;
            }
            value = null;
            return false;
        }

        /// <summary>
        /// Attempts to get the underlying <see cref="BondingFunction"/> if the datum is a CLR bonding function.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Boolean TryGetClrBonding(in ScriptDatum d, out BondingFunction value)
        {
            if (d.Kind == ValueKind.ClrBonding)
            {
                value = (BondingFunction)d.Object;
                return true;
            }
            value = null;
            return false;
        }

        /// <summary>
        /// Attempts to get the underlying <see cref="ClrMethodBinding"/> if the datum is a CLR method wrapper.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Boolean TryGetClrFunction(in ScriptDatum d, out ClrMethodBinding value)
        {
            if (d.Kind == ValueKind.ClrFunction)
            {
                value = (ClrMethodBinding)d.Object;
                return true;
            }
            value = null;
            return false;
        }

        /// <summary>
        /// Attempts to get the underlying <see cref="ScriptDate"/> if the datum is a date object.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Boolean TryGetDate(in ScriptDatum d, out ScriptDate value)
        {
            if (d.Kind == ValueKind.Date)
            {
                value = (ScriptDate)d.Object;
                return true;
            }
            value = null;
            return false;
        }


        /// <summary>
        /// Attempts to get the underlying <see cref="StringValue"/> if the datum is a string object.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Boolean TryGetString(in ScriptDatum d, out StringValue value)
        {
            if (d.Kind == ValueKind.String)
            {
                value = (StringValue)d.Object;
                return true;
            }
            value = null;
            return false;
        }


        /// <summary>
        /// Attempts to get the underlying <see cref="ClosureFunction"/> if the datum is a script function.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Boolean TryGetFunction(in ScriptDatum d, out ClosureFunction value)
        {
            if (d.Kind == ValueKind.Function)
            {
                value = (ClosureFunction)d.Object;
                return true;
            }
            value = null;
            return false;
        }

        /// <summary>
        /// Attempts to coerce the datum to a numeric value.
        /// Supports numbers, booleans (1/0), and numeric strings.
        /// </summary>
        /// <param name="d">The datum to convert.</param>
        /// <param name="value">The resulting double value.</param>
        /// <returns>True if the coercion was successful.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool TryToNumber(in ScriptDatum d, out double value)
        {
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
                        out value
                    );
            }
            value = double.NaN;
            return false;
        }

        /// <summary>
        /// Attempts to coerce the datum to a 64-bit integer.
        /// Supports numbers (truncated), booleans (1/0), and numeric strings.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool TryToInteger(in ScriptDatum d, out long value)
        {
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
                        out value
                    );
            }
            value = default;
            return false;
        }


        /// <summary>
        /// Creates a copy of the datum.
        /// </summary>
        /// <param name="d">The datum to clone.</param>
        /// <param name="deepth">Whether to perform a deep clone of object-like structures.</param>
        /// <returns>A new <see cref="ScriptDatum"/>.</returns>
        public static ScriptDatum Clone(in ScriptDatum d, bool deepth = false)
        {
            switch (d.Kind)
            {
                case ValueKind.Null:
                case ValueKind.Number:
                case ValueKind.Int64:
                case ValueKind.UInt64:
                case ValueKind.Boolean:
                case ValueKind.String:
                    return d;
                default:
                    if (deepth)
                    {
                        return ScriptDatum.FromObject(d.Object.DeepClone());
                    }
                    else
                    {
                        return ShallowClone(d);
                    }
            }
        }

        private static ScriptDatum ShallowClone(ScriptDatum origin)
        {
            switch (origin.Object)
            {
                case ScriptDate date:
                    return origin;
                case ClrInstanceObject clrInstance:
                    return origin;
                case ScriptRegex regex:
                    return origin;
                case ClosureFunction closure:
                    return origin;
                case ScriptType clrType:
                    return origin;
                case ClrMethodBinding clrFunc:
                    return origin;
                case BondingFunction bonding:
                    return origin;
                case ScriptArray array:
                    return ScriptDatum.FromArray(new ScriptArray(array));
                case ScriptPackedArray packedArray:
                    return ScriptDatum.FromObject(packedArray.ClonePackedArray());
                default:
                    var newObject = new ScriptObject();
                    origin.Object.CopyProperties(newObject);
                    return ScriptDatum.FromObject(newObject);
            }
        }


    }
}
