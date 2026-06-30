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
            workspace.MemorySource("patch.as", "@module(TEST); export func version() { return 2; }"),
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
            workspace.MemorySource(
                "patch.as",
                "@module(TEST); export func version() { return 2; } export func added() { return 40; }"),
            HotPatchType.Incremental);

        ScriptAssert.Equal(2, TestWorkspace.Execute(domain, "version"));
        ScriptAssert.Equal(40, TestWorkspace.Execute(domain, "added"));
    }

    [Fact]
    public async Task IncrementalPatchCanUseModulePathAndSourceText()
    {
        using var workspace = new TestWorkspace();
        var (engine, domain) = await workspace.CompileModuleAsync(
            "@module(TEST); export func version() { return 1; }",
            enableHotReload: true);

        domain.IncrementalPatch(
            System.IO.Path.Combine(workspace.Root, "patch.as"),
            "@module(TEST); export func version() { return 2; }");

        ScriptAssert.Equal(2, TestWorkspace.Execute(domain, "version"));
    }

    [Fact]
    public async Task PatchRejectsGlobalDeclarationFile()
    {
        using var workspace = new TestWorkspace();
        var (engine, domain) = await workspace.CompileModuleAsync(
            "@module(TEST); export func version() { return 1; }",
            enableHotReload: true);

        var error = Assert.Throws<AuroraCompilationException>(() => domain.DynamicPatch(
            workspace.MemorySource("globals.as", "@global();\ndeclare const HOST_CONST;"),
            HotPatchType.Incremental));

        Assert.Contains("@global() declaration files cannot be compiled as modules", error.Message, StringComparison.OrdinalIgnoreCase);
        ScriptAssert.Equal(1, TestWorkspace.Execute(domain, "version"));
    }

    [Fact]
    public async Task PatchModuleCanUseProjectGlobalDeclarations()
    {
        using var workspace = new TestWorkspace();
        workspace.WriteSource("globals.as", "@global();\ndeclare func HOST_ADD(left, right);");
        var (engine, domain) = await workspace.CompileModuleAsync(
            "@module(TEST); export func value() { return 1; }",
            configureGlobal: global => global.Define("HOST_ADD", (Func<int, int, int>)((left, right) => left + right)),
            enableHotReload: true);

        domain.DynamicPatch(
            workspace.MemorySource(
                "patch.as",
                "@module(TEST); export func value() { return HOST_ADD(20, 22); }"),
            HotPatchType.Incremental);

        ScriptAssert.Equal(42, TestWorkspace.Execute(domain, "value"));
    }

    [Fact]
    public async Task StringPatchRejectsRelativeModulePath()
    {
        using var workspace = new TestWorkspace();
        var (engine, domain) = await workspace.CompileModuleAsync(
            "@module(TEST); export func version() { return 1; }",
            enableHotReload: true);

        var error = Assert.Throws<ArgumentException>(() => domain.IncrementalPatch(
            "patch.as",
            "@module(TEST); export func version() { return 2; }"));

        Assert.Contains("absolute", error.Message, StringComparison.OrdinalIgnoreCase);
        ScriptAssert.Equal(1, TestWorkspace.Execute(domain, "version"));
    }

    [Fact]
    public async Task IncrementalPatchCanCreateNewModuleWithNonExportedFunctionWhenConstInliningIsEnabled()
    {
        using var workspace = new TestWorkspace();
        var engine = workspace.CreateEngine(enableHotReload: true, enableModuleConstInlining: true);
        var dependency = workspace.WriteSource("l123.as", "@module(l123); export const value = 1;");
        await engine.BuildAsync(dependency);
        var domain = engine.CreateDomain();

        domain.DynamicPatch(
            workspace.MemorySource(
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
            workspace.MemorySource("replace.as", "@module(TEST); export func keep() { return 3; }"),
            HotPatchType.Replace);

        Assert.Null(domain.GetMethod("TEST", "old"));
        ScriptAssert.Equal(3, TestWorkspace.Execute(domain, "keep"));
    }

    [Fact]
    public async Task StringPatchFullPathUsesResolverRootForDependencyImports()
    {
        using var workspace = new TestWorkspace();
        var engine = workspace.CreateEngine(enableHotReload: true);
        workspace.WriteSource("l123.as", "@module(l123); export const value = 1;");
        await engine.BuildAsync("l123.as");
        var domain = engine.CreateDomain();

        domain.IncrementalPatch(
            System.IO.Path.Combine(workspace.Root, "test.as"),
            "@module(test); import l123 from 'l123'; export func hello() { return l123.value; }",
            ignoreDepends: true);

        ScriptAssert.Equal(1, domain.Execute("test", "hello"));
    }

    [Fact]
    public async Task ScriptHotPatchSingleArgumentUsesCurrentModuleFullPath()
    {
        using var workspace = new TestWorkspace();
        var (engine, domain) = await workspace.CompileModuleAsync(
            """
            @module(TEST);
            export func version() { return 1; }
            export func patchSelf() {
                var patch =
                |> @module(TEST);
                |> export func version() { return 2; }
                    ;
                HotPatch.incremental(patch);
            }
            """,
            enableHotReload: true);

        TestWorkspace.Execute(domain, "patchSelf");

        ScriptAssert.Equal(2, TestWorkspace.Execute(domain, "version"));
    }

    [Fact]
    public async Task ScriptHotPatchRelativePathResolvesFromCurrentModuleFullPath()
    {
        using var workspace = new TestWorkspace();
        var (engine, domain) = await workspace.CompileModuleAsync(
            """
            @module(TEST);
            export func version() { return 1; }
            export func patchSelf() {
                var patch =
                |> @module(TEST);
                |> export func version() { return 3; }
                    ;
                HotPatch.incremental('./main', patch);
            }
            """,
            enableHotReload: true);

        TestWorkspace.Execute(domain, "patchSelf");

        ScriptAssert.Equal(3, TestWorkspace.Execute(domain, "version"));
    }

    [Fact]
    public async Task ReplacePatchCanUseModulePathAndSourceText()
    {
        using var workspace = new TestWorkspace();
        var (engine, domain) = await workspace.CompileModuleAsync(
            "@module(TEST); export func old() { return 1; } export func keep() { return 2; }",
            enableHotReload: true);

        domain.ReplacePatch(
            System.IO.Path.Combine(workspace.Root, "replace.as"),
            "@module(TEST); export func keep() { return 3; }");

        Assert.Null(domain.GetMethod("TEST", "old"));
        ScriptAssert.Equal(3, TestWorkspace.Execute(domain, "keep"));
    }

    [Fact]
    public async Task PatchAffectsOnlyTargetDomain()
    {
        using var workspace = new TestWorkspace();
        var engine = workspace.CreateEngine(enableHotReload: true);
        await engine.BuildAsync(workspace.MemorySource(
            "main.as",
            "@module(TEST); export func version() { return 1; }"));
        var first = engine.CreateDomain();
        var second = engine.CreateDomain();

        first.DynamicPatch(
            workspace.MemorySource("patch.as", "@module(TEST); export func version() { return 2; }"),
            HotPatchType.Incremental);

        ScriptAssert.Equal(2, TestWorkspace.Execute(first, "version"));
        ScriptAssert.Equal(1, TestWorkspace.Execute(second, "version"));
    }
}
