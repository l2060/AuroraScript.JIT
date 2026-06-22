using AuroraScript;
using AuroraScript.Compiler.Analyzer;
using AuroraScript.Compiler.Ast;
using AuroraScript.Compiler.Emits;
using AuroraScript.Compiler.Emits.Builders;
using AuroraScript.Core;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Columns;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Exporters;
using BenchmarkDotNet.Mathematics;
using BenchmarkDotNet.Order;
using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace CompilerBenchmark
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
        private string _baseDirectory;
        private string _smallSource;
        private string _largeSource;
        private string _commentsWhitespaceSource;
        private string _stringsTemplatesRegexSource;
        private string _unicodeIdentifiersSource;
        private string _compileBlockSource;
        private string _multiModuleMainPath;
        private EngineOptions _benchmarkOptions;
        private ModuleDeclaration[] _parsedLargeModules;

        [GlobalSetup]
        public void Setup()
        {
            _baseDirectory = Path.Combine(AppContext.BaseDirectory, "compiler-benchmark-scripts");
            Directory.CreateDirectory(_baseDirectory);

            _smallSource = CreateSmallSource();
            _largeSource = CreateLargeSource(180);
            _commentsWhitespaceSource = CreateCommentsWhitespaceSource(600);
            _stringsTemplatesRegexSource = CreateStringsTemplatesRegexSource(140);
            _unicodeIdentifiersSource = CreateUnicodeIdentifierSource(260);
            _compileBlockSource = CreateCompileBlockSource();
            CreateMultiModuleScripts();
            _benchmarkOptions = CreateOptions();
            _parsedLargeModules = new[] { (ModuleDeclaration)Parse("emit_large.as", _largeSource) };
        }

        public int GetSourceBytes(string benchmarkName)
        {
            if (benchmarkName.Contains("Large", StringComparison.Ordinal)) return Encoding.UTF8.GetByteCount(_largeSource);
            if (benchmarkName.Contains("SingleModule", StringComparison.Ordinal)) return Encoding.UTF8.GetByteCount(_largeSource);
            if (benchmarkName.Contains("CommentsWhitespace", StringComparison.Ordinal)) return Encoding.UTF8.GetByteCount(_commentsWhitespaceSource);
            if (benchmarkName.Contains("StringsTemplatesRegex", StringComparison.Ordinal)) return Encoding.UTF8.GetByteCount(_stringsTemplatesRegexSource);
            if (benchmarkName.Contains("TemplateInterpolation", StringComparison.Ordinal)) return Encoding.UTF8.GetByteCount(_stringsTemplatesRegexSource);
            if (benchmarkName.Contains("UnicodeIdentifiers", StringComparison.Ordinal)) return Encoding.UTF8.GetByteCount(_unicodeIdentifiersSource);
            if (benchmarkName.Contains("CompileBlock", StringComparison.Ordinal)) return Encoding.UTF8.GetByteCount(_compileBlockSource);
            if (benchmarkName.Contains("MultiModule", StringComparison.Ordinal)) return GetFileBytes(_multiModuleMainPath) + GetFileBytes(Path.Combine(_baseDirectory, "dep.as"));
            return Encoding.UTF8.GetByteCount(_smallSource);
        }

        private static int GetFileBytes(string path)
        {
            return File.Exists(path) ? checked((int)new FileInfo(path).Length) : 0;
        }

        [BenchmarkCategory("lexer")]
        [Benchmark]
        public int LexerOnly_Small()
        {
            return Lex("small.as", _smallSource);
        }

        [BenchmarkCategory("lexer")]
        [Benchmark]
        public int LexerOnly_Large()
        {
            return Lex("large.as", _largeSource);
        }

        [BenchmarkCategory("lexer")]
        [Benchmark]
        public int LexerOnly_CommentsWhitespace()
        {
            return Lex("comments_whitespace.as", _commentsWhitespaceSource);
        }

        [BenchmarkCategory("lexer")]
        [Benchmark]
        public int LexerOnly_StringsTemplatesRegex()
        {
            return Lex("strings_templates_regex.as", _stringsTemplatesRegexSource);
        }

        [BenchmarkCategory("lexer")]
        [Benchmark]
        public int LexerOnly_UnicodeIdentifiers()
        {
            return Lex("unicode_identifiers.as", _unicodeIdentifiersSource);
        }

        [BenchmarkCategory("parser")]
        [Benchmark]
        public object ParseOnly_Small()
        {
            return Parse("small.as", _smallSource);
        }

        [BenchmarkCategory("parser")]
        [Benchmark]
        public object ParseOnly_Large()
        {
            return Parse("large.as", _largeSource);
        }

        [BenchmarkCategory("parser")]
        [Benchmark]
        public object ParseOnly_TemplateInterpolation()
        {
            return Parse("strings_templates_regex.as", _stringsTemplatesRegexSource);
        }

        [BenchmarkCategory("emitter")]
        [Benchmark]
        public void EmitOnly_ParsedLargeModule()
        {
            var builder = new DynamicBuilder(_benchmarkOptions);
            var emitter = new CILEmitter(builder, _benchmarkOptions);
            emitter.Visit(_parsedLargeModules);
        }

        [BenchmarkCategory("compile")]
        [Benchmark]
        public async Task FullCompile_SingleModule()
        {
            var options = CreateOptions();
            var engine = new AuroraEngine(options);
            await engine.BuildAsync(new TextSource(_baseDirectory, Path.Combine(_baseDirectory, "single.as"), _largeSource));
        }

        [BenchmarkCategory("compile")]
        [Benchmark]
        public async Task FullCompile_MultiModule()
        {
            var options = CreateOptions();
            var engine = new AuroraEngine(options);
            await engine.BuildAsync(engine.FileSource(_multiModuleMainPath, Encoding.UTF8));
        }

        [BenchmarkCategory("compile")]
        [Benchmark]
        public void CompileBlock()
        {
            var engine = new AuroraEngine(CreateOptions());
            engine.CompileBlock(_compileBlockSource);
        }

        private AuroraLexer CreateLexer(string fileName, string source)
        {
            var fullPath = Path.Combine(_baseDirectory, fileName);
            return new AuroraLexer(_baseDirectory, new TextSource(_baseDirectory, fullPath, source));
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
                .WithBaseDirectory(_baseDirectory)
                .WithCompilationMode(CompilationMode.Dynamic)
                .WithOptimizeOption(OptimizeOptions.Release)
                .WithConsoleStdOut(TextWriter.Null)
                .WithConsoleErrorOut(TextWriter.Null);
        }

        private void CreateMultiModuleScripts()
        {
            var dependencyPath = Path.Combine(_baseDirectory, "dep.as");
            _multiModuleMainPath = Path.Combine(_baseDirectory, "main.as");

            File.WriteAllText(dependencyPath, """
@module(DEP_BENCH);

export func inc(value) {
    return value + 1;
}

export func add(a, b) {
    return a + b;
}
""", Encoding.UTF8);

            File.WriteAllText(_multiModuleMainPath, """
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
                builder.AppendLine("    // this is a row comment with enough text to exercise scanning");
                builder.AppendLine("    /* block comment line 1");
                builder.AppendLine("       block comment line 2");
                builder.AppendLine("       block comment line 3 */");
                builder.AppendLine();
                builder.Append("    value = value + ").Append(i % 17).AppendLine(";");
                builder.AppendLine();
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
            builder.AppendLine("    var 总数 = 0;");
            for (var i = 0; i < declarations; i++)
            {
                builder.Append("    var 名称").Append(i).Append(" = ").Append(i).AppendLine(";");
                builder.Append("    总数 = 总数 + 名称").Append(i).AppendLine(";");
            }
            builder.AppendLine("    return 总数;");
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
