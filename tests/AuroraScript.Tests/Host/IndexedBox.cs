using AuroraScript.Hosting;
using AuroraScript.Runtime;
using AuroraScript.Runtime.Types;

namespace AuroraScript.Tests.Host;

[NativeType("IndexedBox")]
public sealed partial class IndexedBox : ScriptObject, IAuroraNativeIndexer
{
    private ScriptDatum _value;

    [Export]
    public IndexedBox() : base(NativePrototype) { }

    public ScriptDatum this[int index]
    {
        get => index == 0 ? _value : default;
        set { if (index == 0) _value = value; }
    }

    [Export("clear")]
    public void Clear() => _value = default;
    public ScriptDatum[] Collected { get; private set; } = [];
    public bool SawContext { get; private set; }

    [Export("collect")]
    public int Collect(ScriptContext context, ScriptDatum head, params ScriptDatum[] values)
    {
        SawContext = context != null;
        _value = head;
        Collected = values;
        return values.Length;
    }

    [Export("numbers")]
    public double Numbers(double first = 10, params double[] values)
    {
        foreach (var value in values) first += value;
        return first;
    }

}
