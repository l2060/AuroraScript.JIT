using AuroraScript.Core;
using AuroraScript.Runtime;
using AuroraScript.Runtime.Types;
using AuroraScript.Source;
using AuroraScript.Tests.Infrastructure;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace AuroraScript.Tests;

public sealed class ModuleCompilationTests
{
    [Fact]
    public async Task ResolvesImportIncludeAndRelativePaths()
    {
        using var workspace = new TestWorkspace();
        workspace.WriteSource("lib/value.as", "@module(VALUE); export const number = 40;");
        workspace.WriteSource("shared.as", "export const INCLUDED = 2;");
        var main = workspace.WriteSource(
            "main.as",
            """
            @module(TEST);
            import value from './lib/value';
            include './shared';
            export func run() { return value.number + INCLUDED; }
            """);
        var engine = workspace.CreateEngine(maxDegreeOfParallelism: 4);

        await engine.BuildAsync(main);
        var domain = engine.CreateDomain();

        ScriptAssert.Equal(42, TestWorkspace.Execute(domain, "run"));
    }

    [Fact]
    public async Task ResolvesImportedSourceOutsideRootAndIncludesFromImportedDirectory()
    {
        using var workspace = new TestWorkspace();
        var testsRoot = Path.Combine(workspace.Root, "tests");
        workspace.WriteSource("tests/unit.as", """
            @module(UNIT);
            import debug_test from '../temp/debug_test';
            export func run() { return debug_test.main(); }
            """);
        workspace.WriteSource("temp/debug_test.as", """
            @module(DEBUG_TEST);
            include 'debug_inc';
            export func main() { return includedValue(); }
            """);
        workspace.WriteSource("temp/debug_inc.as", """
            export func includedValue() { return 42; }
            """);
        var options = EngineOptions.Default
            .WithCompiler(compiler => compiler.SourceResolver = ScriptSources.FileSystem(testsRoot, Encoding.UTF8))
            .WithCompiler(compiler => compiler.Mode = CompilationMode.Dynamic)
            .WithOptimization(optimization => optimization.Level = OptimizeOptions.Release);
        var engine = new AuroraEngine(options);

        await engine.BuildAsync("unit");
        var domain = engine.CreateDomain();

        ScriptAssert.Equal(42, domain.Execute("UNIT", "run"));
        ScriptAssert.Equal(42, domain.Execute("DEBUG_TEST", "main"));
        var debugModule = Assert.IsType<ScriptModule>(domain.GetModule("DEBUG_TEST"));
        Assert.EndsWith("../temp/debug_test.as", debugModule.Source.ModulePath, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task PathBaseModuleResolvesFromCurrentModuleDirectory()
    {
        using var workspace = new TestWorkspace();
        var main = workspace.WriteSource(
            "app/main.as",
            """
            @module(TEST);
            export func run() {
                return [
                    Path.baseModule('../assets', './config'),
                    Path.baseModule(),
                    Path.join(Path.currentDirectory(), './local')
                ];
            }
            """);
        var engine = workspace.CreateEngine();

        await engine.BuildAsync(main);
        var domain = engine.CreateDomain();

        var mainDirectory = ScriptPath.GetDirectoryName(ScriptPath.GetFullPath(workspace.Root, "app/main.as"));
        ScriptAssert.Equal(
            new object?[]
            {
                ScriptPath.GetFullPath(mainDirectory, "../assets/config"),
                mainDirectory,
                ScriptPath.GetFullPath(mainDirectory, "local")
            },
            TestWorkspace.Execute(domain, "run"));
    }

    [Fact]
    public async Task ResolvesDependenciesThroughCustomSourceResolver()
    {
        const string root = "memory://aurora-tests";
        var files = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["memory://aurora-tests/lib/value.as"] = "@module(VALUE); export const number = 40;",
            ["memory://aurora-tests/shared.as"] = "export const INCLUDED = 2;"
        };
        var options = EngineOptions.Default
            .WithCompiler(compiler => compiler.SourceResolver = AuroraScript.Core.ScriptSources.FileSystem(root))
            .WithCompiler(compiler => compiler.Mode = CompilationMode.Dynamic)
            .WithCompiler(compiler => compiler.SourceResolver = new InMemoryResolver(root, files));
        var engine = new AuroraEngine(options);
        var main = new MemorySource(
            root,
            "memory://aurora-tests/main.as",
            """
            @module(TEST);
            import value from './lib/value';
            include './shared';
            export func run() { return [value.number + INCLUDED, Path.baseModule('assets', 'config')]; }
            """);

        await engine.BuildAsync(main);

        ScriptAssert.Equal(
            new object?[] { 42, "memory://aurora-tests/assets/config" },
            TestWorkspace.Execute(engine.CreateDomain(), "run"));
    }

    [Fact]
    public async Task GlobalDeclarationFilesArePreloadedFromCustomSourceResolver()
    {
        const string root = "memory://aurora-global-tests";
        var files = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["memory://aurora-global-tests/globals.as"] = "@global();\ndeclare const HOST_CONST;",
            ["memory://aurora-global-tests/main.as"] = "@module(TEST); export func run() { return HOST_CONST; }"
        };
        var options = EngineOptions.Default
            .WithCompiler(compiler => compiler.SourceResolver = new InMemoryResolver(root, files))
            .WithCompiler(compiler => compiler.Mode = CompilationMode.Dynamic);
        var engine = new AuroraEngine(options);

        await engine.BuildAsync("main.as");

        var domain = engine.CreateDomain(global => global.Define("HOST_CONST", 42));
        ScriptAssert.Equal(42, TestWorkspace.Execute(domain, "run"));
    }

    [Fact]
    public async Task ReportsDuplicateGlobalDeclarationsFromCustomSourceResolver()
    {
        const string root = "memory://aurora-global-duplicate-tests";
        var files = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["memory://aurora-global-duplicate-tests/main.as"] = "@module(TEST); export func run() { return VERSION; }",
            ["memory://aurora-global-duplicate-tests/a.as"] = "@global();\ndeclare const VERSION;",
            ["memory://aurora-global-duplicate-tests/b.as"] = "@global();\ndeclare var VERSION;"
        };
        var options = EngineOptions.Default
            .WithCompiler(compiler => compiler.SourceResolver = new InMemoryResolver(root, files))
            .WithCompiler(compiler => compiler.Mode = CompilationMode.Dynamic);
        var engine = new AuroraEngine(options);

        var error = await Assert.ThrowsAsync<AuroraCompilationException>(() => engine.BuildAsync("main.as"));

        Assert.Contains("Duplicate global declaration 'VERSION'", error.ToString(), StringComparison.Ordinal);
    }

    [Fact]
    public async Task ExplicitMemoryGlobalDeclarationSourceIsAvailableToModules()
    {
        using var workspace = new TestWorkspace();
        var globalSource = workspace.MemorySource("globals.as", "@global();\ndeclare const HOST_CONST;");
        var mainSource = workspace.MemorySource("main.as", "@module(TEST); export func run() { return HOST_CONST; }");
        var engine = workspace.CreateEngine();

        await engine.BuildAsync(globalSource, mainSource);

        var domain = engine.CreateDomain(global => global.Define("HOST_CONST", 42));
        ScriptAssert.Equal(42, TestWorkspace.Execute(domain, "run"));
    }

    [Fact]
    public async Task IncludeOnlyExposesExportedDeclarations()
    {
        using var workspace = new TestWorkspace();
        workspace.WriteSource("shared.as", "const HIDDEN = 40; export const INCLUDED = 2;");
        var main = workspace.WriteSource(
            "main.as",
            """
            @module(TEST);
            include './shared';
            export func visible() { return INCLUDED; }
            export func hidden() { return HIDDEN; }
            """);
        var engine = workspace.CreateEngine();

        await engine.BuildAsync(main);
        var domain = engine.CreateDomain();

        ScriptAssert.Equal(2, TestWorkspace.Execute(domain, "visible"));
        ScriptAssert.Equal(null, TestWorkspace.Execute(domain, "hidden"));
        var keys = domain.GetModule("TEST").EnumerationKeys();
        Assert.Contains("INCLUDED", keys);
        Assert.DoesNotContain("HIDDEN", keys);
    }

    [Fact]
    public async Task IncludeConflictRejectsDuplicateModuleDeclaration()
    {
        using var workspace = new TestWorkspace();
        workspace.WriteSource("shared.as", "export const VALUE = 1;");
        var main = workspace.WriteSource(
            "main.as",
            """
            @module(TEST);
            include './shared';
            export const VALUE = 2;
            export func run() { return VALUE; }
            """);
        var engine = workspace.CreateEngine();

        var error = await Assert.ThrowsAsync<AuroraCompilationException>(() => engine.BuildAsync(main));

        var diagnostic = Assert.Single(error.Diagnostics);
        Assert.Contains("Duplicate declaration 'VALUE'", diagnostic.Message);
    }

    [Fact]
    public async Task DeduplicatesDiamondDependenciesAndDuplicateRoots()
    {
        using var workspace = new TestWorkspace();
        workspace.WriteSource("base.as", "@module(BASE); export const value = 20;");
        workspace.WriteSource("left.as", "@module(LEFT); import b from 'base'; export const value = b.value + 1;");
        workspace.WriteSource("right.as", "@module(RIGHT); import b from 'base'; export const value = b.value + 1;");
        workspace.WriteSource(
            "main.as",
            "@module(TEST); import l from 'left'; import r from 'right'; export func run() { return l.value + r.value; }");
        var engine = workspace.CreateEngine(maxDegreeOfParallelism: 8);

        await engine.BuildAsync(["main.as", "main.as"]);

        ScriptAssert.Equal(42, TestWorkspace.Execute(engine.CreateDomain(), "run"));
    }

    [Fact]
    public async Task CompilesWideDependencyGraphWithParallelWorkers()
    {
        using var workspace = new TestWorkspace();
        const int dependencyCount = 24;
        var imports = new StringBuilder("@module(TEST);\n");
        var sum = new StringBuilder("export func run() { return ");
        for (var i = 0; i < dependencyCount; i++)
        {
            workspace.WriteSource($"deps/d{i}.as", $"@module(D{i}); export const value = {i};");
            imports.Append("import d").Append(i).Append(" from './deps/d").Append(i).Append("';\n");
            if (i > 0) sum.Append(" + ");
            sum.Append('d').Append(i).Append(".value");
        }
        sum.Append("; }");
        var main = workspace.WriteSource("main.as", imports.Append(sum).ToString());
        var engine = workspace.CreateEngine(maxDegreeOfParallelism: 8);

        await engine.BuildAsync(main);

        ScriptAssert.Equal(276, TestWorkspace.Execute(engine.CreateDomain(), "run"));
    }

    [Fact]
    public async Task RejectsCircularModuleDependency()
    {
        using var workspace = new TestWorkspace();
        var first = workspace.WriteSource("first.as", "@module(FIRST); import second from 'second';");
        workspace.WriteSource("second.as", "@module(SECOND); import first from 'first';");
        var engine = workspace.CreateEngine();

        var error = await Assert.ThrowsAsync<AuroraCompilationException>(
            () => engine.BuildAsync(first));

        Assert.Contains("Circular", error.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task RejectsDuplicateModuleNamesWithBothPathsInDiagnostic()
    {
        using var workspace = new TestWorkspace();
        workspace.WriteSource("first.as", "@module(CONFLICT);");
        workspace.WriteSource("second.as", "@module(CONFLICT);");
        var engine = workspace.CreateEngine();

        var error = await Assert.ThrowsAsync<AuroraCompilationException>(() => engine.BuildAsync(["first.as", "second.as"]));

        Assert.Contains("first.as", error.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("second.as", error.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task ImportsAnonymousModulesWithMatchingFileNamesByPath()
    {
        using var workspace = new TestWorkspace();
        var firstPath = workspace.WriteSource("first/shared.as", "export const value = 20;");
        var secondPath = workspace.WriteSource("second/shared.as", "export const value = 22;");
        var main = workspace.WriteSource(
            "main.as",
            "@module(MAIN); import first from './first/shared'; import second from './second/shared'; export func run() { return first.value + second.value; }");
        var engine = workspace.CreateEngine();

        await engine.BuildAsync(main);
        var domain = engine.CreateDomain();

        ScriptAssert.Equal(42, domain.Execute("MAIN", "run"));
        Assert.Same(ScriptObject.Null, domain.GetModule("first/shared"));
        Assert.Same(ScriptObject.Null, domain.GetModule("second/shared"));
        var moduleKeys = domain.Global.Modules.EnumerationKeys();
        Assert.Contains(moduleKeys, key => ScriptPath.Comparer.Equals(key, ScriptPath.NormalizeFullPath(firstPath)));
        Assert.Contains(moduleKeys, key => ScriptPath.Comparer.Equals(key, ScriptPath.NormalizeFullPath(secondPath)));
        Assert.DoesNotContain("MAIN", moduleKeys);
    }

    [Fact]
    public async Task GlobalGetModuleFindsExplicitNameWithoutAddingNameRegistryKey()
    {
        using var workspace = new TestWorkspace();
        workspace.WriteSource("timer.as", "@module(TIMER_LIB); export const value = 42;");
        workspace.WriteSource("anonymous.as", "export const value = 1;");
        var main = workspace.WriteSource(
            "main.as",
            """
            @module(MAIN);
            import timerByPath from './timer';
            import anonymousByPath from './anonymous';
            export func run() {
                return [
                    global.getModule("TIMER_LIB").value,
                    global.getModule("MAIN") != null,
                    global.getModule("anonymous") == null,
                    global.getModule("MISSING") == null,
                    global.modules["TIMER_LIB"] == null
                ];
            }
            """);
        var engine = workspace.CreateEngine();

        await engine.BuildAsync(main);
        var domain = engine.CreateDomain();

        ScriptAssert.Equal(
            new object?[] { 42, true, true, true, true },
            domain.Execute("MAIN", "run"));
        var moduleKeys = domain.Global.Modules.EnumerationKeys();
        Assert.DoesNotContain("MAIN", moduleKeys);
        Assert.DoesNotContain("TIMER_LIB", moduleKeys);
    }

    [Fact]
    public async Task ExplicitNameMayMatchAnAnonymousModuleFileName()
    {
        using var workspace = new TestWorkspace();
        workspace.WriteSource("shared.as", "export const value = 20;");
        workspace.WriteSource("named.as", "@module(shared); export const value = 22;");
        var main = workspace.WriteSource(
            "main.as",
            "@module(MAIN); import anonymous from './shared'; import named from './named'; export func run() { return anonymous.value + named.value; }");
        var engine = workspace.CreateEngine();

        await engine.BuildAsync(main);
        var domain = engine.CreateDomain();

        ScriptAssert.Equal(42, domain.Execute("MAIN", "run"));
        Assert.IsType<ScriptModule>(domain.GetModule("shared"));
    }

    [Fact]
    public async Task RejectsDuplicateDeclarationsInSameScope()
    {
        using var workspace = new TestWorkspace();
        var moduleEngine = workspace.CreateEngine();

        var moduleDuplicate = workspace.MemorySource(
            "duplicate-module.as",
            """
            @module(TEST);
            export func testTextTemplate() { return 1; }
            export func testTextTemplate(n) { return n; }
        """);
        var moduleError = await Assert.ThrowsAsync<AuroraCompilationException>(() => moduleEngine.BuildAsync(moduleDuplicate));
        var moduleDiagnostic = Assert.Single(moduleError.Diagnostics);
        Assert.Contains("Duplicate declaration 'testTextTemplate'", moduleDiagnostic.Message);
        Assert.Contains("Duplicate declaration 'testTextTemplate'", moduleError.ToString());

        var localEngine = workspace.CreateEngine();
        var localDuplicate = workspace.MemorySource(
            "duplicate-local.as",
            """
            @module(TEST2);
            export func run(n) {
                const n = { a: 1, b: 2 };
                return n;
            }
        """);
        var localError = await Assert.ThrowsAsync<AuroraCompilationException>(() => localEngine.BuildAsync(localDuplicate));
        var localDiagnostic = Assert.Single(localError.Diagnostics);
        Assert.Contains("Duplicate declaration 'n'", localDiagnostic.Message);
        Assert.Contains("Previous declaration:", localDiagnostic.Message);
        Assert.Contains("line:2", localDiagnostic.Message);
        Assert.DoesNotContain("line:-1", localDiagnostic.Message);
        Assert.Contains("Duplicate declaration 'n'", localError.ToString());
    }

    [Fact]
    public async Task ParallelBackendFailuresAreReportedAsCompileReport()
    {
        using var workspace = new TestWorkspace();
        workspace.WriteSource("valid.as", "@module(VALID); export func ok() { return 1; }");
        workspace.WriteSource(
            "invalid.as",
            """
            @module(INVALID);
            export func run(n) {
                const n = 1;
                return n;
            }
            """);
        var engine = workspace.CreateEngine(maxDegreeOfParallelism: 8);

        var error = await Assert.ThrowsAsync<AuroraCompilationException>(() => engine.BuildAsync(["valid.as", "invalid.as"]));

        var diagnostic = Assert.Single(error.Diagnostics);
        Assert.Contains("Duplicate declaration 'n'", error.ToString());
    }

    [Fact]
    public async Task ReportsEveryIndependentCompilationFailureInStablePathOrder()
    {
        using var workspace = new TestWorkspace();
        var engine = workspace.CreateEngine(maxDegreeOfParallelism: 8);
        ScriptSource z = workspace.MemorySource("z-error.as", "@module(Z); var z = ;");
        ScriptSource a = workspace.MemorySource("a-error.as", "@module(A); var a = ;");

        var report = await Assert.ThrowsAsync<AuroraCompilationException>(() => engine.BuildAsync(z, a));

        Assert.Equal(2, report.Diagnostics.Count);
        Assert.EndsWith("a-error.as", report.Diagnostics[0].FileName, StringComparison.OrdinalIgnoreCase);
        Assert.EndsWith("z-error.as", report.Diagnostics[1].FileName, StringComparison.OrdinalIgnoreCase);
        Assert.All(report.Diagnostics, diagnostic => Assert.Contains("requires an expression", diagnostic.Message, StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task MissingImportedFileIsReportedWithoutHangingWorkers()
    {
        using var workspace = new TestWorkspace();
        var main = workspace.WriteSource("main.as", "@module(TEST); import missing from 'missing';");
        var engine = workspace.CreateEngine(maxDegreeOfParallelism: 8);

        var build = engine.BuildAsync(main);
        var completed = await Task.WhenAny(build, Task.Delay(TimeSpan.FromSeconds(10)));

        Assert.Same(build, completed);
        var report = await Assert.ThrowsAsync<AuroraCompilationException>(() => build);
        Assert.Single(report.Diagnostics);
    }

    [Fact]
    public async Task HonorsPreCanceledBuild()
    {
        using var workspace = new TestWorkspace();
        var engine = workspace.CreateEngine();
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => engine.BuildAsync(
            cancellation.Token,
            workspace.MemorySource("main.as", "@module(TEST);")));
    }

    [Fact]
    public async Task SerializesConcurrentBuildsOnOneEngine()
    {
        using var workspace = new TestWorkspace();
        var engine = workspace.CreateEngine(maxDegreeOfParallelism: 4);
        var source = workspace.MemorySource("main.as", "@module(TEST); export func run() { return 42; }");

        await Task.WhenAll(Enumerable.Range(0, 8).Select(_ => engine.BuildAsync(source)));

        ScriptAssert.Equal(42, TestWorkspace.Execute(engine.CreateDomain(), "run"));
    }

    [Fact]
    public async Task FailedRebuildPreservesLastSuccessfulEntryPoint()
    {
        using var workspace = new TestWorkspace();
        var engine = workspace.CreateEngine();
        await engine.BuildAsync(workspace.MemorySource("valid.as", "@module(TEST); export func run() { return 42; }"));
        await Assert.ThrowsAsync<AuroraCompilationException>(() => engine.BuildAsync(
            workspace.MemorySource("invalid.as", "@module(BROKEN); var value = ;")));

        ScriptAssert.Equal(42, TestWorkspace.Execute(engine.CreateDomain(), "run"));
    }

    [Fact]
    public async Task GlobalDeclarationFilesAreSkippedWhenBuildingAllSources()
    {
        using var workspace = new TestWorkspace();
        workspace.WriteSource(
            "globals.as",
            """
            @global();
            declare const HOST_CONST;
            """);
        workspace.WriteSource("main.as", "@module(TEST); export func run() { return HOST_CONST; }");
        var engine = workspace.CreateEngine();

        await engine.BuildAsync();

        var domain = engine.CreateDomain(global => global.Define("HOST_CONST", 42));
        ScriptAssert.Equal(42, TestWorkspace.Execute(domain, "run"));
        Assert.False(domain.Global.TryGetModule("globals", out _));
    }

    [Fact]
    public async Task RejectsImportOrIncludeOfGlobalDeclarationFile()
    {
        using var workspace = new TestWorkspace();
        workspace.WriteSource("globals.as", "@global();\ndeclare const HOST_CONST;");
        workspace.WriteSource("importer.as", "@module(IMPORTER); import globals from 'globals';");
        workspace.WriteSource("includer.as", "@module(INCLUDER); include 'globals';");
        var engine = workspace.CreateEngine();

        var importError = await Assert.ThrowsAsync<AuroraCompilationException>(() => engine.BuildAsync("importer.as"));
        var includeError = await Assert.ThrowsAsync<AuroraCompilationException>(() => engine.BuildAsync("includer.as"));

        Assert.Contains("cannot be imported", importError.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("cannot be included", includeError.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task ReportsDuplicateGlobalDeclarationsAcrossProject()
    {
        using var workspace = new TestWorkspace();
        workspace.WriteSource("main.as", "@module(TEST); export func run() { return VERSION; }");
        workspace.WriteSource("a.as", "@global();\ndeclare const VERSION;");
        workspace.WriteSource("b.as", "@global();\ndeclare func VERSION();");
        var engine = workspace.CreateEngine();

        var error = await Assert.ThrowsAsync<AuroraCompilationException>(() => engine.BuildAsync("main.as"));

        Assert.Contains("Duplicate global declaration 'VERSION'", error.ToString(), StringComparison.Ordinal);
    }

    private sealed class InMemoryResolver : IScriptSourceResolver
    {
        private readonly string _baseDirectory;
        private readonly IReadOnlyDictionary<string, string> _sources;

        public InMemoryResolver(string baseDirectory, IReadOnlyDictionary<string, string> sources)
        {
            _baseDirectory = ScriptPath.NormalizeBaseDirectory(baseDirectory);
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

            var currentSourcePath = ResolveCurrentPath(importer);
            var currentDirectory = importer == null ? _baseDirectory : ScriptPath.GetDirectoryName(currentSourcePath);
            var fullPath = ScriptPath.EnsureExtension(ScriptPath.Combine(currentDirectory, requestedPath), context.Extension);
            if (!ScriptPath.IsWithinNormalizedRoot(_baseDirectory, fullPath))
            {
                return new ValueTask<ScriptSourceReference?>((ScriptSourceReference?)null);
            }

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
            if (!ScriptPath.IsWithinNormalizedRoot(_baseDirectory, source.FullPath))
            {
                throw new FileNotFoundException("Script source not found.", source.FullPath);
            }

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

        private string ResolveCurrentPath(ScriptSourceReference? importer)
        {
            if (importer == null)
            {
                return _baseDirectory;
            }

            return importer.Value.FullPath;
        }
    }
}
