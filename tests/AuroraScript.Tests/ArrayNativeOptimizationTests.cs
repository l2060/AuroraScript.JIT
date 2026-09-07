using AuroraScript.Compiler.Backend;
using AuroraScript.Hosting;
using AuroraScript.Runtime;
using AuroraScript.Runtime.Types;
using AuroraScript.Tests.Infrastructure;
using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Xunit;

namespace AuroraScript.Tests;

public sealed class ArrayNativeOptimizationTests
{
    [Fact]
    public void NativeTypeRegistrationKeepsArrayStorageAndAbiSpecialized()
    {
        var catalog = new HostExportCatalog([]);
        Assert.True(catalog.TryGetNativeObject("Array", out var owner));
        Assert.Null(owner.Constructor);
        Assert.True(owner.TryGetGetter("length", out var getter));
        Assert.Equal(AuroraExportValueKind.Int32, getter.ReturnKind);
        Assert.True(catalog.TryGetGlobal("Array", "withCapacity", out var factory));
        Assert.Equal(typeof(ScriptDatum), factory.GetScriptParameterType(0));
        Assert.Same(Prototypes.ScriptArrayPrototype, new ScriptArray().Prototype);
        Assert.True(ScriptArray.Type.IsFrozen);
    }

    [Theory]
    [InlineData(CompilationMode.Dynamic)]
    [InlineData(CompilationMode.OnlyRun)]
#if NET9_0_OR_GREATER
    [InlineData(CompilationMode.Persistence)]
#endif
    public async Task NativeArrayPathsPreserveCapacityOverridesEvaluationAndAbi(CompilationMode mode)
    {
        using var workspace = new TestWorkspace();
        var (_, domain) = await workspace.CompileModuleAsync("""
            @module(TEST);
            export native func capacity(int32 value) Array { return Array.withCapacity(value); }
            export native func numberCapacity(Number value) Array { return Array.withCapacity(value); }
            export func generic(value) { return Array.withCapacity(value); }
            export func dynamic(value) { var type=Array; return type.withCapacity(value); }
            export native func length(Array value) int32 { return value.length; }
            export native func resize(Array value, int32 count) int32 { value.length=count; return value.length; }
            export func staticMembers() {
                return [Array.from([1,2], x=>x+1), Array.from(), Array.of(3,4), Array.of().length,
                    Array.isArray([]), Array.isArray(1), Array.isArray(),
                    (new Array(8)).length, Array.withCapacity(8).length, (new Array(8,9)).length];
            }
            export func order() {
                var log='';
                func capacity() { log+='c'; return 8; }
                func extra() { log+='e'; return 1; }
                var value=Array.withCapacity(capacity(),extra());
                return [log,value.length];
            }
            export func overrides() {
                var value=[];
                func argument() { value.push=x=>'own'; return 1; }
                return [value.push(argument()), value.length];
            }
            export func shadow() {
                var Array={withCapacity: x=>x+1};
                return Array.withCapacity(3);
            }
            export func aliases() {
                var type=Array;
                return [(new type(8)).length, type.withCapacity(...[8]).length];
            }
            export func forbidden() { return Array(8); }
            """, mode);
        ScriptAssert.Equal(new object?[] { new object[] { 2,3 }, null, new object[] { 3,4 }, 0, true, false, false, 8, 0, 0 }, TestWorkspace.Execute(domain, "staticMembers"));
        ScriptAssert.Equal(new object[] { "ce", 0 }, TestWorkspace.Execute(domain, "order"));
        ScriptAssert.Equal(new object[] { "own", 0 }, TestWorkspace.Execute(domain, "overrides"));
        ScriptAssert.Equal(4, TestWorkspace.Execute(domain, "shadow"));
        ScriptAssert.Equal(new object[] { 8, 0 }, TestWorkspace.Execute(domain, "aliases"));
        Assert.Throws<AuroraRuntimeException>(() => TestWorkspace.Execute(domain, "forbidden"));
        foreach (var value in new[] { ScriptDatum.FromNumber(8), ScriptDatum.FromNumber(0), ScriptDatum.FromNumber(-2),
            ScriptDatum.FromNumber(2.75), ScriptDatum.FromNumber(double.NaN),
            ScriptDatum.FromString("8"), ScriptDatum.FromString("bad"), ScriptDatum.FromBoolean(true),
            ScriptDatum.FromBoolean(false), ScriptDatum.FromInt64(8), ScriptDatum.FromUInt64(8), default(ScriptDatum) })
        {
            var generic = Assert.IsType<ScriptArray>(TestWorkspace.Execute(domain, "generic", arguments: [value]).Object);
            var dynamic = Assert.IsType<ScriptArray>(TestWorkspace.Execute(domain, "dynamic", arguments: [value]).Object);
            Assert.Equal(0, generic.Length);
            Assert.Equal(generic.Length, dynamic.Length);
            Assert.Equal(generic._items.Length, dynamic._items.Length);
        }
        foreach (var method in new[] { "generic", "dynamic", "numberCapacity" })
            Assert.Throws<AuroraRuntimeException>(() => TestWorkspace.Execute(domain, method,
                arguments: [ScriptDatum.FromNumber(double.PositiveInfinity)]));
        foreach (var value in new[] { -2, 0, 1, 8 })
        {
            var array = Assert.IsType<ScriptArray>(TestWorkspace.Execute(domain, "capacity", arguments: [ScriptDatum.FromNumber(value)]).Object);
            Assert.Equal(0, array.Length);
            Assert.Equal(value <= 0 ? 0 : Math.Max(4, value), array._items.Length);
            var argument = ScriptDatum.FromArray(array);
            ScriptAssert.Equal(0, TestWorkspace.Execute(domain, "length", arguments: [argument]));
            ScriptAssert.Equal(3, TestWorkspace.Execute(domain, "resize", arguments: [argument, ScriptDatum.FromNumber(3)]));
            Assert.Equal(3, array.Length);
        }
#if NET9_0_OR_GREATER
        if (mode == CompilationMode.Persistence)
        {
            var methods = Assembly.Load(File.ReadAllBytes(Path.Combine(workspace.Root, "test-output.dll")))
                .GetTypes().SelectMany(type => type.GetMethods()).ToArray();
            var length = methods.Single(method => method.Name == "length$native");
            var lengthCalls = StringOptimizationTests.GetCalls(length);
            Assert.Contains(lengthCalls, method => method.DeclaringType == typeof(ScriptArray) && method.Name == "get_Length");
            Assert.DoesNotContain(lengthCalls, method => method.DeclaringType == typeof(ScriptDatum));
            var capacity = methods.Single(method => method.Name == "capacity$native");
            Assert.Equal(typeof(ScriptDatum), capacity.ReturnType);
            var capacityCalls = StringOptimizationTests.GetCalls(capacity);
            Assert.Contains(capacityCalls, method => method.DeclaringType == typeof(ScriptArray) &&
                method.Name == nameof(ScriptArray.CreateEmptyWithCapacity) && method.GetParameters()[0].ParameterType == typeof(int));
            Assert.DoesNotContain(capacityCalls, method => method.DeclaringType == typeof(ScriptDatum) && method.Name == nameof(ScriptDatum.FromNumber));
            var numberCalls = StringOptimizationTests.GetCalls(methods.Single(method => method.Name == "numberCapacity$native"));
            Assert.Contains(numberCalls, method => method.DeclaringType == typeof(ScriptArray) &&
                method.Name == nameof(ScriptArray.CreateEmptyWithCapacity) && method.GetParameters()[0].ParameterType == typeof(double));
            Assert.DoesNotContain(numberCalls, method => method.DeclaringType == typeof(ScriptDatum) && method.Name == nameof(ScriptDatum.FromNumber));
        }
#endif
    }
}
