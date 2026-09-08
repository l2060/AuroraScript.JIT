using AuroraScript.Runtime;
using BenchmarkDotNet.Attributes;
using System;

namespace AuroraBenchmark;

[MemoryDiagnoser]
[DisassemblyDiagnoser(maxDepth: 3)]
public class ScriptDatumBenchmarks
{
    private const int Count = 1024;
    private readonly int[] _integers = new int[Count];
    private readonly double[] _numbers = new double[Count];
    private readonly ScriptDatum[] _values = new ScriptDatum[Count];
    private readonly ScriptDatum[] _output = new ScriptDatum[Count];
    private readonly ScriptDatum[] _signed = new ScriptDatum[Count];
    private readonly ScriptDatum[] _unsigned = new ScriptDatum[Count];

    [GlobalSetup]
    public void Setup()
    {
        var random = new Random(42);
        for (var i = 0; i < Count; i++)
        {
            _integers[i] = i % 16 == 0 ? 0 : random.Next(int.MinValue, int.MaxValue);
            _numbers[i] = (i % 32) switch
            {
                0 => 0d,
                1 => -0d,
                2 => double.NaN,
                3 => double.Epsilon,
                _ => _integers[i] * 0.125,
            };
            _values[i] = ScriptDatum.FromNumber(_numbers[i]);
            _signed[i] = ScriptDatum.FromInt64(i);
            _unsigned[i] = ScriptDatum.FromUInt64((ulong)i);
        }
    }

    [Benchmark(OperationsPerInvoke = Count)]
    public void CreateInt32()
    {
        for (var i = 0; i < Count; i++) _output[i] = ScriptDatum.FromNumber(_integers[i]);
    }

    [Benchmark(OperationsPerInvoke = Count)]
    public void CreateUInt32()
    {
        for (var i = 0; i < Count; i++) _output[i] = ScriptDatum.FromNumber(unchecked((uint)_integers[i]));
    }

    [Benchmark(OperationsPerInvoke = Count)]
    public void CreateNumberInt64()
    {
        for (var i = 0; i < Count; i++) _output[i] = ScriptDatum.FromNumber((long)_integers[i]);
    }

    [Benchmark(OperationsPerInvoke = Count)]
    public void CreateDouble()
    {
        for (var i = 0; i < Count; i++) _output[i] = ScriptDatum.FromNumber(_numbers[i]);
    }

    [Benchmark(OperationsPerInvoke = Count)]
    public ulong ReadNumber()
    {
        ulong result = 0;
        for (var i = 0; i < Count; i++) result ^= BitConverter.DoubleToUInt64Bits(_values[i].Number);
        return result;
    }

    [Benchmark(OperationsPerInvoke = Count)]
    public int Truthiness()
    {
        var result = 0;
        for (var i = 0; i < Count; i++) if (ScriptDatum.IsTrue(_values[i])) result++;
        return result;
    }

    [Benchmark(OperationsPerInvoke = Count)]
    public ulong ConvertNumber()
    {
        ulong result = 0;
        for (var i = 0; i < Count; i++)
            if (ScriptDatum.TryToNumber(in _values[i], out var value)) result ^= BitConverter.DoubleToUInt64Bits(value);
        return result;
    }

    [Benchmark(OperationsPerInvoke = Count)]
    public int ExactIntegerEquality()
    {
        var result = 0;
        for (var i = 0; i < Count; i++) if (_signed[i].Equals(_unsigned[i])) result++;
        return result;
    }
}
