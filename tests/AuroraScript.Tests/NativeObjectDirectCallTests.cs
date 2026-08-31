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
            export func mutate() Number {
                var vec = new Vec2(1000, 2000);
                var previous = vec.x++;
                var current = ++vec.x;
                vec.x += 65;
                vec.x -= 3;
                return previous + current + vec.x;
            }
            export func captured() Number {
                var vec = new Vec2(1, 2);
                var read = () => vec.x;
                vec.x += 2;
                return read();
            }
            """,
            mode,
            nativeTypes: true);

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
        ScriptAssert.Equal(3066, TestWorkspace.Execute(domain, "mutate"));
        ScriptAssert.Equal(3, TestWorkspace.Execute(domain, "captured"));
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
            nativeTypes: true);

        ScriptAssert.Equal(
            3,
            TestWorkspace.Execute(
                domain,
                "readX",
                arguments: [ScriptDatum.FromObject(new Vec2(3, 4))]));
        ScriptAssert.Equal(1, TestWorkspace.Execute(domain, "shadowed"));
    }

    [Theory]
    [InlineData(CompilationMode.Dynamic)]
    [InlineData(CompilationMode.OnlyRun)]
    public async Task AmbientDeclareTypeDoesNotBlockNativeDirectCalls(CompilationMode mode)
    {
        using var workspace = new TestWorkspace();
        workspace.WriteSource(
            "global.as",
            """
            @global();
            declare type Vec2 {
                constructor(Number x, Number y);
                Number x;
                Number y;
                func length() Number;
                static const Number DIMENSIONS;
                static func from(Number x, Number y) Vec2;
                static func length(Number x, Number y) Number;
            }
            """);
        workspace.WriteSource(
            "main.as",
            """
            @module(TEST);
            export func run() Number {
                var constructed = new Vec2(6, 8);
                var factory = Vec2.from(3, 4);
                return constructed.length() + factory.x + Vec2.DIMENSIONS + Vec2.length(6, 8);
            }
            """);
        var assemblyPath = mode == CompilationMode.Persistence
            ? Path.Combine(workspace.Root, "ambient-output.dll")
            : null;
        var engine = workspace.CreateEngine(mode, assemblyOut: assemblyPath, nativeTypes: true);
        await engine.BuildAsync(["main.as"]);
        using var domain = engine.CreateDomain();
        ScriptAssert.Equal(10D + 3D + 2D + 10D, TestWorkspace.Execute(domain, "run"));
    }

#if NET9_0_OR_GREATER
    [Fact]
    public async Task NativeObjectStaticsBindFromHostExportMetadata()
    {
        using var workspace = new TestWorkspace();
        workspace.WriteSource(
            "global.as",
            """
            @global();
            declare type Vec2 {
                constructor(Number x, Number y);
                Number x;
                static const Number DIMENSIONS;
                static func from(Number x, Number y) Vec2;
                static func length(Number x, Number y) Number;
            }
            """);
        workspace.WriteSource(
            "main.as",
            """
            @module(TEST);
            export func direct() Number {
                return Vec2.length(6, 8) + Vec2.DIMENSIONS;
            }
            export func factory() Number {
                var vec = Vec2.from(3, 4);
                return vec.factoryValue() + vec.x;
            }
            """);
        var assemblyPath = Path.Combine(workspace.Root, "static-output.dll");
        var engine = workspace.CreateEngine(
            CompilationMode.Persistence,
            assemblyOut: assemblyPath,
            nativeTypes: true);
        await engine.BuildAsync(["main.as"]);
        using var domain = engine.CreateDomain();
        ScriptAssert.Equal(12D, TestWorkspace.Execute(domain, "direct"));
        ScriptAssert.Equal(10D, TestWorkspace.Execute(domain, "factory"));

        using var stream = File.OpenRead(assemblyPath);
        using var peReader = new PEReader(stream);
        var reader = peReader.GetMetadataReader();
        var direct = FindMethod(reader, "direct$typed");
        var factory = FindMethod(reader, "factory$typed");
        var il = peReader.GetMethodBody(
            reader.GetMethodDefinition(direct).RelativeVirtualAddress).GetILBytes();
        var factoryIl = peReader.GetMethodBody(
            reader.GetMethodDefinition(factory).RelativeVirtualAddress).GetILBytes();

        Assert.Contains(
            FindVec2MemberTokens(reader, nameof(Vec2.StaticLengthCore)),
            token => ContainsInstruction(il, 0x28, token));
        Assert.Contains(
            FindVec2MemberTokens(reader, nameof(Vec2.Dimensions)),
            token => ContainsInstruction(il, 0x7E, token));
        Assert.Contains(
            FindVec2MemberTokens(reader, nameof(Vec2.FromCore)),
            token => ContainsInstruction(factoryIl, 0x28, token));
        Assert.Contains(
            FindVec2MemberTokens(reader, nameof(Vec2.FactoryValueCore)),
            token => ContainsInstruction(factoryIl, 0x6F, token));
        Assert.Contains(
            FindVec2MemberTokens(reader, nameof(Vec2.X)),
            token => ContainsInstruction(factoryIl, 0x7B, token));
        AssertNoCallsTo(reader, factoryIl, "ScriptDatum", "FromObject");
        AssertNoCallsTo(reader, factoryIl, "ScriptDatum", "ToObject");
    }

    [Fact]
    public async Task NativeObjectStaticsStayDynamicWithoutHostExportMetadata()
    {
        using var workspace = new TestWorkspace();
        workspace.WriteSource(
            "main.as",
            """
            @module(TEST);
            export func dynamic() Number {
                return Vec2.length(6, 8) + Vec2.DIMENSIONS;
            }
            """);
        var assemblyPath = Path.Combine(workspace.Root, "dynamic-static-output.dll");
        var engine = workspace.CreateEngine(
            CompilationMode.Persistence,
            assemblyOut: assemblyPath);
        await engine.BuildAsync(["main.as"]);
        using var domain = engine.CreateDomain(global => Vec2.Register(global));
        ScriptAssert.Equal(12D, TestWorkspace.Execute(domain, "dynamic"));

        using var stream = File.OpenRead(assemblyPath);
        using var peReader = new PEReader(stream);
        var reader = peReader.GetMetadataReader();

        Assert.Empty(FindMemberTokens(reader, "Vec2", nameof(Vec2.StaticLengthCore)));
        Assert.Empty(FindMemberTokens(reader, "Vec2", nameof(Vec2.Dimensions)));
    }

    [Fact]
    public async Task ProvenNativeObjectsBindDirectlyToClrMembers()
    {
        using var workspace = new TestWorkspace();
        workspace.WriteSource(
            "global.as",
            """
            @global();
            declare type Vec2 {
                constructor(Number x, Number y);
                Number x;
                Number y;
                func length() Number;
            }
            """);
        workspace.WriteSource(
            "main.as",
            """
            @module(TEST);
            export func direct() Number {
                var vec = new Vec2(6, 8);
                var original = vec.x++;
                vec.x += 2;
                vec.x = 3;
                return original + vec.length() + vec.y;
            }
            export func dynamicReceiver(vec) Number {
                return vec.length();
            }
            """);
        var assemblyPath = Path.Combine(workspace.Root, "test-output.dll");
        var engine = workspace.CreateEngine(
            CompilationMode.Persistence,
            assemblyOut: assemblyPath,
            nativeTypes: true);
        await engine.BuildAsync(["main.as"]);
        using var domain = engine.CreateDomain();
        // length() runs after the field write, so it measures (3, 8).
        ScriptAssert.Equal(6D + Math.Sqrt(73D) + 8D, TestWorkspace.Execute(domain, "direct"));

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
        AssertNoCallsTo(reader, directIl, "ScriptDatum", "FromObject");
        AssertNoCallsTo(reader, directIl, "ScriptDatum", "ToObject");
        AssertNoCallsTo(reader, directIl, "TypedRuntimeOps", "ChangeByOne");

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
        var tokens = FindMemberTokens(reader, "Vec2", memberName);
        Assert.NotEmpty(tokens);
        return tokens;
    }

    private static int[] FindMemberTokens(
        MetadataReader reader,
        string typeName,
        string memberName)
    {
        return reader.MemberReferences
            .Where(handle =>
            {
                var reference = reader.GetMemberReference(handle);
                return reader.GetString(reference.Name) == memberName &&
                    reference.Parent.Kind == HandleKind.TypeReference &&
                    reader.GetString(
                        reader.GetTypeReference(
                            (TypeReferenceHandle)reference.Parent).Name) == typeName;
            })
            .Select(handle => MetadataTokens.GetToken(handle))
            .ToArray();
    }

    private static void AssertNoCallsTo(
        MetadataReader reader,
        ReadOnlySpan<byte> il,
        string typeName,
        string memberName)
    {
        foreach (var token in FindMemberTokens(reader, typeName, memberName))
        {
            Assert.False(ContainsInstruction(il, 0x28, token));
            Assert.False(ContainsInstruction(il, 0x6F, token));
        }
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
