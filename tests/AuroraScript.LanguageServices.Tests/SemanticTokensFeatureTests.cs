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
            export func run(value) {
                const total = value + 10;
                return `total: ${total}`;
            }
            """;
        var service = new AuroraLanguageService(BuiltinApiLoader.LoadFromFile(BuiltinApiCatalogTests.GetRuntimeApiPath()));

        var result = service.GetSemanticTokens("main.as", source);

        Assert.Contains(result.Tokens, token => token.Type == AuroraSemanticTokenTypes.Keyword);
        Assert.Contains(result.Tokens, token => token.Type == AuroraSemanticTokenTypes.String);
        Assert.Contains(result.Tokens, token => token.Type == AuroraSemanticTokenTypes.Number);
        Assert.Contains(result.Tokens, token => token.Type == AuroraSemanticTokenTypes.Variable);
        Assert.True(result.Tokens.SequenceEqual(result.Tokens.OrderBy(token => token.Line).ThenBy(token => token.Character)));
    }

    [Fact]
    public void ReturnsEmptyTokensForIncompleteSource()
    {
        var service = new AuroraLanguageService(BuiltinApiLoader.LoadFromFile(BuiltinApiCatalogTests.GetRuntimeApiPath()));

        var result = service.GetSemanticTokens("main.as", "export func run(){ return \"unterminated");

        Assert.Empty(result.Tokens);
    }
}
