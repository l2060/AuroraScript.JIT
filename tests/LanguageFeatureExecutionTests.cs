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

    [Fact]
    public async Task ModuleConstInliningPreservesRuntimeResultsWhenEnabled()
    {
        using var workspace = new TestWorkspace();
        var engine = workspace.CreateEngine(enableModuleConstInlining: true);
        await engine.BuildAsync(engine.MemorySource(
            "main.as",
            """
            @module(TEST);
            export const a1 = 1;
            export const a5 = a1 + 4;
            export const boolNumber = true + 1;
            export const nullNumber = null + 1;
            export const NUM = 3.141592678987654321;
            export const STR = 'this is string';
            export const BOOL = true;
            export const BASE = 10;
            export const COMPLEX = BASE * NUM + 5;
            export const TAG = BASE + '_' + 1;
            export const TEMPLATE = STR + BASE + '_' + TAG;
            func make() { return 9; }
            export const fv = make();
            export func run() {
                return [a5, boolNumber, nullNumber, fv, NUM, STR, BOOL, COMPLEX, TAG, TEMPLATE];
            }
            """));
        var domain = engine.CreateDomain();
        var expectedNum = 3.141592678987654321d;

        ScriptAssert.Equal(
            new object?[]
            {
                5,
                2,
                1,
                9,
                expectedNum,
                "this is string",
                true,
                10 * expectedNum + 5,
                "10_1",
                "this is string10_10_1"
            },
            TestWorkspace.Execute(domain, "run"));
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
    public async Task TemplateStringsUseConcatAndBuilderPaths()
    {
        using var workspace = new TestWorkspace();
        var (_, domain) = await workspace.CompileModuleAsync(
            """
            @module(TEST);
            export func run() {
                var name = 'Aurora';
                var small = `${name}:${1 + 2}`;
                var large = `a${1}b${2}c${3}d${4}e`;
                return [small, large];
            }
            """);

        ScriptAssert.Equal(new object?[] { "Aurora:3", "a1b2c3d4e" }, TestWorkspace.Execute(domain, "run"));
    }

    [Fact]
    public async Task TemplateStringsEvaluatePartsLeftToRight()
    {
        using var workspace = new TestWorkspace();
        var (_, domain) = await workspace.CompileModuleAsync(
            """
            @module(TEST);
            export func run() {
                var index = 0;
                func next() {
                    index = index + 1;
                    return index;
                }

                return [`${next()}-${next()}-${next()}-${next()}`, index];
            }
            """);

        ScriptAssert.Equal(new object?[] { "1-2-3-4", 4 }, TestWorkspace.Execute(domain, "run"));
    }

    [Fact]
    public async Task TemplateStringsWorkInModuleInitializers()
    {
        using var workspace = new TestWorkspace();
        var (_, domain) = await workspace.CompileModuleAsync(
            """
            @module(TEST);
            var base = 'module';
            var small = `${base}:${21 + 21}`;
            var large = `a${1}b${2}c${3}d${4}e`;
            export func run() { return [small, large]; }
            """);

        ScriptAssert.Equal(new object?[] { "module:42", "a1b2c3d4e" }, TestWorkspace.Execute(domain, "run"));
    }

    [Fact]
    public async Task ModuleConstTemplateStringsCanBeInlined()
    {
        using var workspace = new TestWorkspace();
        var (_, domain) = await workspace.CompileModuleAsync(
            """
            @module(TEST);
            const prefix = 'const';
            export const value = `${prefix}:${20 + 22}`;
            export func run() { return value; }
            """);

        ScriptAssert.Equal("const:42", TestWorkspace.Execute(domain, "run"));
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
