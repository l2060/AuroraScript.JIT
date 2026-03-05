using System;
using System.Collections.Concurrent;
using System.Text;


namespace AuroraScript.Runtime.Types
{
    /// <summary>
    /// Represents a high-performance string builder in AuroraScript.
    /// Wraps a <see cref="StringBuilder"/> to provide efficient string concatenation.
    /// </summary>
    public sealed partial class StringBuffer : ScriptObject
    {
        private static readonly ConcurrentStack<StringBuilder> _pool = new();
        private StringBuilder _builder;

        /// <summary>
        /// Gets a pooled <see cref="StringBuffer"/> instance or creates a new one.
        /// </summary>
        private static StringBuilder Borrow(string initialValue = null)
        {
            if (!_pool.TryPop(out var instance))
            {
                instance = new StringBuilder();
            }
            if (initialValue != null)
            {
                instance.Append(initialValue);
            }
            return instance;
        }

        /// <summary>
        /// Resets the buffer to its initial state, potentially with a new value.
        /// </summary>
        internal void Reset(string initialValue = null)
        {
            // Reset ScriptObject members
            this.ClearProperties();
            if (_builder != null)
            {
                _builder.Clear();
                if (initialValue != null) _builder.Append(initialValue);
            }
            else if (initialValue != null)
            {
                _builder = new StringBuilder(initialValue);
            }
        }

        /// <summary>
        /// Returns this instance to the pool.
        /// </summary>
        public void Release()
        {
            if (_builder != null)
            {
                _builder.Clear();
                if (_builder.Length > 8) return;
                _pool.Push(_builder);
            }
        }

        private StringBuilder GetBuilder() => _builder ??= Borrow();

        /// <summary>
        /// Initializes a new instance of the <see cref="StringBuffer"/> class with an empty buffer.
        /// </summary>
        public StringBuffer() : base(Prototypes.StringBufferPrototype)
        {

        }

        /// <summary>
        /// Initializes a new instance of the <see cref="StringBuffer"/> class with the specified initial value.
        /// </summary>
        /// <param name="initialValue">The initial string value to add to the buffer.</param>
        public StringBuffer(String initialValue) : base(Prototypes.StringBufferPrototype)
        {
            _builder = Borrow(initialValue);
        }

        /// <summary>
        /// Returns the complete string built by this buffer.
        /// </summary>
        /// <returns>The concatenated string.</returns>
        public override string ToString()
        {
            return _builder?.ToString() ?? string.Empty;
        }

        /// <summary>
        /// Gets the current length of the string in the buffer.
        /// </summary>
        public int Length => _builder?.Length ?? 0;

    }
}
