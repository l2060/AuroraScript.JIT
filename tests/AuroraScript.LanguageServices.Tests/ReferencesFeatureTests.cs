using AuroraScript.LanguageServices;
using AuroraScript.LanguageServices.Builtins;
using AuroraScript.LanguageServices.Text;
using AuroraScript.LanguageServices.Workspace;
using System;
using System.IO;
using System.Linq;
using Xunit;

namespace AuroraScript.LanguageServices.Tests;

public sealed class ReferencesFeatureTests : IDisposable
{
    private readonly string _root;

    public ReferencesFeatureTests()
    {
        _root = Path.Combine(Path.GetTempPath(), "aurora-ref-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_root);
    }

    [Fact]
    public void FindsReferencesForImportedExport()
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
            export func run() { return lib.value + lib.value; }
            """;
        File.WriteAllText(libPath, lib);
        var service = CreateService();

        var references = service.GetReferences(
            mainPath,
            main,
            PositionOf(main, "value"),
            includeDeclaration: true,
            new[]
            {
                new AuroraWorkspaceDocument(libPath, lib),
                new AuroraWorkspaceDocument(otherPath, other)
            });

        Assert.Equal(4, references.Count);
        Assert.Contains(references, reference => PathEquals(reference.Path, libPath));
        Assert.Equal(3, references.Count(reference => !PathEquals(reference.Path, libPath)));
    }

    [Fact]
    public void FindsReferencesForIncludedExport()
    {
        var sharedPath = Path.Combine(_root, "shared.as");
        var mainPath = Path.Combine(_root, "main.as");
        var shared = "@module(SHARED); export const INCLUDED = 2;";
        var main =
            """
            @module(MAIN);
            include './shared';
            export func run() { return INCLUDED + INCLUDED; }
            """;
        File.WriteAllText(sharedPath, shared);
        var service = CreateService();

        var references = service.GetReferences(
            mainPath,
            main,
            PositionOf(main, "INCLUDED"),
            includeDeclaration: true,
            new[] { new AuroraWorkspaceDocument(sharedPath, shared) });

        Assert.Equal(3, references.Count);
        Assert.Contains(references, reference => PathEquals(reference.Path, sharedPath));
        Assert.Equal(2, references.Count(reference => PathEquals(reference.Path, mainPath)));
    }

    [Fact]
    public void FindsReferencesForSameFileModuleSymbol()
    {
        var mainPath = Path.Combine(_root, "main.as");
        var main =
            """
            @module(MAIN);
            const value = 42;
            export func run() { return value + value; }
            """;
        var service = CreateService();

        var references = service.GetReferences(mainPath, main, PositionOfLast(main, "value"), includeDeclaration: true);

        Assert.Equal(3, references.Count);
        Assert.All(references, reference => Assert.True(PathEquals(reference.Path, mainPath)));
    }

    [Fact]
    public void FindsReferencesForLocalVariableWithoutRenamingShadowedNames()
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

        var references = service.GetReferences(mainPath, main, PositionOfLast(main, "value"), includeDeclaration: true);

        Assert.Equal(3, references.Count);
        Assert.All(references, reference => Assert.True(PathEquals(reference.Path, mainPath)));
        Assert.Contains(references, reference => reference.Range.Start.Line == 1);
        Assert.DoesNotContain(references, reference => reference.Range.Start.Line == 4);
        Assert.DoesNotContain(references, reference => reference.Range.Start.Line == 5);
    }

    [Fact]
    public void DoesNotTreatPropertyNameAsModuleSymbolReference()
    {
        var mainPath = Path.Combine(_root, "main.as");
        var main =
            """
            @module(MAIN);
            const value = 42;
            export func run(obj) { return obj.value + value; }
            """;
        var service = CreateService();

        var references = service.GetReferences(mainPath, main, PositionOfLast(main, "value"), includeDeclaration: true);

        Assert.Equal(2, references.Count);
        Assert.DoesNotContain(references, reference => reference.Range.Start.Line == 2 && reference.Range.Start.Character < 40);
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
