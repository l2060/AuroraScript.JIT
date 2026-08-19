using System;
using System.Buffers;
using System.Text;

namespace AuroraScript.Runtime.Serialization
{
    internal enum TypedDataPathSegmentKind : byte
    {
        Property,
        Index
    }

    internal struct TypedDataPathSegment
    {
        internal TypedDataPathSegmentKind Kind;
        internal string PropertyName;
        internal int Index;
    }

    internal struct TypedDataPath : IDisposable
    {
        private TypedDataPathSegment[] _segments;
        private int _count;

        internal TypedDataPath(int initialCapacity)
        {
            _segments = ArrayPool<TypedDataPathSegment>.Shared.Rent(Math.Max(4, initialCapacity));
            _count = 0;
        }

        internal void PushProperty(string propertyName)
        {
            EnsureCapacity();
            _segments[_count++] = new TypedDataPathSegment
            {
                Kind = TypedDataPathSegmentKind.Property,
                PropertyName = propertyName
            };
        }

        internal void PushIndex(int index)
        {
            EnsureCapacity();
            _segments[_count++] = new TypedDataPathSegment
            {
                Kind = TypedDataPathSegmentKind.Index,
                Index = index
            };
        }

        internal void Pop()
        {
            if (_count == 0) return;
            _segments[--_count] = default;
        }

        internal string Format()
        {
            var builder = new StringBuilder(16 + (_count * 8));
            builder.Append('$');
            for (var index = 0; index < _count; index++)
            {
                ref readonly var segment = ref _segments[index];
                if (segment.Kind == TypedDataPathSegmentKind.Index)
                {
                    builder.Append('[').Append(segment.Index).Append(']');
                    continue;
                }

                var name = segment.PropertyName ?? string.Empty;
                if (IsIdentifier(name))
                {
                    builder.Append('.').Append(name);
                    continue;
                }

                builder.Append("[\"");
                foreach (var value in name)
                {
                    if (value is '\\' or '"') builder.Append('\\');
                    builder.Append(value);
                }
                builder.Append("\"]");
            }
            return builder.ToString();
        }

        public void Dispose()
        {
            var segments = _segments;
            if (segments == null) return;
            Array.Clear(segments, 0, _count);
            _segments = null;
            _count = 0;
            ArrayPool<TypedDataPathSegment>.Shared.Return(segments);
        }

        internal static bool IsIdentifier(string value)
        {
            if (string.IsNullOrEmpty(value) || !TypedDataScanner.IsIdentifierStart(value[0]))
            {
                return false;
            }
            for (var index = 1; index < value.Length; index++)
            {
                if (!TypedDataScanner.IsIdentifierPart(value[index])) return false;
            }
            return true;
        }

        private void EnsureCapacity()
        {
            if (_count < _segments.Length) return;
            var replacement = ArrayPool<TypedDataPathSegment>.Shared.Rent(_segments.Length * 2);
            Array.Copy(_segments, replacement, _count);
            Array.Clear(_segments, 0, _count);
            ArrayPool<TypedDataPathSegment>.Shared.Return(_segments);
            _segments = replacement;
        }
    }
}
