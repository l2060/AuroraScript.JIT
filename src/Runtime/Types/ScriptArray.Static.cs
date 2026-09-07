using System;

using AuroraScript.Hosting;

namespace AuroraScript.Runtime.Types
{
    /// <summary>
    /// Represents the native 'Array' constructor function in AuroraScript.
    /// Provides static methods like Array.from(), Array.of(), and Array.isArray().
    /// </summary>
    public sealed partial class ScriptArray
    {
        /// <summary>Copies an iterable using the existing callback contract.</summary>
        [Export("from", DynamicAdapter = nameof(FROM))]
        public static ScriptDatum FromCore(ScriptContext ctx, ScriptDatum value, ScriptDatum callback)
        {
            DatumBuffer2 args = default;
            args[0] = value;
            args[1] = callback;
            var result = default(ScriptDatum);
            FROM(ctx, null, args, ref result);
            return result;
        }

        /// <summary>Creates an array containing the supplied values.</summary>
        [Export("of", DynamicAdapter = nameof(OF))]
        public static ScriptArray OfCore(params ScriptDatum[] values) =>
            values == null || values.Length == 0 ? new ScriptArray() : new ScriptArray(values);

        /// <summary>Checks the array storage tag.</summary>
        [Export("isArray", DynamicAdapter = nameof(IS_ARRAY))]
        public static bool IsArrayCore(ScriptDatum value) => value.Kind == ValueKind.Array;

        internal static void CREATE(ScriptContext ctx, ScriptObject thisObject, Span<ScriptDatum> args, ref ScriptDatum result)
        {
            var capacity = 0;
            if (args.Length == 1)
            {
                var datum = args[0];
                if (datum.Kind == ValueKind.Number)
                {
                    capacity = (int)datum.Number;
                }
            }
            ScriptDatum.WriteAsArray(ref result, new ScriptArray(capacity));
        }


        /// <summary> Native implementation for Array.from(). Creates an array from an iterable or array-like object. </summary>
        internal static void FROM(ScriptContext ctx, ScriptObject thisObject, Span<ScriptDatum> args, ref ScriptDatum result)
        {
            if (args.Length == 0)
            {
                ScriptDatum.MarkAsNull(ref result);
                return;
            }

            if (!args.TryGetEnumerator(0, out var enumerator))
            {
                ScriptDatum.MarkAsNull(ref result);
                return;
            }
            var array = new ScriptArray();
            if (args.TryGetFunction(1, out var callback))
            {
                int i = 0;
                while (enumerator.NextValue(out var data))
                {
                    array.Push(callback.Invoke(ctx, data, i));
                    i++;
                }
            }
            else
            {
                while (enumerator.NextValue(out var data))
                {
                    array.Push(data);
                }
            }
            ScriptDatum.WriteAsArray(ref result, array);
        }

        /// <summary> Native implementation for Array.of(). Creates an array from its arguments. </summary>
        internal static void OF(ScriptContext ctx, ScriptObject thisObject, Span<ScriptDatum> args, ref ScriptDatum result)
        {
            var array = ScriptArray.CreateWithCapacity(args.Length);
            for (int i = 0; i < args.Length; i++)
            {
                array.SetElement(i, args[i]);
            }
            ScriptDatum.WriteAsArray(ref result, array);
        }

        /// <summary> Native implementation for Array.withCapacity(). Creates an empty array with reserved storage. </summary>
        internal static void WITH_CAPACITY(ScriptContext ctx, ScriptObject thisObject, Span<ScriptDatum> args, ref ScriptDatum result)
        {
            ScriptDatum.WriteAsArray(ref result, args.Length == 0
                ? CreateEmptyWithCapacity(0) : CreateEmptyWithCapacity(args[0]));
        }

        /// <summary> Native implementation for Array.isArray(). Checks if the provided value is an array. </summary>
        internal static void IS_ARRAY(ScriptContext ctx, ScriptObject thisObject, Span<ScriptDatum> args, ref ScriptDatum result)
        {
            ScriptDatum.WriteAsBoolean(ref result, args.Length > 0 && args[0].Kind == ValueKind.Array);
        }
    }
}
