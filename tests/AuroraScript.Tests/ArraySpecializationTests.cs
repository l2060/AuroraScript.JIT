using AuroraScript.Runtime;
using AuroraScript.Tests.Infrastructure;
using System;
using System.IO;
using System.Linq;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using System.Reflection.PortableExecutable;
using System.Threading.Tasks;
using Xunit;

namespace AuroraScript.Tests;

public sealed class ArraySpecializationTests
{
    [Theory]
    [InlineData(CompilationMode.Dynamic)]
    [InlineData(CompilationMode.OnlyRun)]
#if NET9_0_OR_GREATER
    [InlineData(CompilationMode.Persistence)]
#endif
    public async Task ExactArraysPreserveConstructionIndexingAndDirectCallSemantics(CompilationMode mode)
    {
        using var workspace = new TestWorkspace();
        var (engine, domain) = await workspace.CompileModuleAsync(
            """
            @module(TEST);

            native func arrayWork(Array values, Number count) Number {
                for (var i = 0; i < count; i++) values[i] += i;
                values[-1]++;
                return count + values.length;
            }

            export func run() {
                var values = Array.withCapacity(8);
                values.push(1, 2, 3);
                var alias = values;
                var directResult = arrayWork(values, 3);
                var previousLast = values[-1];
                values[-1] = 9;
                var pushResult = values.push(10);
                return [
                    directResult, previousLast, values[-2], values[-1],
                    pushResult, values.length, alias == values
                ];
            }
            """,
            mode);

        ScriptAssert.Equal(
            new object?[] { 6, 6, 9, 10, 4, 4, true },
            TestWorkspace.Execute(domain, "run"));

        if (mode == CompilationMode.Persistence)
        {
            ScriptAssert.Equal(
                new object?[] { 6, 6, 9, 10, 4, 4, true },
                TestWorkspace.Execute(engine.CreateDomain(), "run"));
#if NET9_0_OR_GREATER
            AssertNativeArraySignature(Path.Combine(workspace.Root, "test-output.dll"));
#endif
        }
    }

    [Theory]
    [InlineData(CompilationMode.Dynamic)]
    [InlineData(CompilationMode.OnlyRun)]
#if NET9_0_OR_GREATER
    [InlineData(CompilationMode.Persistence)]
#endif
    public async Task ExactArrayFastPathsKeepEvaluationOrderAndBuiltinOverrides(CompilationMode mode)
    {
        using var workspace = new TestWorkspace();
        var (_, domain) = await workspace.CompileModuleAsync(
            """
            @module(TEST);

            export func run() {
                var trace = [];
                func mark(value) {
                    trace.push(value);
                    return value;
                }

                var empty = new Array(mark(1), mark(2));
                var sized = new Array(3);
                var reserved = Array.withCapacity(mark(4), mark(5));
                reserved[mark(0)] = mark(7);

                var overridden = [];
                overridden.push = (value) => value * 10;
                var overrideResult = overridden.push(6);

                return [
                    trace[0], trace[1], trace[2], trace[3], trace[4], trace[5],
                    empty.length, sized.length, sized[0],
                    reserved.length, reserved[0], overrideResult, overridden.length
                ];
            }
            """,
            mode);

        ScriptAssert.Equal(
            new object?[] { 1, 2, 4, 5, 0, 7, 0, 3, null, 1, 7, 60, 0 },
            TestWorkspace.Execute(domain, "run"));
    }

    [Theory]
    [InlineData(CompilationMode.Dynamic)]
    [InlineData(CompilationMode.OnlyRun)]
#if NET9_0_OR_GREATER
    [InlineData(CompilationMode.Persistence)]
#endif
    public async Task ArrayAndObjectFlowMergeFallsBackToDynamicSemantics(CompilationMode mode)
    {
        using var workspace = new TestWorkspace();
        var (_, domain) = await workspace.CompileModuleAsync(
            """
            @module(TEST);
            export func run(flag) {
                var value = [];
                if (flag) value = {};
                value[0] = 7;
                return [value[0], Array.isArray(value)];
            }
            """,
            mode);

        ScriptAssert.Equal(
            new object?[] { 7, false },
            TestWorkspace.Execute(domain, "run", "TEST", ScriptDatum.FromBoolean(true)));
        ScriptAssert.Equal(
            new object?[] { 7, true },
            TestWorkspace.Execute(domain, "run", "TEST", ScriptDatum.FromBoolean(false)));
    }

    [Theory]
    [InlineData(CompilationMode.Dynamic)]
    [InlineData(CompilationMode.OnlyRun)]
#if NET9_0_OR_GREATER
    [InlineData(CompilationMode.Persistence)]
#endif
    public async Task LocalArrayElementFactsPreserveHolesStringsMutationsAndEscapes(
        CompilationMode mode)
    {
        using var workspace = new TestWorkspace();
        var (_, domain) = await workspace.CompileModuleAsync(
            """
            @module(TEST);

            func replace(values) {
                values[0] = "4";
            }

            export func run() {
                var numeric = [41];
                var holes = [];
                var mixed = ["4"];
                var mutated = [1];
                mutated[0] = "4";
                var escaped = [1];
                replace(escaped);
                var pushed = Array.withCapacity(1);
                pushed.push(41);
                var pushedString = [];
                pushedString.push("4");
                return [
                    numeric[0] + 1,
                    holes[3] + 1,
                    mixed[0] + 1,
                    mutated[0] + 1,
                    escaped[0] + 1,
                    pushed[0] + 1,
                    pushedString[0] + 1
                ];
            }
            """,
            mode);

        ScriptAssert.Equal(
            new object?[] { 42, 1, "41", "41", "41", 42, "41" },
            TestWorkspace.Execute(domain, "run"));
    }

#if NET9_0_OR_GREATER
    private static void AssertNativeArraySignature(string assemblyPath)
    {
        using var stream = File.OpenRead(assemblyPath);
        using var peReader = new PEReader(stream);
        var reader = peReader.GetMetadataReader();
        var handle = Assert.Single(reader.MethodDefinitions.Where(candidate =>
            string.Equals(
                reader.GetString(reader.GetMethodDefinition(candidate).Name),
                "arrayWork$native",
                StringComparison.Ordinal)));
        var signature = reader.GetBlobReader(reader.GetMethodDefinition(handle).Signature);

        Assert.Equal(0, signature.ReadByte());
        Assert.Equal(3, signature.ReadCompressedInteger());
        Assert.Equal(0x0d, signature.ReadByte());
        Assert.Equal(0x12, signature.ReadByte());
        signature.ReadCompressedInteger(); // ScriptContext

        Assert.Equal(0x12, signature.ReadByte());
        var encodedType = signature.ReadCompressedInteger();
        Assert.Equal(1, encodedType & 0x03);
        var type = reader.GetTypeReference(MetadataTokens.TypeReferenceHandle(encodedType >> 2));
        Assert.Equal("AuroraScript.Runtime.Types", reader.GetString(type.Namespace));
        Assert.Equal(nameof(AuroraScript.Runtime.Types.ScriptArray), reader.GetString(type.Name));
        Assert.Equal(0x0d, signature.ReadByte());
        Assert.Equal(0, signature.RemainingBytes);
    }
#endif
}
