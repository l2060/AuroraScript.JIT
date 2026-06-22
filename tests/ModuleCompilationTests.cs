using AuroraScript.Core;
using AuroraScript.Runtime;
using AuroraScript.Tests.Infrastructure;
using System;
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
    public async Task IncludeConflictKeepsCurrentModuleDeclaration()
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

        await engine.BuildAsync(engine.FileSource(main, Encoding.UTF8));

        ScriptAssert.Equal(2, TestWorkspace.Execute(engine.CreateDomain(), "run"));
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

        var error = await Assert.ThrowsAsync<AuroraException>(
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

        var error = await Assert.ThrowsAsync<AuroraException>(() => engine.BuildAsync(
            engine.FileSource(first, Encoding.UTF8),
            engine.FileSource(second, Encoding.UTF8)));

        Assert.Contains("first.as", error.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("second.as", error.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task ReportsEveryIndependentCompilationFailureInStablePathOrder()
    {
        using var workspace = new TestWorkspace();
        var engine = workspace.CreateEngine(maxDegreeOfParallelism: 8);
        ScriptSource z = engine.MemorySource("z-error.as", "@module(Z); var z = ;");
        ScriptSource a = engine.MemorySource("a-error.as", "@module(A); var a = ;");

        var report = await Assert.ThrowsAsync<AuroraCompileReportException>(() => engine.BuildAsync(z, a));

        Assert.Equal(2, report.Errors.Count);
        var errors = report.Errors.Cast<AuroraCompileException>().ToArray();
        Assert.EndsWith("a-error.as", errors[0].ModulePath, StringComparison.OrdinalIgnoreCase);
        Assert.EndsWith("z-error.as", errors[1].ModulePath, StringComparison.OrdinalIgnoreCase);
        Assert.All(errors, error => Assert.IsType<AuroraParseException>(error.InnerException));
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
        var report = await Assert.ThrowsAsync<AuroraCompileReportException>(() => build);
        Assert.Single(report.Errors);
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
        await Assert.ThrowsAsync<AuroraCompileReportException>(() => engine.BuildAsync(
            engine.MemorySource("invalid.as", "@module(BROKEN); var value = ;")));

        ScriptAssert.Equal(42, TestWorkspace.Execute(engine.CreateDomain(), "run"));
    }
}
