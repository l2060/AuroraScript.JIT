using AuroraScript.Compiler;
using AuroraScript.Compiler.Backend.Builders;
using AuroraScript.Runtime;
using AuroraScript.Source;
using AuroraScript.Tests.Infrastructure;
using System.IO;
using System.Threading.Tasks;
using Xunit;

namespace AuroraScript.Tests;

public sealed class CompilerPerformanceRegressionTests
{
    [Fact]
    public async Task IncrementalParsingReadsSourceOncePerBuild()
    {
        using var workspace = new TestWorkspace();
        var source = new CountingSource(workspace.Root);
        var engine = workspace.CreateEngine(enableHotReload: true);
        var compiler = new IncrementalCompiler(engine.CreateEmptyDomain(null!),
            engine.Options, new DynamicBuilder(engine.Options));
        Assert.Single(await compiler.BuildSyntaxTreeAsync(source));
        Assert.Equal(1, source.ReadCount);
        Assert.Single(await compiler.BuildSyntaxTreeAsync(source));
        Assert.Equal(2, source.ReadCount);
    }

    [Fact]
    public async Task ExplicitSourceIsReadOncePerBuildWithoutCachingAcrossBuilds()
    {
        using var workspace = new TestWorkspace();
        var source = new CountingSource(workspace.Root);
        var engine = workspace.CreateEngine();
        await engine.BuildAsync(source);
        Assert.Equal(1, source.ReadCount);
        ScriptAssert.Equal(1, TestWorkspace.Execute(engine.CreateDomain(), "run"));

        source.Code = "@module(TEST); export func run() { return 2; }";
        await engine.BuildAsync(source);
        Assert.Equal(2, source.ReadCount);
        ScriptAssert.Equal(2, TestWorkspace.Execute(engine.CreateDomain(), "run"));
    }

    [Theory]
    [InlineData(CompilationMode.Dynamic)]
    [InlineData(CompilationMode.OnlyRun)]
#if NET9_0_OR_GREATER
    [InlineData(CompilationMode.Persistence)]
#endif
    public async Task NestedBranchSnapshotsRemainIndependent(CompilationMode mode)
    {
        using var workspace = new TestWorkspace();
        var (_, domain) = await workspace.CompileModuleAsync("""
            @module(TEST);
            type Point { Number x; }
            func choose(Point a, Point b, Boolean outer, Boolean inner) {
                var value = a;
                if (outer) {
                    if (inner) { value = b; }
                    else { value = { x: 3 }; }
                } else {
                    try {
                        if (inner) { throw 'switch'; }
                        value = { x: 4 };
                    } catch (error) { value = b; }
                    finally { var untouched = a.x; }
                }
                return value.x;
            }
            export func run() {
                var a = { x: 1 };
                var b = { x: 2 };
                return [choose(a, b, true, true), choose(a, b, true, false),
                    choose(a, b, false, true), choose(a, b, false, false)];
            }
            """, mode);
        ScriptAssert.Equal(new object[] { 2, 3, 2, 4 },
            TestWorkspace.Execute(domain, "run"));
    }

    private sealed class CountingSource : ScriptSource
    {
        public CountingSource(string root)
        {
            BaseDirectory = root;
            FullPath = Path.Combine(root, "main.as");
        }

        public string BaseDirectory { get; }
        public string FullPath { get; }
        public string SourcePath => "main.as";
        public string Code { get; set; } = "@module(TEST); export func run() { return 1; }";
        public int ReadCount { get; private set; }

        public string ReadSource()
        {
            ReadCount++;
            return Code;
        }
    }
}
