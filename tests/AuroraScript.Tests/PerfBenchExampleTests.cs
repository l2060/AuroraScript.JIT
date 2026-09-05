using AuroraScript.Runtime;
using AuroraScript.Runtime.Types;
using AuroraScript.Tests.Infrastructure;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace AuroraScript.Tests;

public sealed class PerfBenchExampleTests
{
    [Theory]
    [InlineData(CompilationMode.Dynamic)]
    [InlineData(CompilationMode.OnlyRun)]
#if NET9_0_OR_GREATER
    [InlineData(CompilationMode.Persistence)]
#endif
    public async Task StatisticsSortFloatingSamplesWithoutChangingInput(CompilationMode mode)
    {
        using var workspace = new TestWorkspace();
        var (_, domain) = await workspace.CompileModuleAsync(ReadExample(), mode);
        using var domainScope = domain;

        double[][] cases =
        [
            [],
            [0.125],
            [3, 1, 2],
            [0.75, 0.125, 0.5, 0.125],
            [0, 0, 0],
            Enumerable.Range(1, 100).Reverse().Select(value => value / 1000.0).ToArray()
        ];
        foreach (var values in cases)
        {
            var samples = new ScriptFloat64Array(values.Length);
            for (var i = 0; i < values.Length; i++)
            {
                samples.SetElement(i, values[i]);
            }

            var result = TestWorkspace.Execute(domain, "calcStats", "PERF_BENCH", ScriptDatum.FromObject(samples));
            AssertStatistics(result.Object, values);
            for (var i = 0; i < values.Length; i++)
            {
                Assert.Equal(values[i], samples.GetElement(i));
            }
        }
    }

    [Theory]
    [InlineData(CompilationMode.Dynamic)]
    [InlineData(CompilationMode.OnlyRun)]
#if NET9_0_OR_GREATER
    [InlineData(CompilationMode.Persistence)]
#endif
    public async Task BenchmarkPreservesFractionalMillisecondsPerOperation(CompilationMode mode)
    {
        using var workspace = new TestWorkspace();
        double[] clock = [0, 0.5, 1, 1.25, 2, 2.75];
        var clockIndex = 0;
        var workCalls = 0;
        var (_, domain) = await workspace.CompileModuleAsync(
            ReadExample() + """

            export func testBenchmark() {
                return benchmark('fractional', 2, 3, 4, () => HOST_WORK());
            }
            """,
            mode,
            configureGlobal: global =>
            {
                global.Define("PERF_NOW_MS", ScriptDatum.FromBonding(
                    (ScriptContext ctx, ScriptObject self, Span<ScriptDatum> args, ref ScriptDatum result) =>
                        ScriptDatum.WriteAsNumber(ref result, clock[clockIndex++])), false, false);
                global.Define("HOST_WORK", ScriptDatum.FromBonding(
                    (ScriptContext ctx, ScriptObject self, Span<ScriptDatum> args, ref ScriptDatum result) =>
                    {
                        workCalls++;
                        result = ScriptDatum.FromInt64(1);
                    }), false, false);
            });
        using var domainScope = domain;

        var result = TestWorkspace.Execute(domain, "testBenchmark", "PERF_BENCH").Object;
        var samples = Assert.IsType<ScriptFloat64Array>(result.GetPropertyDatum(null, "rawMsPerOp").Object);
        double[] expected = [0.125, 0.0625, 0.1875];
        Assert.Equal(expected.Length, samples.Length);
        for (var i = 0; i < expected.Length; i++)
        {
            Assert.Equal(expected[i], samples.GetElement(i));
        }
        AssertStatistics(result.GetPropertyDatum(null, "stats").Object, expected);
        ScriptAssert.Equal("ms/op", result.GetPropertyDatum(null, "unit"));
        AssertNumber(result, "innerLoops", 4);
        AssertNumber(result, "guard", 12);
        Assert.Equal(20, workCalls); // Includes warmups, which are not timed or included in guard.
        Assert.Equal(clock.Length, clockIndex);
    }

    [Theory]
    [InlineData(CompilationMode.Dynamic)]
    [InlineData(CompilationMode.OnlyRun)]
#if NET9_0_OR_GREATER
    [InlineData(CompilationMode.Persistence)]
#endif
    public async Task FullExampleRunsInt64WorkloadWithMonotonicClock(CompilationMode mode)
    {
        using var workspace = new TestWorkspace();
        var clockCalls = 0;
        var (_, domain) = await workspace.CompileModuleAsync(
            ReadExample(), mode,
            configureGlobal: global => global.Define("PERF_NOW_MS", ScriptDatum.FromBonding(
                (ScriptContext ctx, ScriptObject self, Span<ScriptDatum> args, ref ScriptDatum result) =>
                    ScriptDatum.WriteAsNumber(ref result, clockCalls++ * 100.0)), false, false),
            enableModuleConstInlining: true);
        using var domainScope = domain;

        var result = TestWorkspace.Execute(domain, "run", "PERF_BENCH").Object;
        AssertNumber(result, "innerLoops", 1);
        AssertNumber(result, "guard", 2499750000.0);
        AssertStatistics(result.GetPropertyDatum(null, "stats").Object, Enumerable.Repeat(100.0, 50).ToArray());
        Assert.Equal(102, clockCalls);
    }

    private static void AssertStatistics(ScriptObject stats, double[] values)
    {
        var sorted = (double[])values.Clone();
        Array.Sort(sorted);
        var mean = values.Length == 0 ? 0 : values.Average();
        var stddev = values.Length == 0 ? 0 : Math.Sqrt(values.Average(value => (value - mean) * (value - mean)));
        double Percentile(double p) => sorted.Length == 0 ? 0 : sorted[(int)Math.Ceiling(p * sorted.Length) - 1];

        AssertNumber(stats, "count", values.Length);
        AssertNumber(stats, "minMs", sorted.Length == 0 ? 0 : sorted[0]);
        AssertNumber(stats, "maxMs", sorted.Length == 0 ? 0 : sorted[^1]);
        AssertNumber(stats, "meanMs", mean);
        AssertNumber(stats, "medianMs", Percentile(0.50));
        AssertNumber(stats, "p90Ms", Percentile(0.90));
        AssertNumber(stats, "p95Ms", Percentile(0.95));
        AssertNumber(stats, "p99Ms", Percentile(0.99));
        AssertNumber(stats, "stddevMs", stddev);
        AssertNumber(stats, "cv", mean == 0 ? 0 : stddev / mean);
    }

    private static void AssertNumber(ScriptObject value, string property, double expected)
    {
        var datum = value.GetPropertyDatum(null, property);
        Assert.Equal(ValueKind.Number, datum.Kind);
        Assert.Equal(expected, datum.Number, precision: 10);
    }

    private static string ReadExample()
    {
        for (var directory = new DirectoryInfo(AppContext.BaseDirectory); directory != null; directory = directory.Parent)
        {
            var path = Path.Combine(directory.FullName, "examples", "tests", "perf-bench.as");
            if (File.Exists(path))
            {
                return File.ReadAllText(path);
            }
        }
        throw new FileNotFoundException("Could not locate examples/tests/perf-bench.as.");
    }
}
