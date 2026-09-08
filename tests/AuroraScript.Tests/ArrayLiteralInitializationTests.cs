using AuroraScript.Runtime;
using AuroraScript.Runtime.Types;
using AuroraScript.Tests.Infrastructure;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Xunit;

namespace AuroraScript.Tests;

public sealed class ArrayLiteralInitializationTests
{
    [Theory]
    [InlineData(CompilationMode.Dynamic)]
    [InlineData(CompilationMode.OnlyRun)]
#if NET9_0_OR_GREATER
    [InlineData(CompilationMode.Persistence)]
#endif
    public async Task FixedLiteralsUseUncheckedInitializationOnly(CompilationMode mode)
    {
        using var workspace = new TestWorkspace();
        var (_, domain) = await workspace.CompileModuleAsync("""
            @module(TEST);
            var initial = [1, 'two', null, [4], 5];
            export func moduleValue() { return initial; }
            export native func literal() Array { return [1, 'two', null, [4], 5]; }
            export native func empty() Array { return []; }
            export native func spread(Array values) Array { return [0, ...values, 6]; }
            export native func update(Array values) void { values[-1] = 7; values[7] = 8; }
            export func order() {
                var trace = '';
                func mark(value) { trace += value; return value; }
                var values = [mark('a'), mark('b'), mark('c'), mark('d'), mark('e')];
                return [trace, values];
            }
            """, mode);

        var expected = new object?[] { 1, "two", null, new object[] { 4 }, 5 };
        ScriptAssert.Equal(expected, TestWorkspace.Execute(domain, "moduleValue"));
        var literal = TestWorkspace.Execute(domain, "literal");
        ScriptAssert.Equal(expected, literal);
        var array = Assert.IsType<ScriptArray>(literal.Object);
        Assert.Equal(5, array.Length);
        Assert.Equal(5, array._items.Length);
        ScriptAssert.Equal(new object[0], TestWorkspace.Execute(domain, "empty"));
        ScriptAssert.Equal(new object?[] { 0, 1, "two", null, new object[] { 4 }, 5, 6 },
            TestWorkspace.Execute(domain, "spread", arguments: [literal]));
        ScriptAssert.Equal(new object[] { "abcde", new object[] { "a", "b", "c", "d", "e" } },
            TestWorkspace.Execute(domain, "order"));
        TestWorkspace.Execute(domain, "update", arguments: [literal]);
        ScriptAssert.Equal(new object?[] { 1, "two", null, new object[] { 4 }, 7, null, null, 8 }, literal);

#if NET9_0_OR_GREATER
        if (mode == CompilationMode.Persistence)
        {
            var methods = Assembly.Load(File.ReadAllBytes(Path.Combine(workspace.Root, "test-output.dll")))
                .GetTypes().SelectMany(type => type.GetMethods()).ToArray();
            foreach (var name in new[] { "Initialize", "literal$native" })
            {
                var calls = StringOptimizationTests.GetCalls(methods.Single(method => method.Name == name));
                Assert.Contains(calls, method => method.DeclaringType == typeof(ScriptArray) &&
                    method.Name == nameof(ScriptArray.InitializeElementUnchecked));
                Assert.DoesNotContain(calls, method => method.DeclaringType == typeof(ScriptArray) &&
                    (method.Name == "set_Item" || method.Name == nameof(ScriptArray.SetElementValue) ||
                     method.Name == nameof(ScriptArray.SetElement)));
            }
            foreach (var name in new[] { "empty$native", "spread$native", "update$native" })
            {
                var calls = StringOptimizationTests.GetCalls(methods.Single(method => method.Name == name));
                Assert.DoesNotContain(calls, method => method.DeclaringType == typeof(ScriptArray) &&
                    method.Name == nameof(ScriptArray.InitializeElementUnchecked));
            }
            Assert.Contains(StringOptimizationTests.GetCalls(methods.Single(method => method.Name == "update$native")),
                method => method.DeclaringType == typeof(ScriptArray) && method.Name == "set_Item");
        }
#endif
    }
}
