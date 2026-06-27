using AuroraScript.Core;
using AuroraScript.Runtime;
using AuroraScript.Tests.Infrastructure;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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

        await engine.BuildAsync(engine.FileSource(main, Encoding.UTF8));
        var domain = engine.CreateDomain();

        ScriptAssert.Equal(42, TestWorkspace.Execute(domain, "run"));
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
            .WithCompiler(compiler => compiler.Directory = root)
            .WithCompiler(compiler => compiler.Mode = CompilationMode.Dynamic)
            .WithCompiler(compiler => compiler.SourceResolver = new InMemoryResolver(root, files));
        var engine = new AuroraEngine(options);
        var main = new MemoryScriptSource(
            root,
            "memory://aurora-tests/main.as",
            """
            @module(TEST);
            import value from './lib/value';
            include './shared';
            export func run() { return value.number + INCLUDED; }
            """);

        await engine.BuildAsync(main);

        ScriptAssert.Equal(42, TestWorkspace.Execute(engine.CreateDomain(), "run"));
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

        await engine.BuildAsync(engine.FileSource(main, Encoding.UTF8));
        var domain = engine.CreateDomain();

        ScriptAssert.Equal(2, TestWorkspace.Execute(domain, "visible"));
        ScriptAssert.Equal(null, TestWorkspace.Execute(domain, "hidden"));
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

        var error = await Assert.ThrowsAsync<AuroraCompilationException>(() => engine.BuildAsync(engine.FileSource(main, Encoding.UTF8)));

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
        var main = workspace.WriteSource(
            "main.as",
            "@module(TEST); import l from 'left'; import r from 'right'; export func run() { return l.value + r.value; }");
        var engine = workspace.CreateEngine(maxDegreeOfParallelism: 8);
        var source = engine.FileSource(main, Encoding.UTF8);

        await engine.BuildAsync(source, source);

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

        await engine.BuildAsync(engine.FileSource(main, Encoding.UTF8));

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
            () => engine.BuildAsync(engine.FileSource(first, Encoding.UTF8)));

        Assert.Contains("Circular", error.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task RejectsDuplicateModuleNamesWithBothPathsInDiagnostic()
    {
        using var workspace = new TestWorkspace();
        var first = workspace.WriteSource("first.as", "@module(CONFLICT);");
        var second = workspace.WriteSource("second.as", "@module(CONFLICT);");
        var engine = workspace.CreateEngine();

        var error = await Assert.ThrowsAsync<AuroraCompilationException>(() => engine.BuildAsync(
            engine.FileSource(first, Encoding.UTF8),
            engine.FileSource(second, Encoding.UTF8)));

        Assert.Contains("first.as", error.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("second.as", error.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task RejectsDuplicateDeclarationsInSameScope()
    {
        using var workspace = new TestWorkspace();
        var moduleEngine = workspace.CreateEngine();

        var moduleDuplicate = moduleEngine.MemorySource(
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
        var localDuplicate = localEngine.MemorySource(
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
        var valid = workspace.WriteSource("valid.as", "@module(VALID); export func ok() { return 1; }");
        var invalid = workspace.WriteSource(
            "invalid.as",
            """
            @module(INVALID);
            export func run(n) {
                const n = 1;
                return n;
            }
            """);
        var engine = workspace.CreateEngine(maxDegreeOfParallelism: 8);

        var error = await Assert.ThrowsAsync<AuroraCompilationException>(() => engine.BuildAsync(
            engine.FileSource(valid, Encoding.UTF8),
            engine.FileSource(invalid, Encoding.UTF8)));

        var diagnostic = Assert.Single(error.Diagnostics);
        Assert.Contains("Duplicate declaration 'n'", error.ToString());
    }

    [Fact]
    public async Task ReportsEveryIndependentCompilationFailureInStablePathOrder()
    {
        using var workspace = new TestWorkspace();
        var engine = workspace.CreateEngine(maxDegreeOfParallelism: 8);
        ScriptSource z = engine.MemorySource("z-error.as", "@module(Z); var z = ;");
        ScriptSource a = engine.MemorySource("a-error.as", "@module(A); var a = ;");

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

        var build = engine.BuildAsync(engine.FileSource(main, Encoding.UTF8));
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
            engine.MemorySource("main.as", "@module(TEST);")));
    }

    [Fact]
    public async Task SerializesConcurrentBuildsOnOneEngine()
    {
        using var workspace = new TestWorkspace();
        var engine = workspace.CreateEngine(maxDegreeOfParallelism: 4);
        var source = engine.MemorySource("main.as", "@module(TEST); export func run() { return 42; }");

        await Task.WhenAll(Enumerable.Range(0, 8).Select(_ => engine.BuildAsync(source)));

        ScriptAssert.Equal(42, TestWorkspace.Execute(engine.CreateDomain(), "run"));
    }

    [Fact]
    public async Task FailedRebuildPreservesLastSuccessfulEntryPoint()
    {
        using var workspace = new TestWorkspace();
        var engine = workspace.CreateEngine();
        await engine.BuildAsync(engine.MemorySource("valid.as", "@module(TEST); export func run() { return 42; }"));
        await Assert.ThrowsAsync<AuroraCompilationException>(() => engine.BuildAsync(
            engine.MemorySource("invalid.as", "@module(BROKEN); var value = ;")));

        ScriptAssert.Equal(42, TestWorkspace.Execute(engine.CreateDomain(), "run"));
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
