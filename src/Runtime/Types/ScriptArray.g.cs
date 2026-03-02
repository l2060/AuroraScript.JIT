using AuroraScript.Core;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AuroraScript.Runtime.Types
{
    /// <summary>
    /// Partial class for <see cref="ScriptArray"/> containing native method implementations for the script runtime.
    /// Includes common JS-like methods: push, pop, reverse, shift, unshift, concat, sort, join, and slice.
    /// </summary>
    public partial class ScriptArray
    {
        private static readonly IComparer<ScriptDatum> CompareDatumForSort = new DefaultComparer();

        /// <summary> Native implementation for the 'length' property. </summary>
        internal new static void LENGTH(ScriptObject thisObject, ref ScriptDatum result)
        {
            if (thisObject is ScriptArray array)
            {
                ScriptDatum.WriteAsNumber(ref result, array.Length);
            }
        }

        /// <summary> Native implementation for the 'push' method. Appends one or more items. </summary>
        internal static void PUSH(ScriptContext ctx, ScriptObject thisObject, Span<ScriptDatum> args, ref ScriptDatum result)
        {
            if (thisObject is ScriptArray array && args != null)
            {
                var len = args.Length;
                for (int i = 0; i < len; i++)
                {
                    array.Push(args[i]);
                }
            }
        }


        /// <summary> Native implementation for the 'has' method. Checks if the array contains an element. </summary>
        internal static void HAS(ScriptContext ctx, ScriptObject thisObject, Span<ScriptDatum> args, ref ScriptDatum result)
        {
            var array = thisObject as ScriptArray;
            if (args != null && args.Length > 0)
            {
                ScriptDatum.WriteAsBoolean(ref result, array.Has(args[0]));
            }
        }

        /// <summary> Native implementation for the 'indexOf' method. Returns the first index of an element. </summary>
        internal static void INDEXOF(ScriptContext ctx, ScriptObject thisObject, Span<ScriptDatum> args, ref ScriptDatum result)
        {
            var array = thisObject as ScriptArray;
            ScriptDatum datum = default;
            int? fromIndex = null;
            if (args.TryGetInt32(1, out var fi)) fromIndex = fi;
            if (args.TryGetRef(0, ref datum))
            {
                ScriptDatum.WriteAsNumber(ref result, array.IndexOf(datum, fromIndex));
            }
        }

        /// <summary> Native implementation for the 'lastIndexOf' method. Returns the last index of an element. </summary>
        internal static void LASTINDEXOF(ScriptContext ctx, ScriptObject thisObject, Span<ScriptDatum> args, ref ScriptDatum result)
        {
            var array = thisObject as ScriptArray;
            ScriptDatum datum = default;
            int? fromIndex = null;
            if (args.TryGetInt32(1, out var fi)) fromIndex = fi;
            if (args.TryGetRef(0, ref datum))
            {
                ScriptDatum.WriteAsNumber(ref result, array.LastIndexOf(datum, fromIndex));
            }
        }


        /// <summary> Native implementation for the 'map' method. Creates a new array with the results of calling a function on every element. </summary>
        internal static void MAP(ScriptContext ctx, ScriptObject thisObject, Span<ScriptDatum> args, ref ScriptDatum result)
        {
            var array = thisObject as ScriptArray;
            if (args.TryGetFunction(0, out var callback))
            {
                var newArray = array.MapInternal(ctx, callback);
                ScriptDatum.WriteAsArray(ref result, newArray);
            }
        }

        /// <summary> Native implementation for the 'filter' method. Creates a new array with all elements that pass the test implemented by the provided function. </summary>
        internal static void FILTER(ScriptContext ctx, ScriptObject thisObject, Span<ScriptDatum> args, ref ScriptDatum result)
        {
            var array = thisObject as ScriptArray;
            if (args.TryGetFunction(0, out var callback))
            {
                var newArray = array.FilterInternal(ctx, callback);
                ScriptDatum.WriteAsArray(ref result, newArray);
            }
        }

        /// <summary> Native implementation for the 'some' method. Tests whether at least one element in the array passes the test implemented by the provided function. </summary>
        internal static void SOME(ScriptContext ctx, ScriptObject thisObject, Span<ScriptDatum> args, ref ScriptDatum result)
        {
            var array = thisObject as ScriptArray;
            if (args.TryGetFunction(0, out var callback))
            {
                var isOk = array.SomeInternal(ctx, callback);
                ScriptDatum.WriteAsBoolean(ref result, isOk);
            }
        }

        /// <summary> Native implementation for the 'every' method. Tests whether all elements in the array pass the test implemented by the provided function. </summary>
        internal static void EVERY(ScriptContext ctx, ScriptObject thisObject, Span<ScriptDatum> args, ref ScriptDatum result)
        {
            var array = thisObject as ScriptArray;
            if (args.TryGetFunction(0, out var callback))
            {
                var isOk = array.EveryInternal(ctx, callback);
                ScriptDatum.WriteAsBoolean(ref result, isOk);
            }
        }

        /// <summary> Native implementation for the 'flat' method. Creates a new array with all sub-array elements concatenated into it recursively up to the specified depth. </summary>
        internal static void FLAT(ScriptContext ctx, ScriptObject thisObject, Span<ScriptDatum> args, ref ScriptDatum result)
        {
            var array = thisObject as ScriptArray;
            if (!args.TryGetInt32(0, out var maxDeep)) maxDeep = 1;
            var newArray = array.FlatInternal(maxDeep);
            ScriptDatum.WriteAsArray(ref result, newArray);
        }


        /// <summary> Native implementation for the 'reduce' method. Executes a reducer function on each element of the array, resulting in a single output value. </summary>
        internal static void REDUCE(ScriptContext ctx, ScriptObject thisObject, Span<ScriptDatum> args, ref ScriptDatum result)
        {
            var array = thisObject as ScriptArray;
            if (args.TryGetFunction(0, out var callback))
            {
                result = array.ReduceInternal(ctx, callback);
            }
        }



        /// <summary> Native implementation for the 'pop' method. Removes and returns the last element. </summary>
        internal static void POP(ScriptContext ctx, ScriptObject thisObject, Span<ScriptDatum> args, ref ScriptDatum result)
        {
            if (thisObject is ScriptArray array)
            {
                if (array._count == 0)
                {
                    ScriptDatum.MarkAsNull(ref result);
                    return;
                }
                array.PopTo(ref result);
                return;
            }
            throw new AuroraRuntimeException("Object is not an array.");
        }

        /// <summary> Native implementation for the 'reverse' method. Reverses the array in-place. </summary>
        internal static void REVERSE(ScriptContext ctx, ScriptObject thisObject, Span<ScriptDatum> args, ref ScriptDatum result)
        {
            if (thisObject is ScriptArray array)
            {
                var count = array._count;
                var items = array._items;
                if (items != null && count > 1)
                {
                    for (int left = 0, right = count - 1; left < right; left++, right--)
                    {
                        (items[left], items[right]) = (items[right], items[left]);
                    }
                }
                ScriptDatum.WriteAsArray(ref result, array);
            }
        }

        /// <summary> Native implementation for the 'unshift' method. Prepends items to the start of the array. </summary>
        internal static void UNSHIFT(ScriptContext ctx, ScriptObject thisObject, Span<ScriptDatum> args, ref ScriptDatum result)
        {
            if (thisObject is ScriptArray array)
            {
                if (args == null || args.Length == 0)
                {
                    ScriptDatum.WriteAsNumber(ref result, array._count);
                    return;
                }

                var insertCount = args.Length;
                array.EnsureCapacity(array._count + insertCount);
                for (int i = array._count - 1; i >= 0; i--)
                {
                    array._items[i + insertCount] = array._items[i];
                }
                for (int i = 0; i < insertCount; i++)
                {
                    array._items[i] = args[i];
                }
                array._count += insertCount;
                ScriptDatum.WriteAsNumber(ref result, array._count);
                return;
            }
            ScriptDatum.WriteAsNumber(ref result, 0);
        }

        /// <summary> Native implementation for the 'shift' method. Removes and returns the first element. </summary>
        internal static void SHIFT(ScriptContext ctx, ScriptObject thisObject, Span<ScriptDatum> args, ref ScriptDatum result)
        {
            if (thisObject is ScriptArray array)
            {
                if (array._count == 0) return;
                var first = array._items[0];
                for (int i = 1; i < array._count; i++)
                {
                    array._items[i - 1] = array._items[i];
                }
                array._count--;
                ScriptDatum.MarkAsNull(ref array._items[array._count]);
                result = first;
            }
        }

        /// <summary> Native implementation for the 'concat' method. Returns a new array containing concatenated elements. </summary>
        internal static void CONCAT(ScriptContext ctx, ScriptObject thisObject, Span<ScriptDatum> args, ref ScriptDatum result)
        {
            if (thisObject is ScriptArray array)
            {
                var newArray = new ScriptArray(array._count);
                AppendArrayContents(newArray, array);
                if (args != null)
                {
                    foreach (var arg in args)
                    {
                        if (ScriptDatum.TryGetArray(in arg, out var scriptArray))
                        {
                            AppendArrayContents(newArray, scriptArray);
                        }
                        else
                        {
                            newArray.Push(arg);
                        }
                    }
                }
                ScriptDatum.WriteAsArray(ref result, newArray);
            }
        }

        /// <summary> Native implementation for the 'sort' method. Sorts the array in-place. </summary>
        internal static void SORT(ScriptContext ctx, ScriptObject thisObject, Span<ScriptDatum> args, ref ScriptDatum result)
        {
            if (thisObject is not ScriptArray array)
            {
                ScriptDatum.MarkAsNull(ref result);
                return;
            }
            var count = array._count;
            if (count > 0)
            {
                Array.Sort(array._items, 0, count, CompareDatumForSort);
            }
            ScriptDatum.WriteAsArray(ref result, array);
        }

        /// <summary> Native implementation for the 'join' method. Concatenates elements into a string using a separator. </summary>
        internal static void JOIN(ScriptContext ctx, ScriptObject thisObject, Span<ScriptDatum> args, ref ScriptDatum result)
        {
            if (thisObject is ScriptArray array)
            {
                if (array.Length == 0)
                {
                    ScriptDatum.WriteAsString(ref result, string.Empty);
                    return;
                }
                var builder = new StringBuilder();
                args.TryGetString(0, out var separator);
                for (int i = 0; i < array.Length; i++)
                {
                    if (i > 0)
                    {
                        builder.Append(separator);
                    }
                    var element = array.GetElement(i);
                    if (element.Kind > ValueKind.Null)
                    {
                        builder.Append(ScriptDatum.ToString(element));
                    }
                }
                ScriptDatum.WriteAsString(ref result, builder.ToString());
                return;
            }
            ScriptDatum.WriteAsString(ref result, string.Empty);
        }

        /// <summary> Native implementation for the 'slice' method. Returns a shallow copy of a portion of the array. </summary>
        public static void SLICE(ScriptContext ctx, ScriptObject thisObject, Span<ScriptDatum> args, ref ScriptDatum result)
        {
            if (thisObject is not ScriptArray array)
            {
                ScriptDatum.MarkAsNull(ref result);
                return;
            }

            if (args == null || args.Length == 0)
            {
                ScriptDatum.WriteAsArray(ref result, array);
                return;
            }
            args.TryGetInteger(0, out var start);
            if (args.TryGetInteger(1, out var end))
            {
                array.SliceTo((int)start, (int)end, ref result);
            }
            else
            {
                array.SliceTo((int)start, ref result);
            }
        }

        private static void AppendArrayContents(ScriptArray target, ScriptArray source)
        {
            if (target == null || source == null || source._count == 0)
            {
                return;
            }

            target.EnsureCapacity(target._count + source._count);
            Array.Copy(source._items, 0, target._items, target._count, source._count);
            target._count += source._count;
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
