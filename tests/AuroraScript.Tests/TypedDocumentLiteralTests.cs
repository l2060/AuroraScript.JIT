using AuroraScript.Runtime;
using AuroraScript.Runtime.Types;
using AuroraScript.Tests.Infrastructure;
using System.Threading.Tasks;
using Xunit;

namespace AuroraScript.Tests;

public sealed class TypedDocumentLiteralTests
{
    [Fact]
    public async Task TDocLiteralBuildsObjectsArraysAndDynamicValues()
    {
        using var workspace = new TestWorkspace();
        var (_, domain) = await workspace.CompileModuleAsync(
            """
            @module(TEST);
            export func run(user) {
                var value = tdoc Object {
                    readonly String id $(user.id),
                    name "Aurora",
                    dynamic $(user.role),
                    tags ["a", $(user.role), 3],
                };
                return [value.id, value.name, value.dynamic, value.tags[1], value.tags.length];
            }
            """);

        var user = new ScriptObject();
        user.Define("id", ScriptDatum.FromString("u-1"));
        user.Define("role", ScriptDatum.FromString("admin"));
        ScriptAssert.Equal(
            new object?[] { "u-1", "Aurora", "admin", "admin", 3 },
            TestWorkspace.Execute(domain, "run", "TEST", ScriptDatum.FromObject(user)));
    }

    [Fact]
    public async Task TDocLiteralBuildsPackedArraysAndReadonlyProperties()
    {
        using var workspace = new TestWorkspace();
        var (_, domain) = await workspace.CompileModuleAsync(
            """
            @module(TEST);
            export func run() {
                var value = tdoc Object {
                    readonly id "u-1",
                    Int8Array bytes [-2, 0, 3],
                    BooleanArray flags [true, false],
                };
                var failed = false;
                try { value.id = "changed"; } catch (e) { failed = true; }
                return [value.id, value.bytes[0], value.bytes[2], value.flags[0], failed];
            }
            """);

        ScriptAssert.Equal(
            new object?[] { "u-1", -2, 3, true, true },
            TestWorkspace.Execute(domain, "run"));
    }

    [Fact]
    public async Task TDocLiteralBuildsBuiltinTypedObjects()
    {
        using var workspace = new TestWorkspace();
        var (_, domain) = await workspace.CompileModuleAsync(
            """
            @module(TEST);
            export func run() {
                var value = tdoc Object {
                    StringBuffer text "hello",
                    Path file "a/b.as",
                    Regex pattern { pattern "a+", flags "i" },
                    HashMap values [["x", 42]],
                };
                return [value.text.toString(), value.file.toString(), value.pattern.test("AAA"), value.values.get("x")];
            }
            """);

        ScriptAssert.Equal(
            new object?[] { "hello", "a/b.as", true, 42 },
            TestWorkspace.Execute(domain, "run"));
    }

    [Fact]
    public async Task TDocLiteralBuildsDateValuesWithMilliseconds()
    {
        using var workspace = new TestWorkspace();
        var (_, domain) = await workspace.CompileModuleAsync(
            """
            @module(TEST);
            export func run() {
                var value = tdoc Object {
                    Date birthday "1991-02-01 12:32:55 666",
                };
                return [value.birthday.year, value.birthday.millisecond, value.birthday.toString("yyyy-MM-dd HH:mm:ss fff"), JSON.stringify(value, false), TDoc.stringify(value, false)];
            }
            """,
            dateTimeFormat: "yyyy-MM-dd HH:mm:ss fff");

        ScriptAssert.Equal(
            new object?[] { 1991, 666, "1991-02-01 12:32:55 666", "{\"birthday\":\"1991-02-01 12:32:55 666\"}", "{Date birthday \"1991-02-01 12:32:55 666\"}" },
            TestWorkspace.Execute(domain, "run"));
    }

    [Fact]
    public async Task TDocLiteralWorksInModuleInitializers()
    {
        using var workspace = new TestWorkspace();
        var (_, domain) = await workspace.CompileModuleAsync(
            """
            @module(TEST);
            const value = tdoc Object {
                readonly id "module",
                StringBuffer text "ok",
                Int32Array values [1, 2, 3],
            };
            export func run() { return [value.id, value.text.toString(), value.values[2]]; }
            """);

        ScriptAssert.Equal(
            new object?[] { "module", "ok", 3 },
            TestWorkspace.Execute(domain, "run"));
    }

    [Theory]
    [InlineData(CompilationMode.Dynamic)]
    [InlineData(CompilationMode.OnlyRun)]
#if NET9_0_OR_GREATER
    [InlineData(CompilationMode.Persistence)]
#endif
    public async Task TDocPackedLiteralsWriteKnownTargetsWithoutLosingWideConstants(CompilationMode mode)
    {
        using var workspace = new TestWorkspace();
        var (_, domain) = await workspace.CompileModuleAsync(
            """
            @module(TEST);
            const moduleInt64 = tdoc Int64Array [-9223372036854775808, 9007199254740993, 9223372036854775807];
            const moduleUInt64 = tdoc UInt64Array [0, 9007199254740993, 18446744073709551615];
            const moduleDate = tdoc Date 638679147930000001;

            export func functionInt64() {
                return tdoc Int64Array [-9223372036854775808, 9007199254740993, 9223372036854775807];
            }
            export func functionUInt64() {
                return tdoc UInt64Array [0, 9007199254740993, 18446744073709551615];
            }
            export func functionDate() { return tdoc Date 638679147930000001; }
            export func moduleValues() { return [moduleInt64, moduleUInt64, moduleDate]; }
            """,
            mode);

        var functionInt64 = Assert.IsType<ScriptInt64Array>(
            TestWorkspace.Execute(domain, "functionInt64").Object);
        Assert.Equal(long.MinValue, functionInt64.GetElement(0));
        Assert.Equal(9007199254740993L, functionInt64.GetElement(1));
        Assert.Equal(long.MaxValue, functionInt64.GetElement(2));

        var functionUInt64 = Assert.IsType<ScriptUInt64Array>(
            TestWorkspace.Execute(domain, "functionUInt64").Object);
        Assert.Equal(0UL, functionUInt64.GetElement(0));
        Assert.Equal(9007199254740993UL, functionUInt64.GetElement(1));
        Assert.Equal(ulong.MaxValue, functionUInt64.GetElement(2));

        var functionDate = Assert.IsType<ScriptDate>(
            TestWorkspace.Execute(domain, "functionDate").Object);
        Assert.Equal(638679147930000001L, functionDate.Ticks);

        var moduleValues = Assert.IsType<ScriptArray>(
            TestWorkspace.Execute(domain, "moduleValues").Object);
        var moduleInt64 = Assert.IsType<ScriptInt64Array>(moduleValues.GetElement(0).Object);
        var moduleUInt64 = Assert.IsType<ScriptUInt64Array>(moduleValues.GetElement(1).Object);
        var moduleDate = Assert.IsType<ScriptDate>(moduleValues.GetElement(2).Object);
        Assert.Equal(9007199254740993L, moduleInt64.GetElement(1));
        Assert.Equal(ulong.MaxValue, moduleUInt64.GetElement(2));
        Assert.Equal(638679147930000001L, moduleDate.Ticks);
    }

    [Theory]
    [InlineData(CompilationMode.Dynamic)]
    [InlineData(CompilationMode.OnlyRun)]
#if NET9_0_OR_GREATER
    [InlineData(CompilationMode.Persistence)]
#endif
    public async Task EveryStaticPackedLiteralUsesItsFinalStorageType(CompilationMode mode)
    {
        using var workspace = new TestWorkspace();
        var (_, domain) = await workspace.CompileModuleAsync(
            """
            @module(TEST);
            const moduleInt32 = tdoc Int32Array [-2147483648, 2147483647];
            const moduleInt8 = tdoc Int8Array [-128, 127];
            const moduleFloat64 = tdoc Float64Array [1.25, -2.5e2];
            const moduleBoolean = tdoc BooleanArray [0, true];
            const moduleUInt8 = tdoc UInt8Array [0, 255];
            const moduleInt16 = tdoc Int16Array [-32768, 32767];
            const moduleUInt16 = tdoc UInt16Array [0, 65535];
            const moduleUInt32 = tdoc UInt32Array [0, 4294967295];
            const moduleInt64 = tdoc Int64Array [-9223372036854775808, 9007199254740993, 9223372036854775807];
            const moduleUInt64 = tdoc UInt64Array [0, 9007199254740993, 18446744073709551615];

            export func moduleValues() {
                return [moduleInt32, moduleInt8, moduleFloat64, moduleBoolean, moduleUInt8,
                    moduleInt16, moduleUInt16, moduleUInt32, moduleInt64, moduleUInt64];
            }
            export func functionValues() {
                return [tdoc Int32Array [-2147483648, 2147483647],
                    tdoc Int8Array [-128, 127],
                    tdoc Float64Array [1.25, -2.5e2],
                    tdoc BooleanArray [0, true],
                    tdoc UInt8Array [0, 255],
                    tdoc Int16Array [-32768, 32767],
                    tdoc UInt16Array [0, 65535],
                    tdoc UInt32Array [0, 4294967295],
                    tdoc Int64Array [-9223372036854775808, 9007199254740993, 9223372036854775807],
                    tdoc UInt64Array [0, 9007199254740993, 18446744073709551615]];
            }
            """,
            mode);

        AssertPackedValues(Assert.IsType<ScriptArray>(TestWorkspace.Execute(domain, "moduleValues").Object));
        AssertPackedValues(Assert.IsType<ScriptArray>(TestWorkspace.Execute(domain, "functionValues").Object));
    }

    [Fact]
    public async Task TDocPackedLiteralsValidateOnlyDynamicElementsBeforeDirectStore()
    {
        using var workspace = new TestWorkspace();
        var (_, domain) = await workspace.CompileModuleAsync(
            """
            @module(TEST);
            const seed = 1;
            const moduleValue = tdoc UInt16Array [0, $(seed), 65535];

            export func int32(value) { return tdoc Int32Array [-1, $(value), 1]; }
            export func int8(value) { return tdoc Int8Array [-1, $(value), 1]; }
            export func float32(value) { return tdoc Float32Array [1.5, $(value), 2.5]; }
            export func float64(value) { return tdoc Float64Array [1.5, $(value), 2.5]; }
            export func boolean(value) { return tdoc BooleanArray [true, $(value), false]; }
            export func uint8(value) { return tdoc UInt8Array [0, $(value), 255]; }
            export func int16(value) { return tdoc Int16Array [-1, $(value), 1]; }
            export func uint16(value) { return tdoc UInt16Array [0, $(value), 65535]; }
            export func uint32(value) { return tdoc UInt32Array [0, $(value), 4294967295]; }
            export func int64(value) { return tdoc Int64Array [-1, $(value), 9007199254740993]; }
            export func uint64(value) { return tdoc UInt64Array [0, $(value), 18446744073709551615]; }
            export func nestedUInt8(value) { return tdoc Object { UInt8Array bytes [$(value)] }; }
            export func moduleResult() { return moduleValue; }
            """);

        var one = ScriptDatum.FromNumber(1);
        Assert.Equal(1, Assert.IsType<ScriptInt32Array>(TestWorkspace.Execute(domain, "int32", "TEST", one).Object).GetElement(1));
        Assert.Equal((sbyte)1, Assert.IsType<ScriptInt8Array>(TestWorkspace.Execute(domain, "int8", "TEST", one).Object).GetElement(1));
        Assert.Equal(1f, Assert.IsType<ScriptFloat32Array>(TestWorkspace.Execute(domain, "float32", "TEST", one).Object).GetElement(1));
        Assert.Equal(1d, Assert.IsType<ScriptFloat64Array>(TestWorkspace.Execute(domain, "float64", "TEST", one).Object).GetElement(1));
        Assert.True(Assert.IsType<ScriptBooleanArray>(TestWorkspace.Execute(domain, "boolean", "TEST", one).Object).GetElement(1));
        Assert.Equal((byte)1, Assert.IsType<ScriptUInt8Array>(TestWorkspace.Execute(domain, "uint8", "TEST", one).Object).GetElement(1));
        Assert.Equal((short)1, Assert.IsType<ScriptInt16Array>(TestWorkspace.Execute(domain, "int16", "TEST", one).Object).GetElement(1));
        Assert.Equal((ushort)1, Assert.IsType<ScriptUInt16Array>(TestWorkspace.Execute(domain, "uint16", "TEST", one).Object).GetElement(1));
        Assert.Equal(1U, Assert.IsType<ScriptUInt32Array>(TestWorkspace.Execute(domain, "uint32", "TEST", one).Object).GetElement(1));
        Assert.Equal(1L, Assert.IsType<ScriptInt64Array>(TestWorkspace.Execute(domain, "int64", "TEST", one).Object).GetElement(1));
        Assert.Equal(1UL, Assert.IsType<ScriptUInt64Array>(TestWorkspace.Execute(domain, "uint64", "TEST", one).Object).GetElement(1));
        Assert.Equal((ushort)1, Assert.IsType<ScriptUInt16Array>(TestWorkspace.Execute(domain, "moduleResult").Object).GetElement(1));

        AssertPackedElementError(domain, "int32", 2147483648d);
        AssertPackedElementError(domain, "int8", 128d);
        AssertPackedElementError(domain, "float32", double.PositiveInfinity);
        AssertPackedElementError(domain, "float64", double.PositiveInfinity);
        AssertPackedElementError(domain, "boolean", 2d);
        AssertPackedElementError(domain, "uint8", 256d);
        AssertPackedElementError(domain, "int16", 32768d);
        AssertPackedElementError(domain, "uint16", 65536d);
        AssertPackedElementError(domain, "uint32", 4294967296d);
        AssertPackedElementError(domain, "int64", 9223372036854775808d);
        AssertPackedElementError(domain, "uint64", 18446744073709551616d);

        var nestedError = Assert.ThrowsAny<System.Exception>(() =>
            TestWorkspace.Execute(domain, "nestedUInt8", "TEST", ScriptDatum.FromNumber(256)));
        Assert.Contains("$.bytes[0]", nestedError.ToString(), System.StringComparison.Ordinal);
    }

    [Fact]
    public async Task TDocLiteralBuildsNativeTypedDocumentsAndKeepsDirectFields()
    {
        using var workspace = new TestWorkspace();
        var (_, domain) = await workspace.CompileModuleAsync(
            """
            @module(TEST);
            export func run() {
                var origin = tdoc Vec2 { x 0, y 0 };
                var named = tdoc Vec2 { x 3, y 4 };
                var packed = tdoc Vec2 [6, 8];
                return [origin.x, named.x, named.y, named.length(), packed.x, packed.y, packed.length()];
            }
            """,
            nativeTypes: true);

        ScriptAssert.Equal(
            new object?[] { 0, 3, 4, 5, 6, 8, 10 },
            TestWorkspace.Execute(domain, "run"));
    }

    [Fact]
    public async Task TDocLiteralBindsInterpolatedNativeTypedDocuments()
    {
        using var workspace = new TestWorkspace();
        var (_, domain) = await workspace.CompileModuleAsync(
            """
            @module(TEST);
            export func run(x, y) {
                var named = tdoc Vec2 { x $(x), y $(y) };
                var packed = tdoc Vec2 [$(x), $(y)];
                return [named.length(), packed.length(), packed.x, packed.y];
            }
            """,
            nativeTypes: true);

        ScriptAssert.Equal(new object?[] { 5d, 5d, 3d, 4d }, TestWorkspace.Execute(domain, "run", "TEST", 3d, 4d));
    }

    [Fact]
    public async Task ModuleConstTDocLiteralBuildsNativeTypedDocuments()
    {
        using var workspace = new TestWorkspace();
        var (_, domain) = await workspace.CompileModuleAsync(
            """
            @module(TEST);
            const origin = tdoc Vec2 [3, 4];
            export func run() {
                return origin.length();
            }
            """,
            nativeTypes: true);

        ScriptAssert.Equal(5d, TestWorkspace.Execute(domain, "run"));
    }

    [Fact]
    public async Task TDocLiteralBuildsNativeScalarTypedDocuments()
    {
        using var workspace = new TestWorkspace();
        var (_, domain) = await workspace.CompileModuleAsync(
            """
            @module(TEST);
            export func run() {
                var off = tdoc Flag false;
                var on = tdoc Flag { value true };
                var state = tdoc State 10000000000;
                var user = tdoc User "xxx,xx,xxx,xx";
                return [off.value, on.value, state.code, user.record];
            }
            """,
            nativeTypes: true);

        ScriptAssert.Equal(
            new object?[] { false, true, 10000000000d, "xxx,xx,xxx,xx" },
            TestWorkspace.Execute(domain, "run"));
    }

    [Fact]
    public async Task TDocLiteralBindsInterpolatedNativeScalarTypedDocuments()
    {
        using var workspace = new TestWorkspace();
        var (_, domain) = await workspace.CompileModuleAsync(
            """
            @module(TEST);
            export func run(flag, code, record) {
                var boundFlag = tdoc Flag $(flag);
                var boundState = tdoc State $(code);
                var boundUser = tdoc User $(record);
                return [boundFlag.value, boundState.code, boundUser.record];
            }
            """,
            nativeTypes: true);

        ScriptAssert.Equal(
            new object?[] { true, 8d, "a,b" },
            TestWorkspace.Execute(domain, "run", "TEST", true, 8d, "a,b"));
    }

    [Fact]
    public async Task ModuleConstTDocLiteralBuildsNativeScalarTypedDocuments()
    {
        using var workspace = new TestWorkspace();
        var (_, domain) = await workspace.CompileModuleAsync(
            """
            @module(TEST);
            const off = tdoc Flag false;
            const code = tdoc State -1;
            export func run() {
                return [off.value, code.code];
            }
            """,
            nativeTypes: true);

        ScriptAssert.Equal(new object?[] { false, -1d }, TestWorkspace.Execute(domain, "run"));
    }

    private static void AssertPackedValues(ScriptArray values)
    {
        Assert.Equal(int.MinValue, Assert.IsType<ScriptInt32Array>(values.GetElement(0).Object).GetElement(0));
        Assert.Equal(sbyte.MaxValue, Assert.IsType<ScriptInt8Array>(values.GetElement(1).Object).GetElement(1));
        Assert.Equal(-250d, Assert.IsType<ScriptFloat64Array>(values.GetElement(2).Object).GetElement(1));
        Assert.True(Assert.IsType<ScriptBooleanArray>(values.GetElement(3).Object).GetElement(1));
        Assert.Equal(byte.MaxValue, Assert.IsType<ScriptUInt8Array>(values.GetElement(4).Object).GetElement(1));
        Assert.Equal(short.MinValue, Assert.IsType<ScriptInt16Array>(values.GetElement(5).Object).GetElement(0));
        Assert.Equal(ushort.MaxValue, Assert.IsType<ScriptUInt16Array>(values.GetElement(6).Object).GetElement(1));
        Assert.Equal(uint.MaxValue, Assert.IsType<ScriptUInt32Array>(values.GetElement(7).Object).GetElement(1));
        Assert.Equal(9007199254740993L, Assert.IsType<ScriptInt64Array>(values.GetElement(8).Object).GetElement(1));
        Assert.Equal(ulong.MaxValue, Assert.IsType<ScriptUInt64Array>(values.GetElement(9).Object).GetElement(2));
    }

    private static void AssertPackedElementError(ScriptDomain domain, string function, double value)
    {
        var error = Assert.ThrowsAny<System.Exception>(() =>
            TestWorkspace.Execute(domain, function, "TEST", ScriptDatum.FromNumber(value)));
        Assert.Contains("$[1]", error.ToString(), System.StringComparison.Ordinal);
    }
}
