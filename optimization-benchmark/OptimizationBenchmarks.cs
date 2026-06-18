using AuroraScript;
using AuroraScript.Runtime;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Mathematics;
using BenchmarkDotNet.Order;
using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace OptimizationBenchmark
{
    [MemoryDiagnoser]
    [MinColumn, MaxColumn, MeanColumn, MedianColumn]
    [GroupBenchmarksBy(BenchmarkLogicalGroupRule.ByCategory)]
    [Orderer(SummaryOrderPolicy.FastestToSlowest)]
    [RankColumn(NumeralSystem.Arabic)]
    [CategoriesColumn]
    public class OptimizationBenchmarks
    {
        private AuroraEngine engine;
        private ScriptDomain domain;

        [Params(1000, 10000)]
        public int Iterations { get; set; }

        [GlobalSetup]
        public async Task Setup()
        {
            var baseDirectory = Path.Combine(AppContext.BaseDirectory, "scripts");
            var options = EngineOptions.Default
                .WithBaseDirectory(baseDirectory)
                .WithConsoleStdOut(TextWriter.Null)
                .WithConsoleErrorOut(TextWriter.Null)
                .WithCompilationMode(CompilationMode.Dynamic)
                .WithOptimizeOption(OptimizeOptions.Release);

            engine = new AuroraEngine(options);
            await engine.BuildAsync(engine.SearchAllFileSource(Encoding.UTF8));
            domain = engine.CreateDomain(_ => { });
        }

        [BenchmarkCategory("domain")]
        [Benchmark]
        public ScriptDomain CreateDomain()
        {
            return engine.CreateDomain(_ => { });
        }

        [BenchmarkCategory("call")]
        [Benchmark]
        public ScriptDatum EmptyCall()
        {
            return domain.Execute("OPT_BENCH", "empty");
        }

        [BenchmarkCategory("call")]
        [Benchmark]
        public ScriptDatum CallNoArgs()
        {
            return domain.Execute("OPT_BENCH", "callNoArgs", Iterations);
        }

        [BenchmarkCategory("call")]
        [Benchmark]
        public ScriptDatum CallOneArg()
        {
            return domain.Execute("OPT_BENCH", "callOneArg", Iterations);
        }

        [BenchmarkCategory("call")]
        [Benchmark]
        public ScriptDatum CallTwoArgs()
        {
            return domain.Execute("OPT_BENCH", "callTwoArgs", Iterations);
        }

        [BenchmarkCategory("call")]
        [Benchmark]
        public ScriptDatum CallThreeArgs()
        {
            return domain.Execute("OPT_BENCH", "callThreeArgs", Iterations);
        }

        [BenchmarkCategory("call")]
        [Benchmark]
        public ScriptDatum CallFourArgs()
        {
            return domain.Execute("OPT_BENCH", "callFourArgs", Iterations);
        }

        [BenchmarkCategory("call")]
        [Benchmark]
        public ScriptDatum CallFiveArgs()
        {
            return domain.Execute("OPT_BENCH", "callFiveArgs", Iterations);
        }

        [BenchmarkCategory("call")]
        [Benchmark]
        public ScriptDatum CallSevenArgs()
        {
            return domain.Execute("OPT_BENCH", "callSevenArgs", Iterations);
        }

        [BenchmarkCategory("call")]
        [Benchmark]
        public ScriptDatum PropertyCallTwoArgs()
        {
            return domain.Execute("OPT_BENCH", "propertyCallTwoArgs", Iterations);
        }

        [BenchmarkCategory("call")]
        [Benchmark]
        public ScriptDatum PropertyCallThreeArgs()
        {
            return domain.Execute("OPT_BENCH", "propertyCallThreeArgs", Iterations);
        }

        [BenchmarkCategory("call")]
        [Benchmark]
        public ScriptDatum PropertyCallFourArgs()
        {
            return domain.Execute("OPT_BENCH", "propertyCallFourArgs", Iterations);
        }

        [BenchmarkCategory("call")]
        [Benchmark]
        public ScriptDatum PropertyCallSevenArgs()
        {
            return domain.Execute("OPT_BENCH", "propertyCallSevenArgs", Iterations);
        }

        [BenchmarkCategory("numeric")]
        [Benchmark]
        public ScriptDatum NumericLoop()
        {
            return domain.Execute("OPT_BENCH", "numericLoop", Iterations);
        }

        [BenchmarkCategory("object")]
        [Benchmark]
        public ScriptDatum ObjectCreateSetGet()
        {
            return domain.Execute("OPT_BENCH", "objectCreateSetGet", Iterations);
        }

        [BenchmarkCategory("object")]
        [Benchmark]
        public ScriptDatum ObjectEnumerate()
        {
            return domain.Execute("OPT_BENCH", "objectEnumerate", Iterations);
        }

        [BenchmarkCategory("array")]
        [Benchmark]
        public ScriptDatum ArrayLiteral()
        {
            return domain.Execute("OPT_BENCH", "arrayLiteral", Iterations);
        }

        [BenchmarkCategory("array")]
        [Benchmark]
        public ScriptDatum ArrayPushIndex()
        {
            return domain.Execute("OPT_BENCH", "arrayPushIndex", Iterations);
        }

        [BenchmarkCategory("string")]
        [Benchmark]
        public ScriptDatum StringConcat()
        {
            return domain.Execute("OPT_BENCH", "stringConcat", Iterations);
        }

        [BenchmarkCategory("closure")]
        [Benchmark]
        public ScriptDatum ClosureInvoke()
        {
            return domain.Execute("OPT_BENCH", "closureInvoke", Iterations);
        }
    }
}
