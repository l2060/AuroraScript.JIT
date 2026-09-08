using AuroraScript.Runtime;
using AuroraScript.Runtime.Types;
using System;
using System.Collections.Generic;
using Xunit;

namespace AuroraScript.Tests;

public sealed class IncludesOptimizationTests
{
    [Fact]
    public void MembershipMatchesLegacyEqualityAndStringRules()
    {
        var shared = new ScriptObject();
        var date = new ScriptDate(1000L);
        ScriptDatum[] needles = [default, ScriptDatum.True, ScriptDatum.False, ScriptDatum.NaN,
            ScriptDatum.FromNumber(0), ScriptDatum.FromNumber(-0d), ScriptDatum.FromNumber(1),
            ScriptDatum.FromNumber(1.5), ScriptDatum.FromInt64(9007199254740993L),
            ScriptDatum.FromUInt64(ulong.MaxValue), ScriptDatum.FromString(""), ScriptDatum.FromString("1"),
            ScriptDatum.FromString("12"), ScriptDatum.FromString("a"), ScriptDatum.FromString("\u4e2d"),
            ScriptDatum.FromString("\ud83d"), ScriptDatum.FromString("\ude00"),
            ScriptDatum.FromObject(shared), ScriptDatum.FromDate(date), ScriptDatum.FromDate(new ScriptDate(1000L))];
        var collections = new List<ScriptDatum>
        {
            default, ScriptDatum.FromObject(new ScriptObject()), ScriptDatum.FromString(""),
            ScriptDatum.FromString("12a\u4e2d\ud83d\ude00"),
            ScriptDatum.FromArray(new ScriptArray()), ScriptDatum.FromArray(new ScriptArray(needles)),
            ScriptDatum.FromArray(new ScriptArray(new ScriptDatum[] { ScriptDatum.FromDate(date) }))
        };
        foreach (var data in PackedBoundaryOptimizationTests.Cases())
            collections.Add(ScriptDatum.FromObject((ScriptObject)Activator.CreateInstance((Type)data[2], 3)!));
        foreach (var collection in collections)
        foreach (var needle in needles)
            Assert.Equal(LegacyIncludes(collection, needle), ObjectOps.Includes(collection, needle));
        Assert.True(ValueOps.EqualBoolean(ScriptDatum.FromDate(date), ScriptDatum.FromDate(new ScriptDate(1000L))));
        Assert.False(ObjectOps.Includes(collections[6], ScriptDatum.FromDate(new ScriptDate(1000L))));
    }

    [Fact]
    public void CustomObjectEnumeratorStillRuns()
    {
        var receiver = new CustomEnumerable();
        Assert.True(ObjectOps.Includes(ScriptDatum.FromObject(receiver), ScriptDatum.FromNumber(42)));
        Assert.Equal(1, receiver.Calls);
    }

    [Theory]
    [InlineData(2)]
    [InlineData(99)]
    public void PackedAccessOrderAndExceptionsMatchEnumerator(int needle)
    {
        var baseline = new RecordingPacked();
        var current = new RecordingPacked();
        var expected = false;
        var actual = false;
        var before = Record.Exception(() => expected = LegacyIncludes(ScriptDatum.FromObject(baseline), needle));
        var after = Record.Exception(() => actual = ObjectOps.Includes(ScriptDatum.FromObject(current), needle));
        Assert.Equal(before?.GetType(), after?.GetType());
        Assert.Equal(before?.Message, after?.Message);
        Assert.Equal(expected, actual);
        Assert.Equal(1, current.LengthReads);
        Assert.Equal(baseline.Indices, current.Indices);
    }

    [Fact]
    public void BuiltinMembershipFastPathsAllocateNothing()
    {
        ScriptDatum[] collections = [ScriptDatum.FromArray(new ScriptArray(32)),
            ScriptDatum.FromObject(new ScriptInt32Array(32)), ScriptDatum.FromString(new string('\u4e2d', 32))];
        ScriptDatum[] needles = [ScriptDatum.FromNumber(-1), ScriptDatum.FromNumber(-1), ScriptDatum.FromString("\u6587")];
        var matched = false;
        for (var i = 0; i < 10000; i++)
            for (var kind = 0; kind < collections.Length; kind++) matched |= ObjectOps.Includes(collections[kind], needles[kind]);
        var before = GC.GetAllocatedBytesForCurrentThread();
        for (var i = 0; i < 10000; i++)
            for (var kind = 0; kind < collections.Length; kind++) matched |= ObjectOps.Includes(collections[kind], needles[kind]);
        var allocated = GC.GetAllocatedBytesForCurrentThread() - before;
        Assert.False(matched);
        Assert.Equal(0, allocated);
    }

    private sealed class CustomEnumerable : ScriptObject
    {
        public int Calls;
        public override ScriptEnumerator GetEnumerator()
        {
            Calls++;
            return new ScriptEnumerator(new ScriptDatum[] { 42 });
        }
    }

    private sealed class RecordingPacked : ScriptPackedArray
    {
        public int LengthReads;
        public List<int> Indices { get; } = [];
        public override int Length { get { LengthReads++; return 3; } }
        protected internal override ScriptDatum TypeOfValue => ScriptDatum.FromString("RecordingPacked");
        internal override ScriptDatum GetElementDatumUnchecked(int index)
        {
            Indices.Add(index);
            if (index == 2) throw new InvalidOperationException("packed sentinel");
            return ScriptDatum.FromNumber(index + 1);
        }
        internal override void SetElementDatumUnchecked(int index, ScriptDatum value) => throw new NotSupportedException();
        internal override void FillDatum(ScriptDatum value) => throw new NotSupportedException();
        internal override ScriptPackedArray ClonePackedArray() => throw new NotSupportedException();
    }

    private static bool LegacyIncludes(ScriptDatum collection, ScriptDatum value)
    {
        if (collection.Kind == ValueKind.String)
        {
            var text = collection.StringText ?? string.Empty;
            if (value.Kind == ValueKind.String)
            {
                var needle = value.StringText ?? string.Empty;
                if (needle.Length > 1) return text.IndexOf(needle, StringComparison.Ordinal) >= 0;
            }
            for (var i = 0; i < text.Length; i++)
                if (ScriptDatum.FromString(StringValue.TextFromChar(text[i])).Equals(value)) return true;
            return false;
        }
        var iterator = ScriptDatum.ToObject(collection).GetEnumerator();
        while (iterator.NextValue(out var current)) if (current.Equals(value)) return true;
        return false;
    }
}
