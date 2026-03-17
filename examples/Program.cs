using AuroraScript;
using AuroraScript.Core;
using AuroraScript.Runtime;
using AuroraScript.Runtime.Types;
using System;
using System.Diagnostics;
using System.Text;
using System.Threading;
using System.Threading.Tasks;


namespace Examples
{


    public class Program
    {
        private static readonly EngineOptions engineOptions = EngineOptions.Default
            .WithBaseDirectory("tests")
            .WithConsoleStdOut(Console.Out)
            .WithConsoleErrorOut(Console.Error)
            .WithDateTimeFormat("yyyy-MM-dd HH:mm:ss")
            .WithAssemblyOut("123.dll")
            .WithEnableConfused(false)
            .WithCompilationMode(CompilationMode.Persistence)
            .WithOptimizeOption(OptimizeOptions.Release);

        private static readonly AuroraEngine engine = new AuroraEngine(engineOptions);
        private static readonly UserState userState = new UserState();


        private static void GlobalConfiguration(ScriptGlobal g)
        {
            g.Define("PI", new NumberValue(Math.PI), writeable: false, enumerable: true);
            g.Define("GIVE", new BondingFunction(Functions.GIVE), false, true);
            g.Define("CREATE_TIMER", new BondingFunction(Functions.CREATE_TIMER));
            g.Define("INPUT_NUMBER", new BondingFunction(Functions.CLIENT_INPUT_NUMBER), false, true);
            g.Define("md5_native", new BondingFunction(Functions.MD5_NATIVE), false, true);
            var fo = new TestObject();
            g.SetPropertyValue("fo", fo);
        }
        public static async Task Main()
        {
            Thread.CurrentThread.Priority = ThreadPriority.AboveNormal;
            Console.WriteLine("Loaded Assemblies:");
            foreach (var a in AppDomain.CurrentDomain.GetAssemblies())
            {
                if (a.FullName.Contains("Aurora")) Console.WriteLine($"- {a.FullName}");
            }
            engine.RegisterType<TestObject>();
            engine.RegisterType(typeof(Math), "Math2");
            try
            {
                var sources = engine.SearchAllFileSource(Encoding.UTF8);
                var s1 = engine.MemorySource("mmmmm1.as", "console.log('qwertyuiop');");
                await engine.BuildAsync([s1, .. sources]);
                Test();
            }
            catch (AuroraCompileReportException ex)
            {
                Console.WriteLine($"BuildAsync failed: count {ex.Errors.Count}");
                foreach (var error in ex.Errors)
                {
                    Console.WriteLine($"    - {error}");
                }
                Console.WriteLine(ex.ToString());
                return;
            }
            catch (AuroraRuntimeException ex)
            {
                Console.WriteLine(ex.ToString());
                return;
            }




            for (int i = 0; i < 10; i++)
            {
                GC.Collect();
                GC.WaitForPendingFinalizers();
                GC.Collect();
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

            // version 1
            domain.DynamicPatch(engine.MemorySource("testPatch", "func good(){ console.log('version:1'); }"), HotPatchType.Replace);
            domain.Execute("testPatch", "good");
            // version 2
            domain.DynamicPatch(engine.MemorySource("testPatch", "func good(){ console.log('version:2'); }"), HotPatchType.Replace);
            domain.Execute("testPatch", "good");
            // version 3
            domain.DynamicPatch(engine.MemorySource("testPatch", "func good(){ console.log('version:3'); }"), HotPatchType.Replace);
            domain.Execute("testPatch", "good");


            // 1. Initial Load
            Console.WriteLine("[1] Initial Load");
            domain.DynamicPatch(engine.MemorySource("test.as", "@module(test);import l123 from 'l123'; func hello() { return 'v1'; } var x = 10;"), HotPatchType.Incremental | HotPatchType.IgnoreDepends);
            var r1 = domain.Execute("test", "hello");
            var x1 = domain.GetModule("test").GetPropertyValue("x");
            Console.WriteLine($"hello() -> {r1}, x -> {x1}");

            // 2. Incremental Patch (Method Replacement)
            Console.WriteLine("[2] Incremental Patch (Method Replacement)");
            domain.DynamicPatch(engine.MemorySource("test.as", "@module(test); func hello() { return 'v2'; }"), HotPatchType.Incremental);
            var r2 = domain.Execute("test", "hello");
            var x2 = domain.GetModule("test").GetPropertyValue("x");
            Console.WriteLine($"hello() -> {r2}, x -> {x2} (x should still be 10)");

            // 3. Incremental Patch (Add Method & Property)
            Console.WriteLine("[3] Incremental Patch (Add Method & Property)");
            domain.DynamicPatch(engine.MemorySource("test.as", "@module(test); func world() { return 'world'; } var y = 20;"), HotPatchType.Incremental);
            var rw = domain.Execute("test", "world");
            var y3 = domain.GetModule("test").GetPropertyValue("y");
            Console.WriteLine($"world() -> {rw}, y -> {y3}");

            // 4. Replace Patch
            Console.WriteLine("\n[4] Replace Patch");
            domain.GetModule("l123").SetPropertyValue("FValue", NumberValue.Of(128.456));
            domain.DynamicPatch(engine.MemorySource("test.as", "@module(test); import l123 from 'l123'; func reset() { return 'reset'; }"), HotPatchType.Replace);
            try
            {
                domain.Execute("test", "hello");
                Console.WriteLine("Error: hello() should not exist after Replace!");
            }
            catch (Exception ex)
            {
                Console.WriteLine("hello() is gone (Expected)");
            }
            finally
            {

            }
            var rr = domain.Execute("test", "reset");
            Console.WriteLine($"reset() -> {rr}");

            Console.WriteLine("\n=== Verification Complete ===");




        }


        private static void Test()
        {
            var domain = engine.CreateDomain(GlobalConfiguration, userState);


            TestHotPatch(domain);

            BenchmarkScript(domain, "UNIT_LIB", "testHotPatch");

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
            Console.WriteLine("Verification complete!");


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
            catch (Exception ex)
            {
                _ex = ex;
            }
            finally
            {
                var useTime = stopwatch.ElapsedMilliseconds;

                var afterAlloc = GC.GetAllocatedBytesForCurrentThread();
                var allocatedBytes = afterAlloc - beforeAlloc;
                WriteBenchmarkResult(module, method, _ex == null, useTime, allocatedBytes / 1024.0);
            }





        }
        private static void WriteBenchmarkResult(string module, string method, Boolean status, double elapsedMs, double allocatedKb)
        {
            var originalColor = Console.ForegroundColor;
            Console.Write($"{module,-12} | {method,-24} | ");
            Console.ForegroundColor = status ? ConsoleColor.Green : ConsoleColor.Red;
            Console.Write($"{status,-12}");
            Console.ForegroundColor = originalColor;
            Console.WriteLine($" | {elapsedMs,10:F3} ms | {allocatedKb,10:F2} KB");
        }


        private static void RunAndReportUnitTests(ScriptDomain domain)
        {
            var context = domain.Execute("UNIT_LIB", "testAllUnits");
            if (ScriptDatum.TryGetAnyObject(context, out var summary))
            {
                if (summary.GetPropertyValue("failedCases") is ScriptArray failedCases)
                {
                    for (int i = 0; i < failedCases.Length; i++)
                    {
                        if (failedCases.GetElement(i).Object is ScriptObject failedCase)
                        {
                            var name = failedCase.GetPropertyValue("name");
                            var checks = failedCase.GetPropertyValue("checks");
                            ;
                            Console.WriteLine($"  ✖ {name} (checks: {checks})");

                            if (failedCase.GetPropertyValue("failures") is ScriptArray failures)
                            {
                                for (int j = 0; j < failures.Length; j++)
                                {
                                    if (failures.GetElement(j).Object is ScriptObject failure)
                                    {
                                        var message = failure.GetPropertyValue("message");
                                        var actual = failure.GetPropertyValue("actual");
                                        var expected = failure.GetPropertyValue("expected");
                                        Console.WriteLine($"      - {message}");
                                        Console.WriteLine($"        actual: {actual}");
                                        Console.WriteLine($"        expected: {expected}");
                                    }
                                }
                            }
                        }
                    }
                }

            }


            Console.WriteLine(context);
        }
    }


}
