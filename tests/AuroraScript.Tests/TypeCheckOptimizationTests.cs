using AuroraScript.Runtime;
using System;
using System.Collections.Generic;
using Xunit;

namespace AuroraScript.Tests;

public sealed class TypeCheckOptimizationTests
{
    [Fact]
    public void NumberChecksMatchOriginalPredicatesAndKeepBoundaryBits()
    {
        foreach (var number in Samples())
        {
            var positiveZero = number != 0 || BitConverter.DoubleToInt64Bits(number) >= 0;
            var uint32 = number >= 0 && number <= uint.MaxValue && number == Math.Truncate(number) && positiveZero;
            var int64 = number >= -9223372036854775808d && number < 9223372036854775808d && number == Math.Truncate(number) && positiveZero;
            var uint64 = number >= 0 && number < 18446744073709551616d && number == Math.Truncate(number) && positiveZero;
            Assert.Equal(uint32, TypeCheckOps.IsUInt32(number));
            Assert.Equal(int64, TypeCheckOps.IsInt64(number));
            Assert.Equal(uint64, TypeCheckOps.IsUInt64(number));
            var datum = ScriptDatum.FromNumber(number);
            Check(uint32, "uint32", datum, () =>
            {
                Assert.Equal((uint)number, TypeCheckOps.CheckUInt32Number(number));
                Assert.Equal((uint)number, TypeCheckOps.CheckUInt32Value(datum));
                var checkedDatum = TypeCheckOps.CheckUInt32(datum);
                Assert.Equal(ValueKind.Number, checkedDatum.Kind);
                Assert.Equal(BitConverter.DoubleToUInt64Bits(number), BitConverter.DoubleToUInt64Bits(checkedDatum.Number));
            }, [() => TypeCheckOps.CheckUInt32Number(number), () => TypeCheckOps.CheckUInt32Value(datum), () => TypeCheckOps.CheckUInt32(datum)]);
            Check(int64, "int64", datum, () =>
            {
                Assert.Equal((long)number, TypeCheckOps.CheckInt64Number(number));
                Assert.Equal((long)number, TypeCheckOps.CheckInt64Value(datum));
                var checkedDatum = TypeCheckOps.CheckInt64(datum);
                Assert.Equal(ValueKind.Int64, checkedDatum.Kind);
                Assert.Equal((long)number, checkedDatum.Int64);
            }, [() => TypeCheckOps.CheckInt64Number(number), () => TypeCheckOps.CheckInt64Value(datum), () => TypeCheckOps.CheckInt64(datum)]);
            Check(uint64, "uint64", datum, () =>
            {
                Assert.Equal((ulong)number, TypeCheckOps.CheckUInt64Number(number));
                Assert.Equal((ulong)number, TypeCheckOps.CheckUInt64Value(datum));
                var checkedDatum = TypeCheckOps.CheckUInt64(datum);
                Assert.Equal(ValueKind.UInt64, checkedDatum.Kind);
                Assert.Equal((ulong)number, checkedDatum.UInt64);
            }, [() => TypeCheckOps.CheckUInt64Number(number), () => TypeCheckOps.CheckUInt64Value(datum), () => TypeCheckOps.CheckUInt64(datum)]);
        }
    }

    private static void Check(bool valid, string expectedType, ScriptDatum input, Action success, Action[] failures)
    {
        if (valid) { success(); return; }
        foreach (var action in failures)
        {
            var error = Assert.Throws<AuroraRuntimeException>(action);
            Assert.Equal("Type check failed: expected " + expectedType + ", actual " + ScriptDatum.GetTypeName(input) + ".", error.Message);
        }
    }

    private static IEnumerable<double> Samples()
    {
        double[] values = [0, -0d, double.Epsilon, -double.Epsilon, double.NaN, double.PositiveInfinity,
            double.NegativeInfinity, 1.25, -1.25, -1, 1, int.MinValue, int.MaxValue, uint.MaxValue,
            4294967296d, 9007199254740992d, -9223372036854775808d, 9223372036854775808d, 18446744073709551616d];
        foreach (var value in values)
        {
            yield return value;
            yield return Math.BitIncrement(value);
            yield return Math.BitDecrement(value);
        }
        var random = new Random(51);
        var bytes = new byte[8];
        for (var i = 0; i < 512; i++)
        {
            random.NextBytes(bytes);
            yield return BitConverter.ToDouble(bytes);
        }
    }
}
