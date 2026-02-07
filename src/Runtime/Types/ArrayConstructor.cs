using System;

namespace AuroraScript.Runtime.Types
{
    /// <summary>
    /// Represents the native 'Array' constructor function in AuroraScript.
    /// Provides static methods like Array.from(), Array.of(), and Array.isArray().
    /// </summary>
    internal class ArrayConstructor : BondingFunction
    {
        /// <summary> The global singleton instance of the Array constructor. </summary>
        internal readonly static ArrayConstructor INSTANCE = new ArrayConstructor();

        internal ArrayConstructor() : base(CONSTRUCTOR)
        {
            _prototype = Prototypes.ArrayConstructorPrototype;
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
                    array.Push(callback.Invoke(ctx, [data, i]));
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
            var array = new ScriptArray(args.Length);
            for (int i = 0; i < args.Length; i++)
            {
                array.SetElement(i, args[i]);
            }
            ScriptDatum.WriteAsArray(ref result, array);
        }

        /// <summary> Native implementation for Array.isArray(). Checks if the provided value is an array. </summary>
        internal static void IS_ARRAY(ScriptContext ctx, ScriptObject thisObject, Span<ScriptDatum> args, ref ScriptDatum result)
        {
            ScriptDatum.WriteAsBoolean(ref result, args.Length > 0 && args[0].Kind == ValueKind.Array);
        }

        /// <summary> Native implementation for the Array constructor (Array() or new Array()). </summary>
        internal static void CONSTRUCTOR(ScriptContext ctx, ScriptObject thisObject, Span<ScriptDatum> args, ref ScriptDatum result)
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
    }
}
