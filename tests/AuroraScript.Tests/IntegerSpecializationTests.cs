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
    public async Task Int32SpecializationWidensAtNumberSemanticBoundaries(CompilationMode mode)
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

        ScriptAssert.Equal(
            new object?[]
            {
                2147483648d, 2147483648d, 2147483648d, 2147483647d,
                double.NegativeInfinity, -1, 4294967295d, int.MinValue
            },
            TestWorkspace.Execute(domain, "run", arguments: ScriptDatum.FromBoolean(false)));
        ScriptAssert.Equal(
            new object?[]
            {
                2147483648d, 2147483648d, 2147483648d, 0.5d,
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
    }

    [Fact]
    public async Task PersistenceUsesInt64ForExactLargeIntegerAndDoubleForFraction()
    {
        using var workspace = new TestWorkspace();
        await workspace.CompileModuleAsync(
            """
            @module(TEST);
            native func largeValue() Number { return 3000000000; }
            native func fractionalValue() Number { return 1.25; }
            export func run() { return [largeValue(), fractionalValue()]; }
            """,
            CompilationMode.Persistence);

        using var stream = File.OpenRead(Path.Combine(workspace.Root, "test-output.dll"));
        using var peReader = new PEReader(stream);
        var reader = peReader.GetMetadataReader();
        Assert.Equal(0x0a, reader.GetBlobBytes(FindMethod(reader, "largeValue$native").Signature)[2]);
        Assert.Equal(0x0d, reader.GetBlobBytes(FindMethod(reader, "fractionalValue$native").Signature)[2]);
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
