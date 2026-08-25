using AuroraScript.Runtime;
using AuroraScript.Tests.Infrastructure;
using System;
using Xunit;

namespace AuroraScript.Tests;

public sealed class ExpressionExecutionTests
{
    [Theory]
    [MemberData(nameof(ExpressionCases))]
    public void EvaluatesOperatorsAndLiteralsInReleaseMode(string expression, object? expected)
    {
        using var workspace = new TestWorkspace();
        var engine = workspace.CreateEngine();
        var block = engine.CompileBlock("return " + expression + ";");

        ScriptAssert.Equal(expected, block.Invoke(Array.Empty<ScriptDatum>()));
    }

    public static TheoryData<string, object?> ExpressionCases => new()
    {
        { "1 + 2 * 3", 7 },
        { "(1 + 2) * 3", 9 },
        { "20 / 4 + 7 % 4", 8 },
        { "-5 + 2", -3 },
        { "~1", -2 },
        { "1 << 5", 32 },
        { "32 >> 2", 8 },
        { "32 >>> 2", 8 },
        { "5 & 3", 1 },
        { "5 | 2", 7 },
        { "5 ^ 1", 4 },
        { "3 < 4", true },
        { "3 <= 3", true },
        { "4 > 3", true },
        { "4 >= 4", true },
        { "4 == 4", true },
        { "4 != 5", true },
        { "null + null", 0 },
        { "1 + null", 1 },
        { "null + 1", 1 },
        { "true && false", false },
        { "false || true", true },
        { "!false", true },
        { "null", null },
        { "'Aurora' + 'Script'", "AuroraScript" },
        { "typeof null", "null" },
        { "typeof 1", "number" },
        { "typeof 'x'", "string" },
        { "typeof true", "boolean" },
        { "typeof []", "array" },
        { "typeof {}", "object" },
        { "typeof new Int8Array(2)", "Int8Array" },
        { "typeof new UInt8Array(2)", "UInt8Array" },
        { "typeof new Int32Array(2)", "Int32Array" },
        { "typeof new StringBuffer('')", "StringBuffer" },
        { "typeof new HashMap()", "HashMap" },
        { "typeof new Path('mem://app')", "Path" },
        { "[1, 2, 3][1]", 2 },
        { "{ value: 9 }.value", 9 },
        { "'value' in { value: 1 }", true },
        { "`sum=${1 + 2}`", "sum=3" },
        { "[1, ...[2, 3], 4]", new object?[] { 1, 2, 3, 4 } }
    };

    [Fact]
    public void EvaluatesAssignmentsAndIncrementOperators()
    {
        using var workspace = new TestWorkspace();
        var engine = workspace.CreateEngine();
        var block = engine.CompileBlock(
            """
            var value = 2;
            var before = value++;
            var after = ++value;
            value += 3;
            value *= 2;
            value -= 4;
            value /= 2;
            value %= 5;
            return [before, after, value];
            """);

        ScriptAssert.Equal(new object?[] { 2, 4, 0 }, block.Invoke(Array.Empty<ScriptDatum>()));
    }

    [Fact]
    public void EvaluatesMemberAndIndexMutationAndDelete()
    {
        using var workspace = new TestWorkspace();
        var engine = workspace.CreateEngine();
        var block = engine.CompileBlock(
            """
            var object = { value: 1 };
            object.value = 4;
            var array = [1, 2];
            array[1] = object.value;
            delete object.value;
            return [array[1], 'value' in object];
            """);

        ScriptAssert.Equal(new object?[] { 4, false }, block.Invoke(Array.Empty<ScriptDatum>()));
    }
}
