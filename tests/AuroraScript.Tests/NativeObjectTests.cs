using AuroraScript.Runtime;
using AuroraScript.Runtime.Types;
using AuroraScript.Tests.Host;
using AuroraScript.Tests.Infrastructure;
using System;
using System.Threading.Tasks;
using Xunit;

namespace AuroraScript.Tests;

public sealed class NativeObjectTests
{
    [Fact]
    public void GeneratedRegisterCanTargetAnyScriptObject()
    {
        var owner = new ScriptObject();

        Vec2.Register(owner);

        Assert.Same(Vec2.Type, owner.GetPropertyDatum(null!, "Vec2").Object);
    }

    [Fact]
    public async Task HostInstanceExposesNativeFieldsAndMethods()
    {
        using var workspace = new TestWorkspace();
        var (_, domain) = await workspace.CompileModuleAsync(
            """
            @module(TEST);
            export func readX(vec) Number {
                return vec.x;
            }
            export func scaleX(vec, Number value) Number {
                vec.x = value;
                return vec.x;
            }
            export func lengthOf(vec) Number {
                return vec.length();
            }
            export func addY(vec, other) Number {
                return vec.add(other).y;
            }
            export func typeName(vec) String {
                return typeof vec;
            }
            """,
            configureGlobal: global => Vec2.Register(global));

        var vec = new Vec2(3, 4);
        ScriptAssert.Equal(
            3,
            TestWorkspace.Execute(
                domain,
                "readX",
                arguments: [ScriptDatum.FromObject(vec)]));
        ScriptAssert.Equal(
            5,
            TestWorkspace.Execute(
                domain,
                "lengthOf",
                arguments: [ScriptDatum.FromObject(vec)]));
        ScriptAssert.Equal(
            10,
            TestWorkspace.Execute(
                domain,
                "scaleX",
                arguments: [ScriptDatum.FromObject(vec), ScriptDatum.FromNumber(10)]));
        Assert.Equal(10, vec.X);
        ScriptAssert.Equal(
            6,
            TestWorkspace.Execute(
                domain,
                "addY",
                arguments:
                [
                    ScriptDatum.FromObject(vec),
                    ScriptDatum.FromObject(new Vec2(1, 2))
                ]));
        ScriptAssert.Equal(
            "Vec2",
            TestWorkspace.Execute(
                domain,
                "typeName",
                arguments: [ScriptDatum.FromObject(vec)]));
    }

    [Fact]
    public async Task ScriptCanConstructNativeObject()
    {
        using var workspace = new TestWorkspace();
        var (_, domain) = await workspace.CompileModuleAsync(
            """
            @module(TEST);
            export func run() Number {
                var vec = new Vec2(6, 8);
                return vec.length();
            }
            """,
            configureGlobal: global => Vec2.Register(global));

        ScriptAssert.Equal(10, TestWorkspace.Execute(domain, "run"));
    }

    [Fact]
    public async Task NativeConstructorExposesStaticExportsAndConstants()
    {
        using var workspace = new TestWorkspace();
        var (_, domain) = await workspace.CompileModuleAsync(
            """
            @module(TEST);
            export func run() {
                var vec = new Vec2(3, 4);
                return [Vec2.length(6, 8), Vec2.DIMENSIONS, vec.length()];
            }
            """,
            configureGlobal: global => Vec2.Register(global));

        ScriptAssert.Equal(
            new object?[] { 10D, 2D, 5D },
            TestWorkspace.Execute(domain, "run"));
    }

    [Fact]
    public async Task StaticFactoryReturnsProvenNativeInstance()
    {
        using var workspace = new TestWorkspace();
        var (_, domain) = await workspace.CompileModuleAsync(
            """
            @module(TEST);
            export func run() Number {
                var vec = Vec2.from(3, 4);
                return vec.factoryValue() + vec.length();
            }
            """,
            nativeTypes: true);

        ScriptAssert.Equal(12, TestWorkspace.Execute(domain, "run"));
    }

    [Fact]
    public async Task StaticFactoryKeepsDynamicFallbackWithoutHostMetadata()
    {
        using var workspace = new TestWorkspace();
        var (_, domain) = await workspace.CompileModuleAsync(
            """
            @module(TEST);
            export func run() Number {
                return Vec2.from(3, 4).factoryValue();
            }
            """,
            configureGlobal: global => Vec2.Register(global));

        ScriptAssert.Equal(7, TestWorkspace.Execute(domain, "run"));
    }

    [Fact]
    public async Task NativeFieldsDoNotUsePropertySlots()
    {
        var vec = new Vec2(1, 2);
        vec.Define("tag", ScriptDatum.FromString("ok"));
        Assert.Equal(1, vec.X);
        Assert.Equal("ok", vec.GetPropertyDatum(null!, "tag").StringText);
        Assert.Equal(1d, vec.GetPropertyDatum(null!, "x").Number);
        Assert.False(vec.DeletePropertyValue("x"));
        Assert.Equal(1d, vec.GetPropertyDatum(null!, "x").Number);
        Assert.Equal(ValueKind.Null, vec.GetPropertyDatum(null!, "X").Kind);
        vec.SetPropertyDatum(null!, "tag", ScriptDatum.FromString("renamed"));
        Assert.True(vec.DeletePropertyValue("tag"));
        Assert.Equal(ValueKind.Null, vec.GetPropertyDatum(null!, "tag").Kind);
    }

    [Fact]
    public async Task NativeObjectDoesNotExposeClrMembersByReflection()
    {
        using var workspace = new TestWorkspace();
        var (_, domain) = await workspace.CompileModuleAsync(
            """
            @module(TEST);
            export func readPascal(vec) {
                return vec.X;
            }
            export func extras(vec) String {
                vec.tag = "ok";
                return vec.tag;
            }
            """,
            configureGlobal: global => Vec2.Register(global));

        var vec = new Vec2(3, 4);
        Assert.Equal(
            ValueKind.Null,
            TestWorkspace.Execute(
                domain,
                "readPascal",
                arguments: [ScriptDatum.FromObject(vec)]).Kind);
        ScriptAssert.Equal(
            "ok",
            TestWorkspace.Execute(
                domain,
                "extras",
                arguments: [ScriptDatum.FromObject(vec)]));
    }
}
