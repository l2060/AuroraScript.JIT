using AuroraScript.Runtime;
using AuroraScript.Tests.Infrastructure;
using System;
using System.IO;
using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;
using System.Threading.Tasks;
using Xunit;

namespace AuroraScript.Tests;

public sealed class BooleanSpecializationTests
{
    private const string Source = """
        @module(TEST);

        native func negate(Boolean value) Boolean {
            return !value;
        }

        native func combine(Boolean first, Boolean second) Boolean {
            return negate(first) && second;
        }

        native func boolGate(Boolean flag, Number left, Number right) Boolean {
            if (flag) return left < right;
            return left > right;
        }

        export func run() {
            return [
                combine(false, true),
                combine(true, true),
                boolGate(true, 1, 2),
                boolGate(false, 1, 2)
            ];
        }
        """;

    [Theory]
    [InlineData(CompilationMode.Dynamic)]
    [InlineData(CompilationMode.OnlyRun)]
#if NET9_0_OR_GREATER
    [InlineData(CompilationMode.Persistence)]
#endif
    public async Task BooleanParametersAndReturnsStayNativeAcrossDirectCalls(CompilationMode mode)
    {
        using var workspace = new TestWorkspace();
        var (engine, domain) = await workspace.CompileModuleAsync(Source, mode);
        var expected = new object?[] { true, false, true, false };

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
    public async Task ReadOnlyParameterShadowsPreserveDynamicCoercionSemantics(CompilationMode mode)
    {
        using var workspace = new TestWorkspace();
        var (_, domain) = await workspace.CompileModuleAsync(
            """
            @module(TEST);

            export func arithmetic(value) {
                return [value - 1, value * 2, value / 2, value % 2, -value, value | 0];
            }

            export func truth(value) {
                var score = 0;
                for (var i = 0; i < 3; i++) {
                    if (value) score++;
                }
                return [score, !value, value && 7, value || 9];
            }

            export func written(value) {
                var first = value - 1;
                value = "x";
                return [first, value - 1, !value];
            }
            """,
            mode);

        ScriptAssert.Equal(
            new object?[] { -1, 0, 0, 0, -0d, 0 },
            TestWorkspace.Execute(domain, "arithmetic", arguments: ScriptDatum.Null));
        ScriptAssert.Equal(
            new object?[] { 3, 8, 2, 0, -4, double.NaN },
            TestWorkspace.Execute(domain, "arithmetic", arguments: ScriptDatum.FromString("4")));
        ScriptAssert.Equal(
            new object?[] { 0, true, "", 9 },
            TestWorkspace.Execute(domain, "truth", arguments: ScriptDatum.FromString("")));
        ScriptAssert.Equal(
            new object?[] { 3, false, 7, "x" },
            TestWorkspace.Execute(domain, "truth", arguments: ScriptDatum.FromString("x")));
        ScriptAssert.Equal(
            new object?[] { 3, double.NaN, false },
            TestWorkspace.Execute(domain, "written", arguments: ScriptDatum.FromNumber(4)));
    }

#if NET9_0_OR_GREATER
    [Fact]
    public async Task PersistenceEmitsBooleanNativeSignatures()
    {
        using var workspace = new TestWorkspace();
        await workspace.CompileModuleAsync(Source, CompilationMode.Persistence);

        using var stream = File.OpenRead(Path.Combine(workspace.Root, "test-output.dll"));
        using var peReader = new PEReader(stream);
        var reader = peReader.GetMetadataReader();

        AssertNativeSignature(reader, "negate$native", 2, [0x02]);
        AssertNativeSignature(reader, "combine$native", 3, [0x02, 0x02]);
        AssertNativeSignature(reader, "boolGate$native", 4, [0x02, 0x0d, 0x0d]);
    }

    private static void AssertNativeSignature(
        MetadataReader reader,
        string name,
        byte parameterCount,
        byte[] nativeParameters)
    {
        var signature = reader.GetBlobBytes(FindMethod(reader, name).Signature);
        Assert.Equal(0x00, signature[0]);
        Assert.Equal(parameterCount, signature[1]);
        Assert.Equal(0x02, signature[2]);
        Assert.Equal(0x12, signature[3]); // ScriptContext class marker.
        Assert.Equal(nativeParameters, signature[^nativeParameters.Length..]);
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
#endif
}
