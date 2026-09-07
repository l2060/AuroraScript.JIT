using System;

using AuroraScript.Hosting;

namespace AuroraScript.Runtime.Types
{
    /// <summary>
    /// Partial class for <see cref="ScriptHashMap"/> containing native method implementations for the script runtime.
    /// Provides common map operations: set, get, getOrInsert, has, clear, delete, keys, values, and size.
    /// </summary>
    public sealed partial class ScriptHashMap : ScriptObject
    {
        internal static void CREATE(ScriptContext ctx, ScriptObject thisObject, Span<ScriptDatum> args, ref ScriptDatum result)
        {
            var capacity = args.Length > 0 && args[0].Kind == ValueKind.Number
                ? Math.Max(0, (int)args[0].Number) : 0;
            ScriptDatum.WriteAsObject(ref result, capacity > 0 ? new ScriptHashMap(capacity) : new ScriptHashMap());
        }

        /// <summary>Removes a key without returning the removed value to scripts.</summary>
        [Export("delete", DynamicAdapter = nameof(DELETE))]
        public void DeleteCore(ScriptDatum key) => Delete(key);

        /// <summary>Preserves callback and default-value insertion semantics.</summary>
        [Export("getOrInsert", DynamicAdapter = nameof(OGETORINSERT))]
        public ScriptDatum GetOrInsertCore(ScriptContext ctx, ScriptDatum key, ScriptDatum value)
        {
            DatumBuffer2 args = default;
            args[0] = key;
            args[1] = value;
            var result = default(ScriptDatum);
            OGETORINSERT(ctx, this, args, ref result);
            return result;
        }

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
            else if (args.TryGetRef(1, ref addValue))
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
        internal static void KEYS(ScriptContext ctx, ScriptObject thisObject, Span<ScriptDatum> args, ref ScriptDatum result)
        {
            if (thisObject is ScriptHashMap hashMap)
            {
                ScriptDatum.WriteAsArray(ref result, hashMap.KeysArray());
            }
        }

        /// <summary> Native implementation for HashMap.values(). Returns an array of values. </summary>
        internal static void VALUES(ScriptContext ctx, ScriptObject thisObject, Span<ScriptDatum> args, ref ScriptDatum result)
        {
            if (thisObject is ScriptHashMap hashMap)
            {
                ScriptDatum.WriteAsArray(ref result, hashMap.ValuesArray());
            }
        }

        /// <summary> Native implementation for reading HashMap.size. </summary>
        internal static void SIZE(ScriptContext ctx, ScriptObject thisObject, Span<ScriptDatum> args, ref ScriptDatum result)
        {
            if (thisObject is ScriptHashMap hashMap)
            {
                ScriptDatum.WriteAsNumber(ref result, hashMap.Length());
            }
        }
    }
}
