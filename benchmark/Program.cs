using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace AuroraBenchmark
{
    internal static class Program
    {
        private static async Task Main(string[] args)
        {
            if (args.Any(arg => string.Equals(arg, "--string-smoke", StringComparison.OrdinalIgnoreCase)))
            {
                var strings = new StringBenchmarks();
                try { await strings.Setup(); Console.WriteLine("StringBenchmarks: all outputs verified"); }
                finally { strings.Cleanup(); }
                return;
            }
            if (args.Any(arg => string.Equals(arg, "--string-compare", StringComparison.OrdinalIgnoreCase)))
            {
                await StringBenchmarkComparison.RunAsync();
                return;
            }

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

            BenchmarkSwitcher.FromTypes(new[] { typeof(RuntimeBenchmarks), typeof(CompilerPipelineBenchmarks), typeof(TypedDocumentBenchmarks), typeof(StringBenchmarks) }).Run(args);
        }

        private static async Task SmokeTest()
        {
            await CompilerRegressionSmoke.RunAsync();
            Console.WriteLine("CompilerRegressionSmoke: completed");

            await SmokeType(new RuntimeBenchmarks());
            await SmokeType(new CompilerPipelineBenchmarks());
        }

        private static async Task Compare()
        {
            Console.WriteLine("Suite,Name,Category,Iterations,SourceBytes,ElapsedMs,AllocatedBytes,Gen0,Gen1,Gen2");
            await CompareType(new RuntimeBenchmarks());
            await CompareType(new CompilerPipelineBenchmarks());
        }

        private static async Task SmokeType(object benchmarks)
        {
            await Setup(benchmarks);
            foreach (var method in BenchmarkMethods(benchmarks.GetType()))
            {
                var result = await Invoke(method, benchmarks);
                Console.WriteLine($"{benchmarks.GetType().Name}.{method.Name}: {result ?? "completed"}");
            }
        }

        private static async Task CompareType(object benchmarks)
        {
            await Setup(benchmarks);
            foreach (var method in BenchmarkMethods(benchmarks.GetType()))
            {
                await Measure(method, benchmarks);
            }
        }

        private static async Task Setup(object benchmarks)
        {
            foreach (var method in benchmarks.GetType().GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic))
            {
                if (method.GetCustomAttribute<GlobalSetupAttribute>() == null)
                {
                    continue;
                }

                var result = method.Invoke(benchmarks, Array.Empty<object>());
                if (result is Task task)
                {
                    await task.ConfigureAwait(false);
                }
            }
        }

        private static IEnumerable<MethodInfo> BenchmarkMethods(Type type)
        {
            return type.GetMethods(BindingFlags.Instance | BindingFlags.Public)
                .Where(method => method.GetCustomAttribute<BenchmarkAttribute>() != null)
                .OrderBy(method => method.GetCustomAttribute<BenchmarkCategoryAttribute>()?.Categories.FirstOrDefault(), StringComparer.Ordinal)
                .ThenBy(method => method.Name, StringComparer.Ordinal);
        }

        private static async Task Measure(MethodInfo method, object benchmarks)
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
            var category = method.GetCustomAttribute<BenchmarkCategoryAttribute>()?.Categories.FirstOrDefault() ?? string.Empty;
            var iterations = GetOptionalIntProperty(benchmarks, "Iterations");
            var sourceBytes = InvokeOptionalIntMethod(benchmarks, "GetSourceBytes", method.Name);

            Console.WriteLine(
                $"{benchmarks.GetType().Name},{method.Name},{category},{iterations},{sourceBytes},{stopwatch.Elapsed.TotalMilliseconds / runs:0.###},{(afterBytes - beforeBytes) / runs},{gen0After - gen0Before},{gen1After - gen1Before},{gen2After - gen2Before}");
        }

        private static async Task<object> Invoke(MethodInfo method, object benchmarks)
        {
            var result = method.Invoke(benchmarks, Array.Empty<object>());
            if (result is Task task)
            {
                await task.ConfigureAwait(false);
                return null;
            }
            return result;
        }

        private static int GetOptionalIntProperty(object instance, string name)
        {
            return instance.GetType().GetProperty(name)?.GetValue(instance) is int value ? value : 0;
        }

        private static int InvokeOptionalIntMethod(object instance, string name, string argument)
        {
            return instance.GetType().GetMethod(name)?.Invoke(instance, new object[] { argument }) is int value ? value : 0;
        }
    }
}
