using AuroraScript.Runtime;
using AuroraScript.Runtime.Types;
using AuroraScript.Tests.Infrastructure;
using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace AuroraScript.Tests;

public sealed class ReleaseRegressionTests
{
    [Fact]
    public async Task PostfixMutationKeepsTheOriginalDynamicValueKind()
    {
        using var workspace = new TestWorkspace();
        var (_, domain) = await workspace.CompileModuleAsync(
            """
            @module(TEST);
            export func run() {
                var numericText = "2";
                var invalidText = "x";
                var truth = true;
                var empty = null;
                return [
                    numericText++ + 1,
                    numericText,
                    invalidText++ + 1,
                    Number.isNaN(invalidText),
                    truth++,
                    truth,
                    empty++,
                    Number.isNaN(empty)
                ];
            }
            """);

        ScriptAssert.Equal(
            new object?[] { "21", 3, "x1", true, true, 2, null, true },
            TestWorkspace.Execute(domain, "run"));
    }

    [Fact]
    public async Task UnsignedRightShiftKeepsUInt32ResultsOnTheNativeNumberPath()
    {
        using var workspace = new TestWorkspace();
        var (_, domain) = await workspace.CompileModuleAsync(
            "@module(TEST); export func run(value) { return [value >>> 0, value >>> 1, -2147483648 >>> 0]; }");

        ScriptAssert.Equal(
            new object?[] { 4294967295d, 2147483647d, 2147483648d },
            TestWorkspace.Execute(domain, "run", arguments: ScriptDatum.FromNumber(-1)));
    }

    [Fact]
    public async Task NativeBitwiseTruncationMatchesTheDynamicRuntimeBoundary()
    {
        using var workspace = new TestWorkspace();
        var (_, domain) = await workspace.CompileModuleAsync(
            """
            @module(TEST);
            export func run(value) {
                return [
                    4294967295 & 1, value & 1,
                    4294967295 | 0, value | 0,
                    4294967295 ^ 0, value ^ 0,
                    ~4294967295, ~value
                ];
            }
            """);

        ScriptAssert.Equal(
            new object?[] { 1, 1, -1, -1, -1, -1, 0, 0 },
            TestWorkspace.Execute(domain, "run", arguments: ScriptDatum.FromNumber(4294967295d)));
    }

    [Fact]
    public async Task ParameterComparisonCachePreservesStringEquality()
    {
        using var workspace = new TestWorkspace();
        var (_, domain) = await workspace.CompileModuleAsync(
            "@module(TEST); export func run(left, right) { return [left == right, left != right, left < right]; }");

        ScriptAssert.Equal(
            new object?[] { false, true, false },
            TestWorkspace.Execute(
                domain,
                "run",
                arguments: [ScriptDatum.FromString("01"), ScriptDatum.FromString("1")]));
        ScriptAssert.Equal(
            new object?[] { true, false, false },
            TestWorkspace.Execute(
                domain,
                "run",
                arguments: [ScriptDatum.FromString("same"), ScriptDatum.FromString("same")]));
    }

    [Fact]
    public async Task InOperatorPreservesSubstringAndEstablishedElementEquality()
    {
        using var workspace = new TestWorkspace();
        var (_, domain) = await workspace.CompileModuleAsync(
            "@module(TEST); export func run() { return [(\"ur\" in \"Aurora\"), (\"u\" in \"Aurora\"), (1 in \"1\")]; }");

        ScriptAssert.Equal(
            new object?[] { true, true, true },
            TestWorkspace.Execute(domain, "run"));
    }

    [Fact]
    public async Task StringIterationHandlesAllUtf16CodeUnits()
    {
        using var workspace = new TestWorkspace();
        var (_, domain) = await workspace.CompileModuleAsync(
            """
            @module(TEST);
            export func run() {
                var text = "A你😀";
                var copy = "";
                var count = 0;
                for (var ch in text) {
                    copy += ch;
                    count++;
                }
                return [copy, count];
            }
            """);

        ScriptAssert.Equal(
            new object?[] { "A你😀", 4 },
            TestWorkspace.Execute(domain, "run"));
    }

    [Fact]
    public async Task BreakAndContinueCanLeaveTryRegionsAndStillRunFinally()
    {
        using var workspace = new TestWorkspace();
        var (_, domain) = await workspace.CompileModuleAsync(
            """
            @module(TEST);
            export func run() {
                var total = 0;
                for (var i = 0; i < 5; i++) {
                    try {
                        if (i == 1) continue;
                        if (i == 3) break;
                        total += i;
                    } finally {
                        total += 10;
                    }
                }
                return total;
            }
            """);

        ScriptAssert.Equal(42, TestWorkspace.Execute(domain, "run"));
    }

    [Fact]
    public async Task BreakAndContinueFromFinallyPreserveStructuredControlFlow()
    {
        using var workspace = new TestWorkspace();
        var (_, domain) = await workspace.CompileModuleAsync(
            """
            @module(TEST);
            func transferTrace() {
                var trace = "";
                for (var i = 0; i < 4; i++) {
                    try {
                        trace += "T" + i;
                    } finally {
                        if (i == 0) { trace += "C"; continue; }
                        if (i == 2) { trace += "B"; break; }
                        trace += "F";
                    }
                    trace += "A";
                }
                return trace;
            }
            func bypassCatch() {
                for (var i = 0; i < 1; i++) {
                    try {
                        try { }
                        finally { break; }
                    } catch (error) {
                        return 99;
                    }
                }
                return 7;
            }
            func overridePendingReturn() {
                for (var i = 0; i < 1; i++) {
                    try { return 1; }
                    finally { break; }
                }
                return 2;
            }
            func nestedLoopInFinally() {
                var count = 0;
                try { }
                finally {
                    while (true) {
                        count++;
                        break;
                    }
                }
                return count;
            }
            export func run() {
                return [transferTrace(), bypassCatch(), overridePendingReturn(), nestedLoopInFinally()];
            }
            """);

        ScriptAssert.Equal(
            new object?[] { "T0CT1FAT2B", 7, 2, 1 },
            TestWorkspace.Execute(domain, "run"));
    }

    [Fact]
    public async Task ReturnFromFinallyOverridesPendingReturnAndBypassesScriptCatch()
    {
        using var workspace = new TestWorkspace();
        var (_, domain) = await workspace.CompileModuleAsync(
            """
            @module(TEST);
            func direct() {
                try { return 1; }
                finally { return 2; }
            }
            export func run() {
                try {
                    try { throw "ignored"; }
                    finally { return direct() + 5; }
                } catch (error) {
                    return 99;
                }
            }
            """);

        ScriptAssert.Equal(7, TestWorkspace.Execute(domain, "run"));
    }

    [Fact]
    public async Task DirectCallFastPathsCoverZeroThroughSevenAndFallbackArity()
    {
        using var workspace = new TestWorkspace();
        var (_, domain) = await workspace.CompileModuleAsync(
            """
            @module(TEST);
            func f0() { return 0; }
            func f1(a) { return a; }
            func f2(a,b) { return a+b; }
            func f3(a,b,c) { return a+b+c; }
            func f4(a,b,c,d) { return a+b+c+d; }
            func f5(a,b,c,d,e) { return a+b+c+d+e; }
            func f6(a,b,c,d,e,f) { return a+b+c+d+e+f; }
            func f7(a,b,c,d,e,f,g) { return a+b+c+d+e+f+g; }
            func f8(a,b,c,d,e,f,g,h) { return a+b+c+d+e+f+g+h; }
            export func run() { return [f0(),f1(1),f2(1,2),f3(1,2,3),f4(1,2,3,4),f5(1,2,3,4,5),f6(1,2,3,4,5,6),f7(1,2,3,4,5,6,7),f8(1,2,3,4,5,6,7,8)]; }
            """,
            enableHotReload: false);

        ScriptAssert.Equal(
            new object?[] { 0, 1, 3, 6, 10, 15, 21, 28, 36 },
            TestWorkspace.Execute(domain, "run"));
    }

    [Fact]
    public async Task DirectCallCandidateWithSpreadKeepsDynamicAritySemantics()
    {
        using var workspace = new TestWorkspace();
        var (_, domain) = await workspace.CompileModuleAsync(
            """
            @module(TEST);
            func join(left, right) { return left + "-" + right; }
            export func run() { return join(...["Aurora", "Script"]); }
            """,
            enableHotReload: false);

        ScriptAssert.Equal("Aurora-Script", TestWorkspace.Execute(domain, "run"));
    }

    [Fact]
    public async Task DirectCallDefaultParametersUseTheGenericAdapter()
    {
        using var workspace = new TestWorkspace();
        var (_, domain) = await workspace.CompileModuleAsync(
            """
            @module(TEST);
            func add(left, right = 5) { return left + right; }
            export func run() { return [add(2), add(2, 3)]; }
            """,
            enableHotReload: false);

        ScriptAssert.Equal(new object?[] { 7, 5 }, TestWorkspace.Execute(domain, "run"));
    }

    [Fact]
    public async Task NativeDirectCallsSupportRecursion()
    {
        using var workspace = new TestWorkspace();
        var (_, domain) = await workspace.CompileModuleAsync(
            """
            @module(TEST);
            native func sum(Number value, Number total) Number {
                if (value <= 0) return total;
                return sum(value - 1, total + value);
            }
            export func run() { return sum(100, 0); }
            """,
            enableHotReload: false);

        ScriptAssert.Equal(5050, TestWorkspace.Execute(domain, "run"));
    }

    [Fact]
    public async Task NativeDirectSpecializationDoesNotCoerceGenericEntryArguments()
    {
        using var workspace = new TestWorkspace();
        var (_, domain) = await workspace.CompileModuleAsync(
            """
            @module(TEST);
            type Datum {}
            native func identity(Datum value) Datum { return value; }
            export native func relay(Datum value) Datum { return identity(value); }
            export func warmup() { return relay(41) + 1; }
            """,
            enableHotReload: false);

        ScriptAssert.Equal(42, TestWorkspace.Execute(domain, "warmup"));
        ScriptAssert.Equal(
            "Aurora",
            TestWorkspace.Execute(domain, "relay", arguments: ScriptDatum.FromString("Aurora")));
    }

    [Fact]
    public async Task CapturedParameterComparisonReadsTheCurrentUpvalue()
    {
        using var workspace = new TestWorkspace();
        var (_, domain) = await workspace.CompileModuleAsync(
            """
            @module(TEST);
            export func run(value) {
                func overwrite() { value = 2; }
                overwrite();
                return value == 2;
            }
            """);

        ScriptAssert.Equal(
            true,
            TestWorkspace.Execute(domain, "run", arguments: ScriptDatum.FromNumber(1)));
    }

    [Fact]
    public async Task DynamicBitwiseOrKeepsTheEstablishedNullLeftValueSemantics()
    {
        using var workspace = new TestWorkspace();
        var (_, domain) = await workspace.CompileModuleAsync(
            """
            @module(TEST);
            export func run() { return [null | true, null | "Aurora", null | 2]; }
            """);

        ScriptAssert.Equal(
            new object?[] { true, "Aurora", 2 },
            TestWorkspace.Execute(domain, "run"));
    }

    [Fact]
    public async Task NativeCallDoesNotEnterALightweightFunctionFrame()
    {
        using var workspace = new TestWorkspace();
        var (_, domain) = await workspace.CompileModuleAsync(
            """
            @module(TEST);
            native func captureFrame() String { return HOST_FRAME(); }
            export var capturedFrame = captureFrame();
            export func run() { return capturedFrame; }
            """,
            configureGlobal: global =>
            {
                ClrDatumDelegate callback = static (
                    ScriptContext context,
                    ScriptObject receiver,
                    Span<ScriptDatum> arguments,
                    ref ScriptDatum result) =>
                {
                    ScriptDatum.WriteAsString(ref result, context.DirectName ?? string.Empty);
                };
                global.Define("HOST_FRAME", ScriptDatum.FromBonding(callback));
            },
            enableHotReload: false);

        ScriptAssert.Equal(string.Empty, TestWorkspace.Execute(domain, "run"));
    }

    [Fact]
    public async Task ScriptCatchReleasesDirectCallContextBeforeContinuing()
    {
        using var workspace = new TestWorkspace();
        var (_, domain) = await workspace.CompileModuleAsync(
            """
            @module(TEST);
            func fail() {
                throw new Error("boom");
            }
            export func run() {
                try {
                    fail();
                } catch (error) {
                }
                throw new Error("after");
            }
            """);

        var error = Assert.Throws<AuroraRuntimeException>(() => TestWorkspace.Execute(domain, "run"));

        Assert.Contains("after", error.Message, StringComparison.Ordinal);
        Assert.NotNull(error.StackTrace);
        Assert.Contains(error.StackTrace, frame => frame.Method.Contains("run", StringComparison.Ordinal));
        Assert.DoesNotContain(error.StackTrace, frame => frame.Method.Contains("fail", StringComparison.Ordinal));
    }

    [Fact]
    public async Task DirectCallArgumentOptimizationPreservesEvaluationOrder()
    {
        using var workspace = new TestWorkspace();
        var (_, domain) = await workspace.CompileModuleAsync(
            """
            @module(TEST);
            func pair(a, b, c) {
                return [a, b, c];
            }
            export func run() {
                var value = 1;
                func mutate() {
                    value = 2;
                    return 9;
                }
                return pair(value, mutate(), value);
            }
            """);

        ScriptAssert.Equal(new object?[] { 1, 9, 2 }, TestWorkspace.Execute(domain, "run"));
    }

    [Fact]
    public async Task ClosureCapturesLoopAndNestedBlockVariablesWithoutSharingSlots()
    {
        using var workspace = new TestWorkspace();
        var (_, domain) = await workspace.CompileModuleAsync(
            """
            @module(TEST);
            export func run() {
                var callbacks = [];
                for (var i = 0; i < 3; i++) {
                    var captured = i;
                    callbacks.push(() => captured);
                }
                var outer = 10;
                var nested;
                {
                    var inner = 20;
                    nested = () => outer + inner;
                }
                return [callbacks[0](), callbacks[1](), callbacks[2](), nested()];
            }
            """);

        ScriptAssert.Equal(new object?[] { 0, 1, 2, 30 }, TestWorkspace.Execute(domain, "run"));
    }

    [Fact]
    public async Task NestedClosureRemainsCallableAfterCreatingContextWasReturned()
    {
        using var workspace = new TestWorkspace();
        var (_, domain) = await workspace.CompileModuleAsync(
            """
            @module(TEST);
            export func create(start) {
                var value = start;
                return () => { value += 2; return value; };
            }
            """);

        var created = TestWorkspace.Execute(domain, "create", arguments: ScriptDatum.FromNumber(10));
        var callback = Assert.IsType<AuroraScript.Runtime.Types.ClosureFunction>(created.Object);

        ScriptAssert.Equal(12, callback.InvokeClrDetached(AuroraScript.Runtime.Types.ScriptObject.Null));
        ScriptAssert.Equal(14, callback.InvokeClrDetached(AuroraScript.Runtime.Types.ScriptObject.Null));
    }

    [Fact]
    public async Task DeepMemberChainsAndMixedIndexAccessRemainStackBalanced()
    {
        using var workspace = new TestWorkspace();
        var (_, domain) = await workspace.CompileModuleAsync(
            """
            @module(TEST);
            export func run() {
                var root = { a: { b: { c: [{ value: 40 }, { value: 42 }] } } };
                root.a.b.c[0].value += 2;
                return [root.a.b.c[0].value, root['a']['b']['c'][1]['value']];
            }
            """);

        ScriptAssert.Equal(new object?[] { 42, 42 }, TestWorkspace.Execute(domain, "run"));
    }

    [Fact]
    public async Task NumericElementFastPathsPreserveAdditionOrderAndPropertyKeys()
    {
        using var workspace = new TestWorkspace();
        var (_, domain) = await workspace.CompileModuleAsync(
            """
            @module(TEST);
            export func run() {
                var array = [10, 20, 30, 40];
                var map = {};
                map["12"] = 12;
                map["21"] = 21;
                var text = "2";
                return [
                    array[1],
                    array[1 + 1],
                    map[1 + text],
                    map[text + 1]
                ];
            }
            """);

        ScriptAssert.Equal(new object?[] { 20, 30, 12, 21 }, TestWorkspace.Execute(domain, "run"));
    }

    [Fact]
    public void ArraySpreadCopiesOnlyLogicalElementsNotBackingCapacity()
    {
        using var workspace = new TestWorkspace();
        var engine = workspace.CreateEngine();
        var block = engine.CompileBlock(
            "var source = [2, 3]; return [1, ...source, 4];");

        ScriptAssert.Equal(
            new object?[] { 1, 2, 3, 4 },
            block.Invoke(Array.Empty<ScriptDatum>()));
    }

    [Fact]
    public async Task RepeatedReleaseCompilationDoesNotLeakTokenPayloadAcrossSources()
    {
        using var workspace = new TestWorkspace();
        for (var i = 0; i < 64; i++)
        {
            var engine = workspace.CreateEngine();
            var source = workspace.MemorySource(
                $"module-{i}.as",
                $"@module(M{i}); export func value() {{ return 'value-{i}'; }}");
            await engine.BuildAsync(source);
            var domain = engine.CreateDomain();
            ScriptAssert.Equal($"value-{i}", domain.Execute($"M{i}", "value"));
        }
    }

    [Fact]
    public async Task LargeSourceWithCommentsUnicodeAndCrLfCompilesInRelease()
    {
        using var workspace = new TestWorkspace();
        var source = new StringBuilder("@module(TEST);\r\n");
        for (var i = 0; i < 500; i++)
        {
            source.Append("// line ").Append(i).Append("\r\n");
            source.Append("var value").Append(i).Append(" = ").Append(i).Append("; /* block */\r\n");
        }
        source.Append("export func 结果() { return value499 + 1; }");

        var (_, domain) = await workspace.CompileModuleAsync(source.ToString());

        ScriptAssert.Equal(500, TestWorkspace.Execute(domain, "结果"));
    }

    [Fact]
    public async Task ConfusedReleaseModePreservesObservableBehavior()
    {
        using var workspace = new TestWorkspace();
        var (_, domain) = await workspace.CompileModuleAsync(
            "@module(TEST); const secret = 40; export func run() { return secret + 2; }",
            enableConfused: true);

        ScriptAssert.Equal(42, TestWorkspace.Execute(domain, "run"));
    }

    [Fact]
    public async Task EmptyAndMetadataOnlyModulesInitializeSuccessfully()
    {
        using var workspace = new TestWorkspace();
        var engine = workspace.CreateEngine();
        await engine.BuildAsync(
            workspace.MemorySource("empty.as", "@module(EMPTY);"),
            workspace.MemorySource("test.as", "@module(TEST); export func run() { return 42; }"));

        var domain = engine.CreateDomain();
        Assert.NotSame(AuroraScript.Runtime.Types.ScriptObject.Null, domain.GetModule("EMPTY"));
        ScriptAssert.Equal(42, TestWorkspace.Execute(domain, "run"));
    }
}
