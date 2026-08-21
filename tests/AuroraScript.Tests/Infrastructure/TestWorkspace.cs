using AuroraScript.Runtime;
using AuroraScript.Runtime.Types;
using AuroraScript.Core;
using System;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using AuroraScript.Source;

namespace AuroraScript.Tests.Infrastructure;

internal sealed class TestWorkspace : IDisposable
{
    public TestWorkspace()
    {
        Root = Path.Combine(Path.GetTempPath(), "aurora-tests-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(Root);
    }

    public string Root { get; }

    public string WriteSource(string relativePath, string source)
    {
        var path = Path.Combine(Root, relativePath);
        var directory = Path.GetDirectoryName(path);
        if (!string.IsNullOrEmpty(directory))
        {
            Directory.CreateDirectory(directory);
        }
        File.WriteAllText(path, source, Encoding.UTF8);
        return path;
    }

    public ScriptSource MemorySource(string relativePath, string source)
    {
        return new MemorySource(Root, relativePath, source);
    }

    public AuroraEngine CreateEngine(
        CompilationMode mode = CompilationMode.Dynamic,
        bool enableHotReload = false,
        bool enableConfused = false,
        int maxDegreeOfParallelism = 4,
        string? assemblyOut = null,
        TextWriter? output = null,
        bool enableModuleConstInlining = false,
        bool stackTrace = true,
        string? dateTimeFormat = null)
    {
        var options = EngineOptions.Default
            .WithCompiler(compiler => compiler.SourceResolver = AuroraScript.Core.ScriptSources.FileSystem(Root))
            .WithCompiler(compiler => compiler.Mode = mode)
            .WithOptimization(optimization => optimization.Level = OptimizeOptions.Release)
            .WithRuntime(runtime => runtime.HotReload = enableHotReload)
            .WithOptimization(optimization => optimization.ModuleConstInlining = enableModuleConstInlining)
            .WithOptimization(optimization => optimization.StackTrace = stackTrace)
            .WithOutput(output => output.Confused = enableConfused)
            .WithCompiler(compiler => compiler.MaxDegreeOfParallelism = maxDegreeOfParallelism)
            .WithRuntime(runtime => runtime.ConsoleStdOut = output ?? TextWriter.Null)
            .WithRuntime(runtime => runtime.ConsoleErrorOut = output ?? TextWriter.Null);

        if (dateTimeFormat != null)
        {
            options = options.WithRuntime(runtime => runtime.DateTimeFormat = dateTimeFormat);
        }

        if (!string.IsNullOrEmpty(assemblyOut))
        {
            options = options.WithOutput(output => output.AssemblyFile = assemblyOut);
        }
        return new AuroraEngine(options);
    }

    public async Task<(AuroraEngine Engine, ScriptDomain Domain)> CompileModuleAsync(
        string source,
        CompilationMode mode = CompilationMode.Dynamic,
        Action<ScriptGlobal>? configureGlobal = null,
        bool enableHotReload = false,
        bool enableConfused = false,
        int maxDegreeOfParallelism = 4,
        CancellationToken cancellationToken = default,
        string? dateTimeFormat = null)
    {
        var assemblyOut = mode == CompilationMode.Persistence ? Path.Combine(Root, "test-output.dll") : null;
        var engine = CreateEngine(
            mode,
            enableHotReload,
            enableConfused,
            maxDegreeOfParallelism,
            assemblyOut,
            dateTimeFormat: dateTimeFormat);
        WriteSource("main.as", source);
        await engine.BuildAsync(["main.as"], cancellationToken);
        var domain = configureGlobal == null ? engine.CreateDomain() : engine.CreateDomain(configureGlobal);
        return (engine, domain);
    }

    public static ScriptDatum Execute(
        ScriptDomain domain,
        string method,
        string module = "TEST",
        params ScriptDatum[] arguments)
    {
        return domain.Execute(module, method, ScriptObject.Null, arguments);
    }

    public void Dispose()
    {
        if (Directory.Exists(Root))
        {
            Directory.Delete(Root, recursive: true);
        }
    }
}
