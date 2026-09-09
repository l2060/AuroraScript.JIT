using AuroraScript;
using AuroraScript.Compiler;
using AuroraScript.Compiler.Backend;
using AuroraScript.Compiler.Backend.Builders;
using AuroraScript.Compiler.Backend.Code;
using AuroraScript.Core;
using AuroraScript.Source;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace AuroraBenchmark;

internal static class ExampleCompilationProfile
{
    public static async Task RunAsync(string assemblyPath)
    {
        // Reuse host types and settings; AppContext belongs to this benchmark process.
        var assembly = Assembly.LoadFrom(Path.GetFullPath(assemblyPath));
        var program = assembly.GetType("Examples.Program", throwOnError: true);
        var engine = (AuroraEngine)program.GetField("engine", BindingFlags.NonPublic | BindingFlags.Static).GetValue(null);
        var root = Path.Combine(Path.GetDirectoryName(assembly.Location), "tests");
        var options = new AuroraEngine(engine.Options.WithCompiler(compiler =>
            compiler.SourceResolver = ScriptSources.Composite(
                ScriptSources.Memory(root).Add("seed.as", "console.log('load from memory overlay'); export func go(){ console.log('seed from memory...');  }"),
                ScriptSources.FileSystem(root, Encoding.UTF8)))).Options;
        Console.WriteLine("Run,Stage,ElapsedMs,AllocatedBytes");
        for (var run = 0; run < 4; run++)
        {
            var allocated = GC.GetTotalAllocatedBytes(true);
            var start = Stopwatch.GetTimestamp();
            var sources = new List<ScriptSource>();
            await foreach (var source in options.Compiler.SourceResolver.GetAllSourcesAsync(
                new ScriptSourceQuery(options.Compiler.ExtName, Encoding.UTF8)))
                sources.Add(source);
            Report(run, "discover", ref start, ref allocated);
            var compiler = new ScriptCompiler(options);
            var modules = await compiler.BuildModuleGraphAsync(sources.ToArray());
            Report(run, "parse-link", ref start, ref allocated);
            var backend = new BackendCompiler(new PersistedBuilder(options), options, compiler.GlobalDeclarations);
            var session = backend.CreateModulePlans(modules);
            Report(run, "bind-plan", ref start, ref allocated);
            CallableReturnPredictions.Build(session.Modules, session.HostExports);
            Report(run, "types-and-return-prediction", ref start, ref allocated);
        }
    }

    private static void Report(int run, string stage, ref long start, ref long allocated)
    {
        var elapsed = Stopwatch.GetElapsedTime(start);
        var bytes = GC.GetTotalAllocatedBytes(true) - allocated;
        Console.WriteLine(FormattableString.Invariant($"{run},{stage},{elapsed.TotalMilliseconds:F3},{bytes}"));
        allocated = GC.GetTotalAllocatedBytes(true);
        start = Stopwatch.GetTimestamp();
    }
}
