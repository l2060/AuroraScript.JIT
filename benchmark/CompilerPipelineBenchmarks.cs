using AuroraScript;
using AuroraScript.Compiler.Analyzer;
using AuroraScript.Compiler.Ast;
using AuroraScript.Compiler.Backend;
using AuroraScript.Compiler.Backend.Builders;
using AuroraScript.Compiler.Backend.Emission;
using AuroraScript.Core;
using AuroraScript.Source;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Mathematics;
using BenchmarkDotNet.Order;
using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace AuroraBenchmark
{
    [MemoryDiagnoser]
    [ShortRunJob]
    [MarkdownExporter, JsonExporter, CsvExporter]
    [MinColumn, MaxColumn, MeanColumn, MedianColumn]
    [GroupBenchmarksBy(BenchmarkLogicalGroupRule.ByCategory)]
    [Orderer(SummaryOrderPolicy.FastestToSlowest)]
    [RankColumn(NumeralSystem.Arabic)]
    [CategoriesColumn]
    public class CompilerPipelineBenchmarks
    {
#pragma warning disable CS8618
        private string baseDirectory;
        private string smallSource;
        private string largeSource;
        private string commentsWhitespaceSource;
        private string stringsTemplatesRegexSource;
        private string unicodeIdentifiersSource;
        private string compileBlockSource;
        private string astarSource;
        private string md5Source;
        private string multiModuleMainPath;
        private EngineOptions benchmarkOptions;
        private ModuleDeclaration[] parsedLargeModules;
#pragma warning restore CS8618

        [GlobalSetup]
        public void Setup()
        {
            baseDirectory = Path.Combine(AppContext.BaseDirectory, "compiler-benchmark-scripts");
            Directory.CreateDirectory(baseDirectory);

            smallSource = CreateSmallSource();
            largeSource = CreateLargeSource(180);
            commentsWhitespaceSource = CreateCommentsWhitespaceSource(600);
            stringsTemplatesRegexSource = CreateStringsTemplatesRegexSource(140);
            unicodeIdentifiersSource = CreateUnicodeIdentifierSource(260);
            compileBlockSource = CreateCompileBlockSource();
            astarSource = File.ReadAllText(
                Path.Combine(AppContext.BaseDirectory, "real-scripts", "astar.as"),
                Encoding.UTF8);
            md5Source = File.ReadAllText(
                Path.Combine(AppContext.BaseDirectory, "real-scripts", "md5.as"),
                Encoding.UTF8);
            CreateMultiModuleScripts();
            benchmarkOptions = CreateOptions();
            parsedLargeModules = new[] { (ModuleDeclaration)Parse("emit_large.as", largeSource) };
        }

        public int GetSourceBytes(string benchmarkName)
        {
            if (benchmarkName.Contains("Large", StringComparison.Ordinal)) return Encoding.UTF8.GetByteCount(largeSource);
            if (benchmarkName.Contains("SingleModule", StringComparison.Ordinal)) return Encoding.UTF8.GetByteCount(largeSource);
            if (benchmarkName.Contains("CommentsWhitespace", StringComparison.Ordinal)) return Encoding.UTF8.GetByteCount(commentsWhitespaceSource);
            if (benchmarkName.Contains("StringsTemplatesRegex", StringComparison.Ordinal)) return Encoding.UTF8.GetByteCount(stringsTemplatesRegexSource);
            if (benchmarkName.Contains("TemplateInterpolation", StringComparison.Ordinal)) return Encoding.UTF8.GetByteCount(stringsTemplatesRegexSource);
            if (benchmarkName.Contains("UnicodeIdentifiers", StringComparison.Ordinal)) return Encoding.UTF8.GetByteCount(unicodeIdentifiersSource);
            if (benchmarkName.Contains("CompileBlock", StringComparison.Ordinal)) return Encoding.UTF8.GetByteCount(compileBlockSource);
            if (benchmarkName.Contains("MultiModule", StringComparison.Ordinal)) return GetFileBytes(multiModuleMainPath) + GetFileBytes(Path.Combine(baseDirectory, "dep.as"));
            if (benchmarkName.Contains("RealAstar", StringComparison.Ordinal)) return Encoding.UTF8.GetByteCount(astarSource);
            if (benchmarkName.Contains("RealMd5", StringComparison.Ordinal)) return Encoding.UTF8.GetByteCount(md5Source);
            return Encoding.UTF8.GetByteCount(smallSource);
        }

        [BenchmarkCategory("lexer")]
        [Benchmark]
        public int LexerOnly_Small()
        {
            return Lex("small.as", smallSource);
        }

        [BenchmarkCategory("lexer")]
        [Benchmark]
        public int LexerOnly_Large()
        {
            return Lex("large.as", largeSource);
        }

        [BenchmarkCategory("lexer")]
        [Benchmark]
        public int LexerOnly_CommentsWhitespace()
        {
            return Lex("comments_whitespace.as", commentsWhitespaceSource);
        }

        [BenchmarkCategory("lexer")]
        [Benchmark]
        public int LexerOnly_StringsTemplatesRegex()
        {
            return Lex("strings_templates_regex.as", stringsTemplatesRegexSource);
        }

        [BenchmarkCategory("lexer")]
        [Benchmark]
        public int LexerOnly_UnicodeIdentifiers()
        {
            return Lex("unicode_identifiers.as", unicodeIdentifiersSource);
        }

        [BenchmarkCategory("parser")]
        [Benchmark]
        public object ParseOnly_Small()
        {
            return Parse("small.as", smallSource);
        }

        [BenchmarkCategory("parser")]
        [Benchmark]
        public object ParseOnly_Large()
        {
            return Parse("large.as", largeSource);
        }

        [BenchmarkCategory("parser")]
        [Benchmark]
        public object ParseOnly_TemplateInterpolation()
        {
            return Parse("strings_templates_regex.as", stringsTemplatesRegexSource);
        }

        // Excludes parsing, but includes planning, binding, type analysis and IL emission.
        [BenchmarkCategory("emitter")]
        [Benchmark]
        public void EmitOnly_ParsedLargeModule()
        {
            var builder = new DynamicBuilder(benchmarkOptions);
            var backend = new BackendCompiler(builder, benchmarkOptions);
            var compileSession = backend.CreateModulePlans(parsedLargeModules);
            new BackendBuildEmitter(new EmissionSession(compileSession, builder, emitExecutableCode: true)).Emit();
        }

        [BenchmarkCategory("compile")]
        [Benchmark]
        public async Task FullCompile_SingleModule()
        {
            var options = CreateOptions();
            var engine = new AuroraEngine(options);
            await engine.BuildAsync(new MemorySource(baseDirectory, Path.Combine(baseDirectory, "single.as"), largeSource));
        }

        [BenchmarkCategory("compile")]
        [Benchmark]
        public async Task FullCompile_MultiModule()
        {
            var options = CreateOptions();
            var engine = new AuroraEngine(options);
            await engine.BuildAsync("main.as");
        }

        [BenchmarkCategory("compile")]
        [Benchmark]
        public async Task FullCompile_RealAstar()
        {
            var options = CreateOptions()
                .WithPackages(packages => packages.Add(NativePackages.FileSystem));
            var engine = new AuroraEngine(options);
            await engine.BuildAsync(new MemorySource(
                baseDirectory,
                Path.Combine(baseDirectory, "real-astar.as"),
                astarSource));
        }

        [BenchmarkCategory("compile")]
        [Benchmark]
        public async Task FullCompile_RealMd5()
        {
            var options = CreateOptions();
            var engine = new AuroraEngine(options);
            await engine.BuildAsync(new MemorySource(
                baseDirectory,
                Path.Combine(baseDirectory, "real-md5.as"),
                md5Source));
        }

        [BenchmarkCategory("compile")]
        [Benchmark]
        public void CompileBlock()
        {
            var engine = new AuroraEngine(CreateOptions());
            engine.CompileBlock(compileBlockSource);
        }

        private static int GetFileBytes(string path)
        {
            return File.Exists(path) ? checked((int)new FileInfo(path).Length) : 0;
        }

        private AuroraLexer CreateLexer(string fileName, string source)
        {
            var fullPath = Path.Combine(baseDirectory, fileName);
            return new AuroraLexer(baseDirectory, new MemorySource(baseDirectory, fullPath, source));
        }

        private int Lex(string fileName, string source)
        {
            using var lexer = CreateLexer(fileName, source);
            return lexer.TokenCount;
        }

        private object Parse(string fileName, string source)
        {
            var lexer = CreateLexer(fileName, source);
            var parser = new AuroraParser(lexer, CreateOptions());
            return parser.Parse();
        }

        private EngineOptions CreateOptions()
        {
            return EngineOptions.Default
                .WithCompiler(compiler => compiler.SourceResolver = ScriptSources.FileSystem(baseDirectory, Encoding.UTF8))
                .WithCompiler(compiler => compiler.Mode = CompilationMode.Dynamic)
                .WithOptimization(optimization => optimization.Level = OptimizeOptions.Release)
                .WithRuntime(runtime => runtime.ConsoleStdOut = TextWriter.Null)
                .WithRuntime(runtime => runtime.ConsoleErrorOut = TextWriter.Null);
        }

        private void CreateMultiModuleScripts()
        {
            var dependencyPath = Path.Combine(baseDirectory, "dep.as");
            multiModuleMainPath = Path.Combine(baseDirectory, "main.as");

            File.WriteAllText(dependencyPath, """
@module(DEP_BENCH);

export func inc(value) {
    return value + 1;
}

export func add(a, b) {
    return a + b;
}
""", Encoding.UTF8);

            File.WriteAllText(multiModuleMainPath, """
@module(MAIN_BENCH);

import dep from 'dep';

export func run(count = 100) {
    var sum = 0;
    for (var i = 0; i < count; i++) {
        sum = dep.add(sum, dep.inc(i));
    }
    return sum;
}
""", Encoding.UTF8);
        }

        private static string CreateSmallSource()
        {
            return """
@module(SMALL_BENCH);

const seed = 1;

export func run(value = 10) {
    var total = seed;
    for (var i = 0; i < value; i++) {
        total = total + i;
    }
    return total;
}
""";
        }

        public static async Task RunSlimmingProbe(string scenario, int size)
        {
            if (size <= 0 || scenario is not ("W1" or "W2" or "W2-native" or "W3" or "W3-native" or
                "W4" or "W5" or "W6" or "W9" or "W10-astar" or "W10-md5")) throw new ArgumentException("Invalid slimming sample.");
            if (scenario == "W4" && size is not (1 or 10 or 100)) throw new ArgumentException("W4 uses 1, 10 or 100 modules.");
            var benchmark = new CompilerPipelineBenchmarks();
            benchmark.Setup();
            Func<Task> compile;
            if (scenario == "W10-astar") compile = benchmark.FullCompile_RealAstar;
            else if (scenario == "W10-md5") compile = benchmark.FullCompile_RealMd5;
            else if (scenario == "W9")
            {
                var root = Path.Combine(benchmark.baseDirectory, "shared-import");
                Directory.CreateDirectory(root);
                File.WriteAllText(Path.Combine(root, "shared.as"), "@module(SHARED); export const value = 7;", Encoding.UTF8);
                var sources = new ScriptSource[size];
                for (var index = 0; index < size; index++)
                {
                    var path = Path.Combine(root, $"importer{index}.as");
                    File.WriteAllText(path, $"@module(IMPORTER{index}); import shared from './shared'; export func run() {{ return shared.value; }}", Encoding.UTF8);
                    sources[index] = new FileSource(root, path, Encoding.UTF8);
                }
                var options = benchmark.CreateOptions().WithCompiler(c => c.SourceResolver = ScriptSources.FileSystem(root, Encoding.UTF8));
                compile = () => new AuroraEngine(options).BuildAsync(sources);
            }
            else
            {
                var moduleCount = scenario == "W4" ? size : 1;
                var functionCount = scenario == "W4" ? 1000 / moduleCount : size;
                var sources = new ScriptSource[moduleCount];
                for (var module = 0; module < moduleCount; module++)
                {
                    var source = new StringBuilder($"@module(PROBE{module});\n");
                    if (scenario == "W6")
                    {
                        source.Append("export func run(Number x) { var s = 'abc'; return [Math.abs(x), Math.pow(x, 2), s.substring(0), s.substring(0, 1, 2), Math.abs(...[x])]; }\n");
                        functionCount = 0;
                    }
                    var native = scenario.EndsWith("-native", StringComparison.Ordinal);
                    if (scenario.StartsWith("W3", StringComparison.Ordinal))
                    {
                        for (var function = 0; function < size; function++)
                            source.Append($"{(native ? "native " : "")}func r{function}(Number n){(native ? " Number" : "")} {{ if (n <= 0) return 1; return r{(function + 1) % size}(n - 1); }}\n");
                        functionCount = 500;
                    }
                    for (var function = 0; function < functionCount; function++)
                    {
                        if (scenario == "W5")
                        {
                            var body = (function % 4) switch
                            {
                                0 => "const value = 7; return () => value;",
                                1 => "var value = 7; var get = () => value; value++; return get;",
                                2 => "var get = () => value; var value = 7; return get;",
                                _ => "const value = 7; return () => () => value;"
                            };
                            source.Append($"func f{function}() {{ {body} }}\n");
                            continue;
                        }
                        var chain = scenario.StartsWith("W2", StringComparison.Ordinal);
                        source.Append($"{(native && chain ? "native " : "")}func f{function}(){(native && chain ? " Number" : "")} {{ return ");
                        source.Append(chain && function + 1 < functionCount ? $"f{function + 1}()" : "7");
                        source.Append("; }\n");
                    }
                    sources[module] = new MemorySource(benchmark.baseDirectory,
                        Path.Combine(benchmark.baseDirectory, $"probe{module}.as"), source.ToString());
                }
                compile = () => new AuroraEngine(benchmark.CreateOptions()).BuildAsync(sources);
            }
            Console.WriteLine("Run,ElapsedMs,AllocatedBytes,Gen0,Gen1,Gen2");
            for (var run = 0; run < 16; run++)
            {
                var gen0 = GC.CollectionCount(0);
                var gen1 = GC.CollectionCount(1);
                var gen2 = GC.CollectionCount(2);
                var allocated = GC.GetTotalAllocatedBytes(true);
                var start = System.Diagnostics.Stopwatch.GetTimestamp();
                await compile();
                var elapsed = System.Diagnostics.Stopwatch.GetElapsedTime(start);
                allocated = GC.GetTotalAllocatedBytes(true) - allocated;
                Console.WriteLine(FormattableString.Invariant(
                    $"{run},{elapsed.TotalMilliseconds:F3},{allocated},{GC.CollectionCount(0) - gen0},{GC.CollectionCount(1) - gen1},{GC.CollectionCount(2) - gen2}"));
            }
        }

        private static string CreateLargeSource(int functions)
        {
            var builder = new StringBuilder(functions * 420);
            builder.AppendLine("@module(LARGE_BENCH);");
            builder.AppendLine();
            builder.AppendLine("const moduleSeed = 7;");
            for (var i = 0; i < functions; i++)
            {
                builder.Append("export func f").Append(i).AppendLine("(count = 64) {");
                builder.Append("    var total = moduleSeed + ").Append(i).AppendLine(";");
                builder.AppendLine("    var local = { a: total, b: total + 1, c: total + 2 };");
                builder.AppendLine("    for (var n = 0; n < count; n++) {");
                builder.AppendLine("        total = total + local.a + local.b - local.c + n;");
                builder.AppendLine("        if (total > 100000) { total = total % 97; }");
                builder.AppendLine("    }");
                builder.AppendLine("    return total;");
                builder.AppendLine("}");
                builder.AppendLine();
            }
            return builder.ToString();
        }

        private static string CreateCommentsWhitespaceSource(int blocks)
        {
            var builder = new StringBuilder(blocks * 160);
            builder.AppendLine("@module(COMMENTS_WHITESPACE_BENCH);");
            builder.AppendLine();
            builder.AppendLine("export func run() {");
            builder.AppendLine("    var value = 0;");
            for (var i = 0; i < blocks; i++)
            {
                builder.AppendLine("    // scanner comment workload");
                builder.AppendLine("    /* block comment line 1");
                builder.AppendLine("       block comment line 2");
                builder.AppendLine("       block comment line 3 */");
                builder.Append("    value = value + ").Append(i % 17).AppendLine(";");
            }
            builder.AppendLine("    return value;");
            builder.AppendLine("}");
            return builder.ToString();
        }

        private static string CreateStringsTemplatesRegexSource(int blocks)
        {
            var builder = new StringBuilder(blocks * 280);
            builder.AppendLine("@module(STRINGS_TEMPLATES_REGEX_BENCH);");
            builder.AppendLine();
            builder.AppendLine("export func run(name = 'aurora') {");
            builder.AppendLine("    var output = '';");
            builder.AppendLine("    var matcher = /[a-z_]+/g;");
            for (var i = 0; i < blocks; i++)
            {
                builder.Append("    var text").Append(i).Append(" = `hello ${name} #").Append(i).AppendLine("`;");
                builder.Append("    output = output + text").Append(i).AppendLine(" + matcher.test(name);");
            }
            builder.AppendLine("    return output;");
            builder.AppendLine("}");
            return builder.ToString();
        }

        private static string CreateUnicodeIdentifierSource(int declarations)
        {
            var builder = new StringBuilder(declarations * 110);
            builder.AppendLine("@module(UNICODE_IDENTIFIERS_BENCH);");
            builder.AppendLine();
            builder.AppendLine("export func run() {");
            builder.AppendLine("    var total = 0;");
            for (var i = 0; i < declarations; i++)
            {
                builder.Append("    var name").Append(i).Append(" = ").Append(i).AppendLine(";");
                builder.Append("    total = total + name").Append(i).AppendLine(";");
            }
            builder.AppendLine("    return total;");
            builder.AppendLine("}");
            return builder.ToString();
        }

        private static string CreateCompileBlockSource()
        {
            return """
var total = 0;
for (var i = 0; i < 256; i++) {
    total = total + i;
}
return total;
""";
        }
    }
}
