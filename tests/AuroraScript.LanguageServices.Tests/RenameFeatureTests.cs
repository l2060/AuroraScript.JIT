using AuroraScript.LanguageServices;
using AuroraScript.LanguageServices.Builtins;
using AuroraScript.LanguageServices.Text;
using AuroraScript.LanguageServices.Workspace;
using System;
using System.IO;
using System.Linq;
using Xunit;

namespace AuroraScript.LanguageServices.Tests;

public sealed class RenameFeatureTests : IDisposable
{
    private readonly string _root;

    public RenameFeatureTests()
    {
        _root = Path.Combine(Path.GetTempPath(), "aurora-rename-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_root);
    }

    [Fact]
    public void RenamesLocalVariableWithoutEditingShadowedLocal()
    {
        var mainPath = Path.Combine(_root, "main.as");
        var main =
            """
            @module(MAIN);
            export func run(value) {
                var local = value;
                {
                    var value = 2;
                    local = local + value;
                }
                return local + value;
            }
            """;
        var service = CreateService();

        var result = service.Rename(mainPath, main, PositionOfLast(main, "value"), "nextValue");

        Assert.True(result.Success, result.ErrorMessage);
        var changes = Assert.Single(result.Changes);
        Assert.True(PathEquals(mainPath, changes.Path));
        Assert.Equal(3, changes.Edits.Count);
        Assert.DoesNotContain(changes.Edits, edit => edit.Range.Start.Line == 4);
        Assert.DoesNotContain(changes.Edits, edit => edit.Range.Start.Line == 5);
        Assert.All(changes.Edits, edit => Assert.Equal("nextValue", edit.NewText));
    }

    [Fact]
    public void RenamesImportedExportAcrossWorkspaceDocuments()
    {
        var libPath = Path.Combine(_root, "lib.as");
        var mainPath = Path.Combine(_root, "main.as");
        var otherPath = Path.Combine(_root, "other.as");
        var lib = "@module(LIB); export const value = 42;";
        var main =
            """
            @module(MAIN);
            import lib from './lib';
            export func run() { return lib.value; }
            """;
        var other =
            """
            @module(OTHER);
            import lib from './lib';
            export func run() { return lib.value; }
            """;
        File.WriteAllText(libPath, lib);
        var service = CreateService();

        var result = service.Rename(
            mainPath,
            main,
            PositionOf(main, "value"),
            "total",
            new[]
            {
                new AuroraWorkspaceDocument(libPath, lib),
                new AuroraWorkspaceDocument(otherPath, other)
            });

        Assert.True(result.Success, result.ErrorMessage);
        Assert.Equal(3, result.Changes.Sum(change => change.Edits.Count));
        Assert.Contains(result.Changes, change => PathEquals(change.Path, libPath));
        Assert.Contains(result.Changes, change => PathEquals(change.Path, mainPath));
        Assert.Contains(result.Changes, change => PathEquals(change.Path, otherPath));
    }

    [Theory]
    [InlineData("1value")]
    [InlineData("return")]
    [InlineData("bad-name")]
    public void RejectsInvalidRenameIdentifier(string newName)
    {
        var mainPath = Path.Combine(_root, "main.as");
        var main =
            """
            @module(MAIN);
            export func run(value) {
                return value;
            }
            """;
        var service = CreateService();

        var result = service.Rename(mainPath, main, PositionOfLast(main, "value"), newName);

        Assert.False(result.Success);
        Assert.Empty(result.Changes);
    }

    public void Dispose()
    {
        if (Directory.Exists(_root))
        {
            Directory.Delete(_root, recursive: true);
        }
    }

    private static AuroraLanguageService CreateService()
    {
        return new AuroraLanguageService(BuiltinApiLoader.LoadFromFile(BuiltinApiCatalogTests.GetRuntimeApiPath()));
    }

    private static bool PathEquals(string left, string right)
    {
        return string.Equals(Path.GetFullPath(left), Path.GetFullPath(right), OperatingSystem.IsWindows() ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal);
    }

    private static TextPosition PositionOf(string source, string needle)
    {
        return PositionAtOffset(source, source.IndexOf(needle, StringComparison.Ordinal));
    }

    private static TextPosition PositionOfLast(string source, string needle)
    {
        return PositionAtOffset(source, source.LastIndexOf(needle, StringComparison.Ordinal));
    }

    private static TextPosition PositionAtOffset(string source, int offset)
    {
        Assert.True(offset >= 0);
        var line = 0;
        var character = 0;
        for (var i = 0; i < offset; i++)
        {
            if (source[i] == '\r')
            {
                if (i + 1 < offset && source[i + 1] == '\n')
                {
                    i++;
                }
                line++;
                character = 0;
            }
            else if (source[i] == '\n')
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
}
