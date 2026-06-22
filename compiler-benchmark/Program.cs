using BenchmarkDotNet.Running;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace CompilerBenchmark
{
    internal static class Program
    {
        private static async Task Main(string[] args)
        {
            if (args.Any(arg => string.Equals(arg, "--smoke", StringComparison.OrdinalIgnoreCase)))
            {
                await SmokeTest();
                return;
            }

            if (args.Any(arg => string.Equals(arg, "--compare", StringComparison.OrdinalIgnoreCase)))
            {
                await Compare();
                return;
            }

            BenchmarkRunner.Run<CompilerPipelineBenchmarks>();
        }

        private static async Task SmokeTest()
        {
            await CompilerRegressionSmoke.RunAsync();
            Console.WriteLine("CompilerRegressionSmoke: completed");

            var benchmarks = new CompilerPipelineBenchmarks();
            benchmarks.Setup();

            foreach (var method in BenchmarkMethods())
            {
                var result = method.Invoke(benchmarks, Array.Empty<object>());
                if (result is Task task)
                {
                    await task;
                    Console.WriteLine($"{method.Name}: completed");
                }
                else
                {
                    Console.WriteLine($"{method.Name}: {result ?? "completed"}");
                }
            }
        }

        private static async Task Compare()
        {
            var benchmarks = new CompilerPipelineBenchmarks();
            benchmarks.Setup();
            Console.WriteLine("Name,SourceBytes,ElapsedMs,AllocatedBytes,Gen0,Gen1,Gen2");

            foreach (var method in BenchmarkMethods())
            {
                await Measure(method, benchmarks);
            }
        }

        private static IEnumerable<MethodInfo> BenchmarkMethods()
        {
            return typeof(CompilerPipelineBenchmarks)
                .GetMethods(BindingFlags.Instance | BindingFlags.Public)
                .Where(method => method.GetCustomAttribute<BenchmarkDotNet.Attributes.BenchmarkAttribute>() != null)
                .OrderBy(method => method.Name, StringComparer.Ordinal);
        }

        private static async Task Measure(MethodInfo method, CompilerPipelineBenchmarks benchmarks)
        {
            const int runs = 8;
            const int warmups = 2;

            for (var i = 0; i < warmups; i++)
            {
                await Invoke(method, benchmarks);
            }

            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();

            var gen0Before = GC.CollectionCount(0);
            var gen1Before = GC.CollectionCount(1);
            var gen2Before = GC.CollectionCount(2);
            var beforeBytes = GC.GetTotalAllocatedBytes(true);
            var stopwatch = Stopwatch.StartNew();

            for (var i = 0; i < runs; i++)
            {
                await Invoke(method, benchmarks);
            }

            stopwatch.Stop();
            var afterBytes = GC.GetTotalAllocatedBytes(true);
            var gen0After = GC.CollectionCount(0);
            var gen1After = GC.CollectionCount(1);
            var gen2After = GC.CollectionCount(2);

            Console.WriteLine(
                $"{method.Name},{benchmarks.GetSourceBytes(method.Name)},{stopwatch.Elapsed.TotalMilliseconds / runs:0.###},{(afterBytes - beforeBytes) / runs},{gen0After - gen0Before},{gen1After - gen1Before},{gen2After - gen2Before}");
        }

        private static async Task Invoke(MethodInfo method, CompilerPipelineBenchmarks benchmarks)
        {
            var result = method.Invoke(benchmarks, Array.Empty<object>());
            if (result is Task task)
            {
                await task.ConfigureAwait(false);
            }
        }
    }
}
