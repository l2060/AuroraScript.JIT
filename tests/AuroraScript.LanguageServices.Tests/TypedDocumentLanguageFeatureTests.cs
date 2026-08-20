using AuroraScript.LanguageServices.Builtins;
using AuroraScript.LanguageServices.Features.SemanticTokens;
using System;
using Xunit;

namespace AuroraScript.LanguageServices.Tests;

public sealed class TypedDocumentLanguageFeatureTests
{
    [Fact]
    public void StandaloneTDocDocumentsUseTDocParserAndSemanticTokens()
    {
        const string source =
            "Object { readonly String id \"UX01\", tags [String \"system\", Number 4], " +
            "Object nested { Boolean enabled true, }, }";
        var service = new AuroraLanguageService(
            BuiltinApiLoader.LoadFromFile(BuiltinApiCatalogTests.GetRuntimeApiPath()));

        Assert.Empty(service.GetDiagnostics("config.tdoc", source));
        var result = service.GetSemanticTokens("config.tdoc", source);

        AssertToken(source, result, "Object", AuroraSemanticTokenTypes.Type);
        AssertToken(source, result, "String", AuroraSemanticTokenTypes.Type);
        AssertToken(source, result, "Number", AuroraSemanticTokenTypes.Type);
        AssertToken(source, result, "readonly", AuroraSemanticTokenTypes.Keyword);
        AssertToken(source, result, "id", AuroraSemanticTokenTypes.MapKey);
        AssertToken(source, result, "tags", AuroraSemanticTokenTypes.MapKey);
        AssertToken(source, result, "nested", AuroraSemanticTokenTypes.MapKey);
        AssertToken(source, result, "enabled", AuroraSemanticTokenTypes.MapKey);
        AssertToken(source, result, "\"UX01\"", AuroraSemanticTokenTypes.String);
        AssertToken(source, result, "4", AuroraSemanticTokenTypes.Number);
    }

    [Fact]
    public void StandaloneTDocDocumentsRejectScriptInterpolation()
    {
        var service = new AuroraLanguageService(
            BuiltinApiLoader.LoadFromFile(BuiltinApiCatalogTests.GetRuntimeApiPath()));

        var diagnostics = service.GetDiagnostics("config.tdoc", "Object { id $(value) }");

        var diagnostic = Assert.Single(diagnostics);
        Assert.Contains("Standalone TDoc", diagnostic.Message, StringComparison.Ordinal);
    }

    private static void AssertToken(string source, SemanticTokensResult result, string text, int type)
    {
        Assert.Contains(result.Tokens, token =>
            token.Type == type &&
            TokenText(source, token) == text);
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
