using AuroraScript.LanguageServices;
using AuroraScript.LanguageServices.Builtins;
using AuroraScript.LanguageServices.Features.SemanticTokens;
using System.Linq;
using Xunit;

namespace AuroraScript.LanguageServices.Tests;

public sealed class SemanticTokensFeatureTests
{
    [Fact]
    public void ScansLexerTokensForHighlighting()
    {
        const string source =
            """
            @module(TEST);
            export func run() {
                const string = 1;
                const number = 2;
                const label = "text";
                const local = { log: 1 };
                console.log($arg, $state);
                global.modules;
                JSON.stringify(Math.PI);
                HotPatch.apply();
                String.fromCharCode(65);
                local.log();
                return `total: ${10}`;
            }
            """;
        var service = new AuroraLanguageService(BuiltinApiLoader.LoadFromFile(BuiltinApiCatalogTests.GetRuntimeApiPath()));

        var result = service.GetSemanticTokens("main.as", source);

        Assert.Contains(result.Tokens, token => token.Type == AuroraSemanticTokenTypes.Keyword);
        AssertToken(source, result, "\"text\"", AuroraSemanticTokenTypes.String);
        Assert.Contains(result.Tokens, token => token.Type == AuroraSemanticTokenTypes.Number);
        AssertToken(source, result, "console", AuroraSemanticTokenTypes.Type);
        AssertToken(source, result, "global", AuroraSemanticTokenTypes.Namespace);
        AssertToken(source, result, "JSON", AuroraSemanticTokenTypes.Type);
        AssertToken(source, result, "Math", AuroraSemanticTokenTypes.Type);
        AssertToken(source, result, "HotPatch", AuroraSemanticTokenTypes.Type);
        AssertToken(source, result, "$arg", AuroraSemanticTokenTypes.Parameter);
        AssertToken(source, result, "$state", AuroraSemanticTokenTypes.Parameter);
        AssertToken(source, result, "String", AuroraSemanticTokenTypes.Type);
        AssertToken(source, result, "fromCharCode", AuroraSemanticTokenTypes.Method);
        AssertToken(source, result, "log", AuroraSemanticTokenTypes.Method);
        AssertToken(source, result, "modules", AuroraSemanticTokenTypes.Property);
        AssertToken(source, result, "PI", AuroraSemanticTokenTypes.Property);
        AssertNoToken(source, result, "string", AuroraSemanticTokenTypes.Type);
        AssertNoToken(source, result, "number", AuroraSemanticTokenTypes.Type);
        AssertNoToken(source, result, "`total: ${10}`", AuroraSemanticTokenTypes.String);
        Assert.True(result.Tokens.SequenceEqual(result.Tokens.OrderBy(token => token.Line).ThenBy(token => token.Character)));
    }

    [Fact]
    public void SemanticTokenPositionsDoNotDriftAfterStrings()
    {
        const string source =
            """
            expectTrue(ctx, typeof timer.reset == "function", "Timer exposes reset function", typeof timer.reset, "function");
            const block =
            |> line 1
            |> line 2
            ;
            /* comment */ typeof block;
            """;
        var service = new AuroraLanguageService(BuiltinApiLoader.LoadFromFile(BuiltinApiCatalogTests.GetRuntimeApiPath()));

        var result = service.GetSemanticTokens("main.as", source);

        Assert.Equal(3, result.Tokens.Count(token => token.Type == AuroraSemanticTokenTypes.Keyword && TokenText(source, token) == "typeof"));
        Assert.DoesNotContain(result.Tokens, token => TokenText(source, token) == ", type");
    }

    [Fact]
    public void ReturnsEmptyTokensForIncompleteSource()
    {
        var service = new AuroraLanguageService(BuiltinApiLoader.LoadFromFile(BuiltinApiCatalogTests.GetRuntimeApiPath()));

        var result = service.GetSemanticTokens("main.as", "export func run(){ return \"unterminated");

        Assert.Empty(result.Tokens);
    }

    private static void AssertToken(string source, SemanticTokensResult result, string text, int type)
    {
        Assert.Contains(result.Tokens, token => token.Type == type && TokenText(source, token) == text);
    }

    private static void AssertNoToken(string source, SemanticTokensResult result, string text, int type)
    {
        Assert.DoesNotContain(result.Tokens, token => token.Type == type && TokenText(source, token) == text);
    }

    private static string TokenText(string source, SemanticToken token)
    {
        var offset = 0;
        var line = 0;
        var character = 0;
        while (offset < source.Length && (line < token.Line || character < token.Character))
        {
            if (source[offset] == '\r')
            {
                if (offset + 1 < source.Length && source[offset + 1] == '\n')
                {
                    offset++;
                }

                line++;
                character = 0;
            }
            else if (source[offset] == '\n')
            {
                line++;
                character = 0;
            }
            else
            {
                character++;
            }

            offset++;
        }

        return offset + token.Length <= source.Length
            ? source.Substring(offset, token.Length)
            : string.Empty;
    }
}
