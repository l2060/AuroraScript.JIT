using AuroraScript.Compiler.Backend.Code;
using AuroraScript.Runtime;
using AuroraScript.Tests.Infrastructure;
using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Xunit;

namespace AuroraScript.Tests;

public sealed class CompilerBoundarySimplificationTests
{
    private static readonly string[] Comparisons = ["Equal", "NotEqual", "Less", "LessEqual", "Greater", "GreaterEqual"];

    [Fact]
    public void MetadataBindsCoreMethodsWithoutRemovingCompatibilityEntryPoints()
    {
        Assert.Equal(typeof(ScriptDatum).GetMethod(nameof(ScriptDatum.TypeOf)), TypedRuntimeMetadata.TypeOf);
        Assert.Equal(9, TypedRuntimeMetadata.ResolveClosureDelegate.Length);
        for (var index = 0; index < 9; index++)
        {
            var name = index == 0 ? "Resolve" : "Resolve" + (index - 1);
            var method = TypedRuntimeMetadata.ResolveClosureDelegate[index];
            Assert.Equal(typeof(DynamicMethodRegistry).GetMethod(name), method);
            Assert.Equal(typeof(ClosureOps).GetMethod(name)!.ReturnType, method.ReturnType);
        }
        foreach (var name in Comparisons)
        {
            Assert.Null(typeof(TypedRuntimeMetadata).GetField(name));
            Assert.NotNull(typeof(ValueOps).GetMethod(name));
        }
        Assert.NotNull(typeof(ValueOps).GetMethod(nameof(ValueOps.TypeOf)));
    }

    [Theory]
    [InlineData(CompilationMode.Dynamic)]
    [InlineData(CompilationMode.OnlyRun)]
#if NET9_0_OR_GREATER
    [InlineData(CompilationMode.Persistence)]
#endif
    public async Task ComparisonsTypeOfAndClosuresKeepTheirResults(CompilationMode mode)
    {
        using var workspace = new TestWorkspace();
        var (_, domain) = await workspace.CompileModuleAsync("""
            @module(TEST);
            var a = 2;
            var b = 3;
            var comparisons = [a == b, a != b, a < b, a <= b, a > b, a >= b];
            var kind = typeof a;
            var condition = (a < b) && (a != b);
            var sum = 4;
            sum += a < b;
            var sameDate = Date.parse(1000) == Date.parse(1000);
            func make() { var captured = 5; return x => x + captured; }
            export func classify(value) { return typeof value; }
            export func run() {
                var closure = make();
                return [comparisons, kind, condition, sum, sameDate, closure(4)];
            }
            """, mode);
        ScriptAssert.Equal(new object[] { new object[] { false, true, true, true, false, false },
            "number", true, 5, true, 9 }, TestWorkspace.Execute(domain, "run"));
        ScriptAssert.Equal("string", TestWorkspace.Execute(domain, "classify", arguments: [ScriptDatum.FromString("text")]));
#if NET9_0_OR_GREATER
        if (mode == CompilationMode.Persistence)
        {
            var methods = Assembly.Load(File.ReadAllBytes(Path.Combine(workspace.Root, "test-output.dll")))
                .GetTypes().SelectMany(t => t.GetMethods()).ToArray();
            var calls = StringOptimizationTests.GetCalls(methods.Single(m => m.Name == "Initialize"));
            foreach (var name in Comparisons)
            {
                Assert.Contains(calls, m => m.DeclaringType == typeof(ValueOps) && m.Name == name + "Boolean");
                Assert.DoesNotContain(calls, m => m.DeclaringType == typeof(ValueOps) && m.Name == name);
            }
            Assert.Contains(calls, m => m.DeclaringType == typeof(ScriptDatum) && m.Name == nameof(ScriptDatum.TypeOf));
            Assert.DoesNotContain(calls, m => m.DeclaringType == typeof(ValueOps) && m.Name == nameof(ValueOps.TypeOf));
        }
#endif
    }
}
