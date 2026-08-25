using AuroraScript.Runtime;
using AuroraScript.Runtime.Types;
using AuroraScript.Tests.Infrastructure;
using System.Threading.Tasks;
using Xunit;

namespace AuroraScript.Tests;

public sealed class TypeCheckTests
{
    [Theory]
    [InlineData(CompilationMode.Dynamic)]
#if NET9_0_OR_GREATER
    [InlineData(CompilationMode.Persistence)]
#endif
    public async Task CheckExpressionsAndParametersAssertExactTypes(
        CompilationMode mode)
    {
        using var workspace = new TestWorkspace();
        var (_, domain) = await workspace.CompileModuleAsync(
            """
            @module(TEST);
            export func add(Number a, Number b) {
                return a + b;
            }
            export func increment(value) {
                var number = value as Number;
                return number + 1;
            }
            export func first(value) {
                var values = value as Float64Array;
                values[0] = values[0] + 1;
                return values[0];
            }
            export func identity(Number) {
                return Number;
            }
            """,
            mode);

        ScriptAssert.Equal(
            5,
            TestWorkspace.Execute(
                domain,
                "add",
                arguments: [
                    ScriptDatum.FromNumber(2),
                    ScriptDatum.FromNumber(3)]));
        ScriptAssert.Equal(
            8,
            TestWorkspace.Execute(
                domain,
                "increment",
                arguments: [ScriptDatum.FromNumber(7)]));
        ScriptAssert.Equal(
            "not a type declaration",
            TestWorkspace.Execute(
                domain,
                "identity",
                arguments: [ScriptDatum.FromString("not a type declaration")]));

        var values = new ScriptFloat64Array(1);
        values._items[0] = 4;
        ScriptAssert.Equal(
            5,
            TestWorkspace.Execute(
                domain,
                "first",
                arguments: [ScriptDatum.FromObject(values)]));

        Assert.Throws<AuroraRuntimeException>(() =>
            TestWorkspace.Execute(
                domain,
                "add",
                arguments: [
                    ScriptDatum.FromString("2"),
                    ScriptDatum.FromNumber(3)]));
        Assert.Throws<AuroraRuntimeException>(() =>
            TestWorkspace.Execute(
                domain,
                "first",
                arguments: [ScriptDatum.FromObject(new ScriptInt32Array(1))]));
    }

    [Fact]
    public async Task CheckRejectsUnsupportedTypeNamesAtCompileTime()
    {
        using var workspace = new TestWorkspace();
        await Assert.ThrowsAsync<AuroraCompilationException>(() =>
            workspace.CompileModuleAsync(
                """
                @module(TEST);
                export func run(value) {
                    return value as MissingType;
                }
                """));
    }
}
