using AuroraScript;
using AuroraScript.Core;
using AuroraScript.Runtime;
using AuroraScript.Runtime.Types;
using AuroraScript.Source;
using BenchmarkDotNet.Attributes;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Threading.Tasks;

namespace AuroraBenchmark;

[MemoryDiagnoser]
public class GeneratedBoundaryBenchmarks
{
    public static void DumpSlimmingIl(string assemblyPath, string outputPath)
    {
        var assembly = Assembly.Load(File.ReadAllBytes(assemblyPath));
        var opcodes = typeof(OpCodes).GetFields(BindingFlags.Public | BindingFlags.Static)
            .Where(field => field.FieldType == typeof(OpCode))
            .Select(field => (OpCode)field.GetValue(null))
            .ToDictionary(opcode => unchecked((ushort)opcode.Value));
        var methods = new List<object>();
        foreach (var method in assembly.GetTypes().SelectMany(type => type.GetMethods(
            BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance | BindingFlags.DeclaredOnly)
            .Cast<MethodBase>().Concat(type.GetConstructors(
                BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance | BindingFlags.DeclaredOnly)))
            .OrderBy(method => method.DeclaringType.FullName + "::" + method, StringComparer.Ordinal))
        {
            var body = method.GetMethodBody();
            if (body == null) continue;
            var il = body.GetILAsByteArray();
            var instructions = new List<string>();
            for (var offset = 0; offset < il.Length;)
            {
                ushort code = il[offset++];
                if (code == 0xfe) code = (ushort)(0xfe00 | il[offset++]);
                var opcode = opcodes[code];
                var size = opcode.OperandType switch
                {
                    OperandType.InlineNone => 0,
                    OperandType.ShortInlineBrTarget or OperandType.ShortInlineI or OperandType.ShortInlineVar => 1,
                    OperandType.InlineVar => 2,
                    OperandType.InlineI8 or OperandType.InlineR => 8,
                    OperandType.InlineSwitch => 4 + 4 * BitConverter.ToInt32(il, offset),
                    _ => 4
                };
                var operand = Convert.ToHexString(il.AsSpan(offset, size));
                if (opcode.OperandType is OperandType.InlineField or OperandType.InlineMethod or OperandType.InlineTok or OperandType.InlineType)
                {
                    var member = method.Module.ResolveMember(BitConverter.ToInt32(il, offset));
                    operand = member.DeclaringType?.FullName + "::" + member;
                }
                else if (opcode.OperandType == OperandType.InlineString)
                    operand = System.Text.Json.JsonSerializer.Serialize(method.Module.ResolveString(BitConverter.ToInt32(il, offset)));
                else if (opcode.OperandType == OperandType.InlineSig)
                    operand = Convert.ToHexString(method.Module.ResolveSignature(BitConverter.ToInt32(il, offset)));
                instructions.Add(opcode.Name + " " + operand);
                offset += size;
            }
            methods.Add(new
            {
                Method = method.DeclaringType.FullName + "::" + method,
                ILBytes = il.Length,
                body.InitLocals,
                body.MaxStackSize,
                Locals = body.LocalVariables.Select(local => $"{local.LocalIndex}:{local.LocalType}:{local.IsPinned}").ToArray(),
                Exceptions = body.ExceptionHandlingClauses.Select(clause => new
                {
                    clause.Flags, clause.TryOffset, clause.TryLength, clause.HandlerOffset, clause.HandlerLength,
                    FilterOffset = clause.Flags == ExceptionHandlingClauseOptions.Filter ? clause.FilterOffset : -1,
                    CatchType = clause.Flags == ExceptionHandlingClauseOptions.Clause ? clause.CatchType?.FullName : null
                }).ToArray(),
                Instructions = instructions
            });
        }
        File.WriteAllText(outputPath, System.Text.Json.JsonSerializer.Serialize(methods,
            new System.Text.Json.JsonSerializerOptions { WriteIndented = true }));
    }

    public static async Task RunSlimmingRuntimeProbe(string scenario, int invocations)
    {
        if (scenario is not ("W7-hit" or "W7-miss" or "W7-noop" or "W8") || invocations <= 0)
            throw new ArgumentException("Invalid runtime sample.");
        var root = Path.Combine(Path.GetTempPath(), "aurora-slimming-runtime");
        var engine = new AuroraEngine(EngineOptions.Default
            .WithCompiler(c => { c.SourceResolver = ScriptSources.Memory(root); c.Mode = CompilationMode.Persistence; })
            .WithOptimization(o => { o.Level = OptimizeOptions.Release; o.StackTrace = false; })
            .WithOutput(o => o.AssemblyFile = Path.Combine(Environment.CurrentDirectory, "slimming-runtime.dll")));
        await engine.BuildAsync(new MemorySource(root, "runtime.as", """
            @module(SLIMMING);
            func source() { return 3; }
            export func replace() { source = () => '3'; }
            export func guarded(int32 count) {
                var total = 0;
                for (var i = 0; i < count; i++) total += source() * 2;
                return total;
            }
            export func noop(int32 count) {
                var total = 0;
                for (var i = 0; i < count; i++) { var kind = typeof source(); total += kind.length; }
                return total;
            }
            export native func arrays(int32 count) Number {
                var a = new Int32Array(1);
                var b = new UInt32Array(1);
                var c = new Int64Array(1);
                var d = new UInt64Array(1);
                var e = new Float64Array(1);
                var total = 0;
                for (var i = 0; i < count; i++) {
                    total += a[0]++ + ++a[0]; a[0] -= 2;
                    total += b[0]++ + ++b[0]; b[0] -= 2;
                    total += Number(c[0]++) + Number(++c[0]); c[0] -= 2L;
                    total += Number(d[0]++) + Number(++d[0]); d[0] -= 2UL;
                    total += e[0]++ + ++e[0]; e[0] -= 2;
                }
                return total;
            }
            """));
        using var domain = engine.CreateDomain();
        if (scenario == "W7-miss") domain.Execute("SLIMMING", "replace", ScriptObject.Null, Array.Empty<ScriptDatum>());
        var method = scenario == "W8" ? "arrays" : scenario == "W7-noop" ? "noop" : "guarded";
        var arguments = new[] { ScriptDatum.FromNumber(512) };
        var expected = scenario == "W8" ? 5120 : 3072;
        for (var warmup = 0; warmup < 100; warmup++)
            if (domain.Execute("SLIMMING", method, ScriptObject.Null, arguments).Number != expected)
                throw new InvalidOperationException("Runtime sample result mismatch.");
        Console.WriteLine("Run,ElapsedMs,AllocatedBytes,Gen0,Gen1,Gen2");
        for (var run = 0; run < 16; run++)
        {
            var gen0 = GC.CollectionCount(0);
            var gen1 = GC.CollectionCount(1);
            var gen2 = GC.CollectionCount(2);
            var allocated = GC.GetTotalAllocatedBytes(true);
            var start = System.Diagnostics.Stopwatch.GetTimestamp();
            for (var invocation = 0; invocation < invocations; invocation++)
                if (domain.Execute("SLIMMING", method, ScriptObject.Null, arguments).Number != expected)
                    throw new InvalidOperationException("Runtime sample result changed.");
            var elapsed = System.Diagnostics.Stopwatch.GetElapsedTime(start);
            allocated = GC.GetTotalAllocatedBytes(true) - allocated;
            Console.WriteLine(FormattableString.Invariant(
                $"{run},{elapsed.TotalMilliseconds:F3},{allocated},{GC.CollectionCount(0) - gen0},{GC.CollectionCount(1) - gen1},{GC.CollectionCount(2) - gen2}"));
        }
    }

    private const int Count = 512;
    private ScriptDomain _domain;
    private ScriptDatum[] _comparisonArgs, _cacheArgs, _loopArgs;

    [GlobalSetup]
    public async Task Setup()
    {
        var root = Path.Combine(Path.GetTempPath(), "aurora-generated-boundary-bench");
        var engine = new AuroraEngine(EngineOptions.Default
            .WithCompiler(c => { c.SourceResolver = ScriptSources.Memory(root); c.Mode = CompilationMode.Persistence; })
            .WithOptimization(o => { o.Level = OptimizeOptions.Release; o.StackTrace = false; })
            .WithOutput(o => o.AssemblyFile = Path.Combine(AppContext.BaseDirectory, "generated-boundary-bench.dll")));
        await engine.BuildAsync(new MemorySource(root, "boundary.as", """
            @module(BOUNDARY);
            export func compare(state, Number bound, int32 count) {
                var result = 0;
                for (var i = 0; i < count; i++) {
                    if (state.value < bound) result++;
                    if (bound > state.value) result++;
                }
                return result;
            }
            export func cached(value, int32 count) {
                var result = 0;
                for (var i = 0; i < count; i++) {
                    result += value * 2;
                    if (value < 10) result++;
                    if (i == count / 2) value = '4';
                }
                return result;
            }
            export func iterate(values) {
                var result = 0;
                for (var value in values) result += value;
                return result;
            }
            """));
        _domain = engine.CreateDomain();
        var state = new ScriptObject();
        state.Define("value", ScriptDatum.FromNumber(3));
        var array = new ScriptArray(Count);
        for (var i = 0; i < Count; i++) array[i] = ScriptDatum.FromNumber(1);
        _comparisonArgs = [ScriptDatum.FromObject(state), ScriptDatum.FromNumber(10), ScriptDatum.FromNumber(Count)];
        _cacheArgs = [ScriptDatum.FromNumber(3), ScriptDatum.FromNumber(Count)];
        _loopArgs = [ScriptDatum.FromArray(array)];
        if (Comparison().Number != 2 * Count || CachedNumber().Number != 257 * 7 + 255 * 9 ||
            Iteration().Number != Count) throw new InvalidOperationException("Boundary benchmark result mismatch.");
    }

    [Benchmark(OperationsPerInvoke = Count)]
    public ScriptDatum Comparison() => _domain.Execute("BOUNDARY", "compare", ScriptObject.Null, _comparisonArgs);

    [Benchmark(OperationsPerInvoke = Count)]
    public ScriptDatum CachedNumber() => _domain.Execute("BOUNDARY", "cached", ScriptObject.Null, _cacheArgs);

    [Benchmark(OperationsPerInvoke = Count)]
    public ScriptDatum Iteration() => _domain.Execute("BOUNDARY", "iterate", ScriptObject.Null, _loopArgs);
}
