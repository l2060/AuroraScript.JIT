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
