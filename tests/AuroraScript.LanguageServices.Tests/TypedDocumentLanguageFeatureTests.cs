using AuroraScript.LanguageServices.Builtins;
using AuroraScript.LanguageServices.Features.Completion;
using AuroraScript.LanguageServices.Text;
using AuroraScript.LanguageServices.Features.SemanticTokens;
using System;
using System.IO;
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
    public void StructuralTypesProvideSemanticTokensHoverAndDefinitions()
    {
        const string source =
            "export type Point {\n" +
            "    Number x;\n" +
            "    Number y;\n" +
            "}\n" +
            "// Creates a point.\n" +
            "export func make(Number x, Number y) Point {\n" +
            "    return { x: x, y: y };\n" +
            "}\n" +
            "export func accept(Point value) {\n" +
            "    return value as Point;\n" +
            "}\n";
        var service = new AuroraLanguageService(
            BuiltinApiLoader.LoadFromFile(BuiltinApiCatalogTests.GetRuntimeApiPath()));

        Assert.Empty(service.GetDiagnostics("main.as", source));
        var tokens = service.GetSemanticTokens("main.as", source);
        AssertToken(source, tokens, "Point", AuroraSemanticTokenTypes.Type);
        AssertToken(source, tokens, "x", AuroraSemanticTokenTypes.Property);

        var returnType = PositionOf(source, "Point {\n    return");
        var hover = service.GetHover("main.as", source, returnType);
        Assert.NotNull(hover);
        Assert.Contains("type Point", hover!.Contents, StringComparison.Ordinal);
        Assert.NotNull(service.GetDefinition("main.as", source, returnType));

        var functionHover = service.GetHover(
            "main.as",
            source,
            PositionOf(source, "make("));
        Assert.NotNull(functionHover);
        Assert.Contains(
            "func make(Number x, Number y) Point",
            functionHover!.Contents,
            StringComparison.Ordinal);
    }

    [Fact]
    public void ShapeFieldsProvideHoverDefinitionAndCompletions()
    {
        const string source =
            "export type Point {\n" +
            "    Number x;\n" +
            "    Number y;\n" +
            "}\n" +
            "export type Rect {\n" +
            "    Point origin;\n" +
            "    Number width;\n" +
            "}\n" +
            "export func add(Point p) Number {\n" +
            "    return p.x + p.y;\n" +
            "}\n" +
            "export func left(Rect rect) Number {\n" +
            "    return rect.origin.x + rect.width;\n" +
            "}\n";
        var service = new AuroraLanguageService(
            BuiltinApiLoader.LoadFromFile(BuiltinApiCatalogTests.GetRuntimeApiPath()));

        var fieldHover = service.GetHover("main.as", source, PositionAfter(source, "return p."));
        Assert.NotNull(fieldHover);
        Assert.Contains("Number x", fieldHover!.Contents, StringComparison.Ordinal);

        var nestedHover = service.GetHover(
            "main.as",
            source,
            PositionAfter(source, "rect.origin."));
        Assert.NotNull(nestedHover);
        Assert.Contains("Number x", nestedHover!.Contents, StringComparison.Ordinal);

        var originHover = service.GetHover("main.as", source, PositionAfter(source, "rect."));
        Assert.NotNull(originHover);
        Assert.Contains("Point origin", originHover!.Contents, StringComparison.Ordinal);

        var definition = service.GetDefinition("main.as", source, PositionAfter(source, "return p."));
        Assert.NotNull(definition);
        Assert.Equal(
            PositionOf(source, "Number x;").Line,
            definition!.Range.Start.Line);

        var nestedDefinition = service.GetDefinition(
            "main.as",
            source,
            PositionAfter(source, "rect.origin."));
        Assert.NotNull(nestedDefinition);
        Assert.Equal(
            PositionOf(source, "Number x;").Line,
            nestedDefinition!.Range.Start.Line);

        var pointCompletions = service.GetCompletions(
            "main.as",
            source,
            PositionAfter(source, "return p."));
        Assert.Contains(pointCompletions.Items, item => item.Label == "x" && item.Kind == CompletionItemKind.Property);
        Assert.Contains(pointCompletions.Items, item => item.Label == "y");
        Assert.DoesNotContain(pointCompletions.Items, item => item.Label == "Math");

        var originCompletions = service.GetCompletions(
            "main.as",
            source,
            PositionAfter(source, "return rect.origin."));
        Assert.Contains(originCompletions.Items, item => item.Label == "x");
        Assert.Contains(originCompletions.Items, item => item.Label == "y");
        Assert.DoesNotContain(originCompletions.Items, item => item.Label == "width");
    }

    [Fact]
    public void QualifiedImportedTypesProvideHoverAndDefinition()
    {
        var root = Path.Combine(
            Path.GetTempPath(),
            "aurora-type-index-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        try
        {
            var modelsPath = Path.Combine(root, "models.as");
            var mainPath = Path.Combine(root, "main.as");
            const string models =
                "export type Point { Number x; Number y; }\n";
            const string main =
                "import models from './models';\n" +
                "export func add(models.Point p) Number {\n" +
                "    return p.x + p.y;\n" +
                "}\n";
            File.WriteAllText(modelsPath, models);

            var service = new AuroraLanguageService(
                new AuroraLanguageServiceOptions(
                    BuiltinApiLoader.LoadFromFile(
                        BuiltinApiCatalogTests.GetRuntimeApiPath()))
                {
                    BaseDirectory = root,
                    IndexWorkspaceFiles = true
                });
            service.OpenOrUpdateDocument(modelsPath, models);
            service.OpenOrUpdateDocument(mainPath, main);

            var pointPosition = PositionOf(main, "Point p");
            var hover = service.GetHover(mainPath, pointPosition);
            Assert.NotNull(hover);
            Assert.Contains("export type Point", hover!.Contents, StringComparison.Ordinal);

            var definition = service.GetDefinition(mainPath, pointPosition);
            Assert.NotNull(definition);
            Assert.Equal(
                Path.GetFullPath(modelsPath),
                Path.GetFullPath(definition!.Path));

            var qualifierDefinition = service.GetDefinition(
                mainPath,
                PositionOf(main, "models.Point"));
            Assert.NotNull(qualifierDefinition);
            Assert.Equal(
                Path.GetFullPath(mainPath),
                Path.GetFullPath(qualifierDefinition!.Path));

            var fieldHover = service.GetHover(mainPath, PositionAfter(main, "return p."));
            Assert.NotNull(fieldHover);
            Assert.Contains("Number x", fieldHover!.Contents, StringComparison.Ordinal);

            var fieldCompletions = service.GetCompletions(
                mainPath,
                PositionAfter(main, "return p."));
            Assert.Contains(fieldCompletions.Items, item => item.Label == "x");
            Assert.Contains(fieldCompletions.Items, item => item.Label == "y");
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
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
        return PositionAtOffset(source, offset);
    }

    private static TextPosition PositionAfter(string source, string text)
    {
        var offset = source.IndexOf(text, StringComparison.Ordinal);
        Assert.True(offset >= 0, $"Source does not contain '{text}'.");
        return PositionAtOffset(source, offset + text.Length);
    }

    private static TextPosition PositionAtOffset(string source, int offset)
    {
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
