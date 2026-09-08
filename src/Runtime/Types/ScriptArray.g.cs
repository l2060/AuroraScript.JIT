using System;
using System.Collections.Generic;

namespace AuroraScript.Runtime.Types
{
    /// <summary>
    /// Partial class for <see cref="ScriptArray"/> containing native method implementations for the script runtime.
    /// Includes common JS-like methods: push, pop, reverse, shift, unshift, concat, sort, join, and slice.
    /// </summary>
    public partial class ScriptArray
    {
        private static readonly IComparer<ScriptDatum> CompareDatumForSort = new DefaultComparer();

        /// <summary> Native implementation for the 'push' method. Appends one or more items. </summary>
        internal static void PUSH(ScriptContext ctx, ScriptObject thisObject, Span<ScriptDatum> args, ref ScriptDatum result)
        {
            if (thisObject is ScriptArray array)
                ScriptDatum.WriteAsNumber(ref result, array.AddRange(args));
        }
        private static bool TryGetLength(ScriptDatum value, out int length)
        {
            switch (value.Kind)
            {
                case ValueKind.Number:
                    var number = value.Number;
                    if (double.IsFinite(number) && number >= 0d && number <= Array.MaxLength &&
                        number == Math.Truncate(number))
                    {
                        length = (int)number;
                        return true;
                    }
                    break;
                case ValueKind.Int64:
                    if (value.Int64 >= 0 && value.Int64 <= Array.MaxLength)
                    {
                        length = (int)value.Int64;
                        return true;
                    }
                    break;
                case ValueKind.UInt64:
                    if (value.UInt64 <= (ulong)Array.MaxLength)
                    {
                        length = (int)value.UInt64;
                        return true;
                    }
                    break;
            }
            length = 0;
            return false;
        }

        /// <summary> Native implementation for the 'has' method. Checks if the array contains an element. </summary>
        internal static void HAS(ScriptContext ctx, ScriptObject thisObject, Span<ScriptDatum> args, ref ScriptDatum result)
        {
            if (thisObject is ScriptArray array && args.Length > 0)
                ScriptDatum.WriteAsBoolean(ref result, array.HasCore(args[0]));
        }

        /// <summary> Native implementation for the 'indexOf' method. Returns the first index of an element. </summary>
        internal static void INDEXOF(ScriptContext ctx, ScriptObject thisObject, Span<ScriptDatum> args, ref ScriptDatum result)
        {
            if (thisObject is ScriptArray array && args.Length > 0)
                ScriptDatum.WriteAsNumber(ref result, array.IndexOfCore(args[0], args.Length > 1 ? args[1] : default));
        }

        /// <summary> Native implementation for the 'lastIndexOf' method. Returns the last index of an element. </summary>
        internal static void LASTINDEXOF(ScriptContext ctx, ScriptObject thisObject, Span<ScriptDatum> args, ref ScriptDatum result)
        {
            if (thisObject is ScriptArray array && args.Length > 0)
                ScriptDatum.WriteAsNumber(ref result, array.LastIndexOfCore(args[0], args.Length > 1 ? args[1] : default));
        }

        /// <summary> Native implementation for the 'find' method. Returns the value of the first element in the array that satisfies the provided testing function. </summary>
        internal static void FIND(ScriptContext ctx, ScriptObject thisObject, Span<ScriptDatum> args, ref ScriptDatum result)
        {
            if (thisObject is ScriptArray array && args.Length > 0)
                result = array.FindCore(ctx, args[0]);
        }

        /// <summary> Native implementation for the 'findIndex' method. Returns the index of the first element in the array that satisfies the provided testing function. </summary>
        internal static void FINDINDEX(ScriptContext ctx, ScriptObject thisObject, Span<ScriptDatum> args, ref ScriptDatum result)
        {
            if (thisObject is ScriptArray array && args.Length > 0)
                result = array.FindIndexCore(ctx, args[0]);
        }

        /// <summary> Native implementation for the 'findLast' method. Returns the value of the last element in the array that satisfies the provided testing function. </summary>
        internal static void FINDLAST(ScriptContext ctx, ScriptObject thisObject, Span<ScriptDatum> args, ref ScriptDatum result)
        {
            if (thisObject is ScriptArray array && args.Length > 0)
                result = array.FindLastCore(ctx, args[0]);
        }

        /// <summary> Native implementation for the 'findLastIndex' method. Returns the index of the last element in the array that satisfies the provided testing function. </summary>
        internal static void FINDLASTINDEX(ScriptContext ctx, ScriptObject thisObject, Span<ScriptDatum> args, ref ScriptDatum result)
        {
            if (thisObject is ScriptArray array && args.Length > 0)
                result = array.FindLastIndexCore(ctx, args[0]);
        }

        /// <summary> Native implementation for the 'map' method. Creates a new array with the results of calling a function on every element. </summary>
        internal static void MAP(ScriptContext ctx, ScriptObject thisObject, Span<ScriptDatum> args, ref ScriptDatum result)
        {
            if (thisObject is ScriptArray array && args.Length > 0)
                result = ScriptDatum.FromObject(array.MapCore(ctx, args[0]));
        }

        /// <summary> Native implementation for the 'filter' method. Creates a new array with all elements that pass the test implemented by the provided function. </summary>
        internal static void FILTER(ScriptContext ctx, ScriptObject thisObject, Span<ScriptDatum> args, ref ScriptDatum result)
        {
            if (thisObject is ScriptArray array && args.Length > 0)
                result = ScriptDatum.FromObject(array.FilterCore(ctx, args[0]));
        }

        /// <summary> Native implementation for the 'some' method. Tests whether at least one element in the array passes the test implemented by the provided function. </summary>
        internal static void SOME(ScriptContext ctx, ScriptObject thisObject, Span<ScriptDatum> args, ref ScriptDatum result)
        {
            if (thisObject is ScriptArray array && args.Length > 0)
                result = array.SomeCore(ctx, args[0]);
        }

        /// <summary> Native implementation for the 'every' method. Tests whether all elements in the array pass the test implemented by the provided function. </summary>
        internal static void EVERY(ScriptContext ctx, ScriptObject thisObject, Span<ScriptDatum> args, ref ScriptDatum result)
        {
            if (thisObject is ScriptArray array && args.Length > 0)
                result = array.EveryCore(ctx, args[0]);
        }

        /// <summary> Native implementation for the 'flat' method. Creates a new array with all sub-array elements concatenated into it recursively up to the specified depth. </summary>
        internal static void FLAT(ScriptContext ctx, ScriptObject thisObject, Span<ScriptDatum> args, ref ScriptDatum result)
        {
            if (thisObject is ScriptArray array)
                ScriptDatum.WriteAsArray(ref result, array.FlatCore(args.Length > 0 ? args[0] : default));
        }

        /// <summary> Native implementation for the 'reduce' method. Executes a reducer function on each element of the array, resulting in a single output value. </summary>
        internal static void REDUCE(ScriptContext ctx, ScriptObject thisObject, Span<ScriptDatum> args, ref ScriptDatum result)
        {
            if (thisObject is ScriptArray array && args.Length > 0)
                result = array.ReduceCore(ctx, args[0]);
        }

        /// <summary> Native implementation for the 'pop' method. Removes and returns the last element. </summary>
        internal static void POP(ScriptContext ctx, ScriptObject thisObject, Span<ScriptDatum> args, ref ScriptDatum result)
        {
            if (thisObject is not ScriptArray array)
                throw new AuroraRuntimeException("Object is not an array.");
            result = array.PopCore();
        }

        /// <summary> Native implementation for the 'reverse' method. Reverses the array in-place. </summary>
        internal static void REVERSE(ScriptContext ctx, ScriptObject thisObject, Span<ScriptDatum> args, ref ScriptDatum result)
        {
            if (thisObject is ScriptArray array)
                ScriptDatum.WriteAsArray(ref result, array.ReverseCore());
        }

        /// <summary> Native implementation for the 'unshift' method. Prepends items to the start of the array. </summary>
        internal static void UNSHIFT(ScriptContext ctx, ScriptObject thisObject, Span<ScriptDatum> args, ref ScriptDatum result)
        {
            if (thisObject is ScriptArray array)
                ScriptDatum.WriteAsNumber(ref result, array.UnshiftValues(args));
        }

        /// <summary> Native implementation for the 'shift' method. Removes and returns the first element. </summary>
        internal static void SHIFT(ScriptContext ctx, ScriptObject thisObject, Span<ScriptDatum> args, ref ScriptDatum result)
        {
            if (thisObject is ScriptArray array) result = array.ShiftCore();
        }

        /// <summary> Native implementation for the 'concat' method. Returns a new array containing concatenated elements. </summary>
        internal static void CONCAT(ScriptContext ctx, ScriptObject thisObject, Span<ScriptDatum> args, ref ScriptDatum result)
        {
            if (thisObject is ScriptArray array)
                ScriptDatum.WriteAsArray(ref result, array.ConcatValues(args));
        }

        /// <summary> Native implementation for the 'sort' method. Sorts the array in-place. </summary>
        internal static void SORT(ScriptContext ctx, ScriptObject thisObject, Span<ScriptDatum> args, ref ScriptDatum result)
        {
            if (thisObject is ScriptArray array)
                ScriptDatum.WriteAsArray(ref result, array.SortCore());
        }

        /// <summary> Native implementation for the 'join' method. Concatenates elements into a string using a separator. </summary>
        internal static void JOIN(ScriptContext ctx, ScriptObject thisObject, Span<ScriptDatum> args, ref ScriptDatum result)
        {
            ScriptDatum.WriteAsString(ref result, thisObject is ScriptArray array
                ? args.Length == 0 ? array.JoinCore() : array.JoinCore(args[0]) : string.Empty);
        }

        /// <summary> Native implementation for the 'slice' method. Returns a shallow copy of a portion of the array. </summary>
        public static void SLICE(ScriptContext ctx, ScriptObject thisObject, Span<ScriptDatum> args, ref ScriptDatum result)
        {
            if (thisObject is not ScriptArray array)
            {
                ScriptDatum.MarkAsNull(ref result);
                return;
            }
            result = args.Length == 0 ? ScriptDatum.FromArray(array.SliceCore())
                : array.SliceCore(args[0], args.Length > 1 ? args[1] : default);
        }

    }

    /// <summary> Default comparer for sorting script data. </summary>
    class DefaultComparer : IComparer<ScriptDatum>
    {
        public int Compare(ScriptDatum left, ScriptDatum right)
        {
            if (left.Kind == ValueKind.Number && right.Kind == ValueKind.Number)
            {
                return left.Number.CompareTo(right.Number);
            }
            var leftString = CoerceScriptValueToString(in left);
            var rightString = CoerceScriptValueToString(in right);
            return string.CompareOrdinal(leftString, rightString);
        }

        private static string CoerceScriptValueToString(in ScriptDatum value)
        {
            if (value.Kind == ValueKind.Null)
            {
                return string.Empty;
            }
            return ScriptDatum.ToString(value);
        }
    }
}
