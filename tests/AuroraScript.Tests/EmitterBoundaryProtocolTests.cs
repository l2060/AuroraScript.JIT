using AuroraScript.Compiler.Backend.Code;
using AuroraScript.Core;
using AuroraScript.Runtime;
using AuroraScript.Runtime.Types;
using AuroraScript.Tests.Infrastructure;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Xunit;

namespace AuroraScript.Tests;

public sealed class EmitterBoundaryProtocolTests
{
    [Fact]
    public void MetadataKeepsHotWrappersAndUsesDirectFrameSignatures()
    {
        Assert.Equal(typeof(ValueOps), TypedRuntimeMetadata.TryToNumber.DeclaringType);
        Assert.Equal(typeof(ScriptDatum), TypedRuntimeMetadata.TryToNumber.GetParameters()[0].ParameterType);
        Assert.Equal(typeof(IterationOps), TypedRuntimeMetadata.GetEnumerator.DeclaringType);
        Assert.True(TypedRuntimeMetadata.GetEnumerator.IsStatic);
        Assert.Equal(typeof(IterationOps), TypedRuntimeMetadata.MoveNext.DeclaringType);
        Assert.True(TypedRuntimeMetadata.MoveNext.IsStatic);
        Assert.Equal(typeof(ScriptContext), TypedRuntimeMetadata.EnterModuleFrame.DeclaringType);
        Assert.Equal(typeof(ScriptContext), TypedRuntimeMetadata.LeaveFrame.DeclaringType);
        Assert.False(TypedRuntimeMetadata.EnterModuleFrame.IsStatic);
        Assert.False(TypedRuntimeMetadata.LeaveFrame.IsStatic);
        foreach (var name in new[] { "AddStringLeft", "AddStringRight", "AddStringMiddle" })
        {
            Assert.Null(typeof(TypedRuntimeMetadata).GetField(name));
            Assert.NotNull(typeof(ValueOps).GetMethod(name));
        }
        Assert.NotNull(typeof(ValueOps).GetMethod(nameof(ValueOps.TryToNumber)));
        Assert.NotNull(typeof(IterationOps).GetMethod(nameof(IterationOps.GetEnumerator)));
        Assert.NotNull(typeof(IterationOps).GetMethod(nameof(IterationOps.MoveNext)));
        Assert.NotNull(typeof(CallFrameOps).GetMethod(nameof(CallFrameOps.EnterModule)));
        Assert.NotNull(typeof(CallFrameOps).GetMethod(nameof(CallFrameOps.Leave)));
    }

    [Fact]
    public void StringCorePreservesFormattingOrderAndCompatibilityWrapper()
    {
        foreach (var legacy in new[] { false, true })
        {
            var trace = new List<string>();
            var right = new FormattingObject("R", "old", trace);
            var left = new FormattingObject("L", "left", trace) { OnFormat = () => right.Text = "changed" };
            var l = ScriptDatum.FromObject(left);
            var r = ScriptDatum.FromObject(right);
            var text = legacy ? ValueOps.AddStringMiddle(l, "|", r).StringText : ValueOps.ConcatStringMiddle(l, "|", r);
            Assert.Equal("left|changed", text);
            Assert.Equal(new[] { "L", "R" }, trace);
        }
        Assert.Equal("nullTrue", ValueOps.ConcatStringMiddle(ScriptDatum.Null, null!, ScriptDatum.True));
    }

#if NET9_0_OR_GREATER
    [Fact]
    public async Task PersistedIlKeepsHotWrappersAndCallsCoreFramesAndConcat()
    {
        using var workspace = new TestWorkspace();
        await workspace.CompileModuleAsync("""
            @module(TEST);
            var value = 1;
            var prefix = '[' + value;
            var suffix = value + ']';
            var middle = value + ':' + value;
            export func compare(state, Number bound) { return [state.value < bound, bound > state.value]; }
            export func iterate(values) { for (var value in values) { if (value != null) return value; } return null; }
            """, CompilationMode.Persistence);
        var methods = Assembly.Load(File.ReadAllBytes(Path.Combine(workspace.Root, "test-output.dll")))
            .GetTypes().SelectMany(t => t.GetMethods()).ToArray();
        var calls = methods.SelectMany(StringOptimizationTests.GetCalls).ToArray();
        Assert.Contains(calls, m => m.DeclaringType == typeof(ValueOps) && m.Name == nameof(ValueOps.TryToNumber) && !m.GetParameters()[0].ParameterType.IsByRef);
        Assert.Contains(calls, m => m.DeclaringType == typeof(IterationOps) && m.Name == nameof(IterationOps.GetEnumerator));
        Assert.Contains(calls, m => m.DeclaringType == typeof(IterationOps) && m.Name == nameof(IterationOps.MoveNext));
        Assert.Contains(calls, m => m.DeclaringType == typeof(ScriptContext) && m.Name == nameof(ScriptContext.EnterModule));
        Assert.Contains(calls, m => m.DeclaringType == typeof(ScriptContext) && m.Name == nameof(ScriptContext.LeaveFrame));
        Assert.DoesNotContain(calls, m => m.DeclaringType == typeof(CallFrameOps) && m.Name is "EnterModule" or "Leave");
        foreach (var direction in new[] { "Left", "Right", "Middle" })
        {
            Assert.Contains(calls, m => m.DeclaringType == typeof(ValueOps) && m.Name == "ConcatString" + direction);
            Assert.DoesNotContain(calls, m => m.DeclaringType == typeof(ValueOps) && m.Name == "AddString" + direction);
        }
    }

#endif

    [Theory]
    [InlineData(CompilationMode.Dynamic)]
    [InlineData(CompilationMode.OnlyRun)]
#if NET9_0_OR_GREATER
    [InlineData(CompilationMode.Persistence)]
#endif
    public async Task ComparisonEvaluationAndNumericCacheUpdatesStayOrdered(CompilationMode mode)
    {
        using var workspace = new TestWorkspace();
        var (_, domain) = await workspace.CompileModuleAsync("""
            @module(TEST);
            var trace = '';
            func left(value) { trace += 'L'; return value; }
            native func right(Number value) Number { trace += 'R'; return value; }
            export func compare(value) {
                trace = '';
                var a = left(value) < right(2);
                var first = trace;
                trace = '';
                var b = right(2) > left(value);
                return [a, b, first, trace];
            }
            export func cached(value) {
                var result = [];
                for (var i = 0; i < 4; i++) {
                    result.push(value * 2, value < 2);
                    if (i == 0) value = '1';
                    if (i == 1) value = null;
                    if (i == 2) value = 'bad';
                }
                return result;
            }
            """, mode);
        foreach (var value in new[] { ScriptDatum.FromNumber(1), ScriptDatum.FromString("1"),
            ScriptDatum.FromNumber(3), ScriptDatum.FromString("bad"), ScriptDatum.Null, ScriptDatum.NaN })
        {
            var expected = ScriptDatum.TryToNumber(in value, out var number) && number < 2;
            ScriptAssert.Equal(new object[] { expected, expected, "LR", "RL" },
                TestWorkspace.Execute(domain, "compare", arguments: [value]));
        }
        ScriptAssert.Equal(new object[] { 6, false, 2, true, 0, false, double.NaN, false },
            TestWorkspace.Execute(domain, "cached", arguments: [ScriptDatum.FromNumber(3)]));
    }

    [Theory]
    [InlineData(CompilationMode.Dynamic)]
    [InlineData(CompilationMode.OnlyRun)]
#if NET9_0_OR_GREATER
    [InlineData(CompilationMode.Persistence)]
#endif
    public async Task IterationRetainsVirtualDispatchAndFinallyTransfers(CompilationMode mode)
    {
        using var workspace = new TestWorkspace();
        var (_, domain) = await workspace.CompileModuleAsync("""
            @module(TEST);
            export func run(values) {
                var result = [];
                for (var value in values) {
                    try {
                        if (value == 2) continue;
                        if (value == 4) break;
                        result.push(value);
                    } finally { result.push('f'); }
                }
                return result;
            }
            """, mode);
        var array = new ScriptArray(new ScriptDatum[] { 1, 2, 3, 4, 5 });
        var packed = new ScriptInt32Array(5);
        for (var i = 0; i < packed.Length; i++) packed.SetElement(i, i + 1);
        var custom = new CustomEnumerable();
        foreach (var input in new[] { ScriptDatum.FromArray(array), ScriptDatum.FromObject(packed), ScriptDatum.FromObject(custom) })
            ScriptAssert.Equal(new object[] { 1, "f", "f", 3, "f", "f" }, TestWorkspace.Execute(domain, "run", arguments: [input]));
        Assert.Equal(1, custom.Calls);
        ScriptAssert.Equal(new object[] { "1", "f", "f", "3", "f" },
            TestWorkspace.Execute(domain, "run", arguments: [ScriptDatum.FromString("123")]));
        ScriptAssert.Equal(Array.Empty<object>(), TestWorkspace.Execute(domain, "run", arguments: [ScriptDatum.Null]));
    }

    [Theory]
    [InlineData(CompilationMode.Dynamic)]
    [InlineData(CompilationMode.OnlyRun)]
#if NET9_0_OR_GREATER
    [InlineData(CompilationMode.Persistence)]
#endif
    public async Task ModuleConcatenationAndPatchFramesPreserveResults(CompilationMode mode)
    {
        using var workspace = new TestWorkspace();
        var (_, domain) = await workspace.CompileModuleAsync("""
            @module(TEST);
            var trace = '';
            func mark(value) { trace += value; return value; }
            var left = '[' + mark('L');
            var right = mark('R') + ']';
            var middle = mark('A') + ':' + mark('B');
            export func read() { return [left, right, middle, trace]; }
            """, mode, enableHotReload: true);
        ScriptAssert.Equal(new object[] { "[L", "R]", "A:B", "LRAB" }, TestWorkspace.Execute(domain, "read"));
        domain.DynamicPatch(workspace.MemorySource("main.as", """
            @module(TEST);
            var trace = '';
            var middle = mark('P') + '-' + mark('Q');
            """), HotPatchType.Incremental);
        ScriptAssert.Equal(new object[] { "[L", "R]", "P-Q", "PQ" }, TestWorkspace.Execute(domain, "read"));
    }

#if NET9_0_OR_GREATER
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task PersistedInitializerRestoresTheCallerFrame(bool fail)
    {
        using var workspace = new TestWorkspace();
        var output = Path.Combine(workspace.Root, "frame.dll");
        var engine = workspace.CreateEngine(CompilationMode.Persistence, assemblyOut: output);
        workspace.WriteSource("main.as", "@module(TEST); var initialized = hook();");
        await engine.BuildAsync(["main.as"]);
        using var domain = engine.CreateEmptyDomain(null);
        var marker = new ScriptObject();
        var context = new ScriptContext(domain, marker) { Location = 123 };
        var called = false;
        domain.Global.Define("hook", ScriptDatum.FromBonding((ScriptContext active, ScriptObject self, Span<ScriptDatum> args, ref ScriptDatum result) =>
        {
            called = true;
            Assert.Same(context, active);
            Assert.Equal("TEST", active.Module.Name);
            active.UserState = new ScriptObject();
            active.With(module: null);
            if (fail) throw new AuroraRuntimeException("initialization failure");
            result = ScriptDatum.True;
        }));
        var entry = Assembly.Load(File.ReadAllBytes(output)).GetType("AuroraScriptInitializer")!
            .GetMethod("InitializeDomain")!.CreateDelegate<ScriptFunctionDelegate>();
        if (fail) Assert.Throws<AuroraRuntimeException>(() => entry(context, Span<ScriptDatum>.Empty));
        else entry(context, Span<ScriptDatum>.Empty);
        Assert.True(called);
        Assert.Null(context.Module);
        Assert.Null(context.Target);
        Assert.Null(context.Next);
        Assert.Equal(123, context.Location);
        Assert.Same(marker, context.UserState);
    }
#endif

    private sealed class FormattingObject(string name, string text, List<string> trace) : ScriptObject
    {
        public string Text = text;
        public Action? OnFormat;
        public override string ToString()
        {
            trace.Add(name);
            OnFormat?.Invoke();
            return Text;
        }
    }

    private sealed class CustomEnumerable : ScriptObject
    {
        public int Calls;
        public override ScriptEnumerator GetEnumerator()
        {
            Calls++;
            return new ScriptEnumerator(new ScriptDatum[] { 1, 2, 3, 4, 5 });
        }
    }
}
