using BenchmarkDotNet.Running;
using System.Threading.Tasks;

namespace AuroraBenchmark
{
    internal class Program
    {
        private static async Task Main(string[] args)
        {


            BenchmarkRunner.Run<ScriptBenchmark>();

        }
    }
}
