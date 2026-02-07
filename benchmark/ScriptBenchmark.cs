using AuroraScript.Runtime;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Mathematics;
using BenchmarkDotNet.Order;
using System.Runtime.CompilerServices;


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


        [Benchmark]
        public void TestDatum()
        {
            ScriptDatum a = ScriptDatum.FromNumber(123);
            ScriptDatum b = ScriptDatum.FromNumber(123);
            for (int i = 0; i < 10000000; i++)
            {
                ScriptDatum c = Add2(a, b);
            }
        }

        [Benchmark]
        public void TestDatumRef()
        {

            ScriptDatum a = ScriptDatum.FromNumber(123);
            ScriptDatum b = ScriptDatum.FromNumber(123);
            for (int i = 0; i < 10000000; i++)
            {
                ScriptDatum c = default;
                Add(in a, in b, ref c);
            }
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Add(in ScriptDatum a, in ScriptDatum b, ref ScriptDatum result)
        {
            if (a.Kind == ValueKind.Number && b.Kind == ValueKind.Number)
            {
                ScriptDatum.WriteAsNumber(ref result, a.Number + b.Number);
            }
            else if (ScriptDatum.TryToNumber(in a, out var aa) && ScriptDatum.TryToNumber(in b, out var bb))
            {
                ScriptDatum.WriteAsNumber(ref result, aa + bb);
            }
            else
            {
                ScriptDatum.WriteAsNumber(ref result, double.NaN);
            }
        }



        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ScriptDatum Add2(ScriptDatum a, ScriptDatum b)
        {
            if (a.Kind == ValueKind.Number && b.Kind == ValueKind.Number)
            {
                return ScriptDatum.FromNumber(a.Number + b.Number);
            }
            else if (ScriptDatum.TryToNumber(in a, out var aa) && ScriptDatum.TryToNumber(in b, out var bb))
            {
                return ScriptDatum.FromNumber(aa + bb);
            }
            else
            {
                return ScriptDatum.FromNumber(double.NaN);
            }
        }



    }
}
