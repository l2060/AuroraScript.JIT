using AuroraScript.Runtime;
using AuroraScript.Runtime.Interop;
using AuroraScript.Runtime.Types;
using AuroraScript.Tests.Infrastructure;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Threading.Tasks;
using Xunit;

namespace AuroraScript.Tests;

public sealed class ClrDirectCompilationTests
{
    private const string MemberScript = """
        @module(TEST);
        export func run() {
            var host = new Host(4);
            host.Field = 5;
            host.Value = 6;
            host.Field += 2;
            var before = host.Value++;
            Host.StaticField = 10;
            Host.StaticValue = 11;
            Host.StaticField++;
            Host.StaticValue += 2;
            return [host.Add(3), host.Field, before, host.Value,
                Host.Sum(2, 3), Host.StaticField, Host.StaticValue,
                new Host(9).Value, host.Child().Add(1), host.Self.Value];
        }
        """;

    [Theory]
    [InlineData(CompilationMode.Dynamic)]
    [InlineData(CompilationMode.OnlyRun)]
#if NET9_0_OR_GREATER
    [InlineData(CompilationMode.Persistence)]
#endif
    public async Task RegisteredMembersExecuteInEveryCompilationMode(CompilationMode mode)
    {
        using var workspace = new TestWorkspace();
        var engine = workspace.CreateEngine(mode: mode, assemblyOut: Path.Combine(workspace.Root, "clr.dll"),
            configureRuntime: runtime => runtime.RegisterCLRType<Host>("Host"));
        await engine.BuildAsync(workspace.MemorySource("main.as", MemberScript));
        ScriptAssert.Equal(new object?[] { 10, 7, 6, 7, 5, 11, 13, 9, 9, 7 },
            TestWorkspace.Execute(engine.CreateDomain(), "run"));
    }

#if NET9_0_OR_GREATER
    [Fact]
    public async Task PersistedIlContainsDirectCallsConstructionAndFieldAccess()
    {
        using var workspace = new TestWorkspace();
        var path = Path.Combine(workspace.Root, "clr-direct.dll");
        var engine = workspace.CreateEngine(mode: CompilationMode.Persistence, assemblyOut: path,
            configureRuntime: runtime => runtime.RegisterCLRType<Host>("Host"));
        await engine.BuildAsync(workspace.MemorySource("main.as", MemberScript));
        var assembly = Assembly.Load(File.ReadAllBytes(path));
        var instructions = assembly.GetTypes().SelectMany(type => type.GetMethods(
            BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.DeclaredOnly))
            .SelectMany(ReadMemberInstructions).ToArray();
        Assert.Contains(instructions, entry => entry.Op == OpCodes.Newobj && entry.Member.DeclaringType == typeof(Host));
        Assert.Contains(instructions, entry => entry.Op == OpCodes.Call && entry.Member.Name == nameof(Host.Sum));
        Assert.Contains(instructions, entry => entry.Op == OpCodes.Callvirt && entry.Member.Name == nameof(Host.Add));
        Assert.Contains(instructions, entry => entry.Op == OpCodes.Callvirt && entry.Member.Name == "get_Value");
        Assert.Contains(instructions, entry => entry.Op == OpCodes.Callvirt && entry.Member.Name == "set_Value");
        Assert.Contains(instructions, entry => entry.Op == OpCodes.Ldfld && entry.Member.Name == nameof(Host.Field));
        Assert.Contains(instructions, entry => entry.Op == OpCodes.Stfld && entry.Member.Name == nameof(Host.Field));
        Assert.Contains(instructions, entry => entry.Op == OpCodes.Ldsfld && entry.Member.Name == nameof(Host.StaticField));
        Assert.Contains(instructions, entry => entry.Op == OpCodes.Stsfld && entry.Member.Name == nameof(Host.StaticField));
    }
#endif

    [Theory]
    [InlineData(CompilationMode.Dynamic)]
    [InlineData(CompilationMode.OnlyRun)]
#if NET9_0_OR_GREATER
    [InlineData(CompilationMode.Persistence)]
#endif
    public async Task CompilationCatalogAliasesAreFrozenAndIgnoreGlobalShadowing(CompilationMode mode)
    {
        using var workspace = new TestWorkspace();
        var engine = workspace.CreateEngine(mode: mode, assemblyOut: Path.Combine(workspace.Root, "rebind.dll"),
            configureRuntime: runtime => runtime.RegisterCLRType<Host>("Host"));
        await engine.BuildAsync(workspace.MemorySource("main.as", """
            @module(TEST);
            export func run() { var h = new Host(4); h.Value = 6; return [h.Add(1), Host.Sum(2, 3), h.Value]; }
            """));
        using var domain = engine.CreateDomain();
        ScriptAssert.Equal(new object?[] { 7, 5, 6 }, TestWorkspace.Execute(domain, "run"));
        var frozen = Assert.Throws<InvalidOperationException>(() =>
            engine.ClrRegistry.UnregisterType("Host"));
        Assert.Contains("frozen compilation catalog", frozen.Message, StringComparison.Ordinal);

        var replacement = new ScriptObject();
        replacement.Define("Sum", ClrMarshaller.ToScript((Func<int, int, int>)((a, b) => a * b)));
        var shadow = Assert.Throws<AuroraRuntimeException>(() =>
            engine.CreateDomain(global => global.Define("Host", replacement)));
        Assert.Contains("cannot be shadowed", shadow.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task RuntimeOnlyRegistrationsRemainDynamic()
    {
        using var workspace = new TestWorkspace();
        var engine = workspace.CreateEngine();
        engine.ClrRegistry.RegisterType(typeof(Host), "RuntimeHost", TypeAccess.All);
        await engine.BuildAsync(workspace.MemorySource("main.as", """
            @module(TEST);
            export func run() { return RuntimeHost.Sum(2, 3); }
            """));
        using var domain = engine.CreateDomain();
        Assert.Equal(5d, TestWorkspace.Execute(domain, "run"));
        Assert.True(engine.ClrRegistry.UnregisterType("RuntimeHost"));
    }

#if NET9_0_OR_GREATER
    [Fact]
    public async Task AssignmentAndMatchingBranchFlowPreserveClrTypeProof()
    {
        using var workspace = new TestWorkspace();
        var path = Path.Combine(workspace.Root, "clr-flow.dll");
        var engine = workspace.CreateEngine(
            mode: CompilationMode.Persistence,
            assemblyOut: path,
            configureRuntime: runtime => runtime.RegisterCLRType<Host>("Host"));
        await engine.BuildAsync(workspace.MemorySource("main.as", """
            @module(TEST);
            export func run(flag) {
                var host;
                if (flag) host = new Host(4);
                else host = new Host(5);
                return host.Add(3);
            }
            """));
        using var domain = engine.CreateDomain();
        Assert.Equal(7d, TestWorkspace.Execute(
            domain, "run", arguments: [ScriptDatum.FromBoolean(true)]));
        Assert.Equal(8d, TestWorkspace.Execute(
            domain, "run", arguments: [ScriptDatum.FromBoolean(false)]));

        var assembly = Assembly.Load(File.ReadAllBytes(path));
        var instructions = assembly.GetTypes().SelectMany(type => type.GetMethods(
                BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.DeclaredOnly))
            .SelectMany(ReadMemberInstructions);
        Assert.Contains(instructions, entry =>
            entry.Op == OpCodes.Callvirt && entry.Member.Name == nameof(Host.Add));
    }
#endif

    [Theory]
    [InlineData(CompilationMode.Dynamic)]
    [InlineData(CompilationMode.OnlyRun)]
#if NET9_0_OR_GREATER
    [InlineData(CompilationMode.Persistence)]
#endif
    public async Task FrozenClrAliasesSupportChecksAndParameterContracts(
        CompilationMode mode)
    {
        using var workspace = new TestWorkspace();
        var engine = workspace.CreateEngine(
            mode: mode,
            assemblyOut: Path.Combine(workspace.Root, "clr-contracts.dll"),
            configureRuntime: runtime => runtime.RegisterCLRType<Host>("Host"));
        await engine.BuildAsync(workspace.MemorySource("main.as", """
            @module(TEST);
            func read(Host value) { return value.Add(3); }
            export func parameter() { return read(new Host(4)); }
            export func checked() {
                var value = (new Host(5)) as Host;
                return value.Add(2);
            }
            export func badParameter() { return read(1); }
            export func badCheck() { return 1 as Host; }
            """));
        using var domain = engine.CreateDomain();

        Assert.Equal(7d, TestWorkspace.Execute(domain, "parameter"));
        Assert.Equal(7d, TestWorkspace.Execute(domain, "checked"));
        Assert.ThrowsAny<Exception>(() =>
            TestWorkspace.Execute(domain, "badParameter"));
        Assert.ThrowsAny<Exception>(() =>
            TestWorkspace.Execute(domain, "badCheck"));

#if NET9_0_OR_GREATER
        if (mode == CompilationMode.Persistence)
        {
            var assembly = Assembly.Load(File.ReadAllBytes(
                Path.Combine(workspace.Root, "clr-contracts.dll")));
            var instructions = assembly.GetTypes().SelectMany(type => type.GetMethods(
                    BindingFlags.Public | BindingFlags.NonPublic |
                    BindingFlags.Static | BindingFlags.DeclaredOnly))
                .SelectMany(ReadMemberInstructions);
            Assert.Contains(instructions, entry =>
                entry.Op == OpCodes.Callvirt &&
                entry.Member.Name == nameof(Host.Add));
        }
#endif
    }

    [Fact]
    public async Task RuntimeOnlyClrAliasesCannotBeUsedAsTypeContracts()
    {
        using var workspace = new TestWorkspace();
        var engine = workspace.CreateEngine();
        engine.ClrRegistry.RegisterType(typeof(Host), "RuntimeHost", TypeAccess.All);

        var error = await Assert.ThrowsAsync<AuroraCompilationException>(() =>
            engine.BuildAsync(workspace.MemorySource("main.as", """
                @module(TEST);
                export func read(RuntimeHost value) { return value.Value; }
                """)));
        Assert.Contains(
            "Unknown or inaccessible type 'RuntimeHost'",
            error.Message,
            StringComparison.Ordinal);
    }

    [Fact]
    public async Task UnknownMemberOnProvenClrTypeProducesNonFatalWarning()
    {
        using var workspace = new TestWorkspace();
        using var warningOutput = new StringWriter();
        var engine = workspace.CreateEngine(configureRuntime: runtime =>
        {
            runtime.RegisterCLRType<Host>("Host");
            runtime.ConsoleErrorOut = warningOutput;
        });
        await engine.BuildAsync(workspace.MemorySource("main.as", """
            @module(TEST);
            export func run() {
                var value = new Host(1);
                value.fs = 123456;
                return [value.Value, Host.Optional(1)];
            }
            """));

        var warning = Assert.Single(engine.CompilationWarnings);
        Assert.Equal(
            AuroraCompilationDiagnosticSeverity.Warning,
            warning.Severity);
        Assert.Contains(
            "does not contain an accessible writable instance member 'fs'",
            warning.Message,
            StringComparison.Ordinal);
        Assert.Contains("warning:", warningOutput.ToString(), StringComparison.Ordinal);

        using var domain = engine.CreateDomain();
        ScriptAssert.Equal(
            new object?[] { 1, 6 },
            TestWorkspace.Execute(domain, "run"));
    }

    [Fact]
    public async Task ReceiverAndArgumentsAreEvaluatedOnceAcrossFallback()
    {
        using var workspace = new TestWorkspace();
        var engine = workspace.CreateEngine(configureRuntime: runtime => runtime.RegisterCLRType<Host>("Host"));
        await engine.BuildAsync(workspace.MemorySource("main.as", """
            @module(TEST);
            export func run() {
                var h = new Host(1);
                var count = 0;
                h = { Value: 3, Add: (x) => x + 20 };
                var answer = h.Add(++count);
                h.Value += ++count;
                return [answer, h.Value, count];
            }
            """));
        ScriptAssert.Equal(new object?[] { 21, 5, 2 }, TestWorkspace.Execute(engine.CreateDomain(), "run"));
    }

    [Fact]
    public async Task UnknownArgumentsOptionalParamsAndMethodValuesRemainDynamic()
    {
        using var workspace = new TestWorkspace();
        var engine = workspace.CreateEngine(configureRuntime: runtime => runtime.RegisterCLRType<Host>("Host"));
        await engine.BuildAsync(workspace.MemorySource("main.as", """
            @module(TEST);
            export func run(value) {
                var method = Host.Select;
                var args = [2, 3];
                return [Host.Select(value), method(value), Host.Optional(2), Host.Many(...args), Host.Sum(...args)];
            }
            """));
        using var domain = engine.CreateDomain();
        ScriptAssert.Equal(new object?[] { "s:x", "s:x", 7, 5, 5 }, TestWorkspace.Execute(domain, "run", arguments: [ScriptDatum.FromString("x")]));
        ScriptAssert.Equal(new object?[] { "n:3", "n:3", 7, 5, 5 }, TestWorkspace.Execute(domain, "run", arguments: [ScriptDatum.FromNumber(3)]));
    }

    [Fact]
    public async Task ConstructorExceptionsAndAccessRestrictionsArePreserved()
    {
        using var workspace = new TestWorkspace();
        var engine = workspace.CreateEngine(configureRuntime: runtime => runtime
            .RegisterCLRType<Host>("Host").RegisterCLRType<Host>("StaticOnly", TypeAccess.Static)
            .RegisterCLRType<Host>("ConstructorOnly", TypeAccess.Constructor));
        await engine.BuildAsync(workspace.MemorySource("main.as", """
            @module(TEST);
            export func fail() { return [1, new Host(-1)]; }
            export func constructor() { return new StaticOnly(1); }
            export func method() { return ConstructorOnly.Sum(1, 2); }
            export func read() { return ConstructorOnly.StaticValue; }
            """));
        using var domain = engine.CreateDomain();
        var error = Assert.ThrowsAny<Exception>(() => TestWorkspace.Execute(domain, "fail"));
        var chain = new List<Exception>();
        for (Exception? current = error; current != null; current = current.InnerException) chain.Add(current);
        Assert.Contains(chain, exception => exception is TargetInvocationException);
        Assert.Contains(chain, exception => exception is InvalidOperationException && exception.Message == "negative");
        Assert.ThrowsAny<Exception>(() => TestWorkspace.Execute(domain, "constructor"));
        Assert.ThrowsAny<Exception>(() => TestWorkspace.Execute(domain, "method"));
        Assert.ThrowsAny<Exception>(() => TestWorkspace.Execute(domain, "read"));
    }

    [Theory]
    [InlineData(CompilationMode.Dynamic)]
    [InlineData(CompilationMode.OnlyRun)]
#if NET9_0_OR_GREATER
    [InlineData(CompilationMode.Persistence)]
#endif
    public async Task DirectAndDynamicPathsAgreeOnConversionsAndReturnValues(CompilationMode mode)
    {
        using var workspace = new TestWorkspace();
        const string source = """
            @module(TEST);
            export func run() {
                return [Host.Sum(1.9, 2.8), Host.Select(3), Host.Select('x'),
                    Host.NullText(), Host.NoResult(), Host.Pi, Host.Big,
                    Host.LongValue(9007199254740993L), Host.UnsignedValue(18446744073709551615UL),
                    Host.Narrow(9007199254740993L), Host.LongAsDouble(18446744073709551615UL)];
            }
            """;
        var direct = workspace.CreateEngine(mode: mode, assemblyOut: Path.Combine(workspace.Root, "direct.dll"),
            configureRuntime: runtime => runtime.RegisterCLRType<Host>("Host"));
        var dynamic = workspace.CreateEngine(mode: mode, assemblyOut: Path.Combine(workspace.Root, "dynamic.dll"));
        dynamic.ClrRegistry.RegisterType(typeof(Host), "Host", TypeAccess.All);
        await direct.BuildAsync(workspace.MemorySource("direct.as", source));
        await dynamic.BuildAsync(workspace.MemorySource("dynamic.as", source));
        using var directDomain = direct.CreateDomain();
        using var dynamicDomain = dynamic.CreateDomain();
        var actual = TestWorkspace.Execute(directDomain, "run");
        var expected = TestWorkspace.Execute(dynamicDomain, "run");
        var expectedValues = new object?[] { 3, "n:3", "s:x", null, null, 3.5, ulong.MaxValue,
            9007199254740993L, ulong.MaxValue, "long", (double)ulong.MaxValue };
        var actualArray = Assert.IsType<ScriptArray>(actual.Object);
        var expectedArray = Assert.IsType<ScriptArray>(expected.Object);
        Assert.Equal(expectedValues.Length, actualArray.Length);
        for (var i = 0; i < expectedValues.Length; i++)
        {
            var value = ClrMarshaller.ToDatum(expectedValues[i]);
            Assert.Equal(value.Kind, actualArray.GetElement(i).Kind);
            Assert.Equal(value, actualArray.GetElement(i));
            Assert.Equal(expectedArray.GetElement(i), actualArray.GetElement(i));
        }
    }

    private static IEnumerable<(OpCode Op, MemberInfo Member)> ReadMemberInstructions(MethodInfo method)
    {
        var codes = typeof(OpCodes).GetFields(BindingFlags.Public | BindingFlags.Static)
            .Where(field => field.FieldType == typeof(OpCode)).Select(field => (OpCode)field.GetValue(null)!)
            .ToDictionary(code => unchecked((ushort)code.Value));
        var bytes = method.GetMethodBody()?.GetILAsByteArray() ?? Array.Empty<byte>();
        for (var offset = 0; offset < bytes.Length;)
        {
            var first = bytes[offset++];
            var code = codes[first == 0xfe ? (ushort)(0xfe00 | bytes[offset++]) : first];
            if (code.OperandType is OperandType.InlineMethod or OperandType.InlineField)
                yield return (code, method.Module.ResolveMember(BitConverter.ToInt32(bytes, offset))!);
            offset += code.OperandType switch
            {
                OperandType.InlineNone => 0,
                OperandType.ShortInlineBrTarget or OperandType.ShortInlineI or OperandType.ShortInlineVar => 1,
                OperandType.InlineVar => 2,
                OperandType.InlineI8 or OperandType.InlineR => 8,
                OperandType.InlineSwitch => 4 + 4 * BitConverter.ToInt32(bytes, offset),
                _ => 4
            };
        }
    }

    public class Host
    {
        public Host(int value) { if (value < 0) throw new InvalidOperationException("negative"); Value = value; }
        public int Field;
        public int Value { get; set; }
        public static int StaticField;
        public static int StaticValue { get; set; }
        public Host Self => this;
        public virtual int Add(int value) => Value + value;
        public Host Child() => new(Value + 1);
        public static int Sum(int a, int b) => a + b;
        public static string Select(int value) => "n:" + value;
        public static string Select(string value) => "s:" + value;
        public static int Optional(int value, int offset = 5) => value + offset;
        public static int Many(params int[] values) => values.Sum();
        public const double Pi = 3.5;
        public const ulong Big = ulong.MaxValue;
        public static string? NullText() => null;
        public static void NoResult() { }
        public static long LongValue(long value) => value;
        public static ulong UnsignedValue(ulong value) => value;
        public static double LongAsDouble(double value) => value;
        public static string Narrow(int value) => "int";
        public static string Narrow(long value) => "long";
    }

    public sealed class OtherHost
    {
        public OtherHost(int value) => Value = value;
        public int Value { get; set; }
        public int Add(int value) => 100 + Value + value;
        public static int Sum(int a, int b) => 100 + a + b;
    }
}
