using System.Collections.Concurrent;
using System.Linq;

namespace AuroraScript.Runtime.Types
{
    /// <summary>
    /// Represents the native 'HashMap' constructor function in AuroraScript.
    /// Used for creating high-performance concurrent hash map objects.
    /// </summary>
    internal class ScriptHashMapConstructor : ScriptType
    {
        /// <summary> The global singleton instance of the HashMap constructor. </summary>
        internal readonly static ScriptHashMapConstructor INSTANCE = new ScriptHashMapConstructor();

        internal ScriptHashMapConstructor() : base("HashMap")
        {
            _prototype = Prototypes.ObjectPrototype;
        }

        /// <summary> Constructs a new <see cref="ScriptHashMap"/> instance. </summary>
        public override void Construct(ScriptContext ctx, ScriptDatum[] args, ref ScriptDatum result)
        {
            ScriptHashMap proxy = new ScriptHashMap();
            ScriptDatum.WriteAsObject(ref result, proxy);
        }
    }

    /// <summary>
    /// Represents a high-performance, concurrent hash map in AuroraScript.
    /// Wraps a <see cref="ConcurrentDictionary{ScriptDatum, ScriptDatum}"/> to provide thread-safe key-value storage.
    /// </summary>
    public sealed partial class ScriptHashMap : ScriptObject
    {
        private ConcurrentDictionary<ScriptDatum, ScriptDatum> keyValues = new();

        /// <summary> Initializes a new instance of the <see cref="ScriptHashMap"/> class. </summary>
        public ScriptHashMap()
        {
            _prototype = Prototypes.HashMapPrototype;
        }

        /// <summary> Adds or updates a value in the hash map. </summary>
        public void Put(ScriptDatum key, ScriptDatum value)
        {
            keyValues.AddOrUpdate(key, value, (a, b) => value);
        }

        /// <summary> Gets a value from the hash map. Returns <see cref="ScriptDatum.Null"/> if not found. </summary>
        public ScriptDatum Get(ScriptDatum key)
        {
            if (keyValues.TryGetValue(key, out var value))
            {
                return value;
            }
            return ScriptDatum.Null;
        }

        /// <summary> Gets a value or inserts the provided value if the key does not exist. </summary>
        public ScriptDatum GetOrInsert(ScriptDatum key, ScriptDatum value)
        {
            return keyValues.GetOrAdd(key, value);
        }

        /// <summary> Gets a value or inserts the result of the callback if the key does not exist. </summary>
        public ScriptDatum GetOrInsert(ScriptContext ctx, ScriptDatum key, ClosureFunction callback)
        {
            return keyValues.GetOrAdd(key, callback.Invoke(ctx, [key]));
        }

        /// <summary> Checks if the hash map contains the specified key. </summary>
        public bool Has(ScriptDatum key)
        {
            return keyValues.ContainsKey(key);
        }

        /// <summary> Removes the specified key and returns its value. Returns <see cref="ScriptDatum.Null"/> if not found. </summary>
        public ScriptDatum Delete(ScriptDatum key)
        {
            if (keyValues.TryRemove(key, out var value))
            {
                return value;
            }
            return ScriptDatum.Null;
        }

        /// <summary> Returns an array of all values in the hash map. </summary>
        public ScriptDatum[] Values()
        {
            return keyValues.Values.ToArray();
        }

        /// <summary> Returns an array of all keys in the hash map. </summary>
        public ScriptDatum[] Keys()
        {
            return keyValues.Keys.ToArray();
        }

        /// <summary> Returns the number of elements in the hash map. </summary>
        public int Length()
        {
            return keyValues.Count;
        }

        /// <summary> Clears all elements from the hash map. </summary>
        public void Clear()
        {
            keyValues.Clear();
        }

        /// <summary> Returns an enumerator capable of iterating over the keys of the hash map. </summary>
        public sealed override ScriptEnumerator GetEnumerator()
        {
            return new ScriptEnumerator(keyValues.Keys.ToArray());
        }
    }
}
