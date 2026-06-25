using AuroraScript.Runtime;
using AuroraScript.Runtime.Types;
using System;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

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

    public AuroraEngine CreateEngine(
        CompilationMode mode = CompilationMode.Dynamic,
        bool enableHotReload = false,
        bool enableConfused = false,
        int maxDegreeOfParallelism = 4,
        string? assemblyOut = null,
        TextWriter? output = null,
        bool enableModuleConstInlining = false)
    {
        var options = EngineOptions.Default
            .WithCompiler(compiler => compiler.WithDirectory(Root))
            .WithCompiler(compiler => compiler.Mode = mode)
            .WithOptimization(optimization => optimization.Level = OptimizeOptions.Release)
            .WithRuntime(runtime => runtime.HotReload = enableHotReload)
            .WithOptimization(optimization => optimization.ModuleConstInlining = enableModuleConstInlining)
            .WithOutput(output => output.Confused = enableConfused)
            .WithCompiler(compiler => compiler.MaxDegreeOfParallelism = maxDegreeOfParallelism)
            .WithRuntime(runtime => runtime.ConsoleStdOut = output ?? TextWriter.Null)
            .WithRuntime(runtime => runtime.ConsoleErrorOut = output ?? TextWriter.Null);

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
        CancellationToken cancellationToken = default)
    {
        var assemblyOut = mode == CompilationMode.Persistence ? Path.Combine(Root, "test-output.dll") : null;
        var engine = CreateEngine(mode, enableHotReload, enableConfused, maxDegreeOfParallelism, assemblyOut);
        var mainPath = WriteSource("main.as", source);
        await engine.BuildAsync(cancellationToken, engine.FileSource(mainPath, Encoding.UTF8));
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
