using AuroraScript;
using AuroraScript.Core;
using AuroraScript.Runtime;
using AuroraScript.Runtime.Types;
using AuroraScript.Source;
using BenchmarkDotNet.Attributes;
using System;
using System.IO;
using System.Threading.Tasks;

namespace AuroraBenchmark;

[MemoryDiagnoser]
public class GeneratedBoundaryBenchmarks
{
    private const int Count = 512;
    private ScriptDomain _domain;
    private ScriptDatum[] _comparisonArgs, _cacheArgs, _loopArgs;

    [GlobalSetup]
    public async Task Setup()
    {
        var root = Path.Combine(Path.GetTempPath(), "aurora-generated-boundary-bench");
        var engine = new AuroraEngine(EngineOptions.Default
            .WithCompiler(c => { c.SourceResolver = ScriptSources.Memory(root); c.Mode = CompilationMode.Persistence; })
            .WithOptimization(o => { o.Level = OptimizeOptions.Release; o.StackTrace = false; })
            .WithOutput(o => o.AssemblyFile = Path.Combine(AppContext.BaseDirectory, "generated-boundary-bench.dll")));
        await engine.BuildAsync(new MemorySource(root, "boundary.as", """
            @module(BOUNDARY);
            export func compare(state, Number bound, int32 count) {
                var result = 0;
                for (var i = 0; i < count; i++) {
                    if (state.value < bound) result++;
                    if (bound > state.value) result++;
                }
                return result;
            }
            export func cached(value, int32 count) {
                var result = 0;
                for (var i = 0; i < count; i++) {
                    result += value * 2;
                    if (value < 10) result++;
                    if (i == count / 2) value = '4';
                }
                return result;
            }
            export func iterate(values) {
                var result = 0;
                for (var value in values) result += value;
                return result;
            }
            """));
        _domain = engine.CreateDomain();
        var state = new ScriptObject();
        state.Define("value", ScriptDatum.FromNumber(3));
        var array = new ScriptArray(Count);
        for (var i = 0; i < Count; i++) array[i] = ScriptDatum.FromNumber(1);
        _comparisonArgs = [ScriptDatum.FromObject(state), ScriptDatum.FromNumber(10), ScriptDatum.FromNumber(Count)];
        _cacheArgs = [ScriptDatum.FromNumber(3), ScriptDatum.FromNumber(Count)];
        _loopArgs = [ScriptDatum.FromArray(array)];
        if (Comparison().Number != 2 * Count || CachedNumber().Number != 257 * 7 + 255 * 9 ||
            Iteration().Number != Count) throw new InvalidOperationException("Boundary benchmark result mismatch.");
    }

    [Benchmark(OperationsPerInvoke = Count)]
    public ScriptDatum Comparison() => _domain.Execute("BOUNDARY", "compare", ScriptObject.Null, _comparisonArgs);

    [Benchmark(OperationsPerInvoke = Count)]
    public ScriptDatum CachedNumber() => _domain.Execute("BOUNDARY", "cached", ScriptObject.Null, _cacheArgs);

    [Benchmark(OperationsPerInvoke = Count)]
    public ScriptDatum Iteration() => _domain.Execute("BOUNDARY", "iterate", ScriptObject.Null, _loopArgs);
}
