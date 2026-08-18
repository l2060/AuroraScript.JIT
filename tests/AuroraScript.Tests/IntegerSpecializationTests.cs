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

        @directCall
        func hashStep(value) {
            value = value ^ (value << 13);
            value = value ^ (value >> 17);
            return value ^ (value << 5);
        }

        @directCall
        func fillAndHash(values, count, seed) {
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

        @directCall
        func mix12(a, b, c, d, e, f, g, h, i, j, k, l) {
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
            return [hash, values.length, values[0], values[255], mixed];
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

#if NET9_0_OR_GREATER
    [Fact]
    public async Task PersistenceEmitsInt32KernelsAndRawArraySignatures()
    {
        using var workspace = new TestWorkspace();
        await workspace.CompileModuleAsync(IntegerKernelSource, CompilationMode.Persistence);

        var assemblyPath = Path.Combine(workspace.Root, "test-output.dll");
        using var stream = File.OpenRead(assemblyPath);
        using var peReader = new PEReader(stream);
        var reader = peReader.GetMetadataReader();

        var fill = FindMethod(reader, "fillAndHash$native");
        // DEFAULT, three parameters, return I4, then int[], I4, I4.
        Assert.Equal(
            [0x00, 0x03, 0x08, 0x1d, 0x08, 0x08, 0x08],
            reader.GetBlobBytes(fill.Signature));
        var fillOpcodes = ReadOpCodes(
            peReader.GetMethodBody(fill.RelativeVirtualAddress).GetILBytes().AsSpan());
        Assert.Contains(OpCodes.Ldelem_I4, fillOpcodes);
        Assert.Contains(OpCodes.Stelem_I4, fillOpcodes);
        Assert.Contains(OpCodes.Xor, fillOpcodes);
        Assert.DoesNotContain(OpCodes.Ldfld, fillOpcodes);
        Assert.DoesNotContain(OpCodes.Conv_R8, fillOpcodes);

        var mix = FindMethod(reader, "mix12$native");
        var expectedMixSignature = new byte[15];
        expectedMixSignature[0] = 0x00;
        expectedMixSignature[1] = 0x0c;
        expectedMixSignature[2] = 0x08;
        Array.Fill(expectedMixSignature, (byte)0x08, 3, 12);
        Assert.Equal(expectedMixSignature, reader.GetBlobBytes(mix.Signature));
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
        return [hash, values.Length, values[0], values[^1], mixed];
    }
}
