using AuroraScript.Runtime;
using AuroraScript.Runtime.Interop;
using AuroraScript.Tests.Infrastructure;
using System;
using System.Threading.Tasks;
using Xunit;

namespace AuroraScript.Tests;

public sealed class ClrInteropTests
{
    [Fact]
    public async Task RegisteredTypeSupportsConstructorPropertiesFieldsInstanceAndStaticMethods()
    {
        using var workspace = new TestWorkspace();
        var engine = workspace.CreateEngine();
        engine.RegisterType<HostCalculator>("Calculator");
        await engine.BuildAsync(engine.MemorySource(
            "main.as",
            """
            @module(TEST);
            export func run() {
                var host = new Calculator(5);
                host.Value = 7;
                host.Field = 3;
                return [host.Add(2), host.Join('A', 2), Calculator.Multiply(3, 4), host.Value, host.Field];
            }
            """));

        ScriptAssert.Equal(
            new object?[] { 9, "AA", 12, 7, 3 },
            TestWorkspace.Execute(engine.CreateDomain(), "run"));
    }

    [Fact]
    public async Task GlobalClrValuesCollectionsAndDelegateAreMarshalledBothWays()
    {
        using var workspace = new TestWorkspace();
        var (_, domain) = await workspace.CompileModuleAsync(
            """
            @module(TEST);
            export func run() {
                return [HOST_NUMBER, HOST_TEXT, HOST_VALUES[1], HOST_ADD(20, 22)];
            }
            """,
            configureGlobal: global =>
            {
                global.Define("HOST_NUMBER", 7);
                global.Define("HOST_TEXT", "Aurora");
                global.Define("HOST_VALUES", new[] { 1, 2, 3 });
                global.Define("HOST_ADD", (Func<int, int, int>)((left, right) => left + right));
            });

        ScriptAssert.Equal(new object?[] { 7, "Aurora", 2, 42 }, TestWorkspace.Execute(domain, "run"));
    }

    [Fact]
    public async Task OverloadResolutionSupportsNumericStringOptionalAndVariadicArguments()
    {
        using var workspace = new TestWorkspace();
        var engine = workspace.CreateEngine();
        engine.RegisterType<HostOverloads>();
        await engine.BuildAsync(engine.MemorySource(
            "main.as",
            """
            @module(TEST);
            export func run() {
                return [HostOverloads.Select(2), HostOverloads.Select('x'), HostOverloads.Optional(2), HostOverloads.Sum(1, 2, 3, 4)];
            }
            """));

        ScriptAssert.Equal(
            new object?[] { "number:2", "string:x", 7, 10 },
            TestWorkspace.Execute(engine.CreateDomain(), "run"));
    }

    [Fact]
    public async Task TypeAccessRestrictionsAreEnforced()
    {
        using var workspace = new TestWorkspace();
        var constructorOnly = workspace.CreateEngine();
        constructorOnly.RegisterType<HostCalculator>("ConstructorOnly", TypeAccess.Constructor);
        await constructorOnly.BuildAsync(constructorOnly.MemorySource(
            "constructor.as",
            "@module(TEST); export func run() { return ConstructorOnly.Multiply(2, 3); }"));
        Assert.ThrowsAny<Exception>(() => TestWorkspace.Execute(constructorOnly.CreateDomain(), "run"));

        var staticOnly = workspace.CreateEngine();
        staticOnly.RegisterType<HostCalculator>("StaticOnly", TypeAccess.Static);
        await staticOnly.BuildAsync(staticOnly.MemorySource(
            "static.as",
            "@module(TEST); export func run() { return new StaticOnly(1); }"));
        Assert.ThrowsAny<Exception>(() => TestWorkspace.Execute(staticOnly.CreateDomain(), "run"));
    }

    [Fact]
    public void RegistryRejectsDuplicateAliasesAndUseAfterDispose()
    {
        var registry = new ClrTypeRegistry();
        registry.RegisterType(typeof(HostCalculator), "Host", TypeAccess.All);

        Assert.Throws<ArgumentException>(() => registry.RegisterType(typeof(HostOverloads), "Host", TypeAccess.All));
        Assert.True(registry.UnregisterType("Host"));
        Assert.False(registry.UnregisterType("Host"));
        registry.Dispose();
        Assert.Throws<ObjectDisposedException>(() => registry.TryGetClrType("Host", out _));
    }

    public sealed class HostCalculator
    {
        public HostCalculator(int value) => Value = value;
        public int Value { get; set; }
        public int Field;
        public int Add(int value) => Value + value;
        public string Join(string value, int count) => string.Concat(System.Linq.Enumerable.Repeat(value, count));
        public static int Multiply(int left, int right) => left * right;
    }

    public sealed class HostOverloads
    {
        public static string Select(int value) => "number:" + value;
        public static string Select(string value) => "string:" + value;
        public static int Optional(int value, int offset = 5) => value + offset;
        public static int Sum(params int[] values) => System.Linq.Enumerable.Sum(values);
    }
}
