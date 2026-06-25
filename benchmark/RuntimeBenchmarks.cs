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

namespace AuroraBenchmark
{
    [MemoryDiagnoser]
    [ShortRunJob]
    [MarkdownExporter, JsonExporter, CsvExporter]
    [MinColumn, MaxColumn, MeanColumn, MedianColumn]
    [GroupBenchmarksBy(BenchmarkLogicalGroupRule.ByCategory)]
    [Orderer(SummaryOrderPolicy.FastestToSlowest)]
    [RankColumn(NumeralSystem.Arabic)]
    [CategoriesColumn]
    public class RuntimeBenchmarks
    {
#pragma warning disable CS8618
        private AuroraEngine engine;
        private ScriptDomain domain;
        private ScriptDatum iterationsDatum;
#pragma warning restore CS8618

        [Params(1000, 10000)]
        public int Iterations { get; set; } = 1000;

        [GlobalSetup]
        public async Task Setup()
        {
            var scriptDirectory = Path.Combine(AppContext.BaseDirectory, "scripts");
            var options = EngineOptions.Default
                .WithCompiler(compiler => compiler.WithDirectory(scriptDirectory))
                .WithRuntime(runtime => runtime.ConsoleStdOut = TextWriter.Null)
                .WithRuntime(runtime => runtime.ConsoleErrorOut = TextWriter.Null)
                .WithRuntime(runtime => runtime.DateTimeFormat = "yyyy-MM-dd HH:mm:ss")
                .WithOutput(output => output.AssemblyFile = Path.Combine(AppContext.BaseDirectory, "runtime-benchmark.dll"))
                .WithOutput(output => output.Confused = false)
                .WithCompiler(compiler => compiler.Mode = CompilationMode.Dynamic)
                .WithOptimization(optimization => optimization.Level = OptimizeOptions.Release);

            engine = new AuroraEngine(options);
            engine.RegisterType<HostObject>();
            await engine.BuildAsync(engine.SearchAllFileSource(Encoding.UTF8));
            domain = engine.CreateDomain(global => global.SetPropertyValue("host", new HostObject()));
            iterationsDatum = ScriptDatum.FromNumber(Iterations);
        }

        [BenchmarkCategory("domain")]
        [Benchmark]
        public ScriptDomain CreateDomain()
        {
            return engine.CreateDomain(global => global.SetPropertyValue("host", new HostObject()));
        }

        [BenchmarkCategory("call")]
        [Benchmark]
        public ScriptDatum EmptyCall()
        {
            return domain.Execute("RUNTIME_BENCH", "emptyCall");
        }

        [BenchmarkCategory("call")]
        [Benchmark]
        public ScriptDatum FunctionCallLoop()
        {
            return Execute("functionCallLoop");
        }

        [BenchmarkCategory("call")]
        [Benchmark]
        public ScriptDatum ModuleCallLoop()
        {
            return Execute("moduleCallLoop");
        }

        [BenchmarkCategory("loop")]
        [Benchmark]
        public ScriptDatum NumericLoop()
        {
            return Execute("numericLoop");
        }

        [BenchmarkCategory("object")]
        [Benchmark]
        public ScriptDatum ObjectCreateSetGet()
        {
            return Execute("objectCreateSetGet");
        }

        [BenchmarkCategory("object")]
        [Benchmark]
        public ScriptDatum ObjectForIn()
        {
            return Execute("objectForIn");
        }

        [BenchmarkCategory("array")]
        [Benchmark]
        public ScriptDatum ArrayPushIndex()
        {
            return Execute("arrayPushIndex");
        }

        [BenchmarkCategory("array")]
        [Benchmark]
        public ScriptDatum ArrayLiteralIndex()
        {
            return Execute("arrayLiteralIndex");
        }

        [BenchmarkCategory("hashmap")]
        [Benchmark]
        public ScriptDatum HashMapSetGet()
        {
            return Execute("hashMapSetGet");
        }

        [BenchmarkCategory("string")]
        [Benchmark]
        public ScriptDatum StringConcat()
        {
            return Execute("stringConcat");
        }

        [BenchmarkCategory("string")]
        [Benchmark]
        public ScriptDatum StringBufferAppend()
        {
            return Execute("stringBufferAppend");
        }

        [BenchmarkCategory("json")]
        [Benchmark]
        public ScriptDatum JsonStringify()
        {
            return Execute("jsonStringify");
        }

        [BenchmarkCategory("json")]
        [Benchmark]
        public ScriptDatum JsonParse()
        {
            return Execute("jsonParse");
        }

        [BenchmarkCategory("json")]
        [Benchmark]
        public ScriptDatum JsonRoundTrip()
        {
            return Execute("jsonRoundTrip");
        }

        [BenchmarkCategory("regex")]
        [Benchmark]
        public ScriptDatum RegexMatchAll()
        {
            return Execute("regexMatchAll");
        }

        [BenchmarkCategory("closure")]
        [Benchmark]
        public ScriptDatum ClosureInvoke()
        {
            return Execute("closureInvoke");
        }

        [BenchmarkCategory("clr")]
        [Benchmark]
        public ScriptDatum ClrPropertyGetSet()
        {
            return Execute("clrPropertyGetSet");
        }

        [BenchmarkCategory("clr")]
        [Benchmark]
        public ScriptDatum ClrInstanceMethod()
        {
            return Execute("clrInstanceMethod");
        }

        [BenchmarkCategory("clr")]
        [Benchmark]
        public ScriptDatum ClrStaticMethod()
        {
            return Execute("clrStaticMethod");
        }

        [BenchmarkCategory("clr")]
        [Benchmark]
        public ScriptDatum ClrArrayArgument()
        {
            return Execute("clrArrayArgument");
        }

        private ScriptDatum Execute(string methodName)
        {
            return domain.Execute("RUNTIME_BENCH", methodName, iterationsDatum);
        }
    }
}
