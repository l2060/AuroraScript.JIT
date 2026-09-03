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
        Assert.Contains(error.StackTrace, frame => frame.Method.Contains("inner", StringComparison.Ordinal));
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
    public async Task ConstModulePropertyCannotBeReassignedAtCompileTime()
    {
        using var workspace = new TestWorkspace();

        var error = await Assert.ThrowsAsync<AuroraCompilationException>(() => workspace.CompileModuleAsync(
            "@module(TEST); export const value = 1; export func mutate() { value = 2; }"));

        Assert.Contains("Cannot assign to constant 'value'", error.Message);
    }

    [Fact]
    public async Task UserStateIsVisibleThroughStateAndCanDifferPerDomain()
    {
        using var workspace = new TestWorkspace();
        var engine = workspace.CreateEngine();
        await engine.BuildAsync(workspace.MemorySource(
            "main.as",
            "@module(TEST); context bag; export func read() { return bag.Value; }"));

        var firstState = new ScriptObject();
        firstState.Define("Value", ScriptDatum.FromNumber(10));
        var secondState = new ScriptObject();
        secondState.Define("Value", ScriptDatum.FromNumber(20));

        var first = engine.CreateDomain(userState: firstState);
        var second = engine.CreateDomain(userState: secondState);

        ScriptAssert.Equal(10, first.Execute("TEST", "read"));
        ScriptAssert.Equal(20, second.Execute("TEST", "read"));
    }

    [Fact]
    public async Task UserStateCanDefineScriptDatumPropertiesDirectly()
    {
        using var workspace = new TestWorkspace();
        var engine = workspace.CreateEngine();
        await engine.BuildAsync(workspace.MemorySource(
            "main.as",
            """
            @module(TEST);
            context bag;
            export func run() {
                return [bag.Name, bag.Count, bag.Enabled, bag.Empty];
            }
            """));

        var userState = new ScriptObject();
        userState.Define("Name", ScriptDatum.FromString("datum"));
        userState.Define("Count", ScriptDatum.FromNumber(3));
        userState.Define("Enabled", ScriptDatum.FromBoolean(true));
        userState.Define("Empty", ScriptDatum.Null);

        var domain = engine.CreateDomain(userState: userState);

        ScriptAssert.Equal(new object?[] { "datum", 3, true, null }, domain.Execute("TEST", "run"));
    }

    [Fact]
    public async Task UserStateFallsBackToClrMethods()
    {
        using var workspace = new TestWorkspace();
        var engine = workspace.CreateEngine();
        await engine.BuildAsync(workspace.MemorySource(
            "main.as",
            """
            @module(TEST);
            context bag;
            export func run() {
                return [bag.Add(20, 22), bag.Title];
            }
            """));

        var domain = engine.CreateDomain(userState: new MethodState());

        ScriptAssert.Equal(new object?[] { 42, "state" }, domain.Execute("TEST", "run"));
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

    private sealed class MethodState : ScriptObject
    {
        public string Title => "state";
        public int Add(int left, int right) => left + right;
    }
}
