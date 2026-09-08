using AuroraScript.Runtime;
using AuroraScript.Runtime.Types;
using BenchmarkDotNet.Attributes;

namespace AuroraBenchmark;

[MemoryDiagnoser]
[DisassemblyDiagnoser(maxDepth: 3)]
public class SafeOpsBenchmarks
{
    [Params(32, 256)]
    public int Count { get; set; }

    private ScriptDatum _array, _packed, _text, _needle, _wrapper, _null;
    private ScriptDatum _missing;

    [GlobalSetup]
    public void Setup()
    {
        var array = new ScriptArray(Count);
        var packed = new ScriptInt32Array(Count);
        for (var i = 0; i < Count; i++)
        {
            array[i] = ScriptDatum.FromNumber(i);
            packed.SetElement(i, i);
        }
        _array = ScriptDatum.FromArray(array);
        _packed = ScriptDatum.FromObject(packed);
        _text = ScriptDatum.FromString(new string('\u4e2d', Count));
        _needle = ScriptDatum.FromString("\u6587");
        _wrapper = ScriptDatum.FromObject(new ScriptInt32Array(1));
        _null = ScriptDatum.Null;
        _missing = ScriptDatum.FromNumber(-1);
    }

    [Benchmark]
    public bool ArrayMiss() => ObjectOps.Includes(_array, _missing);

    [Benchmark]
    public bool PackedMiss() => ObjectOps.Includes(_packed, _missing);

    [Benchmark]
    public bool CharacterMiss() => ObjectOps.Includes(_text, _needle);

    [Benchmark]
    public ScriptInt32Array CheckedWrapper() => PackedArrayBoundaryOps.ToInt32Array(_wrapper);

    [Benchmark]
    public ScriptInt32Array NullWrapper() => PackedArrayBoundaryOps.ToInt32Array(_null);
}
