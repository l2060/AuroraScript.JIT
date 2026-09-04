using AuroraScript.Runtime;
using AuroraScript.Runtime.Serialization;
using AuroraScript.Runtime.Types;
using AuroraScript.Tests.Infrastructure;
using System;
using System.Threading.Tasks;
using Xunit;

namespace AuroraScript.Tests;

public sealed class TempTaskProbeTests
{
    [Theory]
    [InlineData(CompilationMode.Dynamic)]
    [InlineData(CompilationMode.OnlyRun)]
#if NET9_0_OR_GREATER
    [InlineData(CompilationMode.Persistence)]
#endif
    public async Task NewArraysAndOperations(CompilationMode mode)
    {
        using var workspace = new TestWorkspace();
        var (_, domain) = await workspace.CompileModuleAsync("""
            @module(TEST);
            func read(a) { return [a[0], a[1], a.length]; }
            export func run() {
                var u8 = tdoc UInt8Array [0, 255];
                var i16 = tdoc Int16Array [-32768, 32767];
                var u16 = tdoc UInt16Array [0, 65535];
                var u32 = tdoc UInt32Array [0, 4294967295];
                var i64 = tdoc Int64Array [-9007199254740991, 9007199254740991];
                var u64 = tdoc UInt64Array [0, 9007199254740991];
                u8[0] = 5; u8[1]++; u8[0] += 2;
                return [read(u8), read(i16), read(u16), read(u32), read(i64), read(u64)];
            }
            """, mode);
        var result = TestWorkspace.Execute(domain, "run");
        var rows = Assert.IsType<ScriptArray>(result.Object);
        ScriptAssert.Equal(new object?[] { 7, 0, 2 }, rows.GetElement(0));
        ScriptAssert.Equal(new object?[] { -32768, 32767, 2 }, rows.GetElement(1));
        ScriptAssert.Equal(new object?[] { 0, 65535, 2 }, rows.GetElement(2));
        ScriptAssert.Equal(new object?[] { 0, 4294967295d, 2 }, rows.GetElement(3));

        var signed = Assert.IsType<ScriptArray>(rows.GetElement(4).Object);
        Assert.Equal(ValueKind.Int64, signed.GetElement(0).Kind);
        Assert.Equal(-9007199254740991L, signed.GetElement(0).Int64);
        Assert.Equal(9007199254740991L, signed.GetElement(1).Int64);
        ScriptAssert.Equal(2, signed.GetElement(2));

        var unsigned = Assert.IsType<ScriptArray>(rows.GetElement(5).Object);
        Assert.Equal(ValueKind.UInt64, unsigned.GetElement(0).Kind);
        Assert.Equal(0UL, unsigned.GetElement(0).UInt64);
        Assert.Equal(9007199254740991UL, unsigned.GetElement(1).UInt64);
        ScriptAssert.Equal(2, unsigned.GetElement(2));
    }

    [Fact]
    public void ReaderNewArraysAndRoundTrip()
    {
        using var workspace = new TestWorkspace();
        var engine = workspace.CreateEngine();
        foreach (var text in new[] { "UInt8Array [0, 255]", "Int16Array [-32768, 32767]", "UInt16Array [0, 65535]", "UInt32Array [0, 4294967295]", "Int64Array [-9223372036854775808, 9223372036854775807]", "UInt64Array [0, 18446744073709551615]" })
        {
            var value = TypedDocumentSerializer.Deserialize(engine, text);
            var output = TypedDocumentSerializer.Serialize(engine, value, new TypedDocumentOptions { Indented = false, EmitTypeNames = true });
            Assert.NotEmpty(output);
            var restored = TypedDocumentSerializer.Deserialize(engine, output);
            Assert.Equal(value.Object?.GetType(), restored.Object?.GetType());
        }
        Assert.IsType<ScriptInt64Array>(TypedDocumentSerializer.Deserialize(engine, "Int64Array [Number 1]").Object);
        Assert.IsType<ScriptUInt64Array>(TypedDocumentSerializer.Deserialize(engine, "UInt64Array [Number 1]").Object);
    }

    [Fact]
    public async Task BinderRejectsShapes()
    {
        using var workspace = new TestWorkspace();
        var (_, domain) = await workspace.CompileModuleAsync("""
            @module(TEST);
            export func run(value) {
                return [tdoc Object $(value), tdoc Array $(value)];
            }
            """);
        Assert.ThrowsAny<Exception>(() => TestWorkspace.Execute(domain, "run", "TEST", ScriptDatum.FromNumber(42)));
    }

    [Fact]
    public async Task BinderChecksNestedPackedAndDate()
    {
        using var workspace = new TestWorkspace();
        var (_, domain) = await workspace.CompileModuleAsync("""
            @module(TEST);
            export func packed(value) { return tdoc Int8Array $(value); }
            export func nested(value) { return tdoc Object { Int8Array bytes $(value) }; }
            export func date(value) { return tdoc Date $(value); }
            """);
        var values = new ScriptArray();
        values.Push(ScriptDatum.FromNumber(255));
        var datum = ScriptDatum.FromArray(values);
        Assert.ThrowsAny<Exception>(() => TestWorkspace.Execute(domain, "packed", "TEST", datum));
        var nestedError = Assert.ThrowsAny<Exception>(() => TestWorkspace.Execute(domain, "nested", "TEST", datum));
        Assert.Contains("$.bytes[0]", nestedError.ToString(), StringComparison.Ordinal);
        Assert.ThrowsAny<Exception>(() => TestWorkspace.Execute(domain, "date", "TEST", ScriptDatum.FromString("bad")));
    }

    [Fact]
    public async Task ModuleInitializerUsesNestedPath()
    {
        using var workspace = new TestWorkspace();
        var engine = workspace.CreateEngine();
        workspace.WriteSource("main.as", """
            @module(TEST);
            const values = [255];
            const value = tdoc Object { Int8Array bytes $(values) };
            export func run() { return value; }
            """);
        await engine.BuildAsync(["main.as"]);
        var error = Assert.ThrowsAny<Exception>(() => engine.CreateDomain());
        Assert.Contains("$.bytes[0]", error.ToString(), StringComparison.Ordinal);
    }

    [Fact]
    public async Task CompileDiagnosticsIncludeTDocPath()
    {
        using var workspace = new TestWorkspace();
        var error = await Assert.ThrowsAsync<AuroraCompilationException>(() =>
            workspace.CompileModuleAsync("""
                @module(TEST);
                export func run() { return tdoc Object { Int8Array bytes [255] }; }
                """));
        Assert.Contains("$.bytes[0]", error.ToString(), StringComparison.Ordinal);
    }
}
