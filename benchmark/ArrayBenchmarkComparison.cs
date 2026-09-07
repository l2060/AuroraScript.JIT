using AuroraScript.Runtime;
using BenchmarkDotNet.Attributes;
using System;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;

namespace AuroraBenchmark;

internal static class ArrayBenchmarkComparison
{
    public static void CompareCompiled(string baselinePath, string optimizedPath)
    {
        var baseline = new ArrayBenchmarks();
        var optimized = new ArrayBenchmarks();
        using var process = Process.GetCurrentProcess();
        var affinity = OperatingSystem.IsWindows() ? process.ProcessorAffinity : IntPtr.Zero;
        try
        {
            if (OperatingSystem.IsWindows()) process.ProcessorAffinity = new IntPtr(1);
            baseline.SetupCompiled(baselinePath);
            optimized.SetupCompiled(optimizedPath);
            var methods = typeof(ArrayBenchmarks).GetMethods().Where(m => Attribute.IsDefined(m, typeof(BenchmarkAttribute))).ToArray();
            var before = methods.Select(m => m.CreateDelegate<Func<ScriptDatum>>(baseline)).ToArray();
            var after = methods.Select(m => m.CreateDelegate<Func<ScriptDatum>>(optimized)).ToArray();
            var warmup = Stopwatch.StartNew();
            do { for (var i=0; i<before.Length; i++) { before[i](); after[i](); } }
            while (warmup.Elapsed < TimeSpan.FromSeconds(3));
            Console.WriteLine("Method,BaselineNs,OptimizedNs,ChangePercent,BaselineBytes,OptimizedBytes");
            const int samples = 15;
            for (var i=0; i<methods.Length; i++)
            {
                var repeats = 1;
                while (Measure(before[i], repeats).Ns * repeats * ArrayBenchmarks.Operations < 15_000_000 && repeats < 16384) repeats *= 2;
                var b = new double[samples]; var a = new double[samples];
                var bb = new double[samples]; var ab = new double[samples];
                for (var sample=0; sample<samples; sample++)
                {
                    if (sample % 2 == 0) { (b[sample],bb[sample])=Measure(before[i],repeats); (a[sample],ab[sample])=Measure(after[i],repeats); }
                    else { (a[sample],ab[sample])=Measure(after[i],repeats); (b[sample],bb[sample])=Measure(before[i],repeats); }
                }
                Array.Sort(b); Array.Sort(a); Array.Sort(bb); Array.Sort(ab);
                var middle=samples/2;
                Console.WriteLine(string.Create(CultureInfo.InvariantCulture,
                    $"{methods[i].Name},{b[middle]:F3},{a[middle]:F3},{(a[middle]/b[middle]-1)*100:F2},{bb[middle]:F3},{ab[middle]:F3}"));
            }
        }
        finally
        {
            baseline.Cleanup(); optimized.Cleanup();
            if (OperatingSystem.IsWindows()) process.ProcessorAffinity = affinity;
        }
    }

    private static (double Ns, double Bytes) Measure(Func<ScriptDatum> call, int repeats)
    {
        var allocated=GC.GetAllocatedBytesForCurrentThread();
        var start=Stopwatch.GetTimestamp();
        for(var i=0; i<repeats; i++) call();
        var elapsed=Stopwatch.GetElapsedTime(start);
        return (elapsed.TotalNanoseconds/(repeats*ArrayBenchmarks.Operations),
            (double)(GC.GetAllocatedBytesForCurrentThread()-allocated)/(repeats*ArrayBenchmarks.Operations));
    }

    public static async Task RunAsync()
    {
        var benchmark = new ArrayBenchmarks();
        try
        {
            await benchmark.Setup();
            var methods = typeof(ArrayBenchmarks).GetMethods().Where(m => Attribute.IsDefined(m, typeof(BenchmarkAttribute))).ToArray();
            var calls = methods.Select(m => m.CreateDelegate<Func<ScriptDatum>>(benchmark)).ToArray();
            const int samples = 9, repeats = 128;
            var times = calls.Select(_ => new double[samples]).ToArray();
            var bytes = calls.Select(_ => new double[samples]).ToArray();
            var warmup = Stopwatch.StartNew();
            do { foreach (var call in calls) call(); }
            while (warmup.Elapsed < TimeSpan.FromSeconds(2));
            for (var sample = 0; sample < samples; sample++)
                for (var step = 0; step < calls.Length; step++)
                {
                    var index = sample % 2 == 0 ? step : calls.Length - step - 1;
                    var allocated = GC.GetAllocatedBytesForCurrentThread();
                    var start = Stopwatch.GetTimestamp();
                    for (var repeat = 0; repeat < repeats; repeat++) calls[index]();
                    var elapsed = Stopwatch.GetElapsedTime(start);
                    times[index][sample] = elapsed.TotalNanoseconds / (repeats * ArrayBenchmarks.Operations);
                    bytes[index][sample] = (double)(GC.GetAllocatedBytesForCurrentThread() - allocated) / (repeats * ArrayBenchmarks.Operations);
                }
            Console.WriteLine("Method,MedianNsPerOp,MedianBytesPerOp");
            for (var i = 0; i < calls.Length; i++)
            {
                Array.Sort(times[i]);
                Array.Sort(bytes[i]);
                Console.WriteLine(string.Create(CultureInfo.InvariantCulture, $"{methods[i].Name},{times[i][samples / 2]:F3},{bytes[i][samples / 2]:F3}"));
            }
        }
        finally { benchmark.Cleanup(); }
    }
}
