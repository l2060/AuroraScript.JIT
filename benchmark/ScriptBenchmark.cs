using AuroraScript;
using AuroraScript.Runtime;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Mathematics;
using BenchmarkDotNet.Order;
using System;
using System.Text;
using System.Threading.Tasks;


namespace AuroraBenchmark
{

    [MemoryDiagnoser, MarkdownExporter, RPlotExporter]
    [MinColumn, MaxColumn, MeanColumn, MedianColumn]
    [GroupBenchmarksBy(BenchmarkLogicalGroupRule.ByCategory)]
    [Orderer(SummaryOrderPolicy.FastestToSlowest)]
    [RankColumn(NumeralSystem.Arabic)]
    [CategoriesColumn]
    public class ScriptBenchmark
    {

#pragma warning disable CS8618
        private AuroraEngine engine;
        private UserState userState;
        private ScriptDomain domain;
#pragma warning restore CS8618

        [GlobalSetup]
        public async Task Setup()
        {
            EngineOptions engineOptions = EngineOptions.Default
           .WithBaseDirectory("scripts")
           .WithConsoleStdOut(Console.Out)
           .WithConsoleErrorOut(Console.Error)
           .WithDateTimeFormat("yyyy-MM-dd HH:mm:ss")
           .WithAssemblyOut("123.dll")
           .WithEnableConfused(false)
           .WithCompilationMode(CompilationMode.Dynamic)
           .WithOptimizeOption(OptimizeOptions.Release);


            engine = new AuroraEngine(engineOptions);
            engine.RegisterType<TestObject>();
            engine.RegisterType<UserState>();
            engine.RegisterType(typeof(Math), "Math2");
            await engine.BuildAsync(engine.SearchAllFileSource(Encoding.UTF8));
            userState = new UserState();
            domain = TestCreateDomain();
        }

        [Benchmark]
        public ScriptDomain TestCreateDomain()
        {
            var g = engine.NewEnvironment();
            return engine.CreateDomain(g, userState);
        }

        [Benchmark]
        public void testDraw()
        {
            domain.Execute("UNIT_LIB", "testDraw");
        }


        [Benchmark]
        public void testMD5()
        {
            domain.Execute("UNIT_LIB", "testMD5");
        }


        [Benchmark]
        public void testMD5_100()
        {
            domain.Execute("UNIT_LIB", "testMD5_100");
        }

        [Benchmark]
        public void testFor1E()
        {
            domain.Execute("UNIT_LIB", "testFor1E");
        }

    }
}
