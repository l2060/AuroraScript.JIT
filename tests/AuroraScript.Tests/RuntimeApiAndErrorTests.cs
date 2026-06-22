using AuroraScript.Runtime;
using AuroraScript.Runtime.Types;
using AuroraScript.Tests.Infrastructure;
using System;
using System.Threading.Tasks;
using Xunit;

namespace AuroraScript.Tests;

public sealed class RuntimeApiAndErrorTests
{
    [Fact]
    public void CreateDomainBeforeBuildFailsClearly()
    {
        using var workspace = new TestWorkspace();
        var engine = workspace.CreateEngine();

        var error = Assert.Throws<AuroraException>(() => engine.CreateDomain());
        Assert.Contains("has not been built", error.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task ExecuteReportsMissingModuleMethodAndNonFunction()
    {
        using var workspace = new TestWorkspace();
        var (_, domain) = await workspace.CompileModuleAsync(
            "@module(TEST); export var value = 1; export func run() { return 1; }");

        Assert.Contains("module", Assert.Throws<AuroraException>(() => domain.Execute("MISSING", "run")).Message, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("does not exist", Assert.Throws<AuroraException>(() => domain.Execute("TEST", "missing")).Message, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("not a valid script method", Assert.Throws<AuroraException>(() => domain.Execute("TEST", "value")).Message, StringComparison.OrdinalIgnoreCase);
        Assert.Null(domain.GetMethod("MISSING", "run"));
        Assert.Null(domain.GetMethod("TEST", "missing"));
        Assert.Same(ScriptObject.Null, domain.GetModule("MISSING"));
    }

    [Fact]
    public async Task UnhandledThrowProducesRuntimeExceptionWithScriptFrame()
    {
        using var workspace = new TestWorkspace();
        var (_, domain) = await workspace.CompileModuleAsync(
            """
            @module(TEST);
            func inner() { throw new Error('release failure'); }
            export func outer() { return inner(); }
            """);

        var error = Assert.Throws<AuroraRuntimeException>(() => TestWorkspace.Execute(domain, "outer"));

        Assert.Contains("release failure", error.Message, StringComparison.Ordinal);
        Assert.NotNull(error.StackTrace);
        Assert.Contains(error.StackTrace, frame => frame.MethodName.Contains("inner", StringComparison.Ordinal));
    }

    [Theory]
    [InlineData("var value = null; return value.name;", "Cannot read")]
    [InlineData("var value = null; value.name = 1; return value;", "Cannot")]
    [InlineData("var value = 1; return value();", "called")]
    public void InvalidRuntimeOperationsThrowAuroraRuntimeException(string body, string message)
    {
        using var workspace = new TestWorkspace();
        var engine = workspace.CreateEngine();
        var block = engine.CompileBlock(body);

        var error = Assert.Throws<AuroraRuntimeException>(() => block.Invoke(Array.Empty<ScriptDatum>()));
        Assert.Contains(message, error.ToString(), StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task ConstModulePropertyCannotBeReassignedAtRuntime()
    {
        using var workspace = new TestWorkspace();
        var (_, domain) = await workspace.CompileModuleAsync(
            "@module(TEST); export const value = 1; export func mutate() { value = 2; }");

        Assert.Throws<AuroraRuntimeException>(() => TestWorkspace.Execute(domain, "mutate"));
    }

    [Fact]
    public async Task UserStateIsVisibleThroughStateAndCanDifferPerDomain()
    {
        using var workspace = new TestWorkspace();
        var engine = workspace.CreateEngine();
        engine.RegisterType<HostState>();
        await engine.BuildAsync(engine.MemorySource(
            "main.as",
            "@module(TEST); export func read() { return $state.Value; }"));

        var first = engine.CreateDomain(userState: new HostState { Value = 10 });
        var second = engine.CreateDomain(userState: new HostState { Value = 20 });

        ScriptAssert.Equal(10, TestWorkspace.Execute(first, "read"));
        ScriptAssert.Equal(20, TestWorkspace.Execute(second, "read"));
    }

    [Fact]
    public async Task DisposedDomainNoLongerExposesModules()
    {
        using var workspace = new TestWorkspace();
        var (_, domain) = await workspace.CompileModuleAsync("@module(TEST); export func run() { return 1; }");

        domain.Dispose();

        Assert.Throws<AuroraException>(() => TestWorkspace.Execute(domain, "run"));
    }

    public sealed class HostState
    {
        public int Value { get; set; }
    }
}
