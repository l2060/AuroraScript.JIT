using AuroraScript.Runtime;
using AuroraScript.Runtime.Types;
using System;
using System.Collections.Generic;
using System.Globalization;
using Xunit;

namespace AuroraScript.Tests;

public sealed class ObjectOpsOptimizationTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void DynamicIndicesPreserveDispatchAndConversions(bool indexed)
    {
        ScriptDatum[] indices = [default, ScriptDatum.True, ScriptDatum.False, ScriptDatum.FromString("1"),
            ScriptDatum.FromString("1.5"), ScriptDatum.FromString("field"), ScriptDatum.FromString(""),
            ScriptDatum.FromInt64(long.MaxValue), ScriptDatum.FromUInt64(ulong.MaxValue),
            ScriptDatum.FromNumber(-1.25), ScriptDatum.FromNumber(-0d), ScriptDatum.NaN,
            ScriptDatum.FromNumber(double.PositiveInfinity), ScriptDatum.FromNumber(4294967296d)];
        foreach (var index in indices)
        {
            var old = CreateReceiver(indexed);
            var current = CreateReceiver(indexed);
            var oldDatum = ScriptDatum.FromObject(old);
            var currentDatum = ScriptDatum.FromObject(current);
            AssertSameResult(() => LegacyGet(oldDatum, index), () => ObjectOps.GetElement(currentDatum, index));
            AssertSameResult(() => LegacySet(oldDatum, index, 42), () => ObjectOps.SetElement(currentDatum, index, 42));
            Assert.Equal(old.Calls, current.Calls);
        }
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void NativeIndicesPreservePropertyFormattingAndIndexSemantics(bool indexed)
    {
        var previousCulture = CultureInfo.CurrentCulture;
        try
        {
            foreach (var culture in new[] { CultureInfo.InvariantCulture, new CultureInfo("fr-FR"), CustomCulture() })
            {
                CultureInfo.CurrentCulture = culture;
                double[] indices = [0, -0d, 1.5, -1.5, int.MinValue, int.MaxValue, 4294967296d,
                    long.MinValue, (double)ulong.MaxValue, double.NaN, double.PositiveInfinity, double.NegativeInfinity];
                foreach (var index in indices)
                {
                    var old = CreateReceiver(indexed);
                    var current = CreateReceiver(indexed);
                    var oldDatum = ScriptDatum.FromObject(old);
                    var currentDatum = ScriptDatum.FromObject(current);
                    AssertSameResult(() => LegacyGetNumber(oldDatum, index), () => ObjectOps.GetElementNumber(currentDatum, index));
                    AssertSameResult(() => LegacySetNumber(oldDatum, index, 42), () => ObjectOps.SetElementNumber(currentDatum, index, 42));
                    Assert.Equal(old.Calls, current.Calls);
                }
                foreach (var index in NativeIndices())
                {
                    var old = CreateReceiver(indexed);
                    var current = CreateReceiver(indexed);
                    var oldDatum = ScriptDatum.FromObject(old);
                    var currentDatum = ScriptDatum.FromObject(current);
                    AssertSameResult(() => LegacyGetNumber(oldDatum, index), () => ObjectOps.GetElementIndex(currentDatum, index));
                    AssertSameResult(() => LegacySetNumber(oldDatum, index, 42), () => ObjectOps.SetElementIndex(currentDatum, index, 42));
                    Assert.Equal(old.Calls, current.Calls);
                }
            }
        }
        finally { CultureInfo.CurrentCulture = previousCulture; }
    }

    [Fact]
    public void PrimitiveAndArrayFallbacksKeepResultsAndErrors()
    {
        Func<ScriptDatum>[] receivers = [() => default, () => ScriptDatum.True, () => ScriptDatum.FromNumber(2),
            () => ScriptDatum.FromInt64(3), () => ScriptDatum.FromString("text"),
            () => ScriptDatum.FromArray(new ScriptArray(new ScriptDatum[] { 1, 2 })),
            () => ScriptDatum.FromObject(new ScriptInt32Array(2))];
        ScriptDatum[] indices = [default, ScriptDatum.True, ScriptDatum.FromString("length"),
            ScriptDatum.FromString("1"), ScriptDatum.FromNumber(-1), ScriptDatum.FromNumber(0), ScriptDatum.FromNumber(4)];
        foreach (var factory in receivers)
        foreach (var index in indices)
        {
            var old = factory();
            var current = factory();
            AssertSameResult(() => LegacyGet(old, index), () => ObjectOps.GetElement(current, index));
            AssertSameResult(() => LegacySet(old, index, 42), () => ObjectOps.SetElement(current, index, 42));
            AssertSameResult(() => LegacyGet(old, index), () => ObjectOps.GetElement(current, index));
        }
    }

    private static CultureInfo CustomCulture()
    {
        var culture = (CultureInfo)CultureInfo.InvariantCulture.Clone();
        culture.NumberFormat.NumberDecimalSeparator = ":";
        culture.NumberFormat.NegativeSign = "~";
        culture.NumberFormat.NaNSymbol = "not-a-number";
        culture.NumberFormat.PositiveInfinitySymbol = "infinite";
        return culture;
    }

    private static IEnumerable<int> NativeIndices()
    {
        foreach (var value in new[] { int.MinValue, -1, 0, 1, int.MaxValue }) yield return value;
        var random = new Random(43);
        for (var i = 0; i < 512; i++) yield return (int)random.NextInt64(int.MinValue, (long)int.MaxValue + 1);
    }

    private static void AssertSameResult(Func<ScriptDatum> baseline, Func<ScriptDatum> optimized)
    {
        ScriptDatum expected = default, actual = default;
        var before = Record.Exception(() => expected = baseline());
        var after = Record.Exception(() => actual = optimized());
        Assert.Equal(before?.GetType(), after?.GetType());
        Assert.Equal(before?.Message, after?.Message);
        if (before != null) return;
        Assert.Equal(expected.Kind, actual.Kind);
        if (expected.Kind == ValueKind.Number)
            Assert.Equal(BitConverter.DoubleToUInt64Bits(expected.Number), BitConverter.DoubleToUInt64Bits(actual.Number));
        else Assert.Equal(ScriptDatum.ToString(expected), ScriptDatum.ToString(actual));
    }

    private static RecordingReceiver CreateReceiver(bool indexed) => indexed ? new RecordingIndexer() : new RecordingReceiver();

    private class RecordingReceiver : ScriptObject
    {
        public List<string> Calls { get; } = [];
        protected internal override ScriptDatum GetPropertyDatum(ScriptContext context, string key)
        {
            Assert.Null(context);
            Calls.Add("get:" + key);
            return ScriptDatum.FromString(key);
        }
        protected internal override void SetPropertyDatum(ScriptContext context, string key, ScriptDatum value)
        {
            Assert.Null(context);
            Calls.Add("set:" + key);
            Assert.Equal(42, value.Number);
        }
    }

    private sealed class RecordingIndexer : RecordingReceiver, IAuroraNativeIndexer
    {
        public ScriptDatum this[int index]
        {
            get { Calls.Add("get-index:" + index); return ScriptDatum.FromNumber(index); }
            set { Calls.Add("set-index:" + index); Assert.Equal(42, value.Number); }
        }
    }

    // Preserve the pre-optimization protocol, including the distinct double/index casts.
    private static ScriptDatum LegacyGet(ScriptDatum receiver, ScriptDatum index)
    {
        if (receiver.Reference is IAuroraNativeIndexer indexed && ScriptDatum.TryToInteger(in index, out var numeric)) return indexed[(int)numeric];
        if (receiver.Reference is ScriptPackedArray packed && ScriptDatum.TryToInteger(in index, out numeric)) return packed.GetElementDatum((int)numeric);
        var instance = ScriptDatum.ToObject(receiver);
        if (instance is IAuroraNativeIndexer fallback && ScriptDatum.TryToInteger(in index, out numeric)) return fallback[(int)numeric];
        return instance.GetPropertyDatum(null, ScriptDatum.ToString(index));
    }

    private static ScriptDatum LegacySet(ScriptDatum receiver, ScriptDatum index, ScriptDatum value)
    {
        var instance = ScriptDatum.ToObject(receiver);
        if (instance is IAuroraNativeIndexer indexed && ScriptDatum.TryToInteger(in index, out var numeric)) indexed[(int)numeric] = value;
        else if (instance is ScriptPackedArray packed && ScriptDatum.TryToInteger(in index, out numeric)) packed.SetElementDatum((int)numeric, value);
        else instance.SetPropertyDatum(null, ScriptDatum.ToString(index), value);
        return value;
    }

    private static ScriptDatum LegacyGetNumber(ScriptDatum receiver, double index)
    {
        if (receiver.Reference is IAuroraNativeIndexer indexed) return indexed[(int)index];
        if (receiver.Reference is ScriptPackedArray packed) return packed.GetElementDatum((int)index);
        return LegacyGet(receiver, ScriptDatum.FromNumber(index));
    }

    private static ScriptDatum LegacySetNumber(ScriptDatum receiver, double index, ScriptDatum value)
    {
        if (receiver.Reference is IAuroraNativeIndexer indexed) indexed[(int)index] = value;
        else if (receiver.Reference is ScriptPackedArray packed) packed.SetElementDatum((int)index, value);
        else return LegacySet(receiver, ScriptDatum.FromNumber(index), value);
        return value;
    }
}
