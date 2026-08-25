using AuroraScript.LanguageServices.Builtins;
using AuroraScript.LanguageServices.Features.SemanticTokens;
using AuroraScript.LanguageServices.Text;
using System;
using Xunit;

namespace AuroraScript.LanguageServices.Tests;

public sealed class TypedDocumentLanguageFeatureTests
{
    [Fact]
    public void TypeAssertionsAndTypedParametersUseSemanticTokens()
    {
        const string source =
            "export func convert(Number value) { return value as Number; }";
        var service = new AuroraLanguageService(
            BuiltinApiLoader.LoadFromFile(BuiltinApiCatalogTests.GetRuntimeApiPath()));

        Assert.Empty(service.GetDiagnostics("main.as", source));
        var result = service.GetSemanticTokens("main.as", source);

        AssertToken(source, result, "as", AuroraSemanticTokenTypes.Keyword);
        AssertToken(source, result, "Number", AuroraSemanticTokenTypes.Type);
    }

    [Fact]
    public void TypedParameterAndAssertionTypesResolveToBuiltinTypes()
    {
        const string source =
            "// Adds a scalar to the first packed element.\n" +
            "export func add(Number value, Float64Array items) {\n" +
            "\tvar buffer = items as Float64Array;\n" +
            "\treturn value + buffer[0];\n" +
            "}\n";
        var service = new AuroraLanguageService(
            BuiltinApiLoader.LoadFromFile(BuiltinApiCatalogTests.GetRuntimeApiPath()));

        Assert.Empty(service.GetDiagnostics("main.as", source));

        var parameterTypeHover = service.GetHover("main.as", source, PositionOf(source, "Number value"));
        Assert.NotNull(parameterTypeHover);
        Assert.Contains("Number", parameterTypeHover!.Contents, StringComparison.Ordinal);

        var packedTypeHover = service.GetHover("main.as", source, PositionOf(source, "Float64Array items"));
        Assert.NotNull(packedTypeHover);
        Assert.Contains("Float64Array", packedTypeHover!.Contents, StringComparison.Ordinal);

        var assertionTypeHover = service.GetHover("main.as", source, PositionOf(source, "Float64Array;"));
        Assert.NotNull(assertionTypeHover);
        Assert.Contains("Float64Array", assertionTypeHover!.Contents, StringComparison.Ordinal);

        Assert.NotNull(service.GetDefinition("main.as", source, PositionOf(source, "Number value")));
        Assert.NotNull(service.GetDefinition("main.as", source, PositionOf(source, "Float64Array;")));

        var functionHover = service.GetHover("main.as", source, PositionOf(source, "add("));
        Assert.NotNull(functionHover);
        Assert.Contains(
            "func add(Number value, Float64Array items)",
            functionHover!.Contents,
            StringComparison.Ordinal);
    }

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

    [Fact]
    public void StandaloneTDocDiagnosticsCoverBuiltinShapesRangesDatesAndScientificNotation()
    {
        var service = new AuroraLanguageService(
            BuiltinApiLoader.LoadFromFile(BuiltinApiCatalogTests.GetRuntimeApiPath()));

        const string valid =
            "Object { String \"display name\" \"Aurora\", Float64Array values [-2.5e2, 1e3], User user { name \"Hanks\" }, }";
        Assert.Empty(service.GetDiagnostics("config.tdoc", valid));

        var range = Assert.Single(service.GetDiagnostics(
            "config.tdoc",
            "Object { UInt8Array bytes [0, 256] }"));
        Assert.Contains("$.bytes[1]", range.Message, StringComparison.Ordinal);

        var shape = Assert.Single(service.GetDiagnostics(
            "config.tdoc",
            "Object { String name 42 }"));
        Assert.Contains("$.name", shape.Message, StringComparison.Ordinal);

        var date = Assert.Single(service.GetDiagnostics(
            "config.tdoc",
            "Object { Date createdAt \"2026-08-19 21:08:07 123\" }"));
        Assert.Contains("$.createdAt", date.Message, StringComparison.Ordinal);
        Assert.Contains("DateTimeFormat", date.Message, StringComparison.Ordinal);
    }

    private static TextPosition PositionOf(string source, string text)
    {
        var offset = source.IndexOf(text, StringComparison.Ordinal);
        Assert.True(offset >= 0, $"Source does not contain '{text}'.");

        var line = 0;
        var character = 0;
        for (var i = 0; i < offset; i++)
        {
            if (source[i] == '\n')
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
