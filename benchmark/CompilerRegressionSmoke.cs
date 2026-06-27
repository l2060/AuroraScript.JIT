using AuroraScript;
using System;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace AuroraBenchmark
{
    internal static class CompilerRegressionSmoke
    {
        public static async Task RunAsync()
        {
            var root = Path.Combine(Path.GetTempPath(), "aurora-compiler-regression-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(root);
            try
            {
                await CompileWideDependencyGraph(root);
                await AggregateParallelErrors(root);
                await RejectCircularDependencies(root);
                await HonorCancellation(root);
            }
            finally
            {
                Directory.Delete(root, recursive: true);
            }
        }

        private static async Task CompileWideDependencyGraph(string root)
        {
            var directory = CreateCaseDirectory(root, "wide");
            var main = new StringBuilder("@module(WIDE_MAIN);\n");
            for (var i = 0; i < 24; i++)
            {
                var moduleName = "WIDE_DEP_" + i;
                var fileName = "dep" + i;
                File.WriteAllText(
                    Path.Combine(directory, fileName + ".as"),
                    $"@module({moduleName});\nexport var value = {i};\n",
                    Encoding.UTF8);
                main.Append("import dep").Append(i).Append(" from '").Append(fileName).Append("';\n");
            }
            File.WriteAllText(Path.Combine(directory, "main.as"), main.ToString(), Encoding.UTF8);

            var engine = CreateEngine(directory, maxDegreeOfParallelism: 4);
            var source = engine.FileSource("main.as", Encoding.UTF8);
            await engine.BuildAsync(source, source);
            engine.CreateDomain();
        }

        private static async Task AggregateParallelErrors(string root)
        {
            var directory = CreateCaseDirectory(root, "errors");
            File.WriteAllText(
                Path.Combine(directory, "main.as"),
                "@module(ERROR_MAIN);\nimport first from 'first';\nimport second from 'second';\n",
                Encoding.UTF8);
            File.WriteAllText(Path.Combine(directory, "first.as"), "@module(ERROR_FIRST);\nvar = ;\n", Encoding.UTF8);
            File.WriteAllText(Path.Combine(directory, "second.as"), "@module(ERROR_SECOND);\nfunc ( {\n", Encoding.UTF8);

            var engine = CreateEngine(directory, maxDegreeOfParallelism: 4);
            var compile = engine.BuildAsync(engine.FileSource("main.as", Encoding.UTF8));
            var completed = await Task.WhenAny(compile, Task.Delay(TimeSpan.FromSeconds(10)));
            if (!ReferenceEquals(completed, compile))
            {
                throw new InvalidOperationException("Parallel compilation did not terminate after module errors.");
            }

            try
            {
                await compile;
                throw new InvalidOperationException("Invalid modules compiled successfully.");
            }
            catch (AuroraCompilationException report) when (report.Diagnostics.Count == 2)
            {
            }
        }

        private static async Task RejectCircularDependencies(string root)
        {
            var directory = CreateCaseDirectory(root, "cycle");
            File.WriteAllText(Path.Combine(directory, "a.as"), "@module(CYCLE_A);\nimport b from 'b';\n", Encoding.UTF8);
            File.WriteAllText(Path.Combine(directory, "b.as"), "@module(CYCLE_B);\nimport a from 'a';\n", Encoding.UTF8);

            var engine = CreateEngine(directory, maxDegreeOfParallelism: 2);
            try
            {
                await engine.BuildAsync(engine.FileSource("a.as", Encoding.UTF8));
                throw new InvalidOperationException("Circular dependencies compiled successfully.");
            }
            catch (AuroraCompilationException ex) when (ex.Message.Contains("Circular module dependency", StringComparison.Ordinal))
            {
            }
        }

        private static async Task HonorCancellation(string root)
        {
            var directory = CreateCaseDirectory(root, "cancellation");
            File.WriteAllText(Path.Combine(directory, "main.as"), "@module(CANCEL_MAIN);\n", Encoding.UTF8);
            var engine = CreateEngine(directory, maxDegreeOfParallelism: 2);
            using var cancellation = new CancellationTokenSource();
            cancellation.Cancel();
            try
            {
                await engine.BuildAsync(cancellation.Token, engine.FileSource("main.as", Encoding.UTF8));
                throw new InvalidOperationException("A canceled compilation completed successfully.");
            }
            catch (OperationCanceledException)
            {
            }
        }

        private static AuroraEngine CreateEngine(string directory, int maxDegreeOfParallelism)
        {
            var options = EngineOptions.Default
                .WithCompiler(compiler => compiler.WithDirectory(directory))
                .WithCompiler(compiler => compiler.Mode = CompilationMode.Dynamic)
                .WithRuntime(runtime => runtime.HotReload = false)
                .WithCompiler(compiler => compiler.MaxDegreeOfParallelism = maxDegreeOfParallelism)
                .WithRuntime(runtime => runtime.ConsoleStdOut = TextWriter.Null)
                .WithRuntime(runtime => runtime.ConsoleErrorOut = TextWriter.Null);
            return new AuroraEngine(options);
        }

        private static string CreateCaseDirectory(string root, string name)
        {
            var directory = Path.Combine(root, name);
            Directory.CreateDirectory(directory);
            return directory;
        }
    }
}
