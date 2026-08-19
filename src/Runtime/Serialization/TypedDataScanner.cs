using System;
using System.Buffers;
using System.Globalization;

namespace AuroraScript.Runtime.Serialization
{
    internal enum TypedDataTokenKind : byte
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

    internal enum TypedDataScanError : byte
    {
        None,
        UnexpectedCharacter,
        DataMarkerNotAllowed,
        UnterminatedString,
        UnterminatedComment,
        InvalidEscape,
        InvalidUnicodeEscape,
        InvalidHexEscape,
        InvalidNumber
    }

    internal readonly struct TypedDataToken
    {
        internal TypedDataToken(
            TypedDataTokenKind kind,
            int start,
            int length,
            int line,
            int column,
            double number = 0,
            int decodedLength = 0,
            bool hasEscapes = false,
            TypedDataScanError error = TypedDataScanError.None,
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

        internal TypedDataTokenKind Kind { get; }
        internal int Start { get; }
        internal int Length { get; }
        internal int Line { get; }
        internal int Column { get; }
        internal double Number { get; }
        internal int DecodedLength { get; }
        internal bool HasEscapes { get; }
        internal TypedDataScanError Error { get; }
        internal char ErrorCharacter { get; }
    }

    internal struct TypedDataScanner
    {
        private readonly string _source;
        private int _position;
        private int _line;
        private int _column;

        internal TypedDataScanner(string source)
        {
            _source = source ?? string.Empty;
            _position = 0;
            _line = 1;
            _column = 1;
        }

        internal TypedDataToken Read()
        {
            if (!SkipTrivia(out var triviaError))
            {
                return triviaError;
            }
            if (AtEnd)
            {
                return Token(TypedDataTokenKind.EndOfFile, _position, 0, _line, _column);
            }

            var start = _position;
            var line = _line;
            var column = _column;
            var current = Peek();
            switch (current)
            {
                case '[': Advance(); return Token(TypedDataTokenKind.LeftBracket, start, 1, line, column);
                case ']': Advance(); return Token(TypedDataTokenKind.RightBracket, start, 1, line, column);
                case '{': Advance(); return Token(TypedDataTokenKind.LeftBrace, start, 1, line, column);
                case '}': Advance(); return Token(TypedDataTokenKind.RightBrace, start, 1, line, column);
                case ',': Advance(); return Token(TypedDataTokenKind.Comma, start, 1, line, column);
                case '\'':
                case '"':
                    return ScanString();
                case '@':
                    Advance();
                    return Bad(TypedDataScanError.DataMarkerNotAllowed, start, 1, line, column, current);
            }

            if (IsNumberStart(current)) return ScanNumber();
            if (IsIdentifierStart(current)) return ScanIdentifier();

            Advance();
            return Bad(TypedDataScanError.UnexpectedCharacter, start, 1, line, column, current);
        }

        internal bool TextEquals(in TypedDataToken token, string value)
        {
            return _source.AsSpan(token.Start, token.Length).SequenceEqual(value);
        }

        internal string GetIdentifier(in TypedDataToken token)
        {
            return new string(_source.AsSpan(token.Start, token.Length));
        }

        internal string GetString(in TypedDataToken token)
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

        internal bool TryGetInt64Exact(in TypedDataToken token, out long value)
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

        private static void DecodeString(Span<char> destination, string source, int start, int length)
        {
            var input = source.AsSpan(start, length);
            var sourceIndex = 0;
            var destinationIndex = 0;
            while (sourceIndex < input.Length)
            {
                var current = input[sourceIndex++];
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

        private static bool TryParseInt64Exact(ReadOnlySpan<char> source, out long value)
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

        private bool SkipTrivia(out TypedDataToken error)
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
                        TypedDataScanError.UnterminatedComment,
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

        private TypedDataToken ScanIdentifier()
        {
            var start = _position;
            var line = _line;
            var column = _column;
            Advance();
            while (!AtEnd && IsIdentifierPart(Peek())) Advance();
            var length = _position - start;
            var kind = length switch
            {
                4 when _source.AsSpan(start, length).SequenceEqual("null") => TypedDataTokenKind.Null,
                4 when _source.AsSpan(start, length).SequenceEqual("true") => TypedDataTokenKind.True,
                5 when _source.AsSpan(start, length).SequenceEqual("false") => TypedDataTokenKind.False,
                8 when _source.AsSpan(start, length).SequenceEqual("readonly") => TypedDataTokenKind.ReadOnly,
                _ => TypedDataTokenKind.Identifier
            };
            return Token(kind, start, length, line, column);
        }

        private TypedDataToken ScanNumber()
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
                    return Bad(TypedDataScanError.InvalidNumber, start, length, line, column);
                }
                var number = (double)integer;
                return new TypedDataToken(
                    TypedDataTokenKind.Number,
                    start,
                    length,
                    line,
                    column,
                    negative ? -number : number);
            }

            if (!ScanDecimalDigits(ref hasSeparators))
            {
                return Bad(TypedDataScanError.InvalidNumber, start, _position - start, line, column);
            }
            if (!AtEnd && Peek() == '.')
            {
                Advance();
                if (!AtEnd && Peek() == '_')
                {
                    Advance();
                    return Bad(TypedDataScanError.InvalidNumber, start, _position - start, line, column);
                }
                if (!ScanDecimalDigits(ref hasSeparators, requireDigit: false))
                {
                    return Bad(TypedDataScanError.InvalidNumber, start, _position - start, line, column);
                }
            }
            if (!AtEnd && Peek() is 'e' or 'E')
            {
                Advance();
                if (!AtEnd && Peek() is '+' or '-') Advance();
                if (!ScanDecimalDigits(ref hasSeparators))
                {
                    return Bad(
                        TypedDataScanError.InvalidNumber,
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
                return Bad(TypedDataScanError.InvalidNumber, start, tokenLength, line, column);
            }
            return new TypedDataToken(TypedDataTokenKind.Number, start, tokenLength, line, column, value);
        }

        private TypedDataToken ScanString()
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
                    return new TypedDataToken(
                        TypedDataTokenKind.String,
                        contentStart,
                        contentLength,
                        line,
                        column,
                        decodedLength: decodedLength,
                        hasEscapes: hasEscapes);
                }
                if (current is '\r' or '\n')
                {
                    var isCrLf = current == '\r' && Peek(1) == '\n';
                    Advance();
                    decodedLength += isCrLf ? 2 : 1;
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
                        TypedDataScanError.UnterminatedString,
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
                                escaped == 'u' ? TypedDataScanError.InvalidUnicodeEscape : TypedDataScanError.InvalidHexEscape,
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
                    TypedDataScanError.InvalidEscape,
                    contentStart - 1,
                    _position - contentStart + 1,
                    line,
                    column,
                    escaped);
            }

            return Bad(
                TypedDataScanError.UnterminatedString,
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

        private static TypedDataToken Token(
            TypedDataTokenKind kind,
            int start,
            int length,
            int line,
            int column)
        {
            return new TypedDataToken(kind, start, length, line, column);
        }

        private static TypedDataToken Bad(
            TypedDataScanError error,
            int start,
            int length,
            int line,
            int column,
            char errorCharacter = '\0')
        {
            return new TypedDataToken(
                TypedDataTokenKind.Bad,
                start,
                length,
                line,
                column,
                error: error,
                errorCharacter: errorCharacter);
        }
    }
}
