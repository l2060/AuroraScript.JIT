using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;
using System;
using System.Reflection;
using System.Threading.Tasks;

namespace AuroraBenchmark
{
    internal class Program
    {
        private static async Task Main(string[] args)
        {

            //test().Wait();
            BenchmarkRunner.Run<ScriptBenchmark>();


        }


        static async Task test()
        {

            var s = new ScriptBenchmark();

            var type = typeof(ScriptBenchmark);
            var methods = type.GetMethods();
            try
            {
                await s.Setup();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }
            foreach (var method in methods)
            {
                var ca = method.GetCustomAttribute<BenchmarkAttribute>();
                if (ca != null)
                {
                    Console.WriteLine(method.Name);
                    method.Invoke(s, []);
                }
            }
            Console.ReadLine();

        }

    }
}
