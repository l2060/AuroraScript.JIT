using AuroraScript;
using AuroraScript.Core;
using AuroraScript.Runtime;
using AuroraScript.Runtime.Types;
using AuroraScript.Source;
using BenchmarkDotNet.Attributes;
using System;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;

namespace AuroraBenchmark;

[MemoryDiagnoser]
public class ArrayBenchmarks
{
    public const int Operations = 2048;
    private ScriptDomain _domain;
    private ScriptDatum[] _read, _write, _push, _capacity, _dynamic;

    [GlobalSetup]
    public async Task Setup()
    {
        var root = Path.Combine(Path.GetTempPath(), "aurora-array-benchmark-source");
        var engine = new AuroraEngine(EngineOptions.Default
            .WithCompiler(c => { c.SourceResolver = ScriptSources.Memory(root); c.Mode = CompilationMode.Persistence; })
            .WithOptimization(o => { o.Level = OptimizeOptions.Release; o.StackTrace = false; })
            .WithOutput(o => o.AssemblyFile = Path.Combine(AppContext.BaseDirectory, "array-bench.dll")));
        await engine.BuildAsync(new MemorySource(root, "array-bench.as", """
            @module(ARRAY_BENCH);
            export native func length(Array a, int32 count) Number {
                var total = 0; for (var i=0; i<count; i++) total += a.length; return total;
            }
            export native func mutableLength(Array a, int32 count) Number {
                var total = 0; for (var i=0; i<count; i++) { a.length=i&15; total+=a.length; } return total;
            }
            export native func read(Array a, int32 count) Number {
                var total = 0; for (var i=0; i<count; i++) total += a[i&63]; return total;
            }
            export native func write(Array a, int32 count) Number {
                for (var i=0; i<count; i++) a[i&63]=i; return a[(count-1)&63];
            }
            export native func push(Array a, int32 count) Number {
                a.length=0; for (var i=0; i<count; i++) a.push(i); return a.length;
            }
            export func capacityInt(int32 capacity, int32 count) {
                var result; for (var i=0; i<count; i++) result=Array.withCapacity(capacity); return result;
            }
            export func capacityNumber(Number capacity, int32 count) {
                var result; for (var i=0; i<count; i++) result=Array.withCapacity(capacity); return result;
            }
            export func capacityDynamic(type, int32 capacity, int32 count) {
                var result; for (var i=0; i<count; i++) result=type.withCapacity(capacity); return result;
            }
            export func construct(int32 capacity, int32 count) {
                var result; for (var i=0; i<count; i++) result=new Array(capacity); return result;
            }
            """));
        Initialize(engine, engine.CreateDomain());
    }

    internal void SetupCompiled(string assemblyPath)
    {
        var engine = new AuroraEngine(EngineOptions.Default
            .WithOptimization(o => { o.Level = OptimizeOptions.Release; o.StackTrace = false; }));
        var domain = engine.CreateEmptyDomain(null);
        var entry = Assembly.Load(File.ReadAllBytes(assemblyPath))
            .GetType("AuroraScriptInitializer", throwOnError: true)
            .GetMethod("InitializeDomain").CreateDelegate<ScriptFunctionDelegate>();
        entry(new ScriptContext(domain), Span<ScriptDatum>.Empty);
        Initialize(engine, domain);
    }

    private void Initialize(AuroraEngine engine, ScriptDomain domain)
    {
        _domain = domain;
        var read = new ScriptArray(64);
        for (var i = 0; i < 64; i++) read.SetElement(i, ScriptDatum.FromNumber(7));
        _read = [ScriptDatum.FromArray(read), ScriptDatum.FromNumber(Operations)];
        _write = [ScriptDatum.FromArray(new ScriptArray(64)), ScriptDatum.FromNumber(Operations)];
        _push = [ScriptDatum.FromArray(ScriptArray.CreateWithCapacity(Operations)), ScriptDatum.FromNumber(Operations)];
        _capacity = [ScriptDatum.FromNumber(8), ScriptDatum.FromNumber(Operations)];
        _dynamic = [engine.Global.GetPropertyDatum(null, "Array"), _capacity[0], _capacity[1]];
        Require(Length().Number == 64 * Operations && MutableLength().Number == 15 * Operations / 2, "length");
        Require(IndexRead().Number == 7 * Operations && IndexWrite().Number == Operations - 1, "index");
        Require(Push().Number == Operations, "push");
        Require(((ScriptArray)WithCapacityInt().Object).Length == 0, "int capacity");
        Require(((ScriptArray)WithCapacityNumber().Object).Length == 0, "number capacity");
        Require(((ScriptArray)WithCapacityDynamic().Object).Length == 0, "dynamic capacity");
        Require(((ScriptArray)Construct().Object).Length == 8, "construct");
    }

    [Benchmark(OperationsPerInvoke = Operations)] public ScriptDatum Length() => Run("length", _read);
    [Benchmark(OperationsPerInvoke = Operations)] public ScriptDatum MutableLength() => Run("mutableLength", _write);
    [Benchmark(OperationsPerInvoke = Operations)] public ScriptDatum IndexRead() => Run("read", _read);
    [Benchmark(OperationsPerInvoke = Operations)] public ScriptDatum IndexWrite() => Run("write", _write);
    [Benchmark(OperationsPerInvoke = Operations)] public ScriptDatum Push() => Run("push", _push);
    [Benchmark(OperationsPerInvoke = Operations)] public ScriptDatum WithCapacityInt() => Run("capacityInt", _capacity);
    [Benchmark(OperationsPerInvoke = Operations)] public ScriptDatum WithCapacityNumber() => Run("capacityNumber", _capacity);
    [Benchmark(OperationsPerInvoke = Operations)] public ScriptDatum WithCapacityDynamic() => Run("capacityDynamic", _dynamic);
    [Benchmark(OperationsPerInvoke = Operations)] public ScriptDatum Construct() => Run("construct", _capacity);

    [GlobalCleanup] public void Cleanup() => _domain?.Dispose();
    private ScriptDatum Run(string name, ScriptDatum[] args) => _domain.Execute("ARRAY_BENCH", name, ScriptObject.Null, args);
    private static void Require(bool value, string operation)
    {
        if (!value) throw new InvalidOperationException("Array benchmark validation failed: " + operation);
    }
}
