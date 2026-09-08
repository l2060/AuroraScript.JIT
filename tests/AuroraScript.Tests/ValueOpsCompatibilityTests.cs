using AuroraScript.Runtime;
using AuroraScript.Runtime.Types;
using System;
using Xunit;

namespace AuroraScript.Tests;

public sealed class ValueOpsCompatibilityTests
{
    [Fact]
    public void BooleanEntryPointsMatchResultTruthinessAndExceptions()
    {
        (Func<ScriptDatum, ScriptDatum, ScriptDatum> Operation, Func<ScriptDatum, ScriptDatum, bool> Boolean)[] operations =
        [
            (ValueOps.Add, ValueOps.AddBoolean), (ValueOps.Subtract, ValueOps.SubtractBoolean),
            (ValueOps.Multiply, ValueOps.MultiplyBoolean), (ValueOps.Divide, ValueOps.DivideBoolean),
            (ValueOps.Modulo, ValueOps.ModuloBoolean), (ValueOps.BitwiseAnd, ValueOps.BitwiseAndBoolean),
            (ValueOps.BitwiseOr, ValueOps.BitwiseOrBoolean), (ValueOps.BitwiseXor, ValueOps.BitwiseXorBoolean),
            (ValueOps.LeftShift, ValueOps.LeftShiftBoolean), (ValueOps.RightShift, ValueOps.RightShiftBoolean),
            (ValueOps.UnsignedRightShift, ValueOps.UnsignedRightShiftBoolean)
        ];
        ScriptDatum[] values = [default, ScriptDatum.True, ScriptDatum.False,
            ScriptDatum.FromNumber(0), ScriptDatum.FromNumber(-0d), ScriptDatum.FromNumber(-1),
            ScriptDatum.FromNumber(1.25), ScriptDatum.NaN, ScriptDatum.FromNumber(double.PositiveInfinity),
            ScriptDatum.FromNumber(double.MaxValue), ScriptDatum.FromNumber(int.MinValue),
            ScriptDatum.FromInt64(long.MinValue), ScriptDatum.FromInt64(-1), ScriptDatum.FromInt64(0),
            ScriptDatum.FromUInt64(0), ScriptDatum.FromUInt64(ulong.MaxValue), ScriptDatum.FromString(""),
            ScriptDatum.FromString("2"), ScriptDatum.FromString("bad"), ScriptDatum.FromArray(new ScriptArray()),
            ScriptDatum.FromObject(new ScriptObject())];
        foreach (var value in values) Assert.Equal(ScriptDatum.IsTrue(value), ValueOps.ToBoolean(value));
        foreach (var (operation, boolean) in operations)
        foreach (var left in values)
        foreach (var right in values)
        {
            bool expected = false, actual = false;
            var expectedError = Record.Exception(() => expected = ScriptDatum.IsTrue(operation(left, right)));
            var actualError = Record.Exception(() => actual = boolean(left, right));
            Assert.Equal(expectedError?.GetType(), actualError?.GetType());
            Assert.Equal(expectedError?.Message, actualError?.Message);
            if (expectedError == null) Assert.Equal(expected, actual);
        }
    }
}
