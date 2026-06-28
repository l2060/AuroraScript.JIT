using AuroraScript.Core;
using AuroraScript.Source;
using AuroraScript.Tests.Infrastructure;
using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace AuroraScript.Tests;

public sealed class EngineOptionsAndSourceTests
{
    [Fact]
    public void RejectsNullOptions()
    {
        Assert.Throws<AuroraException>(() => new AuroraEngine(null!));
    }

    [Fact]
    public void FluentOptionsReturnNewConfiguredInstances()
    {
        using var workspace = new TestWorkspace();
        var original = EngineOptions.Default;
        var configured = original
            .WithCompiler(compiler => compiler.SourceResolver = AuroraScript.Core.ScriptSources.FileSystem(workspace.Root))
            .WithCompiler(compiler => compiler.Mode = CompilationMode.Dynamic)
            .WithOptimization(optimization => optimization.Level = OptimizeOptions.Release)
            .WithRuntime(runtime => runtime.HotReload = false)
            .WithOptimization(optimization => optimization.AutoModuleDirectCall = true)
            .WithOptimization(optimization => optimization.ModuleConstInlining = true)
            .WithOutput(output => output.Confused = true)
            .WithRuntime(runtime => runtime.DateTimeFormat = "O")
            .WithCompiler(compiler => compiler.ExtName = "aurora")
            .WithRuntime(runtime => runtime.StringPooling = StringPoolingStrategy.None)
            .WithCompiler(compiler => compiler.MaxDegreeOfParallelism = 3);

        Assert.NotSame(original, configured);
        Assert.Equal(ScriptPath.NormalizeBaseDirectory(workspace.Root), configured.Compiler.SourceResolver.Root);
        Assert.Equal(CompilationMode.Dynamic, configured.Compiler.Mode);
        Assert.Equal(OptimizeOptions.Release, configured.Optimization.Level);
        Assert.False(configured.Runtime.EnableHotReload);
        Assert.True(configured.Optimization.EnableAutoModuleDirectCall);
        Assert.False(original.Optimization.EnableAutoModuleDirectCall);
        Assert.True(configured.Optimization.EnableModuleConstInlining);
        Assert.False(original.Optimization.EnableModuleConstInlining);
        Assert.True(configured.Output.EnableConfused);
        Assert.Equal("O", configured.Runtime.DateTimeFormat);
        Assert.Equal(".aurora", configured.Compiler.ExtName);
        Assert.Equal(StringPoolingStrategy.None, configured.Runtime.StringPooling);
        Assert.Equal(3, configured.Compiler.MaxDegreeOfParallelism);
    }


    [Theory]
    [InlineData(".a.b")]
    [InlineData("a.b.c")]
    public void RejectsInvalidExtensions(string extension)
    {
        Assert.Throws<ArgumentException>(() => EngineOptions.Default.WithCompiler(compiler => compiler.ExtName = extension));
    }

    [Fact]
    public void RejectsNegativeParallelismAndNullSerializer()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => EngineOptions.Default.WithCompiler(compiler => compiler.MaxDegreeOfParallelism = -1));
        Assert.Throws<AuroraException>(() => EngineOptions.Default.WithRuntime(runtime => runtime.JsonSerializer = null!));
    }

    [Fact]
    public void ScriptSourcesResolveRelativeToTheirOwnRoot()
    {
        using var workspace = new TestWorkspace();

        ScriptSource memory = workspace.MemorySource("nested/memory.as", "@module(MEMORY);");
        var file = new MemorySource(workspace.Root, "nested/file.as", "@module(FILE);");

        Assert.Equal(ScriptPath.GetFullPath(workspace.Root, "nested/memory.as"), memory.FullPath);
        Assert.Equal(ScriptPath.GetFullPath(workspace.Root, "nested/file.as"), file.FullPath);
    }


    [Fact]
    public async Task BuildAllowsMemorySourceAndRejectsMissingRootEntry()
    {
        using var workspace = new TestWorkspace();
        var missing = Path.Combine(Path.GetTempPath(), "aurora-missing-" + Guid.NewGuid().ToString("N"));
        var engine = new AuroraEngine(EngineOptions.Default.WithCompiler(compiler =>
            compiler.SourceResolver = ScriptSources.FileSystem(missing, Encoding.UTF8)));

        await engine.BuildAsync(workspace.MemorySource("main.as", "@module(TEST); export func run() { return 42; }"));
        ScriptAssert.Equal(42, TestWorkspace.Execute(engine.CreateDomain(), "run"));

        var error = await Assert.ThrowsAsync<FileNotFoundException>(() => engine.BuildAsync("missing.as"));
        Assert.Contains("missing.as", error.FileName, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task CompositeSourceResolverDoesNotOverrideFileSystemWhenRootsDiffer()
    {
        using var workspace = new TestWorkspace();
        workspace.WriteSource("main.as", """
            @module(TEST);
            import config from './config';
            export func run() { return config.value; }
            """);
        workspace.WriteSource("config.as", "@module(CONFIG); export const value = 1;");

        var memory = ScriptSources.Memory("mem://override/")
            .Add("config.as", "@module(CONFIG); export const value = 42;");
        var resolver = ScriptSources.Composite(
            memory,
            ScriptSources.FileSystem(workspace.Root, Encoding.UTF8));
        var engine = new AuroraEngine(EngineOptions.Default
            .WithCompiler(compiler => compiler.SourceResolver = AuroraScript.Core.ScriptSources.FileSystem(workspace.Root))
            .WithCompiler(compiler => compiler.Mode = CompilationMode.Dynamic)
            .WithCompiler(compiler => compiler.SourceResolver = resolver));

        await engine.BuildAsync("main.as");

        ScriptAssert.Equal(1, TestWorkspace.Execute(engine.CreateDomain(), "run"));
    }

    [Fact]
    public async Task CompositeSourceResolverAllowsMemorySourcesToOverrideFileSystemWhenRootsMatch()
    {
        using var workspace = new TestWorkspace();
        workspace.WriteSource("main.as", """
            @module(TEST);
            import config from './config';
            export func run() { return config.value; }
            """);
        workspace.WriteSource("config.as", "@module(CONFIG); export const value = 1;");

        var memory = ScriptSources.Memory(workspace.Root)
            .Add("config.as", "@module(CONFIG); export const value = 42;");
        var resolver = ScriptSources.Composite(
            memory,
            ScriptSources.FileSystem(workspace.Root, Encoding.UTF8));
        var engine = new AuroraEngine(EngineOptions.Default
            .WithCompiler(compiler => compiler.SourceResolver = AuroraScript.Core.ScriptSources.FileSystem(workspace.Root))
            .WithCompiler(compiler => compiler.Mode = CompilationMode.Dynamic)
            .WithCompiler(compiler => compiler.SourceResolver = resolver));

        await engine.BuildAsync("main.as");

        ScriptAssert.Equal(42, TestWorkspace.Execute(engine.CreateDomain(), "run"));
    }

    [Fact]
    public async Task CompositeSourceResolverAllowsMemoryParentRootToOverrideRelativeFileSystemImport()
    {
        using var workspace = new TestWorkspace();
        var fileSystemRoot = Path.Combine(workspace.Root, "d");
        Directory.CreateDirectory(fileSystemRoot);
        workspace.WriteSource("d/main.as", """
            @module(TEST);
            import value from '../test';
            export func run() { return value.number; }
            """);
        workspace.WriteSource("test.as", "@module(VALUE); export const number = 1;");

        var memory = ScriptSources.Memory(workspace.Root)
            .Add("test.as", "@module(VALUE); export const number = 42;");
        var resolver = ScriptSources.Composite(
            memory,
            ScriptSources.FileSystem(fileSystemRoot, Encoding.UTF8));
        var engine = new AuroraEngine(EngineOptions.Default
            .WithCompiler(compiler => compiler.Mode = CompilationMode.Dynamic)
            .WithCompiler(compiler => compiler.SourceResolver = resolver));

        await engine.BuildAsync("main.as");

        ScriptAssert.Equal(42, TestWorkspace.Execute(engine.CreateDomain(), "run"));
    }

    [Fact]
    public async Task CompositeSourceResolverBuildAllSourcesDeduplicatesOnlyWithinSameRoot()
    {
        using var workspace = new TestWorkspace();
        workspace.WriteSource("main.as", "@module(FILE_MAIN); export func run() { return 1; }");
        workspace.WriteSource("config.as", "@module(FILE_CONFIG); export const value = 1;");

        var memory = ScriptSources.Memory("mem://override/")
            .Add("config.as", "@module(MEM_CONFIG); export const value = 42;");
        var resolver = ScriptSources.Composite(
            memory,
            ScriptSources.FileSystem(workspace.Root, Encoding.UTF8));
        var engine = new AuroraEngine(EngineOptions.Default
            .WithCompiler(compiler => compiler.SourceResolver = AuroraScript.Core.ScriptSources.FileSystem(workspace.Root))
            .WithCompiler(compiler => compiler.Mode = CompilationMode.Dynamic)
            .WithCompiler(compiler => compiler.SourceResolver = resolver));

        await engine.BuildAsync();

        var domain = engine.CreateDomain();
        Assert.NotSame(AuroraScript.Runtime.Types.ScriptObject.Null, domain.GetModule("FILE_MAIN"));
        Assert.NotSame(AuroraScript.Runtime.Types.ScriptObject.Null, domain.GetModule("FILE_CONFIG"));
        Assert.NotSame(AuroraScript.Runtime.Types.ScriptObject.Null, domain.GetModule("MEM_CONFIG"));
    }

    [Fact]
    public async Task CompositeSourceResolverBuildAllSourcesDeduplicatesMatchingRootsByModulePath()
    {
        using var workspace = new TestWorkspace();
        workspace.WriteSource("main.as", """
            @module(TEST);
            import config from './config';
            export func run() { return config.value; }
            """);
        workspace.WriteSource("config.as", "@module(CONFIG); export const value = 1;");

        var memory = ScriptSources.Memory(workspace.Root)
            .Add("config.as", "@module(CONFIG); export const value = 42;");
        var resolver = ScriptSources.Composite(
            memory,
            ScriptSources.FileSystem(workspace.Root, Encoding.UTF8));
        var engine = new AuroraEngine(EngineOptions.Default
            .WithCompiler(compiler => compiler.SourceResolver = AuroraScript.Core.ScriptSources.FileSystem(workspace.Root))
            .WithCompiler(compiler => compiler.Mode = CompilationMode.Dynamic)
            .WithCompiler(compiler => compiler.SourceResolver = resolver));

        await engine.BuildAsync();

        ScriptAssert.Equal(42, TestWorkspace.Execute(engine.CreateDomain(), "run"));
    }

    [Fact]
    public async Task BuildRejectsNullSourceArrayAndNullElements()
    {
        using var workspace = new TestWorkspace();
        var engine = workspace.CreateEngine();

        await Assert.ThrowsAsync<ArgumentNullException>(() => engine.BuildAsync(default(ScriptSource[])!));
        await Assert.ThrowsAsync<ArgumentNullException>(() => engine.BuildAsync([null!]));
    }
}
