using System;
using System.Buffers;
using System.Globalization;
using System.Runtime.CompilerServices;

namespace AuroraScript.Runtime.Serialization
{
    internal enum TypedDocumentTokenKind : byte
    {
        EndOfFile,
        Bad,
        Identifier,
        String,
        Number,
        Null,
        True,
        False,
        ReadOnly,
        LeftBracket,
        RightBracket,
        LeftBrace,
        RightBrace,
        Comma
    }

    internal enum TypedDocumentScanError : byte
    {
        None,
        UnexpectedCharacter,
        ScriptMarkerNotAllowed,
        UnterminatedString,
        UnterminatedComment,
        InvalidEscape,
        InvalidUnicodeEscape,
        InvalidHexEscape,
        InvalidNumber
    }

    internal readonly struct TypedDocumentToken
    {
        internal TypedDocumentToken(
            TypedDocumentTokenKind kind,
            int start,
            int length,
            int line,
            int column,
            double number = 0,
            int decodedLength = 0,
            bool hasEscapes = false,
            TypedDocumentScanError error = TypedDocumentScanError.None,
            char errorCharacter = '\0')
        {
            Kind = kind;
            Start = start;
            Length = length;
            Line = line;
            Column = column;
            Number = number;
            DecodedLength = decodedLength;
            HasEscapes = hasEscapes;
            Error = error;
            ErrorCharacter = errorCharacter;
        }

        internal TypedDocumentTokenKind Kind { get; }
        internal int Start { get; }
        internal int Length { get; }
        internal int Line { get; }
        internal int Column { get; }
        internal double Number { get; }
        internal int DecodedLength { get; }
        internal bool HasEscapes { get; }
        internal TypedDocumentScanError Error { get; }
        internal char ErrorCharacter { get; }
    }

    internal struct TypedDocumentScanner
    {
        private enum SimpleIntegerKind : byte
        {
            Int16,
            Int32,
            Int64
        }

        private enum SimpleUnsignedIntegerKind : byte
        {
            UInt8,
            UInt16,
            UInt32,
            UInt64
        }

        private readonly string _source;
        private int _position;
        private int _line;
        private int _column;

        internal TypedDocumentScanner(string source)
        {
            _source = source ?? string.Empty;
            _position = 0;
            _line = 1;
            _column = 1;
        }

        internal TypedDocumentToken Read()
        {
            if (!SkipTrivia(out var triviaError))
            {
                return triviaError;
            }
            if (AtEnd)
            {
                return Token(TypedDocumentTokenKind.EndOfFile, _position, 0, _line, _column);
            }

            var start = _position;
            var line = _line;
            var column = _column;
            var current = Peek();
            switch (current)
            {
                case '[': Advance(); return Token(TypedDocumentTokenKind.LeftBracket, start, 1, line, column);
                case ']': Advance(); return Token(TypedDocumentTokenKind.RightBracket, start, 1, line, column);
                case '{': Advance(); return Token(TypedDocumentTokenKind.LeftBrace, start, 1, line, column);
                case '}': Advance(); return Token(TypedDocumentTokenKind.RightBrace, start, 1, line, column);
                case ',': Advance(); return Token(TypedDocumentTokenKind.Comma, start, 1, line, column);
                case '\'':
                case '"':
                    return ScanString();
                case '@':
                    Advance();
                    return Bad(TypedDocumentScanError.ScriptMarkerNotAllowed, start, 1, line, column, current);
            }

            // Packed documents commonly contain millions of small literal values.
            // Recognize a standalone single digit before entering the general
            // number scanner; this avoids the digit-loop and numeric parser call
            // while preserving decimal, exponent, separator, and hexadecimal
            // forms for the general path.
            if (IsDecimalDigit(current) &&
                Peek(1) is not (>= '0' and <= '9') and not ('.' or 'e' or 'E' or '_' or 'x' or 'X'))
            {
                Advance();
                return new TypedDocumentToken(
                    TypedDocumentTokenKind.Number,
                    start,
                    1,
                    line,
                    column,
                    current - '0');
            }

            if (IsNumberStart(current)) return ScanNumber();
            if (IsIdentifierStart(current)) return ScanIdentifier();

            Advance();
            return Bad(TypedDocumentScanError.UnexpectedCharacter, start, 1, line, column, current);
        }

        internal bool TextEquals(in TypedDocumentToken token, string value)
        {
            return _source.AsSpan(token.Start, token.Length).SequenceEqual(value);
        }

        internal string GetIdentifier(in TypedDocumentToken token)
        {
            return new string(_source.AsSpan(token.Start, token.Length));
        }

        internal string GetString(in TypedDocumentToken token)
        {
            if (!token.HasEscapes)
            {
                return new string(_source.AsSpan(token.Start, token.Length));
            }

            return string.Create(
                token.DecodedLength,
                (Source: _source, Start: token.Start, Length: token.Length),
                static (destination, state) => DecodeString(destination, state.Source, state.Start, state.Length));
        }

        internal bool TryGetInt64Exact(in TypedDocumentToken token, out long value)
        {
            var source = _source.AsSpan(token.Start, token.Length);
            if (source.IndexOf('_') < 0)
            {
                return TryParseInt64Exact(source, out value);
            }

            char[] rented = null;
            Span<char> clean = source.Length <= 128
                ? stackalloc char[source.Length]
                : (rented = ArrayPool<char>.Shared.Rent(source.Length));
            var length = 0;
            for (var index = 0; index < source.Length; index++)
            {
                var current = source[index];
                if (current != '_') clean[length++] = current;
            }

            var parsed = TryParseInt64Exact(clean[..length], out value);
            if (rented != null) ArrayPool<char>.Shared.Return(rented);
            return parsed;
        }

        internal bool TryGetUInt64Exact(in TypedDocumentToken token, out ulong value)
        {
            var source = _source.AsSpan(token.Start, token.Length);
            if (source.IndexOf('_') < 0)
            {
                return TryParseUInt64Exact(source, out value);
            }

            char[] rented = null;
            Span<char> clean = source.Length <= 128
                ? stackalloc char[source.Length]
                : (rented = ArrayPool<char>.Shared.Rent(source.Length));
            var length = 0;
            for (var index = 0; index < source.Length; index++)
            {
                var current = source[index];
                if (current != '_') clean[length++] = current;
            }

            var parsed = TryParseUInt64Exact(clean[..length], out value);
            if (rented != null) ArrayPool<char>.Shared.Return(rented);
            return parsed;
        }

        /// <summary>
        /// Advances across <c>,d</c> when <c>d</c> is a single decimal digit and
        /// is immediately followed by another separator or a closing bracket.
        /// This is deliberately narrow: callers use it only after consuming a
        /// one-digit packed-array element, so all other TDoc syntax remains on the
        /// normal token path.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal bool TryReadCompactSingleDigitAfterComma(out sbyte value)
        {
            if (Peek() != ',' || !IsDecimalDigit(Peek(1)))
            {
                value = 0;
                return false;
            }

            var afterDigit = Peek(2);
            if (afterDigit is not (',' or ']'))
            {
                value = 0;
                return false;
            }

            Advance();
            value = (sbyte)(Advance() - '0');
            return true;
        }

        /// <summary>
        /// Materializes an entire compact <c>Int8Array</c> tail such as
        /// <c>1,0,1]</c> directly into its final storage. The scanner is changed
        /// only after the whole tail has been verified, so callers can safely use
        /// the regular token path when this narrow fast path does not apply.
        /// </summary>
        internal bool TryReadEntireCompactInt8Array(sbyte firstValue, out sbyte[] values)
        {
            var initialPosition = _position;
            var position = initialPosition;
            var count = 1;
            while (true)
            {
                if ((uint)position >= (uint)_source.Length)
                {
                    values = null;
                    return false;
                }
                if (_source[position] == ']') break;
                if (_source[position] != ',' ||
                    position > _source.Length - 3 ||
                    !IsDecimalDigit(_source[position + 1]) ||
                    _source[position + 2] is not (',' or ']'))
                {
                    values = null;
                    return false;
                }
                count++;
                position += 2;
            }

            values = new sbyte[count];
            values[0] = firstValue;
            position = initialPosition;
            for (var index = 1; index < count; index++)
            {
                values[index] = (sbyte)(_source[position + 1] - '0');
                position += 2;
            }

            _position = position;
            _column += position - initialPosition;
            return true;
        }

        /// <summary>
        /// Materializes an entire compact <c>UInt8Array</c> tail such as
        /// <c>1,0,1]</c> directly into its final storage. The scanner is changed
        /// only after the whole tail has been verified, so callers can safely use
        /// the regular token path when this narrow fast path does not apply.
        /// </summary>
        internal bool TryReadEntireCompactUInt8Array(byte firstValue, out byte[] values)
        {
            var initialPosition = _position;
            var position = initialPosition;
            var count = 1;
            while (true)
            {
                if ((uint)position >= (uint)_source.Length)
                {
                    values = null;
                    return false;
                }
                if (_source[position] == ']') break;
                if (_source[position] != ',' ||
                    position > _source.Length - 3 ||
                    !IsDecimalDigit(_source[position + 1]) ||
                    _source[position + 2] is not (',' or ']'))
                {
                    values = null;
                    return false;
                }
                count++;
                position += 2;
            }

            values = new byte[count];
            values[0] = firstValue;
            position = initialPosition;
            for (var index = 1; index < count; index++)
            {
                values[index] = (byte)(_source[position + 1] - '0');
                position += 2;
            }

            _position = position;
            _column += position - initialPosition;
            return true;
        }

        internal bool TryReadEntireSimpleInt16Array(
            in TypedDocumentToken first,
            out short[] values)
        {
            values = null;
            if (!TryScanSimpleIntegerArray(
                    first,
                    SimpleIntegerKind.Int16,
                    out var count,
                    out var firstValue,
                    out var endPosition,
                    out var endLine,
                    out var endColumn))
            {
                return false;
            }

            var result = new short[count];
            result[0] = (short)firstValue;
            if (!TryFillSimpleInt16Array(result, count)) return false;

            _position = endPosition;
            _line = endLine;
            _column = endColumn;
            values = result;
            return true;
        }

        internal bool TryReadEntireSimpleInt32Array(
            in TypedDocumentToken first,
            out int[] values)
        {
            values = null;
            if (!TryScanSimpleIntegerArray(
                    first,
                    SimpleIntegerKind.Int32,
                    out var count,
                    out var firstValue,
                    out var endPosition,
                    out var endLine,
                    out var endColumn))
            {
                return false;
            }

            var result = new int[count];
            result[0] = (int)firstValue;
            if (!TryFillSimpleInt32Array(result, count)) return false;

            _position = endPosition;
            _line = endLine;
            _column = endColumn;
            values = result;
            return true;
        }

        internal bool TryReadEntireSimpleInt64Array(
            in TypedDocumentToken first,
            out long[] values)
        {
            values = null;
            if (!TryScanSimpleIntegerArray(
                    first,
                    SimpleIntegerKind.Int64,
                    out var count,
                    out var firstValue,
                    out var endPosition,
                    out var endLine,
                    out var endColumn))
            {
                return false;
            }

            var result = new long[count];
            result[0] = firstValue;
            if (!TryFillSimpleInt64Array(result, count)) return false;

            _position = endPosition;
            _line = endLine;
            _column = endColumn;
            values = result;
            return true;
        }

        private bool TryScanSimpleIntegerArray(
            in TypedDocumentToken first,
            SimpleIntegerKind kind,
            out int count,
            out long firstValue,
            out int endPosition,
            out int endLine,
            out int endColumn)
        {
            count = 0;
            firstValue = 0;
            endPosition = 0;
            endLine = 0;
            endColumn = 0;

            var firstPosition = first.Start;
            var firstLine = first.Line;
            var firstColumn = first.Column;
            if (first.Kind != TypedDocumentTokenKind.Number ||
                !TryReadSimpleIntegerToken(
                    _source,
                    ref firstPosition,
                    ref firstLine,
                    ref firstColumn,
                    kind,
                    out firstValue) ||
                firstPosition != first.Start + first.Length)
            {
                return false;
            }

            count = 1;
            var position = _position;
            var line = _line;
            var column = _column;
            while (true)
            {
                SkipSimpleWhitespace(_source, ref position, ref line, ref column);
                if ((uint)position >= (uint)_source.Length) return false;

                if (_source[position] == ']')
                {
                    endPosition = position;
                    endLine = line;
                    endColumn = column;
                    return true;
                }
                if (_source[position] != ',') return false;

                position++;
                column++;
                SkipSimpleWhitespace(_source, ref position, ref line, ref column);
                if ((uint)position < (uint)_source.Length && _source[position] == ']')
                {
                    endPosition = position;
                    endLine = line;
                    endColumn = column;
                    return true;
                }
                if (!TryReadSimpleIntegerToken(
                        _source,
                        ref position,
                        ref line,
                        ref column,
                        kind,
                        out _))
                {
                    return false;
                }
                count++;
            }
        }

        private bool TryFillSimpleInt16Array(short[] values, int count)
        {
            var position = _position;
            var line = _line;
            var column = _column;
            for (var index = 1; index < count; index++)
            {
                SkipSimpleWhitespace(_source, ref position, ref line, ref column);
                if ((uint)position >= (uint)_source.Length || _source[position] != ',') return false;
                position++;
                column++;
                if (!TryReadSimpleIntegerToken(
                        _source,
                        ref position,
                        ref line,
                        ref column,
                        SimpleIntegerKind.Int16,
                        out var value))
                {
                    return false;
                }
                values[index] = (short)value;
            }
            return true;
        }

        private bool TryFillSimpleInt32Array(int[] values, int count)
        {
            var position = _position;
            var line = _line;
            var column = _column;
            for (var index = 1; index < count; index++)
            {
                SkipSimpleWhitespace(_source, ref position, ref line, ref column);
                if ((uint)position >= (uint)_source.Length || _source[position] != ',') return false;
                position++;
                column++;
                if (!TryReadSimpleIntegerToken(
                        _source,
                        ref position,
                        ref line,
                        ref column,
                        SimpleIntegerKind.Int32,
                        out var value))
                {
                    return false;
                }
                values[index] = (int)value;
            }
            return true;
        }

        private bool TryFillSimpleInt64Array(long[] values, int count)
        {
            var position = _position;
            var line = _line;
            var column = _column;
            for (var index = 1; index < count; index++)
            {
                SkipSimpleWhitespace(_source, ref position, ref line, ref column);
                if ((uint)position >= (uint)_source.Length || _source[position] != ',') return false;
                position++;
                column++;
                if (!TryReadSimpleIntegerToken(
                        _source,
                        ref position,
                        ref line,
                        ref column,
                        SimpleIntegerKind.Int64,
                        out var value))
                {
                    return false;
                }
                values[index] = value;
            }
            return true;
        }

        private static bool TryReadSimpleIntegerToken(
            string source,
            ref int position,
            ref int line,
            ref int column,
            SimpleIntegerKind kind,
            out long value)
        {
            SkipSimpleWhitespace(source, ref position, ref line, ref column);
            var negative = false;
            if ((uint)position < (uint)source.Length && source[position] == '-')
            {
                negative = true;
                position++;
                column++;
            }

            var digitsStart = position;
            var maximumBeforeLastDigit = kind switch
            {
                SimpleIntegerKind.Int16 => 3276UL,
                SimpleIntegerKind.Int32 => 214748364UL,
                _ => 922337203685477580UL
            };
            var maximumLastDigit = negative ? 8U : 7U;
            ulong magnitude = 0;
            while ((uint)position < (uint)source.Length && IsDecimalDigit(source[position]))
            {
                var digit = (uint)(source[position] - '0');
                if (magnitude > maximumBeforeLastDigit ||
                    (magnitude == maximumBeforeLastDigit && digit > maximumLastDigit))
                {
                    value = 0;
                    return false;
                }
                magnitude = (magnitude * 10UL) + digit;
                position++;
                column++;
            }
            if (position == digitsStart)
            {
                value = 0;
                return false;
            }

            if ((uint)position < (uint)source.Length &&
                !char.IsWhiteSpace(source[position]) &&
                source[position] is not (',' or ']'))
            {
                value = 0;
                return false;
            }

            if (!negative)
            {
                value = (long)magnitude;
                return true;
            }
            if (kind == SimpleIntegerKind.Int64 &&
                magnitude == 9223372036854775808UL)
            {
                value = long.MinValue;
                return true;
            }
            value = -(long)magnitude;
            return true;
        }

        private static void SkipSimpleWhitespace(
            string source,
            ref int position,
            ref int line,
            ref int column)
        {
            while ((uint)position < (uint)source.Length)
            {
                var current = source[position];
                if (current == '\r')
                {
                    position++;
                    if ((uint)position < (uint)source.Length && source[position] == '\n') position++;
                    line++;
                    column = 1;
                    continue;
                }
                if (current == '\n')
                {
                    position++;
                    line++;
                    column = 1;
                    continue;
                }
                if (!char.IsWhiteSpace(current)) break;
                position++;
                column++;
            }
        }

        internal bool TryReadEntireSimpleUInt8Array(
            in TypedDocumentToken first,
            out byte[] values)
        {
            values = null;
            if (!TryScanSimpleUnsignedIntegerArray(
                    first,
                    SimpleUnsignedIntegerKind.UInt8,
                    out var count,
                    out var firstValue,
                    out var endPosition,
                    out var endLine,
                    out var endColumn))
            {
                return false;
            }

            var result = new byte[count];
            result[0] = (byte)firstValue;
            if (!TryFillSimpleUInt8Array(result, count)) return false;

            _position = endPosition;
            _line = endLine;
            _column = endColumn;
            values = result;
            return true;
        }

        internal bool TryReadEntireSimpleUInt16Array(
            in TypedDocumentToken first,
            out ushort[] values)
        {
            values = null;
            if (!TryScanSimpleUnsignedIntegerArray(
                    first,
                    SimpleUnsignedIntegerKind.UInt16,
                    out var count,
                    out var firstValue,
                    out var endPosition,
                    out var endLine,
                    out var endColumn))
            {
                return false;
            }

            var result = new ushort[count];
            result[0] = (ushort)firstValue;
            if (!TryFillSimpleUInt16Array(result, count)) return false;

            _position = endPosition;
            _line = endLine;
            _column = endColumn;
            values = result;
            return true;
        }

        internal bool TryReadEntireSimpleUInt32Array(
            in TypedDocumentToken first,
            out uint[] values)
        {
            values = null;
            if (!TryScanSimpleUnsignedIntegerArray(
                    first,
                    SimpleUnsignedIntegerKind.UInt32,
                    out var count,
                    out var firstValue,
                    out var endPosition,
                    out var endLine,
                    out var endColumn))
            {
                return false;
            }

            var result = new uint[count];
            result[0] = (uint)firstValue;
            if (!TryFillSimpleUInt32Array(result, count)) return false;

            _position = endPosition;
            _line = endLine;
            _column = endColumn;
            values = result;
            return true;
        }

        internal bool TryReadEntireSimpleUInt64Array(
            in TypedDocumentToken first,
            out ulong[] values)
        {
            values = null;
            if (!TryScanSimpleUnsignedIntegerArray(
                    first,
                    SimpleUnsignedIntegerKind.UInt64,
                    out var count,
                    out var firstValue,
                    out var endPosition,
                    out var endLine,
                    out var endColumn))
            {
                return false;
            }

            var result = new ulong[count];
            result[0] = firstValue;
            if (!TryFillSimpleUInt64Array(result, count)) return false;

            _position = endPosition;
            _line = endLine;
            _column = endColumn;
            values = result;
            return true;
        }

        private bool TryScanSimpleUnsignedIntegerArray(
            in TypedDocumentToken first,
            SimpleUnsignedIntegerKind kind,
            out int count,
            out ulong firstValue,
            out int endPosition,
            out int endLine,
            out int endColumn)
        {
            count = 0;
            firstValue = 0;
            endPosition = 0;
            endLine = 0;
            endColumn = 0;

            var firstPosition = first.Start;
            var firstLine = first.Line;
            var firstColumn = first.Column;
            if (first.Kind != TypedDocumentTokenKind.Number ||
                !TryReadSimpleUnsignedIntegerToken(
                    _source,
                    ref firstPosition,
                    ref firstLine,
                    ref firstColumn,
                    kind,
                    out firstValue) ||
                firstPosition != first.Start + first.Length)
            {
                return false;
            }

            count = 1;
            var position = _position;
            var line = _line;
            var column = _column;
            while (true)
            {
                SkipSimpleWhitespace(_source, ref position, ref line, ref column);
                if ((uint)position >= (uint)_source.Length) return false;

                if (_source[position] == ']')
                {
                    endPosition = position;
                    endLine = line;
                    endColumn = column;
                    return true;
                }
                if (_source[position] != ',') return false;

                position++;
                column++;
                SkipSimpleWhitespace(_source, ref position, ref line, ref column);
                if ((uint)position < (uint)_source.Length && _source[position] == ']')
                {
                    endPosition = position;
                    endLine = line;
                    endColumn = column;
                    return true;
                }
                if (!TryReadSimpleUnsignedIntegerToken(
                        _source,
                        ref position,
                        ref line,
                        ref column,
                        kind,
                        out _))
                {
                    return false;
                }
                count++;
            }
        }

        private bool TryFillSimpleUInt8Array(byte[] values, int count)
        {
            var position = _position;
            var line = _line;
            var column = _column;
            for (var index = 1; index < count; index++)
            {
                SkipSimpleWhitespace(_source, ref position, ref line, ref column);
                if ((uint)position >= (uint)_source.Length || _source[position] != ',') return false;
                position++;
                column++;
                if (!TryReadSimpleUnsignedIntegerToken(
                        _source,
                        ref position,
                        ref line,
                        ref column,
                        SimpleUnsignedIntegerKind.UInt8,
                        out var value))
                {
                    return false;
                }
                values[index] = (byte)value;
            }
            return true;
        }

        private bool TryFillSimpleUInt16Array(ushort[] values, int count)
        {
            var position = _position;
            var line = _line;
            var column = _column;
            for (var index = 1; index < count; index++)
            {
                SkipSimpleWhitespace(_source, ref position, ref line, ref column);
                if ((uint)position >= (uint)_source.Length || _source[position] != ',') return false;
                position++;
                column++;
                if (!TryReadSimpleUnsignedIntegerToken(
                        _source,
                        ref position,
                        ref line,
                        ref column,
                        SimpleUnsignedIntegerKind.UInt16,
                        out var value))
                {
                    return false;
                }
                values[index] = (ushort)value;
            }
            return true;
        }

        private bool TryFillSimpleUInt32Array(uint[] values, int count)
        {
            var position = _position;
            var line = _line;
            var column = _column;
            for (var index = 1; index < count; index++)
            {
                SkipSimpleWhitespace(_source, ref position, ref line, ref column);
                if ((uint)position >= (uint)_source.Length || _source[position] != ',') return false;
                position++;
                column++;
                if (!TryReadSimpleUnsignedIntegerToken(
                        _source,
                        ref position,
                        ref line,
                        ref column,
                        SimpleUnsignedIntegerKind.UInt32,
                        out var value))
                {
                    return false;
                }
                values[index] = (uint)value;
            }
            return true;
        }

        private bool TryFillSimpleUInt64Array(ulong[] values, int count)
        {
            var position = _position;
            var line = _line;
            var column = _column;
            for (var index = 1; index < count; index++)
            {
                SkipSimpleWhitespace(_source, ref position, ref line, ref column);
                if ((uint)position >= (uint)_source.Length || _source[position] != ',') return false;
                position++;
                column++;
                if (!TryReadSimpleUnsignedIntegerToken(
                        _source,
                        ref position,
                        ref line,
                        ref column,
                        SimpleUnsignedIntegerKind.UInt64,
                        out var value))
                {
                    return false;
                }
                values[index] = value;
            }
            return true;
        }

        private static bool TryReadSimpleUnsignedIntegerToken(
            string source,
            ref int position,
            ref int line,
            ref int column,
            SimpleUnsignedIntegerKind kind,
            out ulong value)
        {
            SkipSimpleWhitespace(source, ref position, ref line, ref column);
            var maximumBeforeLastDigit = kind switch
            {
                SimpleUnsignedIntegerKind.UInt8 => 25UL,
                SimpleUnsignedIntegerKind.UInt16 => 6553UL,
                SimpleUnsignedIntegerKind.UInt32 => 429496729UL,
                _ => 1844674407370955161UL
            };
            var digitsStart = position;
            var magnitude = 0UL;
            while ((uint)position < (uint)source.Length && IsDecimalDigit(source[position]))
            {
                var digit = (uint)(source[position] - '0');
                if (magnitude > maximumBeforeLastDigit ||
                    (magnitude == maximumBeforeLastDigit && digit > 5U))
                {
                    value = 0;
                    return false;
                }
                magnitude = (magnitude * 10UL) + digit;
                position++;
                column++;
            }
            if (position == digitsStart)
            {
                value = 0;
                return false;
            }
            if ((uint)position < (uint)source.Length &&
                !char.IsWhiteSpace(source[position]) &&
                source[position] is not (',' or ']'))
            {
                value = 0;
                return false;
            }
            value = magnitude;
            return true;
        }

        internal bool TryReadEntireSimpleFloat64Array(
            in TypedDocumentToken first,
            out double[] values)
        {
            values = null;
            if (!TryScanSimpleFloatArray(
                    first,
                    out var count,
                    out var firstValue,
                    out var endPosition,
                    out var endLine,
                    out var endColumn))
            {
                return false;
            }

            var result = new double[count];
            result[0] = firstValue;
            if (!TryFillSimpleFloatArray(result, count)) return false;

            _position = endPosition;
            _line = endLine;
            _column = endColumn;
            values = result;
            return true;
        }

        private bool TryScanSimpleFloatArray(
            in TypedDocumentToken first,
            out int count,
            out double firstValue,
            out int endPosition,
            out int endLine,
            out int endColumn)
        {
            count = 0;
            firstValue = 0;
            endPosition = 0;
            endLine = 0;
            endColumn = 0;

            var firstPosition = first.Start;
            var firstLine = first.Line;
            var firstColumn = first.Column;
            if (first.Kind != TypedDocumentTokenKind.Number ||
                !TryReadSimpleFloatSyntax(
                    _source,
                    ref firstPosition,
                    ref firstLine,
                    ref firstColumn,
                    out _,
                    out _) ||
                firstPosition != first.Start + first.Length ||
                !double.IsFinite(first.Number))
            {
                return false;
            }
            firstValue = first.Number;

            count = 1;
            var position = _position;
            var line = _line;
            var column = _column;
            while (true)
            {
                SkipSimpleWhitespace(_source, ref position, ref line, ref column);
                if ((uint)position >= (uint)_source.Length) return false;
                if (_source[position] == ']')
                {
                    endPosition = position;
                    endLine = line;
                    endColumn = column;
                    return true;
                }
                if (_source[position] != ',') return false;

                position++;
                column++;
                SkipSimpleWhitespace(_source, ref position, ref line, ref column);
                if ((uint)position < (uint)_source.Length && _source[position] == ']')
                {
                    endPosition = position;
                    endLine = line;
                    endColumn = column;
                    return true;
                }
                if (!TryReadSimpleFloatSyntax(
                        _source,
                        ref position,
                        ref line,
                        ref column,
                        out _,
                        out _))
                {
                    return false;
                }
                count++;
            }
        }

        private bool TryFillSimpleFloatArray(double[] values, int count)
        {
            var position = _position;
            var line = _line;
            var column = _column;
            for (var index = 1; index < count; index++)
            {
                SkipSimpleWhitespace(_source, ref position, ref line, ref column);
                if ((uint)position >= (uint)_source.Length || _source[position] != ',') return false;
                position++;
                column++;
                if (!TryReadSimpleFloatToken(
                        _source,
                        ref position,
                        ref line,
                        ref column,
                        out var value))
                {
                    return false;
                }
                values[index] = value;
            }
            return true;
        }

        private static bool TryReadSimpleFloatSyntax(
            string source,
            ref int position,
            ref int line,
            ref int column,
            out int start,
            out int length)
        {
            SkipSimpleWhitespace(source, ref position, ref line, ref column);
            start = position;
            if ((uint)position < (uint)source.Length && source[position] == '-')
            {
                position++;
                column++;
            }

            var integerDigits = 0;
            while ((uint)position < (uint)source.Length && IsDecimalDigit(source[position]))
            {
                position++;
                column++;
                integerDigits++;
            }
            if (integerDigits == 0)
            {
                length = 0;
                return false;
            }

            if ((uint)position < (uint)source.Length && source[position] == '.')
            {
                position++;
                column++;
                while ((uint)position < (uint)source.Length && IsDecimalDigit(source[position]))
                {
                    position++;
                    column++;
                }
            }

            if ((uint)position < (uint)source.Length && source[position] is 'e' or 'E')
            {
                position++;
                column++;
                if ((uint)position < (uint)source.Length && source[position] is '+' or '-')
                {
                    position++;
                    column++;
                }
                var exponentDigits = 0;
                while ((uint)position < (uint)source.Length && IsDecimalDigit(source[position]))
                {
                    position++;
                    column++;
                    exponentDigits++;
                }
                if (exponentDigits == 0)
                {
                    length = 0;
                    return false;
                }
            }

            if ((uint)position < (uint)source.Length &&
                !char.IsWhiteSpace(source[position]) &&
                source[position] is not (',' or ']'))
            {
                length = 0;
                return false;
            }

            length = position - start;
            return true;
        }

        private static bool TryReadSimpleFloatToken(
            string source,
            ref int position,
            ref int line,
            ref int column,
            out double value)
        {
            if (!TryReadSimpleFloatSyntax(
                    source,
                    ref position,
                    ref line,
                    ref column,
                    out var start,
                    out var length))
            {
                value = 0;
                return false;
            }

            if (!TryParseDecimal(
                    source.AsSpan(start, length),
                    hasSeparators: false,
                    out value) ||
                !double.IsFinite(value))
            {
                value = 0;
                return false;
            }
            return true;
        }

        private static void DecodeString(Span<char> destination, string source, int start, int length)
        {
            var input = source.AsSpan(start, length);
            var sourceIndex = 0;
            var destinationIndex = 0;
            while (sourceIndex < input.Length)
            {
                var current = input[sourceIndex++];
                if (current == '\r')
                {
                    if (sourceIndex < input.Length && input[sourceIndex] == '\n') sourceIndex++;
                    destination[destinationIndex++] = '\n';
                    continue;
                }
                if (current != '\\')
                {
                    destination[destinationIndex++] = current;
                    continue;
                }

                var escaped = input[sourceIndex++];
                destination[destinationIndex++] = escaped switch
                {
                    '\\' => '\\',
                    '\'' => '\'',
                    '"' => '"',
                    'a' => '\a',
                    '0' => '\0',
                    'b' => '\b',
                    'f' => '\f',
                    'n' => '\n',
                    'r' => '\r',
                    't' => '\t',
                    'v' => '\v',
                    '`' => '`',
                    '$' => '$',
                    '{' => '{',
                    '}' => '}',
                    'u' => (char)ReadHex(input, ref sourceIndex, 4),
                    'x' => (char)ReadHex(input, ref sourceIndex, 2),
                    _ => escaped
                };
            }
        }

        private static int ReadHex(ReadOnlySpan<char> input, ref int index, int digits)
        {
            var value = 0;
            for (var offset = 0; offset < digits; offset++)
            {
                TryHexValue(input[index++], out var digit);
                value = (value << 4) | digit;
            }
            return value;
        }

        internal static bool TryParseInt64Exact(ReadOnlySpan<char> source, out long value)
        {
            var negative = source.Length != 0 && source[0] == '-';
            var numberStart = negative ? 1 : 0;
            if (source.Length >= numberStart + 3 &&
                source[numberStart] == '0' &&
                source[numberStart + 1] is 'x' or 'X')
            {
                if (!ulong.TryParse(
                        source[(numberStart + 2)..],
                        NumberStyles.AllowHexSpecifier,
                        CultureInfo.InvariantCulture,
                        out var magnitude))
                {
                    value = 0;
                    return false;
                }

                if (!negative)
                {
                    if (magnitude > long.MaxValue)
                    {
                        value = 0;
                        return false;
                    }
                    value = (long)magnitude;
                    return true;
                }

                const ulong longMinimumMagnitude = 9223372036854775808UL;
                if (magnitude > longMinimumMagnitude)
                {
                    value = 0;
                    return false;
                }
                value = magnitude == longMinimumMagnitude ? long.MinValue : -(long)magnitude;
                return true;
            }

            if (source.IndexOfAny('.', 'e', 'E') < 0)
            {
                return long.TryParse(
                    source,
                    NumberStyles.AllowLeadingSign,
                    CultureInfo.InvariantCulture,
                    out value);
            }

            if (decimal.TryParse(
                    source,
                    NumberStyles.Float,
                    CultureInfo.InvariantCulture,
                    out var decimalValue) &&
                decimal.Truncate(decimalValue) == decimalValue &&
                decimalValue >= long.MinValue &&
                decimalValue <= long.MaxValue)
            {
                value = decimal.ToInt64(decimalValue);
                return true;
            }
            value = 0;
            return false;
        }

        internal static bool TryParseUInt64Exact(ReadOnlySpan<char> source, out ulong value)
        {
            if (source.Length != 0 && source[0] == '-')
            {
                value = 0;
                return false;
            }
            var numberStart = source.Length != 0 && source[0] == '+' ? 1 : 0;
            if (source.Length >= numberStart + 3 &&
                source[numberStart] == '0' &&
                source[numberStart + 1] is 'x' or 'X')
            {
                return ulong.TryParse(
                    source[(numberStart + 2)..],
                    NumberStyles.AllowHexSpecifier,
                    CultureInfo.InvariantCulture,
                    out value);
            }

            if (source.IndexOfAny('.', 'e', 'E') < 0)
            {
                return ulong.TryParse(
                    source[numberStart..],
                    NumberStyles.AllowLeadingSign,
                    CultureInfo.InvariantCulture,
                    out value);
            }

            if (decimal.TryParse(
                    source[numberStart..],
                    NumberStyles.Float,
                    CultureInfo.InvariantCulture,
                    out var decimalValue) &&
                decimal.Truncate(decimalValue) == decimalValue &&
                decimalValue >= 0m && decimalValue <= ulong.MaxValue)
            {
                value = decimal.ToUInt64(decimalValue);
                return true;
            }
            value = 0;
            return false;
        }

        private bool SkipTrivia(out TypedDocumentToken error)
        {
            error = default;
            while (!AtEnd)
            {
                if (char.IsWhiteSpace(Peek()))
                {
                    Advance();
                    continue;
                }

                if (Peek() != '/' || Peek(1) is not ('/' or '*'))
                {
                    return true;
                }

                if (Peek(1) == '/')
                {
                    Advance();
                    Advance();
                    while (!AtEnd && Peek() is not ('\r' or '\n')) Advance();
                    continue;
                }

                var start = _position;
                var line = _line;
                var column = _column;
                Advance();
                Advance();
                while (!AtEnd && !(Peek() == '*' && Peek(1) == '/')) Advance();
                if (AtEnd)
                {
                    error = Bad(
                        TypedDocumentScanError.UnterminatedComment,
                        start,
                        _position - start,
                        line,
                        column);
                    return false;
                }
                Advance();
                Advance();
            }
            return true;
        }

        private TypedDocumentToken ScanIdentifier()
        {
            var start = _position;
            var line = _line;
            var column = _column;
            Advance();
            while (!AtEnd && IsIdentifierPart(Peek())) Advance();
            var length = _position - start;
            var kind = length switch
            {
                4 when _source.AsSpan(start, length).SequenceEqual("null") => TypedDocumentTokenKind.Null,
                4 when _source.AsSpan(start, length).SequenceEqual("true") => TypedDocumentTokenKind.True,
                5 when _source.AsSpan(start, length).SequenceEqual("false") => TypedDocumentTokenKind.False,
                8 when _source.AsSpan(start, length).SequenceEqual("readonly") => TypedDocumentTokenKind.ReadOnly,
                _ => TypedDocumentTokenKind.Identifier
            };
            return Token(kind, start, length, line, column);
        }

        private TypedDocumentToken ScanNumber()
        {
            var start = _position;
            var line = _line;
            var column = _column;
            var negative = false;
            var hasSeparators = false;
            if (Peek() == '-')
            {
                negative = true;
                Advance();
            }

            if (Peek() == '0' && Peek(1) is 'x' or 'X')
            {
                Advance();
                Advance();
                var digitsStart = _position;
                while (!AtEnd && IsHexDigit(Peek())) Advance();
                var length = _position - start;
                if (_position == digitsStart ||
                    !ulong.TryParse(
                        _source.AsSpan(digitsStart, _position - digitsStart),
                        NumberStyles.AllowHexSpecifier,
                        CultureInfo.InvariantCulture,
                        out var integer))
                {
                    return Bad(TypedDocumentScanError.InvalidNumber, start, length, line, column);
                }
                var number = (double)integer;
                return new TypedDocumentToken(
                    TypedDocumentTokenKind.Number,
                    start,
                    length,
                    line,
                    column,
                    negative ? -number : number);
            }

            if (!ScanDecimalDigits(ref hasSeparators))
            {
                return Bad(TypedDocumentScanError.InvalidNumber, start, _position - start, line, column);
            }
            if (!AtEnd && Peek() == '.')
            {
                Advance();
                if (!AtEnd && Peek() == '_')
                {
                    Advance();
                    return Bad(TypedDocumentScanError.InvalidNumber, start, _position - start, line, column);
                }
                if (!ScanDecimalDigits(ref hasSeparators, requireDigit: false))
                {
                    return Bad(TypedDocumentScanError.InvalidNumber, start, _position - start, line, column);
                }
            }
            if (!AtEnd && Peek() is 'e' or 'E')
            {
                Advance();
                if (!AtEnd && Peek() is '+' or '-') Advance();
                if (!ScanDecimalDigits(ref hasSeparators))
                {
                    return Bad(
                        TypedDocumentScanError.InvalidNumber,
                        start,
                        _position - start,
                        line,
                        column);
                }
            }

            var tokenLength = _position - start;
            if (!TryParseDecimal(_source.AsSpan(start, tokenLength), hasSeparators, out var value) ||
                !double.IsFinite(value))
            {
                return Bad(TypedDocumentScanError.InvalidNumber, start, tokenLength, line, column);
            }
            return new TypedDocumentToken(TypedDocumentTokenKind.Number, start, tokenLength, line, column, value);
        }

        private TypedDocumentToken ScanString()
        {
            var line = _line;
            var column = _column;
            var quote = Advance();
            var contentStart = _position;
            var decodedLength = 0;
            var hasEscapes = false;

            while (!AtEnd)
            {
                var current = Peek();
                if (current == quote)
                {
                    var contentLength = _position - contentStart;
                    Advance();
                    return new TypedDocumentToken(
                        TypedDocumentTokenKind.String,
                        contentStart,
                        contentLength,
                        line,
                        column,
                        decodedLength: decodedLength,
                        hasEscapes: hasEscapes);
                }
                if (current is '\r' or '\n')
                {
                    Advance();
                    // Physical line endings are normalized to LF in the decoded value.
                    // Mark the token for decoding so the raw-string fast path cannot
                    // expose a platform-specific CRLF sequence.
                    hasEscapes = true;
                    decodedLength++;
                    continue;
                }
                if (current != '\\')
                {
                    Advance();
                    decodedLength++;
                    continue;
                }

                hasEscapes = true;
                Advance();
                if (AtEnd)
                {
                    return Bad(
                        TypedDocumentScanError.UnterminatedString,
                        contentStart - 1,
                        _position - contentStart + 1,
                        line,
                        column);
                }
                var escaped = Advance();
                if (escaped is '\\' or '\'' or '"' or 'a' or '0' or 'b' or 'f' or 'n' or 'r' or 't' or 'v' or '`' or '$' or '{' or '}')
                {
                    decodedLength++;
                    continue;
                }
                if (escaped is 'u' or 'x')
                {
                    var digits = escaped == 'u' ? 4 : 2;
                    for (var offset = 0; offset < digits; offset++)
                    {
                        if (AtEnd || !IsHexDigit(Peek()))
                        {
                            return Bad(
                                escaped == 'u' ? TypedDocumentScanError.InvalidUnicodeEscape : TypedDocumentScanError.InvalidHexEscape,
                                contentStart - 1,
                                _position - contentStart + 1,
                                line,
                                column);
                        }
                        Advance();
                    }
                    decodedLength++;
                    continue;
                }

                return Bad(
                    TypedDocumentScanError.InvalidEscape,
                    contentStart - 1,
                    _position - contentStart + 1,
                    line,
                    column,
                    escaped);
            }

            return Bad(
                TypedDocumentScanError.UnterminatedString,
                contentStart - 1,
                _position - contentStart + 1,
                line,
                column);
        }

        private bool AtEnd => _position >= _source.Length;

        private bool ScanDecimalDigits(ref bool hasSeparators, bool requireDigit = true)
        {
            var sawDigit = false;
            var previousWasSeparator = false;
            while (!AtEnd)
            {
                var current = Peek();
                if (IsDecimalDigit(current))
                {
                    sawDigit = true;
                    previousWasSeparator = false;
                    Advance();
                    continue;
                }
                if (current != '_') break;

                hasSeparators = true;
                previousWasSeparator = true;
                Advance();
                if (!sawDigit || AtEnd || !IsDecimalDigit(Peek())) return false;
            }
            return (!requireDigit || sawDigit) && !previousWasSeparator;
        }

        private static bool TryParseDecimal(
            ReadOnlySpan<char> source,
            bool hasSeparators,
            out double value)
        {
            if (!hasSeparators)
            {
                if (source.IndexOfAny('.', 'e', 'E') < 0)
                {
                    var start = source.Length != 0 && source[0] == '-' ? 1 : 0;
                    var result = 0d;
                    for (var index = start; index < source.Length; index++)
                    {
                        result = (result * 10d) + (source[index] - '0');
                        if (!double.IsFinite(result))
                        {
                            value = 0;
                            return false;
                        }
                    }
                    value = start == 0 ? result : -result;
                    return true;
                }
                return double.TryParse(
                    source,
                    NumberStyles.Float,
                    CultureInfo.InvariantCulture,
                    out value);
            }

            char[] rented = null;
            Span<char> clean = source.Length <= 128
                ? stackalloc char[source.Length]
                : (rented = ArrayPool<char>.Shared.Rent(source.Length));
            var length = 0;
            for (var index = 0; index < source.Length; index++)
            {
                var current = source[index];
                if (current != '_') clean[length++] = current;
            }
            var parsed = double.TryParse(
                clean[..length],
                NumberStyles.Float,
                CultureInfo.InvariantCulture,
                out value);
            if (rented != null) ArrayPool<char>.Shared.Return(rented);
            return parsed;
        }

        private char Peek(int offset = 0)
        {
            var index = _position + offset;
            return (uint)index < (uint)_source.Length ? _source[index] : '\0';
        }

        private char Advance()
        {
            var current = _source[_position++];
            if (current == '\r')
            {
                if (!AtEnd && Peek() == '\n') _position++;
                _line++;
                _column = 1;
            }
            else if (current == '\n')
            {
                _line++;
                _column = 1;
            }
            else
            {
                _column++;
            }
            return current;
        }

        private bool IsNumberStart(char value)
        {
            return IsDecimalDigit(value) || (value == '-' && IsDecimalDigit(Peek(1)));
        }

        private static bool IsDecimalDigit(char value) => value is >= '0' and <= '9';

        internal static bool IsIdentifierStart(char value)
        {
            return value is >= 'a' and <= 'z' or >= 'A' and <= 'Z' or '_' or '$' ||
                   value is >= '\u4e00' and <= '\u9fbb';
        }

        internal static bool IsIdentifierPart(char value)
        {
            return IsIdentifierStart(value) || value is >= '0' and <= '9';
        }

        private static bool IsHexDigit(char value)
        {
            return value is >= '0' and <= '9' or >= 'a' and <= 'f' or >= 'A' and <= 'F';
        }

        private static bool TryHexValue(char value, out int result)
        {
            if (value is >= '0' and <= '9')
            {
                result = value - '0';
                return true;
            }
            if (value is >= 'a' and <= 'f')
            {
                result = value - 'a' + 10;
                return true;
            }
            if (value is >= 'A' and <= 'F')
            {
                result = value - 'A' + 10;
                return true;
            }
            result = 0;
            return false;
        }

        private static TypedDocumentToken Token(
            TypedDocumentTokenKind kind,
            int start,
            int length,
            int line,
            int column)
        {
            return new TypedDocumentToken(kind, start, length, line, column);
        }

        private static TypedDocumentToken Bad(
            TypedDocumentScanError error,
            int start,
            int length,
            int line,
            int column,
            char errorCharacter = '\0')
        {
            return new TypedDocumentToken(
                TypedDocumentTokenKind.Bad,
                start,
                length,
                line,
                column,
                error: error,
                errorCharacter: errorCharacter);
        }
    }
}
