using AuroraScript.Runtime;
using AuroraScript.Runtime.Interop;
using AuroraScript.Runtime.Serialization;
using AuroraScript.Runtime.Types;
using AuroraScript.Tests.Infrastructure;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Xunit;

namespace AuroraScript.Tests;

public sealed class Int64PrimitiveTests
{
    [Theory]
    [InlineData("9223372036854775808L")]
    [InlineData("-9223372036854775809L")]
    [InlineData("18446744073709551616UL")]
    public async Task OutOfRangeIntegerLiteralsAreRejected(string literal)
    {
        using var workspace = new TestWorkspace();
        await Assert.ThrowsAsync<AuroraCompilationException>(() =>
            workspace.CompileModuleAsync(
                "@module(TEST); export func run() { return " + literal + "; }",
                CompilationMode.Dynamic));
    }

    [Fact]
    public void DatumAndClrMarshallingPreserveExactPayloads()
    {
        Assert.Equal(16, Unsafe.SizeOf<ScriptDatum>());

        AssertInt64(long.MinValue, ScriptDatum.FromInt64(long.MinValue));
        AssertInt64(long.MaxValue, ClrMarshaller.ToDatum(long.MaxValue));
        AssertUInt64(ulong.MaxValue, ScriptDatum.FromUInt64(ulong.MaxValue));
        AssertUInt64(ulong.MaxValue, ClrMarshaller.ToDatum(ulong.MaxValue));
        AssertInt64(long.MinValue, ClrMarshaller.ToDatum(SignedEnum.Minimum));
        AssertUInt64(ulong.MaxValue, ClrMarshaller.ToDatum(UnsignedEnum.Maximum));

        var signed = ScriptDatum.FromInt64(long.MinValue);
        var unsigned = ScriptDatum.FromUInt64(ulong.MaxValue);
        Assert.True(ClrMarshaller.TryConvertArgument(in signed, typeof(long), out var signedClr));
        Assert.True(ClrMarshaller.TryConvertArgument(in unsigned, typeof(ulong), out var unsignedClr));
        Assert.Equal(long.MinValue, Assert.IsType<long>(signedClr));
        Assert.Equal(ulong.MaxValue, Assert.IsType<ulong>(unsignedClr));
        Assert.Equal(long.MinValue, Assert.IsType<Int64Value>(ScriptDatum.ToObject(signed)).Value);
        Assert.Equal(ulong.MaxValue, Assert.IsType<UInt64Value>(ScriptDatum.ToObject(unsigned)).Value);
    }

    [Theory]
    [InlineData(CompilationMode.Dynamic)]
    [InlineData(CompilationMode.OnlyRun)]
#if NET9_0_OR_GREATER
    [InlineData(CompilationMode.Persistence)]
#endif
    public async Task SameTypeOperationsWrapAndMixedOperationsUseNumber(CompilationMode mode)
    {
        using var workspace = new TestWorkspace();
        var (_, domain) = await workspace.CompileModuleAsync(
            """
            @module(TEST);
            export func run() {
                var max = 9223372036854775807L;
                var min = -9223372036854775808L;
                var umax = 18446744073709551615UL;
                return [
                    typeof max,
                    typeof umax,
                    max + 1L,
                    min - 1L,
                    umax + 1UL,
                    min / -1L,
                    min % -1L,
                    1L << 63L,
                    -1L >>> 63L,
                    0x8000000000000000UL >> 63UL,
                    max == 9223372036854775807UL,
                    -1L < 0UL,
                    umax > max,
                    typeof (max + 0),
                    max + 0,
                    typeof (1L + 1UL),
                    1L + 1UL,
                    Object.equal$(1L, 1L),
                    Object.equal$(1L, 1UL),
                    Object.equal(1L, 1UL),
                    Object.deepEqual(1L, 1UL)
                ];
            }
            """,
            mode);

        var values = Assert.IsType<ScriptArray>(TestWorkspace.Execute(domain, "run").Object);
        ScriptAssert.Equal("int64", values.GetElement(0));
        ScriptAssert.Equal("uint64", values.GetElement(1));
        AssertInt64(long.MinValue, values.GetElement(2));
        AssertInt64(long.MaxValue, values.GetElement(3));
        AssertUInt64(0, values.GetElement(4));
        AssertInt64(long.MinValue, values.GetElement(5));
        AssertInt64(0, values.GetElement(6));
        AssertInt64(long.MinValue, values.GetElement(7));
        AssertUInt64(1, values.GetElement(8));
        AssertUInt64(1, values.GetElement(9));
        ScriptAssert.Equal(true, values.GetElement(10));
        ScriptAssert.Equal(true, values.GetElement(11));
        ScriptAssert.Equal(true, values.GetElement(12));
        ScriptAssert.Equal("number", values.GetElement(13));
        ScriptAssert.Equal((double)long.MaxValue, values.GetElement(14));
        ScriptAssert.Equal("number", values.GetElement(15));
        ScriptAssert.Equal(2d, values.GetElement(16));
        ScriptAssert.Equal(true, values.GetElement(17));
        ScriptAssert.Equal(false, values.GetElement(18));
        ScriptAssert.Equal(true, values.GetElement(19));
        ScriptAssert.Equal(true, values.GetElement(20));
    }

    [Theory]
    [InlineData(CompilationMode.Dynamic)]
    [InlineData(CompilationMode.OnlyRun)]
#if NET9_0_OR_GREATER
    [InlineData(CompilationMode.Persistence)]
#endif
    public async Task StrongNativeBoundariesNormalizeOnlyAcceptedIntegers(CompilationMode mode)
    {
        using var workspace = new TestWorkspace();
        var (_, domain) = await workspace.CompileModuleAsync(
            """
            @module(TEST);
            export native func signed(int64 value) int64 { return value; }
            export native func unsigned(uint64 value) uint64 { return value; }
            export native func number(Number value) Number { return value; }
            export native func defaultSigned(int64 value = 42) int64 { return value; }
            export native func defaultUnsigned(uint64 value = 18446744073709551615UL) uint64 { return value; }
            """,
            mode);

        AssertInt64(42, TestWorkspace.Execute(domain, "signed", arguments: ScriptDatum.FromNumber(42)));
        AssertInt64(42, TestWorkspace.Execute(domain, "signed", arguments: ScriptDatum.FromUInt64(42)));
        AssertUInt64(42, TestWorkspace.Execute(domain, "unsigned", arguments: ScriptDatum.FromInt64(42)));
        ScriptAssert.Equal(42, TestWorkspace.Execute(domain, "number", arguments: ScriptDatum.FromNumber(42)));
        AssertInt64(42, TestWorkspace.Execute(domain, "defaultSigned"));
        AssertUInt64(ulong.MaxValue, TestWorkspace.Execute(domain, "defaultUnsigned"));

        Assert.Throws<AuroraRuntimeException>(() =>
            TestWorkspace.Execute(domain, "signed", arguments: ScriptDatum.FromUInt64(ulong.MaxValue)));
        Assert.Throws<AuroraRuntimeException>(() =>
            TestWorkspace.Execute(domain, "unsigned", arguments: ScriptDatum.FromInt64(-1)));
        Assert.Throws<AuroraRuntimeException>(() =>
            TestWorkspace.Execute(domain, "number", arguments: ScriptDatum.FromInt64(42)));
    }

    [Theory]
    [InlineData(CompilationMode.Dynamic)]
    [InlineData(CompilationMode.OnlyRun)]
#if NET9_0_OR_GREATER
    [InlineData(CompilationMode.Persistence)]
#endif
    public async Task PackedArraysReadWriteAndMutateExactValues(CompilationMode mode)
    {
        using var workspace = new TestWorkspace();
        var (_, domain) = await workspace.CompileModuleAsync(
            """
            @module(TEST);
            export func run() {
                var signed = new Int64Array(2);
                var unsigned = new UInt64Array(2);
                signed[0] = 9223372036854775807L;
                signed[1] = -9223372036854775808L;
                unsigned[0] = 18446744073709551615UL;
                unsigned[1] = 0UL;
                var oldSigned = signed[0]++;
                var newSigned = ++signed[1];
                var oldUnsigned = unsigned[0]++;
                unsigned[1] -= 1UL;
                return [oldSigned, signed[0], newSigned, signed[1], oldUnsigned, unsigned[0], unsigned[1]];
            }
            """,
            mode);

        var values = Assert.IsType<ScriptArray>(TestWorkspace.Execute(domain, "run").Object);
        AssertInt64(long.MaxValue, values.GetElement(0));
        AssertInt64(long.MinValue, values.GetElement(1));
        AssertInt64(long.MinValue + 1, values.GetElement(2));
        AssertInt64(long.MinValue + 1, values.GetElement(3));
        AssertUInt64(ulong.MaxValue, values.GetElement(4));
        AssertUInt64(0, values.GetElement(5));
        AssertUInt64(ulong.MaxValue, values.GetElement(6));
    }

    [Theory]
    [InlineData(CompilationMode.Dynamic)]
    [InlineData(CompilationMode.OnlyRun)]
#if NET9_0_OR_GREATER
    [InlineData(CompilationMode.Persistence)]
#endif
    public async Task DynamicAndMergedOperandsPreserveExactIntegerSemantics(CompilationMode mode)
    {
        using var workspace = new TestWorkspace();
        var (_, domain) = await workspace.CompileModuleAsync(
            """
            @module(TEST);
            export func add(left, right) { return left + right; }
            export func subtract(left, right) { return left - right; }
            export func multiply(left, right) { return left * right; }
            export func divide(left, right) { return left / right; }
            export func modulo(left, right) { return left % right; }
            export func invert(value) { return ~value; }
            export func negate(value) { return -value; }
            export func increment(value) { return ++value; }
            export func merged(Boolean useUnsigned) {
                var value = 1L;
                if (useUnsigned) value = 1UL;
                return value + value;
            }
            """,
            mode);

        AssertInt64(long.MinValue, TestWorkspace.Execute(
            domain, "add", arguments: [ScriptDatum.FromInt64(long.MaxValue), ScriptDatum.FromInt64(1)]));
        AssertInt64(long.MaxValue, TestWorkspace.Execute(
            domain, "subtract", arguments: [ScriptDatum.FromInt64(long.MinValue), ScriptDatum.FromInt64(1)]));
        AssertInt64(-2, TestWorkspace.Execute(
            domain, "multiply", arguments: [ScriptDatum.FromInt64(long.MaxValue), ScriptDatum.FromInt64(2)]));
        AssertInt64(long.MinValue, TestWorkspace.Execute(
            domain, "divide", arguments: [ScriptDatum.FromInt64(long.MinValue), ScriptDatum.FromInt64(-1)]));
        AssertInt64(0, TestWorkspace.Execute(
            domain, "modulo", arguments: [ScriptDatum.FromInt64(long.MinValue), ScriptDatum.FromInt64(-1)]));
        AssertInt64(long.MinValue, TestWorkspace.Execute(
            domain, "invert", arguments: ScriptDatum.FromInt64(long.MaxValue)));
        AssertInt64(long.MinValue, TestWorkspace.Execute(
            domain, "negate", arguments: ScriptDatum.FromInt64(long.MinValue)));
        AssertUInt64(0, TestWorkspace.Execute(
            domain, "increment", arguments: ScriptDatum.FromUInt64(ulong.MaxValue)));
        AssertUInt64(0, TestWorkspace.Execute(
            domain, "add", arguments: [ScriptDatum.FromUInt64(ulong.MaxValue), ScriptDatum.FromUInt64(1)]));
        ScriptAssert.Equal(2d, TestWorkspace.Execute(
            domain, "add", arguments: [ScriptDatum.FromInt64(1), ScriptDatum.FromUInt64(1)]));
        AssertInt64(2, TestWorkspace.Execute(domain, "merged", arguments: false));
        AssertUInt64(2, TestWorkspace.Execute(domain, "merged", arguments: true));
    }

    [Theory]
    [InlineData(CompilationMode.Dynamic)]
    [InlineData(CompilationMode.OnlyRun)]
#if NET9_0_OR_GREATER
    [InlineData(CompilationMode.Persistence)]
#endif
    public async Task StrongIntegerParametersRemainExactWhenCaptured(CompilationMode mode)
    {
        using var workspace = new TestWorkspace();
        var (_, domain) = await workspace.CompileModuleAsync(
            """
            @module(TEST);
            export func signed(int64 value) {
                func read() { return value; }
                return read();
            }
            export func unsigned(uint64 value) {
                func read() { return value; }
                return read();
            }
            """,
            mode);

        AssertInt64(long.MinValue, TestWorkspace.Execute(
            domain, "signed", arguments: ScriptDatum.FromInt64(long.MinValue)));
        AssertUInt64(ulong.MaxValue, TestWorkspace.Execute(
            domain, "unsigned", arguments: ScriptDatum.FromUInt64(ulong.MaxValue)));
    }

    [Fact]
    public void JsonAndTypedDocumentsPreserveWideIntegers()
    {
        var json = ScriptJsonSerializer.Default;
        Assert.Equal(long.MinValue, Assert.IsType<Int64Value>(json.Deserialize(long.MinValue.ToString())).Value);
        Assert.Equal(ulong.MaxValue, Assert.IsType<UInt64Value>(json.Deserialize(ulong.MaxValue.ToString())).Value);
        Assert.IsType<NumberValue>(json.Deserialize("42"));

        using var workspace = new TestWorkspace();
        var engine = workspace.CreateEngine();
        var signedText = TypedDocumentSerializer.Serialize(
            engine,
            ScriptDatum.FromInt64(long.MinValue),
            new TypedDocumentOptions { EmitTypeNames = true, Indented = false });
        var unsignedText = TypedDocumentSerializer.Serialize(
            engine,
            ScriptDatum.FromUInt64(ulong.MaxValue),
            new TypedDocumentOptions { EmitTypeNames = true, Indented = false });

        AssertInt64(long.MinValue, TypedDocumentSerializer.Deserialize(engine, signedText));
        AssertUInt64(ulong.MaxValue, TypedDocumentSerializer.Deserialize(engine, unsignedText));
    }

    [Theory]
    [InlineData(CompilationMode.Dynamic)]
    [InlineData(CompilationMode.OnlyRun)]
#if NET9_0_OR_GREATER
    [InlineData(CompilationMode.Persistence)]
#endif
    public async Task BitwiseLocalsPreserveStaticDynamicAndMergedIntegers(CompilationMode mode)
    {
        using var workspace = new TestWorkspace();
        var (_, domain) = await workspace.CompileModuleAsync(
            """
            @module(TEST);
            native func kernel(int64 value) int64 {
                var shifted = value << 1L;
                return shifted;
            }
            export func run() {
                var shifted = 1L << 40L;
                var inverted = ~0UL;
                var masked = 9223372036854775807L & -1L;
                var combined = 0x8000000000000000UL | 1UL;
                var toggled = 0x8000000000000000UL ^ 1UL;
                var right = 0x8000000000000000UL >> 1UL;
                var fallback = null | 9007199254740993L;
                return [shifted, inverted, masked, combined, toggled, right, kernel(shifted), fallback];
            }
            export func dynamic(value, shift) {
                var inverted = ~value;
                var shifted = value << shift;
                var masked = value & value;
                var combined = value | shift;
                var toggled = value ^ shift;
                var right = value >> shift;
                return [inverted, shifted, masked, combined, toggled, right];
            }
            export func merged(Boolean unsigned) {
                var value = 1L;
                if (unsigned) value = 1UL;
                var inverted = ~value;
                return inverted;
            }
            """, mode);

        var values = Assert.IsType<ScriptArray>(TestWorkspace.Execute(domain, "run").Object);
        AssertInt64(1L << 40, values.GetElement(0));
        AssertUInt64(ulong.MaxValue, values.GetElement(1));
        AssertInt64(long.MaxValue, values.GetElement(2));
        AssertUInt64(0x8000000000000001UL, values.GetElement(3));
        AssertUInt64(0x8000000000000001UL, values.GetElement(4));
        AssertUInt64(0x4000000000000000UL, values.GetElement(5));
        AssertInt64(1L << 41, values.GetElement(6));
        AssertInt64(9007199254740993L, values.GetElement(7));

        const long value = 1L << 40;
        var expected = new[] { ~value, value << 1, value, value | 1, value ^ 1, value >> 1 };
        foreach (var unsigned in new[] { false, true })
        {
            var argument = unsigned ? ScriptDatum.FromUInt64((ulong)value) : ScriptDatum.FromInt64(value);
            var shift = unsigned ? ScriptDatum.FromUInt64(1) : ScriptDatum.FromInt64(1);
            var dynamicValues = Assert.IsType<ScriptArray>(TestWorkspace.Execute(
                domain, "dynamic", arguments: [argument, shift]).Object);
            for (var i = 0; i < expected.Length; i++)
            {
                if (unsigned) AssertUInt64(unchecked((ulong)expected[i]), dynamicValues.GetElement(i));
                else AssertInt64(expected[i], dynamicValues.GetElement(i));
            }
        }
        AssertInt64(-2, TestWorkspace.Execute(domain, "merged", arguments: false));
        AssertUInt64(ulong.MaxValue - 1, TestWorkspace.Execute(domain, "merged", arguments: true));
    }

    [Theory]
    [InlineData(CompilationMode.Dynamic)]
    [InlineData(CompilationMode.OnlyRun)]
#if NET9_0_OR_GREATER
    [InlineData(CompilationMode.Persistence)]
#endif
    public async Task IntegerObjectKeysRemainExactAcrossReadsWritesAndMutations(CompilationMode mode)
    {
        using var workspace = new TestWorkspace();
        var (_, domain) = await workspace.CompileModuleAsync(
            """
            @module(TEST);
            export func run() {
                var o = {};
                o[9007199254740992L] = 10;
                o[9007199254740993L] = 20;
                o[18446744073709551614UL] = 30;
                o[18446744073709551615UL] = 40;
                o[9007199254740993L] += 2;
                var pre = ++o[9007199254740993L];
                var post = o[9007199254740993L]--;
                o[18446744073709551615UL] -= 2;
                var upre = --o[18446744073709551615UL];
                var upost = o[18446744073709551615UL]++;
                return [o, pre, post, upre, upost,
                    o[9007199254740992L], o[9007199254740993L],
                    o[18446744073709551614UL], o[18446744073709551615UL]];
            }
            export func read(o, key) { return o[key]; }
            export func write(o, key, value) { o[key] = value; }
            """, mode);

        var values = Assert.IsType<ScriptArray>(TestWorkspace.Execute(domain, "run").Object);
        var expected = new[] { 23, 23, 37, 37, 10, 22, 30, 38 };
        for (var i = 0; i < expected.Length; i++) ScriptAssert.Equal(expected[i], values.GetElement(i + 1));
        var obj = values.GetElement(0);
        var keys = new[] {
            ScriptDatum.FromInt64(9007199254740992L), ScriptDatum.FromInt64(9007199254740993L),
            ScriptDatum.FromUInt64(ulong.MaxValue - 1), ScriptDatum.FromUInt64(ulong.MaxValue)
        };
        for (var i = 0; i < keys.Length; i++)
        {
            ScriptAssert.Equal(expected[i + 4], TestWorkspace.Execute(domain, "read", arguments: [obj, keys[i]]));
            ScriptAssert.Equal(expected[i + 4], TestWorkspace.Execute(
                domain, "read", arguments: [obj, ScriptDatum.FromString(keys[i].ToString())]));
            TestWorkspace.Execute(domain, "write", arguments: [obj, keys[i], ScriptDatum.FromNumber(i)]);
            ScriptAssert.Equal(i, TestWorkspace.Execute(
                domain, "read", arguments: [obj, ScriptDatum.FromString(keys[i].ToString())]));
        }
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
    public async Task TypedIntegerConstantsKeepTheirAnnotations(CompilationMode mode, bool inline)
    {
        using var workspace = new TestWorkspace();
        var (_, domain) = await workspace.CompileModuleAsync(
            """
            @module(TEST);
            const signed = tdoc Int64 9007199254740993;
            const unsigned = tdoc UInt64 18446744073709551615;
            const small = tdoc UInt64 42;
            const minimum = tdoc Int64 -9223372036854775808;
            const alias = signed;
            const next = signed + 1L;
            native func defaultSigned(int64 value = tdoc Int64 9007199254740993) int64 { return value; }
            native func defaultUnsigned(uint64 value = tdoc UInt64 18446744073709551615) uint64 { return value; }
            export func run() { return [signed, unsigned, small, minimum, alias, next, defaultSigned(), defaultUnsigned()]; }
            """, mode, enableModuleConstInlining: inline);

        var values = Assert.IsType<ScriptArray>(TestWorkspace.Execute(domain, "run").Object);
        AssertInt64(9007199254740993L, values.GetElement(0));
        AssertUInt64(ulong.MaxValue, values.GetElement(1));
        AssertUInt64(42, values.GetElement(2));
        AssertInt64(long.MinValue, values.GetElement(3));
        AssertInt64(9007199254740993L, values.GetElement(4));
        AssertInt64(9007199254740994L, values.GetElement(5));
        AssertInt64(9007199254740993L, values.GetElement(6));
        AssertUInt64(ulong.MaxValue, values.GetElement(7));
    }

    [Fact]
    public void EqualIntegerDatumsHaveCompatibleHashesAcrossNumericKinds()
    {
        ScriptDatum[] values = [
            ScriptDatum.FromInt64(0), ScriptDatum.FromUInt64(0), ScriptDatum.FromNumber(-0d),
            ScriptDatum.FromInt64(1), ScriptDatum.FromUInt64(1), ScriptDatum.FromNumber(1),
            ScriptDatum.FromBoolean(true), ScriptDatum.FromBoolean(false),
            ScriptDatum.FromString("1"), ScriptDatum.FromString("01"), ScriptDatum.FromString("0"),
            ScriptDatum.FromInt64(long.MinValue), ScriptDatum.FromNumber(long.MinValue),
            ScriptDatum.FromInt64(long.MaxValue), ScriptDatum.FromUInt64((ulong)long.MaxValue),
            ScriptDatum.FromUInt64(ulong.MaxValue), ScriptDatum.FromNumber(ulong.MaxValue),
            ScriptDatum.FromInt64(9007199254740992L), ScriptDatum.FromInt64(9007199254740993L),
            ScriptDatum.FromNumber(9007199254740992d)
        ];
        foreach (var left in values)
        foreach (var right in values)
        {
            if (!left.Equals(right)) continue;
            Assert.Equal(left.GetHashCode(), right.GetHashCode());
            var set = new HashSet<ScriptDatum> { left };
            Assert.False(set.Add(right));
            Assert.True(new Dictionary<ScriptDatum, int> { [left] = 42 }.TryGetValue(right, out var result));
            Assert.Equal(42, result);
        }
        // Hash collisions must not collapse distinct exact integer values.
        Assert.Equal(2, new HashSet<ScriptDatum> {
            ScriptDatum.FromInt64(9007199254740992L), ScriptDatum.FromInt64(9007199254740993L)
        }.Count);
    }

    private static void AssertInt64(long expected, ScriptDatum actual)
    {
        Assert.Equal(ValueKind.Int64, actual.Kind);
        Assert.Equal(expected, actual.Int64);
    }

    private static void AssertUInt64(ulong expected, ScriptDatum actual)
    {
        Assert.Equal(ValueKind.UInt64, actual.Kind);
        Assert.Equal(expected, actual.UInt64);
    }

    private enum SignedEnum : long
    {
        Minimum = long.MinValue
    }

    private enum UnsignedEnum : ulong
    {
        Maximum = ulong.MaxValue
    }
}
