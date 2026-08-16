using AuroraScript.Runtime;
using AuroraScript.Runtime.Types;
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

public sealed class PackedArrayTests
{
#if NET9_0_OR_GREATER
    private static readonly IReadOnlyDictionary<ushort, OpCode> CilOpCodes =
        typeof(OpCodes)
            .GetFields(BindingFlags.Public | BindingFlags.Static)
            .Where(field => field.FieldType == typeof(OpCode))
            .Select(field => (OpCode)field.GetValue(null)!)
            .ToDictionary(opcode => unchecked((ushort)opcode.Value));
#endif

    [Fact]
    public void PackedArraysUsePrimitiveClrStorage()
    {
        var int32 = new ScriptInt32Array(1_000);
        var int8 = new ScriptInt8Array(1_000);
        var boolean = new ScriptBooleanArray(1_000);

        Assert.IsType<int[]>(int32._items);
        Assert.IsType<sbyte[]>(int8._items);
        Assert.IsType<bool[]>(boolean._items);
        Assert.Equal(1_000, int32.Length);
        Assert.Equal(1_000, int8.Length);
        Assert.Equal(1_000, boolean.Length);
    }

    [Theory]
    [InlineData(CompilationMode.Dynamic)]
    [InlineData(CompilationMode.OnlyRun)]
#if NET9_0_OR_GREATER
    [InlineData(CompilationMode.Persistence)]
#endif
    public async Task PackedArraySemanticsMatchAcrossCompilationModes(CompilationMode mode)
    {
        using var workspace = new TestWorkspace();
        var (engine, domain) = await workspace.CompileModuleAsync(
            """
            @module(TEST);
            export func run() {
                var ints = new Int32Array(4);
                var bytes = new Int8Array(3);
                var flags = new BooleanArray(3);

                ints.fill(7);
                ints[1] = 42;
                bytes[0] = 130;
                bytes[1] = -129;
                flags[0] = 1;
                flags[1] = 0;
                flags[2] = "non-empty";

                var sum = 0;
                for (var value in ints) sum += value;
                var copied = Array.from(bytes);
                var spread = [...flags];

                return [
                    ints.length, ints[1], sum,
                    bytes[0], bytes[1], copied[0],
                    flags[0], flags[1], flags[2], spread[2],
                    Array.isArray(ints), JSON.stringify(ints)
                ];
            }
            """,
            mode);

        ScriptAssert.Equal(
            new object?[]
            {
                4, 42, 63,
                -126, 127, -126,
                true, false, true, true,
                false, "[7,42,7,7]"
            },
            TestWorkspace.Execute(domain, "run"));

        if (mode == CompilationMode.Persistence)
        {
            ScriptAssert.Equal(
                new object?[]
                {
                    4, 42, 63,
                    -126, 127, -126,
                    true, false, true, true,
                    false, "[7,42,7,7]"
                },
                TestWorkspace.Execute(engine.CreateDomain(), "run"));
        }
    }

    [Theory]
    [InlineData(CompilationMode.Dynamic)]
    [InlineData(CompilationMode.OnlyRun)]
#if NET9_0_OR_GREATER
    [InlineData(CompilationMode.Persistence)]
#endif
    public async Task PackedArraysStayNativeAcrossDirectCalls(CompilationMode mode)
    {
        using var workspace = new TestWorkspace();
        var (_, domain) = await workspace.CompileModuleAsync(
            """
            @module(TEST);

            @directCall
            func intWork(data, count) {
                for (var i = 0; i < count; i++) data[i] = i * 3;
                data[1]++;
                var sum = 0;
                for (var j = 0; j < count; j++) sum += data[j];
                return sum;
            }

            @directCall
            func byteAndBooleanWork(bytes, flags, count) {
                for (var i = 0; i < count; i++) {
                    bytes[i] = i + 128;
                    flags[i] = (i % 2) == 0;
                }
                var sum = 0;
                for (var j = 0; j < count; j++) {
                    if (flags[j]) sum += bytes[j];
                }
                return sum;
            }

            export func run() {
                var ints = new Int32Array(8);
                var bytes = new Int8Array(4);
                var flags = new BooleanArray(4);
                return [intWork(ints, ints.length), byteAndBooleanWork(bytes, flags, 4)];
            }
            """,
            mode);

        ScriptAssert.Equal(new object?[] { 85, -254 }, TestWorkspace.Execute(domain, "run"));
    }

    [Theory]
    [InlineData(CompilationMode.Dynamic)]
    [InlineData(CompilationMode.OnlyRun)]
#if NET9_0_OR_GREATER
    [InlineData(CompilationMode.Persistence)]
#endif
    public async Task DirectCodeCanAllocatePackedArraysWithoutElementInitialization(CompilationMode mode)
    {
        using var workspace = new TestWorkspace();
        var (_, domain) = await workspace.CompileModuleAsync(
            """
            @module(TEST);
            @directCall
            func createAndSum(count) {
                var values = new Int32Array(count);
                var sum = 0;
                for (var i = 0; i < count; i++) {
                    sum += values[i];
                    values[i] = i + 1;
                }
                for (var j = 0; j < count; j++) sum += values[j];
                return sum;
            }
            export func run() { return createAndSum(100); }
            """,
            mode);

        ScriptAssert.Equal(5050, TestWorkspace.Execute(domain, "run"));
    }

    [Theory]
    [InlineData(CompilationMode.Dynamic)]
    [InlineData(CompilationMode.OnlyRun)]
#if NET9_0_OR_GREATER
    [InlineData(CompilationMode.Persistence)]
#endif
    public async Task PackedArrayDynamicIdentityFallsBackWithoutLosingSemantics(CompilationMode mode)
    {
        using var workspace = new TestWorkspace();
        var (_, domain) = await workspace.CompileModuleAsync(
            """
            @module(TEST);
            @directCall
            func identityCheck(values) {
                if (values == values) return 1;
                return 0;
            }
            @directCall
            func truthyCheck(values) {
                if (values) return values.length;
                return -1;
            }
            export func run() {
                var values = new Int32Array(4);
                return [identityCheck(values), truthyCheck(values)];
            }
            """,
            mode);

        ScriptAssert.Equal(new object?[] { 1, 4 }, TestWorkspace.Execute(domain, "run"));
    }

    [Theory]
    [InlineData(CompilationMode.Dynamic)]
    [InlineData(CompilationMode.OnlyRun)]
#if NET9_0_OR_GREATER
    [InlineData(CompilationMode.Persistence)]
#endif
    public async Task PackedArrayDynamicObjectBoundaryRetainsSemantics(CompilationMode mode)
    {
        using var workspace = new TestWorkspace();
        var (_, domain) = await workspace.CompileModuleAsync(
            """
            @module(TEST);
            export func run() {
                var holder = { values: new Int32Array(3) };
                holder.values.fill(5);
                holder.values[1] = 9;
                return [holder.values.length, holder.values[0], holder.values[1]];
            }
            """,
            mode);

        ScriptAssert.Equal(new object?[] { 3, 5, 9 }, TestWorkspace.Execute(domain, "run"));
    }

    [Theory]
    [InlineData(CompilationMode.Dynamic)]
    [InlineData(CompilationMode.OnlyRun)]
#if NET9_0_OR_GREATER
    [InlineData(CompilationMode.Persistence)]
#endif
    public async Task PackedArraysParticipateInCloneAndDeepEquality(CompilationMode mode)
    {
        using var workspace = new TestWorkspace();
        var (_, domain) = await workspace.CompileModuleAsync(
            """
            @module(TEST);
            export func run() {
                var first = new Int32Array(3);
                first[0] = 4;
                first[1] = 5;
                first[2] = 6;

                var second = new Int32Array(3);
                second[0] = 4;
                second[1] = 5;
                second[2] = 6;

                var equalBefore = Object.deepEqual(first, second);
                second[1] = 9;
                var equalAfter = Object.deepEqual(first, second);

                var clone = Object.clone(first);
                clone[0] = 20;
                var deepClone = Object.deepClone(first);
                deepClone[2] = 30;

                return [
                    equalBefore, equalAfter,
                    first[0], clone[0],
                    first[2], deepClone[2],
                    Object.equal$(first, clone)
                ];
            }
            """,
            mode);

        ScriptAssert.Equal(
            new object?[] { true, false, 4, 20, 6, 30, false },
            TestWorkspace.Execute(domain, "run"));
    }

    [Theory]
    [InlineData(CompilationMode.Dynamic)]
    [InlineData(CompilationMode.OnlyRun)]
#if NET9_0_OR_GREATER
    [InlineData(CompilationMode.Persistence)]
#endif
    public async Task ShadowedPackedArrayNameDoesNotTriggerNativeIntrinsic(CompilationMode mode)
    {
        using var workspace = new TestWorkspace();
        var (_, domain) = await workspace.CompileModuleAsync(
            """
            @module(TEST);
            export func run() {
                var Int32Array = Array;
                var values = new Int32Array(2);
                values[0] = 7;
                values[1] = 8;
                return [Array.isArray(values), values.length, values[0] + values[1]];
            }
            """,
            mode);

        ScriptAssert.Equal(
            new object?[] { true, 2, 15 },
            TestWorkspace.Execute(domain, "run"));
    }

    [Fact]
    public async Task PackedArrayRejectsOutOfRangeWritesAndDelete()
    {
        using var workspace = new TestWorkspace();
        var (_, domain) = await workspace.CompileModuleAsync(
            """
            @module(TEST);
            export func writePastEnd() {
                var values = new Int32Array(1);
                values[1] = 2;
            }
            export func deleteElement() {
                var values = new BooleanArray(1);
                delete values[0];
            }
            """);

        Assert.ThrowsAny<System.Exception>(() => TestWorkspace.Execute(domain, "writePastEnd"));
        Assert.ThrowsAny<System.Exception>(() => TestWorkspace.Execute(domain, "deleteElement"));
    }

#if NET9_0_OR_GREATER
    [Fact]
    public async Task PersistenceUsesRawArrayNativeAbiAndPrimitiveElementInstructions()
    {
        using var workspace = new TestWorkspace();
        await workspace.CompileModuleAsync(
            """
            @module(TEST);

            @directCall
            func packedWork(ints, bytes, flags, count) {
                var sum = 0;
                for (var i = 0; i < count; i++) {
                    ints[i] = i;
                    bytes[i] = i;
                    flags[i] = (i % 2) == 0;
                    sum += ints[i] + bytes[i];
                    if (flags[i]) sum += 1;
                }
                return sum;
            }

            export func run() {
                var ints = new Int32Array(8);
                var bytes = new Int8Array(8);
                var flags = new BooleanArray(8);
                return packedWork(ints, bytes, flags, 8);
            }
            """,
            CompilationMode.Persistence);

        var assemblyPath = Path.Combine(workspace.Root, "test-output.dll");
        using var stream = File.OpenRead(assemblyPath);
        using var peReader = new PEReader(stream);
        var reader = peReader.GetMetadataReader();

        var methodHandle = Assert.Single(reader.MethodDefinitions.Where(handle =>
            string.Equals(
                reader.GetString(reader.GetMethodDefinition(handle).Name),
                "packedWork$native",
                StringComparison.Ordinal)));
        var method = reader.GetMethodDefinition(methodHandle);
        // DEFAULT, four parameters, return R8, then int[], sbyte[], bool[], int.
        Assert.Equal(
            [0x00, 0x04, 0x0d, 0x1d, 0x08, 0x1d, 0x04, 0x1d, 0x02, 0x08],
            reader.GetBlobBytes(method.Signature));
        var il = peReader.GetMethodBody(method.RelativeVirtualAddress).GetILBytes();
        var opcodes = ReadOpCodes(il.AsSpan());

        Assert.DoesNotContain(OpCodes.Ldfld, opcodes);
        Assert.Contains(OpCodes.Ldelem_I4, opcodes);
        Assert.Contains(OpCodes.Stelem_I4, opcodes);
        Assert.Contains(OpCodes.Ldelem_I1, opcodes);
        Assert.Contains(OpCodes.Ldelem_U1, opcodes);
        Assert.Contains(OpCodes.Stelem_I1, opcodes);
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
            OperandType.ShortInlineBrTarget or
            OperandType.ShortInlineI or
            OperandType.ShortInlineVar => 1,
            OperandType.InlineVar => 2,
            OperandType.InlineBrTarget or
            OperandType.InlineField or
            OperandType.InlineI or
            OperandType.InlineMethod or
            OperandType.InlineSig or
            OperandType.InlineString or
            OperandType.InlineTok or
            OperandType.InlineType or
            OperandType.ShortInlineR => 4,
            OperandType.InlineI8 or OperandType.InlineR => 8,
            OperandType.InlineSwitch => 4 +
                (BinaryPrimitives.ReadInt32LittleEndian(remaining) * 4),
            _ => throw new InvalidOperationException("Unsupported CIL operand type: " + operandType)
        };
    }
#endif
}
