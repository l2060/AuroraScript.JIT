using AuroraScript.Runtime;
using AuroraScript.Runtime.Types;
using AuroraScript.Tests.Infrastructure;
using System;
using System.Threading.Tasks;
using Xunit;

namespace AuroraScript.Tests;

public sealed class SpreadOptimizationTests
{
    [Theory]
    [InlineData(0, false)]
    [InlineData(1, false)]
    [InlineData(128, false)]
    [InlineData(128, true)]
    public void ArraySpreadKeepsPrefixAndReferenceIdentity(int count, bool reserve)
    {
        var marker = new ScriptObject();
        var source = new ScriptArray(count);
        for (var i = 0; i < count; i++) source[i] = i % 2 == 0 ? ScriptDatum.FromObject(marker) : ScriptDatum.Null;
        var target = reserve ? ScriptArray.CreateEmptyWithCapacity(count + 1) : new ScriptArray();
        target.Push(42);
        ObjectOps.SpreadInto(target, ScriptDatum.FromArray(source));
        Assert.Equal(count + 1, target.Length);
        Assert.Equal(42, target[0].Number);
        for (var i = 0; i < count; i++)
        {
            Assert.Equal(source[i].Kind, target[i + 1].Kind);
            Assert.Same(source[i].Reference, target[i + 1].Reference);
        }
        Assert.Equal(count, source.Length);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void SelfSpreadUsesTheOriginalLength(bool reserve)
    {
        var target = reserve ? ScriptArray.CreateEmptyWithCapacity(16) : new ScriptArray();
        var marker = ScriptDatum.FromObject(new ScriptObject());
        target.Push(1);
        target.Push(marker);
        target.Push(ScriptDatum.Null);
        ObjectOps.SpreadInto(target, ScriptDatum.FromArray(target));
        Assert.Equal(6, target.Length);
        Assert.Equal(1, target[3].Number);
        Assert.Same(marker.Reference, target[4].Reference);
        Assert.Equal(ValueKind.Null, target[5].Kind);
        var empty = new ScriptArray();
        ObjectOps.SpreadInto(empty, ScriptDatum.FromArray(empty));
        Assert.Equal(0, empty.Length);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void ArgumentSpreadPreservesCountPrefixAndContentsAcrossGrowth(bool grow)
    {
        var arguments = CallOps.RentArguments(4);
        var count = 0;
        try
        {
            arguments = CallOps.AppendArgument(arguments, ref count, 42);
            var marker = ScriptDatum.FromObject(new ScriptObject());
            arguments = CallOps.AppendArgument(arguments, ref count, marker);
            var length = grow ? arguments.Length + 3 : 2;
            var source = new ScriptArray(length);
            for (var i = 0; i < length; i++) source[i] = i == 0 ? marker : ScriptDatum.FromNumber(i);
            arguments = CallOps.AppendSpread(arguments, ref count, ScriptDatum.FromArray(source));
            Assert.Equal(length + 2, count);
            Assert.Equal(42, arguments[0].Number);
            Assert.Same(marker.Reference, arguments[1].Reference);
            Assert.Same(marker.Reference, arguments[2].Reference);
            for (var i = 1; i < length; i++) Assert.Equal(i, arguments[i + 2].Number);
            arguments = CallOps.AppendSpread(arguments, ref count, ScriptDatum.FromArray(new ScriptArray()));
            Assert.Equal(length + 2, count);
            arguments = CallOps.AppendSpread(arguments, ref count, ScriptDatum.Null);
            Assert.Equal(length + 3, count);
            Assert.Equal(ValueKind.Null, arguments[count - 1].Kind);
        }
        finally { CallOps.ReturnArguments(arguments, count); }
    }

    [Theory]
    [InlineData(CompilationMode.Dynamic)]
    [InlineData(CompilationMode.OnlyRun)]
#if NET9_0_OR_GREATER
    [InlineData(CompilationMode.Persistence)]
#endif
    public async Task CompiledSpreadsKeepEvaluationOrderAndDynamicCalls(CompilationMode mode)
    {
        using var workspace = new TestWorkspace();
        var (_, domain) = await workspace.CompileModuleAsync("""
            @module(TEST);
            var initial = [0, ...[1, 2], 3];
            func sum(a, b, c, d, e, f, g, h) { return a + b + c + d + e + f + g + h; }
            export native func copy(Array values) Array { return [...values]; }
            export func run() {
                var values = [1, 2];
                func mutate() { values[0] = 9; return 3; }
                var result = [...values, mutate(), ...values];
                var shared = {value: 7};
                var original = [shared, null];
                var copied = [...original];
                copied[0].value = 8;
                var packed = new Int32Array(2);
                packed[0] = 6;
                packed[1] = 7;
                var fn = sum;
                return [initial, result, original[0].value, [...packed],
                    fn(...[1, 2, 3, 4], ...[5, 6, 7, 8]), copy(values)];
            }
            """, mode);
        ScriptAssert.Equal(new object[] { new object[] { 0, 1, 2, 3 }, new object[] { 1, 2, 3, 9, 2 },
            8, new object[] { 6, 7 }, 36, new object[] { 9, 2 } }, TestWorkspace.Execute(domain, "run"));
    }
}
