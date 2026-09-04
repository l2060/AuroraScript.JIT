using AuroraScript.Tests.Infrastructure;
using AuroraScript.Runtime;
using AuroraScript.Runtime.Types;
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

public sealed class AstarExampleTests
{
    private static readonly Dictionary<ushort, OpCode> CilOpCodes = typeof(OpCodes)
        .GetFields(BindingFlags.Public | BindingFlags.Static)
        .Where(field => field.FieldType == typeof(OpCode))
        .Select(field => (OpCode)field.GetValue(null)!)
        .ToDictionary(opcode => unchecked((ushort)opcode.Value));

    [Theory]
    [InlineData(CompilationMode.Dynamic)]
    [InlineData(CompilationMode.OnlyRun)]
#if NET9_0_OR_GREATER
    [InlineData(CompilationMode.Persistence)]
#endif
    public async Task OptimizedAstarPreservesSearchBehavior(CompilationMode mode)
    {
        using var workspace = new TestWorkspace();
        var source = File.ReadAllText(FindRepositoryFile("examples", "tests", "astar.as"));
        source = source.Replace("import fs from 'fs';", "", StringComparison.Ordinal);
        source = source[..source.IndexOf("// examples", StringComparison.Ordinal)] +
            """

            export func verifyOptimizedAstar() {
                var openMap = new Int8Array(25);
                openMap.fill(1);
                var finder = createAStar(
                    { width: 5, height: 5, data: openMap },
                    null);
                var path = newPathBuffer(finder);
                var diagonalCount = findPathInto(finder, 0, 0, 4, 4, path, true, true);
                var diagonalOk = diagonalCount == 5 && path[0] == 0 && path[4] == 24;

                var cornerMap = new Int8Array(4);
                cornerMap.fill(1);
                cornerMap[1] = 0;
                cornerMap[2] = 0;
                var cornerFinder = createAStar(
                    { width: 2, height: 2, data: cornerMap },
                    null);
                var cornerPath = newPathBuffer(cornerFinder);
                var blockedCorner = findPathInto(cornerFinder, 0, 0, 1, 1, cornerPath, true, true);
                var allowedCorner = findPathInto(cornerFinder, 0, 0, 1, 1, cornerPath, true, false);

                var weightedMap = new Int8Array(6);
                weightedMap.fill(1);
                var weights = new Float64Array(6);
                weights.fill(1);
                weights[1] = 100;
                var weightedFinder = createAStar(
                    { width: 3, height: 2, data: weightedMap },
                    weights);
                var weightedPath = newPathBuffer(weightedFinder);
                var weightedCount = findPathInto(weightedFinder, 0, 0, 2, 0, weightedPath, false, true);
                var avoidsExpensiveCell = weightedCount == 5 && weightedPath[1] == 3 &&
                    weightedPath[2] == 4 && weightedPath[3] == 5;

                return [
                    diagonalOk,
                    blockedCorner,
                    allowedCorner,
                    avoidsExpensiveCell,
                    weightedFinder.expanded > 0
                ];
            }
            """;

        var (_, domain) = await workspace.CompileModuleAsync(source, mode);
        using (domain)
        {
            ScriptAssert.Equal(
                new object?[] { true, 0, 2, true, true },
                TestWorkspace.Execute(domain, "verifyOptimizedAstar", "ASTAR"));
        }
    }

#if NET9_0_OR_GREATER
    [Fact]
    public async Task PersistenceEmitsNativeAstarHotCalls()
    {
        using var workspace = new TestWorkspace();
        var source = File.ReadAllText(FindRepositoryFile("examples", "tests", "astar.as"));
        source = source.Replace("import fs from 'fs';", "", StringComparison.Ordinal);
        source = source[..source.IndexOf("// examples", StringComparison.Ordinal)];
        var assemblyPath = Path.Combine(workspace.Root, "test-output.dll");
        var engine = workspace.CreateEngine(
            CompilationMode.Persistence,
            assemblyOut: assemblyPath,
            enableModuleConstInlining: true);
        workspace.WriteSource("main.as", source);
        await engine.BuildAsync(["main.as"]);

        using var stream = File.OpenRead(assemblyPath);
        using var peReader = new PEReader(stream);
        var reader = peReader.GetMetadataReader();
        MethodDefinition? heuristic = null;
        MethodDefinitionHandle heuristicHandle = default;
        MethodDefinitionHandle heuristicDirectHandle = default;
        MethodDefinitionHandle heapNativeHandle = default;
        MethodDefinitionHandle findPathHandle = default;
        foreach (var handle in reader.MethodDefinitions)
        {
            var method = reader.GetMethodDefinition(handle);
            var name = reader.GetString(method.Name);
            if (string.Equals(name, "astarHeuristic$native", StringComparison.Ordinal))
            {
                heuristic = method;
                heuristicHandle = handle;
            }
            else if (string.Equals(name, "astarHeuristic$direct6", StringComparison.Ordinal))
            {
                heuristicDirectHandle = handle;
            }
            else if (string.Equals(name, "astarHeapPush$native", StringComparison.Ordinal))
            {
                heapNativeHandle = handle;
            }
            else if (string.Equals(name, "findPathInto$native", StringComparison.Ordinal))
            {
                findPathHandle = handle;
            }
        }

        Assert.True(heuristic.HasValue, "Persisted Astar heuristic native method was not emitted.");
        // DEFAULT, seven parameters, return R8, then ScriptContext,
        // I4 x4, Boolean, R8.
        var heuristicSignature = reader.GetBlobBytes(heuristic.Value.Signature);
        Assert.Equal(0x00, heuristicSignature[0]);
        Assert.Equal(0x07, heuristicSignature[1]);
        Assert.Equal(0x0d, heuristicSignature[2]);
        Assert.Equal(0x12, heuristicSignature[3]);
        Assert.Equal(
            [0x08, 0x08, 0x08, 0x08, 0x02, 0x0d],
            heuristicSignature[5..]);
        Assert.False(heuristicHandle.IsNil);
        Assert.False(heapNativeHandle.IsNil, "Persisted Astar heap push native method was not emitted.");
        var heapNative = reader.GetMethodDefinition(heapNativeHandle);
        var heapOpcodes = ReadOpCodes(
            peReader.GetMethodBody(heapNative.RelativeVirtualAddress).GetILBytes().AsSpan());
        Assert.Contains(OpCodes.Ldelem_I4, heapOpcodes);
        Assert.Contains(OpCodes.Ldelem_R8, heapOpcodes);
        Assert.Contains(OpCodes.Stelem_I4, heapOpcodes);
        Assert.Contains(OpCodes.Stelem_R8, heapOpcodes);
        Assert.False(findPathHandle.IsNil, "Persisted Astar path finder method was not emitted.");

        var findPath = reader.GetMethodDefinition(findPathHandle);
        var il = peReader.GetMethodBody(findPath.RelativeVirtualAddress).GetILBytes().AsSpan();
        var astarToBooleanCalls = CountCalls(
            il,
            GetMemberReferenceTokens(reader, "ValueOps", "ToBoolean"));
        var astarFromBooleanCalls = CountCalls(
            il,
            GetMemberReferenceTokens(reader, "ScriptDatum", "FromBoolean"));
        Assert.InRange(astarToBooleanCalls, 0, 12);
        Assert.InRange(astarFromBooleanCalls, 0, 2);
        Assert.Equal(
            0,
            CountCalls(
                il,
                GetMemberReferenceTokens(reader, "ValueOps", "NotEqualBoolean")));
        // The explicit Number checks on the A* boundary keep neighbour costs
        // and heap scores native throughout the hot loop.
        Assert.Equal(
            0,
            CountCalls(
                il,
                GetMemberReferenceTokens(reader, "ValueOps", "Add")));
        Assert.True(
            ContainsCall(il, MetadataTokens.GetToken(heapNativeHandle)),
            "findPathInto should call astarHeapPush$native.");
        Assert.True(
            ContainsCall(il, MetadataTokens.GetToken(heuristicHandle)),
            "findPathInto should call astarHeuristic$native.");
        if (!heuristicDirectHandle.IsNil)
        {
            Assert.False(
                ContainsCall(il, MetadataTokens.GetToken(heuristicDirectHandle)),
                "Generic callers should not route coercion-only heuristic calls through an adapter.");
        }
    }

    [Fact]
    public async Task PersistenceComparesNumericOperandsWithoutDynamicRelationalCalls()
    {
        using var workspace = new TestWorkspace();
        var (_, domain) = await workspace.CompileModuleAsync(
            """
            @module(TEST);
            export func compare(value) {
                return [
                    value < 0,
                    value <= 0,
                    value > 0,
                    value >= 0,
                    0 < value
                ];
            }
            """,
            CompilationMode.Persistence);

        ScriptAssert.Equal(
            new object?[] { false, false, true, true, true },
            TestWorkspace.Execute(
                domain,
                "compare",
                arguments: [ScriptDatum.FromNumber(3)]));
        ScriptAssert.Equal(
            new object?[] { true, true, false, false, false },
            TestWorkspace.Execute(
                domain,
                "compare",
                arguments: [ScriptDatum.FromString("-2")]));
        // Values that cannot be coerced compare false in every direction.
        ScriptAssert.Equal(
            new object?[] { false, false, false, false, false },
            TestWorkspace.Execute(
                domain,
                "compare",
                arguments: [ScriptDatum.FromString("aurora")]));

        var assemblyPath = Path.Combine(workspace.Root, "test-output.dll");
        using var stream = File.OpenRead(assemblyPath);
        using var peReader = new PEReader(stream);
        var reader = peReader.GetMetadataReader();
        var handle = Assert.Single(reader.MethodDefinitions.Where(candidate =>
            string.Equals(
                reader.GetString(reader.GetMethodDefinition(candidate).Name),
                "compare$typed",
                StringComparison.Ordinal)));
        var il = peReader
            .GetMethodBody(reader.GetMethodDefinition(handle).RelativeVirtualAddress)
            .GetILBytes()
            .AsSpan();

        foreach (var name in new[]
        {
            "LessBoolean", "LessEqualBoolean", "GreaterBoolean", "GreaterEqualBoolean"
        })
        {
            Assert.Equal(
                0,
                CountCalls(il, GetMemberReferenceTokens(reader, "ValueOps", name)));
        }
    }

    [Fact]
    public async Task PersistenceComparesNativeReferencesWithNullWithoutDatumBoxing()
    {
        using var workspace = new TestWorkspace();
        var (_, domain) = await workspace.CompileModuleAsync(
            """
            @module(TEST);
            export func missing(Int8Array values) {
                return values == null;
            }
            """,
            CompilationMode.Persistence);

        ScriptAssert.Equal(
            false,
            TestWorkspace.Execute(
                domain,
                "missing",
                arguments: [ScriptDatum.FromObject(new ScriptInt8Array(1))]));

        var assemblyPath = Path.Combine(workspace.Root, "test-output.dll");
        using var stream = File.OpenRead(assemblyPath);
        using var peReader = new PEReader(stream);
        var reader = peReader.GetMetadataReader();
        var handle = Assert.Single(reader.MethodDefinitions.Where(candidate =>
            string.Equals(
                reader.GetString(reader.GetMethodDefinition(candidate).Name),
                "missing$typed",
                StringComparison.Ordinal)));
        var method = reader.GetMethodDefinition(handle);
        var il = peReader.GetMethodBody(method.RelativeVirtualAddress).GetILBytes().AsSpan();

        Assert.Contains(OpCodes.Ceq, ReadOpCodes(il));
        Assert.Equal(
            0,
            CountCalls(
                il,
                GetMemberReferenceTokens(reader, "ValueOps", "EqualBoolean")));
        Assert.Equal(
            0,
            CountCalls(
                il,
                GetMemberReferenceTokens(reader, "ScriptDatum", "FromObject")));
    }

    [Fact]
    public async Task PersistenceEmitsLogicalConditionsWithoutBooleanDatumRoundTrips()
    {
        using var workspace = new TestWorkspace();
        var (_, domain) = await workspace.CompileModuleAsync(
            """
            @module(TEST);
            export func check(first, second, value) {
                if ((value != 1) && first && second && (value != 0)) return 1;
                return 0;
            }
            export func preserveAnd(first, second) {
                return first && second;
            }
            export func preserveOr(first, second) {
                return first || second;
            }
            """,
            CompilationMode.Persistence);

        ScriptAssert.Equal(
            1,
            TestWorkspace.Execute(
                domain,
                "check",
                arguments:
                [
                    ScriptDatum.FromBoolean(true),
                    ScriptDatum.FromNumber(1),
                    ScriptDatum.FromNumber(2)
                ]));
        ScriptAssert.Equal(
            0,
            TestWorkspace.Execute(
                domain,
                "preserveAnd",
                arguments:
                [
                    ScriptDatum.FromNumber(0),
                    ScriptDatum.FromString("right")
                ]));
        ScriptAssert.Equal(
            "left",
            TestWorkspace.Execute(
                domain,
                "preserveOr",
                arguments:
                [
                    ScriptDatum.FromString("left"),
                    ScriptDatum.FromString("right")
                ]));

        var assemblyPath = Path.Combine(workspace.Root, "test-output.dll");
        using var stream = File.OpenRead(assemblyPath);
        using var peReader = new PEReader(stream);
        var reader = peReader.GetMetadataReader();
        var checkHandle = Assert.Single(reader.MethodDefinitions.Where(candidate =>
            string.Equals(
                reader.GetString(reader.GetMethodDefinition(candidate).Name),
                "check$typed",
                StringComparison.Ordinal)));
        var check = reader.GetMethodDefinition(checkHandle);
        var il = peReader.GetMethodBody(check.RelativeVirtualAddress).GetILBytes().AsSpan();
        var toBooleanTokens = GetMemberReferenceTokens(reader, "ValueOps", "ToBoolean");
        var fromBooleanTokens = GetMemberReferenceTokens(reader, "ScriptDatum", "FromBoolean");
        var toBooleanCalls = 0;
        for (var i = 0; i < toBooleanTokens.Length; i++)
        {
            toBooleanCalls += CountCalls(il, toBooleanTokens[i]);
        }
        var fromBooleanCalls = 0;
        for (var i = 0; i < fromBooleanTokens.Length; i++)
        {
            fromBooleanCalls += CountCalls(il, fromBooleanTokens[i]);
        }

        Assert.Equal(2, toBooleanCalls);
        Assert.Equal(0, fromBooleanCalls);
    }

    [Fact]
    public async Task PersistenceCachesRepeatedNumericLocalComparisonsAndRefreshesWrites()
    {
        using var workspace = new TestWorkspace();
        var (_, domain) = await workspace.CompileModuleAsync(
            """
            @module(TEST);
            export func compare(value) {
                var key = value;
                var values = new Int32Array(1);
                values[0] = 1;
                var before = values[0] != key;
                if (before) key = 1;
                return [before, values[0] != key, key];
            }
            """,
            CompilationMode.Persistence);

        ScriptAssert.Equal(
            new object?[] { false, false, "1" },
            TestWorkspace.Execute(
                domain,
                "compare",
                arguments: [ScriptDatum.FromString("1")]));
        ScriptAssert.Equal(
            new object?[] { true, false, 1 },
            TestWorkspace.Execute(
                domain,
                "compare",
                arguments: [ScriptDatum.FromString("not-number")]));

        var assemblyPath = Path.Combine(workspace.Root, "test-output.dll");
        using var stream = File.OpenRead(assemblyPath);
        using var peReader = new PEReader(stream);
        var reader = peReader.GetMetadataReader();
        var handle = Assert.Single(reader.MethodDefinitions.Where(candidate =>
            string.Equals(
                reader.GetString(reader.GetMethodDefinition(candidate).Name),
                "compare$typed",
                StringComparison.Ordinal)));
        var method = reader.GetMethodDefinition(handle);
        var il = peReader.GetMethodBody(method.RelativeVirtualAddress).GetILBytes().AsSpan();

        Assert.Equal(
            0,
            CountCalls(
                il,
                GetMemberReferenceTokens(reader, "ValueOps", "NotEqualBoolean")));
    }
#endif

    private static bool ContainsCall(ReadOnlySpan<byte> il, int metadataToken)
    {
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
                BinaryPrimitives.ReadInt32LittleEndian(il.Slice(offset, 4)) == metadataToken)
            {
                return true;
            }
            offset += GetOperandSize(opcode.OperandType, il.Slice(offset));
        }
        return false;
    }

    private static int CountCalls(ReadOnlySpan<byte> il, int metadataToken)
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
                BinaryPrimitives.ReadInt32LittleEndian(il.Slice(offset, 4)) == metadataToken)
            {
                count++;
            }
            offset += GetOperandSize(opcode.OperandType, il.Slice(offset));
        }
        return count;
    }

    private static int CountCalls(ReadOnlySpan<byte> il, int[] metadataTokens)
    {
        var count = 0;
        for (var i = 0; i < metadataTokens.Length; i++)
        {
            count += CountCalls(il, metadataTokens[i]);
        }
        return count;
    }

    private static int[] GetMemberReferenceTokens(
        MetadataReader reader,
        string typeName,
        string methodName)
    {
        return reader.MemberReferences
            .Where(handle =>
            {
                var member = reader.GetMemberReference(handle);
                if (!string.Equals(reader.GetString(member.Name), methodName, StringComparison.Ordinal) ||
                    member.Parent.Kind != HandleKind.TypeReference)
                {
                    return false;
                }
                var type = reader.GetTypeReference((TypeReferenceHandle)member.Parent);
                return string.Equals(reader.GetString(type.Name), typeName, StringComparison.Ordinal);
            })
            .Select(handle => MetadataTokens.GetToken(handle))
            .ToArray();
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
                value = (ushort)(0xfe00 | il[offset++]);
            }
            var opcode = CilOpCodes[value];
            result.Add(opcode);
            offset += GetOperandSize(opcode.OperandType, il.Slice(offset));
        }
        return result;
    }

    private static int GetOperandSize(
        OperandType operandType,
        ReadOnlySpan<byte> remaining)
    {
        return operandType switch
        {
            OperandType.InlineNone => 0,
            OperandType.ShortInlineBrTarget or OperandType.ShortInlineI or
                OperandType.ShortInlineVar => 1,
            OperandType.InlineVar => 2,
            OperandType.InlineBrTarget or OperandType.InlineField or
                OperandType.InlineI or OperandType.InlineMethod or
                OperandType.InlineSig or OperandType.InlineString or
                OperandType.InlineTok or OperandType.InlineType or
                OperandType.ShortInlineR => 4,
            OperandType.InlineI8 or OperandType.InlineR => 8,
            OperandType.InlineSwitch =>
                4 + BinaryPrimitives.ReadInt32LittleEndian(remaining) * 4,
            _ => throw new InvalidOperationException(
                "Unsupported CIL operand type: " + operandType)
        };
    }

    private static string FindRepositoryFile(params string[] segments)
    {
        for (var directory = new DirectoryInfo(AppContext.BaseDirectory);
             directory != null;
             directory = directory.Parent)
        {
            var candidate = directory.FullName;
            foreach (var segment in segments)
            {
                candidate = Path.Combine(candidate, segment);
            }
            if (File.Exists(candidate)) return candidate;
        }

        throw new FileNotFoundException(
            "Could not locate repository file: " + Path.Combine(segments));
    }
}
