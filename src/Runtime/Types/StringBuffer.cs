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
        private const int MaxPooledCapacity = 4096;
        private const int MaxPooledBuilders = 64;
        private static readonly ConcurrentStack<StringBuilder> _pool = new();
        private static int _pooledBuilderCount;
        private StringBuilder _builder;

        /// <inheritdoc />
        protected internal override ScriptDatum TypeOfValue => TypeNames.StringBuffer;

        /// <summary>
        /// Gets a pooled <see cref="StringBuffer"/> instance or creates a new one.
        /// </summary>
        private static StringBuilder Borrow(string initialValue = null)
        {
            if (!_pool.TryPop(out var instance))
            {
                instance = new StringBuilder();
            }
            else
            {
                System.Threading.Interlocked.Decrement(ref _pooledBuilderCount);
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
                if (_builder.Capacity > MaxPooledCapacity)
                {
                    _builder = initialValue == null ? null : new StringBuilder(initialValue);
                }
                else
                {
                    _builder.Clear();
                    if (initialValue != null) _builder.Append(initialValue);
                }
            }
            else if (initialValue != null)
            {
                _builder = Borrow(initialValue);
            }
        }

        /// <summary>
        /// Returns this instance to the pool.
        /// </summary>
        public void Release()
        {
            if (_builder != null)
            {
                var builder = _builder;
                _builder = null;
                if (builder.Capacity > MaxPooledCapacity)
                {
                    return;
                }

                builder.Clear();
                if (System.Threading.Interlocked.Increment(ref _pooledBuilderCount) <= MaxPooledBuilders)
                {
                    _pool.Push(builder);
                    return;
                }

                System.Threading.Interlocked.Decrement(ref _pooledBuilderCount);
            }
        }

        private StringBuilder GetBuilder() => _builder ??= Borrow();

        internal StringBuilder Builder => _builder;

        /// <summary>
        /// Initializes a new instance of the <see cref="StringBuffer"/> class with an empty buffer.
        /// </summary>
        public StringBuffer() : base(Prototypes.StringBufferPrototype)
        {
            EnableValueEquality();
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="StringBuffer"/> class with the specified initial value.
        /// </summary>
        /// <param name="initialValue">The initial string value to add to the buffer.</param>
        public StringBuffer(String initialValue) : base(Prototypes.StringBufferPrototype)
        {
            EnableValueEquality();
            _builder = Borrow(initialValue);
        }

        internal override bool ValueEquals(ScriptObject other)
        {
            return other is StringBuffer buffer &&
                string.Equals(ToString(), buffer.ToString(), StringComparison.Ordinal);
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
