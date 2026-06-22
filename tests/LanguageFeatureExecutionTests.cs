using AuroraScript.Runtime;
using AuroraScript.Tests.Infrastructure;
using System;
using System.Threading.Tasks;
using Xunit;

namespace AuroraScript.Tests;

public sealed class LanguageFeatureExecutionTests
{
    [Fact]
    public async Task EnumValuesSupportExplicitAndImplicitNumbering()
    {
        using var workspace = new TestWorkspace();
        var (_, domain) = await workspace.CompileModuleAsync(
            """
            @module(TEST);
            enum Color { Red, Green = 4, Blue }
            export func run() { return [Color.Red, Color.Green, Color.Blue]; }
            """);

        ScriptAssert.Equal(new object?[] { 0, 4, 5 }, TestWorkspace.Execute(domain, "run"));
    }

    [Fact]
    public async Task ExpressionAndBlockLambdasCaptureArguments()
    {
        using var workspace = new TestWorkspace();
        var (_, domain) = await workspace.CompileModuleAsync(
            """
            @module(TEST);
            export func run() {
                var offset = 2;
                var expression = (left, right) => left + right + offset;
                var block = (value) => { return value * 2; };
                return [expression(10, 20), block(5)];
            }
            """);

        ScriptAssert.Equal(new object?[] { 32, 10 }, TestWorkspace.Execute(domain, "run"));
    }

    [Fact]
    public async Task SparseArrayObjectShorthandAndObjectSpreadProduceExpectedShape()
    {
        using var workspace = new TestWorkspace();
        var (_, domain) = await workspace.CompileModuleAsync(
            """
            @module(TEST);
            export func run() {
                var name = 'Aurora';
                var sparse = [1, , 3];
                var object = { name, first: 1, ...{ second: 2 } };
                return [sparse.length, sparse[1], object.name, object.first, object.second];
            }
            """);

        ScriptAssert.Equal(new object?[] { 3, null, "Aurora", 1, 2 }, TestWorkspace.Execute(domain, "run"));
    }

    [Fact]
    public async Task LogicalOperatorsShortCircuitAndPreserveSideEffects()
    {
        using var workspace = new TestWorkspace();
        var (_, domain) = await workspace.CompileModuleAsync(
            """
            @module(TEST);
            export func run() {
                var count = 0;
                func increment() { count++; return true; }
                false && increment();
                true || increment();
                true && increment();
                false || increment();
                return count;
            }
            """);

        ScriptAssert.Equal(2, TestWorkspace.Execute(domain, "run"));
    }

    [Theory]
    [InlineData("null", false)]
    [InlineData("false", false)]
    [InlineData("0", false)]
    [InlineData("''", false)]
    [InlineData("1", true)]
    [InlineData("'x'", true)]
    [InlineData("[]", true)]
    [InlineData("{}", true)]
    public void ConditionalTruthinessMatchesRuntimeRules(string expression, bool expected)
    {
        using var workspace = new TestWorkspace();
        var engine = workspace.CreateEngine();
        var block = engine.CompileBlock($"if ({expression}) return true; else return false;");

        ScriptAssert.Equal(expected, block.Invoke(Array.Empty<ScriptDatum>()));
    }

    [Fact]
    public async Task DeclaredHostFunctionResolvesFromConfiguredGlobal()
    {
        using var workspace = new TestWorkspace();
        var (_, domain) = await workspace.CompileModuleAsync(
            """
            @module(TEST);
            export declare func HOST_ADD(left, right);
            export func run() { return HOST_ADD(20, 22); }
            """,
            configureGlobal: global => global.Define(
                "HOST_ADD",
                (Func<int, int, int>)((left, right) => left + right)));

        ScriptAssert.Equal(42, TestWorkspace.Execute(domain, "run"));
    }

    [Fact]
    public async Task NestedTemplatesHandleExpressionsBracesAndEscapedText()
    {
        using var workspace = new TestWorkspace();
        var (_, domain) = await workspace.CompileModuleAsync(
            """
            @module(TEST);
            export func run() {
                var object = { value: 40 };
                return `outer={${`inner=${object.value + 2}`}}`;
            }
            """);

        ScriptAssert.Equal("outer={inner=42}", TestWorkspace.Execute(domain, "run"));
    }

    [Fact]
    public async Task AssignmentIsRightAssociativeAndCompoundAssignmentEvaluatesTargetOnce()
    {
        using var workspace = new TestWorkspace();
        var (_, domain) = await workspace.CompileModuleAsync(
            """
            @module(TEST);
            export func run() {
                var a = 0;
                var b = 0;
                a = b = 4;
                var index = 0;
                var values = [1];
                values[index++] += 2;
                return [a, b, values[0], index];
            }
            """);

        ScriptAssert.Equal(new object?[] { 4, 4, 3, 1 }, TestWorkspace.Execute(domain, "run"));
    }

    [Fact]
    public async Task FunctionWithoutReturnAndBareReturnProduceNull()
    {
        using var workspace = new TestWorkspace();
        var (_, domain) = await workspace.CompileModuleAsync(
            """
            @module(TEST);
            func implicitNull() { var value = 1; }
            func explicitNull() { return; }
            export func run() { return [implicitNull(), explicitNull()]; }
            """);

        ScriptAssert.Equal(new object?[] { null, null }, TestWorkspace.Execute(domain, "run"));
    }
}
