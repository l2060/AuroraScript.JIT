using BenchmarkDotNet.Running;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace OptimizationBenchmark
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

            BenchmarkRunner.Run<OptimizationBenchmarks>();
        }

        private static async Task SmokeTest()
        {
            var benchmarks = new OptimizationBenchmarks { Iterations = 100 };
            await benchmarks.Setup();

            foreach (var method in typeof(OptimizationBenchmarks).GetMethods(BindingFlags.Instance | BindingFlags.Public))
            {
                if (method.GetCustomAttribute<BenchmarkDotNet.Attributes.BenchmarkAttribute>() == null)
                {
                    continue;
                }

                var result = method.Invoke(benchmarks, Array.Empty<object>());
                Console.WriteLine($"{method.Name}: {result}");
            }
        }

        private static async Task Compare()
        {
            var iterationArgs = new[] { 1000, 10000 };
            foreach (var enableHotReload in new[] { true, false })
            {
                foreach (var iterations in iterationArgs)
                {
                    var benchmarks = new OptimizationBenchmarks
                    {
                        EnableHotReload = enableHotReload,
                        Iterations = iterations
                    };
                    await benchmarks.Setup();
                    Console.WriteLine($"EnableHotReload={enableHotReload};Iterations={iterations}");
                    Console.WriteLine("Name,ElapsedMs,AllocatedBytes,Result");

                    foreach (var method in BenchmarkMethods())
                    {
                        Measure(method, benchmarks);
                    }
                }
            }
        }

        private static IEnumerable<MethodInfo> BenchmarkMethods()
        {
            return typeof(OptimizationBenchmarks)
                .GetMethods(BindingFlags.Instance | BindingFlags.Public)
                .Where(method => method.GetCustomAttribute<BenchmarkDotNet.Attributes.BenchmarkAttribute>() != null);
        }

        private static void Measure(MethodInfo method, OptimizationBenchmarks benchmarks)
        {
            const int runs = 8;
            const int warmups = 2;

            for (var i = 0; i < warmups; i++)
            {
                method.Invoke(benchmarks, Array.Empty<object>());
            }

            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();

            var beforeBytes = GC.GetAllocatedBytesForCurrentThread();
            var stopwatch = Stopwatch.StartNew();
            object result = null;
            for (var i = 0; i < runs; i++)
            {
                result = method.Invoke(benchmarks, Array.Empty<object>());
            }
            stopwatch.Stop();
            var afterBytes = GC.GetAllocatedBytesForCurrentThread();

            Console.WriteLine($"{method.Name},{stopwatch.Elapsed.TotalMilliseconds / runs:0.###},{(afterBytes - beforeBytes) / runs},{result}");
        }
    }
}
