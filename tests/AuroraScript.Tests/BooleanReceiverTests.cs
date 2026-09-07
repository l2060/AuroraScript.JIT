using AuroraScript.Compiler.Backend;
using AuroraScript.Compiler.Backend.Code;
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

public sealed class BooleanReceiverTests
{
    [Fact]
    public void ReceiverConfigurationIsInternalAndBooleanUsesPrimitiveMetadata()
    {
        Assert.False(typeof(ReceiverExportAttribute).IsPublic);
        Assert.False(typeof(NativeReceiverAttribute).IsPublic);
        Assert.Null(typeof(NativeTypeAttribute).GetProperty("NativeReceiverType"));
        Assert.Null(typeof(NativeTypeAttribute).GetProperty("NativeConstructor"));
        Assert.True(typeof(NativeTypeAttribute).IsPublic);
        Assert.True(typeof(ExportAttribute).IsPublic);

        var catalog = new HostExportCatalog([]);
        Assert.True(catalog.TryGetNativeValue(FlowValueType.Boolean, out var owner));
        Assert.Equal(typeof(bool), owner.ClrType);
        Assert.Equal(typeof(BooleanValue), owner.DeclaringType);
        Assert.False(catalog.TryGetNativeObject("Boolean", out _));
        Assert.True(owner.TryGetMethod("toString", out var method));
        Assert.Equal(nameof(BooleanValue.FormatString), method.Method.Name);
        Assert.Equal(typeof(bool), method.ReceiverType);
        Assert.Equal(0, method.GetReceiverCost(FlowValueType.Boolean));
        Assert.Equal(-1, method.GetReceiverCost(FlowValueType.Number));
        Assert.True(BooleanValue.True.Prototype.IsFrozen);
        Assert.Same(BooleanValue.True.Prototype, BooleanValue.False.Prototype);
        Assert.Same(Prototypes.ObjectPrototype, BooleanValue.True.Prototype.Prototype);
        Assert.Throws<ArgumentException>(() => EngineOptions.Default.WithCompiler(c => c.AddNativeType<BooleanValue>()));
        Assert.True(catalog.TryGetValueFactory("Boolean", out var factory));
        Assert.Equal(nameof(BooleanValue.CreateCore), factory.Method.Name);
        Assert.Equal(typeof(bool), factory.Method.ReturnType);
        Assert.Equal(0, factory.RequiredScriptParameterCount);
        Assert.True(catalog.TryGetConstant("Boolean", "true", out var trueField));
        Assert.Equal(typeof(bool), trueField.FieldType);
        Assert.True(catalog.TryGetConstant("Boolean", "false", out var falseField));
        Assert.Equal(typeof(bool), falseField.FieldType);
        Assert.True(BooleanValue.Type.IsFrozen);
        Assert.Equal("Boolean", BooleanValue.Type.Name);
        Assert.Null(typeof(BooleanValue).Assembly.GetType("AuroraScript.Runtime.Types.TypeConstruct.BooleanConstructor"));
    }

    [Fact]
    public void DynamicAdapterPreservesInvalidReceiverBehavior()
    {
        var result = default(ScriptDatum);
        BooleanValue.TOSTRING(null!, ScriptObject.Null, Span<ScriptDatum>.Empty, ref result);
        ScriptAssert.Equal("false", result);
        BooleanValue.TOSTRING(null!, BooleanValue.True, Span<ScriptDatum>.Empty, ref result);
        ScriptAssert.Equal("true", result);
        Assert.Same("true", BooleanValue.FormatString(true));
        Assert.Same("false", BooleanValue.FormatString(false));
    }

    [Theory]
    [InlineData(CompilationMode.Dynamic)]
    [InlineData(CompilationMode.OnlyRun)]
#if NET9_0_OR_GREATER
    [InlineData(CompilationMode.Persistence)]
#endif
    public async Task BooleanMembersKeepTheirBehaviorAndUseUnboxedCore(CompilationMode mode)
    {
        using var workspace = new TestWorkspace();
        var (_, domain) = await workspace.CompileModuleAsync("""
            @module(TEST);
            export native func text(Boolean value) String { return value.toString(); }
            export native func create(Boolean value) Boolean { return Boolean(value); }
            export native func construct(Boolean value) Boolean { return new Boolean(value); }
            export native func valueOf(Boolean value) Boolean { return Boolean.valueOf(value); }
            export native func empty() Boolean { return Boolean(); }
            export func conversions(value) { return [Boolean(value), new Boolean(value), Boolean.valueOf(value)]; }
            export func alias(type, value) { return [type(value), new type(value), type.valueOf(value)]; }
            export func defaults() { return [Boolean(), new Boolean(), Boolean.valueOf(), Boolean(...[])]; }
            export func spreads() { return [Boolean(...[1, 0]), new Boolean(...[0, 1]), Boolean.valueOf(...['x'])]; }
            export func order() {
                var log = '';
                func value() { log += 'v'; return true; }
                func extra() { log += 'e'; return false; }
                var a = Boolean(value(), extra());
                var b = new Boolean(value(), extra());
                var c = Boolean.valueOf(value(), extra());
                return [a, b, c, log];
            }
            export func constants() {
                var changed = false;
                try { Boolean['true'] = false; } catch (error) { changed = true; }
                return [Boolean['true'], Boolean['false'], typeof Boolean['true'], Object.keys(Boolean).length, changed];
            }
            export func dynamicText(value) { return value.toString(); }
            export func run() {
                var calls = 0;
                func tick() { calls++; return 1; }
                var extra = true.toString(tick());
                return [true.toString(), false.toString(), Boolean(1), new Boolean(0),
                    Boolean.valueOf('x'), typeof true, extra, calls,
                    Boolean['true'], Boolean['false']];
            }
            """, mode);
        ScriptAssert.Equal(new object[] { "true", "false", true, false, true, "boolean", "true", 1, true, false },
            TestWorkspace.Execute(domain, "run"));
        ScriptAssert.Equal(new object[] { false, false, false, false }, TestWorkspace.Execute(domain, "defaults"));
        ScriptAssert.Equal(new object[] { true, false, true }, TestWorkspace.Execute(domain, "spreads"));
        ScriptAssert.Equal(new object[] { true, true, true, "veveve" }, TestWorkspace.Execute(domain, "order"));
        ScriptAssert.Equal(new object[] { true, false, "boolean", 0, true }, TestWorkspace.Execute(domain, "constants"));
        foreach (var input in new[] { default(ScriptDatum), ScriptDatum.FromBoolean(false), ScriptDatum.FromBoolean(true),
            ScriptDatum.FromNumber(0), ScriptDatum.FromNumber(1), ScriptDatum.FromNumber(double.NaN),
            ScriptDatum.FromString(""), ScriptDatum.FromString("false"), ScriptDatum.FromInt64(0),
            ScriptDatum.FromUInt64(1), ScriptDatum.FromObject(new ScriptObject()) })
        {
            var expected = ScriptDatum.IsTrue(input);
            var expectedValues = new object[] { expected, expected, expected };
            ScriptAssert.Equal(expectedValues, TestWorkspace.Execute(domain, "conversions", arguments: [input]));
            ScriptAssert.Equal(expectedValues, TestWorkspace.Execute(domain, "alias", arguments: [ScriptDatum.FromObject(BooleanValue.Type), input]));
        }
        foreach (var value in new[] { true, false })
        {
            var expected = value ? "true" : "false";
            ScriptAssert.Equal(expected, TestWorkspace.Execute(domain, "text", arguments: [ScriptDatum.FromBoolean(value)]));
            ScriptAssert.Equal(expected, TestWorkspace.Execute(domain, "dynamicText", arguments: [ScriptDatum.FromBoolean(value)]));
            foreach (var method in new[] { "create", "construct", "valueOf" })
                ScriptAssert.Equal(value, TestWorkspace.Execute(domain, method, arguments: [ScriptDatum.FromBoolean(value)]));
        }
#if NET9_0_OR_GREATER
        if (mode == CompilationMode.Persistence)
        {
            var methods = Assembly.Load(File.ReadAllBytes(Path.Combine(workspace.Root, "test-output.dll")))
                .GetTypes().SelectMany(type => type.GetMethods()).ToArray();
            foreach (var name in new[] { "text", "create", "construct", "valueOf", "empty" })
            {
                var calls = StringOptimizationTests.GetCalls(methods.Single(method => method.Name == name + "$native"));
                Assert.Contains(calls, call => call.DeclaringType == typeof(BooleanValue) &&
                    call.Name == (name == "text" ? nameof(BooleanValue.FormatString) : nameof(BooleanValue.CreateCore)));
                Assert.DoesNotContain(calls, call => call.DeclaringType == typeof(CallOps) || call.DeclaringType == typeof(ScriptDatum));
            }
        }
#endif
    }
}
