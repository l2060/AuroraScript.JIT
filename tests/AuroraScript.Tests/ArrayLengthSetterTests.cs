using AuroraScript.Runtime;
using AuroraScript.Tests.Infrastructure;
using System.Threading.Tasks;
using Xunit;

namespace AuroraScript.Tests;

public sealed class ArrayLengthSetterTests
{
    [Fact]
    public async Task LengthCanShrinkAndGrowWithoutRestoringRemovedValues()
    {
        using var workspace = new TestWorkspace();
        var (_, domain) = await workspace.CompileModuleAsync(
            """
            @module(TEST);
            export func run() {
                var values = [1, 2, 3];
                var assigned = values.length = 0;
                values.length = 2;
                return [assigned, values.length, values[0], values[1]];
            }
            """);

        ScriptAssert.Equal(
            new object?[] { 0D, 2D, null, null },
            TestWorkspace.Execute(domain, "run"));
    }

    [Theory]
    [InlineData("-1")]
    [InlineData("1.5")]
    [InlineData("Number.NaN")]
    [InlineData("Number.POSITIVE_INFINITY")]
    [InlineData("2147483647")]
    public async Task InvalidLengthsAreRejected(string length)
    {
        using var workspace = new TestWorkspace();
        var (_, domain) = await workspace.CompileModuleAsync(
            $$"""
            @module(TEST);
            export func run() {
                var values = [];
                values.length = {{length}};
            }
            """);

        Assert.Throws<AuroraRuntimeException>(() =>
            TestWorkspace.Execute(domain, "run"));
    }

    [Fact]
    public async Task PackedArrayLengthRemainsReadOnly()
    {
        using var workspace = new TestWorkspace();
        var (_, domain) = await workspace.CompileModuleAsync(
            """
            @module(TEST);
            export func run() {
                var values = new Int32Array(1);
                values.length = 0;
            }
            """);

        Assert.Throws<AuroraRuntimeException>(() =>
            TestWorkspace.Execute(domain, "run"));
    }
}
