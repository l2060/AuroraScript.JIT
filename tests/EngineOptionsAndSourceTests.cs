using AuroraScript.Core;
using AuroraScript.Runtime.Serialization;
using AuroraScript.Tests.Infrastructure;
using System;
using System.IO;
using System.Linq;
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
            .WithCompiler(compiler => compiler.WithDirectory(workspace.Root))
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
        Assert.Equal(Path.GetFullPath(workspace.Root), configured.Compiler.BaseDirectory);
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

    [Fact]
    public void LegacyFluentOptionsRemainCompatible()
    {
        using var workspace = new TestWorkspace();
        using var stdOut = new StringWriter();
        using var errorOut = new StringWriter();
        var original = EngineOptions.Default;

#pragma warning disable CS0618
        var configured = original
            .WithBaseDirectory(workspace.Root)
            .WithCompilationMode(CompilationMode.Dynamic)
            .WithOptimizeOption(OptimizeOptions.Debug)
            .WithEnableHotReload(false)
            .WithEnableAutoModuleDirectCall(true)
            .WithEnableModuleConstInlining(true)
            .WithEnableConfused(true)
            .WithJsonSerializer(ScriptJsonSerializer.Default)
            .WithDateTimeFormat("O")
            .WithConsoleStdOut(stdOut)
            .WithConsoleErrorOut(errorOut)
            .WithAssemblyOut("legacy.dll")
            .WithExtName("aurora")
            .WithStringPooling(StringPoolingStrategy.None)
            .WithMaxDegreeOfParallelism(4);

        Assert.NotSame(original, configured);
        Assert.Equal(Path.GetFullPath(workspace.Root), configured.BaseDirectory);
        Assert.Equal(Path.GetFullPath(workspace.Root), configured.Compiler.BaseDirectory);
        Assert.Equal(CompilationMode.Dynamic, configured.CompilationMode);
        Assert.Equal(CompilationMode.Dynamic, configured.Compiler.Mode);
        Assert.Equal(OptimizeOptions.Debug, configured.OptimizeOption);
        Assert.Equal(OptimizeOptions.Debug, configured.Optimization.Level);
        Assert.False(configured.EnableHotReload);
        Assert.False(configured.Runtime.EnableHotReload);
        Assert.True(configured.EnableAutoModuleDirectCall);
        Assert.True(configured.Optimization.EnableAutoModuleDirectCall);
        Assert.True(configured.EnableModuleConstInlining);
        Assert.True(configured.Optimization.EnableModuleConstInlining);
        Assert.True(configured.EnableConfused);
        Assert.True(configured.Output.EnableConfused);
        Assert.Same(ScriptJsonSerializer.Default, configured.JsonSerializer);
        Assert.Same(ScriptJsonSerializer.Default, configured.Runtime.JsonSerializer);
        Assert.Equal("O", configured.DateTimeFormat);
        Assert.Equal("O", configured.Runtime.DateTimeFormat);
        Assert.Same(stdOut, configured.ConsoleStdOut);
        Assert.Same(stdOut, configured.Runtime.ConsoleStdOut);
        Assert.Same(errorOut, configured.ConsoleErrorOut);
        Assert.Same(errorOut, configured.Runtime.ConsoleErrorOut);
        Assert.Equal("legacy.dll", configured.AssemblyOut);
        Assert.Equal("legacy.dll", configured.Output.AssemblyFile);
        Assert.Equal(".aurora", configured.ExtName);
        Assert.Equal(".aurora", configured.Compiler.ExtName);
        Assert.Equal(StringPoolingStrategy.None, configured.StringPooling);
        Assert.Equal(StringPoolingStrategy.None, configured.Runtime.StringPooling);
        Assert.Equal(4, configured.MaxDegreeOfParallelism);
        Assert.Equal(4, configured.Compiler.MaxDegreeOfParallelism);
        Assert.False(original.Optimization.EnableModuleConstInlining);
#pragma warning restore CS0618
    }

    [Fact]
    public void LegacyInitPropertiesUpdateGroupedOptions()
    {
        using var workspace = new TestWorkspace();
        using var stdOut = new StringWriter();
        using var errorOut = new StringWriter();

#pragma warning disable CS0618
        var configured = EngineOptions.Default with
        {
            BaseDirectory = workspace.Root,
            CompilationMode = CompilationMode.Dynamic,
            OptimizeOption = OptimizeOptions.Debug,
            EnableHotReload = false,
            EnableAutoModuleDirectCall = true,
            EnableModuleConstInlining = true,
            EnableConfused = true,
            JsonSerializer = ScriptJsonSerializer.Default,
            DateTimeFormat = "O",
            ConsoleStdOut = stdOut,
            ConsoleErrorOut = errorOut,
            AssemblyOut = "legacy-init.dll",
            ExtName = "legacy",
            StringPooling = StringPoolingStrategy.None,
            MaxDegreeOfParallelism = 2
        };

        configured.ExtName = "changed";

        Assert.Equal(Path.GetFullPath(workspace.Root), configured.Compiler.BaseDirectory);
        Assert.Equal(CompilationMode.Dynamic, configured.Compiler.Mode);
        Assert.Equal(OptimizeOptions.Debug, configured.Optimization.Level);
        Assert.False(configured.Runtime.EnableHotReload);
        Assert.True(configured.Optimization.EnableAutoModuleDirectCall);
        Assert.True(configured.Optimization.EnableModuleConstInlining);
        Assert.True(configured.Output.EnableConfused);
        Assert.Same(ScriptJsonSerializer.Default, configured.Runtime.JsonSerializer);
        Assert.Equal("O", configured.Runtime.DateTimeFormat);
        Assert.Same(stdOut, configured.Runtime.ConsoleStdOut);
        Assert.Same(errorOut, configured.Runtime.ConsoleErrorOut);
        Assert.Equal("legacy-init.dll", configured.Output.AssemblyFile);
        Assert.Equal(".changed", configured.Compiler.ExtName);
        Assert.Equal(".changed", configured.ExtName);
        Assert.Equal(StringPoolingStrategy.None, configured.Runtime.StringPooling);
        Assert.Equal(2, configured.Compiler.MaxDegreeOfParallelism);
#pragma warning restore CS0618
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
    public void MemoryAndFileSourcesResolveRelativeToBaseDirectory()
    {
        using var workspace = new TestWorkspace();
        var engine = workspace.CreateEngine();

        ScriptSource memory = engine.MemorySource("nested/memory.as", "@module(MEMORY);");
        ScriptSource file = engine.FileSource("nested/file.as", Encoding.UTF8);

        Assert.Equal(Path.Combine(workspace.Root, "nested", "memory.as"), memory.FullPath);
        Assert.Equal(Path.Combine(workspace.Root, "nested", "file.as"), file.FullPath);
    }

    [Fact]
    public void SearchAllFileSourceUsesConfiguredExtensionRecursively()
    {
        using var workspace = new TestWorkspace();
        workspace.WriteSource("a.as", "@module(A);");
        workspace.WriteSource("nested/b.as", "@module(B);");
        workspace.WriteSource("ignored.txt", "ignored");
        var engine = workspace.CreateEngine();

        var sources = engine.SearchAllFileSource(Encoding.UTF8);

        Assert.Equal(2, sources.Length);
        Assert.All(sources, source => Assert.EndsWith(".as", source.FullPath, StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void SearchAllFileSourceRejectsMissingBaseDirectory()
    {
        var missing = Path.Combine(Path.GetTempPath(), "aurora-missing-" + Guid.NewGuid().ToString("N"));
        var engine = new AuroraEngine(EngineOptions.Default.WithCompiler(compiler => compiler.WithDirectory(missing)));

        Assert.Throws<AuroraException>(() => engine.SearchAllFileSource(Encoding.UTF8));
    }

    [Fact]
    public async Task BuildRejectsMissingBaseDirectoryAndMissingRootFile()
    {
        var missing = Path.Combine(Path.GetTempPath(), "aurora-missing-" + Guid.NewGuid().ToString("N"));
        var engine = new AuroraEngine(EngineOptions.Default.WithCompiler(compiler => compiler.WithDirectory(missing)));

        await Assert.ThrowsAsync<AuroraException>(() => engine.BuildAsync(engine.MemorySource("main.as", "@module(TEST);")));

        Directory.CreateDirectory(missing);
        try
        {
            var error = await Assert.ThrowsAsync<AuroraCompilationException>(() => engine.BuildAsync(engine.FileSource("missing.as", Encoding.UTF8)));
            var diagnostic = Assert.Single(error.Diagnostics);
            Assert.Contains("missing.as", diagnostic.Message, StringComparison.OrdinalIgnoreCase);
        }
        finally
        {
            Directory.Delete(missing);
        }
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
