using AuroraScript;
using AuroraScript.Core;
using AuroraScript.Runtime;
using AuroraScript.Source;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using System;
using System.IO;
using System.Threading.Tasks;

namespace AuroraBenchmark;

/// <summary>Measures script loops, not host invocation overhead. Inputs stay runtime parameters.</summary>
[MemoryDiagnoser]
[ShortRunJob]
[GroupBenchmarksBy(BenchmarkLogicalGroupRule.ByCategory)]
[CategoriesColumn]
public class StringBenchmarks
{
    public const int Operations = 10000;
    private ScriptDomain _domain;
    private ScriptDatum[] _arguments;
    private ScriptDatum[] _wordArguments;
    private ScriptDatum[] _signedArguments;
    private ScriptDatum[] _unsignedArguments;

    [GlobalSetup]
    public async Task Setup()
    {
        var root = AppContext.BaseDirectory;
        var engine = new AuroraEngine(EngineOptions.Default
            .WithCompiler(c => c.SourceResolver = ScriptSources.FileSystem(root))
            .WithCompiler(c => c.Mode = CompilationMode.Dynamic)
            .WithOptimization(o => o.Level = OptimizeOptions.Release)
            .WithOptimization(o => o.StackTrace = false));
        await engine.BuildAsync(new MemorySource(root, "string-bench.as", """
            @module(STRING_BENCH);
            native func sliceInt(String text, int32 start, int32 end) String { return text.substring(start, end); }
            native func sliceNumber(String text, Number start, Number end) String { return text.substring(start, end); }
            func sliceDynamic(text, start, end) { return text.substring(start, end); }
            native func searchNative(String text, String part) Boolean { return text.contains(part); }
            func searchDynamic(text, part) { return text.contains(part); }
            native func normalizeNative(String text) String { return text.trim().toLowerCase(); }
            func normalizeDynamic(text) { return text.trim().toLowerCase(); }
            native func replaceNativeCore(String text) String { return text.replace('Aurora', 'Native'); }
            func replaceDynamicCore(text) { return text.replace('Aurora', 'Native'); }
            native func padNativeCore(String text) String { return text.padLeft(20, '0'); }
            func padDynamicCore(text) { return text.padLeft(20, '0'); }
            func splitNativeCore(String text) { return text.split(' '); }
            func splitDynamicCore(text) { return text.split(' '); }
            func matchNativeCore(String text) { return text.matchAll('Aurora'); }
            func matchDynamicCore(text) { return text.matchAll('Aurora'); }
            native func constructNativeCore(String text) String { return new String(text); }
            func constructDynamicCore(text) { var ctor = String; return new ctor(text); }
            native func wordNative(uint32 value) String {
                var result = ''; var temp = ''; var octet; var count;
                for (count = 0; count <= 3; count++) {
                    octet = (value >> (count * 8)) & 255;
                    temp = '0' + octet.toString(16);
                    result += temp.substring(temp.length - 2, 2);
                }
                return result;
            }
            export func substringInt(Number count, String text) {
                var result = ''; for (var i = 0; i < count; i++) result = sliceInt(text, 2, 8); return result;
            }
            export func substringNumber(Number count, String text) {
                var result = ''; for (var i = 0; i < count; i++) result = sliceNumber(text, 2, 8); return result;
            }
            export func substringDynamic(Number count, String text) {
                var result = ''; for (var i = 0; i < count; i++) result = sliceDynamic(text, 2, 8); return result;
            }
            export func containsNative(Number count, String text) {
                var result = false; for (var i = 0; i < count; i++) result = searchNative(text, 'Aurora'); return result;
            }
            export func containsDynamic(Number count, String text) {
                var result = false; for (var i = 0; i < count; i++) result = searchDynamic(text, 'Aurora'); return result;
            }
            export func chainNative(Number count, String text) {
                var result = ''; for (var i = 0; i < count; i++) result = normalizeNative(text); return result;
            }
            export func chainDynamic(Number count, String text) {
                var result = ''; for (var i = 0; i < count; i++) result = normalizeDynamic(text); return result;
            }
            export func wordToHex(Number count, uint32 value) {
                var result = ''; for (var i = 0; i < count; i++) result = wordNative(value); return result;
            }
            export func replaceNative(Number count, String text) {
                var result = ''; for (var i = 0; i < count; i++) result = replaceNativeCore(text); return result;
            }
            export func replaceDynamic(Number count, String text) {
                var result = ''; for (var i = 0; i < count; i++) result = replaceDynamicCore(text); return result;
            }
            export func padNative(Number count, String text) {
                var result = ''; for (var i = 0; i < count; i++) result = padNativeCore(text); return result;
            }
            export func padDynamic(Number count, String text) {
                var result = ''; for (var i = 0; i < count; i++) result = padDynamicCore(text); return result;
            }
            export func splitNative(Number count, String text) {
                var result; for (var i = 0; i < count; i++) result = splitNativeCore(text); return result.length;
            }
            export func splitDynamic(Number count, String text) {
                var result; for (var i = 0; i < count; i++) result = splitDynamicCore(text); return result.length;
            }
            export func matchNative(Number count, String text) {
                var result; for (var i = 0; i < count; i++) result = matchNativeCore(text); return result[0][0];
            }
            export func matchDynamic(Number count, String text) {
                var result; for (var i = 0; i < count; i++) result = matchDynamicCore(text); return result[0][0];
            }
            export func constructNative(Number count, String text) {
                var result = ''; for (var i = 0; i < count; i++) result = constructNativeCore(text); return result;
            }
            export func constructDynamic(Number count, String text) {
                var result = ''; for (var i = 0; i < count; i++) result = constructDynamicCore(text); return result;
            }
            native func formatSigned64(int64 value) String { return value.toString(16); }
            native func formatUnsigned64(uint64 value) String { return value.toString(16); }
            export func signed64(Number count, int64 value) {
                var result = ''; for (var i = 0; i < count; i++) result = formatSigned64(value); return result;
            }
            export func unsigned64(Number count, uint64 value) {
                var result = ''; for (var i = 0; i < count; i++) result = formatUnsigned64(value); return result;
            }
            """));
        _domain = engine.CreateDomain();
        _arguments = [ScriptDatum.FromNumber(Operations), ScriptDatum.FromString("  Aurora String  ")];
        _wordArguments = [ScriptDatum.FromNumber(Operations), ScriptDatum.FromNumber(0x12345678u)];
        _signedArguments = [ScriptDatum.FromNumber(Operations), ScriptDatum.FromInt64(9007199254740993L)];
        _unsignedArguments = [ScriptDatum.FromNumber(Operations), ScriptDatum.FromUInt64(ulong.MaxValue)];
        Check(SubstringInt(), "Aurora");
        Check(SubstringNumber(), "Aurora");
        Check(SubstringDynamic(), "Aurora");
        Check(ContainsNative(), "True");
        Check(ContainsDynamic(), "True");
        Check(ChainNative(), "aurora string");
        Check(ChainDynamic(), "aurora string");
        Check(WordToHex(), "7531");
        Check(Int64Hex(), "20000000000001");
        Check(UInt64Hex(), "FFFFFFFFFFFFFFFF");
        Check(ReplaceNative(), "  Native String  ");
        Check(ReplaceDynamic(), "  Native String  ");
        Check(PadNative(), "000  Aurora String  ");
        Check(PadDynamic(), "000  Aurora String  ");
        Check(SplitNative(), "6");
        Check(SplitDynamic(), "6");
        Check(MatchNative(), "Aurora");
        Check(MatchDynamic(), "Aurora");
        Check(ConstructNative(), "  Aurora String  ");
        Check(ConstructDynamic(), "  Aurora String  ");
    }

    [GlobalCleanup]
    public void Cleanup() => _domain?.Dispose();

    [Benchmark(OperationsPerInvoke = Operations), BenchmarkCategory("substring")]
    public ScriptDatum SubstringInt() => Execute("substringInt");
    [Benchmark(OperationsPerInvoke = Operations), BenchmarkCategory("substring")]
    public ScriptDatum SubstringNumber() => Execute("substringNumber");
    [Benchmark(Baseline = true, OperationsPerInvoke = Operations), BenchmarkCategory("substring")]
    public ScriptDatum SubstringDynamic() => Execute("substringDynamic");
    [Benchmark(OperationsPerInvoke = Operations), BenchmarkCategory("contains")]
    public ScriptDatum ContainsNative() => Execute("containsNative");
    [Benchmark(Baseline = true, OperationsPerInvoke = Operations), BenchmarkCategory("contains")]
    public ScriptDatum ContainsDynamic() => Execute("containsDynamic");
    [Benchmark(OperationsPerInvoke = Operations), BenchmarkCategory("chain")]
    public ScriptDatum ChainNative() => Execute("chainNative");
    [Benchmark(Baseline = true, OperationsPerInvoke = Operations), BenchmarkCategory("chain")]
    public ScriptDatum ChainDynamic() => Execute("chainDynamic");
    [Benchmark(OperationsPerInvoke = Operations), BenchmarkCategory("wordToHex")]
    public ScriptDatum WordToHex() => _domain.Execute("STRING_BENCH", "wordToHex", arguments: _wordArguments);
    [Benchmark(OperationsPerInvoke = Operations), BenchmarkCategory("exact64")]
    public ScriptDatum Int64Hex() => _domain.Execute("STRING_BENCH", "signed64", arguments: _signedArguments);
    [Benchmark(OperationsPerInvoke = Operations), BenchmarkCategory("exact64")]
    public ScriptDatum UInt64Hex() => _domain.Execute("STRING_BENCH", "unsigned64", arguments: _unsignedArguments);

    [Benchmark(OperationsPerInvoke = Operations), BenchmarkCategory("replace")]
    public ScriptDatum ReplaceNative() => Execute("replaceNative");
    [Benchmark(Baseline = true, OperationsPerInvoke = Operations), BenchmarkCategory("replace")]
    public ScriptDatum ReplaceDynamic() => Execute("replaceDynamic");
    [Benchmark(OperationsPerInvoke = Operations), BenchmarkCategory("pad")]
    public ScriptDatum PadNative() => Execute("padNative");
    [Benchmark(Baseline = true, OperationsPerInvoke = Operations), BenchmarkCategory("pad")]
    public ScriptDatum PadDynamic() => Execute("padDynamic");
    [Benchmark(OperationsPerInvoke = Operations), BenchmarkCategory("split")]
    public ScriptDatum SplitNative() => Execute("splitNative");
    [Benchmark(Baseline = true, OperationsPerInvoke = Operations), BenchmarkCategory("split")]
    public ScriptDatum SplitDynamic() => Execute("splitDynamic");
    [Benchmark(OperationsPerInvoke = Operations), BenchmarkCategory("matchAll")]
    public ScriptDatum MatchNative() => Execute("matchNative");
    [Benchmark(Baseline = true, OperationsPerInvoke = Operations), BenchmarkCategory("matchAll")]
    public ScriptDatum MatchDynamic() => Execute("matchDynamic");
    [Benchmark(OperationsPerInvoke = Operations), BenchmarkCategory("construct")]
    public ScriptDatum ConstructNative() => Execute("constructNative");
    [Benchmark(Baseline = true, OperationsPerInvoke = Operations), BenchmarkCategory("construct")]
    public ScriptDatum ConstructDynamic() => Execute("constructDynamic");

    private ScriptDatum Execute(string name) => _domain.Execute("STRING_BENCH", name, arguments: _arguments);
    private static void Check(ScriptDatum value, string expected)
    {
        var actual = ScriptDatum.ToString(value);
        if (actual != expected) throw new InvalidOperationException($"String benchmark output mismatch: expected '{expected}', actual '{actual}'.");
    }
}
