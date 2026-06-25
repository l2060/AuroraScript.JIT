using AuroraScript.Core;
using AuroraScript.Tests.Infrastructure;
using System;
using System.Threading.Tasks;
using Xunit;

namespace AuroraScript.Tests;

public sealed class HotReloadTests
{
    [Fact]
    public async Task DisabledHotReloadRejectsDynamicPatch()
    {
        using var workspace = new TestWorkspace();
        var (engine, domain) = await workspace.CompileModuleAsync(
            "@module(TEST); export func version() { return 1; }",
            enableHotReload: false);

        var error = Assert.Throws<AuroraException>(() => domain.DynamicPatch(
            engine.MemorySource("patch.as", "@module(TEST); export func version() { return 2; }"),
            HotPatchType.Incremental));

        Assert.Contains("disabled", error.Message, StringComparison.OrdinalIgnoreCase);
        ScriptAssert.Equal(1, TestWorkspace.Execute(domain, "version"));
    }

    [Fact]
    public async Task IncrementalPatchUpdatesExistingFunctionAndAddsNewFunction()
    {
        using var workspace = new TestWorkspace();
        var (engine, domain) = await workspace.CompileModuleAsync(
            "@module(TEST); export func version() { return 1; }",
            enableHotReload: true);

        domain.DynamicPatch(
            engine.MemorySource(
                "patch.as",
                "@module(TEST); export func version() { return 2; } export func added() { return 40; }"),
            HotPatchType.Incremental);

        ScriptAssert.Equal(2, TestWorkspace.Execute(domain, "version"));
        ScriptAssert.Equal(40, TestWorkspace.Execute(domain, "added"));
    }

    [Fact]
    public async Task IncrementalPatchCanCreateNewModuleWithNonExportedFunctionWhenConstInliningIsEnabled()
    {
        using var workspace = new TestWorkspace();
        var engine = workspace.CreateEngine(enableHotReload: true, enableModuleConstInlining: true);
        var dependency = workspace.WriteSource("l123.as", "@module(l123); export const value = 1;");
        await engine.BuildAsync(engine.FileSource(dependency, System.Text.Encoding.UTF8));
        var domain = engine.CreateDomain();

        domain.DynamicPatch(
            engine.MemorySource(
                "test.as",
                "@module(test); import l123 from 'l123'; func hello() { return 'v1'; } var x = 10;"),
            HotPatchType.Incremental | HotPatchType.IgnoreDepends);

        ScriptAssert.Equal("v1", domain.Execute("test", "hello"));
        ScriptAssert.Equal(10, domain.GetModule("test").GetPropertyDatum(null, "x"));
    }

    [Fact]
    public async Task ReplacePatchRemovesMembersMissingFromReplacement()
    {
        using var workspace = new TestWorkspace();
        var (engine, domain) = await workspace.CompileModuleAsync(
            "@module(TEST); export func old() { return 1; } export func keep() { return 2; }",
            enableHotReload: true);

        domain.DynamicPatch(
            engine.MemorySource("replace.as", "@module(TEST); export func keep() { return 3; }"),
            HotPatchType.Replace);

        Assert.Null(domain.GetMethod("TEST", "old"));
        ScriptAssert.Equal(3, TestWorkspace.Execute(domain, "keep"));
    }

    [Fact]
    public async Task PatchAffectsOnlyTargetDomain()
    {
        using var workspace = new TestWorkspace();
        var engine = workspace.CreateEngine(enableHotReload: true);
        await engine.BuildAsync(engine.MemorySource(
            "main.as",
            "@module(TEST); export func version() { return 1; }"));
        var first = engine.CreateDomain();
        var second = engine.CreateDomain();

        first.DynamicPatch(
            engine.MemorySource("patch.as", "@module(TEST); export func version() { return 2; }"),
            HotPatchType.Incremental);

        ScriptAssert.Equal(2, TestWorkspace.Execute(first, "version"));
        ScriptAssert.Equal(1, TestWorkspace.Execute(second, "version"));
    }
}
