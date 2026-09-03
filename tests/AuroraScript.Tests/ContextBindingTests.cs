using AuroraScript.Runtime;
using AuroraScript.Runtime.Types;
using AuroraScript.Tests.Host;
using AuroraScript.Tests.Infrastructure;
using System;
using System.Threading.Tasks;
using Xunit;

namespace AuroraScript.Tests;

public sealed class ContextBindingTests
{
    [Theory]
    [InlineData(CompilationMode.Dynamic)]
    [InlineData(CompilationMode.OnlyRun)]
    public async Task TypedContextIsUserStateAndUsesNativeMembers(CompilationMode mode)
    {
        using var workspace = new TestWorkspace();
        var (engine, _) = await workspace.CompileModuleAsync(
            """
            @module(TEST);
            context vec as Vec2;
            export func length() Number {
                return vec.length();
            }
            """,
            mode,
            nativeTypes: true);

        var domain = engine.CreateDomain(userState: new Vec2(3, 4));
        ScriptAssert.Equal(5, domain.Execute("TEST", "length"));
    }

    [Theory]
    [InlineData(CompilationMode.Dynamic)]
    [InlineData(CompilationMode.OnlyRun)]
    public async Task MultipleContextNamesCacheIndependentlyInOneFunction(CompilationMode mode)
    {
        using var workspace = new TestWorkspace();
        var (engine, _) = await workspace.CompileModuleAsync(
            """
            @module(TEST);
            context vec as Vec2;
            context pos as Vec2;
            export func run() Number {
                return vec.x + pos.y;
            }
            """,
            mode,
            nativeTypes: true);

        var domain = engine.CreateDomain(userState: new Vec2(6, 8));
        ScriptAssert.Equal(14, domain.Execute("TEST", "run"));
    }

    [Theory]
    [InlineData(CompilationMode.Dynamic)]
    [InlineData(CompilationMode.OnlyRun)]
    public async Task NativeTypeReturnKeepsNativeProof(CompilationMode mode)
    {
        using var workspace = new TestWorkspace();
        var (engine, _) = await workspace.CompileModuleAsync(
            """
            @module(TEST);
            context vec as Vec2;
            export native func current() Vec2 {
                return vec;
            }
            export func use() Number {
                return current().length();
            }
            """,
            mode,
            nativeTypes: true);

        var domain = engine.CreateDomain(userState: new Vec2(6, 8));
        ScriptAssert.Equal(10, domain.Execute("TEST", "use"));
    }

        [Theory]
    [InlineData(CompilationMode.Dynamic)]
    [InlineData(CompilationMode.OnlyRun)]
    public async Task UntypedContextReadsAndWritesScriptObjectProperties(CompilationMode mode)
    {
        using var workspace = new TestWorkspace();
        var (engine, _) = await workspace.CompileModuleAsync(
            """
            @module(TEST);
            context bag;
            export func run() {
                bag.count = bag.count + 1;
                return bag.name;
            }
            """,
            mode);

        var userState = new ScriptObject();
        userState.Define("name", ScriptDatum.FromString("aurora"));
        userState.Define("count", ScriptDatum.FromNumber(1));
        var domain = engine.CreateDomain(userState: userState);

        ScriptAssert.Equal("aurora", domain.Execute("TEST", "run"));
        ScriptAssert.Equal(2, userState.GetPropertyDatum(null, "count"));
    }

    [Fact]
    public async Task DuplicateContextNameIsRejected()
    {
        using var workspace = new TestWorkspace();
        var error = await Assert.ThrowsAsync<AuroraCompilationException>(() =>
            workspace.CompileModuleAsync(
                """
                @module(TEST);
                context bag;
                context bag as Vec2;
                export func run() { return bag; }
                """,
                nativeTypes: true));
        Assert.Contains("Duplicate context", error.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task ContextTypeMustBeHostNativeType()
    {
        using var workspace = new TestWorkspace();
        var error = await Assert.ThrowsAsync<AuroraCompilationException>(() =>
            workspace.CompileModuleAsync(
                """
                @module(TEST);
                type Point { Number x; }
                context p as Point;
                export func run() { return p; }
                """));
        Assert.Contains("NativeType", error.Message, StringComparison.Ordinal);
    }
}
