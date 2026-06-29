using AuroraScript.Compiler;
using AuroraScript.Compiler.Syntax;
using AuroraScript.LanguageServices.Text;
using System;
using System.Collections.Generic;
using System.Text;

namespace AuroraScript.LanguageServices.Features.Formatting;

internal static class AuroraDocumentFormatter
{
    public static FormattingResult Format(string sourceName, string sourceText, FormattingOptions options)
    {
        sourceText ??= string.Empty;
        options ??= new FormattingOptions();

        if (sourceText.Length == 0)
        {
            return FormattingResult.Empty;
        }

        var lines = SourceLine.Split(sourceText);
        if (lines.Count == 0)
        {
            return FormattingResult.Empty;
        }

        var protectedLines = new bool[lines.Count];
        var blockStringLines = new bool[lines.Count];
        var tokens = new List<FormatToken>();
        CollectTokens(sourceText, sourceName, lines.Count, protectedLines, blockStringLines, tokens);

        var lineIndents = ComputeLineIndents(lines.Count, protectedLines, tokens);
        var formatted = BuildFormattedText(sourceText, lines, protectedLines, blockStringLines, lineIndents, tokens, options);
        if (string.Equals(formatted, sourceText, StringComparison.Ordinal))
        {
            return FormattingResult.Empty;
        }

        return new FormattingResult(new[]
        {
            new TextEdit(new TextRange(sourceName, TextPosition.Zero, EndPosition(sourceText)), formatted)
        });
    }

    private static void CollectTokens(
        string sourceText,
        string sourceName,
        int lineCount,
        bool[] protectedLines,
        bool[] blockStringLines,
        List<FormatToken> tokens)
    {
        var scanner = new AuroraSyntaxScanner(sourceText, sourceName);
        while (scanner.TryRead(out var element))
        {
            if (element.IsToken)
            {
                var token = element.Token;
                if (token.Kind == SyntaxTokenKind.EndOfFile)
                {
                    return;
                }

                if (token.Kind == SyntaxTokenKind.StringBlock)
                {
                    MarkProtectedLines(blockStringLines, token.StartLine, token.EndLine);
                }
                else if (IsProtectedMultilineToken(token))
                {
                    MarkProtectedLines(protectedLines, token.StartLine + 1, token.EndLine);
                }

                tokens.Add(new FormatToken(
                    token.StartLine - 1,
                    token.Offset,
                    token.Length,
                    token.Kind,
                    token.SymbolId));
            }
            else if (element.Trivia.Kind == SyntaxTriviaKind.BlockComment &&
                element.Trivia.EndLine > element.Trivia.StartLine)
            {
                MarkProtectedLines(protectedLines, element.Trivia.StartLine + 1, element.Trivia.EndLine);
            }
        }
    }

    private static bool IsProtectedMultilineToken(SyntaxToken token)
    {
        return token.EndLine > token.StartLine &&
            (token.Kind == SyntaxTokenKind.String ||
             token.Kind == SyntaxTokenKind.StringTemplate ||
             token.Kind == SyntaxTokenKind.Regex);
    }

    private static void MarkProtectedLines(bool[] protectedLines, int startLine, int endLine)
    {
        var start = Math.Max(0, startLine - 1);
        var end = Math.Min(protectedLines.Length - 1, endLine - 1);
        for (var i = start; i <= end; i++)
        {
            protectedLines[i] = true;
        }
    }

    private static int[] ComputeLineIndents(
        int lineCount,
        bool[] protectedLines,
        List<FormatToken> tokens)
    {
        var firstToken = new int[lineCount];
        Array.Fill(firstToken, -1);
        for (var i = 0; i < tokens.Count; i++)
        {
            var token = tokens[i];
            if ((uint)token.Line >= (uint)lineCount || protectedLines[token.Line])
            {
                continue;
            }

            if (firstToken[token.Line] < 0 && token.SymbolId >= 0)
            {
                firstToken[token.Line] = token.SymbolId;
            }
        }

        var indents = new int[lineCount];
        var depth = 0;
        var tokenIndex = 0;
        for (var line = 0; line < lineCount; line++)
        {
            if (protectedLines[line])
            {
                indents[line] = -1;
                while (tokenIndex < tokens.Count && tokens[tokenIndex].Line <= line)
                {
                    tokenIndex++;
                }
                continue;
            }

            var lineDepth = depth;
            if (IsClosingSymbol(firstToken[line]))
            {
                lineDepth = Math.Max(0, lineDepth - 1);
            }

            indents[line] = lineDepth;
            while (tokenIndex < tokens.Count && tokens[tokenIndex].Line == line)
            {
                var symbolId = tokens[tokenIndex].SymbolId;
                if (symbolId >= 0 && IsClosingSymbol(symbolId))
                {
                    depth = Math.Max(0, depth - 1);
                }

                if (symbolId >= 0 && IsOpeningSymbol(symbolId))
                {
                    depth++;
                }

                tokenIndex++;
            }
        }

        return indents;
    }

    private static string BuildFormattedText(
        string sourceText,
        IReadOnlyList<SourceLine> lines,
        bool[] protectedLines,
        bool[] blockStringLines,
        int[] lineIndents,
        IReadOnlyList<FormatToken> tokens,
        FormattingOptions options)
    {
        var builder = new StringBuilder(sourceText.Length);
        var tokenIndex = 0;
        for (var i = 0; i < lines.Count; i++)
        {
            var line = lines[i];
            while (tokenIndex < tokens.Count && tokens[tokenIndex].Line < i)
            {
                tokenIndex++;
            }

            if (protectedLines[i] && !blockStringLines[i])
            {
                builder.Append(sourceText, line.Offset, line.Length);
                while (tokenIndex < tokens.Count && tokens[tokenIndex].Line == i)
                {
                    tokenIndex++;
                }
                continue;
            }

            var contentStart = line.Offset;
            var contentEnd = line.Offset + line.ContentLength;
            while (contentStart < contentEnd && IsIndentWhiteSpace(sourceText[contentStart]))
            {
                contentStart++;
            }

            while (contentEnd > contentStart && IsIndentWhiteSpace(sourceText[contentEnd - 1]))
            {
                contentEnd--;
            }

            if (contentEnd > contentStart)
            {
                AppendIndent(builder, lineIndents[i], options);
                if (blockStringLines[i])
                {
                    builder.Append(sourceText, contentStart, contentEnd - contentStart);
                }
                else
                {
                    tokenIndex = AppendFormattedLineContent(
                        builder,
                        sourceText,
                        contentStart,
                        contentEnd,
                        i,
                        tokens,
                        tokenIndex);
                }
            }

            if (line.NewLineLength > 0)
            {
                builder.Append(sourceText, line.Offset + line.ContentLength, line.NewLineLength);
            }
        }

        return builder.ToString();
    }

    private static int AppendFormattedLineContent(
        StringBuilder builder,
        string sourceText,
        int contentStart,
        int contentEnd,
        int line,
        IReadOnlyList<FormatToken> tokens,
        int tokenIndex)
    {
        var lineStartTokenIndex = tokenIndex;
        while (tokenIndex < tokens.Count && tokens[tokenIndex].Line == line && tokens[tokenIndex].EndOffset <= contentStart)
        {
            tokenIndex++;
        }

        lineStartTokenIndex = tokenIndex;
        var lineTokenCount = 0;
        while (lineStartTokenIndex + lineTokenCount < tokens.Count &&
            tokens[lineStartTokenIndex + lineTokenCount].Line == line &&
            tokens[lineStartTokenIndex + lineTokenCount].Offset < contentEnd)
        {
            lineTokenCount++;
        }

        if (lineTokenCount == 0)
        {
            builder.Append(sourceText, contentStart, contentEnd - contentStart);
            return tokenIndex;
        }

        var current = contentStart;
        FormatToken? previousToken = null;
        FormatToken? beforePreviousToken = null;
        for (var i = 0; i < lineTokenCount; i++)
        {
            var token = tokens[lineStartTokenIndex + i];
            if (token.Offset >= current)
            {
                AppendInterTokenText(builder, sourceText, current, token.Offset, beforePreviousToken, previousToken, token);
            }

            var tokenEnd = Math.Min(token.EndOffset, contentEnd);
            builder.Append(sourceText, token.Offset, tokenEnd - token.Offset);
            current = tokenEnd;
            beforePreviousToken = previousToken;
            previousToken = token;
        }

        if (current < contentEnd)
        {
            builder.Append(sourceText, current, contentEnd - current);
        }

        return lineStartTokenIndex + lineTokenCount;
    }

    private static void AppendInterTokenText(
        StringBuilder builder,
        string sourceText,
        int start,
        int end,
        FormatToken? beforePreviousToken,
        FormatToken? previousToken,
        FormatToken currentToken)
    {
        if (previousToken == null ||
            ContainsNonWhitespace(sourceText, start, end) ||
            !ShouldNormalizeSpacing(beforePreviousToken, previousToken.Value, currentToken, out var requiredSpace))
        {
            builder.Append(sourceText, start, end - start);
            return;
        }

        if (requiredSpace)
        {
            builder.Append(' ');
        }
    }

    private static void AppendIndent(StringBuilder builder, int depth, FormattingOptions options)
    {
        if (depth <= 0)
        {
            return;
        }

        if (options.InsertSpaces)
        {
            builder.Append(' ', checked(depth * options.TabSize));
        }
        else
        {
            builder.Append('\t', depth);
        }
    }

    private static TextPosition EndPosition(string text)
    {
        var line = 0;
        var character = 0;
        for (var i = 0; i < text.Length; i++)
        {
            var c = text[i];
            if (c == '\r')
            {
                if (i + 1 < text.Length && text[i + 1] == '\n')
                {
                    i++;
                }

                line++;
                character = 0;
            }
            else if (c == '\n')
            {
                line++;
                character = 0;
            }
            else
            {
                character++;
            }
        }

        return new TextPosition(line, character);
    }

    private static bool IsOpeningSymbol(int symbolId)
    {
        return symbolId == Symbols.PT_LEFTBRACE.Id ||
            symbolId == Symbols.PT_LEFTBRACKET.Id ||
            symbolId == Symbols.PT_LEFTPARENTHESIS.Id;
    }

    private static bool IsClosingSymbol(int symbolId)
    {
        return symbolId == Symbols.PT_RIGHTBRACE.Id ||
            symbolId == Symbols.PT_RIGHTBRACKET.Id ||
            symbolId == Symbols.PT_RIGHTPARENTHESIS.Id;
    }

    private static bool IsIndentWhiteSpace(char c)
    {
        return c == ' ' || c == '\t';
    }

    private static bool ContainsNonWhitespace(string text, int start, int end)
    {
        for (var i = start; i < end; i++)
        {
            var c = text[i];
            if (c != ' ' && c != '\t')
            {
                return true;
            }
        }

        return false;
    }

    private static bool ShouldNormalizeSpacing(
        FormatToken? beforePrevious,
        FormatToken previous,
        FormatToken current,
        out bool requiredSpace)
    {
        requiredSpace = false;
        if (previous.SymbolId == Symbols.PT_DOT.Id ||
            current.SymbolId == Symbols.PT_DOT.Id)
        {
            return true;
        }

        if (previous.SymbolId == Symbols.PT_METAINFO.Id ||
            previous.SymbolId == Symbols.PT_LEFTPARENTHESIS.Id ||
            previous.SymbolId == Symbols.PT_LEFTBRACKET.Id ||
            current.SymbolId == Symbols.PT_RIGHTPARENTHESIS.Id ||
            current.SymbolId == Symbols.PT_RIGHTBRACKET.Id ||
            current.SymbolId == Symbols.PT_SEMICOLON.Id ||
            current.SymbolId == Symbols.PT_COMMA.Id ||
            current.SymbolId == Symbols.PT_COLON.Id)
        {
            return true;
        }

        if (previous.SymbolId == Symbols.PT_COMMA.Id ||
            previous.SymbolId == Symbols.PT_COLON.Id)
        {
            requiredSpace = true;
            return true;
        }

        if (previous.SymbolId == Symbols.KW_IF.Id ||
            previous.SymbolId == Symbols.KW_FOR.Id ||
            previous.SymbolId == Symbols.KW_WHILE.Id ||
            previous.SymbolId == Symbols.KW_CATCH.Id ||
            previous.SymbolId == Symbols.KW_FUNCTION.Id ||
            previous.SymbolId == Symbols.KW_FUNC.Id)
        {
            requiredSpace = true;
            return true;
        }

        if (current.SymbolId == Symbols.PT_LEFTPARENTHESIS.Id)
        {
            requiredSpace = false;
            return true;
        }

        if (previous.SymbolId == Symbols.KW_RETURN.Id ||
            previous.SymbolId == Symbols.KW_THROW.Id ||
            previous.SymbolId == Symbols.KW_NEW.Id ||
            previous.SymbolId == Symbols.KW_DELETE.Id ||
            previous.SymbolId == Symbols.OP_TYPEOF.Id)
        {
            requiredSpace = !IsStatementTerminator(current);
            return true;
        }

        if (current.SymbolId == Symbols.KW_ELSE.Id ||
            current.SymbolId == Symbols.KW_CATCH.Id ||
            current.SymbolId == Symbols.KW_FINALLY.Id ||
            current.SymbolId == Symbols.KW_FROM.Id)
        {
            requiredSpace = true;
            return true;
        }

        if (previous.SymbolId == Symbols.PT_RIGHTBRACE.Id &&
            current.SymbolId != Symbols.PT_SEMICOLON.Id &&
            current.SymbolId != Symbols.PT_COMMA.Id &&
            current.SymbolId != Symbols.PT_RIGHTPARENTHESIS.Id &&
            current.SymbolId != Symbols.PT_RIGHTBRACKET.Id)
        {
            requiredSpace = true;
            return true;
        }

        if (current.SymbolId == Symbols.PT_LEFTBRACE.Id)
        {
            requiredSpace = true;
            return true;
        }

        if (IsSpacedOperator(current.SymbolId) && IsOperandOrClose(previous) && !IsUnaryOperatorUse(previous, current))
        {
            requiredSpace = true;
            return true;
        }

        if (IsSpacedOperator(previous.SymbolId) &&
            !IsUnaryOperatorUse(beforePrevious, previous) &&
            (IsOperandOrOpen(current) || IsPrefixOperator(current.SymbolId)))
        {
            requiredSpace = true;
            return true;
        }

        if (RequiresWordBoundary(previous, current))
        {
            requiredSpace = true;
            return true;
        }

        return false;
    }

    private static bool RequiresWordBoundary(FormatToken previous, FormatToken current)
    {
        return IsWordLike(previous) && IsWordLike(current);
    }

    private static bool IsWordLike(FormatToken token)
    {
        return token.Kind == SyntaxTokenKind.Identifier ||
            token.Kind == SyntaxTokenKind.Keyword ||
            token.Kind == SyntaxTokenKind.Number ||
            token.Kind == SyntaxTokenKind.String ||
            token.Kind == SyntaxTokenKind.StringBlock ||
            token.Kind == SyntaxTokenKind.StringTemplate ||
            token.Kind == SyntaxTokenKind.Regex ||
            token.Kind == SyntaxTokenKind.Boolean ||
            token.Kind == SyntaxTokenKind.Null;
    }

    private static bool IsStatementTerminator(FormatToken token)
    {
        return token.SymbolId == Symbols.PT_SEMICOLON.Id ||
            token.SymbolId == Symbols.PT_COMMA.Id ||
            token.SymbolId == Symbols.PT_RIGHTPARENTHESIS.Id ||
            token.SymbolId == Symbols.PT_RIGHTBRACKET.Id ||
            token.SymbolId == Symbols.PT_RIGHTBRACE.Id;
    }

    private static bool IsSpacedOperator(int symbolId)
    {
        return symbolId == Symbols.OP_ASSIGNMENT.Id ||
            symbolId == Symbols.OP_COMPOUNDADD.Id ||
            symbolId == Symbols.OP_COMPOUNDSUBTRACT.Id ||
            symbolId == Symbols.OP_COMPOUNDMULTIPLY.Id ||
            symbolId == Symbols.OP_COMPOUNDDIVIDE.Id ||
            symbolId == Symbols.OP_COMPOUNDMODULO.Id ||
            symbolId == Symbols.OP_EQUAL.Id ||
            symbolId == Symbols.OP_NOT_EQUAL.Id ||
            symbolId == Symbols.OP_LESSTHAN.Id ||
            symbolId == Symbols.OP_GREATERTHAN.Id ||
            symbolId == Symbols.OP_LESS_EQUAL.Id ||
            symbolId == Symbols.OP_GREATER_EQUAL.Id ||
            symbolId == Symbols.OP_PLUS.Id ||
            symbolId == Symbols.OP_SUBTRACT.Id ||
            symbolId == Symbols.OP_MULTIPLY.Id ||
            symbolId == Symbols.OP_DIVIDE.Id ||
            symbolId == Symbols.OP_MODULO.Id ||
            symbolId == Symbols.OP_LOGICAL_AND.Id ||
            symbolId == Symbols.OP_LOGICAL_OR.Id ||
            symbolId == Symbols.OP_BIT_AND.Id ||
            symbolId == Symbols.OP_BIT_OR.Id ||
            symbolId == Symbols.OP_BIT_XOR.Id ||
            symbolId == Symbols.OP_LEFTSHIFT.Id ||
            symbolId == Symbols.OP_SIGNEDRIGHTSHIFT.Id ||
            symbolId == Symbols.OP_UNSIGNEDRIGHTSHIFT.Id ||
            symbolId == Symbols.OP_IN.Id ||
            symbolId == Symbols.PT_LAMBDA.Id;
    }

    private static bool IsUnaryOperatorUse(FormatToken? beforeOperator, FormatToken operatorToken)
    {
        if (!IsPrefixOperator(operatorToken.SymbolId))
        {
            return false;
        }

        return beforeOperator == null || IsPrefixContext(beforeOperator.Value);
    }

    private static bool IsUnaryOperatorUse(FormatToken previous, FormatToken currentOperator)
    {
        return IsPrefixOperator(currentOperator.SymbolId) && IsPrefixContext(previous);
    }

    private static bool IsPrefixOperator(int symbolId)
    {
        return symbolId == Symbols.OP_PLUS.Id ||
            symbolId == Symbols.OP_SUBTRACT.Id ||
            symbolId == Symbols.OP_LOGICALNOT.Id ||
            symbolId == Symbols.OP_BIT_NOT.Id ||
            symbolId == Symbols.OP_TYPEOF.Id ||
            symbolId == Symbols.KW_DELETE.Id ||
            symbolId == Symbols.KW_NEW.Id;
    }

    private static bool IsPrefixContext(FormatToken token)
    {
        return token.Kind == SyntaxTokenKind.Operator ||
            token.SymbolId == Symbols.PT_LEFTPARENTHESIS.Id ||
            token.SymbolId == Symbols.PT_LEFTBRACKET.Id ||
            token.SymbolId == Symbols.PT_LEFTBRACE.Id ||
            token.SymbolId == Symbols.PT_COMMA.Id ||
            token.SymbolId == Symbols.PT_COLON.Id ||
            token.SymbolId == Symbols.KW_RETURN.Id ||
            token.SymbolId == Symbols.KW_THROW.Id ||
            token.SymbolId == Symbols.KW_NEW.Id ||
            token.SymbolId == Symbols.KW_DELETE.Id ||
            token.SymbolId == Symbols.OP_TYPEOF.Id;
    }

    private static bool IsOperandOrClose(FormatToken token)
    {
        return IsWordLike(token) ||
            token.SymbolId == Symbols.PT_RIGHTPARENTHESIS.Id ||
            token.SymbolId == Symbols.PT_RIGHTBRACKET.Id ||
            token.SymbolId == Symbols.PT_RIGHTBRACE.Id;
    }

    private static bool IsOperandOrOpen(FormatToken token)
    {
        return IsWordLike(token) ||
            token.SymbolId == Symbols.PT_LEFTPARENTHESIS.Id ||
            token.SymbolId == Symbols.PT_LEFTBRACKET.Id ||
            token.SymbolId == Symbols.PT_LEFTBRACE.Id;
    }

    private readonly record struct FormatToken(
        int Line,
        int Offset,
        int Length,
        SyntaxTokenKind Kind,
        int SymbolId)
    {
        public int EndOffset => Offset + Length;
    }

    private readonly struct SourceLine
    {
        public SourceLine(int offset, int contentLength, int newLineLength)
        {
            Offset = offset;
            ContentLength = contentLength;
            NewLineLength = newLineLength;
        }

        public int Offset { get; }
        public int ContentLength { get; }
        public int NewLineLength { get; }
        public int Length => ContentLength + NewLineLength;

        public static IReadOnlyList<SourceLine> Split(string text)
        {
            var lines = new List<SourceLine>();
            var lineStart = 0;
            for (var i = 0; i < text.Length; i++)
            {
                var c = text[i];
                if (c == '\r' || c == '\n')
                {
                    var contentLength = i - lineStart;
                    var newLineLength = 1;
                    if (c == '\r' && i + 1 < text.Length && text[i + 1] == '\n')
                    {
                        newLineLength = 2;
                        i++;
                    }

                    lines.Add(new SourceLine(lineStart, contentLength, newLineLength));
                    lineStart = i + 1;
                }
            }

            if (lineStart < text.Length || text.Length == 0)
            {
                lines.Add(new SourceLine(lineStart, text.Length - lineStart, 0));
            }

            return lines;
        }
    }
}
