using AuroraScript.Runtime;
using BenchmarkDotNet.Attributes;
using System;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;

namespace AuroraBenchmark;

/// <summary>Fast same-machine A/B probe; use BenchmarkDotNet for statistical analysis.</summary>
internal static class StringBenchmarkComparison
{
    public static async Task RunAsync()
    {
        var benchmarks = new StringBenchmarks();
        try
        {
            await benchmarks.Setup();
            var methods = typeof(StringBenchmarks).GetMethods()
                .Where(m => Attribute.IsDefined(m, typeof(BenchmarkAttribute))).ToArray();
            var calls = methods.Select(m => m.CreateDelegate<Func<ScriptDatum>>(benchmarks)).ToArray();
            const int samples = 9, repeats = 50;
            var durations = methods.Select(_ => new double[samples]).ToArray();
            var allocations = methods.Select(_ => new double[samples]).ToArray();
            // Warm compilation and tiered JIT outside the measurements.
            for (var warmup = 0; warmup < 24; warmup++)
                foreach (var call in calls) call();
            for (var sample = 0; sample < samples; sample++)
            {
                for (var step = 0; step < calls.Length; step++)
                {
                    var index = sample % 2 == 0 ? step : calls.Length - step - 1;
                    var call = calls[index];
                    var allocated = GC.GetAllocatedBytesForCurrentThread();
                    var start = Stopwatch.GetTimestamp();
                    for (var repeat = 0; repeat < repeats; repeat++) call();
                    var elapsed = Stopwatch.GetElapsedTime(start);
                    var bytes = GC.GetAllocatedBytesForCurrentThread() - allocated;
                    durations[index][sample] = elapsed.TotalNanoseconds / (repeats * StringBenchmarks.Operations);
                    allocations[index][sample] = (double)bytes / (repeats * StringBenchmarks.Operations);
                }
            }
            Console.WriteLine("Method,MedianNsPerOp,MedianBytesPerOp");
            for (var i = 0; i < calls.Length; i++)
            {
                Array.Sort(durations[i]);
                Array.Sort(allocations[i]);
                Console.WriteLine(string.Create(CultureInfo.InvariantCulture,
                    $"{methods[i].Name},{durations[i][samples / 2]:F3},{allocations[i][samples / 2]:F3}"));
            }
        }
        finally { benchmarks.Cleanup(); }
    }
}
