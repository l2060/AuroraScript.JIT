using AuroraScript.Core;
using AuroraScript.LanguageServices;
using AuroraScript.LanguageServices.Builtins;
using AuroraScript.LanguageServices.Text;
using AuroraScript.LanguageServices.Workspace;
using AuroraScript.Source;
using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
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

    [Fact]
    public void ResolvesBuiltinMemberDefinitionToVirtualDocument()
    {
        var mainPath = Path.Combine(_root, "main.as");
        var main =
            """
            @module(MAIN);
            export func run() {
                return Math.abs(-1);
            }
            """;
        var service = CreateService();

        var definition = service.GetDefinition(mainPath, main, PositionOf(main, "abs"));

        Assert.NotNull(definition);
        Assert.Equal("aurora-builtin:/Math.as", definition!.Path);
        var document = service.GetBuiltinDocument(definition.Path);
        Assert.NotNull(document);
        Assert.Contains("export declare Math.abs(value: Number): Number;", document!.Text, StringComparison.Ordinal);
        Assert.Contains("export declare Math.PI: Number;", document.Text, StringComparison.Ordinal);
    }

    [Fact]
    public void ResolvesBuiltinMemberDefinitionBeforeIncludedExportWithSameName()
    {
        var mainPath = Path.Combine(_root, "main.as");
        var constantPath = Path.Combine(_root, "constant.as");
        var main =
            """
            @module(MAIN);
            include './constant';
            export func run() {
                console.log("reset");
            }
            """;
        var constant =
            """
            @module(CONSTANT);
            export func log() {
            }
            """;
        var service = CreateService();

        var definition = service.GetDefinition(
            mainPath,
            main,
            PositionOf(main, "log"),
            new[] { new AuroraWorkspaceDocument(constantPath, constant) });

        Assert.NotNull(definition);
        Assert.Equal("aurora-builtin:/console.as", definition!.Path);
        var document = service.GetBuiltinDocument(definition.Path);
        Assert.NotNull(document);
        Assert.Contains("export declare console.log(...values: Object[]): void;", document!.Text, StringComparison.Ordinal);
    }

    [Fact]
    public void ResolvesBuiltinObjectNameBeforeBuiltinMemberName()
    {
        var mainPath = Path.Combine(_root, "main.as");
        var constantPath = Path.Combine(_root, "constant.as");
        var main =
            """
            @module(MAIN);
            include './constant';
            export func run() {
                console.log("reset");
            }
            """;
        var constant =
            """
            @module(CONSTANT);
            export func log() {
            }
            """;
        var service = CreateService();

        var consoleDefinition = service.GetDefinition(
            mainPath,
            main,
            PositionOf(main, "console"),
            new[] { new AuroraWorkspaceDocument(constantPath, constant) });
        var logDefinition = service.GetDefinition(
            mainPath,
            main,
            PositionOf(main, "log"),
            new[] { new AuroraWorkspaceDocument(constantPath, constant) });

        Assert.NotNull(consoleDefinition);
        Assert.Equal("aurora-builtin:/console.as", consoleDefinition!.Path);
        var consoleDocument = service.GetBuiltinDocument(consoleDefinition.Path);
        Assert.NotNull(consoleDocument);
        Assert.Contains("export declare console;", consoleDocument!.Text, StringComparison.Ordinal);
        Assert.NotNull(logDefinition);
        Assert.Equal("aurora-builtin:/console.as", logDefinition!.Path);
        var document = service.GetBuiltinDocument(logDefinition.Path);
        Assert.NotNull(document);
        Assert.Contains("export declare console.log(...values: Object[]): void;", document!.Text, StringComparison.Ordinal);
    }

    [Fact]
    public void ResolvesBuiltinMemberDefinitionBeforeWorkspaceSymbolWithSameName()
    {
        var mainPath = Path.Combine(_root, "main.as");
        var constantPath = Path.Combine(_root, "constant.as");
        var main =
            """
            @module(MAIN);
            include './constant';
            export func run() {
                console.log("reset");
            }
            """;
        var constant =
            """
            @module(CONSTANT);
            export func log() {
            }
            """;
        File.WriteAllText(mainPath, main);
        File.WriteAllText(constantPath, constant);
        var service = CreateService();
        service.OpenOrUpdateDocument(mainPath, main);
        service.OpenOrUpdateDocument(constantPath, constant);

        var definition = service.GetDefinition(mainPath, PositionOf(main, "log"));

        Assert.NotNull(definition);
        Assert.Equal("aurora-builtin:/console.as", definition!.Path);
    }

    [Fact]
    public void ResolvesPlainObjectPropertyNameWithoutUsingIncludedExportWithSameName()
    {
        var mainPath = Path.Combine(_root, "main.as");
        var constantPath = Path.Combine(_root, "constant.as");
        var main =
            """
            @module(MAIN);
            include './constant';
            export func run() {
                const local = { log: 1 };
                return local.log;
            }
            """;
        var constant =
            """
            @module(CONSTANT);
            export func log() {
            }
            """;
        var service = CreateService();

        var definition = service.GetDefinition(
            mainPath,
            main,
            PositionOfLast(main, "log"),
            new[] { new AuroraWorkspaceDocument(constantPath, constant) });

        Assert.NotNull(definition);
        Assert.Equal(Path.GetFullPath(mainPath), definition!.Path);
        Assert.Equal(3, definition.Range.Start.Line);
    }

    [Fact]
    public void ResolvesObjectLiteralMemberDefinition()
    {
        var mainPath = Path.Combine(_root, "main.as");
        var main =
            """
            @module(MAIN);
            export func run() {
                const timer = {
                    reset: () => 0,
                    count: 1
                };
                timer.reset();
                return timer.count;
            }
            """;
        var service = CreateService();

        var resetDefinition = service.GetDefinition(mainPath, main, PositionOfLast(main, "reset"));
        var countDefinition = service.GetDefinition(mainPath, main, PositionOfLast(main, "count"));

        Assert.NotNull(resetDefinition);
        Assert.Equal(Path.GetFullPath(mainPath), resetDefinition!.Path);
        Assert.Equal(3, resetDefinition.Range.Start.Line);
        Assert.NotNull(countDefinition);
        Assert.Equal(Path.GetFullPath(mainPath), countDefinition!.Path);
        Assert.Equal(4, countDefinition.Range.Start.Line);
    }

    [Fact]
    public void ResolvesAssignedObjectMemberDefinition()
    {
        var mainPath = Path.Combine(_root, "main.as");
        var main =
            """
            @module(MAIN);
            export func run() {
                const timer = {};
                timer.reset = () => 0;
                return timer.reset();
            }
            """;
        var service = CreateService();

        var definition = service.GetDefinition(mainPath, main, PositionOfLast(main, "reset"));

        Assert.NotNull(definition);
        Assert.Equal(Path.GetFullPath(mainPath), definition!.Path);
        Assert.Equal(3, definition.Range.Start.Line);
    }

    [Fact]
    public void ResolvesImportedFactoryObjectMemberDefinition()
    {
        var mainPath = Path.Combine(_root, "main.as");
        var timerPath = Path.Combine(_root, "timer.as");
        var main =
            """
            @module(MAIN);
            import time from './timer';
            export func run() {
                var timer = time.createTimer();
                timer.reset();
                return timer.count;
            }
            """;
        var timer =
            """
            @module(TIMER);
            export function createTimer() {
                var timer = {
                    count: 50,
                    reset: () => {
                        timer.count = 0;
                    }
                };
                return Object(timer);
            }
            """;
        File.WriteAllText(timerPath, timer);
        var service = CreateService();

        var resetDefinition = service.GetDefinition(
            mainPath,
            main,
            PositionOfLast(main, "reset"),
            new[] { new AuroraWorkspaceDocument(timerPath, timer) });
        var countDefinition = service.GetDefinition(
            mainPath,
            main,
            PositionOfLast(main, "count"),
            new[] { new AuroraWorkspaceDocument(timerPath, timer) });

        Assert.NotNull(resetDefinition);
        Assert.Equal(Path.GetFullPath(timerPath), resetDefinition!.Path);
        Assert.Equal(4, resetDefinition.Range.Start.Line);
        Assert.NotNull(countDefinition);
        Assert.Equal(Path.GetFullPath(timerPath), countDefinition!.Path);
        Assert.Equal(3, countDefinition.Range.Start.Line);
    }

    [Fact]
    public void ResolvesLocalFactoryObjectMemberDefinition()
    {
        var mainPath = Path.Combine(_root, "main.as");
        var main =
            """
            @module(MAIN);
            function createContext() {
                return {
                    failures: [],
                    passed: true
                };
            }
            export func run() {
                var ctx = createContext();
                ctx.failures.push("x");
                return ctx.passed;
            }
            """;
        var service = CreateService();

        var failuresDefinition = service.GetDefinition(mainPath, main, PositionOfLast(main, "failures"));
        var passedDefinition = service.GetDefinition(mainPath, main, PositionOfLast(main, "passed"));

        Assert.NotNull(failuresDefinition);
        Assert.Equal(Path.GetFullPath(mainPath), failuresDefinition!.Path);
        Assert.Equal(3, failuresDefinition.Range.Start.Line);
        Assert.NotNull(passedDefinition);
        Assert.Equal(Path.GetFullPath(mainPath), passedDefinition!.Path);
        Assert.Equal(4, passedDefinition.Range.Start.Line);
    }

    [Fact]
    public void ResolvesFactoryObjectMemberForMatchingLocalDeclaration()
    {
        var mainPath = Path.Combine(_root, "main.as");
        var main =
            """
            @module(MAIN);
            function createFirst() {
                return { passed: false };
            }
            function createSecond() {
                return { passed: true };
            }
            export func first() {
                var ctx = createFirst();
                return ctx.passed;
            }
            export func second() {
                var ctx = createSecond();
                return ctx.passed;
            }
            """;
        var service = CreateService();

        var definition = service.GetDefinition(mainPath, main, PositionOfLast(main, "passed"));

        Assert.NotNull(definition);
        Assert.Equal(Path.GetFullPath(mainPath), definition!.Path);
        Assert.Equal(5, definition.Range.Start.Line);
    }

    [Fact]
    public void ResolvesBuiltinGlobalDefinitionToVirtualDocument()
    {
        var mainPath = Path.Combine(_root, "main.as");
        var main =
            """
            @module(MAIN);
            export func run() {
                return Math.PI;
            }
            """;
        var service = CreateService();

        var definition = service.GetDefinition(mainPath, main, PositionOf(main, "Math"));

        Assert.NotNull(definition);
        Assert.Equal("aurora-builtin:/Math.as", definition!.Path);
        var document = service.GetBuiltinDocument(definition.Path);
        Assert.NotNull(document);
        Assert.Contains("export declare Math;", document!.Text, StringComparison.Ordinal);
    }

    [Fact]
    public void ResolvesBuiltinStringGlobalDefinitionToVirtualDocument()
    {
        var mainPath = Path.Combine(_root, "main.as");
        var main =
            """
            @module(MAIN);
            export func run() {
                return String.fromCharCode(65);
            }
            """;
        var service = CreateService();

        var definition = service.GetDefinition(mainPath, main, PositionOf(main, "String"));

        Assert.NotNull(definition);
        Assert.Equal("aurora-builtin:/String.as", definition!.Path);
    }

    [Fact]
    public void ResolvesBuiltinStringMemberDefinitionToVirtualDocument()
    {
        var mainPath = Path.Combine(_root, "main.as");
        var main =
            """
            @module(MAIN);
            export func run() {
                return String.fromCharCode(65);
            }
            """;
        var service = CreateService();

        var definition = service.GetDefinition(mainPath, main, PositionOf(main, "fromCharCode"));

        Assert.NotNull(definition);
        Assert.Equal("aurora-builtin:/String.as", definition!.Path);
        var document = service.GetBuiltinDocument(definition.Path);
        Assert.NotNull(document);
        Assert.Contains("export declare String.fromCharCode(charCode: Number): String;", document!.Text, StringComparison.Ordinal);
    }

    [Fact]
    public void ResolvesBuiltinStringDefinitionsInMd5Example()
    {
        var sourcePath = FindRepositoryFile("examples", "tests", "md5.as");
        var source = File.ReadAllText(sourcePath);
        var service = CreateService();

        var firstStringDefinition = service.GetDefinition(sourcePath, source, PositionOf(source, "String.fromCharCode"));
        var firstMemberDefinition = service.GetDefinition(sourcePath, source, PositionOf(source, "fromCharCode"));
        var lastStringDefinition = service.GetDefinition(sourcePath, source, PositionOfLast(source, "String.fromCharCode"));
        var lastMemberDefinition = service.GetDefinition(sourcePath, source, PositionOfLast(source, "fromCharCode"));

        Assert.NotNull(firstStringDefinition);
        Assert.Equal("aurora-builtin:/String.as", firstStringDefinition!.Path);
        Assert.NotNull(firstMemberDefinition);
        Assert.Equal("aurora-builtin:/String.as", firstMemberDefinition!.Path);
        Assert.NotNull(lastStringDefinition);
        Assert.Equal("aurora-builtin:/String.as", lastStringDefinition!.Path);
        Assert.NotNull(lastMemberDefinition);
        Assert.Equal("aurora-builtin:/String.as", lastMemberDefinition!.Path);
    }

    [Fact]
    public void ResolvesCompilerProvidedGlobalDefinitionToVirtualDocument()
    {
        var mainPath = Path.Combine(_root, "main.as");
        var main =
            """
            @module(MAIN);
            export func run() {
                return global.modules;
            }
            """;
        var service = CreateService();

        var globalDefinition = service.GetDefinition(mainPath, main, PositionOf(main, "global"));
        var modulesDefinition = service.GetDefinition(mainPath, main, PositionOf(main, "modules"));

        Assert.NotNull(globalDefinition);
        Assert.Equal("aurora-builtin:/global.as", globalDefinition!.Path);
        Assert.NotNull(modulesDefinition);
        Assert.Equal("aurora-builtin:/global.as", modulesDefinition!.Path);
        var document = service.GetBuiltinDocument(modulesDefinition.Path);
        Assert.NotNull(document);
        Assert.Contains("export declare global.modules: Object;", document!.Text, StringComparison.Ordinal);
    }

    [Fact]
    public void ResolvesBuiltinDocumentTypeReferenceToVirtualDocument()
    {
        var service = CreateService();
        var mathDocument = service.GetBuiltinDocument("aurora-builtin:/Math.as");
        Assert.NotNull(mathDocument);
        var position = PositionOf(mathDocument!.Text, "Number");

        var definition = service.GetDefinition(mathDocument.Uri, position);

        Assert.NotNull(definition);
        Assert.Equal("aurora-builtin:/Number.as", definition!.Path);
        var numberDocument = service.GetBuiltinDocument(definition.Path);
        Assert.NotNull(numberDocument);
        Assert.Contains("export declare Number;", numberDocument!.Text, StringComparison.Ordinal);
    }

    [Fact]
    public void ResolvesBuiltinDocumentStringTypeReferenceToVirtualDocument()
    {
        var service = CreateService();
        var jsonDocument = service.GetBuiltinDocument("aurora-builtin:/JSON.as");
        Assert.NotNull(jsonDocument);
        var position = PositionOf(jsonDocument!.Text, "String");

        var definition = service.GetDefinition(jsonDocument.Uri, position);

        Assert.NotNull(definition);
        Assert.Equal("aurora-builtin:/String.as", definition!.Path);
    }

    [Fact]
    public void BuiltinDocumentIncludesPrototypeMembersWithTypedDeclarations()
    {
        var service = CreateService();

        var document = service.GetBuiltinDocument("aurora-builtin:/Array.as");

        Assert.NotNull(document);
        Assert.Contains("export declare Array.prototype.push(...values: Object[]): Number;", document!.Text, StringComparison.Ordinal);
        Assert.Contains("export declare Array.prototype.length: Number;", document.Text, StringComparison.Ordinal);
    }

    [Fact]
    public void ResolvesSyntheticBuiltinTypeReferenceToVirtualDocument()
    {
        var service = CreateService();
        var arrayDocument = service.GetBuiltinDocument("aurora-builtin:/Array.as");
        Assert.NotNull(arrayDocument);
        var position = PositionOf(arrayDocument!.Text, "Function");

        var definition = service.GetDefinition(arrayDocument.Uri, position);

        Assert.NotNull(definition);
        Assert.Equal("aurora-builtin:/Function.as", definition!.Path);
        var functionDocument = service.GetBuiltinDocument(definition.Path);
        Assert.NotNull(functionDocument);
        Assert.Contains("export declare Function;", functionDocument!.Text, StringComparison.Ordinal);
    }

    [Fact]
    public void BuiltinDefinitionDoesNotOverrideLocalShadow()
    {
        var mainPath = Path.Combine(_root, "main.as");
        var main =
            """
            @module(MAIN);
            export func run() {
                const Math = { abs: 1 };
                return Math.abs;
            }
            """;
        var service = CreateService();

        var definition = service.GetDefinition(mainPath, main, PositionOfLast(main, "Math"));

        Assert.NotNull(definition);
        Assert.Equal(Path.GetFullPath(mainPath), definition!.Path);
        Assert.Equal(2, definition.Range.Start.Line);
    }

    [Fact]
    public void LightweightBuiltinFallbackDoesNotOverrideLocalShadow()
    {
        var mainPath = Path.Combine(_root, "main.as");
        var main =
            """
            @module(MAIN);
            export func run() {
                const String = { fromCharCode: 1 };
                return String.fromCharCode(65);
            }
            @
            """;
        var service = CreateService();

        var stringDefinition = service.GetDefinition(mainPath, main, PositionOfLast(main, "String"));
        var memberDefinition = service.GetDefinition(mainPath, main, PositionOf(main, "fromCharCode"));

        Assert.Null(stringDefinition);
        Assert.Null(memberDefinition);
    }

    [Fact]
    public void LightweightBuiltinFallbackDoesNotTreatMemberNameAsGlobal()
    {
        var mainPath = Path.Combine(_root, "main.as");
        var main =
            """
            @module(MAIN);
            export func run() {
                return value.String;
            }
            @
            """;
        var service = CreateService();

        var definition = service.GetDefinition(mainPath, main, PositionOf(main, "String"));

        Assert.Null(definition);
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

    private static string FindRepositoryFile(params string[] segments)
    {
        var directory = AppContext.BaseDirectory;
        for (var i = 0; i < 10; i++)
        {
            var parts = new string[segments.Length + 1];
            parts[0] = directory;
            Array.Copy(segments, 0, parts, 1, segments.Length);
            var candidate = Path.GetFullPath(Path.Combine(parts));
            if (File.Exists(candidate))
            {
                return candidate;
            }

            var parent = Directory.GetParent(directory);
            if (parent == null)
            {
                break;
            }

            directory = parent.FullName;
        }

        throw new FileNotFoundException("Repository file was not found from test output path.", Path.Combine(segments));
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

        public string Root => _baseDirectory;

        public ValueTask<ScriptSourceReference?> ResolveAsync(
            ScriptSourceReference? importer,
            string requestedPath,
            ScriptResolveContext context,
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var currentSourcePath = importer?.FullPath ?? _baseDirectory;
            var fullPath = WithExtension(importer == null
                ? ResolveFromRoot(_baseDirectory, requestedPath)
                : Resolve(currentSourcePath, requestedPath), context.Extension);
            if (!_sources.ContainsKey(fullPath))
            {
                return new ValueTask<ScriptSourceReference?>((ScriptSourceReference?)null);
            }

            return new ValueTask<ScriptSourceReference?>(new ScriptSourceReference(_baseDirectory, fullPath));
        }

        public ValueTask<ScriptSource> GetSourceAsync(
            ScriptSourceReference source,
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (!_sources.TryGetValue(source.FullPath, out var text))
            {
                throw new FileNotFoundException("Script source not found.", source.FullPath);
            }

            return new ValueTask<ScriptSource>(new MemorySource(source.BaseDirectory, source.FullPath, text));
        }

        public async IAsyncEnumerable<ScriptSource> GetAllSourcesAsync(
            ScriptSourceQuery query,
            [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            foreach (var pair in _sources)
            {
                cancellationToken.ThrowIfCancellationRequested();
                await Task.Yield();
                yield return new MemorySource(_baseDirectory, pair.Key, pair.Value);
            }
        }

        private static string Resolve(string currentSourcePath, string requestedPath)
        {
            var slash = currentSourcePath.LastIndexOf('/');
            var currentDirectory = slash >= 0 ? currentSourcePath.Substring(0, slash + 1) : currentSourcePath + "/";
            return new Uri(new Uri(currentDirectory, UriKind.Absolute), requestedPath.Replace('\\', '/')).ToString();
        }

        private static string ResolveFromRoot(string root, string requestedPath)
        {
            root = root.EndsWith("/", StringComparison.Ordinal) ? root : root + "/";
            return new Uri(new Uri(root, UriKind.Absolute), requestedPath.Replace('\\', '/')).ToString();
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
