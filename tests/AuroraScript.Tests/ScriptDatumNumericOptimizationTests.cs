using AuroraScript.Runtime;
using AuroraScript.Runtime.Types;
using AuroraScript.Tests.Infrastructure;
using System;
using System.Threading.Tasks;
using Xunit;

namespace AuroraScript.Tests;

public sealed class ScriptDatumNumericOptimizationTests
{
    [Fact]
    public void NumericFactoriesAndWritersPreserveAllSpecialPatterns()
    {
        ulong[] patterns = [0, 1, 2, 3, 0x8000000000000000, 0x8000000000000001,
            0x8000000000000002, 0x000fffffffffffff, 0x0010000000000000,
            0x7fefffffffffffff, 0xffefffffffffffff, 0x7ff0000000000000,
            0xfff0000000000000, 0x7ff0000000000001, 0xfff0000000000001,
            0x7ff8000000000000, 0x7ff8000000000001, 0x7ff8000000000002,
            0x7ff8000000000003, 0x7ff8000000000004, 0x7ff8000000000005,
            0xffffffffffffffff];
        foreach (var bits in patterns) CheckNumber(BitConverter.UInt64BitsToDouble(bits));
        var random = new Random(42);
        var bytes = new byte[8];
        for (var i = 0; i < 10000; i++)
        {
            random.NextBytes(bytes);
            CheckNumber(BitConverter.UInt64BitsToDouble(BitConverter.ToUInt64(bytes)));
        }
    }

    private static void CheckNumber(double value)
    {
        var expectedBits = BitConverter.DoubleToUInt64Bits(double.IsNaN(value) ? double.NaN : value);
        var expectedTruth = value != 0 && !double.IsNaN(value);
        var created = ScriptDatum.FromNumber(value);
        var written = ScriptDatum.FromString("old reference");
        ScriptDatum.WriteNumber(ref written, value);
        var writtenAs = ScriptDatum.FromArray(new ScriptArray());
        ScriptDatum.WriteAsNumber(ref writtenAs, value);
        foreach (var datum in new[] { created, written, writtenAs })
        {
            Assert.Equal(ValueKind.Number, datum.Kind);
            Assert.Null(datum.Reference);
            Assert.Equal(expectedBits, BitConverter.DoubleToUInt64Bits(datum.Number));
            Assert.Equal(expectedTruth, ScriptDatum.IsTrue(datum));
            Assert.Equal(!expectedTruth, ScriptDatum.IsFalse(datum));
            Assert.True(ScriptDatum.TryToNumber(in datum, out var converted));
            Assert.Equal(expectedBits, BitConverter.DoubleToUInt64Bits(converted));
            Assert.True(ScriptDatum.TryToInteger(in datum, out var integer));
            Assert.Equal((long)value, integer);
            Assert.Equal(!double.IsNaN(value), datum.Equals(created));
        }
    }

    [Fact]
    public void IntegerFactoriesMatchDoubleEncodingAtBoundaries()
    {
        foreach (var value in new[] { int.MinValue, -1, 0, 1, int.MaxValue })
            AssertNumber((double)value, ScriptDatum.FromNumber(value));
        foreach (var value in new[] { 0U, 1U, (uint)int.MaxValue, uint.MaxValue })
            AssertNumber((double)value, ScriptDatum.FromNumber(value));
        foreach (var value in new[] { long.MinValue, -9007199254740993L, -1L, 0L, 1L,
            9007199254740991L, 9007199254740992L, 9007199254740993L, long.MaxValue })
            AssertNumber((double)value, ScriptDatum.FromNumber(value));
    }

    private static void AssertNumber(double expected, ScriptDatum actual)
    {
        Assert.Equal(ValueKind.Number, actual.Kind);
        Assert.Equal(BitConverter.DoubleToUInt64Bits(expected), BitConverter.DoubleToUInt64Bits(actual.Number));
    }

    [Fact]
    public void EqualityAndCoercionKeepCrossKindSemantics()
    {
        ScriptDatum[] values = [default, ScriptDatum.False, ScriptDatum.True,
            ScriptDatum.FromNumber(0d), ScriptDatum.FromNumber(-0d), ScriptDatum.NaN,
            ScriptDatum.FromNumber(1), ScriptDatum.FromInt64(-1), ScriptDatum.FromInt64(0),
            ScriptDatum.FromInt64(9007199254740993L), ScriptDatum.FromUInt64(0),
            ScriptDatum.FromUInt64(9007199254740993UL), ScriptDatum.FromUInt64(ulong.MaxValue),
            ScriptDatum.FromString("1"), ScriptDatum.FromString("bad"), ScriptDatum.FromString(""),
            ScriptDatum.FromArray(new ScriptArray())];
        foreach (var a in values)
        foreach (var b in values)
        {
            var expected = LegacyEquals(a, b);
            Assert.Equal(expected, a.Equals(b));
            Assert.Equal(expected, b.Equals(a));
        }
        foreach (var value in values)
            Assert.Equal(!ScriptDatum.IsTrue(value), ScriptDatum.IsFalse(value));
    }

    private static bool LegacyEquals(ScriptDatum a, ScriptDatum b)
    {
        if (a.Kind == b.Kind)
            return a.Kind switch
            {
                ValueKind.Null => true,
                ValueKind.Boolean => a.Boolean == b.Boolean,
                ValueKind.Number => a.Number == b.Number,
                ValueKind.Int64 => a.Int64 == b.Int64,
                ValueKind.UInt64 => a.UInt64 == b.UInt64,
                ValueKind.String => a.StringText == b.StringText,
                _ => ReferenceEquals(a.Reference, b.Reference),
            };
        if (!ScriptDatum.TryToNumber(a, out var na) || !ScriptDatum.TryToNumber(b, out var nb)) return false;
        if (a.Kind == ValueKind.Int64 && b.Kind == ValueKind.UInt64) return a.Int64 >= 0 && (ulong)a.Int64 == b.UInt64;
        if (a.Kind == ValueKind.UInt64 && b.Kind == ValueKind.Int64) return b.Int64 >= 0 && a.UInt64 == (ulong)b.Int64;
        return na == nb;
    }

    [Theory]
    [InlineData(CompilationMode.Dynamic, false)]
    [InlineData(CompilationMode.Dynamic, true)]
    [InlineData(CompilationMode.OnlyRun, false)]
    [InlineData(CompilationMode.OnlyRun, true)]
#if NET9_0_OR_GREATER
    [InlineData(CompilationMode.Persistence, false)]
    [InlineData(CompilationMode.Persistence, true)]
#endif
    public async Task NegativeZeroConstantsSurviveDatumEmission(CompilationMode mode, bool inlineConstants)
    {
        using var workspace = new TestWorkspace();
        var (_, domain) = await workspace.CompileModuleAsync("""
            @module(TEST);
            const NEGATIVE_ZERO = -0.0;
            var initial = [-0.0];
            export func moduleValue() { return initial; }
            export native func literal() Array { return [-0.0, 0.0, -1.0]; }
            export func direct() { return -0.0; }
            export func constant() { return NEGATIVE_ZERO; }
            export native func constantArray() Array { return [NEGATIVE_ZERO]; }
            """, mode, enableModuleConstInlining: inlineConstants);
        AssertNumber(-0d, TestWorkspace.Execute(domain, "direct"));
        AssertNumber(-0d, TestWorkspace.Execute(domain, "constant"));
        foreach (var method in new[] { "moduleValue", "literal", "constantArray" })
        {
            var array = Assert.IsType<ScriptArray>(TestWorkspace.Execute(domain, method).Object);
            AssertNumber(-0d, array[0]);
        }
    }
}
