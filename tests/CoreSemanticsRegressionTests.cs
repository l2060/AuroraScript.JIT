using AuroraScript.Runtime;
using AuroraScript.Tests.Infrastructure;
using System;
using Xunit;

namespace AuroraScript.Tests;

public sealed class CoreSemanticsRegressionTests
{
    [Fact]
    public void NumericOperatorsCoerceBooleansAndNumericStringsConsistently()
    {
        using var workspace = new TestWorkspace();
        var engine = workspace.CreateEngine();
        var block = engine.CompileBlock(
            """
            return [
                true + 2,
                false + 2,
                true * 3,
                '8' - '3',
                '8' / '2',
                '7' % '4',
                '6' + 2,
                6 + '2',
                'x' + true
            ];
            """);

        ScriptAssert.Equal(
            new object?[] { 3, 2, 3, 5, 4, 3, "62", "62", "xTrue" },
            block.Invoke(Array.Empty<ScriptDatum>()));
    }

    [Fact]
    public void NullAdditionUsesNumericCoercionUnlessAStringParticipates()
    {
        using var workspace = new TestWorkspace();
        var engine = workspace.CreateEngine();
        var block = engine.CompileBlock(
            """
            return [
                null + null,
                1 + null,
                null + 1,
                true + null,
                null + true,
                (null + null) + 1,
                'x' + null,
                null + 'x',
                `${null + null}`
            ];
            """);

        ScriptAssert.Equal(
            new object?[] { 0, 1, 1, 1, 1, 1, "xnull", "nullx", "0" },
            block.Invoke(Array.Empty<ScriptDatum>()));
    }

    [Fact]
    public void EqualityAndComparisonCoercionsStayStable()
    {
        using var workspace = new TestWorkspace();
        var engine = workspace.CreateEngine();
        var block = engine.CompileBlock(
            """
            return [
                1 == true,
                0 == false,
                '2' == 2,
                '2' != 3,
                '10' > 2,
                '2' <= 2,
                null == null,
                null != 0,
                'abc' == 'abc',
                'abc' != 'def'
            ];
            """);

        ScriptAssert.Equal(
            new object?[] { true, true, true, true, true, true, true, true, true, true },
            block.Invoke(Array.Empty<ScriptDatum>()));
    }

    [Fact]
    public void LogicalOperatorsShortCircuitAndReturnTheSelectedOperand()
    {
        using var workspace = new TestWorkspace();
        var engine = workspace.CreateEngine();
        var block = engine.CompileBlock(
            """
            var count = 0;
            func hit(value) {
                count++;
                return value;
            }
            return [
                null || 'fallback',
                'left' || hit('right'),
                'left' && 7,
                0 && hit(9),
                count
            ];
            """);

        ScriptAssert.Equal(
            new object?[] { "fallback", "left", 7, 0, 0 },
            block.Invoke(Array.Empty<ScriptDatum>()));
    }

    [Fact]
    public void NewArrayLengthSlotsAreNullAndDoNotTurnNumericSummationIntoStringConcatenation()
    {
        using var workspace = new TestWorkspace();
        var engine = workspace.CreateEngine();
        var block = engine.CompileBlock(
            """
            var array = new Array(3);
            var sum = 0;
            for (var i = 0; i < array.length; i++) {
                sum = sum + array[i];
            }
            array.push(4);
            return [array.length, array[0], sum, typeof sum, array[3]];
            """);

        ScriptAssert.Equal(
            new object?[] { 4, null, 0, "number", 4 },
            block.Invoke(Array.Empty<ScriptDatum>()));
    }

    [Fact]
    public void ArrayIndexingMutationDeletionAndExpansionKeepLogicalLengthStable()
    {
        using var workspace = new TestWorkspace();
        var engine = workspace.CreateEngine();
        var block = engine.CompileBlock(
            """
            var array = [1, 2, 3];
            array[-1] = 9;
            array[5] = 7;
            delete array[1];
            return [array.length, array[0], array[1], array[2], array[3], array[4], array[5], array[-1]];
            """);

        ScriptAssert.Equal(
            new object?[] { 6, 1, null, 9, null, null, 7, 7 },
            block.Invoke(Array.Empty<ScriptDatum>()));
    }

    [Fact]
    public void ArrayWithCapacityReservesStorageWithoutChangingLength()
    {
        using var workspace = new TestWorkspace();
        var engine = workspace.CreateEngine();
        var block = engine.CompileBlock(
            """
            var array = Array.withCapacity(32);
            var initialLength = array.length;
            var pushResult = array.push(1, 2, 3, 4);
            var sum = 0;
            for (var i = 0; i < array.length; i++) {
                sum = sum + array[i];
            }
            return [initialLength, pushResult, array.length, sum, typeof sum, array[4]];
            """);

        ScriptAssert.Equal(
            new object?[] { 0, 4, 4, 10, "number", null },
            block.Invoke(Array.Empty<ScriptDatum>()));
    }

    [Fact]
    public void ObjectPropertiesSupportDotBracketInAndDelete()
    {
        using var workspace = new TestWorkspace();
        var engine = workspace.CreateEngine();
        var block = engine.CompileBlock(
            """
            var object = { name: 'Aurora' };
            object.count = 1;
            object['count'] += 2;
            var beforeDelete = 'count' in object;
            delete object.count;
            return [object.name, beforeDelete, 'count' in object, object.count, object['missing']];
            """);

        ScriptAssert.Equal(
            new object?[] { "Aurora", true, false, null, null },
            block.Invoke(Array.Empty<ScriptDatum>()));
    }

    [Fact]
    public void FunctionScopeDefaultsLoopsAndClosuresWorkTogether()
    {
        using var workspace = new TestWorkspace();
        var engine = workspace.CreateEngine();
        var block = engine.CompileBlock(
            """
            func makeAdder(base, step = 1) {
                var current = base;
                return () => {
                    current = current + step;
                    return current;
                };
            }
            var addTwo = makeAdder(10, 2);
            var total = 0;
            for (var i = 0; i < 5; i++) {
                if (i == 1) continue;
                if (i == 4) break;
                total = total + i;
            }
            return [addTwo(), addTwo(), total];
            """);

        ScriptAssert.Equal(
            new object?[] { 12, 14, 5 },
            block.Invoke(Array.Empty<ScriptDatum>()));
    }
}
