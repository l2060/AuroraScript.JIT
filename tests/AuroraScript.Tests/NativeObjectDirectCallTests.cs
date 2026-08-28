using AuroraScript.Runtime;
using AuroraScript.Tests.Host;
using AuroraScript.Tests.Infrastructure;
using System;
using System.Buffers.Binary;
using System.IO;
using System.Linq;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using System.Reflection.PortableExecutable;
using System.Threading.Tasks;
using Xunit;

namespace AuroraScript.Tests;

public sealed class NativeObjectDirectCallTests
{
    [Theory]
    [InlineData(CompilationMode.Dynamic)]
    [InlineData(CompilationMode.OnlyRun)]
    public async Task ProvenNativeObjectsKeepDynamicSemantics(CompilationMode mode)
    {
        using var workspace = new TestWorkspace();
        var (_, domain) = await workspace.CompileModuleAsync(
            """
            @module(TEST);
            export func construct() Number {
                var vec = new Vec2(6, 8);
                return vec.length();
            }
            export func writeField() Number {
                var vec = new Vec2(1, 2);
                vec.x = 10;
                return vec.x + vec.y;
            }
            export func chain() Number {
                var vec = new Vec2(1, 2);
                return vec.add(new Vec2(3, 4)).y;
            }
            export func loop(Number count) Number {
                var vec = new Vec2(1, 2);
                var total = 0;
                for (var i = 0; i < count; i++) {
                    vec.x = i;
                    total = total + vec.x;
                }
                return total;
            }
            export func branch(Boolean flag) Number {
                var vec = new Vec2(1, 2);
                if (flag) {
                    vec = new Vec2(5, 6);
                }
                return vec.x;
            }
            export func reassignedToDynamic(other) Number {
                var vec = new Vec2(1, 2);
                vec = other;
                return vec.x;
            }
            """,
            mode,
            configureGlobal: global => Vec2.Register(global),
            hostExports: true);

        ScriptAssert.Equal(10, TestWorkspace.Execute(domain, "construct"));
        ScriptAssert.Equal(12, TestWorkspace.Execute(domain, "writeField"));
        ScriptAssert.Equal(6, TestWorkspace.Execute(domain, "chain"));
        ScriptAssert.Equal(
            10,
            TestWorkspace.Execute(
                domain,
                "loop",
                arguments: [ScriptDatum.FromNumber(5)]));
        ScriptAssert.Equal(
            5,
            TestWorkspace.Execute(
                domain,
                "branch",
                arguments: [ScriptDatum.True]));
        ScriptAssert.Equal(
            1,
            TestWorkspace.Execute(
                domain,
                "branch",
                arguments: [ScriptDatum.False]));
        ScriptAssert.Equal(
            7,
            TestWorkspace.Execute(
                domain,
                "reassignedToDynamic",
                arguments: [ScriptDatum.FromObject(new Vec2(7, 8))]));
    }

    [Fact]
    public async Task UnprovenReceiverStaysOnTheDynamicProtocol()
    {
        using var workspace = new TestWorkspace();
        var (_, domain) = await workspace.CompileModuleAsync(
            """
            @module(TEST);
            export func readX(vec) Number {
                return vec.x;
            }
            export func shadowed() Number {
                var Vec2 = 1;
                return Vec2;
            }
            """,
            configureGlobal: global => Vec2.Register(global),
            hostExports: true);

        ScriptAssert.Equal(
            3,
            TestWorkspace.Execute(
                domain,
                "readX",
                arguments: [ScriptDatum.FromObject(new Vec2(3, 4))]));
        ScriptAssert.Equal(1, TestWorkspace.Execute(domain, "shadowed"));
    }

#if NET9_0_OR_GREATER
    [Fact]
    public async Task ProvenNativeObjectsBindDirectlyToClrMembers()
    {
        using var workspace = new TestWorkspace();
        workspace.WriteSource(
            "main.as",
            """
            @module(TEST);
            export func direct() Number {
                var vec = new Vec2(6, 8);
                vec.x = 3;
                return vec.length() + vec.y;
            }
            export func dynamicReceiver(vec) Number {
                return vec.length();
            }
            """);
        var assemblyPath = Path.Combine(workspace.Root, "test-output.dll");
        var engine = workspace.CreateEngine(
            CompilationMode.Persistence,
            assemblyOut: assemblyPath,
            hostExports: true);
        await engine.BuildAsync(["main.as"]);
        using var domain = engine.CreateDomain(global => Vec2.Register(global));
        // length() runs after the field write, so it measures (3, 8).
        ScriptAssert.Equal(Math.Sqrt(73D) + 8D, TestWorkspace.Execute(domain, "direct"));

        using var stream = File.OpenRead(assemblyPath);
        using var peReader = new PEReader(stream);
        var reader = peReader.GetMetadataReader();
        var direct = FindMethod(reader, "direct$typed");
        var dynamicReceiver = FindMethod(reader, "dynamicReceiver$typed");
        var directIl = peReader.GetMethodBody(
            reader.GetMethodDefinition(direct).RelativeVirtualAddress).GetILBytes();
        var dynamicIl = peReader.GetMethodBody(
            reader.GetMethodDefinition(dynamicReceiver).RelativeVirtualAddress).GetILBytes();

        Assert.Contains(
            FindVec2MemberTokens(reader, ".ctor"),
            token => ContainsInstruction(directIl, 0x73, token));
        Assert.Contains(
            FindVec2MemberTokens(reader, "LengthCore"),
            token => ContainsInstruction(directIl, 0x6F, token));
        Assert.Contains(
            FindVec2MemberTokens(reader, "Y"),
            token => ContainsInstruction(directIl, 0x7B, token));
        Assert.Contains(
            FindVec2MemberTokens(reader, "X"),
            token => ContainsInstruction(directIl, 0x7D, token));

        // A receiver the compiler cannot prove keeps using the dynamic protocol.
        Assert.DoesNotContain(
            FindVec2MemberTokens(reader, "LengthCore"),
            token => ContainsInstruction(dynamicIl, 0x6F, token));
    }

    private static MethodDefinitionHandle FindMethod(MetadataReader reader, string name)
    {
        foreach (var handle in reader.MethodDefinitions)
        {
            if (reader.GetString(reader.GetMethodDefinition(handle).Name) == name)
            {
                return handle;
            }
        }
        Assert.Fail($"Method '{name}' was not emitted.");
        return default;
    }

    private static int[] FindVec2MemberTokens(MetadataReader reader, string memberName)
    {
        var tokens = reader.MemberReferences
            .Where(handle =>
            {
                var reference = reader.GetMemberReference(handle);
                return reader.GetString(reference.Name) == memberName &&
                    reference.Parent.Kind == HandleKind.TypeReference &&
                    reader.GetString(
                        reader.GetTypeReference(
                            (TypeReferenceHandle)reference.Parent).Name) == "Vec2";
            })
            .Select(handle => MetadataTokens.GetToken(handle))
            .ToArray();
        Assert.NotEmpty(tokens);
        return tokens;
    }

    private static bool ContainsInstruction(
        ReadOnlySpan<byte> il,
        byte opcode,
        int metadataToken)
    {
        for (var i = 0; i + 5 <= il.Length; i++)
        {
            if (il[i] == opcode &&
                BinaryPrimitives.ReadInt32LittleEndian(
                    il.Slice(i + 1, 4)) == metadataToken)
            {
                return true;
            }
        }
        return false;
    }
#endif
}
