using AuroraScript.Runtime;
using AuroraScript.Tests.Infrastructure;
using System;
using System.Buffers.Binary;
using System.IO;
using System.Reflection;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using System.Reflection.PortableExecutable;
using System.Threading.Tasks;
using Xunit;

namespace AuroraScript.Tests;

public sealed class HostExportGeneratorTests
{
    [Fact]
    public void StatsSupportExportsAreGeneratedAtBuildTime()
    {
        var mean = typeof(AuroraScript.Runtime.Extensions.StatsSupport).GetMethod(
            "MEAN",
            BindingFlags.Public | BindingFlags.Static);
        var sumExact = typeof(AuroraScript.Runtime.Extensions.StatsSupport).GetMethod(
            "SUMEXACT",
            BindingFlags.Public | BindingFlags.Static);

        Assert.NotNull(mean);
        Assert.NotNull(sumExact);
    }

    [Fact]
    public async Task StatsMeanUsesWeakCoercionAndReturnsNaNOnFailure()
    {
        using var workspace = new TestWorkspace();
        var (_, domain) = await workspace.CompileModuleAsync(
            """
            @module(TEST);
            export func run() {
                var parsed = Stats.mean("3", 5);
                var invalid = Stats.mean("bad", 1);
                return [parsed, invalid != invalid];
            }
            """);

        ScriptAssert.Equal(new object?[] { 4D, true }, TestWorkspace.Execute(domain, "run"));
    }

    [Fact]
    public async Task StatsSumExactUsesExactNumbers()
    {
        using var workspace = new TestWorkspace();
        var (_, domain) = await workspace.CompileModuleAsync(
            """
            @module(TEST);
            export func run() {
                return Stats.sumExact(2, 5);
            }
            """);

        ScriptAssert.Equal(7D, TestWorkspace.Execute(domain, "run"));
    }

    [Fact]
    public async Task StatsSumExactThrowsWhenArgumentIsNotExactNumber()
    {
        using var workspace = new TestWorkspace();
        var (_, domain) = await workspace.CompileModuleAsync(
            """
            @module(TEST);
            export func run() {
                return Stats.sumExact("2", 5);
            }
            """);

        Assert.Throws<AuroraRuntimeException>(() => TestWorkspace.Execute(domain, "run"));
    }

#if NET9_0_OR_GREATER
    [Fact]
    public async Task ProvenArgumentsCallGeneratedCoreDirectly()
    {
        using var workspace = new TestWorkspace();
        var assemblyPath = Path.Combine(workspace.Root, "host-export.dll");
        workspace.WriteSource(
            "main.as",
            """
            @module(TEST);
            export func run() Number {
                return Stats.mean(3, 5);
            }
            """);

        var engine = workspace.CreateEngine(
            CompilationMode.Persistence,
            assemblyOut: assemblyPath);
        await engine.BuildAsync(["main.as"]);
        using var domain = engine.CreateDomain();
        ScriptAssert.Equal(4D, TestWorkspace.Execute(domain, "run"));

        using var stream = File.OpenRead(assemblyPath);
        using var peReader = new PEReader(stream);
        var reader = peReader.GetMetadataReader();
        var coreToken = 0;
        foreach (var handle in reader.MemberReferences)
        {
            var member = reader.GetMemberReference(handle);
            if (reader.GetString(member.Name) == nameof(
                    AuroraScript.Runtime.Extensions.StatsSupport.MeanCore))
            {
                coreToken = MetadataTokens.GetToken(handle);
                break;
            }
        }

        Assert.NotEqual(0, coreToken);
        MethodDefinitionHandle caller = default;
        foreach (var handle in reader.MethodDefinitions)
        {
            var method = reader.GetMethodDefinition(handle);
            if (reader.GetString(method.Name) == "run$typed")
            {
                caller = handle;
                break;
            }
        }

        Assert.False(caller.IsNil);
        var callerMethod = reader.GetMethodDefinition(caller);
        var il = peReader.GetMethodBody(
            callerMethod.RelativeVirtualAddress).GetILBytes();
        Assert.True(ContainsCall(il, coreToken));
    }
#endif

    [Fact]
    public async Task ProvenObjectArgumentCallsSameCoreSemantics()
    {
        using var workspace = new TestWorkspace();
        var (_, domain) = await workspace.CompileModuleAsync(
            """
            @module(TEST);
            export func run() {
                var value = { answer: 42 };
                return Stats.identity(value).answer;
            }
            """);

        ScriptAssert.Equal(42D, TestWorkspace.Execute(domain, "run"));
    }

    [Fact]
    public async Task ShadowedBuiltinGlobalKeepsOrdinaryPropertyCall()
    {
        using var workspace = new TestWorkspace();
        var (_, domain) = await workspace.CompileModuleAsync(
            """
            @module(TEST);
            export func run() {
                var Stats = { mean: (a, b) => 99 };
                return Stats.mean(3, 5);
            }
            """);

        ScriptAssert.Equal(99D, TestWorkspace.Execute(domain, "run"));
    }

    [Fact]
    public async Task GeneratedExportSupportsStringAndInt32CoreParameters()
    {
        using var workspace = new TestWorkspace();
        var (_, domain) = await workspace.CompileModuleAsync(
            """
            @module(TEST);
            export func run() {
                return Stats.chat("piece-", 7);
            }
            """);

        ScriptAssert.Equal("piece-7", TestWorkspace.Execute(domain, "run"));
    }

    private static bool ContainsCall(
        ReadOnlySpan<byte> il,
        int metadataToken)
    {
        for (var i = 0; i + 5 <= il.Length; i++)
        {
            if (il[i] == 0x28 &&
                BinaryPrimitives.ReadInt32LittleEndian(
                    il.Slice(i + 1, 4)) == metadataToken)
            {
                return true;
            }
        }
        return false;
    }
}
