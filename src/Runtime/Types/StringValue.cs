using System.Runtime.CompilerServices;

namespace AuroraScript.Runtime.Types
{
    /// <summary>
    /// Represents a string value in AuroraScript.
    /// This is an immutable object wrapping a CLI <see cref="string"/>.
    /// Uses a bounded cache for common UTF-16 characters.
    /// </summary>
    public sealed partial class StringValue : ScriptImmutable
    {
        /// <summary> Gets the underlying CLI string value. </summary>
        public readonly string Value;

        /// <inheritdoc />
        protected internal override ScriptDatum TypeOfValue => TypeNames.String;

        private static readonly StringValue[] _charCache = new StringValue[256];

        /// <summary>
        /// Initializes a new instance of the <see cref="StringValue"/> class from a string.
        /// </summary>
        /// <param name="str">The string value.</param>
        public StringValue(string str) : base(Prototypes.StringValuePrototype)
        {
            Value = str;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="StringValue"/> class from a character.
        /// </summary>
        /// <param name="str">The character.</param>
        public StringValue(char str) : base(Prototypes.StringValuePrototype)
        {
            Value = str.ToString();
        }

        /// <summary> Returns the underlying string value. </summary>
        public override string ToString()
        {
            return Value;
        }

        /// <summary>
        /// Returns a <see cref="StringValue"/> instance for the given string.
        /// </summary>
        /// <param name="value">The string value.</param>
        /// <returns>A new <see cref="StringValue"/> instance.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static StringValue Of(string value)
        {
            if (string.IsNullOrEmpty(value)) return Empty;
            return new StringValue(value);
        }

        internal static StringValue FromChar(char ch)
        {
            if ((uint)ch >= (uint)_charCache.Length)
            {
                return new StringValue(ch);
            }

            var cached = _charCache[ch];
            if (cached == null)
            {
                cached = new StringValue(ch);
                _charCache[ch] = cached;
            }
            return cached;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static string TextFromChar(char ch)
        {
            return (uint)ch < (uint)_charCache.Length
                ? FromChar(ch).Value
                : ch.ToString();
        }

        /// <summary>
        /// Checks if the string represents a "truthy" value (not null or empty).
        /// </summary>
        /// <returns>True if the string is not null or empty; otherwise, false.</returns>
        public override bool IsTrue()
        {
            return !string.IsNullOrEmpty(Value);
        }

        /// <summary>
        /// Returns an enumerator capable of iterating over the characters of the string.
        /// </summary>
        /// <returns>A <see cref="ScriptEnumerator"/> for the string.</returns>
        public sealed override ScriptEnumerator GetEnumerator()
        {
            return ScriptEnumerator.FromString(Value);
        }
    }
}
