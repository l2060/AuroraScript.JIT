using AuroraScript.Compiler.Analyzer;
using AuroraScript.Core;
using AuroraScript.LanguageServices.Builtins;
using AuroraScript.LanguageServices.Features.SemanticTokens;
using AuroraScript.Source;
using System;
using System.Collections.Generic;
using System.IO;

namespace AuroraScript.LanguageServices.Internal;

internal static class SemanticTokenScanner
{
    private static readonly HashSet<string> LanguageVariables = new(StringComparer.Ordinal)
    {
        "$arg",
        "$args",
        "$state"
    };

    public static SemanticTokensResult Scan(
        string sourceName,
        string sourceText,
        string? baseDirectory,
        BuiltinApiCatalog builtins)
    {
        baseDirectory ??= Directory.GetCurrentDirectory();
        var fullPath = ScriptPath.GetFullPath(baseDirectory, sourceName);
        try
        {
            using var lexer = new AuroraLexer(baseDirectory, new MemorySource(baseDirectory, fullPath, sourceText));
            var tokenInfos = new AuroraLexer.LexerTokenInfo[lexer.TokenCount];
            for (var i = 0; i < tokenInfos.Length; i++)
            {
                tokenInfos[i] = lexer.GetTokenInfo(i);
            }

            var tokens = new List<SemanticToken>();
            for (var i = 0; i < tokenInfos.Length; i++)
            {
                var token = tokenInfos[i];
                if (TryAddStringBlockTokens(sourceText, token, tokens))
                {
                    continue;
                }

                var type = GetSemanticType(tokenInfos, i, builtins);
                if (type < 0 || token.Range.Length <= 0 || token.Range.StartLine != token.Range.EndLine)
                {
                    continue;
                }

                var position = PositionFromOffset(sourceText, token.Range.Offset);
                tokens.Add(new SemanticToken(
                    position.Line,
                    position.Character,
                    token.Range.Length,
                    type,
                    0));
            }

            return new SemanticTokensResult(tokens);
        }
        catch (AuroraCompilationException)
        {
            return new SemanticTokensResult(Array.Empty<SemanticToken>());
        }
    }

    private static bool TryAddStringBlockTokens(
        string sourceText,
        AuroraLexer.LexerTokenInfo token,
        List<SemanticToken> tokens)
    {
        if (token.Kind != AuroraLexer.LexTokenKind.String ||
            token.Range.Length < 3 ||
            token.Range.Offset < 0 ||
            token.Range.Offset + token.Range.Length > sourceText.Length ||
            sourceText[token.Range.Offset] != '|' ||
            sourceText[token.Range.Offset + 1] != '>' ||
            sourceText[token.Range.Offset + 2] != ' ')
        {
            return false;
        }

        var line = token.Range.StartLine - 1;
        var character = token.Range.StartColumn - 1;
        var offset = token.Range.Offset;
        var end = token.Range.Offset + token.Range.Length;

        while (offset < end)
        {
            var lineStart = offset;
            while (offset < end && sourceText[offset] != '\r' && sourceText[offset] != '\n')
            {
                offset++;
            }

            AddStringBlockLineToken(sourceText, lineStart, offset, line, character, tokens);

            if (offset < end)
            {
                if (sourceText[offset] == '\r' && offset + 1 < end && sourceText[offset + 1] == '\n')
                {
                    offset += 2;
                }
                else
                {
                    offset++;
                }

                line++;
                character = 0;
                while (offset < end && (sourceText[offset] == ' ' || sourceText[offset] == '\t'))
                {
                    offset++;
                    character++;
                }
            }
        }

        return true;
    }

    private static void AddStringBlockLineToken(
        string sourceText,
        int lineStart,
        int lineEnd,
        int line,
        int character,
        List<SemanticToken> tokens)
    {
        if (lineEnd - lineStart < 3 ||
            sourceText[lineStart] != '|' ||
            sourceText[lineStart + 1] != '>' ||
            sourceText[lineStart + 2] != ' ')
        {
            return;
        }

        var textStart = lineStart + 3;
        var length = lineEnd - textStart;
        if (length <= 0)
        {
            return;
        }

        tokens.Add(new SemanticToken(
            line,
            character + 3,
            length,
            AuroraSemanticTokenTypes.String,
            0));
    }

    private static int GetSemanticType(
        IReadOnlyList<AuroraLexer.LexerTokenInfo> tokens,
        int index,
        BuiltinApiCatalog builtins)
    {
        var token = tokens[index];
        if (token.Kind == AuroraLexer.LexTokenKind.Identifier)
        {
            return GetIdentifierSemanticType(tokens, index, builtins);
        }

        return token.Kind switch
        {
            AuroraLexer.LexTokenKind.Keyword => AuroraSemanticTokenTypes.Keyword,
            AuroraLexer.LexTokenKind.Punctuator => IsWordOperator(token.Value) ? AuroraSemanticTokenTypes.Keyword : AuroraSemanticTokenTypes.Operator,
            AuroraLexer.LexTokenKind.Operator => IsWordOperator(token.Value) ? AuroraSemanticTokenTypes.Keyword : AuroraSemanticTokenTypes.Operator,
            AuroraLexer.LexTokenKind.String => AuroraSemanticTokenTypes.String,
            AuroraLexer.LexTokenKind.StringTemplate => -1,
            AuroraLexer.LexTokenKind.Number => AuroraSemanticTokenTypes.Number,
            AuroraLexer.LexTokenKind.Regex => AuroraSemanticTokenTypes.Regexp,
            AuroraLexer.LexTokenKind.Boolean => AuroraSemanticTokenTypes.Keyword,
            AuroraLexer.LexTokenKind.Null => AuroraSemanticTokenTypes.Keyword,
            _ => -1
        };
    }

    private static int GetIdentifierSemanticType(
        IReadOnlyList<AuroraLexer.LexerTokenInfo> tokens,
        int index,
        BuiltinApiCatalog builtins)
    {
        var value = tokens[index].Value;
        if (string.IsNullOrEmpty(value))
        {
            return -1;
        }

        if (LanguageVariables.Contains(value))
        {
            return AuroraSemanticTokenTypes.Parameter;
        }

        if (PreviousTokenIs(tokens, index, "."))
        {
            return NextTokenIs(tokens, index, "(")
                ? AuroraSemanticTokenTypes.Method
                : AuroraSemanticTokenTypes.Property;
        }

        if (builtins != null && builtins.TryGetGlobal(value, out var global))
        {
            return GetBuiltinGlobalSemanticType(value, global);
        }

        return NextTokenIs(tokens, index, "(")
            ? AuroraSemanticTokenTypes.Function
            : -1;
    }

    private static int GetBuiltinGlobalSemanticType(string value, BuiltinApiSymbol global)
    {
        if (string.Equals(value, "global", StringComparison.Ordinal))
        {
            return AuroraSemanticTokenTypes.Namespace;
        }

        return global.Kind switch
        {
            BuiltinApiKind.Constructor => AuroraSemanticTokenTypes.Type,
            BuiltinApiKind.Object => AuroraSemanticTokenTypes.Type,
            BuiltinApiKind.Function => AuroraSemanticTokenTypes.Function,
            _ => AuroraSemanticTokenTypes.Variable
        };
    }

    private static bool PreviousTokenIs(IReadOnlyList<AuroraLexer.LexerTokenInfo> tokens, int index, string value)
    {
        return index > 0 && string.Equals(tokens[index - 1].Value, value, StringComparison.Ordinal);
    }

    private static bool NextTokenIs(IReadOnlyList<AuroraLexer.LexerTokenInfo> tokens, int index, string value)
    {
        return index + 1 < tokens.Count && string.Equals(tokens[index + 1].Value, value, StringComparison.Ordinal);
    }

    private static bool IsWordOperator(string value)
    {
        return string.Equals(value, "in", StringComparison.Ordinal) ||
            string.Equals(value, "typeof", StringComparison.Ordinal);
    }

    private static (int Line, int Character) PositionFromOffset(string sourceText, int targetOffset)
    {
        var line = 0;
        var character = 0;
        var limit = Math.Min(Math.Max(targetOffset, 0), sourceText.Length);
        for (var offset = 0; offset < limit; offset++)
        {
            if (sourceText[offset] == '\r')
            {
                if (offset + 1 < limit && sourceText[offset + 1] == '\n')
                {
                    offset++;
                }

                line++;
                character = 0;
            }
            else if (sourceText[offset] == '\n')
            {
                line++;
                character = 0;
            }
            else
            {
                character++;
            }
        }

        return (line, character);
    }
}
