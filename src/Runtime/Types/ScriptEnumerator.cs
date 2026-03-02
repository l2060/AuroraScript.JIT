using System;

namespace AuroraScript.Runtime.Types
{
    /// <summary>
    /// Represents an enumerator used for iterating over various collection types within AuroraScript.
    /// Supports iteration over <see cref="ScriptArray"/>, <see cref="ScriptDatum"/> arrays, and strings.
    /// </summary>
    public sealed class ScriptEnumerator : ScriptObject
    {
        private enum IteratorKind
        {
            DatumArray,
            ScriptArray,
            String
        }

        private readonly IteratorKind _kind;
        private readonly ScriptDatum[] _datumItems;
        private readonly ScriptArray _array;
        private readonly string _stringValue;
        private readonly int _length;
        private int _index;

        /// <summary> Initializes a new instance of the <see cref="ScriptEnumerator"/> from a <see cref="ScriptArray"/>. </summary>
        public ScriptEnumerator(ScriptArray array)
        {
            _kind = IteratorKind.ScriptArray;
            _array = array;
            _length = array.Length;
            _index = 0;
        }

        /// <summary> Initializes a new instance of the <see cref="ScriptEnumerator"/> from a raw <see cref="ScriptDatum"/> array. </summary>
        public ScriptEnumerator(ScriptDatum[] items)
        {
            _kind = IteratorKind.DatumArray;
            _datumItems = items ?? Array.Empty<ScriptDatum>();
            _length = _datumItems.Length;
            _index = 0;
        }

        private ScriptEnumerator(string value)
        {
            _kind = IteratorKind.String;
            _stringValue = value ?? string.Empty;
            _length = _stringValue.Length;
            _index = 0;
        }

        /// <summary> Creates a new string-based <see cref="ScriptEnumerator"/>. </summary>
        public static ScriptEnumerator FromString(string value)
        {
            return new ScriptEnumerator(value);
        }

        /// <summary> Returns the current element without advancing the enumerator. </summary>
        public ScriptDatum Value()
        {
            return _kind switch
            {
                IteratorKind.ScriptArray => _array._items[_index],
                IteratorKind.DatumArray => _datumItems[_index],
                IteratorKind.String => ScriptDatum.FromString(StringValue.FromChar(_stringValue[_index])),
                _ => _datumItems[_index],
            };
        }

        /// <summary> Advances the enumerator to the next element and returns it via an out parameter. </summary>
        /// <param name="data">The next datum in the collection.</param>
        /// <returns>True if an element was successfully retrieved; otherwise, false.</returns>
        public bool NextValue(out ScriptDatum data)
        {
            if (_index < _length)
            {
                data = Value();
                _index++;
                return true;
            }
            data = default;
            return false;
        }

        /// <summary> Checks if there are more elements to iterate over. </summary>
        public bool HasValue()
        {
            return _index < _length;
        }

        /// <summary> Advances the enumerator index by one. </summary>
        public void Next()
        {
            _index++;
        }

        /// <summary> Resets the enumerator index to zero. </summary>
        public void Reset()
        {
            _index = 0;
        }
    }
}
