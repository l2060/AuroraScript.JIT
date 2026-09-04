using AuroraScript.Common;
using AuroraScript.Tokens;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace AuroraScript.Compiler.Syntax
{
    internal sealed class AuroraSyntaxScanner
    {
        private readonly string _source;
        private readonly string _fileName;
        private int _offset;
        private int _line = 1;
        private int _column = 1;
        private bool _eofRead;
        private bool _hasPreviousSignificantToken;
        private SyntaxTokenKind _previousSignificantKind;
        private int _previousSignificantSymbolId = -1;

        public AuroraSyntaxScanner(string source, string fileName = "")
        {
            _source = source ?? string.Empty;
            _fileName = fileName ?? string.Empty;
        }

        public int Line => _line;
        public int Column => _column;
        public int Offset => _offset;

        public bool TryRead(out SyntaxElement element)
        {
            if (_offset >= _source.Length)
            {
                if (_eofRead)
                {
                    element = default;
                    return false;
                }

                _eofRead = true;
                element = SyntaxElement.FromToken(new SyntaxToken(
                    SyntaxTokenKind.EndOfFile,
                    _offset,
                    0,
                    _line,
                    _column,
                    _line,
                    _column,
                    Symbols.KW_EOF.Id));
                return true;
            }

            var startOffset = _offset;
            var startLine = _line;
            var startColumn = _column;
            var span = _source.AsSpan(_offset);
            if (!TryScan(span, out var result))
            {
                throw new AuroraCompilationException(AuroraCompilationStage.Lexing, _fileName, _line, _column, "Invalid keywords 。");
            }

            Advance(result.Length);
            if (result.IsTrivia)
            {
                element = SyntaxElement.FromTrivia(new SyntaxTrivia(
                    result.TriviaKind,
                    startOffset,
                    result.Length,
                    startLine,
                    startColumn,
                    _line,
                    _column));
                return true;
            }

            var token = new SyntaxToken(
                result.TokenKind,
                startOffset,
                result.Length,
                startLine,
                startColumn,
                _line,
                _column,
                result.SymbolId);
            _hasPreviousSignificantToken = true;
            _previousSignificantKind = token.Kind;
            _previousSignificantSymbolId = token.SymbolId;
            element = SyntaxElement.FromToken(token);
            return true;
        }

        public static List<SyntaxElement> ScanAll(string source, string fileName = "")
        {
            var scanner = new AuroraSyntaxScanner(source, fileName);
            var elements = new List<SyntaxElement>();
            while (scanner.TryRead(out var element))
            {
                elements.Add(element);
                if (element.IsToken && element.Token.Kind == SyntaxTokenKind.EndOfFile)
                {
                    break;
                }
            }

            return elements;
        }

        private bool TryScan(ReadOnlySpan<char> span, out ScanResult result)
        {
            result = default;
            var c = span[0];

            if (c == ' ' || c == '\t')
            {
                return ScanWhiteSpace(span, out result);
            }

            if (c == '\n' || c == '\r')
            {
                return ScanNewLine(span, out result);
            }

            if (c == '/')
            {
                if (span.Length >= 2)
                {
                    var next = span[1];
                    if (next == '/') return ScanLineComment(span, out result);
                    if (next == '*') return ScanBlockComment(span, out result);
                }

                if (ShouldParseRegexLiteral() && ScanRegex(span, out result))
                {
                    return true;
                }

                return ScanPunctuator(span, out result);
            }

            if (c == '|' && span.Length > 1 && span[1] == '>')
            {
                return ScanStringBlock(span, out result);
            }

            if (c == '"' || c == '\'' || c == '`')
            {
                return ScanString(span, out result);
            }

            if (c == '0' && span.Length > 1 && (span[1] == 'x' || span[1] == 'X'))
            {
                if (ScanHexNumber(span, out result))
                {
                    return true;
                }

                result = ScanResult.Trivia(SyntaxTriviaKind.SkippedText, Math.Min(2, span.Length));
                return true;
            }

            if (IsDigit(c))
            {
                return ScanNumber(span, out result);
            }

            if (IsIdentifierStart(c))
            {
                return ScanIdentifier(span, out result);
            }

            if (IsPunctuatorStart(c))
            {
                return ScanPunctuator(span, out result);
            }

            result = ScanResult.Trivia(SyntaxTriviaKind.SkippedText, 1);
            return true;
        }

        private static bool ScanWhiteSpace(ReadOnlySpan<char> span, out ScanResult result)
        {
            var index = 0;
            while (index < span.Length && (span[index] == ' ' || span[index] == '\t'))
            {
                index++;
            }

            result = ScanResult.Trivia(SyntaxTriviaKind.WhiteSpace, index);
            return index > 0;
        }

        private static bool ScanNewLine(ReadOnlySpan<char> span, out ScanResult result)
        {
            var length = span[0] == '\r' && span.Length > 1 && span[1] == '\n' ? 2 : 1;
            result = ScanResult.Trivia(SyntaxTriviaKind.NewLine, length);
            return true;
        }

        private static bool ScanLineComment(ReadOnlySpan<char> span, out ScanResult result)
        {
            var i = 2;
            while (i < span.Length && span[i] != '\n' && span[i] != '\r')
            {
                i++;
            }

            result = ScanResult.Trivia(SyntaxTriviaKind.LineComment, i);
            return true;
        }

        private static bool ScanBlockComment(ReadOnlySpan<char> span, out ScanResult result)
        {
            for (var i = 2; i < span.Length - 1; i++)
            {
                if (span[i] == '*' && span[i + 1] == '/')
                {
                    result = ScanResult.Trivia(SyntaxTriviaKind.BlockComment, i + 2);
                    return true;
                }
            }

            result = ScanResult.Trivia(SyntaxTriviaKind.BlockComment, span.Length);
            return true;
        }

        private static bool ScanIdentifier(ReadOnlySpan<char> span, out ScanResult result)
        {
            var i = 1;
            while (i < span.Length && IsIdentifierPart(span[i]))
            {
                i++;
            }

            var symbol = Symbols.FromSpan(span.Slice(0, i));
            result = symbol == null
                ? ScanResult.Token(SyntaxTokenKind.Identifier, i)
                : ScanResult.Token(ToTokenKind(symbol.Type), i, symbol.Id);
            return true;
        }

        private static bool ScanHexNumber(ReadOnlySpan<char> span, out ScanResult result)
        {
            var i = 2;
            while (i < span.Length)
            {
                var c = span[i];
                if ((c >= 'a' && c <= 'f') || (c >= 'A' && c <= 'F') || (c >= '0' && c <= '9'))
                {
                    i++;
                }
                else
                {
                    break;
                }
            }

            if (i <= 2)
            {
                result = default;
                return false;
            }

            if (NumericLiteralFacts.TryConsumeSuffix(
                    span,
                    i,
                    hexadecimal: true,
                    out _,
                    out var suffixLength))
            {
                i += suffixLength;
            }

            result = ScanResult.Token(SyntaxTokenKind.Number, i);
            return true;
        }

        private static bool ScanNumber(ReadOnlySpan<char> span, out ScanResult result)
        {
            var dot = -1;
            var lastChar = span[0];
            var i = 1;
            for (; i < span.Length; i++)
            {
                var c = span[i];
                if (IsDigit(c))
                {
                }
                else if (c == '_')
                {
                    if (lastChar == '.' || lastChar == '_')
                    {
                        result = default;
                        return false;
                    }
                }
                else if (c == '.')
                {
                    if (lastChar == '.' || lastChar == '_')
                    {
                        result = default;
                        return false;
                    }

                    if (dot > -1)
                    {
                        break;
                    }

                    dot = i;
                }
                else
                {
                    break;
                }

                lastChar = c;
            }

            if (lastChar == '_')
            {
                result = default;
                return false;
            }

            if (NumericLiteralFacts.TryConsumeSuffix(
                    span,
                    i,
                    hexadecimal: false,
                    out var suffix,
                    out var suffixLength) &&
                (suffix == NumericLiteralSuffix.Number || dot < 0))
            {
                i += suffixLength;
            }

            result = ScanResult.Token(SyntaxTokenKind.Number, i);
            return true;
        }

        private static bool ScanPunctuator(ReadOnlySpan<char> span, out ScanResult result)
        {
            var c0 = span[0];
            var length = 0;

            if (span.Length >= 3)
            {
                var c1 = span[1];
                var c2 = span[2];
                if (c0 == '.' && c1 == '.' && c2 == '.')
                {
                    length = 3;
                }
                else if (c0 == '>' && c1 == '>' && c2 == '>')
                {
                    length = 3;
                }
            }

            if (length == 0 && span.Length >= 2)
            {
                var c1 = span[1];
                switch (c0)
                {
                    case '+': if (c1 == '=' || c1 == '+') length = 2; break;
                    case '-': if (c1 == '=' || c1 == '-') length = 2; break;
                    case '*': if (c1 == '=') length = 2; break;
                    case '/': if (c1 == '=') length = 2; break;
                    case '%': if (c1 == '=') length = 2; break;
                    case '=': if (c1 == '=' || c1 == '>') length = 2; break;
                    case '!': if (c1 == '=') length = 2; break;
                    case '>': if (c1 == '=' || c1 == '>') length = 2; break;
                    case '<': if (c1 == '=' || c1 == '<') length = 2; break;
                    case '|': if (c1 == '|') length = 2; break;
                    case '&': if (c1 == '&') length = 2; break;
                }
            }

            if (length == 0 && IsPunctuatorStart(c0))
            {
                length = 1;
            }

            if (length == 0)
            {
                result = default;
                return false;
            }

            var symbol = Symbols.FromSpan(span.Slice(0, length));
            result = symbol == null
                ? ScanResult.Token(SyntaxTokenKind.Punctuator, length)
                : ScanResult.Token(ToTokenKind(symbol.Type), length, symbol.Id);
            return true;
        }

        private bool ScanString(ReadOnlySpan<char> span, out ScanResult result)
        {
            var quote = span[0];
            var interpolationDepth = 0;

            for (var i = 1; i < span.Length; i++)
            {
                var c = span[i];
                if (c == '\\')
                {
                    if (i + 1 >= span.Length)
                    {
                        break;
                    }

                    i++;
                    continue;
                }

                if (quote == '`')
                {
                    if (c == '$' && i + 1 < span.Length && span[i + 1] == '{')
                    {
                        interpolationDepth++;
                        i++;
                        continue;
                    }

                    if (interpolationDepth > 0)
                    {
                        if (c == '{')
                        {
                            interpolationDepth++;
                        }
                        else if (c == '}')
                        {
                            interpolationDepth--;
                        }

                        continue;
                    }
                }

                if (c == quote)
                {
                    result = ScanResult.Token(quote == '`' ? SyntaxTokenKind.StringTemplate : SyntaxTokenKind.String, i + 1);
                    return true;
                }
            }

            result = ScanResult.Token(quote == '`' ? SyntaxTokenKind.StringTemplate : SyntaxTokenKind.String, span.Length);
            return true;
        }

        private static bool ScanStringBlock(ReadOnlySpan<char> span, out ScanResult result)
        {
            if (span.Length < 3 || span[2] != ' ')
            {
                result = ScanResult.Trivia(SyntaxTriviaKind.SkippedText, Math.Min(2, span.Length));
                return true;
            }

            var i = 3;

            while (i < span.Length)
            {
                var c = span[i];
                if (c == '\r')
                {
                    i++;
                    continue;
                }

                if (c == '\n')
                {
                    i++;
                    while (i < span.Length)
                    {
                        var next = span[i];
                        if (next == ' ' || next == '\t')
                        {
                            i++;
                        }
                        else
                        {
                            break;
                        }
                    }

                    if (i + 2 < span.Length && span[i] == '|' && span[i + 1] == '>' && span[i + 2] == ' ')
                    {
                        i += 3;
                        continue;
                    }

                    result = ScanResult.Token(SyntaxTokenKind.StringBlock, i);
                    return true;
                }

                i++;
            }

            result = ScanResult.Token(SyntaxTokenKind.StringBlock, i);
            return true;
        }

        private static bool ScanRegex(ReadOnlySpan<char> span, out ScanResult result)
        {
            if (span.Length < 2)
            {
                result = default;
                return false;
            }

            var lookahead = span[1];
            if (lookahead == '/' || lookahead == '*' || lookahead == '=')
            {
                result = default;
                return false;
            }

            var inCharacterClass = false;
            var escaped = false;

            for (var i = 1; i < span.Length; i++)
            {
                var current = span[i];
                if (current == '\n' || current == '\r')
                {
                    result = default;
                    return false;
                }

                if (escaped)
                {
                    escaped = false;
                    continue;
                }

                if (current == '\\')
                {
                    escaped = true;
                    continue;
                }

                if (current == '[')
                {
                    inCharacterClass = true;
                    continue;
                }

                if (current == ']' && inCharacterClass)
                {
                    inCharacterClass = false;
                    continue;
                }

                if (current == '/' && !inCharacterClass)
                {
                    var literalLength = i + 1;
                    var flagsLength = 0;
                    while (literalLength + flagsLength < span.Length && IsRegexFlag(span[literalLength + flagsLength]))
                    {
                        flagsLength++;
                    }

                    result = ScanResult.Token(SyntaxTokenKind.Regex, literalLength + flagsLength);
                    return true;
                }
            }

            result = default;
            return false;
        }

        private void Advance(int length)
        {
            var end = _offset + length;
            while (_offset < end)
            {
                var c = _source[_offset++];
                if (c == '\r')
                {
                    if (_offset < end && _source[_offset] == '\n')
                    {
                        _offset++;
                    }

                    _line++;
                    _column = 1;
                }
                else if (c == '\n')
                {
                    _line++;
                    _column = 1;
                }
                else
                {
                    _column++;
                }
            }
        }

        private bool ShouldParseRegexLiteral()
        {
            if (!_hasPreviousSignificantToken)
            {
                return true;
            }

            if (_previousSignificantKind == SyntaxTokenKind.Keyword)
            {
                return true;
            }

            if (_previousSignificantKind == SyntaxTokenKind.Operator)
            {
                return _previousSignificantSymbolId != Symbols.OP_INCREMENT.Id &&
                    _previousSignificantSymbolId != Symbols.OP_DECREMENT.Id;
            }

            if (_previousSignificantKind == SyntaxTokenKind.Punctuator)
            {
                return _previousSignificantSymbolId != Symbols.PT_RIGHTPARENTHESIS.Id &&
                    _previousSignificantSymbolId != Symbols.PT_RIGHTBRACKET.Id &&
                    _previousSignificantSymbolId != Symbols.PT_RIGHTBRACE.Id &&
                    _previousSignificantSymbolId != Symbols.PT_DOT.Id;
            }

            return false;
        }

        private static SyntaxTokenKind ToTokenKind(SymbolTypes symbolType)
        {
            return symbolType switch
            {
                SymbolTypes.KeyWord => SyntaxTokenKind.Keyword,
                SymbolTypes.Punctuator => SyntaxTokenKind.Punctuator,
                SymbolTypes.Operator => SyntaxTokenKind.Operator,
                SymbolTypes.NullValue => SyntaxTokenKind.Null,
                SymbolTypes.BooleanValue => SyntaxTokenKind.Boolean,
                SymbolTypes.Identifier => SyntaxTokenKind.Identifier,
                _ => SyntaxTokenKind.Identifier
            };
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool IsIdentifierStart(char c)
        {
            return (c >= 'a' && c <= 'z') ||
                   (c >= 'A' && c <= 'Z') ||
                   c == '_' ||
                   c == '$' ||
                   (c >= 0x4e00 && c <= 0x9fbb);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool IsIdentifierPart(char c)
        {
            return IsIdentifierStart(c) || (c >= '0' && c <= '9');
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool IsDigit(char c)
        {
            return c >= '0' && c <= '9';
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool IsPunctuatorStart(char c)
        {
            return c is '.' or '>' or '+' or '*' or '-' or '/' or '=' or '%' or '<' or ',' or ';' or ':' or '?' or '!' or '^' or '{' or '}' or '[' or ']' or '(' or ')' or '|' or '~' or '&' or '@';
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool CanEscape(char c, out char escaped)
        {
            switch (c)
            {
                case 'a': escaped = '\a'; return true;
                case 'b': escaped = '\b'; return true;
                case 'f': escaped = '\f'; return true;
                case 'n': escaped = '\n'; return true;
                case 'r': escaped = '\r'; return true;
                case 't': escaped = '\t'; return true;
                case 'v': escaped = '\v'; return true;
                case '0': escaped = '\0'; return true;
                case '\\': escaped = '\\'; return true;
                case '\'': escaped = '\''; return true;
                case '"': escaped = '"'; return true;
                case '`': escaped = '`'; return true;
                case '$': escaped = '$'; return true;
                case '{': escaped = '{'; return true;
                case '}': escaped = '}'; return true;
                default: escaped = '\0'; return false;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool IsRegexFlag(char c)
        {
            return c is 'g' or 'i' or 'm' or 'u' or 'y';
        }

        private readonly struct ScanResult
        {
            private ScanResult(
                bool isTrivia,
                SyntaxTokenKind tokenKind,
                SyntaxTriviaKind triviaKind,
                int length,
                int symbolId)
            {
                IsTrivia = isTrivia;
                TokenKind = tokenKind;
                TriviaKind = triviaKind;
                Length = length;
                SymbolId = symbolId;
            }

            public bool IsTrivia { get; }
            public SyntaxTokenKind TokenKind { get; }
            public SyntaxTriviaKind TriviaKind { get; }
            public int Length { get; }
            public int SymbolId { get; }

            public static ScanResult Token(SyntaxTokenKind kind, int length, int symbolId = -1)
            {
                return new ScanResult(false, kind, default, length, symbolId);
            }

            public static ScanResult Trivia(SyntaxTriviaKind kind, int length)
            {
                return new ScanResult(true, default, kind, length, -1);
            }
        }
    }
}
