using AuroraScript.Common;
using System;
using System.Runtime.CompilerServices;
using System.Text;

namespace AuroraScript.Scanning
{
    internal struct RuleTestResult
    {
        public Boolean Success;
        public Int32 LineCount;
        public Int32 ColumnNumber;
        public String Value;
        public Int32 Length;
        public Int32 Offset;
        public TokenTyped Type;
    }

    internal abstract class TokenRules
    {
        public static readonly TokenRules NewLine = new NewLineRule();
        public static readonly TokenRules WhiteSpace = new WhiteSpaceRule();
        public static readonly TokenRules RowComment = new RowCommentRule();
        public static readonly TokenRules BlockComment = new BlockCommentRule();
        public static readonly TokenRules HexNumber = new HexNumberCommentRule();
        public static readonly TokenRules Number = new NumberCommentRule();
        public static readonly TokenRules StringTemplate = new StringTemplateRule();
        public static readonly TokenRules Identifier = new IdentifierRule();
        public static readonly TokenRules Punctuator = new PunctuatorRule();
        public static readonly TokenRules StringBlock = new StringBlockRule();
        public static readonly TokenRules RegexLiteral = new RegexRule();

        private static readonly bool[] _IdStart = new bool[128];
        private static readonly bool[] _IdPart = new bool[128];
        private static readonly bool[] _IsPunc = new bool[128];

        static TokenRules()
        {
            for (char c = 'a'; c <= 'z'; c++) { _IdStart[c] = _IdPart[c] = true; }
            for (char c = 'A'; c <= 'Z'; c++) { _IdStart[c] = _IdPart[c] = true; }
            for (char c = '0'; c <= '9'; c++) { _IdPart[c] = true; }
            _IdStart['_'] = _IdPart['_'] = true;
            _IdStart['$'] = _IdPart['$'] = true;

            string puncs = "+-*/=%<>. ,;:?!^{}[]()|~&@";
            foreach (char c in puncs) { _IsPunc[c] = true; }
        }

        public abstract RuleTestResult Test(in ReadOnlySpan<Char> codeSpan, in Int32 LineNumber, in Int32 ColumnNumber);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected bool IsIdStart(char c)
        {
            if (c < 128) return _IdStart[c];
            return c >= 0x4e00 && c <= 0x9fbb;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected bool IsIdPart(char c)
        {
            if (c < 128) return _IdPart[c];
            return c >= 0x4e00 && c <= 0x9fbb;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected bool IsPunc(char c) => c < 128 && _IsPunc[c];

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected unsafe Boolean IsNumber(char lpChar)
        {
            return (lpChar >= '0' && lpChar <= '9');
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected unsafe Boolean IsChinese(char lpChar)
        {
            return (lpChar >= 0x4e00 && lpChar <= 0x9fbb);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected unsafe Boolean IsLetter(char lpChar)
        {
            return (lpChar >= 'a' && lpChar <= 'z') || (lpChar >= 'A' && lpChar <= 'Z');
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected unsafe Boolean canEscape(char lpChar, out char outchar)
        {
            switch (lpChar)
            {
                case 'a': outchar = '\a'; return true;
                case 'b': outchar = '\b'; return true;
                case 'f': outchar = '\f'; return true;
                case 'n': outchar = '\n'; return true;
                case 'r': outchar = '\r'; return true;
                case 't': outchar = '\t'; return true;
                case 'v': outchar = '\v'; return true;
                case '0': outchar = '\0'; return true;
                case '\\': outchar = '\\'; return true;
                case '\'': outchar = '\''; return true;
                case '"': outchar = '"'; return true;
                default: outchar = '\0'; return false;
            }
        }
    }

    internal class PunctuatorRule : TokenRules
    {
        public override RuleTestResult Test(in ReadOnlySpan<Char> codeSpan, in Int32 LineNumber, in Int32 ColumnNumber)
        {
            var result = new RuleTestResult();
            if (codeSpan.Length == 0) return result;

            char c0 = codeSpan[0];
            int length = 0;

            if (codeSpan.Length >= 3)
            {
                char c1 = codeSpan[1];
                char c2 = codeSpan[2];
                if (c0 == '.' && c1 == '.' && c2 == '.') length = 3;
                else if (c0 == '>' && c1 == '>' && c2 == '>') length = 3;
            }

            if (length == 0 && codeSpan.Length >= 2)
            {
                char c1 = codeSpan[1];
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

            if (length == 2 && ((c0 == '<' && codeSpan[1] == '<') || (c0 == '>' && codeSpan[1] == '>')))
            {
                // Bitwise shifts correctly handled above
            }

            if (length == 0 && IsPunc(c0))
            {
                length = 1;
            }

            if (length > 0)
            {
                result.ColumnNumber = ColumnNumber + length;
                result.Length = length;
                result.Value = codeSpan.Slice(0, length).ToString();
                result.Type = TokenTyped.Punctuator;
                result.Success = true;
            }

            return result;
        }
    }

    internal class IdentifierRule : TokenRules
    {
        public override unsafe RuleTestResult Test(in ReadOnlySpan<Char> codeSpan, in Int32 LineNumber, in Int32 ColumnNumber)
        {
            var result = new RuleTestResult();
            if (codeSpan.Length == 0 || !IsIdStart(codeSpan[0])) return result;

            int i = 1;
            while (i < codeSpan.Length && IsIdPart(codeSpan[i]))
            {
                i++;
            }

            result.ColumnNumber = ColumnNumber + i;
            result.Length = i;
            result.Value = codeSpan.Slice(0, i).ToString();
            result.Success = true;
            result.Type = TokenTyped.Identifier;
            return result;
        }
    }


    internal class StringTemplateRule : TokenRules
    {
        public override RuleTestResult Test(in ReadOnlySpan<Char> codeSpan, in Int32 LineNumber, in Int32 ColumnNumber)
        {
            var result = new RuleTestResult();
            if (codeSpan.Length == 0) return result;
            char keychar = codeSpan[0];
            if (keychar != '`' && keychar != '"' && keychar != '\'') return result;

            var sb = new StringBuilder();
            int currentColumn = ColumnNumber;
            int currentLineCount = 0;

            for (int i = 1; i < codeSpan.Length; i++)
            {
                char viewChar = codeSpan[i];
                if (viewChar == '\\')
                {
                    if (i + 1 >= codeSpan.Length) break;
                    if (!base.canEscape(codeSpan[i + 1], out viewChar))
                    {
                        throw new AuroraLexicalException("", LineNumber, currentColumn, "Unrecognizable escape characters");
                    }
                    currentColumn += 2;
                    i++;
                }
                else if (viewChar == '\n')
                {
                    currentColumn = 0;
                    currentLineCount += 1;
                }
                else
                {
                    currentColumn++;
                    if (viewChar == keychar)
                    {
                        result.ColumnNumber = currentColumn;
                        result.LineCount = currentLineCount;
                        result.Length = i + 1;
                        result.Value = sb.ToString();
                        result.Success = true;
                        result.Type = keychar == '`' ? TokenTyped.StringTemplate : TokenTyped.String;
                        return result;
                    }
                }
                sb.Append(viewChar);
            }
            return result;
        }
    }

    internal class CharReader
    {
        private int _current = 0;
        public Char Current(in ReadOnlySpan<Char> codeSpan)
        {
            return _current < codeSpan.Length ? codeSpan[_current] : '\0';
        }

        public Char Peek(in ReadOnlySpan<Char> codeSpan)
        {
            var pos = _current + 1;
            return pos < codeSpan.Length ? codeSpan[pos] : '\0';
        }

        public void Advance(int len = 1)
        {
            _current += len;
        }

        public int Length => _current;
    }

    internal class StringBlockRule : TokenRules
    {
        public override RuleTestResult Test(in ReadOnlySpan<Char> codeSpan, in Int32 LineNumber, in Int32 ColumnNumber)
        {
            var result = new RuleTestResult();
            if (codeSpan.Length > 2 && codeSpan[0] == '|' && codeSpan[1] == '>')
            {
                int currentLineCount = 0;
                int currentColumn = ColumnNumber + 2;
                var sb = new StringBuilder();
                var reader = new CharReader();
                reader.Advance(2);
                if (reader.Current(codeSpan) == ' ') { reader.Advance(); currentColumn++; }

                while (reader.Current(codeSpan) != '\0')
                {
                    char c = reader.Current(codeSpan);
                    if (c == '\r') { reader.Advance(); continue; }
                    if (c == '\n')
                    {
                        currentLineCount++;
                        currentColumn = 1;
                        sb.AppendLine();
                        reader.Advance();
                        while (true)
                        {
                            char cNext = reader.Current(codeSpan);
                            if (cNext == ' ' || cNext == '\t') { reader.Advance(); currentColumn++; }
                            else break;
                        }

                        if (reader.Current(codeSpan) == '|' && reader.Peek(codeSpan) == '>')
                        {
                            reader.Advance(2);
                            currentColumn += 2;
                            if (reader.Current(codeSpan) == ' ') { reader.Advance(); currentColumn++; }
                            continue;
                        }
                        else
                        {
                            result.LineCount = currentLineCount;
                            result.ColumnNumber = currentColumn;
                            result.Length = reader.Length;
                            result.Value = sb.ToString();
                            result.Success = true;
                            result.Type = TokenTyped.String;
                            return result;
                        }
                    }
                    sb.Append(c);
                    reader.Advance();
                    currentColumn++;
                }
                // Handle end of file without newline
                result.LineCount = currentLineCount;
                result.ColumnNumber = currentColumn;
                result.Length = reader.Length;
                result.Value = sb.ToString();
                result.Success = true;
                result.Type = TokenTyped.String;
                return result;
            }
            return result;
        }
    }

    internal class RegexRule : TokenRules
    {
        public override RuleTestResult Test(in ReadOnlySpan<Char> codeSpan, in Int32 LineNumber, in Int32 ColumnNumber)
        {
            var result = new RuleTestResult();
            if (codeSpan.Length < 2 || codeSpan[0] != '/') return result;

            char lookahead = codeSpan[1];
            if (lookahead == '/' || lookahead == '*' || lookahead == '=') return result;

            bool inCharacterClass = false;
            bool escaped = false;

            for (int i = 1; i < codeSpan.Length; i++)
            {
                char current = codeSpan[i];
                if (current == '\n' || current == '\r') return result;

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
                    int literalLength = i + 1;
                    int flagsLength = 0;
                    while (literalLength + flagsLength < codeSpan.Length)
                    {
                        char flagChar = codeSpan[literalLength + flagsLength];
                        if (!IsValidFlag(flagChar)) break;
                        flagsLength++;
                    }

                    int totalLength = literalLength + flagsLength;
                    result.ColumnNumber = ColumnNumber + totalLength;
                    result.Length = totalLength;
                    result.Value = codeSpan.Slice(0, totalLength).ToString();
                    result.Type = TokenTyped.Regex;
                    result.Success = true;
                    return result;
                }
            }
            return result;
        }

        private bool IsValidFlag(char c) => "gimuy".Contains(c);
    }

    internal class NumberCommentRule : TokenRules
    {
        public override RuleTestResult Test(in ReadOnlySpan<Char> codeSpan, in Int32 LineNumber, in Int32 ColumnNumber)
        {
            var result = new RuleTestResult();
            if (codeSpan.Length == 0) return result;

            char c0 = codeSpan[0];
            bool isNeg = c0 == '-';
            if (!isNeg && !IsNumber(c0)) return result;

            int dot = -1;
            char lastChar = c0;
            int i = 1;
            for (; i < codeSpan.Length; i++)
            {
                char c = codeSpan[i];
                if (IsNumber(c)) { }
                else if (c == '_')
                {
                    if (lastChar == '-' || lastChar == '.' || lastChar == '_') return result;
                }
                else if (c == '.')
                {
                    if (lastChar == '-' || lastChar == '.' || lastChar == '_') return result;
                    if (dot > -1) break;
                    dot = i;
                }
                else break;
                lastChar = c;
            }

            if (isNeg && i == 1) return result; // Just "-"
            if (lastChar == '_') return result; // Can't end with _

            result.ColumnNumber = ColumnNumber + i;
            result.Length = i;
            result.Value = codeSpan.Slice(0, i).ToString();
            result.Success = true;
            result.Type = TokenTyped.Number;
            return result;
        }
    }

    internal class HexNumberCommentRule : TokenRules
    {
        public override RuleTestResult Test(in ReadOnlySpan<Char> codeSpan, in Int32 LineNumber, in Int32 ColumnNumber)
        {
            var result = new RuleTestResult();
            if (codeSpan.Length > 2 && codeSpan[0] == '0' && (codeSpan[1] == 'x' || codeSpan[1] == 'X'))
            {
                int i = 2;
                while (i < codeSpan.Length)
                {
                    char c = codeSpan[i];
                    if ((c >= 'a' && c <= 'f') || (c >= 'A' && c <= 'F') || (c >= '0' && c <= '9')) i++;
                    else break;
                }
                if (i > 2)
                {
                    result.ColumnNumber = ColumnNumber + i;
                    result.Length = i;
                    result.Value = codeSpan.Slice(0, i).ToString();
                    result.Success = true;
                    result.Type = TokenTyped.Number;
                    return result;
                }
            }
            return result;
        }
    }

    internal class BlockCommentRule : TokenRules
    {
        public override RuleTestResult Test(in ReadOnlySpan<Char> codeSpan, in Int32 LineNumber, in Int32 ColumnNumber)
        {
            var result = new RuleTestResult();
            if (codeSpan.Length >= 2 && codeSpan[0] == '/' && codeSpan[1] == '*')
            {
                int currentColumn = ColumnNumber + 2;
                int currentLineCount = 0;
                for (int i = 2; i < codeSpan.Length - 1; i++)
                {
                    char c = codeSpan[i];
                    if (c == '\n')
                    {
                        currentColumn = 0;
                        currentLineCount++;
                    }
                    else
                    {
                        currentColumn++;
                        if (c == '*' && codeSpan[i + 1] == '/')
                        {
                            result.ColumnNumber = currentColumn + 1;
                            result.LineCount = currentLineCount;
                            result.Length = i + 2;
                            result.Value = codeSpan.Slice(0, i + 2).ToString();
                            result.Type = TokenTyped.Comment;
                            result.Success = true;
                            return result;
                        }
                    }
                }
            }
            return result;
        }
    }

    internal class RowCommentRule : TokenRules
    {
        public override RuleTestResult Test(in ReadOnlySpan<Char> codeSpan, in Int32 LineNumber, in Int32 ColumnNumber)
        {
            var result = new RuleTestResult();
            if (codeSpan.Length >= 2 && codeSpan[0] == '/' && codeSpan[1] == '/')
            {
                int i = 2;
                while (i < codeSpan.Length && codeSpan[i] != '\n') i++;

                bool hasNewLine = i < codeSpan.Length && codeSpan[i] == '\n';
                int length = hasNewLine ? i + 1 : i;

                result.ColumnNumber = hasNewLine ? 1 : ColumnNumber + length;
                result.LineCount = hasNewLine ? 1 : 0;
                result.Length = length;
                result.Value = codeSpan.Slice(0, length).ToString();
                result.Type = TokenTyped.Comment;
                result.Success = true;
            }
            return result;
        }
    }

    internal class WhiteSpaceRule : TokenRules
    {
        public override RuleTestResult Test(in ReadOnlySpan<Char> codeSpan, in Int32 LineNumber, in Int32 ColumnNumber)
        {
            var result = new RuleTestResult();
            int index = 0;
            while (index < codeSpan.Length && (codeSpan[index] == ' ' || codeSpan[index] == '\t')) index++;

            if (index > 0)
            {
                result.ColumnNumber = ColumnNumber + index;
                result.Length = index;
                result.Value = codeSpan.Slice(0, index).ToString();
                result.Type = TokenTyped.WhiteSpace;
                result.Success = true;
            }
            return result;
        }
    }

    internal class NewLineRule : TokenRules
    {
        public override RuleTestResult Test(in ReadOnlySpan<Char> codeSpan, in Int32 LineNumber, in Int32 ColumnNumber)
        {
            var result = new RuleTestResult();
            if (codeSpan.Length > 0 && codeSpan[0] == '\n')
            {
                result.Value = "\n";
                result.LineCount = 1;
                result.ColumnNumber = 1;
                result.Length = 1;
                result.Type = TokenTyped.NewLine;
                result.Success = true;
            }
            return result;
        }
    }
}