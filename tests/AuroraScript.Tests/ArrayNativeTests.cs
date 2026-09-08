using AuroraScript.Compiler.Backend;
using AuroraScript.Hosting;
using AuroraScript.Runtime;
using AuroraScript.Runtime.Types;
using AuroraScript.Tests.Infrastructure;
using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Xunit;

namespace AuroraScript.Tests;

public sealed class ArrayNativeTests
{
    [Fact]
    public void NativeTypeRegistrationUsesGeneratedMembersAndPrototype()
    {
        var catalog = new HostExportCatalog([]);
        Assert.True(catalog.TryGetNativeObject("Array", out var owner));
        Assert.Null(owner.Constructor);
        Assert.True(owner.TryGetGetter("length", out var getter));
        Assert.Equal(AuroraExportValueKind.Int32, getter.ReturnKind);
        Assert.Equal(typeof(ScriptArray).GetProperty(nameof(ScriptArray.Length))!.GetMethod, getter.Method);
        Assert.True(catalog.TryGetGlobal("Array", "withCapacity", out var factory));
        Assert.Equal(typeof(int), factory.GetScriptParameterType(0));
        Assert.Same(ScriptArray.NativePrototype, new ScriptArray().Prototype);
        Assert.True(ScriptArray.Type.IsFrozen);
        Assert.True(owner.TryGetSetter("length", out _));
        foreach (var name in new[] { "push", "pop", "shift", "unshift", "reverse", "sort", "join", "slice",
            "concat", "has", "indexOf", "lastIndexOf", "find", "findIndex", "findLast", "findLastIndex",
            "map", "filter", "some", "every", "flat", "reduce" })
            Assert.True(owner.TryGetMethod(name, out _), name);
    }

    [Theory]
    [InlineData(CompilationMode.Dynamic)]
    [InlineData(CompilationMode.OnlyRun)]
#if NET9_0_OR_GREATER
    [InlineData(CompilationMode.Persistence)]
#endif
    public async Task NativeArrayPathsPreserveCapacityOverridesEvaluationAndAbi(CompilationMode mode)
    {
        using var workspace = new TestWorkspace();
        var (_, domain) = await workspace.CompileModuleAsync("""
            @module(TEST);
            export native func capacity(int32 value) Array { return Array.withCapacity(value); }
            export native func numberCapacity(Number value) Array { return Array.withCapacity(value); }
            export func generic(value) { return Array.withCapacity(value); }
            export func dynamic(value) { var type=Array; return type.withCapacity(value); }
            export native func length(Array value) int32 { return value.length; }
            export native func resize(Array value, int32 count) int32 { value.length=count; return value.length; }
            export func staticMembers() {
                return [Array.from([1,2], x=>x+1), Array.from(), Array.of(3,4), Array.of().length,
                    Array.isArray([]), Array.isArray(1), Array.isArray(),
                    (new Array(8)).length, Array.withCapacity(8).length, (new Array(8,9)).length];
            }
            export func order() {
                var log='';
                func capacity() { log+='c'; return 8; }
                func extra() { log+='e'; return 1; }
                var value=Array.withCapacity(capacity(),extra());
                return [log,value.length];
            }
            export func overrides() {
                var value=[];
                func argument() { value.push=x=>'own'; return 1; }
                return [value.push(argument()), value.length];
            }
            export func shadow() {
                var Array={withCapacity: x=>x+1};
                return Array.withCapacity(3);
            }
            export func aliases() {
                var type=Array;
                return [(new type(8)).length, type.withCapacity(...[8]).length];
            }
            export func forbidden() { return Array(8); }
            """, mode);
        ScriptAssert.Equal(new object?[] { new object[] { 2,3 }, null, new object[] { 3,4 }, 0, true, false, false, 8, 0, 0 }, TestWorkspace.Execute(domain, "staticMembers"));
        ScriptAssert.Equal(new object[] { "ce", 0 }, TestWorkspace.Execute(domain, "order"));
        ScriptAssert.Equal(new object[] { "own", 0 }, TestWorkspace.Execute(domain, "overrides"));
        ScriptAssert.Equal(4, TestWorkspace.Execute(domain, "shadow"));
        ScriptAssert.Equal(new object[] { 8, 0 }, TestWorkspace.Execute(domain, "aliases"));
        Assert.Throws<AuroraRuntimeException>(() => TestWorkspace.Execute(domain, "forbidden"));
        foreach (var value in new[] { ScriptDatum.FromNumber(8), ScriptDatum.FromNumber(0), ScriptDatum.FromNumber(-2),
            ScriptDatum.FromNumber(2.75), ScriptDatum.FromNumber(double.NaN),
            ScriptDatum.FromString("8"), ScriptDatum.FromString("bad"), ScriptDatum.FromBoolean(true),
            ScriptDatum.FromBoolean(false), ScriptDatum.FromInt64(8), ScriptDatum.FromUInt64(8), default(ScriptDatum) })
        {
            var generic = Assert.IsType<ScriptArray>(TestWorkspace.Execute(domain, "generic", arguments: [value]).Object);
            var dynamic = Assert.IsType<ScriptArray>(TestWorkspace.Execute(domain, "dynamic", arguments: [value]).Object);
            Assert.Equal(0, generic.Length);
            Assert.Equal(generic.Length, dynamic.Length);
            Assert.Equal(generic._items.Length, dynamic._items.Length);
        }
        foreach (var method in new[] { "generic", "dynamic", "numberCapacity" })
            Assert.Throws<AuroraRuntimeException>(() => TestWorkspace.Execute(domain, method,
                arguments: [ScriptDatum.FromNumber(double.PositiveInfinity)]));
        foreach (var value in new[] { -2, 0, 1, 8 })
        {
            var array = Assert.IsType<ScriptArray>(TestWorkspace.Execute(domain, "capacity", arguments: [ScriptDatum.FromNumber(value)]).Object);
            Assert.Equal(0, array.Length);
            Assert.Equal(value <= 0 ? 0 : Math.Max(4, value), array._items.Length);
            var argument = ScriptDatum.FromArray(array);
            ScriptAssert.Equal(0, TestWorkspace.Execute(domain, "length", arguments: [argument]));
            ScriptAssert.Equal(3, TestWorkspace.Execute(domain, "resize", arguments: [argument, ScriptDatum.FromNumber(3)]));
            Assert.Equal(3, array.Length);
        }
#if NET9_0_OR_GREATER
        if (mode == CompilationMode.Persistence)
        {
            var methods = Assembly.Load(File.ReadAllBytes(Path.Combine(workspace.Root, "test-output.dll")))
                .GetTypes().SelectMany(type => type.GetMethods()).ToArray();
            var length = methods.Single(method => method.Name == "length$native");
            var lengthCalls = StringOptimizationTests.GetCalls(length);
            Assert.Contains(lengthCalls, method => method.DeclaringType == typeof(ScriptArray) && method.Name == "get_Length");
            Assert.DoesNotContain(lengthCalls, method => method.DeclaringType == typeof(ScriptDatum));
            var capacity = methods.Single(method => method.Name == "capacity$native");
            Assert.Equal(typeof(ScriptArray), capacity.ReturnType);
            var capacityCalls = StringOptimizationTests.GetCalls(capacity);
            Assert.Contains(capacityCalls, method => method.DeclaringType == typeof(ScriptArray) &&
                method.Name == nameof(ScriptArray.CreateEmptyWithCapacity) && method.GetParameters()[0].ParameterType == typeof(int));
            Assert.DoesNotContain(capacityCalls, method => method.DeclaringType == typeof(ScriptDatum));
            var numberCalls = StringOptimizationTests.GetCalls(methods.Single(method => method.Name == "numberCapacity$native"));
            Assert.Contains(numberCalls, method => method.DeclaringType == typeof(ScriptArray) &&
                method.Name == nameof(ScriptArray.CreateEmptyWithCapacity) && method.GetParameters()[0].ParameterType == typeof(double));
            Assert.DoesNotContain(numberCalls, method => method.DeclaringType == typeof(ScriptDatum));
        }
#endif
    }
    [Theory]
    [InlineData(CompilationMode.Dynamic)]
    [InlineData(CompilationMode.OnlyRun)]
#if NET9_0_OR_GREATER
    [InlineData(CompilationMode.Persistence)]
#endif
    public async Task NativeArrayReferencesUseTheCommonIndexerWithoutDatumReceiverConversions(CompilationMode mode)
    {
        using var workspace = new TestWorkspace();
        var (_, domain) = await workspace.CompileModuleAsync("""
            @module(TEST);
            export native func work(Array array) void {
                for (var i = 0; i <= 10; i++) array.push(i);
                var len = array.length;
                for (var n = 0; n < len; n++) array[n] = array[n];
            }
            export native func create() Array {
                var array = [1, 2, 3, 4];
                work(array);
                return array;
            }
            export native func read(Array array, int32 index) Number { return array[index]; }
            export native func write(Array array, int32 index, value) void { array[index] = value; }
            export native func mutate(Array array, int32 index) Array {
                var old = array[index]++;
                array[index] += 3;
                return [old, ++array[index]];
            }
            export func dynamicRead(array, index) { return array[index]; }
            export func dynamicPush(array) { return array.push(6); }
            export native func nativePush(Array array) int32 { return array.push(6); }
            export func order() {
                var values = [1];
                var trace = '';
                func index() { trace += 'i'; return 0; }
                func value() { trace += 'v'; return 2; }
                values[index()] += value();
                return [trace, values[0]];
            }
            """, mode);
        var array = TestWorkspace.Execute(domain, "create");
        Assert.Equal(15, ((ScriptArray)array.Object).Length);
        ScriptAssert.Equal(10, TestWorkspace.Execute(domain, "read", arguments: [array, ScriptDatum.FromNumber(-1)]));
        ScriptAssert.Equal(null, TestWorkspace.Execute(domain, "dynamicRead", arguments: [array, ScriptDatum.FromNumber(15)]));
        ScriptAssert.Equal(null, TestWorkspace.Execute(domain, "write", arguments: [array, ScriptDatum.FromNumber(17), ScriptDatum.FromString("tail")]));
        ScriptAssert.Equal(null, TestWorkspace.Execute(domain, "dynamicRead", arguments: [array, ScriptDatum.FromNumber(16)]));
        ScriptAssert.Equal("tail", TestWorkspace.Execute(domain, "dynamicRead", arguments: [array, ScriptDatum.FromString("17")]));
        ScriptAssert.Equal(new object[] { 1, 6 }, TestWorkspace.Execute(domain, "mutate", arguments: [array, ScriptDatum.FromNumber(0)]));
        ScriptAssert.Equal(new object[] { "iv", 3 }, TestWorkspace.Execute(domain, "order"));
        Assert.Throws<AuroraRuntimeException>(() => TestWorkspace.Execute(domain, "read", arguments: [ScriptDatum.FromNumber(1), ScriptDatum.FromNumber(0)]));
        var overridden = new ScriptArray();
        overridden.Define("push", ScriptDatum.FromBonding((ScriptContext ctx, ScriptObject self, Span<ScriptDatum> args, ref ScriptDatum result) =>
            result = ScriptDatum.FromNumber(60)));
        var overriddenDatum = ScriptDatum.FromArray(overridden);
        ScriptAssert.Equal(60, TestWorkspace.Execute(domain, "dynamicPush", arguments: [overriddenDatum]));
        ScriptAssert.Equal(1, TestWorkspace.Execute(domain, "nativePush", arguments: [overriddenDatum]));
#if NET9_0_OR_GREATER
        if (mode == CompilationMode.Persistence)
        {
            var methods = Assembly.Load(File.ReadAllBytes(Path.Combine(workspace.Root, "test-output.dll")))
                .GetTypes().SelectMany(type => type.GetMethods()).ToArray();
            foreach (var name in new[] { "work", "read", "write", "mutate" })
            {
                var method = methods.Single(method => method.Name == name + "$native");
                Assert.Equal(typeof(ScriptArray), method.GetParameters()[1].ParameterType);
                var calls = StringOptimizationTests.GetCalls(method);
                Assert.DoesNotContain(calls, call => call.DeclaringType == typeof(ScriptDatum) &&
                    (call.Name == nameof(ScriptDatum.ToObject) || name != "mutate" && call.Name == nameof(ScriptDatum.FromObject)));
                Assert.DoesNotContain(calls, call => call.DeclaringType == typeof(ObjectOps) && call.Name.StartsWith("GetElement"));
                Assert.Contains(calls, call => call.DeclaringType == typeof(ScriptArray) &&
                    call.Name == (name == "write" ? "set_Item" : "get_Item"));
            }
            var workCalls = StringOptimizationTests.GetCalls(methods.Single(method => method.Name == "work$native"));
            Assert.Contains(workCalls, call => call.DeclaringType == typeof(ScriptArray) && call.Name == nameof(ScriptArray.PushCore));
            Assert.DoesNotContain(workCalls, call => call.Name.Contains("InvokeProperty") || call.Name == "HasOwnPushProperty");
            Assert.Equal(typeof(ScriptArray), methods.Single(method => method.Name == "create$native").ReturnType);
        }
#endif
    }

    [Theory]
    [InlineData(CompilationMode.Dynamic)]
    [InlineData(CompilationMode.OnlyRun)]
#if NET9_0_OR_GREATER
    [InlineData(CompilationMode.Persistence)]
#endif
    public async Task NativeArrayMembersMatchDynamicAdapters(CompilationMode mode)
    {
        using var workspace = new TestWorkspace();
        // The same operations run once with a proven receiver and once through the dynamic protocol.
        const string operations = """
            var result = [array.push(), array.push(3), array.push(4, 5), array.unshift(-1, 0),
                array.shift(), array.pop(), array.has(3), array.indexOf(3), array.lastIndexOf(3),
                array.join(), array.join(null), array.join(7), array.slice().length,
                array.slice(-2).length, array.slice(0, 1).length];
            result.push(array.reverse().join(','), array.sort().join(','), array.concat([9], 10).length);
            result.push(array.map(x => x + 1).join(','), array.filter(x => x > 1).length,
                array.find(x => x > 1), array.findIndex(x => x > 1), array.findLast(x => x > 1),
                array.findLastIndex(x => x > 1), array.some(x => x > 1), array.every(x => x >= 0),
                array.flat().length, array.reduce((a, b) => a + b));
            return result;
            """;
        var (_, domain) = await workspace.CompileModuleAsync("@module(TEST); export func known(Array array) {" + operations +
            "} export func dynamic(array) {" + operations + "}", mode);
        var known = TestWorkspace.Execute(domain, "known", arguments: [ScriptDatum.FromArray(new ScriptArray(new ScriptDatum[] { 1, 2 }))]);
        var dynamic = TestWorkspace.Execute(domain, "dynamic", arguments: [ScriptDatum.FromArray(new ScriptArray(new ScriptDatum[] { 1, 2 }))]);
        Assert.Equal(known.ToString(), dynamic.ToString());
    }

#if NET9_0_OR_GREATER
    [Fact]
    public async Task ImportedNativeReferencesAndHostIndexersUseTheSameProtocol()
    {
        using var workspace = new TestWorkspace();
        workspace.WriteSource("worker.as", """
            export native func update(Array value) Array { value[0] += 1; return value; }
            export native func updateBox(IndexedBox value) IndexedBox { value[0] += 1; return value; }
            """);
        workspace.WriteSource("main.as", """
            @module(TEST);
            import worker from './worker';
            export native func run() Array { return worker.update([41]); }
            export native func box() IndexedBox {
                var value = new IndexedBox();
                value[0] = 41;
                return worker.updateBox(value);
            }
            export native func readBox(IndexedBox value) Number { return value[0]; }
            export func dynamicBox(value) { value[0]++; return value[0]; }
            export native func collectBox(IndexedBox value) int32 { return value.collect(9, 1, 'two', true); }
            export native func clearCollection(IndexedBox value) int32 { return value.collect(8); }
            export native func numbersBox(IndexedBox value) Number { return value.numbers(1, 2, 3); }
            export native func defaultNumbers(IndexedBox value) Number { return value.numbers(); }
            """);
        var output = Path.Combine(workspace.Root, "test-output.dll");
        var options = EngineOptions.Default
            .WithCompiler(compiler => compiler.SourceResolver = AuroraScript.Core.ScriptSources.FileSystem(workspace.Root))
            .WithCompiler(compiler => compiler.Mode = CompilationMode.Persistence)
            .WithCompiler(compiler => compiler.WithNativeTypes(typeof(Host.IndexedBox)))
            .WithOptimization(optimization => optimization.Level = OptimizeOptions.Release)
            .WithOutput(settings => settings.AssemblyFile = output);
        var engine = new AuroraEngine(options);
        await engine.BuildAsync(["main.as"]);
        using var domain = engine.CreateDomain();
        ScriptAssert.Equal(new object[] { 42 }, TestWorkspace.Execute(domain, "run"));
        var box = TestWorkspace.Execute(domain, "box");
        ScriptAssert.Equal(42, TestWorkspace.Execute(domain, "readBox", arguments: [box]));
        ScriptAssert.Equal(43, TestWorkspace.Execute(domain, "dynamicBox", arguments: [box]));
        ScriptAssert.Equal(3, TestWorkspace.Execute(domain, "collectBox", arguments: [box]));
        var instance = (Host.IndexedBox)box.Object;
        var retained = instance.Collected;
        Assert.True(instance.SawContext);
        Assert.Equal(3, retained.Length);
        ScriptAssert.Equal("two", retained[1]);
        ScriptAssert.Equal(0, TestWorkspace.Execute(domain, "clearCollection", arguments: [box]));
        Assert.Empty(instance.Collected);
        ScriptAssert.Equal("two", retained[1]);
        ScriptAssert.Equal(6, TestWorkspace.Execute(domain, "numbersBox", arguments: [box]));
        ScriptAssert.Equal(10, TestWorkspace.Execute(domain, "defaultNumbers", arguments: [box]));
        var method = Assembly.Load(File.ReadAllBytes(output)).GetTypes().SelectMany(type => type.GetMethods())
            .Single(method => method.Name == "readBox$native");
        Assert.Equal(typeof(Host.IndexedBox), method.GetParameters()[1].ParameterType);
        var nativeMethods = method.DeclaringType!.Assembly.GetTypes().SelectMany(type => type.GetMethods()).ToArray();
        foreach (var name in new[] { "collectBox", "clearCollection", "numbersBox", "defaultNumbers" })
        {
            var nativeCalls = StringOptimizationTests.GetCalls(nativeMethods.Single(candidate => candidate.Name == name + "$native"));
            Assert.Contains(nativeCalls, call => call.DeclaringType == typeof(Host.IndexedBox) &&
                call.Name == (name is "collectBox" or "clearCollection" ? "Collect" : "Numbers"));
            Assert.DoesNotContain(nativeCalls, call => call.Name.Contains("InvokeProperty"));
        }
        var calls = StringOptimizationTests.GetCalls(method);
        Assert.Contains(calls, call => call.DeclaringType == typeof(Host.IndexedBox) && call.Name == "get_Item");
        Assert.DoesNotContain(calls, call => call.DeclaringType == typeof(ObjectOps) ||
            call.DeclaringType == typeof(ScriptDatum) && call.Name is nameof(ScriptDatum.FromObject) or nameof(ScriptDatum.ToObject));
    }
#endif

    [Theory]
    [InlineData(CompilationMode.Dynamic)]
    [InlineData(CompilationMode.OnlyRun)]
#if NET9_0_OR_GREATER
    [InlineData(CompilationMode.Persistence)]
#endif
    public async Task NativeParameterErrorsIncludeTheCalleeAndCallerFrames(CompilationMode mode)
    {
        using var workspace = new TestWorkspace();
        var (_, domain) = await workspace.CompileModuleAsync("""
            @module(TEST);
            export native func arrayWork(Array value) void { value.push(1); }
            export func run() { var action = arrayWork; action(); }
            """, mode);
        var error = Assert.Throws<AuroraRuntimeException>(() => TestWorkspace.Execute(domain, "run"));
        Assert.Contains("expected Array, actual null", error.Message);
        Assert.Contains(error.StackTrace, frame => frame.Method == "arrayWork" && frame.Line == 2);
        Assert.Contains(error.StackTrace, frame => frame.Method == "run" && frame.Line == 3);
    }

    [Theory]
    [InlineData(CompilationMode.Dynamic)]
    [InlineData(CompilationMode.OnlyRun)]
#if NET9_0_OR_GREATER
    [InlineData(CompilationMode.Persistence)]
#endif
    public async Task NativePushPrefersFixedOverloadsAndFallsBackToDynamicAdapter(CompilationMode mode)
    {
        using var workspace = new TestWorkspace();
        var (_, domain) = await workspace.CompileModuleAsync("""
            @module(TEST);
            export native func zero(Array array) int32 { return array.push(); }
            export native func one(Array array) int32 { return array.push(1); }
            export native func two(Array array) int32 { return array.push(1, 'x'); }
            export native func three(Array array) int32 { return array.push(0, 1, 2); }
            export native func four(Array array) int32 { return array.push(1, 'x', true, null); }
            export native func five(Array array) int32 { return array.push(1, 'x', true, null, 5); }
            export native func many(Array array) int32 { return array.push(1, 'x', true, null, 5, 6, 7, 8, 9); }
            export native func prepend(Array array) int32 { return array.unshift(1, 2, 3, 4, 5); }
            export native func concatenate(Array array) Array { return array.concat(1, 2, 3, 4, 5); }
            export func spread() { var array = []; array.push(...[1, 2, 3]); return array; }
            export func order() {
                var trace = '';
                var array = [];
                func mark(value) { trace += value; return value; }
                array.push(mark('a'), mark('b'), mark('c'), mark('d'), mark('e'));
                return [trace, array];
            }
            """, mode);
        var cases = new (string Name, object?[] Values)[] {
            ("zero", []), ("one", [1]), ("two", [1, "x"]), ("three", [0, 1, 2]),
            ("four", [1, "x", true, null]), ("five", [1, "x", true, null, 5]),
            ("many", [1, "x", true, null, 5, 6, 7, 8, 9])
        };
        foreach (var (name, values) in cases)
        {
            var array = new ScriptArray();
            // Repeated appends exercise growth and appending to nonempty storage.
            for (var iteration = 1; iteration <= 3; iteration++)
            {
                ScriptAssert.Equal(values.Length * iteration, TestWorkspace.Execute(domain, name, arguments: [ScriptDatum.FromArray(array)]));
                ScriptAssert.Equal(Enumerable.Range(0, iteration).SelectMany(_ => values).ToArray(), ScriptDatum.FromArray(array));
            }
        }
        var prepended = new ScriptArray();
        ScriptAssert.Equal(5, TestWorkspace.Execute(domain, "prepend", arguments: [ScriptDatum.FromArray(prepended)]));
        ScriptAssert.Equal(new object[] { 1, 2, 3, 4, 5 }, ScriptDatum.FromArray(prepended));
        ScriptAssert.Equal(new object[] { 1, 2, 3, 4, 5 }, TestWorkspace.Execute(domain, "concatenate", arguments: [ScriptDatum.FromArray(new ScriptArray())]));
        ScriptAssert.Equal(new object[] { 1, 2, 3 }, TestWorkspace.Execute(domain, "spread"));
        ScriptAssert.Equal(new object[] { "abcde", new object[] { "a", "b", "c", "d", "e" } }, TestWorkspace.Execute(domain, "order"));
#if NET9_0_OR_GREATER
        if (mode == CompilationMode.Persistence)
        {
            var methods = Assembly.Load(File.ReadAllBytes(Path.Combine(workspace.Root, "test-output.dll")))
                .GetTypes().SelectMany(type => type.GetMethods()).ToArray();
            foreach (var (name, values) in cases)
            {
                var method = methods.Single(method => method.Name == name + "$native");
                var calls = StringOptimizationTests.GetCalls(method);
                if (values.Length > 4)
                {
                    Assert.DoesNotContain(calls, call => call.DeclaringType == typeof(ScriptArray) && call.Name == nameof(ScriptArray.PushCore));
                    Assert.Contains(calls, call => call.Name.Contains("InvokeProperty"));
                    continue;
                }
                Assert.Contains(calls, call => call.DeclaringType == typeof(ScriptArray) &&
                    call.Name == nameof(ScriptArray.PushCore) && call.GetParameters().Length == values.Length);
                Assert.DoesNotContain(calls, call => call.Name.Contains("InvokeProperty"));
                Assert.DoesNotContain(calls, call => call.DeclaringType == typeof(ScriptDatum) &&
                    call.Name is nameof(ScriptDatum.FromObject) or nameof(ScriptDatum.ToObject));
                Assert.Empty(method.GetMethodBody()!.ExceptionHandlingClauses);

                // Measure the compiled script call, excluding harness allocations and backing storage growth.
                var invoke = method.CreateDelegate<Func<ScriptContext, ScriptArray, int>>();
                var context = new ScriptContext(domain);
                const int iterations = 10_000;
                var array = ScriptArray.CreateEmptyWithCapacity(values.Length * (iterations + 100));
                for (var i = 0; i < 100; i++) invoke(context, array);
                var before = GC.GetAllocatedBytesForCurrentThread();
                for (var i = 0; i < iterations; i++) invoke(context, array);
                var allocated = GC.GetAllocatedBytesForCurrentThread() - before;
                Assert.Equal(0L, allocated);
                Assert.Equal(values.Length * (iterations + 100), array.Length);
            }
        }
#endif
    }

}
