using AuroraScript.Tests.Infrastructure;
using System.IO;
using System.Threading.Tasks;
using Xunit;

namespace AuroraScript.Tests;

public sealed class CompilationModeTests
{
    [Theory]
    [InlineData(CompilationMode.Dynamic)]
    [InlineData(CompilationMode.OnlyRun)]
    [InlineData(CompilationMode.Persistence)]
    public async Task ReleaseCompilationModesProduceSameResult(CompilationMode mode)
    {
        using var workspace = new TestWorkspace();
        var (engine, domain) = await workspace.CompileModuleAsync(
            "@module(TEST); export func run(value) { return value * 2 + 2; }",
            mode);

        ScriptAssert.Equal(42, TestWorkspace.Execute(
            domain,
            "run",
            arguments: AuroraScript.Runtime.ScriptDatum.FromNumber(20)));

        if (mode == CompilationMode.Persistence)
        {
            var assemblyPath = Path.Combine(workspace.Root, "test-output.dll");
            Assert.True(File.Exists(assemblyPath));
            Assert.True(new FileInfo(assemblyPath).Length > 0);
            ScriptAssert.Equal(42, TestWorkspace.Execute(engine.CreateDomain(), "run", arguments: AuroraScript.Runtime.ScriptDatum.FromNumber(20)));
        }
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task ReleaseBuildWorksWithHotReloadDisabledOrEnabled(bool enableHotReload)
    {
        using var workspace = new TestWorkspace();
        var (_, domain) = await workspace.CompileModuleAsync(
            "@module(TEST); func local(value) { return value + 1; } export func run() { return local(41); }",
            enableHotReload: enableHotReload);

        ScriptAssert.Equal(42, TestWorkspace.Execute(domain, "run"));
    }
}
