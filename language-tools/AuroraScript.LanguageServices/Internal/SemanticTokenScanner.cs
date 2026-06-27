using AuroraScript.Compiler;
using AuroraScript.Compiler.Analyzer;
using AuroraScript.Core;
using AuroraScript.LanguageServices.Features.SemanticTokens;
using System;
using System.Collections.Generic;
using System.IO;

namespace AuroraScript.LanguageServices.Internal;

internal static class SemanticTokenScanner
{
    public static SemanticTokensResult Scan(string sourceName, string sourceText, string? baseDirectory)
    {
        baseDirectory ??= Directory.GetCurrentDirectory();
        var fullPath = ScriptPath.GetFullPath(baseDirectory, sourceName);
        try
        {
            using var lexer = new AuroraLexer(baseDirectory, new MemoryScriptSource(baseDirectory, fullPath, sourceText));
            var tokens = new List<SemanticToken>();
            for (var i = 0; i < lexer.TokenCount; i++)
            {
                var token = lexer.GetTokenInfo(i);
                var type = GetSemanticType(token.Kind);
                if (type < 0 || token.Range.Length <= 0)
                {
                    continue;
                }

                tokens.Add(new SemanticToken(
                    token.Range.StartLine > 0 ? token.Range.StartLine - 1 : 0,
                    token.Range.StartColumn > 0 ? token.Range.StartColumn - 1 : 0,
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

    private static int GetSemanticType(AuroraLexer.LexTokenKind kind)
    {
        return kind switch
        {
            AuroraLexer.LexTokenKind.Identifier => AuroraSemanticTokenTypes.Variable,
            AuroraLexer.LexTokenKind.Keyword => AuroraSemanticTokenTypes.Keyword,
            AuroraLexer.LexTokenKind.Punctuator => AuroraSemanticTokenTypes.Operator,
            AuroraLexer.LexTokenKind.Operator => AuroraSemanticTokenTypes.Operator,
            AuroraLexer.LexTokenKind.String => AuroraSemanticTokenTypes.String,
            AuroraLexer.LexTokenKind.StringTemplate => AuroraSemanticTokenTypes.String,
            AuroraLexer.LexTokenKind.Number => AuroraSemanticTokenTypes.Number,
            AuroraLexer.LexTokenKind.Regex => AuroraSemanticTokenTypes.Regexp,
            AuroraLexer.LexTokenKind.Boolean => AuroraSemanticTokenTypes.Keyword,
            AuroraLexer.LexTokenKind.Null => AuroraSemanticTokenTypes.Keyword,
            _ => -1
        };
    }
}
