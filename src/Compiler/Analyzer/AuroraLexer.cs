using AuroraScript.Common;
using AuroraScript.Core;
using AuroraScript.Scanning;
using AuroraScript.Tokens;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;

namespace AuroraScript.Compiler.Analyzer
{
    internal class AuroraLexer
    {
        public Int32 LineNumber { get; private set; } = 1;
        public Int32 ColumnNumber { get; private set; } = 1;
        public Int32 Offset { get; private set; } = 0;

        public String FullPath { get; private set; }
        public String FileName { get; private set; }
        public String InputData { get; private set; }

        public String Directory { get; private set; }

        private List<Token> tokens = new List<Token>();
        private Int32 readOffset { get; set; } = 0;
        private Int32 bufferLength { get; set; } = 0;
        public Int32 Position { get; private set; } = 0;

        public String BaseDirectory { get; private set; }

        public class LexerSnapshot
        {
            public int Position { get; set; }
            public int LineNumber { get; set; }
            public int ColumnNumber { get; set; }
        }

        public AuroraLexer(String baseDirectory, ScriptSource source)
        {
            this.BaseDirectory = baseDirectory;
            this.Directory = Path.GetDirectoryName(source.FullPath);
            this.FullPath = source.FullPath;
            this.FileName = Path.GetFileName(source.FullPath);
            this.InputData = source.ReadSource().Replace("\r\n", "\n");
            this.bufferLength = this.InputData.Length;
            this.InitRegex();
            this.ParseTokens();
            this.Position = 0;
        }

        private List<TokenRules>[] _dispatchTable;
        private List<TokenRules> _nonAsciiRules;

        private void InitRegex()
        {
            var allRules = new List<TokenRules>
            {
                TokenRules.StringBlock,
                TokenRules.NewLine,
                TokenRules.WhiteSpace,
                TokenRules.RowComment,
                TokenRules.BlockComment,
                TokenRules.HexNumber,
                TokenRules.Identifier,
                TokenRules.Number,
                TokenRules.RegexLiteral,
                TokenRules.Punctuator,
                TokenRules.StringTemplate
            };

            this._dispatchTable = new List<TokenRules>[128];
            this._nonAsciiRules = new List<TokenRules>();

            // Fill dispatch table for ASCII characters
            for (int i = 0; i < 128; i++)
            {
                char c = (char)i;
                this._dispatchTable[i] = new List<TokenRules>();
                foreach (var rule in allRules)
                {
                    // Check if rule can possibly match char c
                    // We need a way to check this without executing the rule fully.
                    // For now, let's use a dummy span.
                    var result = rule.Test(new ReadOnlySpan<char>(new[] { c }), 0, 0);
                    if (result.Success || CanPotentiallyMatch(rule, c))
                    {
                        this._dispatchTable[i].Add(rule);
                    }
                }
            }

            // Rules that match non-ASCII (currently just Identifier)
            foreach (var rule in allRules)
            {
                if (rule is IdentifierRule) this._nonAsciiRules.Add(rule);
            }
        }

        private bool CanPotentiallyMatch(TokenRules rule, char c)
        {
            // Some rules might fail on single char but succeed on multiple (e.g. HexNumber 0x)
            if (rule is HexNumberCommentRule) return c == '0';
            if (rule is StringBlockRule) return c == '|';
            if (rule is StringTemplateRule) return "'\"`".Contains(c);
            if (rule is RowCommentRule) return c == '/';
            if (rule is BlockCommentRule) return c == '/';
            if (rule is RegexRule) return c == '/';
            if (rule is PunctuatorRule) return "...>+*-/=%<>.,;:?!^{}[]()|~&@".Contains(c);
            return false;
        }

        /// <summary>
        /// If it is the specified symbol, return, otherwise report an error
        /// </summary>
        /// <param name="symbol"></param>
        /// <returns></returns>
        /// <exception cref="InvalidOperationException"></exception>
        public Token NextOfKind(Symbols symbol)
        {
            var token = this.Next();
            if (token.Symbol != symbol)
            {
                throw new AuroraLexicalException(this.FullPath, token.LineNumber, token.ColumnNumber, $"The keyword {token.Value} appears in the wrong place, it should be {symbol.Name}.");
            }
            return token;
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
            var nextToken = this.LookAtHead();
            if (endSymbols.Contains(nextToken.Symbol))
            {
                this.Next();
                return true;
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
            var nextToken = this.LookAtHead();
            return (nextToken.Symbol == symbol);
        }

        /// <summary>
        /// If it is the specified symbol, take it out and return true, otherwise return false
        /// </summary>
        /// <param name="symbol"></param>
        /// <returns></returns>
        /// <exception cref="InvalidOperationException"></exception>
        public Boolean TestNext(Symbols symbol)
        {
            var nextToken = this.LookAtHead();
            if (nextToken.Symbol == symbol)
            {
                this.Next();
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
            return this.tokens[this.Position];
        }



        /// <summary>
        /// get next token without removing it.
        /// </summary>
        /// <returns></returns>
        public Token Previous(Int32 offset = 2)
        {
            return this.tokens[this.Position - offset];
        }

        /// <summary>
        /// get next token
        /// </summary>
        /// <returns></returns>
        public Token Next()
        {
            var token = this.tokens[this.Position];
            this.Position++;
            return token;
        }

        public void RollBack()
        {
            this.Position--;
        }

        public LexerSnapshot CreateSnapshot()
        {
            return new LexerSnapshot
            {
                Position = this.Position,
                LineNumber = this.LineNumber,
                ColumnNumber = this.ColumnNumber
            };
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
                if (token is EndOfFileToken) return;
            }
        }

        private Token ParseNext()
        {
            if (this.bufferLength <= 0)
            {
                var eof = new EndOfFileToken();
                eof.FileName = this.FileName;
                eof.LineNumber = this.LineNumber;
                eof.ColumnNumber = this.ColumnNumber;
                return eof;
            }
            ReadOnlySpan<Char> span = this.InputData.AsSpan(this.readOffset, this.bufferLength);
            char c = span[0];

            var rules = (c < 128) ? _dispatchTable[c] : _nonAsciiRules;
            var allowRegexLiteral = this.ShouldParseRegexLiteral();

            foreach (var rule in rules)
            {
                if (!allowRegexLiteral && Object.ReferenceEquals(rule, TokenRules.RegexLiteral))
                {
                    continue;
                }
                var result = rule.Test(span, this.LineNumber, this.ColumnNumber);
                if (result.Success)
                {
                    if (result.Type == TokenTyped.Comment || result.Type == TokenTyped.NewLine || result.Type == TokenTyped.WhiteSpace)
                    {
                        this.readOffset += result.Length;
                        this.bufferLength -= result.Length;
                        this.LineNumber += result.LineCount;
                        this.ColumnNumber = result.ColumnNumber;
                        this.Offset += result.Length;
                        return this.ParseNext();
                    }
                    result.Offset = this.Offset;
                    var token = this.CreateToken(result);
                    this.readOffset += result.Length;
                    this.bufferLength -= result.Length;
                    this.LineNumber += result.LineCount;
                    this.ColumnNumber = result.ColumnNumber;
                    this.Offset += result.Length;
                    return token;
                }
            }
            throw new AuroraLexicalException(this.FileName, this.LineNumber, this.ColumnNumber, "Invalid keywords 。");
        }

        private Boolean ShouldParseRegexLiteral()
        {
            if (this.tokens.Count == 0) return true;

            var previousToken = this.tokens[this.tokens.Count - 1];
            if (previousToken == Token.EOF) return true;
            if (previousToken is KeywordToken) return true;

            if (previousToken is OperatorToken operatorToken)
            {
                var symbol = operatorToken.Symbol;
                return symbol != Symbols.OP_INCREMENT && symbol != Symbols.OP_DECREMENT;
            }

            if (previousToken is PunctuatorToken punctuator)
            {
                var symbol = punctuator.Symbol;
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
        private Token CreateToken(in RuleTestResult result)
        {
            Token token = null;
            if (result.Type == TokenTyped.String)
            {
                token = new StringToken();
            }
            if (result.Type == TokenTyped.StringTemplate)
            {
                token = new StringTemplateToken();
            }
            if (result.Type == TokenTyped.Number) token = new NumberToken(result.Value);
            if (result.Type == TokenTyped.Regex) token = new RegexToken(result.Value);

            if (token == null)
            {
                var symbol = Symbols.FromString(result.Value);
                if (symbol != null)
                {
                    if (symbol.Type == SymbolTypes.KeyWord) token = new KeywordToken();
                    if (symbol.Type == SymbolTypes.Punctuator) token = new PunctuatorToken();
                    if (symbol.Type == SymbolTypes.Operator) token = new OperatorToken();
                    //if (symbol.Type == SymbolTypes.Typed) token = new TypedToken();
                    if (symbol.Type == SymbolTypes.NullValue)
                    {
                        token = new NullToken();
                    }
                    if (symbol.Type == SymbolTypes.BooleanValue) token = new BooleanToken(result.Value);
                    if (symbol.Type == SymbolTypes.Identifier) token = new IdentifierToken();
                    token.Symbol = symbol;
                }
                else
                {
                    if (result.Type == TokenTyped.Identifier) token = new IdentifierToken();
                }
            }

            if (token == null) throw new AuroraLexicalException(this.FileName, this.LineNumber, this.ColumnNumber, $"Invalid Identifier {result.Value}");
            token.Value = result.Value;
            token.Range = new SourceSpan
            {
                FileName = this.FullPath,
                StartLine = this.LineNumber,
                StartColumn = this.ColumnNumber,
                EndLine = this.LineNumber + result.LineCount,
                EndColumn = result.ColumnNumber,
                Offset = result.Offset,
                Length = result.Length
            };
            return token;
        }
    }
}
