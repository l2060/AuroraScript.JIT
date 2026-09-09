using AuroraScript.Runtime.Builtin;
using AuroraScript.Runtime;
using AuroraScript.Runtime.Types;
using AuroraScript.Tests.Infrastructure;
using AuroraScript.Tests.Host;
using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Xunit;

namespace AuroraScript.Tests;

public sealed class DynamicFunctionReturnInferenceTests
{
    [Theory]
    [InlineData(CompilationMode.Dynamic)]
#if NET9_0_OR_GREATER
    [InlineData(CompilationMode.Persistence)]
#endif
    public async Task NonNumericElementAccessDoesNotGuardUnchangedDynamicHelpers(CompilationMode mode)
    {
        using var workspace = new TestWorkspace();
        var (_, domain) = await workspace.CompileModuleAsync("""
            @module(TEST);
            var valueCalls = 0;
            var keyCalls = 0;
            func values() { valueCalls++; return new Int32Array(3); }
            func key() { keyCalls++; return 'length'; }
            export func read() { return values()['length']; }
            export func missingProperty() { return key().missing; }
            export func readKey(object) { return object[key()]; }
            export func write(object) { object[key()] = 3; return object[key()]; }
            export func writeProperty(object) { object.length = key(); return object.length; }
            export func counts() { return [valueCalls, keyCalls]; }
            """, mode);
        using (domain)
        {
            var value = new ScriptObject();
            value.Define("length", ScriptDatum.FromNumber(7));
            ScriptAssert.Equal(3, TestWorkspace.Execute(domain, "read"));
            ScriptAssert.Equal(7, TestWorkspace.Execute(domain, "readKey", arguments: [ScriptDatum.FromObject(value)]));
            ScriptAssert.Equal(3, TestWorkspace.Execute(domain, "write", arguments: [ScriptDatum.FromObject(value)]));
            ScriptAssert.Equal(null, TestWorkspace.Execute(domain, "missingProperty"));
            ScriptAssert.Equal("length", TestWorkspace.Execute(domain, "writeProperty", arguments: [ScriptDatum.FromObject(value)]));
            ScriptAssert.Equal(new object[] { 1, 5 }, TestWorkspace.Execute(domain, "counts"));
        }
#if NET9_0_OR_GREATER
        if (mode == CompilationMode.Persistence)
        {
            var methods = Assembly.Load(File.ReadAllBytes(Path.Combine(workspace.Root, "test-output.dll")))
                .GetTypes().SelectMany(type => type.GetMethods());
            foreach (var method in methods.Where(method => method.Name is "read$typed" or "readKey$typed" or "write$typed" or "missingProperty$typed" or "writeProperty$typed"))
            {
                var calls = StringOptimizationTests.GetCalls(method);
                Assert.DoesNotContain(calls, call => call.DeclaringType == typeof(ScriptDatum) && call.Name == "get_Kind");
                Assert.DoesNotContain(calls, call => call.DeclaringType?.Name == "PackedArrayBoundaryOps");
            }
        }
#endif
    }

    [Theory]
    [InlineData(CompilationMode.Dynamic)]
#if NET9_0_OR_GREATER
    [InlineData(CompilationMode.Persistence)]
#endif
    public async Task GuardedOperationsAgreeWithDynamicOperationsForReplacementValues(CompilationMode mode)
    {
        using var workspace = new TestWorkspace();
        var operations = new[] { "x + 1", "x - 1", "x * 2", "x == 1", "x < 2", "x | 0", "Math.pow(x, 2)" };
        var script = new System.Text.StringBuilder("@module(TEST); func source() Number { return 1; } export func replace(value) { source = () => value; } ");
        for (var i = 0; i < operations.Length; i++)
        {
            script.Append($"export func baseline{i}(x) {{ return {operations[i]}; }} ");
            script.Append($"export func guarded{i}() {{ const x = source(); return {operations[i]}; }} ");
        }
        var (_, domain) = await workspace.CompileModuleAsync(script.ToString(), mode);
        using (domain)
        {
            var values = new[] { ScriptDatum.FromNumber(3), ScriptDatum.FromNumber(-0d),
                ScriptDatum.FromNumber(double.NaN), ScriptDatum.FromString("3"),
                ScriptDatum.FromString("text"), ScriptDatum.True, ScriptDatum.Null,
                ScriptDatum.FromInt64(9_007_199_254_740_993L), ScriptDatum.FromUInt64(ulong.MaxValue) };
            foreach (var value in values)
            {
                TestWorkspace.Execute(domain, "replace", arguments: [value]);
                for (var i = 0; i < operations.Length; i++)
                {
                    ScriptDatum expected = default, actual = default;
                    var expectedError = Record.Exception(() => expected = TestWorkspace.Execute(domain, $"baseline{i}", arguments: [value]));
                    var actualError = Record.Exception(() => actual = TestWorkspace.Execute(domain, $"guarded{i}"));
                    Assert.Equal(expectedError?.GetType(), actualError?.GetType());
                    if (expectedError != null) continue;
                    Assert.Equal(expected.Kind, actual.Kind);
                    if (expected.Kind == ValueKind.Number && double.IsNaN(expected.Number))
                        Assert.True(double.IsNaN(actual.Number));
                    else Assert.Equal(expected, actual);
                }
            }
        }
    }

    [Theory]
    [InlineData(CompilationMode.Dynamic)]
    [InlineData(CompilationMode.OnlyRun)]
#if NET9_0_OR_GREATER
    [InlineData(CompilationMode.Persistence)]
#endif
    public async Task NativeObjectsArraysAndNativeCalleesConsumeOrdinaryReturns(CompilationMode mode)
    {
        using var workspace = new TestWorkspace();
        var (_, domain) = await workspace.CompileModuleAsync("""
            @module(TEST);
            func vector() { return new Vec2(3, 4); }
            func values() { return new Float64Array(2); }
            func scalar() Number { return 6; }
            func label() { return 'seed'; }
            native func twice(Number input) Number { return input * 2; }
            export func run() {
                const v = vector();
                v.x = scalar();
                const items = values();
                items[0] = scalar();
                const created = new Vec2(scalar(), 8);
                var current = new Vec2(3, 4);
                const sum = current.add((current = new Vec2(30, 40), vector()));
                return [v.x, v.y, items[0], items.length, twice(scalar()),
                    label().toUpperCase(), created.length(), sum.x, sum.y, current.x];
            }
            """, mode, nativeTypes: true);
        using (domain)
            ScriptAssert.Equal(new object[] { 6, 4, 6, 2, 12, "SEED", 10, 6, 8, 30 }, TestWorkspace.Execute(domain, "run"));
#if NET9_0_OR_GREATER
        if (mode == CompilationMode.Persistence)
        {
            var methods = Assembly.Load(File.ReadAllBytes(Path.Combine(workspace.Root, "test-output.dll")))
                .GetTypes().SelectMany(type => type.GetMethods());
            var run = methods.Single(method => method.Name == "run$typed");
            var calls = StringOptimizationTests.GetCalls(run);
            Assert.Contains(calls, call => call.Name == "twice$native");
            Assert.Contains(calls, call => call.DeclaringType == typeof(Vec2) && call.Name == nameof(Vec2.LengthCore));
        }
#endif
    }

    [Theory]
    [InlineData(CompilationMode.Dynamic)]
#if NET9_0_OR_GREATER
    [InlineData(CompilationMode.Persistence)]
#endif
    public async Task HostReplacementAndShortCircuitingPreserveEvaluationOrder(CompilationMode mode)
    {
        using var workspace = new TestWorkspace();
        var (_, domain) = await workspace.CompileModuleAsync("""
            @module(TEST);
            var calls = 0;
            func value(unused) Number { calls++; return 4; }
            func replacement() { calls++; return 'host'; }
            export func run() {
                const before = value(change(replacement)) + 1;
                const after = value() + 2;
                const skipped = true || value();
                return [before, after, calls, skipped];
            }
            """, mode, configureGlobal: global => global.Define("change", ScriptDatum.FromBonding(
                (ScriptContext context, ScriptObject thisObject, Span<ScriptDatum> arguments, ref ScriptDatum result) =>
                {
                    context.Module.SetPropertyDatum(context, "value", arguments[0]);
                    result = ScriptDatum.Null;
                }), false, false));
        using (domain)
            ScriptAssert.Equal(new object[] { 5, "host2", 2, true }, TestWorkspace.Execute(domain, "run"));
    }

    [Theory]
    [InlineData(CompilationMode.Dynamic)]
#if NET9_0_OR_GREATER
    [InlineData(CompilationMode.Persistence)]
#endif
    public async Task ObjectAndArrayGuardsFallBackAfterTypeChangingReplacement(CompilationMode mode)
    {
        using var workspace = new TestWorkspace();
        var (_, domain) = await workspace.CompileModuleAsync("""
            @module(TEST);
            func vector() { return new Vec2(3, 4); }
            func values() { return new Float64Array(2); }
            export func run() {
                vector = () => ({ x: 'changed' });
                values = () => ['replacement'];
                const v = vector();
                const items = values();
                return [v.x + 1, items[0] + '!', items.length];
            }
            """, mode, nativeTypes: true);
        using (domain)
            ScriptAssert.Equal(new object[] { "changed1", "replacement!", 1 }, TestWorkspace.Execute(domain, "run"));
    }

    [Theory]
    [InlineData(CompilationMode.Dynamic)]
#if NET9_0_OR_GREATER
    [InlineData(CompilationMode.Persistence)]
#endif
    public async Task ImportedInferredReturnChainsReachConsumers(CompilationMode mode)
    {
        using var workspace = new TestWorkspace();
        workspace.WriteSource("leaf.as", """
            @module(LEAF);
            export func text() { return 'imported'; }
            """);
        workspace.WriteSource("lib.as", """
            @module(LIB);
            import leaf from 'leaf.as';
            export func text() { return leaf.text(); }
            """);
        workspace.WriteSource("main.as", """
            @module(TEST);
            import lib from 'lib.as';
            const initial = lib.text().length;
            export func run() { const alias = lib.text; return [initial, alias().length]; }
            """);
        var output = Path.Combine(workspace.Root, "imports.dll");
        var engine = workspace.CreateEngine(mode, assemblyOut: output);
        await engine.BuildAsync("main.as");
        using var domain = engine.CreateDomain();
        ScriptAssert.Equal(new object[] { 8, 8 }, TestWorkspace.Execute(domain, "run"));
#if NET9_0_OR_GREATER
        if (mode == CompilationMode.Persistence)
        {
            var methods = Assembly.Load(File.ReadAllBytes(output)).GetTypes().SelectMany(type => type.GetMethods());
            var run = methods.Single(method => method.Name == "run$typed");
            Assert.Contains(StringOptimizationTests.GetCalls(run),
                call => call.DeclaringType == typeof(ScriptDatum) && call.Name == "get_Kind");
        }
#endif
    }

    [Theory]
    [InlineData(CompilationMode.Dynamic)]
#if NET9_0_OR_GREATER
    [InlineData(CompilationMode.Persistence)]
#endif
    public async Task ImplicitReturnsHonorAnnotationsAndFinally(CompilationMode mode)
    {
        using var workspace = new TestWorkspace();
        var (_, domain) = await workspace.CompileModuleAsync("""
            @module(TEST);
            export func missing() Number { }
            export func partial(flag) Number { if (flag) return 2; }
            export func final() Number { try { } finally { return 7; } }
            export func inferred(flag) { if (flag) return 2; }
            """, mode);
        using (domain)
        {
            Assert.Throws<AuroraRuntimeException>(() => TestWorkspace.Execute(domain, "missing"));
            Assert.Throws<AuroraRuntimeException>(() => TestWorkspace.Execute(domain, "partial"));
            ScriptAssert.Equal(2, TestWorkspace.Execute(domain, "partial", arguments: [ScriptDatum.True]));
            ScriptAssert.Equal(7, TestWorkspace.Execute(domain, "final"));
            Assert.Equal(ValueKind.Null, TestWorkspace.Execute(domain, "inferred").Kind);
        }
    }

    [Theory]
    [InlineData(CompilationMode.Dynamic)]
    [InlineData(CompilationMode.OnlyRun)]
#if NET9_0_OR_GREATER
    [InlineData(CompilationMode.Persistence)]
#endif
    public async Task OrdinaryReturnsReachInitializerAndFunctionConsumers(CompilationMode mode)
    {
        using var workspace = new TestWorkspace();
        var (_, domain) = await workspace.CompileModuleAsync("""
            @module(TEST);
            func number() Number { return 5; }
            func text() String { return 'seed'; }
            const initial = Math.pow(number(), 2);
            export func run() {
                const value = number();
                const label = text();
                return [initial, value + 2, Math.pow(value, 2), label.length, label + '!'];
            }
            """, mode);
        using (domain)
            ScriptAssert.Equal(new object[] { 25, 7, 25, 4, "seed!" }, TestWorkspace.Execute(domain, "run"));
#if NET9_0_OR_GREATER
        if (mode == CompilationMode.Persistence)
        {
            var methods = Assembly.Load(File.ReadAllBytes(Path.Combine(workspace.Root, "test-output.dll")))
                .GetTypes().SelectMany(type => type.GetMethods()).ToArray();
            foreach (var methodName in new[] { "Initialize", "run$typed" })
            {
                var method = Assert.Single(methods, method => method.Name == methodName);
                var calls = StringOptimizationTests.GetCalls(method);
                Assert.Contains(calls, call => call.DeclaringType == typeof(MathSupport) && call.Name == nameof(MathSupport.PowCore));
                Assert.Contains(calls, call => call.DeclaringType == typeof(ScriptDatum) && call.Name == "get_Kind");
            }
            Assert.DoesNotContain(methods, method => method.Name is "number$native" or "text$native");
            foreach (var methodName in new[] { "number$typed", "text$typed" })
                Assert.Equal(typeof(ScriptDatum), Assert.Single(methods, method => method.Name == methodName).ReturnType);
        }
#endif
    }

    [Theory]
    [InlineData(CompilationMode.Dynamic)]
    [InlineData(CompilationMode.OnlyRun)]
#if NET9_0_OR_GREATER
    [InlineData(CompilationMode.Persistence)]
#endif
    public async Task RebindingAndArgumentEffectsUseActualResultsOnce(CompilationMode mode)
    {
        using var workspace = new TestWorkspace();
        var (_, domain) = await workspace.CompileModuleAsync("""
            @module(TEST);
            var count = 0;
            func value() Number { count++; return 4; }
            func replace() { value = () => { count++; return 'changed'; }; return 1; }
            export func run() {
                const before = value() + replace();
                const after = value() + 1;
                const alias = value;
                const saved = alias();
                return [before, after, saved + 2, count];
            }
            """, mode);
        using (domain)
            ScriptAssert.Equal(new object[] { 5, "changed1", "changed2", 3 }, TestWorkspace.Execute(domain, "run"));
    }

    [Theory]
    [InlineData(CompilationMode.Dynamic)]
#if NET9_0_OR_GREATER
    [InlineData(CompilationMode.Persistence)]
#endif
    public async Task InferredChainsRecursionAndLocalAliasesRemainDynamicCallables(CompilationMode mode)
    {
        using var workspace = new TestWorkspace();
        var (_, domain) = await workspace.CompileModuleAsync("""
            @module(TEST);
            func leaf() { return 'abc'; }
            func chain() { return leaf(); }
            func recurse(Number depth) { if (depth <= 0) return 2; return recurse(depth - 1); }
            export func run() {
                const alias = chain;
                func local() { return alias(); }
                const length = local().length;
                return [length, Math.pow(recurse(5), 3)];
            }
            """, mode);
        using (domain)
            ScriptAssert.Equal(new object[] { 3, 8 }, TestWorkspace.Execute(domain, "run"));
    }
}
