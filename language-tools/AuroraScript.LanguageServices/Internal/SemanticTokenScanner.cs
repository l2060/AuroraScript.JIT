using AuroraScript.Compiler;
using AuroraScript.Compiler.Analyzer;
using AuroraScript.Core;
using AuroraScript.LanguageServices.Builtins;
using AuroraScript.LanguageServices.Features.SemanticTokens;
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
            using var lexer = new AuroraLexer(baseDirectory, new MemoryScriptSource(baseDirectory, fullPath, sourceText));
            var tokenInfos = new AuroraLexer.LexerTokenInfo[lexer.TokenCount];
            for (var i = 0; i < tokenInfos.Length; i++)
            {
                tokenInfos[i] = lexer.GetTokenInfo(i);
            }

            var tokens = new List<SemanticToken>();
            for (var i = 0; i < tokenInfos.Length; i++)
            {
                var token = tokenInfos[i];
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
