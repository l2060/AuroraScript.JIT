using AuroraScript.Core;
using AuroraScript.Runtime;
using AuroraScript.Runtime.Types;
using AuroraScript.Source;
using AuroraScript.Tests.Infrastructure;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace AuroraScript.Tests;

public sealed class CompileBlockImportTests
{
    [Theory]
    [InlineData(CompilationMode.Dynamic)]
    [InlineData(CompilationMode.OnlyRun)]
#if NET9_0_OR_GREATER
    [InlineData(CompilationMode.Persistence)]
#endif
    public async Task LoadedExportsKeepNativeDefaultsClosuresAndMutableCalls(CompilationMode mode)
    {
        using var workspace = new TestWorkspace();
        var (engine, domain) = await workspace.CompileModuleAsync("""
            @module(TEST);
            var state = 10;
            export const amount = 2;
            export native func add(Number value = 20, Number increment = 1) Number { return value + increment; }
            export native func touch(Number value) void { state = value; }
            export native func read() Number { return state; }
            func make(Number start) { return (value) => start + value; }
            export const fixed = make(10);
            export var change = (value) => value + 1;
            export func replace() { change = (value) => value + 2; }
            """, mode);
        ScriptAssert.Equal(10, Assert.Single(domain.GetMethod("TEST", "fixed").Upvalues).Value);
        ScriptAssert.Equal(14, TestWorkspace.Execute(domain, "fixed", arguments: [ScriptDatum.FromNumber(4)]));
        var modulesBefore = domain.Global.Modules.OwnProperties.Length;
        var registryBefore = DynamicMethodRegistry.Count;
        using var block = engine.CompileBlock("""
            import m from './main';
            m.touch(12);
            var count = 0;
            return m.add() + m.amount + m.fixed(input) + m.change(input) + m.read() + m.add(1, 2, count++) + count;
            """, new CompileBlockOptions { Domain = domain, Parameters = ["input"] });
        ScriptAssert.Equal(58, block.Invoke(ScriptDatum.FromNumber(4)));
        TestWorkspace.Execute(domain, "replace");
        ScriptAssert.Equal(59, block.Invoke(ScriptDatum.FromNumber(4)));
        Assert.Equal(modulesBefore, domain.Global.Modules.OwnProperties.Length);
        Assert.Equal(registryBefore, DynamicMethodRegistry.Count);
        var native = domain.GetMethod("TEST", "add");
        Assert.NotNull(native.NativeEntry);
        Assert.Equal(typeof(double), native.NativeEntry.ReturnType);
        Assert.Equal(2, native.NativeDefaults.Length);
    }

    [Fact]
    public async Task ImportsUseOnlyResolutionAndDoNotRunInitializersAgain()
    {
        using var workspace = new TestWorkspace();
        workspace.WriteSource("lib.as", "export var value = 1; value++;");
        var resolver = new CountingResolver(ScriptSources.FileSystem(workspace.Root));
        var engine = new AuroraEngine(EngineOptions.Default.WithCompiler(c => c.SourceResolver = resolver));
        await engine.BuildAsync(["lib.as"]);
        using var domain = engine.CreateDomain();
        resolver.Reads = 0;
        resolver.Resolves = 0;
        using var block = engine.CompileBlock("import m from './lib'; return m.value;", new CompileBlockOptions { Domain = domain });
        for (var i = 0; i < 3; i++) ScriptAssert.Equal(2, block.Invoke(Array.Empty<ScriptDatum>()));
        Assert.Equal(1, resolver.Resolves);
        Assert.Equal(0, resolver.Reads);
    }

    [Fact]
    public async Task CapturedImportsAndShadowsUseTheirOwnBindings()
    {
        using var workspace = new TestWorkspace();
        var (engine, domain) = await workspace.CompileModuleAsync("""
            @module(TEST);
            export native func add(Number value) Number { return value + 1; }
            export const amount = 8;
            """);
        using var block = engine.CompileBlock("""
            import m from './main';
            func outer() { return () => m.add(m.amount); }
            func shadow(m) { return m.add(1); }
            return [outer(), shadow({add: (x) => x + 20})];
            """, new CompileBlockOptions { Domain = domain });
        var result = Assert.IsType<ScriptArray>(block.Invoke(Array.Empty<ScriptDatum>()).Object);
        ScriptAssert.Equal(21, result.GetElement(1));
        var callback = Assert.IsType<ClosureFunction>(result.GetElement(0).Object);
        var ctx = new ScriptContext(domain);
        ScriptAssert.Equal(9, callback.Invoke(ctx));
    }

    [Theory]
    [InlineData("import m from './main'; import m from './main'; return 1;")]
    [InlineData("return 1; import m from './main';")]
    [InlineData("func local() { import m from './main'; }")]
    [InlineData("import m from './main'; m = null;")]
    [InlineData("import m from './main'; func local() { m = null; } local();")]
    public async Task RejectsInvalidImports(string source)
    {
        using var workspace = new TestWorkspace();
        var (engine, domain) = await workspace.CompileModuleAsync("@module(TEST); export const value = 1;");
        Assert.Throws<AuroraCompilationException>(() => engine.CompileBlock(source, new CompileBlockOptions { Domain = domain }));
    }

    [Fact]
    public async Task RejectsChangedStaticExportsAndOtherDomains()
    {
        using var workspace = new TestWorkspace();
        var (engine, domain) = await workspace.CompileModuleAsync("@module(TEST); export const value = 1;");
        using var block = engine.CompileBlock("import m from './main'; return m.value;", new CompileBlockOptions { Domain = domain });
        using var other = engine.CreateDomain();
        Assert.Throws<AuroraException>(() => block.Invoke(other));
        Assert.True(domain.Global.TryGetModule("TEST", out var module));
        module.DefineExport("value", ScriptDatum.FromNumber(2), false, false, true);
        Assert.Throws<AuroraException>(() => block.Invoke(Array.Empty<ScriptDatum>()));
    }

    [Fact]
    public async Task DynamicArgumentsKeepNativeTypeChecksAndRestoreContextOnFailure()
    {
        using var workspace = new TestWorkspace();
        var (engine, domain) = await workspace.CompileModuleAsync("""
            @module(TEST);
            export native func length(Array values) Number { return values.length; }
            export native func fail(Number value) Number { throw 'failure'; }
            """);
        using var bad = engine.CompileBlock("import m from './main'; return m.length(input);",
            new CompileBlockOptions { Domain = domain, Parameters = ["input"] });
        Assert.ThrowsAny<AuroraException>(() => bad.Invoke(ScriptDatum.FromNumber(1)));
        using var fail = engine.CompileBlock("import m from './main'; return 1 + m.fail(2);", new CompileBlockOptions { Domain = domain });
        var ctx = new ScriptContext(domain);
        Assert.ThrowsAny<AuroraException>(() => fail.Invoke(ctx, ReadOnlySpan<ScriptDatum>.Empty));
        Assert.Null(ctx.Module);
        Assert.Null(ctx.Target);
    }

    [Fact]
    public async Task ProvenCallsUseNativeEntryAndPropagateItsReturnType()
    {
        using var workspace = new TestWorkspace();
        var (engine, domain) = await workspace.CompileModuleAsync("@module(TEST); var secret = 10;");
        Assert.True(domain.Global.TryGetModule("TEST", out var module));
        var closure = new ClosureFunction(domain, module,
            (ScriptFunctionDelegate)((ctx, args) => throw new InvalidOperationException("datum shell")), [], "probe");
        closure.SetNativeEntry(typeof(CompileBlockImportTests).GetMethod(nameof(NativeProbe))!, [ScriptDatum.FromNumber(2)], true);
        module.DefineExport("probe", ScriptDatum.FromObject(closure), false, true, false);
        using var direct = engine.CompileBlock("import m from './main'; return m.probe(m.probe());",
            new CompileBlockOptions { Domain = domain });
        ScriptAssert.Equal(22, direct.Invoke(Array.Empty<ScriptDatum>()));
        using var dynamic = engine.CompileBlock("import m from './main'; return m.probe(input);",
            new CompileBlockOptions { Domain = domain, Parameters = ["input"] });
        Assert.Throws<InvalidOperationException>(() => dynamic.Invoke(ScriptDatum.FromNumber(1)));
    }

    public static double NativeProbe(ScriptContext context, double value) =>
        context.Module.GetPropertyDatum(context, "secret").Number + value;

    [Fact]
    public async Task SystemPackagesAndRelativeUserModulesUseTheNormalResolver()
    {
        using var workspace = new TestWorkspace();
        workspace.WriteSource("fs.as", "export const value = 3;");
        var path = workspace.WriteSource("text.txt", "contents");
        var engine = new AuroraEngine(EngineOptions.Default
            .WithCompiler(c => c.SourceResolver = ScriptSources.FileSystem(workspace.Root))
            .WithPackages(p => p.Add(NativePackages.FileSystem)));
        await engine.BuildAsync(["fs.as"]);
        using var domain = engine.CreateDomain();
        using var block = engine.CompileBlock("""
            import fs from 'fs';
            import user from './fs';
            return fs.readText(String(path)) + user.value;
            """, new CompileBlockOptions { Domain = domain, Parameters = ["path"] });
        ScriptAssert.Equal("contents3", block.Invoke(ScriptDatum.FromString(path)));
    }

    [Fact]
    public async Task WideArgumentsAndSpreadConstCallsPreservePositions()
    {
        using var workspace = new TestWorkspace();
        var (engine, domain) = await workspace.CompileModuleAsync("""
            @module(TEST);
            export const sum = (a, b, c, d, e, f, g, h) => a + h;
            """);
        using var block = engine.CompileBlock("import m from './main'; return m.sum(...[a, b, c, d, e, f, g, h]);",
            new CompileBlockOptions { Domain = domain, Parameters = ["a", "b", "c", "d", "e", "f", "g", "h"] });
        ScriptAssert.Equal(9, block.Invoke(domain, ScriptDatum.FromNumber(1), ScriptDatum.FromNumber(2),
            ScriptDatum.FromNumber(3), ScriptDatum.FromNumber(4), ScriptDatum.FromNumber(5),
            ScriptDatum.FromNumber(6), ScriptDatum.FromNumber(7), ScriptDatum.FromNumber(8)));
    }

    [Fact]
    public async Task UnloadedDependenciesAndDuplicateParametersFailDuringCompilation()
    {
        using var workspace = new TestWorkspace();
        var (engine, domain) = await workspace.CompileModuleAsync("@module(TEST); export const value = 1;");
        workspace.WriteSource("unloaded.as", "export const value = 2;");
        Assert.Throws<AuroraCompilationException>(() => engine.CompileBlock("import m from './unloaded'; return m.value;",
            new CompileBlockOptions { Domain = domain }));
        Assert.Throws<AuroraCompilationException>(() => engine.CompileBlock("import m from './main'; return m.value;",
            new CompileBlockOptions { Domain = domain, Parameters = ["m"] }));
    }

    private sealed class CountingResolver(IScriptSourceResolver inner) : IScriptSourceResolver
    {
        internal int Reads;
        internal int Resolves;
        public string Root => inner.Root;
        public async ValueTask<ScriptSourceReference?> ResolveAsync(ScriptSourceReference? importer, string path,
            ScriptResolveContext context, CancellationToken cancellationToken = default)
        {
            Resolves++;
            await Task.Yield();
            return await inner.ResolveAsync(importer, path, context, cancellationToken);
        }
        public ValueTask<ScriptSource> GetSourceAsync(ScriptSourceReference reference, CancellationToken cancellationToken = default)
        {
            Reads++;
            return inner.GetSourceAsync(reference, cancellationToken);
        }
        public IAsyncEnumerable<ScriptSource> GetAllSourcesAsync(ScriptSourceQuery query, CancellationToken cancellationToken = default) =>
            inner.GetAllSourcesAsync(query, cancellationToken);
    }
}
