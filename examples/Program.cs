using AuroraScript;
using AuroraScript.Core;
using AuroraScript.Runtime;
using AuroraScript.Runtime.Serialization;
using AuroraScript.Runtime.Types;
using AuroraScript.Source;
using System;
using System.Diagnostics;
using System.IO;
using System.Runtime;
using System.Text;
using System.Threading;
using System.Threading.Tasks;


namespace Examples
{


    public class Program
    {
        private static readonly string scriptDirectory = Path.Combine(AppContext.BaseDirectory, "tests");
        // load script from memory, override file system script
        private static readonly IScriptSourceResolver memorySource = ScriptSources.Memory(scriptDirectory).Add("seed.as", "console.log('load from memory overlay'); export func go(){ console.log('seed from memory...');  }");
        // load script from file system
        private static readonly IScriptSourceResolver fileSystemSource = ScriptSources.FileSystem(scriptDirectory, Encoding.UTF8);


        private static readonly EngineOptions engineOptions = EngineOptions.Default

        .WithBuiltIns(builtIns => builtIns.Add(BuiltInModules.FileSystem).Add(BuiltInModules.HttpClient))
        .WithCompiler(compiler =>
        {
            compiler.SourceResolver = ScriptSources.Composite(memorySource, fileSystemSource);
            compiler.MaxDegreeOfParallelism = 0;
            compiler.ExtName = "as";
            compiler.Mode = CompilationMode.Persistence;
            compiler.WithNativeTypes(typeof(Vec2), typeof(StatsSupport));
        })
        .WithOutput(output =>
        {
            output.AssemblyFile = "123.dll";
            output.Confused = false;
        })
        .WithRuntime(runtime =>
        {
            runtime.ConsoleStdOut = Console.Out;
            runtime.ConsoleErrorOut = Console.Error;
            runtime.JsonSerializer = ScriptJsonSerializer.Default;
            runtime.DateTimeFormat = "yyyy-MM-dd HH:mm:ss";
            runtime.HotReload = true;
            runtime.StringPooling = StringPoolingStrategy.None;
        })
        .WithOptimization(optimization =>
        {
            optimization.StackTrace = false;
            optimization.ModuleConstInlining = true;
            optimization.Level = OptimizeOptions.Release;
        });




        private static readonly AuroraEngine engine = new AuroraEngine(engineOptions);
        private static readonly UserState userState = new UserState();


        private static void GlobalConfiguration(ScriptGlobal g)
        {
            g.Define("PI", ScriptDatum.FromNumber(Math.PI), writeable: false, enumerable: true);
            g.Define("ENABLE_HOT_RELOAD", ScriptDatum.FromBoolean(engineOptions.Runtime.EnableHotReload), writeable: false, enumerable: true);
            g.Define("GIVE", ScriptDatum.FromBonding(Functions.GIVE), false, true);
            g.Define("CREATE_TIMER", ScriptDatum.FromBonding(Functions.CREATE_TIMER));
            g.Define("INPUT_NUMBER", ScriptDatum.FromBonding(Functions.CLIENT_INPUT_NUMBER), false, true);
            g.Define("md5_native", ScriptDatum.FromBonding(Functions.MD5_NATIVE), false, true);

            g.Define("APP_VERSION", ScriptDatum.FromString("1.2.3"), false, true);
            g.Define("ONLINE_TOTAL", ScriptDatum.FromNumber(0));
            var fo = new TestObject();
            g.SetPropertyValue("fo", fo);

        }
        public static async Task Main()
        {
            Thread.CurrentThread.Priority = ThreadPriority.AboveNormal;
            engine.RegisterType<TestObject>();
            engine.RegisterType(typeof(Math), "Math2");
            try
            {
                var time = Stopwatch.StartNew();
                await engine.BuildAsync();
                var elapsed = time.ElapsedMilliseconds;
                Console.WriteLine($"BuildAsync {elapsed}ms");
                Console.WriteLine();
                Test();
            }
            catch (AuroraCompilationException ex)
            {
                Console.WriteLine($"BuildAsync failed: count {ex.Diagnostics.Count}");
                foreach (var error in ex.Diagnostics)
                {
                    Console.WriteLine($"    - {error}");
                }
                Console.WriteLine(ex.ToString());
            }
            catch (AuroraRuntimeException ex)
            {
                Console.WriteLine(ex.ToString());
            }

            GCSettings.LargeObjectHeapCompactionMode = GCLargeObjectHeapCompactionMode.CompactOnce;
            GC.Collect(GC.MaxGeneration, GCCollectionMode.Forced, blocking: true);
            // CompactOnce执行一次后自动切回Default


            for (int i = 0; i < 10; i++)
            {
                GC.Collect(GC.MaxGeneration, GCCollectionMode.Forced, true, true);
                GC.WaitForPendingFinalizers();
                GC.Collect(GC.MaxGeneration, GCCollectionMode.Forced, true, true);
                Thread.Sleep(100);
            }
            Console.ReadLine();
        }

        /// <summary>
        /// 动态补丁测试
        /// </summary>
        /// <param name="domain"></param>
        private static void TestHotPatch(ScriptDomain domain)
        {
            //  domain = engine.CreateEmptyDomain(null);
            if (!engineOptions.Runtime.EnableHotReload)
            {
                Console.WriteLine("Engine Options HotReload = false : Skip HotPatch Test Unit.");
                return;
            }
            // version 1
            domain.ReplacePatch(PatchPath("testPatch.as"), "@module(testPatch); func good(){ console.log('version:1'); }");
            domain.Execute("testPatch", "good");
            // version 2
            domain.ReplacePatch(PatchPath("testPatch.as"), "@module(testPatch); func good(){ console.log('version:2'); }");
            domain.Execute("testPatch", "good");
            // version 3
            domain.ReplacePatch(PatchPath("testPatch.as"), "@module(testPatch); func good(){ console.log('version:3'); }");
            domain.Execute("testPatch", "good");


            // 1. Initial Load
            Console.WriteLine("[1] Initial Load");
            domain.IncrementalPatch(
                PatchPath("test.as"),
                "@module(test);import l123 from 'l123'; func hello() { return 'v1'; } var x = 10;",
                ignoreDepends: true);
            var r1 = domain.Execute("test", "hello");
            var x1 = domain.GetModule("test").GetPropertyValue("x");
            Console.WriteLine($"hello() -> {r1}, x -> {x1}");

            // 2. Incremental Patch (Method Replacement)
            Console.WriteLine("[2] Incremental Patch (Method Replacement)");
            domain.IncrementalPatch(
                PatchPath("test.as"),
                "@module(test); func hello() { return 'v2'; }");
            var r2 = domain.Execute("test", "hello");
            var x2 = domain.GetModule("test").GetPropertyValue("x");
            Console.WriteLine($"hello() -> {r2}, x -> {x2} (x should still be 10)");

            // 3. Incremental Patch (Add Method & Property)
            Console.WriteLine("[3] Incremental Patch (Add Method & Property)");
            domain.IncrementalPatch(
                PatchPath("test.as"),
                "@module(test); func world() { return 'world'; } var y = 20;");
            var rw = domain.Execute("test", "world");
            var y3 = domain.GetModule("test").GetPropertyValue("y");
            Console.WriteLine($"world() -> {rw}, y -> {y3}");

            // 4. Replace Patch
            Console.WriteLine("\n[4] Replace Patch");
            domain.GetModule("test").SetPropertyValue("FValue", NumberValue.Of(128.456));
            domain.ReplacePatch(
                PatchPath("test.as"),
                "@module(test); import l123 from 'l123'; func reset() { return 'reset'; }");
            //try
            //{
            //    domain.Execute("test", "hello");
            //    Console.WriteLine("Error: hello() should not exist after Replace!");
            //}
            //catch (Exception)
            //{
            //    Console.WriteLine("hello() is gone (Expected)");
            //}
            //finally
            //{

            //}
            var rr = domain.Execute("test", "reset");
            Console.WriteLine($"reset() -> {rr}");

            Console.WriteLine("\n=== Verification Complete ===");




        }

        private static string PatchPath(string modulePath)
        {
            return Path.GetFullPath(Path.Combine(scriptDirectory, modulePath));
        }


        private static void Test()
        {
            var domain = engine.CreateDomain(GlobalConfiguration, userState);

            var td = new AuroraTypedDocument(engine);
            var doc = File.ReadAllText("map.tdoc");
            for (int i = 0; i < 10; i++)
            {
                td.Deserialize(doc);
            }

            Benchmark("AuroraTypedDocument.Deserialize", () =>
            {
                var p = td.Deserialize(doc);
            });






            TestHotPatch(domain);
            TestCompileBlock(domain);

            if (engineOptions.Runtime.EnableHotReload)
            {
                BenchmarkScript(domain, "UNIT_LIB", "testHotPatch");
            }
            else
            {
                Console.WriteLine("Engine Options HotReload = false : Skip Script HotPatch Test Unit.");
            }

            var context = domain.Execute("MD5_LIB", "MD5", null, StringValue.Of("12345"));
            Console.WriteLine($"MD5('12345') = {ScriptDatum.ToString(context)}");




            Console.WriteLine("--- Exception Stack Trace Test ---");
            try
            {
                domain.Execute("UNIT_LIB", "deepInterruption");
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine(ex.ToString());
                Console.ResetColor();
            }
            Console.WriteLine("--- End Exception Stack Trace Test ---");




            // Test closure inheritance from l123.as
            // closure1 returns { a, b }
            var result = domain.Execute("MAIN", "main");
            Console.WriteLine($"closure1 result type: {result.Kind}");
            //RunAndReportUnitTests(domain);
            //BenchmarkScript(domain, "DEBUG_TEST", "main");
            BenchmarkScript(domain, "UNIT_LIB", "testEmpty");
            BenchmarkScript(domain, "UNIT_LIB", "testMD5");
            BenchmarkScript(domain, "UNIT_LIB", "testClosure");
            BenchmarkScript(domain, "TIMER_LIB", "testCallback");
            BenchmarkScript(domain, "UNIT_LIB", "testInput");
            BenchmarkScript(domain, "UNIT_LIB", "testDatetime");
            BenchmarkScript(domain, "UNIT_LIB", "testDeConstruct");
            BenchmarkScript(domain, "UNIT_LIB", "testRegex");
            BenchmarkScript(domain, "UNIT_LIB", "testJson");
            BenchmarkScript(domain, "UNIT_LIB", "testClrType", new StringValue("PI"), new NumberValue(Math.PI));

            BenchmarkScript(domain, "BUILTIN", "testFileSystem");
            BenchmarkScript(domain, "BUILTIN", "testHttpGet");




            BenchmarkScript(domain, "UNIT_LIB", "testIterator");
            BenchmarkScript(domain, "UNIT_LIB", "test");
            BenchmarkScript(domain, "UNIT_LIB", "testClrFunc");
            BenchmarkScript(domain, "UNIT_LIB", "testClrFunc");
            BenchmarkScript(domain, "UNIT_LIB", "testDeconstruction");
            BenchmarkScript(domain, "UNIT_LIB", "testProxy");
            BenchmarkScript(domain, "UNIT_LIB", "testArray", new NumberValue(1_000_000));
            BenchmarkScript(domain, "UNIT_LIB", "testPeculiarity", new NumberValue(1_000_000), BooleanValue.True);
            BenchmarkScript(domain, "UNIT_LIB", "benchmarkNumbers", NumberValue.Of(1_000_000));
            BenchmarkScript(domain, "UNIT_LIB", "benchmarkArrays", NumberValue.Of(1_000_000));
            BenchmarkScript(domain, "UNIT_LIB", "benchmarkClosure", NumberValue.Of(1_000_000));
            BenchmarkScript(domain, "UNIT_LIB", "benchmarkObjects", NumberValue.Of(200_000));
            BenchmarkScript(domain, "UNIT_LIB", "benchmarkStrings", NumberValue.Of(1_000_000));

            BenchmarkScript(domain, "UNIT_LIB", "testFor", new NumberValue(10000_0000));
            BenchmarkScript(domain, "UNIT_LIB", "testMD5_1000");
            BenchmarkScript(domain, "UNIT_LIB", "testDraw");
            BenchmarkScript(domain, "UNIT_LIB", "testFor", new NumberValue(10000_0000));

            BenchmarkScript(domain, "ASTAR", "runExample");
            BenchmarkScript(domain, "ASTAR", "runExample");

            BenchmarkScript(domain, "UNIT_LIB", "externalDeclare");

            BenchmarkScript(domain, "PERF_BENCH", "run");



            Console.WriteLine("Verification complete!");


        }






        private static void TestCompileBlock(ScriptDomain domain)
        {
            var block = engine.CompileBlock("""
function clamp(v, min, max) {
    if (v < min) return min;
    if (v > max) return max;
    return v;
}

return clamp(x, 0, 100) + PI;
""", new CompileBlockOptions
            {
                Parameters = ["x"],
                SourceName = "examples/compile-block.as"
            });

            var value = block.Invoke(domain, ScriptDatum.FromNumber(125));
            Console.WriteLine($"CompileBlock clamp(125) + PI = {ScriptDatum.ToString(value)}");
            BenchmarkBlock(domain, block, "CompileBlockClamp", ScriptDatum.FromNumber(125));
        }

        private static void BenchmarkScript(ScriptDomain domain, string module, string method, params ScriptObject[] args)
        {
            var beforeAlloc = GC.GetAllocatedBytesForCurrentThread();
            Exception _ex = null;
            Stopwatch stopwatch = Stopwatch.StartNew();
            try
            {
                domain.Execute(module, method, args);
            }
            catch (AuroraRuntimeException ex)
            {
                _ex = ex;
                Console.WriteLine(ex);
            }
            catch (Exception ex)
            {
                _ex = ex;

            }
            finally
            {
                var useTime = stopwatch.Elapsed.TotalMilliseconds;

                var afterAlloc = GC.GetAllocatedBytesForCurrentThread();
                var allocatedBytes = afterAlloc - beforeAlloc;
                WriteBenchmarkResult(module, method, _ex == null, useTime, allocatedBytes / 1024.0);
            }





        }

        private static void BenchmarkBlock(ScriptDomain domain, CompiledBlock block, string name, params ScriptDatum[] args)
        {
            var beforeAlloc = GC.GetAllocatedBytesForCurrentThread();
            Exception _ex = null;
            Stopwatch stopwatch = Stopwatch.StartNew();
            try
            {
                block.Invoke(domain, args);
            }
            catch (Exception ex)
            {
                _ex = ex;
            }
            finally
            {
                var useTime = stopwatch.Elapsed.TotalMilliseconds;
                var afterAlloc = GC.GetAllocatedBytesForCurrentThread();
                var allocatedBytes = afterAlloc - beforeAlloc;
                WriteBenchmarkResult("BLOCK", name, _ex == null, useTime, allocatedBytes / 1024.0);
            }
        }

        private static void WriteBenchmarkResult(string module, string method, Boolean status, double elapsedMs, double allocatedKb)
        {
            var originalColor = Console.ForegroundColor;
            Console.Write($"{module,-12} | {method,-24} | ");
            Console.ForegroundColor = status ? ConsoleColor.Green : ConsoleColor.Red;
            Console.Write($"{status,-12}");
            Console.ForegroundColor = originalColor;
            Console.WriteLine($" | {elapsedMs,10:F3} ms | {allocatedKb,10:F2} KiB");
        }


        private static void Benchmark(String name, Action callback)
        {
            var beforeAlloc = GC.GetAllocatedBytesForCurrentThread();
            Exception _ex = null;
            Stopwatch stopwatch = Stopwatch.StartNew();
            try
            {
                callback();
            }
            catch (AuroraRuntimeException ex)
            {
                _ex = ex;
                Console.WriteLine(ex);
            }
            catch (Exception ex)
            {
                _ex = ex;

            }
            finally
            {
                var useTime = stopwatch.Elapsed.TotalMilliseconds;
                var afterAlloc = GC.GetAllocatedBytesForCurrentThread();
                var allocatedBytes = afterAlloc - beforeAlloc;
                var originalColor = Console.ForegroundColor;
                Console.Write($"{name,-24} | ");
                Console.ForegroundColor = _ex == null ? ConsoleColor.Green : ConsoleColor.Red;
                Console.Write($"{_ex == null,-12}");
                Console.ForegroundColor = originalColor;
                Console.WriteLine($" | {useTime,10:F3} ms | {allocatedBytes / 1024.0,10:F2} KiB");
            }





        }
    }


}
