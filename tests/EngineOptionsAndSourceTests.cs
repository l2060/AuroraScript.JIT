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
            .WithBaseDirectory(workspace.Root)
            .WithCompilationMode(CompilationMode.Dynamic)
            .WithOptimizeOption(OptimizeOptions.Release)
            .WithEnableHotReload(false)
            .WithEnableAutoModuleDirectCall(true)
            .WithEnableConfused(true)
            .WithDateTimeFormat("O")
            .WithExtName("aurora")
            .WithStringPooling(StringPoolingStrategy.None)
            .WithMaxDegreeOfParallelism(3);

        Assert.NotSame(original, configured);
        Assert.Equal(Path.GetFullPath(workspace.Root), configured.BaseDirectory);
        Assert.Equal(CompilationMode.Dynamic, configured.CompilationMode);
        Assert.Equal(OptimizeOptions.Release, configured.OptimizeOption);
        Assert.False(configured.EnableHotReload);
        Assert.True(configured.EnableAutoModuleDirectCall);
        Assert.False(original.EnableAutoModuleDirectCall);
        Assert.True(configured.EnableConfused);
        Assert.Equal("O", configured.DateTimeFormat);
        Assert.Equal(".aurora", configured.ExtName);
        Assert.Equal(StringPoolingStrategy.None, configured.StringPooling);
        Assert.Equal(3, configured.MaxDegreeOfParallelism);
    }

    [Theory]
    [InlineData(".a.b")]
    [InlineData("a.b.c")]
    public void RejectsInvalidExtensions(string extension)
    {
        Assert.Throws<ArgumentException>(() => EngineOptions.Default.WithExtName(extension));
    }

    [Fact]
    public void RejectsNegativeParallelismAndNullSerializer()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => EngineOptions.Default.WithMaxDegreeOfParallelism(-1));
        Assert.Throws<AuroraException>(() => EngineOptions.Default.WithJsonSerializer(null!));
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
        var engine = new AuroraEngine(EngineOptions.Default.WithBaseDirectory(missing));

        Assert.Throws<AuroraException>(() => engine.SearchAllFileSource(Encoding.UTF8));
    }

    [Fact]
    public async Task BuildRejectsMissingBaseDirectoryAndMissingRootFile()
    {
        var missing = Path.Combine(Path.GetTempPath(), "aurora-missing-" + Guid.NewGuid().ToString("N"));
        var engine = new AuroraEngine(EngineOptions.Default.WithBaseDirectory(missing));

        await Assert.ThrowsAsync<AuroraException>(() => engine.BuildAsync(engine.MemorySource("main.as", "@module(TEST);")));

        Directory.CreateDirectory(missing);
        try
        {
            await Assert.ThrowsAsync<AuroraException>(() => engine.BuildAsync(engine.FileSource("missing.as", Encoding.UTF8)));
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
