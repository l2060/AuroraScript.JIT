using System;
using System.Text;


namespace AuroraScript.Runtime.Types
{
    /// <summary>
    /// Represents a high-performance string builder in AuroraScript.
    /// Wraps a <see cref="StringBuilder"/> to provide efficient string concatenation.
    /// </summary>
    public sealed partial class StringBuffer : ScriptObject
    {
        private readonly StringBuilder _builder;

        /// <summary>
        /// Initializes a new instance of the <see cref="StringBuffer"/> class with an empty buffer.
        /// </summary>
        public StringBuffer() : base(Prototypes.StringBufferPrototype)
        {
            _builder = new StringBuilder();
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="StringBuffer"/> class with the specified initial value.
        /// </summary>
        /// <param name="initialValue">The initial string value to add to the buffer.</param>
        public StringBuffer(String initialValue) : base(Prototypes.StringBufferPrototype)
        {
            _builder = new StringBuilder(initialValue);
        }

        /// <summary>
        /// Returns the complete string built by this buffer.
        /// </summary>
        /// <returns>The concatenated string.</returns>
        public override string ToString()
        {
            return _builder.ToString();
        }

        /// <summary>
        /// Gets the current length of the string in the buffer.
        /// </summary>
        public int Length => _builder.Length;

    }
}
