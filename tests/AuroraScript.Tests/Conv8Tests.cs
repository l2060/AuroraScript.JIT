using AuroraScript.Runtime;
using AuroraScript.Runtime.Types;
using AuroraScript.Tests.Infrastructure;
using System.Threading.Tasks;
using Xunit;

namespace AuroraScript.Tests;

public sealed class Conv8Tests
{
    [Fact]
    public async Task Conv8IsATypeButNotConstructible()
    {
        using var workspace = new TestWorkspace();
        var (_, domain) = await workspace.CompileModuleAsync(
            """
            @module(TEST);
            export func typeName() String {
                return typeof Conv8;
            }
            export func construct() {
                return new Conv8();
            }
            """);

        ScriptAssert.Equal("type", TestWorkspace.Execute(domain, "typeName"));
        Assert.Throws<AuroraRuntimeException>(() =>
            TestWorkspace.Execute(domain, "construct"));
    }

    [Theory]
    [InlineData(CompilationMode.Dynamic)]
    [InlineData(CompilationMode.OnlyRun)]
    public async Task ReadsAndWritesPrimitivesWithEndianness(CompilationMode mode)
    {
        using var workspace = new TestWorkspace();
        var (_, domain) = await workspace.CompileModuleAsync(
            """
            @module(TEST);
            export func run() {
                var bytes = new UInt8Array(32);
                Conv8.setBool(bytes, 0, true);
                Conv8.setInt8(bytes, 1, -8);
                Conv8.setUInt8(bytes, 2, 200);
                Conv8.setInt16(bytes, 3, -300, true);
                Conv8.setUInt16(bytes, 5, 50000, false);
                Conv8.setInt32(bytes, 7, -70000, true);
                Conv8.setUInt32(bytes, 11, 3000000000, false);
                Conv8.setInt64(bytes, 15, -123456789, true);
                Conv8.setFloat32(bytes, 23, 1.5, true);
                Conv8.setFloat64(bytes, 23, 2.25, false);
                return [
                    Conv8.getBool(bytes, 0),
                    Conv8.getInt8(bytes, 1),
                    Conv8.getUInt8(bytes, 2),
                    Conv8.getInt16(bytes, 3, true),
                    Conv8.getUInt16(bytes, 5, false),
                    Conv8.getInt32(bytes, 7, true),
                    Conv8.getUInt32(bytes, 11, false),
                    Conv8.getInt64(bytes, 15, true),
                    Conv8.getFloat64(bytes, 23, false),
                    Conv8.BYTES4
                ];
            }
            """,
            mode);

        ScriptAssert.Equal(
            new object?[] { true, -8, 200, -300, 50000, -70000, 3000000000D, -123456789D, 2.25D, 4D },
            TestWorkspace.Execute(domain, "run"));
    }

    [Fact]
    public async Task WritesLittleAndBigEndianInt32()
    {
        using var workspace = new TestWorkspace();
        var (_, domain) = await workspace.CompileModuleAsync(
            """
            @module(TEST);
            export func run() {
                var bytes = new UInt8Array(4);
                Conv8.setInt32(bytes, 0, 0x01020304, true);
                var little = [bytes[0], bytes[1], bytes[2], bytes[3]];
                Conv8.setInt32(bytes, 0, 0x01020304, false);
                var big = [bytes[0], bytes[1], bytes[2], bytes[3]];
                return [little, big, Conv8.getInt32(bytes, 0, false)];
            }
            """);

        ScriptAssert.Equal(
            new object?[]
            {
                new object?[] { 4, 3, 2, 1 },
                new object?[] { 1, 2, 3, 4 },
                0x01020304
            },
            TestWorkspace.Execute(domain, "run"));
    }

    [Fact]
    public async Task EncodesUtf8StringsAndRejectsShortBuffers()
    {
        using var workspace = new TestWorkspace();
        var (_, domain) = await workspace.CompileModuleAsync(
            """
            @module(TEST);
            export func roundTrip() {
                var bytes = new UInt8Array(16);
                var written = Conv8.setString(bytes, 2, "hi");
                return [written, Conv8.getString(bytes, 2, written)];
            }
            export func overflow() {
                var bytes = new UInt8Array(2);
                return Conv8.setString(bytes, 0, "hello");
            }
            """);

        ScriptAssert.Equal(new object?[] { 2, "hi" }, TestWorkspace.Execute(domain, "roundTrip"));
        Assert.Throws<AuroraRuntimeException>(() =>
            TestWorkspace.Execute(domain, "overflow"));
    }

    [Fact]
    public async Task RejectsNonUInt8PackedArrays()
    {
        using var workspace = new TestWorkspace();
        var (_, domain) = await workspace.CompileModuleAsync(
            """
            @module(TEST);
            export func int32() {
                var values = new Int32Array(4);
                return Conv8.getInt32(values, 0);
            }
            export func int8() {
                var values = new Int8Array(4);
                return Conv8.getInt32(values, 0);
            }
            """);

        Assert.Throws<AuroraRuntimeException>(() => TestWorkspace.Execute(domain, "int32"));
        Assert.Throws<AuroraRuntimeException>(() => TestWorkspace.Execute(domain, "int8"));
    }
}
