using AuroraScript.Core;
using AuroraScript.LanguageServices;
using AuroraScript.LanguageServices.Builtins;
using AuroraScript.LanguageServices.Text;
using AuroraScript.LanguageServices.Workspace;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Xunit;

namespace AuroraScript.LanguageServices.Tests;

public sealed class DefinitionFeatureTests : IDisposable
{
    private readonly string _root;

    public DefinitionFeatureTests()
    {
        _root = Path.Combine(Path.GetTempPath(), "aurora-ls-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_root);
    }

    [Fact]
    public void ResolvesImportedModuleMemberDefinition()
    {
        var mainPath = Path.Combine(_root, "main.as");
        var libPath = Path.Combine(_root, "lib.as");
        var main =
            """
            @module(MAIN);
            import lib from './lib';
            export func run() {
                return lib.value;
            }
            """;
        var lib = "@module(LIB); export const value = 42;";
        File.WriteAllText(libPath, lib);
        var service = CreateService();

        var definition = service.GetDefinition(
            mainPath,
            main,
            PositionOf(main, "value"),
            new[] { new AuroraWorkspaceDocument(libPath, lib) });

        Assert.NotNull(definition);
        Assert.Equal(Path.GetFullPath(libPath), definition!.Path);
        Assert.Equal(0, definition.Range.Start.Line);
        Assert.True(definition.Range.Start.Character > 0);
    }

    [Fact]
    public void ResolvesImportedModuleMemberDefinitionFromWorkspaceDocument()
    {
        var mainPath = Path.Combine(_root, "main.as");
        var libPath = Path.Combine(_root, "lib.as");
        var main =
            """
            @module(MAIN);
            import lib from './lib';
            export func run() {
                return lib.value;
            }
            """;
        var lib = "@module(LIB); export const value = 42;";
        var service = CreateService();

        var definition = service.GetDefinition(
            mainPath,
            main,
            PositionOf(main, "value"),
            new[] { new AuroraWorkspaceDocument(libPath, lib) });

        Assert.NotNull(definition);
        Assert.Equal(Path.GetFullPath(libPath), definition!.Path);
    }

    [Fact]
    public void ResolvesImportedModuleMemberDefinitionFromServiceWorkspace()
    {
        var mainPath = Path.Combine(_root, "main.as");
        var libPath = Path.Combine(_root, "lib.as");
        var main =
            """
            @module(MAIN);
            import lib from './lib';
            export func run() {
                return lib.value;
            }
            """;
        var lib = "@module(LIB); export const value = 42;";
        var service = CreateService();
        service.OpenOrUpdateDocument(libPath, lib);
        service.OpenOrUpdateDocument(mainPath, main);

        var definition = service.GetDefinition(mainPath, PositionOf(main, "value"));

        Assert.NotNull(definition);
        Assert.Equal(Path.GetFullPath(libPath), definition!.Path);
    }

    [Fact]
    public void WorkspaceIndexCacheInvalidatesWhenDocumentChanges()
    {
        var mainPath = Path.Combine(_root, "main.as");
        var libPath = Path.Combine(_root, "lib.as");
        var main =
            """
            @module(MAIN);
            import lib from './lib';
            export func run() {
                return lib.value;
            }
            """;
        var firstLib = "@module(LIB); export const value = 1;";
        var secondLib =
            """
            @module(LIB);
            export const other = 1;
            export const value = 2;
            """;
        var service = CreateService();
        service.OpenOrUpdateDocument(libPath, firstLib, version: 1);
        service.OpenOrUpdateDocument(mainPath, main, version: 1);

        var firstDefinition = service.GetDefinition(mainPath, PositionOf(main, "value"));
        service.OpenOrUpdateDocument(libPath, secondLib, version: 2);
        var secondDefinition = service.GetDefinition(mainPath, PositionOf(main, "value"));

        Assert.NotNull(firstDefinition);
        Assert.NotNull(secondDefinition);
        Assert.Equal(0, firstDefinition!.Range.Start.Line);
        Assert.Equal(2, secondDefinition!.Range.Start.Line);
    }

    [Fact]
    public void WorkspaceIndexCacheInvalidatesWhenDiskImportChanges()
    {
        var mainPath = Path.Combine(_root, "main.as");
        var libPath = Path.Combine(_root, "lib.as");
        var main =
            """
            @module(MAIN);
            import lib from './lib';
            export func run() {
                return lib.value;
            }
            """;
        var firstLib = "@module(LIB); export const value = 1;";
        var secondLib =
            """
            @module(LIB);
            export const other = 1;
            export const value = 2;
            """;
        File.WriteAllText(libPath, firstLib);
        var service = CreateService();
        service.OpenOrUpdateDocument(mainPath, main, version: 1);

        var firstDefinition = service.GetDefinition(mainPath, PositionOf(main, "value"));
        File.WriteAllText(libPath, secondLib);
        var secondDefinition = service.GetDefinition(mainPath, PositionOf(main, "value"));

        Assert.NotNull(firstDefinition);
        Assert.NotNull(secondDefinition);
        Assert.Equal(0, firstDefinition!.Range.Start.Line);
        Assert.Equal(2, secondDefinition!.Range.Start.Line);
    }

    [Fact]
    public void ResolvesImportedModuleMemberDefinitionThroughConfiguredResolver()
    {
        const string root = "memory://aurora-ls";
        const string mainPath = "memory://aurora-ls/main.as";
        const string libPath = "memory://aurora-ls/lib.as";
        var main =
            """
            @module(MAIN);
            import lib from './lib';
            export func run() {
                return lib.value;
            }
            """;
        var lib = "@module(LIB); export const value = 42;";
        var resolver = new InMemoryResolver(root, new Dictionary<string, string>(StringComparer.Ordinal)
        {
            [libPath] = lib
        });
        var service = new AuroraLanguageService(new AuroraLanguageServiceOptions(
            BuiltinApiLoader.LoadFromFile(BuiltinApiCatalogTests.GetRuntimeApiPath()))
        {
            BaseDirectory = root,
            SourceResolver = resolver
        });
        service.OpenOrUpdateDocument(mainPath, main);

        var definition = service.GetDefinition(mainPath, PositionOf(main, "value"));

        Assert.NotNull(definition);
        Assert.Equal(libPath, definition!.Path);
    }

    [Fact]
    public void ResolvesIncludedExportDefinition()
    {
        var mainPath = Path.Combine(_root, "main.as");
        var sharedPath = Path.Combine(_root, "shared.as");
        var main =
            """
            @module(MAIN);
            include './shared';
            export func run() {
                return INCLUDED;
            }
            """;
        var shared = "@module(SHARED); export const INCLUDED = 2;";
        File.WriteAllText(sharedPath, shared);
        var service = CreateService();

        var definition = service.GetDefinition(
            mainPath,
            main,
            PositionOf(main, "INCLUDED"),
            new[] { new AuroraWorkspaceDocument(sharedPath, shared) });

        Assert.NotNull(definition);
        Assert.Equal(Path.GetFullPath(sharedPath), definition!.Path);
    }

    [Fact]
    public void ResolvesModuleLevelDefinitionInSameFile()
    {
        var mainPath = Path.Combine(_root, "main.as");
        var main =
            """
            @module(MAIN);
            const value = 42;
            export func run() {
                return value;
            }
            """;
        var service = CreateService();

        var definition = service.GetDefinition(mainPath, main, PositionOfLast(main, "value"));

        Assert.NotNull(definition);
        Assert.Equal(Path.GetFullPath(mainPath), definition!.Path);
        Assert.Equal(1, definition.Range.Start.Line);
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

    private sealed class InMemoryResolver : IScriptSourceResolver
    {
        private readonly string _baseDirectory;
        private readonly IReadOnlyDictionary<string, string> _sources;

        public InMemoryResolver(string baseDirectory, IReadOnlyDictionary<string, string> sources)
        {
            _baseDirectory = baseDirectory;
            _sources = sources;
        }

        public bool TryResolve(
            string baseDirectory,
            string currentSourcePath,
            string requestedPath,
            string extension,
            out ScriptSourceReference source)
        {
            var fullPath = WithExtension(Resolve(currentSourcePath, requestedPath), extension);
            if (!_sources.ContainsKey(fullPath))
            {
                source = default;
                return false;
            }

            source = new ScriptSourceReference(_baseDirectory, fullPath);
            return true;
        }

        public ScriptSource Open(ScriptSourceReference source, Encoding encoding)
        {
            if (!_sources.TryGetValue(source.FullPath, out var text))
            {
                throw new FileNotFoundException("Script source not found.", source.FullPath);
            }

            return new MemoryScriptSource(source.BaseDirectory, source.FullPath, text);
        }

        private static string Resolve(string currentSourcePath, string requestedPath)
        {
            var slash = currentSourcePath.LastIndexOf('/');
            var currentDirectory = slash >= 0 ? currentSourcePath.Substring(0, slash + 1) : currentSourcePath + "/";
            return new Uri(new Uri(currentDirectory, UriKind.Absolute), requestedPath.Replace('\\', '/')).ToString();
        }

        private static string WithExtension(string path, string extension)
        {
            if (string.IsNullOrWhiteSpace(extension))
            {
                return path;
            }

            if (extension[0] != '.')
            {
                extension = "." + extension;
            }

            var slash = path.LastIndexOf('/');
            var dot = path.LastIndexOf('.');
            return dot > slash ? path.Substring(0, dot) + extension : path + extension;
        }
    }
}
