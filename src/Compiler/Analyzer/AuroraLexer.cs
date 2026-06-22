using AuroraScript.Common;
using AuroraScript.Core;
using AuroraScript.Tokens;
using System;
using System.Buffers;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;

namespace AuroraScript.Compiler.Analyzer
{
    internal class AuroraLexer : IDisposable
    {
        public Int32 LineNumber { get; private set; } = 1;
        public Int32 ColumnNumber { get; private set; } = 1;
        public Int32 Offset { get; private set; } = 0;

        public String FullPath { get; private set; }
        public String FileName { get; private set; }
        public String InputData { get; private set; }

        public String Directory { get; private set; }

        private enum LexTokenKind : byte
        {
            Identifier,
            Keyword,
            Punctuator,
            Operator,
            String,
            StringTemplate,
            Number,
            Regex,
            Boolean,
            Null,
            EndOfFile
        }

        private struct LexToken
        {
            private const int PayloadMask = 0x007fffff;
            private const int TextPayloadFlag = 0x00800000;

            public int Data;
            public int StartLine;
            public int StartColumn;
            public int Offset;
            public int Length;

            public readonly LexTokenKind Kind
            {
                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                get => (LexTokenKind)((uint)Data >> 24);
            }

            public readonly int SymbolId
            {
                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                get => (Data & TextPayloadFlag) == 0 ? (Data & PayloadMask) - 1 : -1;
            }

            public readonly int TextId
            {
                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                get => (Data & TextPayloadFlag) != 0 ? (Data & PayloadMask) - 1 : -1;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static int Pack(LexTokenKind kind, int payload = -1)
            {
                if ((uint)(payload + 1) > PayloadMask)
                {
                    throw new InvalidOperationException("Lexer token payload limit exceeded.");
                }

                return ((int)kind << 24) | (payload + 1);
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static int PackText(LexTokenKind kind, int textId)
            {
                if ((uint)(textId + 1) > PayloadMask)
                {
                    throw new InvalidOperationException("Lexer text table limit exceeded.");
                }

                return ((int)kind << 24) | TextPayloadFlag | (textId + 1);
            }
        }

        private sealed class LexTokenBuffer : IDisposable
        {
            private readonly List<LexToken[]> chunks = new List<LexToken[]>(1);
            private readonly int chunkShift;
            private readonly int chunkMask;
            private int count;

            public LexTokenBuffer(int chunkSize)
            {
                this.chunkShift = GetShift(chunkSize);
                this.chunkMask = chunkSize - 1;
                this.chunks.Add(ArrayPool<LexToken>.Shared.Rent(chunkSize));
            }

            public int Count => this.count;

            public LexToken this[int index]
            {
                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                get => this.chunks[index >> this.chunkShift][index & this.chunkMask];
            }

            public void Add(in LexToken token)
            {
                var chunkIndex = this.count >> this.chunkShift;
                if (chunkIndex == this.chunks.Count)
                {
                    this.chunks.Add(ArrayPool<LexToken>.Shared.Rent(this.chunkMask + 1));
                }

                this.chunks[chunkIndex][this.count & this.chunkMask] = token;
                this.count++;
            }

            public void Dispose()
            {
                for (int i = 0; i < this.chunks.Count; i++)
                {
                    ArrayPool<LexToken>.Shared.Return(this.chunks[i], clearArray: false);
                }
                this.chunks.Clear();
                this.count = 0;
            }

            private static int GetShift(int size)
            {
                var shift = 0;
                while ((1 << shift) < size) shift++;
                return shift;
            }
        }

        private LexTokenBuffer tokens;
        private Token cachedToken;
        private int cachedTokenIndex = -1;
        private List<string> tokenValues;
        private readonly Dictionary<string, int> _nameIds = new Dictionary<string, int>(StringComparer.Ordinal);
        private readonly List<string> _names = new List<string> { String.Empty };
        private Int32 readOffset { get; set; } = 0;
        private Int32 bufferLength { get; set; } = 0;
        public Int32 Position { get; private set; } = 0;

        public String BaseDirectory { get; private set; }

        private struct RuleTestResult
        {
            public Boolean Success;
            public Int32 LineCount;
            public Int32 ColumnNumber;
            public String Value;
            public Int32 Length;
            public Int32 Offset;
            public TokenTyped Type;
            public Symbols Symbol;
        }

        public readonly struct LexerSnapshot
        {
            public LexerSnapshot(int position, int lineNumber, int columnNumber)
            {
                Position = position;
                LineNumber = lineNumber;
                ColumnNumber = columnNumber;
            }

            public int Position { get; }
            public int LineNumber { get; }
            public int ColumnNumber { get; }
        }

        public AuroraLexer(String baseDirectory, ScriptSource source)
        {
            this.BaseDirectory = baseDirectory;
            this.Directory = Path.GetDirectoryName(source.FullPath);
            this.FullPath = source.FullPath;
            this.FileName = Path.GetFileName(source.FullPath);
            this.InputData = source.ReadSource();
            this.bufferLength = this.InputData.Length;
            this.tokens = new LexTokenBuffer(GetTokenChunkSize(this.bufferLength));
            this.ParseTokens();
            this.Position = 0;
        }

        private static int GetTokenChunkSize(int sourceLength)
        {
            if (sourceLength < 64) return 8;
            if (sourceLength < 1024) return 32;
            if (sourceLength < 8192) return 256;
            return 1024;
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
                default: escaped = '\0'; return false;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool IsRegexFlag(char c)
        {
            return c is 'g' or 'i' or 'm' or 'u' or 'y';
        }

        /// <summary>
        /// If it is the specified symbol, return, otherwise report an error
        /// </summary>
        /// <param name="symbol"></param>
        /// <returns></returns>
        /// <exception cref="InvalidOperationException"></exception>
        public Token NextOfKind(Symbols symbol)
        {
            var lexToken = this.tokens[this.Position];
            if (lexToken.SymbolId == symbol.Id)
            {
                return this.Next();
            }

            var token = this.Materialize(this.Position);
            throw new AuroraLexicalException(this.FullPath, token.LineNumber, token.ColumnNumber, $"The keyword {token.Value} appears in the wrong place, it should be {symbol.Name}.");
        }

        public SourceSpan NextRangeOfKind(Symbols symbol)
        {
            var lexToken = this.tokens[this.Position];
            if (lexToken.SymbolId != symbol.Id)
            {
                var token = this.Materialize(this.Position);
                throw new AuroraLexicalException(this.FullPath, token.LineNumber, token.ColumnNumber, $"The keyword {token.Value} appears in the wrong place, it should be {symbol.Name}.");
            }

            this.Position++;
            return this.CreateRange(in lexToken);
        }

        public SourceSpan NextRangeOfKind(Symbols symbol1, Symbols symbol2)
        {
            var lexToken = this.tokens[this.Position];
            if (lexToken.SymbolId != symbol1.Id && lexToken.SymbolId != symbol2.Id)
            {
                var token = this.Materialize(this.Position);
                throw new AuroraLexicalException(this.FullPath, token.LineNumber, token.ColumnNumber, $"The keyword {token.Value} appears in the wrong place, it should be {symbol1} or {symbol2}.");
            }

            this.Position++;
            return this.CreateRange(in lexToken);
        }

        public void Expect(Symbols symbol)
        {
            var lexToken = this.tokens[this.Position];
            if (lexToken.SymbolId == symbol.Id)
            {
                this.Position++;
                return;
            }

            var token = this.Materialize(this.Position);
            throw new AuroraLexicalException(this.FullPath, token.LineNumber, token.ColumnNumber, $"The keyword {token.Value} appears in the wrong place, it should be {symbol.Name}.");
        }

        public Token NextOfKind(params Symbols[] symbols)
        {
            var token = this.Next();
            for (int i = 0; i < symbols.Length; i++)
            {
                if (token.Symbol == symbols[i]) return token;
            }
            throw new AuroraLexicalException(this.FullPath, token.LineNumber, token.ColumnNumber, $"The keyword {token.Value} appears in the wrong place, it should be {String.Join(",", symbols.Select(s => s.ToString()))}.");
        }

        public Token NextOfKind(Symbols symbol1, Symbols symbol2)
        {
            var lexToken = this.tokens[this.Position];
            if (lexToken.SymbolId == symbol1.Id || lexToken.SymbolId == symbol2.Id)
            {
                return this.Next();
            }

            var token = this.Materialize(this.Position);
            throw new AuroraLexicalException(this.FullPath, token.LineNumber, token.ColumnNumber, $"The keyword {token.Value} appears in the wrong place, it should be {symbol1},{symbol2}.");
        }


        /// <summary>
        /// If it is the specified token, return, otherwise report an error
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        /// <exception cref="InvalidOperationException"></exception>
        public T NextOfKind<T>() where T : Token
        {
            var token = this.Next();
            if (token is T) return (T)token;
            throw new AuroraLexicalException(this.FullPath, token.LineNumber, token.ColumnNumber, $"Invalid or unexpected token “{token.Value}”");
        }

        public Token NextOfToken<T1, T2>() where T1 : Token where T2 : Token
        {
            var token = this.Next();
            if (token is T1) return token;
            if (token is T2) return token;
            throw new AuroraLexicalException(this.FullPath, token.LineNumber, token.ColumnNumber, $"Invalid or unexpected token “{token.Value}”");
        }


        public T TestNextOfKind<T>() where T : Token
        {
            var nextToken = this.LookAtHead();
            if (nextToken is T)
            {
                this.Next();
                return (T)nextToken;
            }
            return null;
        }


        public Boolean TestNextIn(Symbols[] endSymbols)
        {
            var nextToken = this.tokens[this.Position];
            for (int i = 0; i < endSymbols.Length; i++)
            {
                if (nextToken.SymbolId == endSymbols[i].Id)
                {
                    this.Position++;
                    return true;
                }
            }
            return false;
        }


        public Boolean TestAtHead<T>() where T : Token
        {
            var nextToken = this.LookAtHead();
            return nextToken is T;
        }

        /// <summary>
        /// If it is the specified symbol, take it out and return true, otherwise return false
        /// </summary>
        /// <param name="symbol"></param>
        /// <returns></returns>
        /// <exception cref="InvalidOperationException"></exception>
        public Boolean TestSymbol(Symbols symbol)
        {
            return this.tokens[this.Position].SymbolId == symbol.Id;
        }

        public Boolean IsAtEnd => this.tokens[this.Position].Kind == LexTokenKind.EndOfFile;

        internal int TokenCount => this.tokens?.Count ?? 0;

        public void Dispose()
        {
            this.tokens?.Dispose();
            this.tokens = null;
            this.cachedToken = null;
            this.cachedTokenIndex = -1;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Symbols PeekSymbol()
        {
            return Symbols.FromId(this.tokens[this.Position].SymbolId);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public SourceSpan PeekRange()
        {
            var token = this.tokens[this.Position];
            return this.CreateRange(in token);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public SourceSpan PreviousRange(Int32 offset = 2)
        {
            var token = this.tokens[this.Position - offset];
            return this.CreateRange(in token);
        }

        /// <summary>
        /// If it is the specified symbol, take it out and return true, otherwise return false
        /// </summary>
        /// <param name="symbol"></param>
        /// <returns></returns>
        /// <exception cref="InvalidOperationException"></exception>
        public Boolean TestNext(Symbols symbol)
        {
            if (this.tokens[this.Position].SymbolId == symbol.Id)
            {
                this.Position++;
                return true;
            }
            return false;
        }

        /// <summary>
        /// get next token without removing it.
        /// </summary>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Token LookAtHead()
        {
            return this.Materialize(this.Position);
        }



        /// <summary>
        /// get next token without removing it.
        /// </summary>
        /// <returns></returns>
        public Token Previous(Int32 offset = 2)
        {
            return this.Materialize(this.Position - offset);
        }

        /// <summary>
        /// get next token
        /// </summary>
        /// <returns></returns>
        public Token Next()
        {
            var token = this.Materialize(this.Position);
            this.Position++;
            return token;
        }

        public Symbols NextSymbol(out SourceSpan range)
        {
            var token = this.tokens[this.Position];
            this.Position++;
            range = this.CreateRange(in token);
            return Symbols.FromId(token.SymbolId);
        }

        public void RollBack()
        {
            this.Position--;
        }

        public LexerSnapshot CreateSnapshot()
        {
            return new LexerSnapshot(this.Position, this.LineNumber, this.ColumnNumber);
        }

        public void RestoreSnapshot(LexerSnapshot snapshot)
        {
            this.Position = snapshot.Position;
            this.LineNumber = snapshot.LineNumber;
            this.ColumnNumber = snapshot.ColumnNumber;
        }

        /// <summary>
        /// Parse all tokens
        /// </summary>
        private void ParseTokens()
        {
            while (true)
            {
                var token = this.ParseNext();
                this.tokens.Add(token);
                if (token.Kind == LexTokenKind.EndOfFile) return;
            }
        }

        private LexToken ParseNext()
        {
            while (true)
            {
                if (this.bufferLength <= 0)
                {
                    return new LexToken
                    {
                        Data = LexToken.Pack(LexTokenKind.EndOfFile, Symbols.KW_EOF.Id),
                        StartLine = this.LineNumber,
                        StartColumn = this.ColumnNumber,
                        Offset = this.Offset,
                        Length = 0
                    };
                }

                ReadOnlySpan<Char> span = this.InputData.AsSpan(this.readOffset, this.bufferLength);
                if (this.TryScanNext(span, out var result))
                {
                    if (result.Type == TokenTyped.Comment || result.Type == TokenTyped.NewLine || result.Type == TokenTyped.WhiteSpace)
                    {
                        this.Advance(in result);
                        continue;
                    }
                    result.Offset = this.Offset;
                    var token = this.CreateLexToken(result);
                    this.Advance(in result);
                    return token;
                }
                throw new AuroraLexicalException(this.FileName, this.LineNumber, this.ColumnNumber, "Invalid keywords 。");
            }
        }

        private bool TryScanNext(ReadOnlySpan<char> span, out RuleTestResult result)
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
                    if (next == '/') return ScanRowComment(span, out result);
                    if (next == '*') return ScanBlockComment(span, out result);
                }

                if (this.ShouldParseRegexLiteral() && ScanRegex(span, out result))
                {
                    return true;
                }

                return ScanPunctuator(span, out result);
            }

            if (c == '|' && span.Length > 2 && span[1] == '>')
            {
                return ScanStringBlock(span, out result);
            }

            if (c == '"' || c == '\'' || c == '`')
            {
                return ScanString(span, out result);
            }

            if (c == '0' && span.Length > 2 && (span[1] == 'x' || span[1] == 'X') && ScanHexNumber(span, out result))
            {
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

            return false;
        }

        private bool ScanWhiteSpace(ReadOnlySpan<char> span, out RuleTestResult result)
        {
            int index = 0;
            while (index < span.Length && (span[index] == ' ' || span[index] == '\t')) index++;

            result = new RuleTestResult
            {
                ColumnNumber = this.ColumnNumber + index,
                Length = index,
                Type = TokenTyped.WhiteSpace,
                Success = index > 0
            };
            return result.Success;
        }

        private bool ScanNewLine(ReadOnlySpan<char> span, out RuleTestResult result)
        {
            var length = (span[0] == '\r' && span.Length > 1 && span[1] == '\n') ? 2 : 1;
            result = new RuleTestResult
            {
                LineCount = 1,
                ColumnNumber = 1,
                Length = length,
                Type = TokenTyped.NewLine,
                Success = true
            };
            return true;
        }

        private bool ScanRowComment(ReadOnlySpan<char> span, out RuleTestResult result)
        {
            int i = 2;
            while (i < span.Length && span[i] != '\n' && span[i] != '\r') i++;

            bool hasNewLine = i < span.Length && (span[i] == '\n' || span[i] == '\r');
            int length = i;
            if (hasNewLine)
            {
                length++;
                if (span[i] == '\r' && length < span.Length && span[length] == '\n')
                {
                    length++;
                }
            }

            result = new RuleTestResult
            {
                ColumnNumber = hasNewLine ? 1 : this.ColumnNumber + length,
                LineCount = hasNewLine ? 1 : 0,
                Length = length,
                Type = TokenTyped.Comment,
                Success = true
            };
            return true;
        }

        private bool ScanBlockComment(ReadOnlySpan<char> span, out RuleTestResult result)
        {
            int currentColumn = this.ColumnNumber + 2;
            int currentLineCount = 0;
            for (int i = 2; i < span.Length - 1; i++)
            {
                char c = span[i];
                if (c == '\n')
                {
                    currentColumn = 0;
                    currentLineCount++;
                }
                else
                {
                    currentColumn++;
                    if (c == '*' && span[i + 1] == '/')
                    {
                        result = new RuleTestResult
                        {
                            ColumnNumber = currentColumn + 1,
                            LineCount = currentLineCount,
                            Length = i + 2,
                            Type = TokenTyped.Comment,
                            Success = true
                        };
                        return true;
                    }
                }
            }

            result = default;
            return false;
        }

        private bool ScanIdentifier(ReadOnlySpan<char> span, out RuleTestResult result)
        {
            int i = 1;
            while (i < span.Length && IsIdentifierPart(span[i]))
            {
                i++;
            }

            var symbol = Symbols.FromSpan(span.Slice(0, i));
            result = new RuleTestResult
            {
                ColumnNumber = this.ColumnNumber + i,
                Length = i,
                Symbol = symbol,
                Value = symbol?.Name,
                Success = true,
                Type = TokenTyped.Identifier
            };
            return true;
        }

        private bool ScanHexNumber(ReadOnlySpan<char> span, out RuleTestResult result)
        {
            int i = 2;
            while (i < span.Length)
            {
                char c = span[i];
                if ((c >= 'a' && c <= 'f') || (c >= 'A' && c <= 'F') || (c >= '0' && c <= '9')) i++;
                else break;
            }

            if (i <= 2)
            {
                result = default;
                return false;
            }

            result = new RuleTestResult
            {
                ColumnNumber = this.ColumnNumber + i,
                Length = i,
                Success = true,
                Type = TokenTyped.Number
            };
            return true;
        }

        private bool ScanNumber(ReadOnlySpan<char> span, out RuleTestResult result)
        {
            int dot = -1;
            char lastChar = span[0];
            int i = 1;
            for (; i < span.Length; i++)
            {
                char c = span[i];
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
                    if (dot > -1) break;
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

            result = new RuleTestResult
            {
                ColumnNumber = this.ColumnNumber + i,
                Length = i,
                Success = true,
                Type = TokenTyped.Number
            };
            return true;
        }

        private bool ScanPunctuator(ReadOnlySpan<char> span, out RuleTestResult result)
        {
            char c0 = span[0];
            int length = 0;

            if (span.Length >= 3)
            {
                char c1 = span[1];
                char c2 = span[2];
                if (c0 == '.' && c1 == '.' && c2 == '.') length = 3;
                else if (c0 == '>' && c1 == '>' && c2 == '>') length = 3;
            }

            if (length == 0 && span.Length >= 2)
            {
                char c1 = span[1];
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
            result = new RuleTestResult
            {
                ColumnNumber = this.ColumnNumber + length,
                Length = length,
                Symbol = symbol,
                Value = symbol?.Name,
                Type = TokenTyped.Punctuator,
                Success = true
            };
            return true;
        }

        private bool ScanString(ReadOnlySpan<char> span, out RuleTestResult result)
        {
            char keychar = span[0];
            int currentColumn = this.ColumnNumber;
            int currentLineCount = 0;
            StringBuilder sb = null;
            int segmentStart = 1;

            for (int i = 1; i < span.Length; i++)
            {
                char viewChar = span[i];
                if (viewChar == '\\')
                {
                    if (i + 1 >= span.Length) break;
                    if (!CanEscape(span[i + 1], out var escapedChar))
                    {
                        throw new AuroraLexicalException("", this.LineNumber, currentColumn, "Unrecognizable escape characters");
                    }
                    if (keychar != '`')
                    {
                        sb ??= new StringBuilder();
                        sb.Append(span.Slice(segmentStart, i - segmentStart));
                        sb.Append(escapedChar);
                        segmentStart = i + 2;
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
                        string value;
                        if (keychar == '`')
                        {
                            value = String.Empty;
                        }
                        else if (sb == null)
                        {
                            value = span.Slice(1, i - 1).ToString();
                        }
                        else
                        {
                            sb.Append(span.Slice(segmentStart, i - segmentStart));
                            value = sb.ToString();
                        }

                        result = new RuleTestResult
                        {
                            ColumnNumber = currentColumn,
                            LineCount = currentLineCount,
                            Length = i + 1,
                            Value = value,
                            Success = true,
                            Type = keychar == '`' ? TokenTyped.StringTemplate : TokenTyped.String
                        };
                        return true;
                    }
                }
            }

            result = default;
            return false;
        }

        private bool ScanStringBlock(ReadOnlySpan<char> span, out RuleTestResult result)
        {
            int currentLineCount = 0;
            int currentColumn = this.ColumnNumber + 2;
            var sb = new StringBuilder();
            int i = 2;
            if (i < span.Length && span[i] == ' ')
            {
                i++;
                currentColumn++;
            }

            while (i < span.Length)
            {
                char c = span[i];
                if (c == '\r')
                {
                    i++;
                    continue;
                }

                if (c == '\n')
                {
                    currentLineCount++;
                    currentColumn = 1;
                    sb.AppendLine();
                    i++;
                    while (i < span.Length)
                    {
                        char cNext = span[i];
                        if (cNext == ' ' || cNext == '\t')
                        {
                            i++;
                            currentColumn++;
                        }
                        else
                        {
                            break;
                        }
                    }

                    if (i + 1 < span.Length && span[i] == '|' && span[i + 1] == '>')
                    {
                        i += 2;
                        currentColumn += 2;
                        if (i < span.Length && span[i] == ' ')
                        {
                            i++;
                            currentColumn++;
                        }
                        continue;
                    }

                    result = new RuleTestResult
                    {
                        LineCount = currentLineCount,
                        ColumnNumber = currentColumn,
                        Length = i,
                        Value = sb.ToString(),
                        Success = true,
                        Type = TokenTyped.String
                    };
                    return true;
                }

                sb.Append(c);
                i++;
                currentColumn++;
            }

            result = new RuleTestResult
            {
                LineCount = currentLineCount,
                ColumnNumber = currentColumn,
                Length = i,
                Value = sb.ToString(),
                Success = true,
                Type = TokenTyped.String
            };
            return true;
        }

        private bool ScanRegex(ReadOnlySpan<char> span, out RuleTestResult result)
        {
            if (span.Length < 2)
            {
                result = default;
                return false;
            }

            char lookahead = span[1];
            if (lookahead == '/' || lookahead == '*' || lookahead == '=')
            {
                result = default;
                return false;
            }

            bool inCharacterClass = false;
            bool escaped = false;

            for (int i = 1; i < span.Length; i++)
            {
                char current = span[i];
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
                    int literalLength = i + 1;
                    int flagsLength = 0;
                    while (literalLength + flagsLength < span.Length && IsRegexFlag(span[literalLength + flagsLength]))
                    {
                        flagsLength++;
                    }

                    int totalLength = literalLength + flagsLength;
                    result = new RuleTestResult
                    {
                        ColumnNumber = this.ColumnNumber + totalLength,
                        Length = totalLength,
                        Value = span.Slice(0, totalLength).ToString(),
                        Type = TokenTyped.Regex,
                        Success = true
                    };
                    return true;
                }
            }

            result = default;
            return false;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void Advance(in RuleTestResult result)
        {
            this.readOffset += result.Length;
            this.bufferLength -= result.Length;
            this.LineNumber += result.LineCount;
            this.ColumnNumber = result.ColumnNumber;
            this.Offset += result.Length;
        }

        private Boolean ShouldParseRegexLiteral()
        {
            if (this.tokens.Count == 0) return true;

            var previousToken = this.tokens[this.tokens.Count - 1];
            if (previousToken.Kind == LexTokenKind.EndOfFile) return true;
            if (previousToken.Kind == LexTokenKind.Keyword) return true;

            if (previousToken.Kind == LexTokenKind.Operator)
            {
                var symbol = Symbols.FromId(previousToken.SymbolId);
                return symbol != Symbols.OP_INCREMENT && symbol != Symbols.OP_DECREMENT;
            }

            if (previousToken.Kind == LexTokenKind.Punctuator)
            {
                var symbol = Symbols.FromId(previousToken.SymbolId);
                return symbol != Symbols.PT_RIGHTPARENTHESIS &&
                       symbol != Symbols.PT_RIGHTBRACKET &&
                       symbol != Symbols.PT_RIGHTBRACE &&
                       symbol != Symbols.PT_DOT;
            }

            return false;
        }

        /// <summary>
        /// create token from rule result
        /// </summary>
        /// <param name="result"></param>
        /// <returns></returns>
        /// <exception cref="AuroraLexicalException"></exception>
        private LexToken CreateLexToken(in RuleTestResult result)
        {
            var tokenSpan = this.InputData.AsSpan(result.Offset, result.Length);
            var lexToken = new LexToken
            {
                Data = LexToken.Pack(LexTokenKind.Identifier),
                StartLine = this.LineNumber,
                StartColumn = this.ColumnNumber,
                Offset = result.Offset,
                Length = result.Length
            };

            if (result.Type == TokenTyped.String)
            {
                lexToken.Data = LexToken.PackText(LexTokenKind.String, this.AddTokenValue(result.Value));
                return lexToken;
            }

            if (result.Type == TokenTyped.StringTemplate)
            {
                lexToken.Data = LexToken.PackText(LexTokenKind.StringTemplate, this.AddTokenValue(result.Value));
                return lexToken;
            }

            if (result.Type == TokenTyped.Number)
            {
                lexToken.Data = LexToken.Pack(LexTokenKind.Number);
                return lexToken;
            }

            if (result.Type == TokenTyped.Regex)
            {
                lexToken.Data = LexToken.PackText(LexTokenKind.Regex, this.AddTokenValue(result.Value));
                return lexToken;
            }

            var symbol = result.Symbol ?? (result.Value != null ? Symbols.FromString(result.Value) : Symbols.FromSpan(tokenSpan));
            if (symbol != null)
            {
                if (symbol.Type == SymbolTypes.KeyWord) lexToken.Data = LexToken.Pack(LexTokenKind.Keyword, symbol.Id);
                else if (symbol.Type == SymbolTypes.Punctuator) lexToken.Data = LexToken.Pack(LexTokenKind.Punctuator, symbol.Id);
                else if (symbol.Type == SymbolTypes.Operator) lexToken.Data = LexToken.Pack(LexTokenKind.Operator, symbol.Id);
                else if (symbol.Type == SymbolTypes.NullValue) lexToken.Data = LexToken.Pack(LexTokenKind.Null, symbol.Id);
                else if (symbol.Type == SymbolTypes.BooleanValue) lexToken.Data = LexToken.Pack(LexTokenKind.Boolean, symbol.Id);
                else if (symbol.Type == SymbolTypes.Identifier) lexToken.Data = LexToken.Pack(LexTokenKind.Identifier, symbol.Id);
                else throw new AuroraLexicalException(this.FileName, this.LineNumber, this.ColumnNumber, $"Invalid Identifier {result.Value ?? tokenSpan.ToString()}");
                return lexToken;
            }

            if (result.Type == TokenTyped.Identifier)
            {
                lexToken.Data = LexToken.PackText(LexTokenKind.Identifier, this.InternName(tokenSpan));
                return lexToken;
            }

            throw new AuroraLexicalException(this.FileName, this.LineNumber, this.ColumnNumber, $"Invalid Identifier {result.Value ?? tokenSpan.ToString()}");
        }

        private Token Materialize(int index)
        {
            if (this.cachedTokenIndex == index && this.cachedToken != null)
            {
                return this.cachedToken;
            }

            var lexToken = this.tokens[index];
            var symbol = Symbols.FromId(lexToken.SymbolId);
            var value = this.GetTokenValue(in lexToken, symbol);
            Token token = lexToken.Kind switch
            {
                LexTokenKind.Identifier => new IdentifierToken(),
                LexTokenKind.Keyword => new KeywordToken(),
                LexTokenKind.Punctuator => new PunctuatorToken(),
                LexTokenKind.Operator => new OperatorToken(),
                LexTokenKind.String => new StringToken(),
                LexTokenKind.StringTemplate => new StringTemplateToken(),
                LexTokenKind.Number => new NumberToken(this.InputData.AsSpan(lexToken.Offset, lexToken.Length)),
                LexTokenKind.Regex => new RegexToken(value),
                LexTokenKind.Boolean => new BooleanToken(symbol == Symbols.VALUE_TRUE),
                LexTokenKind.Null => new NullToken(),
                LexTokenKind.EndOfFile => new EndOfFileToken(),
                _ => throw new InvalidOperationException()
            };

            token.Symbol = symbol;
            token.NameId = lexToken.Kind == LexTokenKind.Identifier ? lexToken.TextId : 0;
            if (lexToken.Kind != LexTokenKind.Number)
            {
                token.Value = value;
            }
            token.Range = this.CreateRange(in lexToken);
            this.cachedTokenIndex = index;
            this.cachedToken = token;
            return token;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private SourceSpan CreateRange(in LexToken token)
        {
            var endLine = token.StartLine;
            var endColumn = token.StartColumn + token.Length;
            if ((token.Kind == LexTokenKind.String || token.Kind == LexTokenKind.StringTemplate) && token.Length > 0)
            {
                var span = this.InputData.AsSpan(token.Offset, token.Length);
                endColumn = token.StartColumn;
                for (int i = 0; i < span.Length; i++)
                {
                    var c = span[i];
                    if (c == '\r')
                    {
                        if (i + 1 < span.Length && span[i + 1] == '\n') i++;
                        endLine++;
                        endColumn = 1;
                    }
                    else if (c == '\n')
                    {
                        endLine++;
                        endColumn = 1;
                    }
                    else
                    {
                        endColumn++;
                    }
                }
            }

            return new SourceSpan
            {
                FileName = this.FullPath,
                StartLine = token.StartLine,
                StartColumn = token.StartColumn,
                EndLine = endLine,
                EndColumn = endColumn,
                Offset = token.Offset,
                Length = token.Length
            };
        }

        private int AddTokenValue(string value)
        {
            this.tokenValues ??= new List<string>();
            var id = this.tokenValues.Count;
            this.tokenValues.Add(value);
            return id;
        }

        private string GetTokenValue(in LexToken token, Symbols symbol)
        {
            if (token.Kind == LexTokenKind.Identifier && token.TextId > 0)
            {
                return _names[token.TextId];
            }

            if (token.TextId >= 0)
            {
                return this.tokenValues[token.TextId];
            }

            return symbol?.Name;
        }

        private int InternName(ReadOnlySpan<char> value)
        {
            if (value.Length == 0) return 0;

            var lookup = _nameIds.GetAlternateLookup<ReadOnlySpan<char>>();
            if (lookup.TryGetValue(value, out var id))
            {
                return id;
            }

            var text = value.ToString();
            id = _names.Count;
            _names.Add(text);
            _nameIds.Add(text, id);
            return id;
        }
    }
}
