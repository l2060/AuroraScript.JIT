using System;
using System.Collections.Generic;

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

        }

        /// <summary> Constructs a new <see cref="ScriptHashMap"/> instance. </summary>
        public override void Construct(ScriptContext ctx, Span<ScriptDatum> args, ref ScriptDatum result)
        {
            var capacity = 0;
            if (args.Length > 0 && args[0].Kind == ValueKind.Number)
            {
                capacity = Math.Max(0, (int)args[0].Number);
            }

            ScriptHashMap proxy = capacity > 0 ? new ScriptHashMap(capacity) : new ScriptHashMap();
            ScriptDatum.WriteAsObject(ref result, proxy);
        }
    }

    /// <summary>
    /// Represents a high-performance hash map in AuroraScript.
    /// Wraps a <see cref="Dictionary{TKey,TValue}"/> to provide low-overhead key-value storage.
    /// </summary>
    public sealed partial class ScriptHashMap : ScriptObject
    {
        private readonly Dictionary<ScriptDatum, ScriptDatum> keyValues;

        /// <inheritdoc />
        protected internal override ScriptDatum TypeOfValue => TypeNames.HashMap;

        internal IReadOnlyDictionary<ScriptDatum, ScriptDatum> DebugEntries => keyValues;
        internal Dictionary<ScriptDatum, ScriptDatum> Entries => keyValues;

        /// <summary> Initializes a new instance of the <see cref="ScriptHashMap"/> class. </summary>
        public ScriptHashMap() : base(Prototypes.HashMapPrototype)
        {
            keyValues = new Dictionary<ScriptDatum, ScriptDatum>(ScriptDatumStrictComparer.Instance);
        }

        /// <summary> Initializes a new instance with the specified initial capacity. </summary>
        public ScriptHashMap(int capacity) : base(Prototypes.HashMapPrototype)
        {
            keyValues = new Dictionary<ScriptDatum, ScriptDatum>(Math.Max(0, capacity), ScriptDatumStrictComparer.Instance);
        }

        /// <summary> Adds or updates a value in the hash map. </summary>
        public void Put(ScriptDatum key, ScriptDatum value)
        {
            keyValues[key] = value;
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
            if (keyValues.TryGetValue(key, out var existing))
            {
                return existing;
            }

            keyValues.Add(key, value);
            return value;
        }

        /// <summary> Gets a value or inserts the result of the callback if the key does not exist. </summary>
        public ScriptDatum GetOrInsert(ScriptContext ctx, ScriptDatum key, ClosureFunction callback)
        {
            if (keyValues.TryGetValue(key, out var existing))
            {
                return existing;
            }

            var value = callback.Invoke(ctx, key);
            keyValues.Add(key, value);
            return value;
        }

        /// <summary> Checks if the hash map contains the specified key. </summary>
        public bool Has(ScriptDatum key)
        {
            return keyValues.ContainsKey(key);
        }

        /// <summary> Removes the specified key and returns its value. Returns <see cref="ScriptDatum.Null"/> if not found. </summary>
        public ScriptDatum Delete(ScriptDatum key)
        {
            if (keyValues.Remove(key, out var value))
            {
                return value;
            }
            return ScriptDatum.Null;
        }

        /// <summary> Returns an array of all values in the hash map. </summary>
        public ScriptDatum[] Values()
        {
            if (keyValues.Count == 0) return Array.Empty<ScriptDatum>();
            var values = new ScriptDatum[keyValues.Count];
            var index = 0;
            foreach (var value in keyValues.Values)
            {
                values[index++] = value;
            }
            return values;
        }

        internal ScriptArray ValuesArray()
        {
            var array = ScriptArray.CreateWithCapacity(keyValues.Count);
            var index = 0;
            foreach (var value in keyValues.Values)
            {
                array.SetElement(index++, value);
            }
            return array;
        }

        /// <summary> Returns an array of all keys in the hash map. </summary>
        public ScriptDatum[] Keys()
        {
            if (keyValues.Count == 0) return Array.Empty<ScriptDatum>();
            var keys = new ScriptDatum[keyValues.Count];
            var index = 0;
            foreach (var key in keyValues.Keys)
            {
                keys[index++] = key;
            }
            return keys;
        }

        internal ScriptArray KeysArray()
        {
            var array = ScriptArray.CreateWithCapacity(keyValues.Count);
            var index = 0;
            foreach (var key in keyValues.Keys)
            {
                array.SetElement(index++, key);
            }
            return array;
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
            return new ScriptEnumerator(Keys());
        }

        private sealed class ScriptDatumStrictComparer : IEqualityComparer<ScriptDatum>
        {
            internal static readonly ScriptDatumStrictComparer Instance = new();

            public bool Equals(ScriptDatum x, ScriptDatum y)
            {
                if (x.Kind != y.Kind) return false;
                return x.Kind switch
                {
                    ValueKind.Null => true,
                    ValueKind.Boolean => x.Boolean == y.Boolean,
                    ValueKind.Number => x.Number == y.Number,
                    ValueKind.Int64 => x.Int64 == y.Int64,
                    ValueKind.UInt64 => x.UInt64 == y.UInt64,
                    ValueKind.String => string.Equals(x.StringText, y.StringText, StringComparison.Ordinal),
                    _ => ReferenceEquals(x.Object, y.Object),
                };
            }

            public int GetHashCode(ScriptDatum obj)
            {
                return obj.Kind switch
                {
                    ValueKind.Null => 0,
                    ValueKind.Boolean => obj.Boolean.GetHashCode(),
                    ValueKind.Number => obj.Number.GetHashCode(),
                    ValueKind.Int64 => obj.Int64.GetHashCode(),
                    ValueKind.UInt64 => obj.UInt64.GetHashCode(),
                    ValueKind.String => obj.StringText.GetHashCode(StringComparison.Ordinal),
                    _ => obj.Object?.GetHashCode() ?? 0,
                };
            }
        }
    }
}
