using AuroraScript.Tests.Host;
using AuroraScript.Tests.Infrastructure;
using AuroraScript.Compiler.Backend;
using AuroraScript.Runtime;
using AuroraScript.Runtime.Types;
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
    public void CompilerCatalogIncludesBuiltinsAndOnlySelectedApplicationTypes()
    {
        var catalog = new HostExportCatalog([typeof(Vec2)]);

        Assert.True(catalog.TryGetNativeObject("Vec2", out _));
        Assert.True(catalog.TryGetGlobal("Vec2", "from", out _));
        Assert.False(catalog.TryGetGlobal("Stats", "mean", out _));
        Assert.True(catalog.TryGetGlobal("Math", "abs", out _));
    }

    [Fact]
    public void MathSupportExportsAreGeneratedAtBuildTime()
    {
        var type = typeof(AuroraScript.Runtime.Builtin.MathSupport);
        var scriptType = type.GetField(
            "Type",
            BindingFlags.Public | BindingFlags.Static)?.GetValue(null);

        Assert.IsAssignableFrom<ScriptType>(scriptType);
        Assert.NotNull(type.GetMethod("Register", BindingFlags.Public | BindingFlags.Static));
        Assert.NotNull(type.GetMethod("__Static_ABS", BindingFlags.Public | BindingFlags.Static));
        Assert.NotNull(type.GetField("PI", BindingFlags.Public | BindingFlags.Static));
    }

    [Fact]
    public async Task StaticOnlyNativeTypeIsATypeButNotConstructible()
    {
        using var workspace = new TestWorkspace();
        var (_, domain) = await workspace.CompileModuleAsync(
            """
            @module(TEST);
            export func typeName() String {
                return typeof Math;
            }
            export func construct() {
                return new Math();
            }
            """);

        ScriptAssert.Equal("type", TestWorkspace.Execute(domain, "typeName"));
        Assert.Throws<AuroraRuntimeException>(() =>
            TestWorkspace.Execute(domain, "construct"));
    }

    [Fact]
    public async Task MathAbsUsesWeakCoercionAndReturnsNaNOnFailure()
    {
        using var workspace = new TestWorkspace();
        var (_, domain) = await workspace.CompileModuleAsync(
            """
            @module(TEST);
            export func run() {
                var parsed = Math.abs("-4");
                var invalid = Math.abs("bad");
                return [parsed, invalid != invalid];
            }
            """);

        ScriptAssert.Equal(new object?[] { 4D, true }, TestWorkspace.Execute(domain, "run"));
    }

    [Fact]
    public async Task MathMaxAcceptsVariadicNumbers()
    {
        using var workspace = new TestWorkspace();
        var (_, domain) = await workspace.CompileModuleAsync(
            """
            @module(TEST);
            export func run() {
                return Math.max(1, 8, 3);
            }
            """);

        ScriptAssert.Equal(8D, TestWorkspace.Execute(domain, "run"));
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
            """,
            nativeTypes: true);

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
            """,
            nativeTypes: true);

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
            """,
            nativeTypes: true);

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
            assemblyOut: assemblyPath,
            nativeTypes: true);
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
            if (reader.GetString(member.Name) == nameof(StatsSupport.MeanCore))
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

    [Fact]
    public async Task UnshadowedMathPiLoadsHostFieldDirectly()
    {
        using var workspace = new TestWorkspace();
        var assemblyPath = Path.Combine(workspace.Root, "host-constant.dll");
        workspace.WriteSource(
            "main.as",
            """
            @module(TEST);
            export func run() Number {
                return Math.PI;
            }
            """);

        var engine = workspace.CreateEngine(
            CompilationMode.Persistence,
            assemblyOut: assemblyPath);
        await engine.BuildAsync(["main.as"]);
        using var domain = engine.CreateDomain();
        ScriptAssert.Equal(Math.PI, TestWorkspace.Execute(domain, "run"));

        using var stream = File.OpenRead(assemblyPath);
        using var peReader = new PEReader(stream);
        var reader = peReader.GetMetadataReader();
        var fieldToken = 0;
        foreach (var handle in reader.MemberReferences)
        {
            var member = reader.GetMemberReference(handle);
            if (reader.GetString(member.Name) == nameof(
                    AuroraScript.Runtime.Builtin.MathSupport.PI))
            {
                fieldToken = MetadataTokens.GetToken(handle);
                break;
            }
        }

        Assert.NotEqual(0, fieldToken);
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
        Assert.True(ContainsLdsfld(il, fieldToken));
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
            """,
            nativeTypes: true);

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
            """,
            nativeTypes: true);

        ScriptAssert.Equal(99D, TestWorkspace.Execute(domain, "run"));
    }

    [Fact]
    public async Task ShadowedMathKeepsOrdinaryPropertyRead()
    {
        using var workspace = new TestWorkspace();
        var (_, domain) = await workspace.CompileModuleAsync(
            """
            @module(TEST);
            export func run() {
                var Math = { PI: 1 };
                return Math.PI;
            }
            """);

        ScriptAssert.Equal(1D, TestWorkspace.Execute(domain, "run"));
    }

    [Fact]
    public async Task GeneratedExportSupportsStringAndInt32CoreParameters()
    {
        using var workspace = new TestWorkspace();
        var (_, domain) = await workspace.CompileModuleAsync(
            """
            @module(TEST);
            export func run(value) {
                return Stats.chat("piece-", value);
            }
            """,
            nativeTypes: true);

        ScriptAssert.Equal(
            "piece-7",
            TestWorkspace.Execute(
                domain,
                "run",
                arguments: [ScriptDatum.FromNumber(7)]));
        Assert.Throws<AuroraRuntimeException>(() =>
            TestWorkspace.Execute(
                domain,
                "run",
                arguments: [ScriptDatum.FromNumber(1.5)]));
        Assert.Throws<AuroraRuntimeException>(() =>
            TestWorkspace.Execute(
                domain,
                "run",
                arguments: [ScriptDatum.FromNumber((double)int.MaxValue + 1)]));
    }

    [Fact]
    public async Task GeneratedExportSupportsScriptDatumArguments()
    {
        using var workspace = new TestWorkspace();
        var (_, domain) = await workspace.CompileModuleAsync(
            """
            @module(TEST);
            export func run() {
                return [
                    Stats.echo(42),
                    Stats.echo("ok"),
                    Stats.echo({ answer: 7 }).answer
                ];
            }
            """,
            nativeTypes: true);

        ScriptAssert.Equal(new object?[] { 42D, "ok", 7D }, TestWorkspace.Execute(domain, "run"));
    }

    [Fact]
    public async Task GeneratedExportInjectsScriptContextAndThisObject()
    {
        using var workspace = new TestWorkspace();
        var (_, domain) = await workspace.CompileModuleAsync(
            """
            @module(TEST);
            export func run() {
                return [
                    Stats.hasEngine(),
                    Stats.sameThis(Stats)
                ];
            }
            """,
            nativeTypes: true);

        ScriptAssert.Equal(new object?[] { true, true }, TestWorkspace.Execute(domain, "run"));
    }

    [Fact]
    public async Task GeneratedExportSupportsRestAndScriptObjectSubtypes()
    {
        using var workspace = new TestWorkspace();
        var (_, domain) = await workspace.CompileModuleAsync(
            """
            @module(TEST);
            export func run() {
                var values = tdoc Object {
                    StringBuffer text "hello",
                    Path file "a/b.as",
                    Int8Array bytes [1, 2],
                    Regex pattern { pattern "a+", flags "i" },
                    HashMap map [["x", 42]],
                    Date date "2020-01-02",
                };
                var array = Stats.array([1, 2]);
                var proxy = new Proxy({}, {
                    get: (object, key) => object[key],
                    set: (object, key, value) => { object[key] = value; }
                });
                var error = new Error("bad");
                var fn = () => 1;
                return [
                    Stats.restCount(1, "two", {}),
                    Stats.restCount(),
                    Stats.restCount(...[1, "two"]),
                    Stats.restAfter(10),
                    Stats.restAfter(10, null, {}),
                    typeof array,
                    typeof Stats.packed(values.bytes),
                    typeof Stats.int8Array(values.bytes),
                    typeof Stats.path(values.file),
                    typeof Stats.stringBuffer(values.text),
                    typeof Stats.proxy(proxy),
                    typeof Stats.regex(values.pattern),
                    typeof Stats.date(values.date),
                    typeof Stats.hashMap(values.map),
                    typeof Stats.error(error),
                    typeof Stats.function(fn),
                    Stats.immutable(42),
                    Stats.nullValue(null)
                ];
            }
            export func wrongSubtype() {
                return Stats.path({});
            }
            """,
            dateTimeFormat: "yyyy-MM-dd",
            nativeTypes: true);

        ScriptAssert.Equal(
            new object?[]
            {
                3D, 0D, 2D, 10D, 12D,
                "array", "Int8Array", "Int8Array", "Path",
                "StringBuffer", "object", "regex", "date", "HashMap",
                "error", "function", 42D, null
            },
            TestWorkspace.Execute(domain, "run"));
        Assert.Throws<AuroraRuntimeException>(() =>
            TestWorkspace.Execute(domain, "wrongSubtype"));
    }

    [Fact]
    public async Task ShadowedThisObjectIsNotTheBuiltinGlobal()
    {
        using var workspace = new TestWorkspace();
        var (_, domain) = await workspace.CompileModuleAsync(
            """
            @module(TEST);
            export func run() {
                var Stats = { sameThis: (other) => false };
                return Stats.sameThis(Stats);
            }
            """,
            nativeTypes: true);

        ScriptAssert.Equal(false, TestWorkspace.Execute(domain, "run"));
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

    private static bool ContainsLdsfld(
        ReadOnlySpan<byte> il,
        int metadataToken)
    {
        for (var i = 0; i + 5 <= il.Length; i++)
        {
            if (il[i] == 0x7E &&
                BinaryPrimitives.ReadInt32LittleEndian(
                    il.Slice(i + 1, 4)) == metadataToken)
            {
                return true;
            }
        }
        return false;
    }
}
