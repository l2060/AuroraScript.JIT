using AuroraScript.Runtime;
using AuroraScript.Runtime.Types;
using System;
using Xunit;

namespace AuroraScript.Tests;

public sealed class ValueOpsTests
{
    [Fact]
    public void ArithmeticCoercionMatchesLanguageSemantics()
    {
        AssertNumber(0, ValueOps.Add(ScriptDatum.Null, ScriptDatum.Null));
        Assert.Equal("11", ValueOps.Add(ScriptDatum.FromString("1"), ScriptDatum.FromNumber(1)).String.Value);
        Assert.Equal("x1", ValueOps.Add(ScriptDatum.FromString("x"), ScriptDatum.FromNumber(1)).String.Value);
        AssertNumber(-5, ValueOps.Subtract(ScriptDatum.Null, ScriptDatum.FromNumber(5)));
        Assert.True(double.IsNaN(ValueOps.Multiply(ScriptDatum.FromString("x"), ScriptDatum.True).Number));
    }

    [Fact]
    public void TruthinessHandlesEveryPrimitiveWithoutBoxing()
    {
        Assert.False(ValueOps.ToBoolean(ScriptDatum.Null));
        Assert.False(ValueOps.ToBoolean(ScriptDatum.False));
        Assert.False(ValueOps.ToBoolean(ScriptDatum.FromNumber(0)));
        Assert.False(ValueOps.ToBoolean(ScriptDatum.NaN));
        Assert.False(ValueOps.ToBoolean(ScriptDatum.FromString(string.Empty)));
        Assert.True(ValueOps.ToBoolean(ScriptDatum.True));
        Assert.True(ValueOps.ToBoolean(ScriptDatum.FromNumber(-1)));
        Assert.True(ValueOps.ToBoolean(ScriptDatum.FromString("value")));
        Assert.True(ValueOps.Not(ScriptDatum.NaN).Boolean);
    }

    [Fact]
    public void EqualityPreservesPrimitiveCoercionAndObjectIdentity()
    {
        Assert.True(ValueOps.EqualBoolean(ScriptDatum.FromString("2"), ScriptDatum.FromNumber(2)));
        Assert.False(ValueOps.EqualBoolean(ScriptDatum.Null, ScriptDatum.FromNumber(0)));

        var instance = new ScriptObject();
        Assert.True(ValueOps.EqualBoolean(ScriptDatum.FromObject(instance), ScriptDatum.FromObject(instance)));
        Assert.False(ValueOps.EqualBoolean(ScriptDatum.FromObject(instance), ScriptDatum.FromObject(new ScriptObject())));
    }

    [Fact]
    public void BitwiseOrPreservesTheRightOperandForANullLeftOperand()
    {
        Assert.Equal(ScriptDatum.True, ValueOps.BitwiseOr(ScriptDatum.Null, ScriptDatum.True));
        Assert.Equal(
            ScriptDatum.FromString("Aurora"),
            ValueOps.BitwiseOr(ScriptDatum.Null, ScriptDatum.FromString("Aurora")));
        AssertNumber(2, ValueOps.BitwiseOr(ScriptDatum.Null, ScriptDatum.FromNumber(2)));
    }

    [Fact]
    public void StrictStringLookupRejectsOtherPrimitiveKinds()
    {
        var values = new[]
        {
            ScriptDatum.FromString("Aurora"),
            ScriptDatum.FromNumber(42),
            ScriptDatum.True,
        };

        Assert.True(values.AsSpan().TryGetStrictString(0, out var text));
        Assert.Equal("Aurora", text);
        Assert.False(values.AsSpan().TryGetStrictString(1, out _));
        Assert.False(values.AsSpan().TryGetStrictString(2, out _));
    }

    private static void AssertNumber(double expected, ScriptDatum actual)
    {
        Assert.Equal(ValueKind.Number, actual.Kind);
        Assert.Equal(expected, actual.Number);
    }
}
