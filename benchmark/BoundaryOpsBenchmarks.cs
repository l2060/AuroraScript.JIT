using AuroraScript.Runtime;
using AuroraScript.Runtime.Types;
using BenchmarkDotNet.Attributes;
using System;

namespace AuroraBenchmark;

[MemoryDiagnoser]
[DisassemblyDiagnoser(maxDepth: 3)]
public class BoundaryOpsBenchmarks
{
    private const int Count = 512;
    private const int SpreadCount = 128;
    private readonly ScriptDatum[] _indices = new ScriptDatum[32];
    private readonly double[] _uintValues = new double[Count];
    private readonly double[] _predicateValues = new double[Count];
    private readonly ScriptDatum[] _longValues = new ScriptDatum[Count];
    private readonly ScriptArray _array = new(32);
    private readonly ScriptArray _source = new(SpreadCount);
    private readonly ScriptArray _target = new(SpreadCount);
    private ScriptDatum _arrayDatum, _sourceDatum, _objectDatum;
    private ScriptDatum[] _arguments;

    [GlobalSetup]
    public void Setup()
    {
        for (var i = 0; i < _indices.Length; i++) _array[i] = _indices[i] = ScriptDatum.FromNumber(i);
        for (var i = 0; i < SpreadCount; i++) _source[i] = ScriptDatum.FromNumber(i);
        var random = new Random(42);
        double[] predicates = [0d, -0d, -1d, 1d, 1.25d, uint.MaxValue, 4294967296d, double.NaN, double.PositiveInfinity];
        for (var i = 0; i < Count; i++)
        {
            _uintValues[i] = i % 8 == 0 ? 0 : (uint)random.NextInt64(0, 4294967296L);
            _predicateValues[i] = predicates[i % predicates.Length];
            _longValues[i] = ScriptDatum.FromNumber(9007199254740992d + 2d * i);
        }
        _arrayDatum = ScriptDatum.FromArray(_array);
        _sourceDatum = ScriptDatum.FromArray(_source);
        var obj = new ScriptObject();
        obj.Define("1.5", ScriptDatum.FromNumber(5));
        obj.Define("7", ScriptDatum.FromNumber(7));
        _objectDatum = ScriptDatum.FromObject(obj);
        _arguments = CallOps.RentArguments(SpreadCount);
    }

    [GlobalCleanup]
    public void Cleanup() => CallOps.ReturnArguments(_arguments, SpreadCount);

    [Benchmark(OperationsPerInvoke = Count)]
    public double ReadDynamicArray()
    {
        double result = 0;
        for (var i = 0; i < Count; i++) result += ObjectOps.GetElement(_arrayDatum, _indices[i & 31]).Number;
        return result;
    }

    [Benchmark(OperationsPerInvoke = Count)]
    public void WriteDynamicArray()
    {
        for (var i = 0; i < Count; i++) ObjectOps.SetElement(_arrayDatum, _indices[i & 31], _indices[(i + 1) & 31]);
    }

    [Benchmark(OperationsPerInvoke = Count)]
    public double ReadNumberProperty()
    {
        double result = 0;
        for (var i = 0; i < Count; i++) result += ObjectOps.GetElementNumber(_objectDatum, 1.5).Number;
        return result;
    }

    [Benchmark(OperationsPerInvoke = Count)]
    public void WriteIntProperty()
    {
        for (var i = 0; i < Count; i++) ObjectOps.SetElementIndex(_objectDatum, 7, _indices[i & 31]);
    }

    [Benchmark(OperationsPerInvoke = SpreadCount)]
    public int ArraySpread()
    {
        _target.SetLength(0);
        ObjectOps.SpreadInto(_target, _sourceDatum);
        return _target.Length;
    }

    [Benchmark(OperationsPerInvoke = SpreadCount)]
    public int ArgumentSpread()
    {
        var count = 0;
        _arguments = CallOps.AppendSpread(_arguments, ref count, _sourceDatum);
        return count;
    }

    [Benchmark(OperationsPerInvoke = Count)]
    public uint CheckUInt32Number()
    {
        uint result = 0;
        for (var i = 0; i < Count; i++) result ^= TypeCheckOps.CheckUInt32Number(_uintValues[i]);
        return result;
    }

    [Benchmark(OperationsPerInvoke = Count)]
    public int IsUInt32()
    {
        var result = 0;
        for (var i = 0; i < Count; i++) if (TypeCheckOps.IsUInt32(_predicateValues[i])) result++;
        return result;
    }

    [Benchmark(OperationsPerInvoke = Count)]
    public long CheckInt64Datum()
    {
        long result = 0;
        for (var i = 0; i < Count; i++) result ^= TypeCheckOps.CheckInt64Value(_longValues[i]);
        return result;
    }

    [Benchmark(OperationsPerInvoke = Count)]
    public ulong CheckUInt64Datum()
    {
        ulong result = 0;
        for (var i = 0; i < Count; i++) result ^= TypeCheckOps.CheckUInt64Value(_longValues[i]);
        return result;
    }
}
