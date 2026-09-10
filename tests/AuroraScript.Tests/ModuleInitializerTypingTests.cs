using AuroraScript.Runtime;
using AuroraScript.Runtime.Types;
using AuroraScript.Runtime.Builtin;
using AuroraScript.Tests.Infrastructure;
using AuroraScript.Tests.Host;
using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Xunit;

namespace AuroraScript.Tests;

public sealed class ModuleInitializerTypingTests
{
    [Theory]
    [InlineData(CompilationMode.Dynamic)]
    [InlineData(CompilationMode.OnlyRun)]
#if NET9_0_OR_GREATER
    [InlineData(CompilationMode.Persistence)]
#endif
    public async Task ModuleDeclarationsShareTypedLocalStorageForScalarsAndNativeMembers(CompilationMode mode)
    {
        using var workspace = new TestWorkspace();
        var (_, domain) = await workspace.CompileModuleAsync("""
            @module(TEST);
            var map = new HashMap();
            var number = 2;
            var text = 'hello';
            var flag = true;
            var empty;
            number += 3;
            text = text + '!';
            var numericCopy = number;
            var textCopy = text;
            var boolCopy = flag;
            var nullCopy = empty;
            var size = map.size;
            export func result() { return [size, numericCopy, textCopy, boolCopy, nullCopy]; }
            """, mode);
        using (domain)
            ScriptAssert.Equal(new object?[] { 0, 5, "hello!", true, null }, TestWorkspace.Execute(domain, "result"));
#if NET9_0_OR_GREATER
        if (mode == CompilationMode.Persistence)
        {
            var initialize = Assembly.Load(File.ReadAllBytes(Path.Combine(workspace.Root, "test-output.dll")))
                .GetTypes().SelectMany(type => type.GetMethods()).Single(method => method.Name == "Initialize");
            var locals = initialize.GetMethodBody()!.LocalVariables;
            foreach (var type in new[] { typeof(int), typeof(string), typeof(bool), typeof(ScriptHashMap) })
                Assert.Contains(locals, local => local.LocalType == type);
            Assert.DoesNotContain(StringOptimizationTests.GetCalls(initialize),
                call => call.DeclaringType == typeof(ScopeOps) && call.Name == nameof(ScopeOps.GetModule));
        }
#endif
    }

    [Theory]
    [InlineData(CompilationMode.Dynamic)]
    [InlineData(CompilationMode.OnlyRun)]
#if NET9_0_OR_GREATER
    [InlineData(CompilationMode.Persistence)]
#endif
    public async Task ModulePackedStoragePreservesWritesAndEvaluationOrder(CompilationMode mode)
    {
        using var workspace = new TestWorkspace();
        var (_, domain) = await workspace.CompileModuleAsync("""
            @module(TEST);
            var values = new Int32Array(2);
            values[0] = 4;
            var before = values[0]++;
            values[1] = values[0] + 1;
            var after = values[1];
            func replace() { values = new Int32Array(2); values[0] = 20; return 0; }
            var original = values[replace()]++;
            var replaced = values[0];
            export func result() { return [before, after, original, replaced]; }
            """, mode);
        using (domain)
            ScriptAssert.Equal(new object[] { 4, 6, 5, 20 }, TestWorkspace.Execute(domain, "result"));
    }

    [Theory]
    [InlineData(CompilationMode.Dynamic)]
    [InlineData(CompilationMode.OnlyRun)]
#if NET9_0_OR_GREATER
    [InlineData(CompilationMode.Persistence)]
#endif
    public async Task ArrayMutationPreservesModuleTypeAndUsesLocalStorage(CompilationMode mode)
    {
        using var workspace = new TestWorkspace();
        var (_, domain) = await workspace.CompileModuleAsync("""
            @module(TEST);
            var a = [0, 1, 2, 3];
            a[0]++;
            var b = a[0]++;
            b++;
            var c = b++;
            var d = ++b;
            b = 8;
            var e = b;
            var scalar = 8;
            scalar += 2;
            var copied = scalar;
            export func result() { return [a[0], b, c, d, e, copied]; }
            """, mode);
        using (domain)
            ScriptAssert.Equal(new object[] { 2, 8, 2, 4, 8, 10 }, TestWorkspace.Execute(domain, "result"));
#if NET9_0_OR_GREATER
        if (mode == CompilationMode.Persistence)
        {
            var initialize = Assembly.Load(File.ReadAllBytes(Path.Combine(workspace.Root, "test-output.dll")))
                .GetTypes().SelectMany(type => type.GetMethods()).Single(method => method.Name == "Initialize");
            var calls = StringOptimizationTests.GetCalls(initialize);
            Assert.Contains(initialize.GetMethodBody()!.LocalVariables, local => local.LocalType == typeof(ScriptArray));
            Assert.DoesNotContain(calls, call => call.DeclaringType == typeof(ScopeOps) && call.Name == nameof(ScopeOps.GetModule));
            Assert.DoesNotContain(calls, call => call.DeclaringType == typeof(ObjectOps) && call.Name.Contains("Element"));
            Assert.Equal(2, calls.Count(call => call.DeclaringType == typeof(ScriptArray) && call.Name == "get_Item"));
        }
#endif
    }

    [Theory]
    [InlineData(CompilationMode.Dynamic)]
    [InlineData(CompilationMode.OnlyRun)]
#if NET9_0_OR_GREATER
    [InlineData(CompilationMode.Persistence)]
#endif
    public async Task ModuleArrayCacheObservesCallbacksAndLaterFunctionWrites(CompilationMode mode)
    {
        using var workspace = new TestWorkspace();
        var (_, domain) = await workspace.CompileModuleAsync("""
            @module(TEST);
            var a = [0];
            a[0]++;
            func replace() { a = [10]; }
            replace();
            var b = a[0]++;
            var after = a[0];
            export func result() {
                replace();
                a[0]++;
                var c = a[0]++;
                return [b, after, c, a[0]];
            }
            """, mode);
        using (domain)
            ScriptAssert.Equal(new object[] { 10, 11, 11, 12 }, TestWorkspace.Execute(domain, "result"));
    }

    [Theory]
    [InlineData(CompilationMode.Dynamic)]
    [InlineData(CompilationMode.OnlyRun)]
#if NET9_0_OR_GREATER
    [InlineData(CompilationMode.Persistence)]
#endif
    public async Task ImplicitCoercionInvalidatesMutableModuleTypes(CompilationMode mode)
    {
        using var workspace = new TestWorkspace();
        var conversions = 0;
        var (_, domain) = await workspace.CompileModuleAsync("""
            @module(TEST);
            const object = MAKE();
            var value = 2;
            const text = '' + object;
            const after = value + 1;
            var second = 2;
            const template = `${object}${second + 1}`;
            export func result() { return [text, after, template]; }
            """, mode, configureGlobal: global => global.Define("MAKE", ScriptDatum.FromBonding(
                (ScriptContext ctx, ScriptObject self, Span<ScriptDatum> args, ref ScriptDatum result) =>
                {
                    result = ScriptDatum.FromObject(new MutatingString(() =>
                        ctx.Module.SetPropertyDatum(ctx, ++conversions == 1 ? "value" : "second", ScriptDatum.FromString("changed"))));
                }), false, false));
        using (domain)
            ScriptAssert.Equal(new object[] { "formatted", "changed1", "formattedchanged1" }, TestWorkspace.Execute(domain, "result"));
    }

    private sealed class MutatingString(Action mutate) : ScriptObject
    {
        public override string ToString() { mutate(); return "formatted"; }
    }

    [Theory]
    [InlineData(CompilationMode.Dynamic)]
    [InlineData(CompilationMode.OnlyRun)]
#if NET9_0_OR_GREATER
    [InlineData(CompilationMode.Persistence)]
#endif
    public async Task InitializerBindsImportedNativeFunctionsAndTypedContexts(CompilationMode mode)
    {
        using var workspace = new TestWorkspace();
        var path = Path.Combine(workspace.Root, "import-context.dll");
        var engine = workspace.CreateEngine(mode, nativeTypes: true, assemblyOut: path);
        workspace.WriteSource("lib.as", """
            @module(LIB);
            export native func scale(Number value) Number { return value * 3; }
            """);
        workspace.WriteSource("main.as", """
            @module(TEST);
            import ops from 'lib.as';
            context vec as Vec2;
            const length = vec.length();
            const answer = ops.scale(length + 1);
            export func result() { return answer; }
            """);
        await engine.BuildAsync("main.as");
        using var domain = engine.CreateDomain(userState: new Vec2(3, 4));
        ScriptAssert.Equal(18, TestWorkspace.Execute(domain, "result"));
#if NET9_0_OR_GREATER
        if (mode == CompilationMode.Persistence)
        {
            var calls = Assembly.Load(File.ReadAllBytes(path)).GetTypes().SelectMany(type => type.GetMethods())
                .Where(method => method.Name == "Initialize").SelectMany(StringOptimizationTests.GetCalls).ToArray();
            Assert.Contains(calls, call => call.Name == "scale$native");
            Assert.Contains(calls, call => call.DeclaringType == typeof(Vec2) && call.Name == "LengthCore");
            Assert.DoesNotContain(calls, call => call.Name.StartsWith("Invoke", StringComparison.Ordinal));
        }
#endif
    }

    [Theory]
    [InlineData(CompilationMode.Dynamic)]
    [InlineData(CompilationMode.OnlyRun)]
#if NET9_0_OR_GREATER
    [InlineData(CompilationMode.Persistence)]
#endif
    public async Task TopLevelExpressionsShareFunctionInferenceAndNativeEmission(CompilationMode mode)
    {
        using var workspace = new TestWorkspace();
        var (_, domain) = await workspace.CompileModuleAsync("""
            @module(TEST);
            native func decorate(String value) String { return value + '!'; }
            native func twice(Number value) Number { return value * 2; }
            const prefix = 'seed';
            const text = decorate(prefix + '...');
            const length = text.length;
            var input = 3;
            const doubled = twice(input + 1);
            const power = Math.pow(doubled, 2);
            const buffer = new StringBuffer();
            buffer.append(text);
            const built = buffer.toString();
            const values = new Int32Array(2);
            values[0] = 3;
            const element = values[0] + 4;
            export func result() { return [text, length, doubled, power, built, element]; }
            """, mode);
        using (domain)
            ScriptAssert.Equal(new object[] { "seed...!", 8, 8, 64, "seed...!", 7 },
                TestWorkspace.Execute(domain, "result"));
#if NET9_0_OR_GREATER
        if (mode == CompilationMode.Persistence)
        {
            var methods = Assembly.Load(File.ReadAllBytes(Path.Combine(workspace.Root, "test-output.dll")))
                .GetTypes().SelectMany(type => type.GetMethods()).ToArray();
            var calls = StringOptimizationTests.GetCalls(methods.Single(method => method.Name == "Initialize"));
            Assert.Contains(calls, call => call.Name.StartsWith("decorate$", StringComparison.Ordinal) && ((MethodInfo)call).ReturnType == typeof(string));
            Assert.Contains(calls, call => call.Name.StartsWith("twice$", StringComparison.Ordinal) && ((MethodInfo)call).ReturnType != typeof(ScriptDatum));
            Assert.Contains(calls, call => call.DeclaringType == typeof(MathSupport) && call.Name == nameof(MathSupport.PowCore));
            Assert.Contains(calls, call => call.DeclaringType == typeof(StringBuffer) && call.Name == nameof(StringBuffer.AppendCore));
            Assert.DoesNotContain(calls, call => call.Name.StartsWith("Invoke", StringComparison.Ordinal));
        }
#endif
    }

    [Theory]
    [InlineData(CompilationMode.Dynamic)]
    [InlineData(CompilationMode.OnlyRun)]
#if NET9_0_OR_GREATER
    [InlineData(CompilationMode.Persistence)]
#endif
    public async Task ModuleInferenceInvalidatesFactsAcrossCallbacksAndMergesConditionalWrites(CompilationMode mode)
    {
        using var workspace = new TestWorkspace();
        var (_, domain) = await workspace.CompileModuleAsync("""
            @module(TEST);
            var value = 2;
            func mutate() { value = 'changed'; }
            mutate();
            const afterCall = value + 1;
            var conditional = 2;
            false && (conditional = 'changed');
            const afterBranch = conditional + 1;
            var selected = 2;
            true && (selected = 'changed');
            const afterTakenBranch = selected + 1;
            export func result() { return [afterCall, afterBranch, afterTakenBranch]; }
            """, mode);
        using (domain)
            ScriptAssert.Equal(new object[] { "changed1", 3, "changed1" }, TestWorkspace.Execute(domain, "result"));
    }

    [Theory]
    [InlineData(CompilationMode.Dynamic)]
    [InlineData(CompilationMode.OnlyRun)]
#if NET9_0_OR_GREATER
    [InlineData(CompilationMode.Persistence)]
#endif
    public async Task ModuleLiteralsUseTheSameNativeOverloadsAsFunctions(CompilationMode mode)
    {
        using var workspace = new TestWorkspace();
        using var output = new StringWriter();
        var assemblyPath = Path.Combine(workspace.Root, "module-host.dll");
        var engine = workspace.CreateEngine(mode, assemblyOut: assemblyPath, output: output);
        workspace.WriteSource("main.as", """
            @module(TEST);
            console.log('same');
            console.log(42);
            console.log(1.5);
            console.log(true);
            console.log(9223372036854775807L);
            console.log(18446744073709551615UL);
            console.log();
            var empty = console.log(('value'));
            var root = Math.pow(9, 0.5);
            var optional = Math.max(1.5);
            var json = JSON.stringify(root);
            var surplus = Math.pow(2, 3, console.log('extra'));
            export func go() { console.log('same'); return [empty == null, root, optional, json, surplus]; }
            """);
        await engine.BuildAsync("main.as");
        using var domain = engine.CreateDomain();
        ScriptAssert.Equal(new object[] { true, 3, 1.5, "3", 8 }, TestWorkspace.Execute(domain, "go"));
        Assert.Equal(string.Join(Environment.NewLine, new[] {
            "same", "42", 1.5.ToString(System.Globalization.CultureInfo.CurrentCulture),
            "True", "9223372036854775807", "18446744073709551615", "value", "extra", "same", ""
        }), output.ToString());
#if NET9_0_OR_GREATER
        if (mode == CompilationMode.Persistence)
        {
            var methods = Assembly.Load(File.ReadAllBytes(assemblyPath)).GetTypes()
                .SelectMany(type => type.GetMethods()).ToArray();
            var calls = StringOptimizationTests.GetCalls(methods.Single(method => method.Name == "Initialize"));
            var logs = calls.Where(call => call.DeclaringType == typeof(ConsoleSupport) && call.Name == nameof(ConsoleSupport.LogCore)).ToArray();
            Assert.Equal(9, logs.Length);
            foreach (var type in new[] { typeof(string), typeof(int), typeof(double), typeof(bool), typeof(long), typeof(ulong) })
                Assert.Contains(logs, call => call.GetParameters().Last().ParameterType == type);
            Assert.DoesNotContain(calls, call => call.Name.StartsWith("InvokeProperty", StringComparison.Ordinal));
            var functionCalls = StringOptimizationTests.GetCalls(methods.Single(method => method.Name == "go$typed"));
            Assert.Contains(functionCalls, call => call == logs[0]);
        }
#endif
    }

    [Theory]
    [InlineData(CompilationMode.Dynamic)]
    [InlineData(CompilationMode.OnlyRun)]
#if NET9_0_OR_GREATER
    [InlineData(CompilationMode.Persistence)]
#endif
    public async Task DynamicAndVariadicModuleCallsKeepTheirArguments(CompilationMode mode)
    {
        using var workspace = new TestWorkspace();
        using var output = new StringWriter();
        var engine = workspace.CreateEngine(mode,
            assemblyOut: Path.Combine(workspace.Root, "fallback.dll"), output: output);
        workspace.WriteSource("main.as", """
            @module(TEST);
            var value = 'dynamic';
            console.log(value);
            console.log('first', 'second');
            console.log(...['spread']);
            export func go() { return true; }
            """);
        await engine.BuildAsync("main.as");
        using var domain = engine.CreateDomain();
        ScriptAssert.Equal(true, TestWorkspace.Execute(domain, "go"));
        Assert.Equal(string.Join(Environment.NewLine, new[] { "dynamic", "first, second", "spread", "" }), output.ToString());
    }

    [Theory]
    [InlineData(CompilationMode.Dynamic)]
    [InlineData(CompilationMode.OnlyRun)]
#if NET9_0_OR_GREATER
    [InlineData(CompilationMode.Persistence)]
#endif
    public async Task ModuleSymbolsShadowNativeExportOwners(CompilationMode mode)
    {
        using var workspace = new TestWorkspace();
        var (_, domain) = await workspace.CompileModuleAsync("""
            @module(TEST);
            var captured = '';
            var console = { log: (value) => { captured = value; } };
            console.log('shadowed');
            export func go() { return captured; }
            """, mode);
        using (domain) ScriptAssert.Equal("shadowed", TestWorkspace.Execute(domain, "go"));
    }
}
