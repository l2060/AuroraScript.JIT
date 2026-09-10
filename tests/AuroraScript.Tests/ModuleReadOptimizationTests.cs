using AuroraScript.Runtime;
using AuroraScript.Runtime.Types;
using AuroraScript.Tests.Infrastructure;
using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Threading.Tasks;
using Xunit;

namespace AuroraScript.Tests;

public sealed class ModuleReadOptimizationTests
{
    private const string MutationSource = """
        @module(TEST);
        var a = [0, 1, 2, 3];
        var observed;
        export func aaa() {
            a[0]++;
            var b = a[0]++;
            b++;
            var c = b++;
            observed = [b, c];
        }
        export func result() { return [a[0], observed]; }
        """;

    [Theory]
    [InlineData(CompilationMode.Dynamic)]
    [InlineData(CompilationMode.OnlyRun)]
#if NET9_0_OR_GREATER
    [InlineData(CompilationMode.Persistence)]
#endif
    public async Task RepeatedModuleReadsUseOneDynamicLocalWithoutTypeBranches(CompilationMode mode)
    {
        using var workspace = new TestWorkspace();
        var (_, domain) = await workspace.CompileModuleAsync(MutationSource, mode);
        using (domain)
        {
            TestWorkspace.Execute(domain, "aaa");
            ScriptAssert.Equal(new object[] { 2, new object[] { 3, 2 } }, TestWorkspace.Execute(domain, "result"));
            TestWorkspace.Execute(domain, "aaa");
            ScriptAssert.Equal(new object[] { 4, new object[] { 5, 4 } }, TestWorkspace.Execute(domain, "result"));
        }
#if NET9_0_OR_GREATER
        if (mode == CompilationMode.Persistence)
        {
            var method = Assembly.Load(File.ReadAllBytes(Path.Combine(workspace.Root, "test-output.dll")))
                .GetTypes().SelectMany(type => type.GetMethods()).Single(method => method.Name == "aaa$typed");
            var calls = StringOptimizationTests.GetCalls(method, opcode =>
            {
                Assert.NotEqual(FlowControl.Cond_Branch, opcode.FlowControl);
                Assert.NotEqual(OpCodes.Isinst, opcode);
                Assert.NotEqual(OpCodes.Castclass, opcode);
            });
            Assert.Single(calls, call => call.DeclaringType == typeof(ScopeOps) && call.Name == nameof(ScopeOps.GetModule));
            Assert.Equal(2, calls.Count(call => call.DeclaringType == typeof(ObjectOps) && call.Name == nameof(ObjectOps.GetElementIndex)));
            Assert.DoesNotContain(calls, call => call.DeclaringType == typeof(ScriptArray) && call.Name == "get_Item");
        }
#endif
    }

    [Theory]
    [InlineData(CompilationMode.Dynamic)]
    [InlineData(CompilationMode.OnlyRun)]
#if NET9_0_OR_GREATER
    [InlineData(CompilationMode.Persistence)]
#endif
    public async Task ScalarReadRegionsHandleTypeChangesCallsAndBranches(CompilationMode mode)
    {
        using var workspace = new TestWorkspace();
        var (_, domain) = await workspace.CompileModuleAsync("""
            @module(TEST);
            var value = 3;
            var stable = 7;
            export func sum() { return stable + stable; }
            export func replace(next) { value = next; }
            export func read(flag) { if (flag) return value + value; return 0; }
            export func acrossCall() {
                var before = value + value;
                replace(10);
                var after = value + value;
                return [before, after];
            }
            """, mode);
        using (domain)
        {
            ScriptAssert.Equal(14, TestWorkspace.Execute(domain, "sum"));
            ScriptAssert.Equal(6, TestWorkspace.Execute(domain, "read", arguments: [ScriptDatum.FromBoolean(true)]));
            ScriptAssert.Equal(new object[] { 6, 20 }, TestWorkspace.Execute(domain, "acrossCall"));
            TestWorkspace.Execute(domain, "replace", arguments: [ScriptDatum.FromString("x")]);
            ScriptAssert.Equal("xx", TestWorkspace.Execute(domain, "read", arguments: [ScriptDatum.FromBoolean(true)]));
            Assert.True(domain.Global.TryGetModule("TEST", out var module));
            var getterCalls = 0;
            module.DefineInternal("value", ScriptDatum.FromObject(new BondingGetter(
                (ScriptObject _, ref ScriptDatum result) => result = ScriptDatum.FromNumber(++getterCalls))), true, false, true);
            ScriptAssert.Equal(0, TestWorkspace.Execute(domain, "read", arguments: [ScriptDatum.FromBoolean(false)]));
            Assert.Equal(0, getterCalls);
            ScriptAssert.Equal(3, TestWorkspace.Execute(domain, "read", arguments: [ScriptDatum.FromBoolean(true)]));
            Assert.Equal(2, getterCalls);
        }
#if NET9_0_OR_GREATER
        if (mode == CompilationMode.Persistence)
        {
            var methods = Assembly.Load(File.ReadAllBytes(Path.Combine(workspace.Root, "test-output.dll")))
                .GetTypes().SelectMany(type => type.GetMethods()).ToArray();
            var method = methods.Single(method => method.Name == "acrossCall$typed");
            // Four value reads plus resolving the writable module function replace.
            Assert.Equal(5, StringOptimizationTests.GetCalls(method).Count(
                call => call.DeclaringType == typeof(ScopeOps) && call.Name == nameof(ScopeOps.GetModule)));
            var sumCalls = StringOptimizationTests.GetCalls(methods.Single(method => method.Name == "sum$typed"));
            Assert.Single(sumCalls, call => call.DeclaringType == typeof(ScopeOps) && call.Name == nameof(ScopeOps.GetModule));
            Assert.Contains(sumCalls, call => call.DeclaringType == typeof(ValueOps) && call.Name == nameof(ValueOps.Add));
        }
#endif
    }

    [Theory]
    [InlineData(CompilationMode.Dynamic)]
    [InlineData(CompilationMode.OnlyRun)]
#if NET9_0_OR_GREATER
    [InlineData(CompilationMode.Persistence)]
#endif
    public async Task UnprovenIndexersCanReplaceModuleBindingsBetweenReads(CompilationMode mode)
    {
        using var workspace = new TestWorkspace();
        ReplacingIndexer? indexer = null;
        var replacement = new ScriptArray(1);
        replacement[0] = ScriptDatum.FromNumber(10);
        var (_, domain) = await workspace.CompileModuleAsync(
            MutationSource.Replace("var a = [0, 1, 2, 3];", "var a = MAKE();"), mode,
            configureGlobal: global => global.Define("MAKE", ScriptDatum.FromBonding(
                (ScriptContext context, ScriptObject _, Span<ScriptDatum> args, ref ScriptDatum result) =>
                {
                    var module = context.Module;
                    indexer = new ReplacingIndexer(() => module.DefineInternal(
                        "a", ScriptDatum.FromObject(replacement), true, false, true));
                    result = ScriptDatum.FromObject(indexer);
                }), false, false));
        using (domain)
        {
            TestWorkspace.Execute(domain, "aaa");
            ScriptAssert.Equal(new object[] { 11, new object[] { 12, 11 } }, TestWorkspace.Execute(domain, "result"));
            Assert.Equal(1, indexer!.Value.Number);
        }
    }

    private sealed class ReplacingIndexer(Action replace) : ScriptObject, IAuroraNativeIndexer
    {
        internal ScriptDatum Value = ScriptDatum.FromNumber(0);
        public ScriptDatum this[int index]
        {
            get { replace(); return Value; }
            set => Value = value;
        }
    }
}
