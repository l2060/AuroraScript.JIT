using AuroraScript.Runtime;
using AuroraScript.Runtime.Interop;
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
        var float64 = new ScriptFloat64Array(1_000);
        var boolean = new ScriptBooleanArray(1_000);

        Assert.IsType<int[]>(int32._items);
        Assert.IsType<sbyte[]>(int8._items);
        Assert.IsType<double[]>(float64._items);
        Assert.IsType<bool[]>(boolean._items);
        Assert.Equal(1_000, int32.Length);
        Assert.Equal(1_000, int8.Length);
        Assert.Equal(1_000, float64.Length);
        Assert.Equal(1_000, boolean.Length);

        Assert.True(ClrMarshaller.TryConvertArgument(float64, typeof(double[]), out var storage));
        Assert.Same(float64._items, storage);
    }

    [Fact]
    public void NativeObjectsKeepObjectKindAndReportConstructorTypeNames()
    {
        AssertNativeType(new ScriptInt8Array(1), "Int8Array");
        AssertNativeType(new ScriptUInt8Array(1), "UInt8Array");
        AssertNativeType(new ScriptInt32Array(1), "Int32Array");
        AssertNativeType(new StringBuffer(""), "StringBuffer");
        AssertNativeType(new ScriptHashMap(), "HashMap");
        AssertNativeType(new ScriptPathValue("mem://app"), "Path");
    }

    private static void AssertNativeType(ScriptObject value, string typeName)
    {
        var datum = ScriptDatum.FromObject(value);
        Assert.Equal(ValueKind.Object, datum.Kind);
        Assert.Equal(typeName, ScriptDatum.GetTypeName(datum));
        Assert.Equal(typeName, ScriptDatum.TypeOf(datum).StringText);
    }

    [Theory]
    [InlineData(CompilationMode.Dynamic)]
    [InlineData(CompilationMode.OnlyRun)]
#if NET9_0_OR_GREATER
    [InlineData(CompilationMode.Persistence)]
#endif
    public async Task Float64ArraysStayNativeAndPreserveNumberSemantics(CompilationMode mode)
    {
        using var workspace = new TestWorkspace();
        var (engine, domain) = await workspace.CompileModuleAsync(
            """
            @module(TEST);

            @directCall
            func floatWork(values, count) {
                for (var i = 0; i < count; i++) values[i] = i + 0.25;
                values[1] *= 2;
                values[2]++;
                var sum = 0;
                for (var j = 0; j < count; j++) sum += values[j];
                return sum;
            }

            export func run() {
                var values = new Float64Array(4);
                var sum = floatWork(values, values.length);
                var clone = Object.clone(values);
                var equalBefore = Object.deepEqual(values, clone);
                clone[0] = 9.5;
                var spread = [...values];
                var filled = new Float64Array(2);
                filled.fill("1.5");
                return [
                    sum, values.length, values[0], values[1], values[2], values[3],
                    spread[2], equalBefore, Object.deepEqual(values, clone),
                    filled[0], filled[1], JSON.stringify(values)
                ];
            }
            """,
            mode);

        var expected = new object?[]
        {
            9.25, 4, 0.25, 2.5, 3.25, 3.25,
            3.25, true, false, 1.5, 1.5, "[0.25,2.5,3.25,3.25]"
        };
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

    [Theory]
    [InlineData(CompilationMode.Dynamic)]
    [InlineData(CompilationMode.OnlyRun)]
#if NET9_0_OR_GREATER
    [InlineData(CompilationMode.Persistence)]
#endif
    public async Task TypedInterpolationAndLocalObjectFieldsPreservePackedSemantics(
        CompilationMode mode)
    {
        using var workspace = new TestWorkspace();
        var (_, domain) = await workspace.CompileModuleAsync(
            """
            @module(TEST);
            export func interpolation(value) {
                var values = tdoc Int32Array $(value);
                values[0] = values[0] + 2;
                return values[0];
            }
            export func localField() {
                var state = { values: new Int32Array(2) };
                state.values[0] = 40;
                state.values[0] += 2;
                return state.values[0];
            }
            """,
            mode);

        var values = new ScriptInt32Array(1);
        values.SetElement(0, 40);
        ScriptAssert.Equal(
            42,
            TestWorkspace.Execute(
                domain,
                "interpolation",
                arguments: ScriptDatum.FromObject(values)));
        ScriptAssert.Equal(42, TestWorkspace.Execute(domain, "localField"));
        Assert.ThrowsAny<Exception>(() =>
            TestWorkspace.Execute(
                domain,
                "interpolation",
                arguments: ScriptDatum.FromString("not an array")));
    }

#if NET9_0_OR_GREATER
    [Fact]
    public async Task PersistenceUsesPackedInstructionsForTDocFieldsAndDirectArguments()
    {
        using var workspace = new TestWorkspace();
        await workspace.CompileModuleAsync(
            """
            @module(TEST);

            func take(value) { return value != null; }

            export func interpolation(value) {
                var values = tdoc Int32Array $(value);
                values[0] = values[0] + 1;
                return values[0];
            }

            @directCall
            func localFieldWork() {
                var state = { values: new Int32Array(2) };
                state.values[0] = 41;
                return state.values[0];
            }

            export func escapedFieldWork() {
                var state = { values: new Int32Array(2) };
                take(state);
                state.values[0] = 41;
                return state.values[0];
            }

            @directCall
            func consume(values) {
                values[0] = values[0] + 1;
                return values[0];
            }

            @directCall
            func forwardField() {
                var state = { values: new Int32Array(1) };
                state.values[0] = 41;
                return consume(state.values);
            }

            export func run() {
                return [localFieldWork(), escapedFieldWork(), forwardField()];
            }
            """,
            CompilationMode.Persistence);

        var assemblyPath = Path.Combine(workspace.Root, "test-output.dll");
        using var stream = File.OpenRead(assemblyPath);
        using var peReader = new PEReader(stream);
        var reader = peReader.GetMetadataReader();

        List<OpCode> GetMethodOpCodes(string methodName)
        {
            var handle = Assert.Single(reader.MethodDefinitions.Where(candidate =>
                string.Equals(
                    reader.GetString(reader.GetMethodDefinition(candidate).Name),
                    methodName,
                    StringComparison.Ordinal)));
            var method = reader.GetMethodDefinition(handle);
            return ReadOpCodes(
                peReader.GetMethodBody(method.RelativeVirtualAddress).GetILBytes().AsSpan());
        }

        var interpolation = GetMethodOpCodes("interpolation$typed");
        Assert.Contains(OpCodes.Ldelem_I4, interpolation);
        Assert.Contains(OpCodes.Stelem_I4, interpolation);

        var localField = GetMethodOpCodes("localFieldWork$typed0");
        Assert.Contains(OpCodes.Ldelem_I4, localField);
        Assert.Contains(OpCodes.Stelem_I4, localField);

        var escapedField = GetMethodOpCodes("escapedFieldWork$typed");
        Assert.DoesNotContain(OpCodes.Ldelem_I4, escapedField);
        Assert.DoesNotContain(OpCodes.Stelem_I4, escapedField);

        var consumeHandle = Assert.Single(reader.MethodDefinitions.Where(candidate =>
            string.Equals(
                reader.GetString(reader.GetMethodDefinition(candidate).Name),
                "consume$native",
                StringComparison.Ordinal)));
        var consume = reader.GetMethodDefinition(consumeHandle);
        // DEFAULT, one parameter, return int32, then int[].
        Assert.Equal([0x00, 0x01, 0x08, 0x1d, 0x08], reader.GetBlobBytes(consume.Signature));
        var consumeOpcodes = ReadOpCodes(
            peReader.GetMethodBody(consume.RelativeVirtualAddress).GetILBytes().AsSpan());
        Assert.Contains(OpCodes.Ldelem_I4, consumeOpcodes);
        Assert.Contains(OpCodes.Stelem_I4, consumeOpcodes);
    }

    [Fact]
    public async Task PersistenceUsesNativeAdditionForProvenLocalArrayElements()
    {
        using var workspace = new TestWorkspace();
        await workspace.CompileModuleAsync(
            """
            @module(TEST);
            export func numericArrayAdd() {
                var values = [41];
                return values[0] + 1;
            }
            export func stringArrayAdd() {
                var values = ["41"];
                return values[0] + 1;
            }
            export func pushedArrayAdd() {
                var values = Array.withCapacity(1);
                values.push(41);
                return values[0] + 1;
            }
            """,
            CompilationMode.Persistence);

        var assemblyPath = Path.Combine(workspace.Root, "test-output.dll");
        using var stream = File.OpenRead(assemblyPath);
        using var peReader = new PEReader(stream);
        var reader = peReader.GetMetadataReader();

        List<OpCode> GetMethodOpCodes(string methodName)
        {
            var handle = Assert.Single(reader.MethodDefinitions.Where(candidate =>
                string.Equals(
                    reader.GetString(reader.GetMethodDefinition(candidate).Name),
                    methodName,
                    StringComparison.Ordinal)));
            var method = reader.GetMethodDefinition(handle);
            return ReadOpCodes(
                peReader.GetMethodBody(method.RelativeVirtualAddress).GetILBytes().AsSpan());
        }

        Assert.Contains(OpCodes.Add, GetMethodOpCodes("numericArrayAdd$typed"));
        Assert.Contains(OpCodes.Add, GetMethodOpCodes("pushedArrayAdd$typed"));
        Assert.DoesNotContain(OpCodes.Add, GetMethodOpCodes("stringArrayAdd$typed"));
    }

    [Fact]
    public async Task PersistenceStoresPackedElementsWithCoercedDynamicNumericIndexes()
    {
        using var workspace = new TestWorkspace();
        await workspace.CompileModuleAsync(
            """
            @module(TEST);
            export func writeNeighbor(state) {
                var width = state.width;
                var opened = new Int32Array(8);
                var last = width - 1;
                opened[last + 1] = 1;
                return opened[last];
            }
            """,
            CompilationMode.Persistence);

        var assemblyPath = Path.Combine(workspace.Root, "test-output.dll");
        using var stream = File.OpenRead(assemblyPath);
        using var peReader = new PEReader(stream);
        var reader = peReader.GetMetadataReader();
        var handle = Assert.Single(reader.MethodDefinitions.Where(candidate =>
            string.Equals(
                reader.GetString(reader.GetMethodDefinition(candidate).Name),
                "writeNeighbor$typed",
                StringComparison.Ordinal)));
        var opcodes = ReadOpCodes(
            peReader.GetMethodBody(reader.GetMethodDefinition(handle).RelativeVirtualAddress)
                .GetILBytes()
                .AsSpan());
        Assert.Contains(OpCodes.Stelem_I4, opcodes);
        Assert.Contains(OpCodes.Ldelem_I4, opcodes);
        Assert.Contains(OpCodes.Add, opcodes);
    }

    [Fact]
    public async Task PersistenceUsesRawFloat64ArrayAbiAndR8ElementInstructions()
    {
        using var workspace = new TestWorkspace();
        await workspace.CompileModuleAsync(
            """
            @module(TEST);
            @directCall
            func floatWork(values, count) {
                var sum = 0;
                for (var i = 0; i < count; i++) {
                    values[i] = i + 0.5;
                    sum += values[i];
                }
                return sum;
            }
            export func run() {
                var values = new Float64Array(8);
                return floatWork(values, values.length);
            }
            """,
            CompilationMode.Persistence);

        var assemblyPath = Path.Combine(workspace.Root, "test-output.dll");
        using var stream = File.OpenRead(assemblyPath);
        using var peReader = new PEReader(stream);
        var reader = peReader.GetMetadataReader();
        var handle = Assert.Single(reader.MethodDefinitions.Where(candidate =>
            string.Equals(
                reader.GetString(reader.GetMethodDefinition(candidate).Name),
                "floatWork$native",
                StringComparison.Ordinal)));
        var method = reader.GetMethodDefinition(handle);

        Assert.Equal(
            [0x00, 0x02, 0x0d, 0x1d, 0x0d, 0x08],
            reader.GetBlobBytes(method.Signature));
        var opcodes = ReadOpCodes(
            peReader.GetMethodBody(method.RelativeVirtualAddress).GetILBytes().AsSpan());
        Assert.Contains(OpCodes.Ldelem_R8, opcodes);
        Assert.Contains(OpCodes.Stelem_R8, opcodes);
        Assert.DoesNotContain(OpCodes.Ldfld, opcodes);
    }

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
