using System;
using System.Buffers;
using System.Globalization;

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
