using AuroraScript.Runtime;
using AuroraScript.Tests.Infrastructure;
using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using System.Reflection.PortableExecutable;
using System.Threading.Tasks;
using Xunit;

namespace AuroraScript.Tests;

public sealed class IntegerSpecializationTests
{
#if NET9_0_OR_GREATER
    private static readonly IReadOnlyDictionary<ushort, OpCode> CilOpCodes =
        typeof(OpCodes)
            .GetFields(BindingFlags.Public | BindingFlags.Static)
            .Where(field => field.FieldType == typeof(OpCode))
            .Select(field => (OpCode)field.GetValue(null)!)
            .ToDictionary(opcode => unchecked((ushort)opcode.Value));
#endif

    private const string IntegerKernelSource = """
        @module(TEST);

        native func hashStep(Number value) Number {
            value = value ^ (value << 13);
            value = value ^ (value >> 17);
            return value ^ (value << 5);
        }

        native func addUnsigned(Number left, Number right) Number {
            return (left + right) | 0;
        }

        native func allocate(int32 count) int32 {
            var values = new Int8Array(count);
            return values.length;
        }

        native func fillAndHash(Int32Array values, Number count, Number seed) Number {
            var state = seed;
            for (var i = 0; i < count; i++) {
                state = hashStep(state);
                values[i] = state;
            }

            var hash = 0;
            for (var j = 0; j < count; j++) {
                hash = hash ^ values[j];
            }
            return hash;
        }

        native func mix12(Number a, Number b, Number c, Number d, Number e, Number f, Number g, Number h, Number i, Number j, Number k, Number l) Number {
            a = a ^ (b << 1);
            c = c ^ (d << 2);
            e = e ^ (f << 3);
            g = g ^ (h << 4);
            i = i ^ (j << 5);
            k = k ^ (l << 6);
            return a ^ c ^ e ^ g ^ i ^ k;
        }

        export func run() {
            var values = new Int32Array(256);
            var hash = fillAndHash(values, values.length, 123456789);
            var mixed = mix12(1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12);
            var wrapped = addUnsigned(2147483647, 1);
            return [hash, values.length, values[0], values[255], mixed, wrapped];
        }
        """;

    [Theory]
    [InlineData(CompilationMode.Dynamic)]
    [InlineData(CompilationMode.OnlyRun)]
#if NET9_0_OR_GREATER
    [InlineData(CompilationMode.Persistence)]
#endif
    public async Task LowercaseInt32ContractsCheckBoundariesAndKeepNumberIdentity(
        CompilationMode mode)
    {
        using var workspace = new TestWorkspace();
        var (_, domain) = await workspace.CompileModuleAsync(
            """
            @module(TEST);

            export type Counter {
                int32 value;
            }

            export native func increment(int32 value) int32 {
                return value + 1;
            }

            export native func bump(int32 value) int32 {
                value++;
                return value;
            }

            export native func index(int32 x, int32 y, int32 width) int32 {
                return y * width + x;
            }

            func relay(int32 value) int32 {
                return increment(value);
            }

            export func run(value) {
                var checked = value as int32;
                var counter = { value: checked } as Counter;
                return [relay(counter.value), typeof checked];
            }

            export func readField(value) int32 {
                var counter = { value: value } as Counter;
                return counter.value;
            }
            """,
            mode);

        ScriptAssert.Equal(
            new object?[] { 42, "number" },
            TestWorkspace.Execute(
                domain,
                "run",
                arguments: [ScriptDatum.FromNumber(41)]));
        ScriptAssert.Equal(
            int.MinValue,
            TestWorkspace.Execute(
                domain,
                "readField",
                arguments: [ScriptDatum.FromNumber(int.MinValue)]));
        ScriptAssert.Equal(
            42,
            TestWorkspace.Execute(
                domain,
                "bump",
                arguments: [ScriptDatum.FromNumber(41)]));
        ScriptAssert.Equal(
            int.MinValue,
            TestWorkspace.Execute(
                domain,
                "bump",
                arguments: [ScriptDatum.FromNumber(int.MaxValue)]));
        ScriptAssert.Equal(
            32,
            TestWorkspace.Execute(
                domain,
                "index",
                arguments: [
                    ScriptDatum.FromNumber(2),
                    ScriptDatum.FromNumber(3),
                    ScriptDatum.FromNumber(10)]));
        ScriptAssert.Equal(
            int.MinValue,
            TestWorkspace.Execute(
                domain,
                "increment",
                arguments: [ScriptDatum.FromNumber(int.MaxValue)]));

        Assert.Throws<AuroraRuntimeException>(() =>
            TestWorkspace.Execute(
                domain,
                "run",
                arguments: [ScriptDatum.FromNumber(1.5)]));
        Assert.Throws<AuroraRuntimeException>(() =>
            TestWorkspace.Execute(
                domain,
                "increment",
                arguments: [ScriptDatum.FromNumber(1.5)]));
        Assert.Throws<AuroraRuntimeException>(() =>
            TestWorkspace.Execute(
                domain,
                "increment",
                arguments: [
                    ScriptDatum.FromNumber(
                        BitConverter.Int64BitsToDouble(long.MinValue))]));
        Assert.Throws<AuroraRuntimeException>(() =>
            TestWorkspace.Execute(
                domain,
                "run",
                arguments: [ScriptDatum.FromNumber((double)int.MaxValue + 1)]));
        Assert.Throws<AuroraRuntimeException>(() =>
            TestWorkspace.Execute(
                domain,
                "readField",
                arguments: [ScriptDatum.FromNumber(1.5)]));
    }

    [Theory]
    [InlineData(CompilationMode.Dynamic)]
    [InlineData(CompilationMode.OnlyRun)]
#if NET9_0_OR_GREATER
    [InlineData(CompilationMode.Persistence)]
#endif
    public async Task LowercaseUInt32ContractsPreserveUnsignedWordSemantics(
        CompilationMode mode)
    {
        using var workspace = new TestWorkspace();
        var (_, domain) = await workspace.CompileModuleAsync(
            """
            @module(TEST);

            export type Word {
                uint32 value;
            }

            export native func identity(uint32 value) uint32 {
                return value;
            }

            export func check(value) {
                var checked = value as uint32;
                var word = { value: checked } as Word;
                return [identity(word.value), typeof checked];
            }

            export func readField(value) uint32 {
                var word = { value: value } as Word;
                return word.value;
            }

            export func checkedReturn(value) uint32 {
                return value;
            }

            export native func operations() Array {
                var values = new UInt32Array(2);
                values[0] = 0xFFFFFFFFu;
                values[1] = values[0] + 1u;
                var max = values[0];
                max += 1u;
                var post = values[0]++;
                var pre = ++values[0];
                values[0] -= 2u;
                var divided = new UInt32Array(1);
                divided[0] = 0xFFFFFFFFu;
                var quotient = divided[0] /= 1D;
                return [
                    values[0],
                    values[1],
                    max,
                    post,
                    pre,
                    0u - 1u,
                    0x80000000u * 2u,
                    0xFFFFFFFFu % 16u,
                    0x80000000u >> 31,
                    1u << 31,
                    ~0u,
                    divided[0],
                    quotient,
                    0xFFFFFFFFu > 1u,
                    0xFFFFFFFFu < 1u,
                    typeof values[0]
                ];
            }

            export func negativeZero() {
                return 1 / -0u;
            }

            export func remainderByZero() {
                return 1u % 0u;
            }
            """,
            mode);

        ScriptAssert.Equal(
            new object?[] { uint.MaxValue, "number" },
            TestWorkspace.Execute(
                domain,
                "check",
                arguments: [ScriptDatum.FromNumber(uint.MaxValue)]));
        ScriptAssert.Equal(
            uint.MaxValue,
            TestWorkspace.Execute(
                domain,
                "readField",
                arguments: [ScriptDatum.FromNumber(uint.MaxValue)]));
        ScriptAssert.Equal(
            new object?[]
            {
                uint.MaxValue, 0u, 0u, uint.MaxValue, 1u, uint.MaxValue, 0u, 15u,
                1u, 0x80000000u, uint.MaxValue, uint.MaxValue, uint.MaxValue,
                true, false, "number"
            },
            TestWorkspace.Execute(domain, "operations"));
        ScriptAssert.Equal(
            double.NegativeInfinity,
            TestWorkspace.Execute(domain, "negativeZero"));

        foreach (var invalid in new[]
        {
            -1d,
            1.5d,
            (double)uint.MaxValue + 1d,
            BitConverter.Int64BitsToDouble(long.MinValue)
        })
        {
            Assert.Throws<AuroraRuntimeException>(() =>
                TestWorkspace.Execute(
                    domain,
                    "identity",
                    arguments: [ScriptDatum.FromNumber(invalid)]));
            Assert.Throws<AuroraRuntimeException>(() =>
                TestWorkspace.Execute(
                    domain,
                    "check",
                    arguments: [ScriptDatum.FromNumber(invalid)]));
            Assert.Throws<AuroraRuntimeException>(() =>
                TestWorkspace.Execute(
                    domain,
                    "readField",
                    arguments: [ScriptDatum.FromNumber(invalid)]));
            Assert.Throws<AuroraRuntimeException>(() =>
                TestWorkspace.Execute(
                    domain,
                    "checkedReturn",
                    arguments: [ScriptDatum.FromNumber(invalid)]));
        }
        Assert.Throws<AuroraRuntimeException>(() =>
            TestWorkspace.Execute(domain, "remainderByZero"));
    }

    [Fact]
    public async Task PascalCaseInt32IsNotAConstraintType()
    {
        using var workspace = new TestWorkspace();
        await Assert.ThrowsAsync<AuroraCompilationException>(() =>
            workspace.CompileModuleAsync(
                """
                @module(TEST);
                export func run(Int32 value) {
                    return value;
                }
                """,
                CompilationMode.Dynamic));
    }

    [Fact]
    public async Task UInt32LiteralAndTypeSpellingsAreValidated()
    {
        using var workspace = new TestWorkspace();
        await Assert.ThrowsAsync<AuroraCompilationException>(() =>
            workspace.CompileModuleAsync(
                """
                @module(TEST);
                export func run(UInt32 value) {
                    return value;
                }
                """,
                CompilationMode.Dynamic));

        foreach (var literal in new[] { "4294967296u", "1.5u" })
        {
            await Assert.ThrowsAsync<AuroraCompilationException>(() =>
                workspace.CompileModuleAsync(
                    "@module(TEST); export func run() { return " + literal + "; }",
                    CompilationMode.Dynamic));
        }
    }

    [Theory]
    [InlineData(CompilationMode.Dynamic)]
    [InlineData(CompilationMode.OnlyRun)]
#if NET9_0_OR_GREATER
    [InlineData(CompilationMode.Persistence)]
#endif
    public async Task IntegerKernelsStayCorrectAcrossCompilationModes(CompilationMode mode)
    {
        using var workspace = new TestWorkspace();
        var (engine, domain) = await workspace.CompileModuleAsync(IntegerKernelSource, mode);
        var expected = BuildExpectedKernelResult();

        ScriptAssert.Equal(expected, TestWorkspace.Execute(domain, "run"));
        if (mode == CompilationMode.Persistence)
        {
            ScriptAssert.Equal(expected, TestWorkspace.Execute(engine.CreateDomain(), "run"));
        }
    }

    [Theory]
    [InlineData(CompilationMode.Dynamic)]
    [InlineData(CompilationMode.OnlyRun)]
#if NET9_0_OR_GREATER
    [InlineData(CompilationMode.Persistence)]
#endif
    public async Task IntegerLocalsWrapWhileNumberLocalsKeepScriptSemantics(CompilationMode mode)
    {
        using var workspace = new TestWorkspace();
        var (_, domain) = await workspace.CompileModuleAsync(
            """
            @module(TEST);
            export func run(useFraction) {
                var max = 2147483647;
                var overflow = max + 1;
                max += 1;

                var values = new Int32Array(1);
                values[0] = 2147483647;
                var fromArray = values[0] + 1;
                var merged = values[0];
                if (useFraction) merged = 0.5;

                var negativeZero = -0;
                return [
                    overflow,
                    max,
                    fromArray,
                    merged + 0,
                    1 / negativeZero,
                    4294967295 | 0,
                    -1 >>> 0,
                    (-2147483648) >> 0
                ];
            }
            """,
            mode);

        // `max` and `fromArray` only ever hold integers, so they keep native
        // int storage and wrap. `merged` is assigned 0.5 on one branch, so it
        // stays a Number, and `-0` keeps its sign because a 32-bit slot cannot.
        ScriptAssert.Equal(
            new object?[]
            {
                int.MinValue, int.MinValue, int.MinValue, 2147483647d,
                double.NegativeInfinity, -1, 4294967295d, int.MinValue
            },
            TestWorkspace.Execute(domain, "run", arguments: ScriptDatum.FromBoolean(false)));
        ScriptAssert.Equal(
            new object?[]
            {
                int.MinValue, int.MinValue, int.MinValue, 0.5d,
                double.NegativeInfinity, -1, 4294967295d, int.MinValue
            },
            TestWorkspace.Execute(domain, "run", arguments: ScriptDatum.FromBoolean(true)));
    }

    [Theory]
    [InlineData(CompilationMode.Dynamic)]
    [InlineData(CompilationMode.OnlyRun)]
#if NET9_0_OR_GREATER
    [InlineData(CompilationMode.Persistence)]
#endif
    public async Task IntegerRemainderStaysIntegralWhileNumberRemainderKeepsScriptSemantics(
        CompilationMode mode)
    {
        using var workspace = new TestWorkspace();
        var (_, domain) = await workspace.CompileModuleAsync(
            """
            @module(TEST);
            export func run() {
                var value = 1000000;
                value++;
                value--;
                var divisor = 7;
                var narrow = value % divisor;

                var wide = 3000000000;
                var wideRemainder = wide % divisor;

                var negative = -14;
                var signedZero = negative % divisor;

                var fraction = 14.5;
                var numberRemainder = fraction % divisor;
                var nan = fraction % 0;
                return [narrow, wideRemainder, 1 / signedZero, numberRemainder, nan];
            }

            export func remainderByZero() {
                var value = 10;
                var zero = 0;
                return value % zero;
            }
            """,
            mode);

        // Both operands are integers, so the remainder is an integer and
        // cannot carry the negative zero a Number remainder would.
        ScriptAssert.Equal(
            new object?[]
            {
                1, 4L, double.PositiveInfinity, 0.5d, double.NaN
            },
            TestWorkspace.Execute(domain, "run"));
        Assert.Throws<AuroraRuntimeException>(() =>
            TestWorkspace.Execute(domain, "remainderByZero"));
    }

    [Theory]
    [InlineData(CompilationMode.Dynamic)]
    [InlineData(CompilationMode.OnlyRun)]
#if NET9_0_OR_GREATER
    [InlineData(CompilationMode.Persistence)]
#endif
    public async Task ExactLargeIntegersUseInt64WhileFractionsUseDouble(CompilationMode mode)
    {
        using var workspace = new TestWorkspace();
        var (engine, domain) = await workspace.CompileModuleAsync(
            """
            @module(TEST);

            native func largeValue() Number {
                var value = 3000000000;
                return value;
            }

            native func fractionalValue() Number {
                var value = 1;
                value = 1.25;
                return value;
            }

            export func run() {
                return [largeValue(), fractionalValue()];
            }
            """,
            mode);

        ScriptAssert.Equal(
            new object?[] { 3000000000d, 1.25d },
            TestWorkspace.Execute(domain, "run"));
        if (mode == CompilationMode.Persistence)
        {
            ScriptAssert.Equal(
                new object?[] { 3000000000d, 1.25d },
                TestWorkspace.Execute(engine.CreateDomain(), "run"));
        }
    }

    [Theory]
    [InlineData(CompilationMode.Dynamic)]
    [InlineData(CompilationMode.OnlyRun)]
#if NET9_0_OR_GREATER
    [InlineData(CompilationMode.Persistence)]
#endif
    public async Task TruncatedIntegerStorageAndStringLengthKeepScriptSemantics(
        CompilationMode mode)
    {
        using var workspace = new TestWorkspace();
        var (_, domain) = await workspace.CompileModuleAsync(
            """
            @module(TEST);

            native func truncate(Number value) Number {
                return value | 0;
            }

            export native func run(String text) Array {
                var wide = 4023233417;
                var copy = wide;
                var visible = 4023233417;
                var visits = 0;
                for (var i = 0; i < text.length; i++) {
                    if (i + 1 < text.length) i++;
                    visits++;
                }
                var values = new Int32Array(32);
                for (var k = 0; k < values.length; k += 16) {
                    values[k + 15] = k + 15;
                }
                var invalidCode = text.charCodeAt(-1);
                return [
                    truncate(wide),
                    truncate(copy),
                    visible,
                    text.length,
                    "".length,
                    visits,
                    values[15],
                    values[31],
                    text.charCodeAt(0),
                    invalidCode != invalidCode
                ];
            }
            """,
            mode);

        ScriptAssert.Equal(
            new object?[]
            {
                -271733879, -271733879, 4023233417d, 3, 0, 2, 15, 31,
                97, true
            },
            TestWorkspace.Execute(domain, "run", arguments: ScriptDatum.FromString("a\u00e9\ud83d")));
    }

    [Theory]
    [InlineData(CompilationMode.Dynamic)]
    [InlineData(CompilationMode.OnlyRun)]
#if NET9_0_OR_GREATER
    [InlineData(CompilationMode.Persistence)]
#endif
    public async Task CountedLoopsOverNumberBoundsKeepDoubleComparisonSemantics(
        CompilationMode mode)
    {
        using var workspace = new TestWorkspace();
        var (_, domain) = await workspace.CompileModuleAsync(
            """
            @module(TEST);

            export native func run(Number limit) Array {
                var visits = 0;
                var last = -1;
                for (var i = 0; i < limit; i++) {
                    visits++;
                    last = i;
                }
                var stepped = 0;
                for (var k = 0; k < limit; k += 3) {
                    stepped = stepped + k;
                }
                var cells = new Int32Array(8);
                var stored = 0;
                for (var c = 0; c < limit; c++) {
                    if (c < 8) {
                        cells[c] = c * 2;
                        stored++;
                    }
                }
                return [visits, last, stepped, stored, cells[0], cells[3]];
            }
            """,
            mode);

        // An integral bound behaves exactly like the double comparison.
        ScriptAssert.Equal(
            new object?[] { 5, 4, 3, 5, 0, 6 },
            TestWorkspace.Execute(domain, "run", arguments: ScriptDatum.FromNumber(5)));

        // A fractional bound still admits every integer below it.
        ScriptAssert.Equal(
            new object?[] { 4, 3, 3, 4, 0, 6 },
            TestWorkspace.Execute(domain, "run", arguments: ScriptDatum.FromNumber(3.5)));

        // NaN and non-positive bounds skip the loop like `i < NaN` would.
        ScriptAssert.Equal(
            new object?[] { 0, -1, 0, 0, 0, 0 },
            TestWorkspace.Execute(domain, "run", arguments: ScriptDatum.FromNumber(double.NaN)));
        ScriptAssert.Equal(
            new object?[] { 0, -1, 0, 0, 0, 0 },
            TestWorkspace.Execute(domain, "run", arguments: ScriptDatum.FromNumber(0)));
    }

#if NET9_0_OR_GREATER
    [Fact]
    public async Task PersistenceEmitsNativeUInt32WordKernel()
    {
        using var workspace = new TestWorkspace();
        await workspace.CompileModuleAsync(
            """
            @module(TEST);

            const ADDEND = 0xD76AA478u;

            native func rotate(uint32 value, int32 shift) uint32 {
                return (value << shift) | (value >> (32 - shift));
            }

            native func step(uint32 value, uint32 addend) uint32 {
                return rotate(value + addend, 7);
            }

            export native func process(UInt32Array values) uint32 {
                values[1] = 1;
                values[0] = step(values[0], ADDEND);
                values[0] += 1u;
                values[0]--;
                return values[0];
            }
            """,
            CompilationMode.Persistence,
            enableModuleConstInlining: true);

        using var stream = File.OpenRead(Path.Combine(workspace.Root, "test-output.dll"));
        using var peReader = new PEReader(stream);
        var reader = peReader.GetMetadataReader();

        var rotate = FindMethod(reader, "rotate$native");
        var rotateSignature = reader.GetBlobBytes(rotate.Signature);
        Assert.Equal(3, rotateSignature[1]);
        Assert.Equal(0x09, rotateSignature[2]); // System.UInt32 return.
        var rotateIl = peReader.GetMethodBody(rotate.RelativeVirtualAddress).GetILBytes();
        var rotateOpcodes = ReadOpCodes(rotateIl.AsSpan());
        Assert.Contains(OpCodes.Shl, rotateOpcodes);
        Assert.Contains(OpCodes.Shr_Un, rotateOpcodes);
        Assert.Contains(OpCodes.Or, rotateOpcodes);
        Assert.DoesNotContain(OpCodes.Conv_R8, rotateOpcodes);
        Assert.DoesNotContain(OpCodes.Conv_R_Un, rotateOpcodes);
        AssertNoNumericChecks(reader, rotateIl);

        var step = FindMethod(reader, "step$native");
        Assert.Equal(0x09, reader.GetBlobBytes(step.Signature)[2]);
        var stepIl = peReader.GetMethodBody(step.RelativeVirtualAddress).GetILBytes();
        var stepOpcodes = ReadOpCodes(stepIl.AsSpan());
        Assert.Contains(OpCodes.Add, stepOpcodes);
        Assert.DoesNotContain(OpCodes.Conv_R8, stepOpcodes);
        Assert.DoesNotContain(OpCodes.Conv_R_Un, stepOpcodes);
        AssertNoNumericChecks(reader, stepIl);

        var process = FindMethod(reader, "process$native");
        Assert.Equal(0x09, reader.GetBlobBytes(process.Signature)[2]);
        var processIl = peReader.GetMethodBody(process.RelativeVirtualAddress).GetILBytes();
        var processOpcodes = ReadOpCodes(processIl.AsSpan());
        Assert.Contains(OpCodes.Ldelem_U4, processOpcodes);
        Assert.Contains(OpCodes.Stelem_I4, processOpcodes);
        Assert.DoesNotContain(OpCodes.Conv_R8, processOpcodes);
        Assert.DoesNotContain(OpCodes.Conv_R_Un, processOpcodes);
        AssertNoNumericChecks(reader, processIl);
    }

    [Fact]
    public async Task PersistenceEmitsExplicitNativeRawArraySignatures()
    {
        using var workspace = new TestWorkspace();
        await workspace.CompileModuleAsync(IntegerKernelSource, CompilationMode.Persistence);

        var assemblyPath = Path.Combine(workspace.Root, "test-output.dll");
        using var stream = File.OpenRead(assemblyPath);
        using var peReader = new PEReader(stream);
        var reader = peReader.GetMetadataReader();

        var fill = FindMethod(reader, "fillAndHash$native");
        var fillSignature = reader.GetBlobBytes(fill.Signature);
        Assert.Equal(4, fillSignature[1]); // ScriptContext plus three parameters.
        Assert.Equal(0x08, fillSignature[2]); // Inferred native Int32 return.
        Assert.Contains((byte)0x1d, fillSignature); // Raw int[] parameter.
        var fillOpcodes = ReadOpCodes(
            peReader.GetMethodBody(fill.RelativeVirtualAddress).GetILBytes().AsSpan());
        Assert.Contains(OpCodes.Ldelem_I4, fillOpcodes);
        Assert.Contains(OpCodes.Stelem_I4, fillOpcodes);
        Assert.Contains(OpCodes.Xor, fillOpcodes);
        Assert.DoesNotContain(OpCodes.Ldfld, fillOpcodes);

        var mix = FindMethod(reader, "mix12$native");
        var mixSignature = reader.GetBlobBytes(mix.Signature);
        Assert.Equal(13, mixSignature[1]); // ScriptContext plus twelve parameters.
        Assert.Equal(0x08, mixSignature[2]);

        var hashStep = FindMethod(reader, "hashStep$native");
        var hashStepSignature = reader.GetBlobBytes(hashStep.Signature);
        Assert.Equal(0x08, hashStepSignature[2]);

        var hashStepOpcodes = ReadOpCodes(
            peReader.GetMethodBody(hashStep.RelativeVirtualAddress).GetILBytes().AsSpan());
        Assert.DoesNotContain(OpCodes.Conv_R8, hashStepOpcodes);

        var addUnsigned = FindMethod(reader, "addUnsigned$native");
        var addUnsignedOpcodes = ReadOpCodes(
            peReader.GetMethodBody(addUnsigned.RelativeVirtualAddress).GetILBytes().AsSpan());
        Assert.Contains(OpCodes.Add, addUnsignedOpcodes);
        Assert.Contains(OpCodes.Or, addUnsignedOpcodes);
        Assert.DoesNotContain(OpCodes.Conv_R8, addUnsignedOpcodes);

        var allocate = FindMethod(reader, "allocate$native");
        var allocateOpcodes = ReadOpCodes(
            peReader.GetMethodBody(allocate.RelativeVirtualAddress).GetILBytes().AsSpan());
        Assert.Contains(OpCodes.Newarr, allocateOpcodes);
        Assert.DoesNotContain(OpCodes.Conv_R8, allocateOpcodes);
    }

    [Fact]
    public async Task PersistenceUsesDoubleForUnsuffixedLargeIntegerAndNativeTypesForSuffixes()
    {
        using var workspace = new TestWorkspace();
        await workspace.CompileModuleAsync(
            """
            @module(TEST);
            native func largeValue() Number { return 3000000000; }
            native func fractionalValue() Number { return 1.25; }
            native func signedValue() int64 { return 3000000000L; }
            native func unsignedValue() uint64 { return 3000000000UL; }
            export func run() { return [largeValue(), fractionalValue(), signedValue(), unsignedValue()]; }
            """,
            CompilationMode.Persistence);

        using var stream = File.OpenRead(Path.Combine(workspace.Root, "test-output.dll"));
        using var peReader = new PEReader(stream);
        var reader = peReader.GetMetadataReader();
        Assert.Equal(0x0d, reader.GetBlobBytes(FindMethod(reader, "largeValue$native").Signature)[2]);
        Assert.Equal(0x0d, reader.GetBlobBytes(FindMethod(reader, "fractionalValue$native").Signature)[2]);
        Assert.Equal(0x0a, reader.GetBlobBytes(FindMethod(reader, "signedValue$native").Signature)[2]);
        Assert.Equal(0x0b, reader.GetBlobBytes(FindMethod(reader, "unsignedValue$native").Signature)[2]);
    }

    [Fact]
    public async Task PersistenceEmitsNativeInt64AndUInt64Kernels()
    {
        using var workspace = new TestWorkspace();
        await workspace.CompileModuleAsync(
            """
            @module(TEST);

            native func signedKernel(int64 value, int64 addend, int64 shift) int64 {
                return (value + addend) ^ (value << shift);
            }

            native func unsignedKernel(uint64 value, uint64 addend, uint64 shift) uint64 {
                return (value + addend) ^ (value >> shift);
            }

            export func run() {
                return [signedKernel(1L, 2L, 3L), unsignedKernel(8UL, 1UL, 2UL)];
            }
            """,
            CompilationMode.Persistence);

        using var stream = File.OpenRead(Path.Combine(workspace.Root, "test-output.dll"));
        using var peReader = new PEReader(stream);
        var reader = peReader.GetMetadataReader();

        var signed = FindMethod(reader, "signedKernel$native");
        var signedSignature = reader.GetBlobBytes(signed.Signature);
        Assert.Equal(4, signedSignature[1]); // ScriptContext plus three Int64 parameters.
        Assert.Equal(0x0a, signedSignature[2]);
        Assert.Equal(new byte[] { 0x0a, 0x0a, 0x0a }, signedSignature[^3..]);
        var signedIl = peReader.GetMethodBody(signed.RelativeVirtualAddress).GetILBytes();
        var signedOpcodes = ReadOpCodes(signedIl.AsSpan());
        Assert.Contains(OpCodes.Add, signedOpcodes);
        Assert.Contains(OpCodes.Xor, signedOpcodes);
        Assert.Contains(OpCodes.Shl, signedOpcodes);
        Assert.DoesNotContain(OpCodes.Conv_R8, signedOpcodes);
        Assert.DoesNotContain(OpCodes.Conv_R_Un, signedOpcodes);
        AssertNoNumericChecks(reader, signedIl);

        var unsigned = FindMethod(reader, "unsignedKernel$native");
        var unsignedSignature = reader.GetBlobBytes(unsigned.Signature);
        Assert.Equal(4, unsignedSignature[1]); // ScriptContext plus three UInt64 parameters.
        Assert.Equal(0x0b, unsignedSignature[2]);
        Assert.Equal(new byte[] { 0x0b, 0x0b, 0x0b }, unsignedSignature[^3..]);
        var unsignedIl = peReader.GetMethodBody(unsigned.RelativeVirtualAddress).GetILBytes();
        var unsignedOpcodes = ReadOpCodes(unsignedIl.AsSpan());
        Assert.Contains(OpCodes.Add, unsignedOpcodes);
        Assert.Contains(OpCodes.Xor, unsignedOpcodes);
        Assert.Contains(OpCodes.Shr_Un, unsignedOpcodes);
        Assert.DoesNotContain(OpCodes.Conv_R8, unsignedOpcodes);
        Assert.DoesNotContain(OpCodes.Conv_R_Un, unsignedOpcodes);
        AssertNoNumericChecks(reader, unsignedIl);
    }

    private static MethodDefinition FindMethod(MetadataReader reader, string name)
    {
        foreach (var handle in reader.MethodDefinitions)
        {
            var method = reader.GetMethodDefinition(handle);
            if (string.Equals(reader.GetString(method.Name), name, StringComparison.Ordinal))
            {
                return method;
            }
        }
        throw new Xunit.Sdk.XunitException("Persisted method not found: " + name);
    }

    private static List<OpCode> ReadOpCodes(ReadOnlySpan<byte> il)
    {
        var result = new List<OpCode>();
        var offset = 0;
        while (offset < il.Length)
        {
            ushort value = il[offset++];
            if (value == 0xfe)
            {
                Assert.True(offset < il.Length, "Truncated two-byte CIL opcode.");
                value = (ushort)(0xfe00 | il[offset++]);
            }
            Assert.True(CilOpCodes.TryGetValue(value, out var opcode), $"Unknown CIL opcode 0x{value:x4}.");
            result.Add(opcode);
            offset += GetOperandSize(opcode.OperandType, il.Slice(offset));
            Assert.True(offset <= il.Length, "Truncated CIL operand.");
        }
        return result;
    }

    private static void AssertNoNumericChecks(MetadataReader reader, ReadOnlySpan<byte> il)
    {
        foreach (var methodName in new[]
        {
            nameof(TypeCheckOps.CheckInt32Number),
            nameof(TypeCheckOps.CheckUInt32Number),
            nameof(ValueOps.ToArithmeticNumber)
        })
        {
            var tokens = reader.MemberReferences
                .Where(handle =>
                {
                    var member = reader.GetMemberReference(handle);
                    return member.Parent.Kind == HandleKind.TypeReference &&
                        string.Equals(
                            reader.GetString(member.Name),
                            methodName,
                            StringComparison.Ordinal);
                })
                .Select(handle => MetadataTokens.GetToken(handle))
                .ToHashSet();
            Assert.Equal(0, CountCalls(il, tokens));
        }
    }

    private static int CountCalls(ReadOnlySpan<byte> il, HashSet<int> metadataTokens)
    {
        var count = 0;
        var offset = 0;
        while (offset < il.Length)
        {
            ushort value = il[offset++];
            if (value == 0xfe)
            {
                value = (ushort)(0xfe00 | il[offset++]);
            }
            var opcode = CilOpCodes[value];
            if (opcode == OpCodes.Call &&
                metadataTokens.Contains(BinaryPrimitives.ReadInt32LittleEndian(il.Slice(offset, 4))))
            {
                count++;
            }
            offset += GetOperandSize(opcode.OperandType, il.Slice(offset));
        }
        return count;
    }

    private static int GetOperandSize(OperandType operandType, ReadOnlySpan<byte> remaining)
    {
        return operandType switch
        {
            OperandType.InlineNone => 0,
            OperandType.ShortInlineBrTarget or OperandType.ShortInlineI or OperandType.ShortInlineVar => 1,
            OperandType.InlineVar => 2,
            OperandType.InlineBrTarget or OperandType.InlineField or OperandType.InlineI or
                OperandType.InlineMethod or OperandType.InlineSig or OperandType.InlineString or
                OperandType.InlineTok or OperandType.InlineType or OperandType.ShortInlineR => 4,
            OperandType.InlineI8 or OperandType.InlineR => 8,
            OperandType.InlineSwitch => 4 + BinaryPrimitives.ReadInt32LittleEndian(remaining) * 4,
            _ => throw new InvalidOperationException("Unsupported CIL operand type: " + operandType)
        };
    }
#endif

    private static object?[] BuildExpectedKernelResult()
    {
        var values = new int[256];
        var state = 123456789;
        var hash = 0;
        unchecked
        {
            for (var index = 0; index < values.Length; index++)
            {
                state ^= state << 13;
                state ^= state >> 17;
                state ^= state << 5;
                values[index] = state;
            }
            for (var index = 0; index < values.Length; index++) hash ^= values[index];
        }

        var mixed = (1 ^ (2 << 1)) ^
            (3 ^ (4 << 2)) ^
            (5 ^ (6 << 3)) ^
            (7 ^ (8 << 4)) ^
            (9 ^ (10 << 5)) ^
            (11 ^ (12 << 6));
        return [hash, values.Length, values[0], values[^1], mixed, int.MinValue];
    }
}
