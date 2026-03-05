using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace AuroraScript.Runtime.Types
{
    /// <summary>
    /// Represents a dynamic array in AuroraScript.
    /// Manages an internal buffer of <see cref="ScriptDatum"/> and provides methods for manipulation.
    /// </summary>
    public sealed partial class ScriptArray : ScriptObject
    {
        internal ScriptDatum[] _items;
        private int _count;

        /// <summary>
        /// Initializes a new <see cref="ScriptArray"/> by copying another array.
        /// </summary>
        /// <param name="array">The source array to copy from.</param>
        public ScriptArray(ScriptArray array) : base(Prototypes.ScriptArrayPrototype)
        {
            var capacity = array._count;
            _items = new ScriptDatum[Math.Max(4, capacity)];
            if (capacity > 0)
            {
                Array.Copy(array._items, _items, capacity);
            }
        }

        /// <summary>
        /// Initializes a new <see cref="ScriptArray"/> with the specified initial capacity.
        /// </summary>
        /// <param name="capacity">The initial capacity of the array.</param>
        public ScriptArray(int capacity) : base(Prototypes.ScriptArrayPrototype)
        {
            if (capacity <= 0)
            {
                _items = Array.Empty<ScriptDatum>();
                _count = 0;
            }
            else
            {
                _items = new ScriptDatum[Math.Max(4, capacity)];
                _count = capacity;
                for (int i = 0; i < _count; i++)
                {
                    _items[i] = ScriptDatum.Null;
                }
            }
        }

        /// <summary>
        /// Initializes a new <see cref="ScriptArray"/> from a list of script objects.
        /// </summary>
        /// <param name="list">The source list.</param>
        public ScriptArray(List<ScriptObject> list) : base(Prototypes.ScriptArrayPrototype)
        {
            if (list == null || list.Count == 0)
            {
                _items = Array.Empty<ScriptDatum>();
                _count = 0;
            }
            else
            {
                _items = new ScriptDatum[Math.Max(4, list.Count)];
                _count = list.Count;
                for (int i = 0; i < _count; i++)
                {
                    ScriptDatum.WriteObject(ref _items[i], list[i]);
                }
            }
        }

        /// <summary>
        /// Initializes a new <see cref="ScriptArray"/> from a span of <see cref="ScriptDatum"/>.
        /// </summary>
        /// <param name="array">The source span.</param>
        public ScriptArray(Span<ScriptDatum> array) : base(Prototypes.ScriptArrayPrototype)
        {
            if (array.Length == 0)
            {
                _items = Array.Empty<ScriptDatum>();
                _count = 0;
            }
            else
            {
                _items = new ScriptDatum[Math.Max(4, array.Length)];
                _count = array.Length;
                for (int i = 0; i < _count; i++)
                {
                    _items[i] = array[i];
                }
            }
        }

        /// <summary>
        /// Initializes a new <see cref="ScriptArray"/> from an array of <see cref="ScriptDatum"/>.
        /// </summary>
        /// <param name="array">The source array.</param>
        public ScriptArray(ScriptDatum[] array) : base(Prototypes.ScriptArrayPrototype)
        {
            if (array.Length == 0)
            {
                _items = Array.Empty<ScriptDatum>();
                _count = 0;
            }
            else
            {
                _items = new ScriptDatum[Math.Max(4, array.Length)];
                _count = array.Length;
                for (int i = 0; i < _count; i++)
                {
                    _items[i] = array[i];
                }
            }
        }

        /// <summary>
        /// Initializes a new <see cref="ScriptArray"/> from an array of <see cref="ScriptObject"/>.
        /// </summary>
        /// <param name="array">The source array.</param>
        public ScriptArray(ScriptObject[] array) : base(Prototypes.ScriptArrayPrototype)
        {
            if (array == null || array.Length == 0)
            {
                _items = Array.Empty<ScriptDatum>();
                _count = 0;
            }
            else
            {
                _items = new ScriptDatum[Math.Max(4, array.Length)];
                _count = array.Length;
                for (int i = 0; i < _count; i++)
                {
                    ScriptDatum.WriteObject(ref _items[i], array[i]);
                }
            }
        }

        /// <summary>
        /// Initializes an empty <see cref="ScriptArray"/>.
        /// </summary>
        public ScriptArray() : base(Prototypes.ScriptArrayPrototype)
        {
            this._items = Array.Empty<ScriptDatum>();
            this._count = 0;
        }

        /// <summary> Gets the element at the specified index. </summary>
        public ScriptDatum GetElement(int index)
        {
            if (index < 0 || index >= _count) return ScriptDatum.Null;
            return _items[index];
        }

        /// <summary> Gets the element at the specified index and writes it to the provided datum. </summary>
        public void GetElement(int index, ref ScriptDatum scriptDatum)
        {
            if (index < 0) index = _count + index;
            if (index < 0 || index >= _count)
            {
                scriptDatum = default;
                return;
            }
            scriptDatum = _items[index];
        }




        /// <summary>
        /// Slices the array from start to end and writes the resulting <see cref="ScriptArray"/> to the provided datum.
        /// Supports negative indices.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void SliceTo(int start, int end, ref ScriptDatum scriptDatum)
        {
            int count = _count;
            // Support negative indexing
            if (start < 0) start += count;
            if (end < 0) end += count;

            // Clamp boundaries
            if (start < 0) start = 0;
            if (end > count) end = count;
            if (end == 0) end = count;

            int len = end - start;
            if (len <= 0)
            {
                ScriptDatum.MarkAsNull(ref scriptDatum);
                return;
            }

            var result = new ScriptArray(len);
            var dst = result._items;
            var src = _items;
            // High speed copy
            Array.Copy(src, start, dst, 0, len);
            result._count = len;
            scriptDatum = ScriptDatum.FromArray(result);
        }

        /// <summary>
        /// Slices the array from start to the end of the array.
        /// </summary>
        public void SliceTo(int start, ref ScriptDatum scriptDatum)
        {
            SliceTo(start, _count, ref scriptDatum);
        }

        /// <summary> Sets the element at the specified index. Expands the buffer if necessary. </summary>
        public void SetElement(int index, in ScriptDatum datum)
        {
            if (index < 0) index = _count + index;
            if (index < 0) return;
            if (index >= _items.Length) EnsureCapacity(index + 1);
            if (index >= _count)
            {
                _count = index + 1;
            }
            _items[index] = datum;
        }


        /// <summary> Determines whether the array contains a specific element. </summary>
        /// <param name="element">The element to locate in the array.</param>
        /// <returns>True if the element is found; otherwise, false.</returns>
        public Boolean Has(in ScriptDatum element)
        {
            for (int i = 0; i < _count; i++)
            {
                if (element.Equals(_items[i])) return true;
            }
            return false;
        }


        /// <summary> Searches for the specified element and returns the index of the first occurrence within the array. </summary>
        /// <param name="searchElement">The element to locate.</param>
        /// <param name="fromIndex">The optional starting index for the search.</param>
        /// <returns>The zero-based index of the first occurrence of the element if found; otherwise, -1.</returns>
        public int IndexOf(in ScriptDatum searchElement, int? fromIndex)
        {
            var from = 0;
            if (fromIndex.HasValue) from = fromIndex.Value;
            for (int i = from; i < _count; i++)
            {
                if (searchElement.Equals(_items[i])) return i;
            }
            return -1;
        }

        /// <summary> Searches for the specified element and returns the index of the last occurrence within the array. </summary>
        /// <param name="searchElement">The element to locate.</param>
        /// <param name="fromIndex">The optional starting index for the search (searches backwards from this index).</param>
        /// <returns>The zero-based index of the last occurrence of the element if found; otherwise, -1.</returns>
        public int LastIndexOf(in ScriptDatum searchElement, int? fromIndex)
        {
            int start = fromIndex ?? (_count - 1);
            if ((uint)start >= (uint)_count) start = _count - 1;
            for (int i = start; i >= 0; i--)
            {
                ref var item = ref _items[i];
                if (item.Equals(searchElement)) return i;
            }
            return -1;
        }



        /// <summary> Appends a datum to the end of the array. </summary>
        public void Push(ScriptDatum datum)
        {
            SetElement(_count, in datum);
        }

        /// <summary> Removes the reference at the specified index by setting it to default. </summary>
        public void Remove(int index)
        {
            if (index < 0 || index >= _count) return;
            _items[index] = default;
        }

        /// <summary> Returns a span of the active elements in the array. </summary>
        public Span<ScriptDatum> Values()
        {
            return _items.AsSpan(0, _count);
        }

        /// <summary> Convers the active elements of the array into a new <see cref="ScriptDatum"/> array. </summary>
        public ScriptDatum[] ToDatumArray()
        {
            if (_count == 0) return Array.Empty<ScriptDatum>();
            var result = new ScriptDatum[_count];
            Array.Copy(_items, result, _count);
            return result;
        }

        /// <summary> Returns a JSON-like string representation of the array. </summary>
        public override string ToString()
        {
            if (_count == 0) return "[]";
            var parts = new string[_count];
            for (int i = 0; i < _count; i++)
            {
                parts[i] = ScriptDatum.ToString(_items[i]);
            }
            return "[" + string.Join(", ", parts) + "]";
        }

        /// <summary> Returns an enumerator capable of iterating over the array. </summary>
        public sealed override ScriptEnumerator GetEnumerator()
        {
            return new ScriptEnumerator(this);
        }

        /// <summary> Gets the number of elements in the array. </summary>
        public int Length
        {
            get
            {
                return _count;
            }
        }

        /// <summary> Removes the last element and writes it to the provided datum. </summary>
        internal void PopTo(ref ScriptDatum datum)
        {
            if (_count > 0)
            {
                datum = _items[--_count];
                ScriptDatum.MarkAsNull(ref _items[_count]);
            }
        }

        internal ScriptArray MapInternal(ScriptContext ctx, ClosureFunction callback)
        {
            var count = _count;
            var newArray = new ScriptArray(count);
            var srcItems = _items;
            var destItems = newArray._items;
            for (int i = 0; i < count; i++)
            {
                destItems[i] = callback.Invoke(ctx, srcItems[i], i);
            }
            return newArray;
        }

        internal ScriptDatum FindInternal(ScriptContext ctx, ClosureFunction callback)
        {
            var count = _count;
            var items = _items;
            for (int i = 0; i < count; i++)
            {
                var result = callback.Invoke(ctx, items[i], i);
                if (ScriptDatum.IsTrue(result)) return items[i];
            }
            return ScriptDatum.Null;
        }

        internal int FindIndexInternal(ScriptContext ctx, ClosureFunction callback)
        {
            var count = _count;
            var items = _items;
            for (int i = 0; i < count; i++)
            {
                var result = callback.Invoke(ctx, items[i], i);
                if (ScriptDatum.IsTrue(result)) return i;
            }
            return -1;
        }


        internal ScriptDatum FindLastInternal(ScriptContext ctx, ClosureFunction callback)
        {
            var count = _count;
            var items = _items;
            for (int i = count - 1; i >= 0; i--)
            {
                var item = items[i];
                var result = callback.Invoke(ctx, item, i);
                if (ScriptDatum.IsTrue(result)) return item;
            }
            return ScriptDatum.Null;
        }

        internal int FindLastIndexInternal(ScriptContext ctx, ClosureFunction callback)
        {
            var count = _count;
            var items = _items;
            for (int i = 0; i < count; i++)
            {
                var result = callback.Invoke(ctx, items[i], i);
                if (ScriptDatum.IsTrue(result)) return i;
            }
            return -1;
        }

        internal ScriptArray FilterInternal(ScriptContext ctx, ClosureFunction callback)
        {
            var newArray = new ScriptArray();
            var items = _items;
            for (int i = 0; i < _count; i++)
            {
                var ok = callback.Invoke(ctx, _items[i], i);
                if (ScriptDatum.IsTrue(ok)) newArray.Push(items[i]);
            }
            return newArray;
        }


        internal Boolean SomeInternal(ScriptContext ctx, ClosureFunction callback)
        {
            var count = _count;
            var items = _items;
            for (int i = 0; i < count; i++)
            {
                var ok = callback.Invoke(ctx, items[i], i);
                if (ScriptDatum.IsTrue(ok)) return true;
            }
            return false;
        }

        internal Boolean EveryInternal(ScriptContext ctx, ClosureFunction callback)
        {
            var count = _count;
            var items = _items;
            for (int i = 0; i < count; i++)
            {
                var ok = callback.Invoke(ctx, items[i], i);
                if (!ScriptDatum.IsTrue(ok)) return false;
            }
            return true;
        }


        internal ScriptArray FlatInternal(int maxDeep)
        {
            var newArray = new ScriptArray();
            void DoFlatten(ScriptArray source, int depth)
            {
                for (int i = 0; i < source._count; i++)
                {
                    var item = source._items[i];
                    if (depth > 0 && item.Kind == ValueKind.Array)
                    {
                        DoFlatten(item.Object as ScriptArray, depth - 1);
                    }
                    else
                    {
                        newArray.Push(item);
                    }
                }
            }
            DoFlatten(this, maxDeep);
            return newArray;
        }


        internal ScriptDatum ReduceInternal(ScriptContext ctx, ClosureFunction callback)
        {
            var count = _count;
            var items = _items;
            if (count == 0) return ScriptDatum.Null;
            ScriptDatum accumulator = items[0];
            for (int i = 1; i < count; i++)
            {
                accumulator = callback.Invoke(ctx, accumulator, items[i], i);
            }
            return accumulator;
        }




        private void EnsureCapacity(int min)
        {
            if (_items.Length >= min) return;
            var newCapacity = _items.Length == 0 ? 4 : _items.Length * 2;
            if (newCapacity < min) newCapacity = min;
            var newArray = new ScriptDatum[newCapacity];
            if (_count > 0)
            {
                Array.Copy(_items, newArray, _count);
            }
            _items = newArray;
        }
    }
}
