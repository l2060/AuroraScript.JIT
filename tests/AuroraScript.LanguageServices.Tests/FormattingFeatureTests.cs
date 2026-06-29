using AuroraScript.LanguageServices;
using AuroraScript.LanguageServices.Builtins;
using AuroraScript.LanguageServices.Features.Formatting;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Xunit;

namespace AuroraScript.LanguageServices.Tests;

public sealed class FormattingFeatureTests
{
    public static IEnumerable<object[]> FormattingFixtures()
    {
        var directory = Path.Combine(AppContext.BaseDirectory, "FormattingFixtures");
        foreach (var inputPath in Directory.EnumerateFiles(directory, "*.as").OrderBy(path => path))
        {
            if (inputPath.EndsWith(".formatted.as", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            var expectedPath = Path.Combine(
                Path.GetDirectoryName(inputPath)!,
                Path.GetFileNameWithoutExtension(inputPath) + ".formatted.as");
            yield return new object[] { inputPath, expectedPath };
        }
    }

    [Theory]
    [MemberData(nameof(FormattingFixtures))]
    public void FormatsGoldenFixturesAndIsIdempotent(string inputPath, string expectedPath)
    {
        var service = CreateService();
        var source = File.ReadAllText(inputPath);
        var expected = File.ReadAllText(expectedPath);

        var result = service.FormatDocument(inputPath, source, new FormattingOptions());
        var edit = Assert.Single(result.Edits);
        Assert.Equal(expected, edit.NewText);

        var second = service.FormatDocument(inputPath, edit.NewText, new FormattingOptions());
        Assert.Empty(second.Edits);
    }

    [Fact]
    public void FormatsIndentationAndTrimsTrailingWhitespace()
    {
        const string source = "@module(TEST);\nexport func run() {\nconst value = 1;   \nif (value) {\nreturn value;\n}\n}\n";
        const string expected = "@module(TEST);\nexport func run() {\n    const value = 1;\n    if (value) {\n        return value;\n    }\n}\n";
        var service = CreateService();

        var result = service.FormatDocument("test.as", source, new FormattingOptions(4, insertSpaces: true));

        var edit = Assert.Single(result.Edits);
        Assert.Equal(expected, edit.NewText);
    }

    [Fact]
    public void PreservesMultilineCommentsStringsAndTemplates()
    {
        const string source =
            "@module(TEST);\n" +
            "export func run() {\n" +
            "/*\n" +
            "  keep this indentation\n" +
            "*/\n" +
            "const text = `{\n" +
            "  keep template\n" +
            "}`;\n" +
            "return text;\n" +
            "}\n";
        var service = CreateService();

        var result = service.FormatDocument("test.as", source, new FormattingOptions(2, insertSpaces: true));

        var edit = Assert.Single(result.Edits);
        Assert.Contains("  keep this indentation", edit.NewText);
        Assert.Contains("  keep template", edit.NewText);
        Assert.Contains("  return text;", edit.NewText);
    }

    [Fact]
    public void AlignsStringBlockPrefixes()
    {
        const string source =
            "@module(TEST);\n" +
            "export func run(){\n" +
            "const block=\n" +
            "|> first\n" +
            "  |> second\n" +
            ";\n" +
            "return block;\n" +
            "}\n";
        var service = CreateService();

        var result = service.FormatDocument("test.as", source, new FormattingOptions());

        var edit = Assert.Single(result.Edits);
        Assert.Contains("    |> first\n    |> second", edit.NewText);
    }

    [Fact]
    public void ReturnsNoEditsWhenDocumentAlreadyMatchesFormatting()
    {
        const string source = "@module(TEST);\nexport func run() {\n    return 1;\n}\n";
        var service = CreateService();

        var result = service.FormatDocument("test.as", source, new FormattingOptions());

        Assert.Empty(result.Edits);
    }

    [Fact]
    public void FormatsRecoverableIncompleteSource()
    {
        const string source = "export func run() {\nreturn \"unterminated\n";
        var service = CreateService();

        var result = service.FormatDocument("test.as", source, new FormattingOptions());

        var edit = Assert.Single(result.Edits);
        Assert.Contains("    return \"unterminated", edit.NewText);
    }

    private static AuroraLanguageService CreateService()
    {
        return new AuroraLanguageService(BuiltinApiLoader.LoadFromFile(BuiltinApiCatalogTests.GetRuntimeApiPath()));
    }
}
