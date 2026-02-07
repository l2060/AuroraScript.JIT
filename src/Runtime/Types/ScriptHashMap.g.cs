using System;
using System.Collections.Concurrent;
using System.Linq;

namespace AuroraScript.Runtime.Types
{
    /// <summary>
    /// Partial class for <see cref="ScriptHashMap"/> containing native method implementations for the script runtime.
    /// Provides common map operations: set, get, getOrInsert, has, clear, delete, keys, values, and size.
    /// </summary>
    public sealed partial class ScriptHashMap : ScriptObject
    {
        /// <summary> Native implementation for HashMap.set(). </summary>
        internal static void SET(ScriptContext ctx, ScriptObject thisObject, Span<ScriptDatum> args, ref ScriptDatum result)
        {
            if (thisObject is ScriptHashMap hashMap && args.Length > 1)
            {
                hashMap.Put(args[0], args[1]);
            }
        }

        /// <summary> Native implementation for HashMap.get(). </summary>
        internal static void GET(ScriptContext ctx, ScriptObject thisObject, Span<ScriptDatum> args, ref ScriptDatum result)
        {
            if (thisObject is ScriptHashMap hashMap && args.Length > 0)
            {
                result = hashMap.Get(args[0]);
            }
        }

        /// <summary> Native implementation for HashMap.getOrInsert(). Supports callback or default value. </summary>
        internal static void OGETORINSERT(ScriptContext ctx, ScriptObject thisObject, Span<ScriptDatum> args, ref ScriptDatum result)
        {
            if (thisObject is not ScriptHashMap hashMap) return;

            ScriptDatum addValue = default;
            if (args.TryGetFunction(1, out var callback))
            {
                result = hashMap.GetOrInsert(ctx, args[0], callback);
            }
            else if (args.TryGetRef(2, ref addValue))
            {
                result = hashMap.GetOrInsert(args[0], addValue);
            }
        }

        /// <summary> Native implementation for HashMap.has(). </summary>
        internal static void HAS(ScriptContext ctx, ScriptObject thisObject, Span<ScriptDatum> args, ref ScriptDatum result)
        {
            if (thisObject is ScriptHashMap hashMap && args.Length > 0)
            {
                ScriptDatum.WriteAsBoolean(ref result, hashMap.Has(args[0]));
            }
            else
            {
                ScriptDatum.WriteAsBoolean(ref result, false);
            }
        }

        /// <summary> Native implementation for HashMap.clear(). </summary>
        internal static void CLEAR(ScriptContext ctx, ScriptObject thisObject, Span<ScriptDatum> args, ref ScriptDatum result)
        {
            if (thisObject is ScriptHashMap hashMap)
            {
                hashMap.Clear();
            }
        }

        /// <summary> Native implementation for HashMap.delete(). </summary>
        internal static void DELETE(ScriptContext ctx, ScriptObject thisObject, Span<ScriptDatum> args, ref ScriptDatum result)
        {
            if (thisObject is ScriptHashMap hashMap && args.Length > 0)
            {
                hashMap.Delete(args[0]);
            }
        }

        /// <summary> Native implementation for HashMap.keys(). Returns an array of keys. </summary>
        internal static void KEYS(ScriptObject thisObject, ref ScriptDatum result)
        {
            if (thisObject is ScriptHashMap hashMap)
            {
                ScriptDatum.WriteAsArray(ref result, new ScriptArray(hashMap.Keys()));
            }
        }

        /// <summary> Native implementation for HashMap.values(). Returns an array of values. </summary>
        internal static void VALUES(ScriptObject thisObject, ref ScriptDatum result)
        {
            if (thisObject is ScriptHashMap hashMap)
            {
                ScriptDatum.WriteAsArray(ref result, new ScriptArray(hashMap.Values()));
            }
        }

        /// <summary> Native implementation for reading HashMap.size. </summary>
        internal static void SIZE(ScriptObject thisObject, ref ScriptDatum result)
        {
            if (thisObject is ScriptHashMap hashMap)
            {
                ScriptDatum.WriteAsNumber(ref result, hashMap.Length());
            }
        }
    }
}
