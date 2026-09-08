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
        /// <summary>Copies a native iterable, optionally mapping its values.</summary>
        [Export("from", DynamicAdapter = nameof(FROM))]
        public static ScriptArray FromCore(ScriptContext ctx, ScriptObject source, ClosureFunction callback = null)
        {
            if (source == null) return null;
            var enumerator = source.GetEnumerator();
            var array = new ScriptArray();
            var index = 0;
            while (enumerator.NextValue(out var item))
                array.Push(callback != null ? callback.Invoke(ctx, item, index++) : item);
            return array;
        }

        /// <summary>Copies an iterable using the existing callback contract.</summary>
        private static ScriptArray FromCore(ScriptContext ctx, ScriptDatum value, ScriptDatum callback = default)
        {
            ScriptDatum.TryGetFunction(in callback, out var function);
            return FromCore(ctx, value.Object, function);
        }

        /// <summary>Creates an array from 0 values without a temporary argument array.</summary>
        [Export("of", DynamicAdapter = nameof(OF))]
        public static ScriptArray OfCore()
        {
            return new ScriptArray();
        }

        /// <summary>Creates an array from 1 values without a temporary argument array.</summary>
        [Export("of", DynamicAdapter = nameof(OF))]
        public static ScriptArray OfCore(ScriptDatum value0)
        {
            DatumBuffer1 values = default;
            values[0] = value0;
            return new ScriptArray((Span<ScriptDatum>)values);
        }

        /// <summary>Creates an array from 2 values without a temporary argument array.</summary>
        [Export("of", DynamicAdapter = nameof(OF))]
        public static ScriptArray OfCore(ScriptDatum value0, ScriptDatum value1)
        {
            DatumBuffer2 values = default;
            values[0] = value0;
            values[1] = value1;
            return new ScriptArray((Span<ScriptDatum>)values);
        }

        /// <summary>Creates an array from 3 values without a temporary argument array.</summary>
        [Export("of", DynamicAdapter = nameof(OF))]
        public static ScriptArray OfCore(ScriptDatum value0, ScriptDatum value1, ScriptDatum value2)
        {
            DatumBuffer3 values = default;
            values[0] = value0;
            values[1] = value1;
            values[2] = value2;
            return new ScriptArray((Span<ScriptDatum>)values);
        }

        /// <summary>Creates an array from 4 values without a temporary argument array.</summary>
        [Export("of", DynamicAdapter = nameof(OF))]
        public static ScriptArray OfCore(ScriptDatum value0, ScriptDatum value1, ScriptDatum value2, ScriptDatum value3)
        {
            DatumBuffer4 values = default;
            values[0] = value0;
            values[1] = value1;
            values[2] = value2;
            values[3] = value3;
            return new ScriptArray((Span<ScriptDatum>)values);
        }

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
            result = args.Length == 0 ? default : ScriptDatum.FromObject(
                FromCore(ctx, args[0], args.Length > 1 ? args[1] : default));
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
