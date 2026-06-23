using AuroraScript.Compiler.Analyzer;
using AuroraScript.Compiler.Ast;
using AuroraScript.Core;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace AuroraScript.Compiler
{
    internal sealed class ScriptCompiler
    {
        private static readonly StringComparer PathComparer = OperatingSystem.IsWindows()
            ? StringComparer.OrdinalIgnoreCase
            : StringComparer.Ordinal;

        private readonly string _baseDirectory;
        private readonly ConcurrentDictionary<string, ScriptSource> _sourcesByPath = new(PathComparer);
        private readonly ConcurrentDictionary<string, ModuleDeclaration> _modulesByPath = new(PathComparer);
        private readonly Channel<ScriptSource> _compileQueue = Channel.CreateUnbounded<ScriptSource>(
            new UnboundedChannelOptions
            {
                SingleReader = false,
                SingleWriter = false,
                AllowSynchronousContinuations = false
            });
        private readonly ConcurrentQueue<Exception> _exceptions = new();
        private readonly EngineOptions _options;
        private readonly object _workerLock = new();
        private Task[] _workers;
        private CancellationToken _compilationCancellationToken;
        private int _maxWorkerCount;
        private int _workerCount;
        private int _pendingModules;
        private int _initialRegistrationCompleted;

        public ScriptCompiler(EngineOptions options)
        {
            _options = options;
            _baseDirectory = Path.GetFullPath(_options.BaseDirectory);
        }

        public async Task<ModuleDeclaration[]> BuildModuleGraphAsync(ScriptSource[] sources, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(sources);
            cancellationToken.ThrowIfCancellationRequested();

            for (var i = 0; i < sources.Length; i++)
            {
                ArgumentNullException.ThrowIfNull(sources[i]);
                ValidateCompileModule(sources[i]);
            }

            _maxWorkerCount = ResolveWorkerCount();
            _workers = new Task[_maxWorkerCount];
            _compilationCancellationToken = cancellationToken;

            for (var i = 0; i < sources.Length; i++)
            {
                cancellationToken.ThrowIfCancellationRequested();
                RegisterCompileModule(sources[i]);
            }

            Volatile.Write(ref _initialRegistrationCompleted, 1);
            if (Volatile.Read(ref _pendingModules) == 0)
            {
                _compileQueue.Writer.TryComplete();
            }

            try
            {
                await AwaitWorkersAsync().ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                _compileQueue.Writer.TryComplete();
                throw;
            }

            if (!_exceptions.IsEmpty)
            {
                var errors = _exceptions.ToArray();
                Array.Sort(errors, CompareCompileErrors);
                throw new AuroraCompileReportException(errors);
            }

            var modules = _modulesByPath.Values.ToArray();
            Array.Sort(modules, CompareModulesByPath);
            LinkModules(modules);
            ModuleNameConflictCheck(modules);
            return ModuleSort(modules);
        }

        public async Task BuildAsync(ScriptSource[] sources, CancellationToken cancellationToken = default)
        {
            var modules = await BuildModuleGraphAsync(sources, cancellationToken).ConfigureAwait(false);
            throw new NotSupportedException("ScriptCompiler no longer owns emission. Use AuroraEngine build pipeline.");
        }

        private int ResolveWorkerCount()
        {
            var configured = _options.MaxDegreeOfParallelism;
            return configured > 0 ? configured : Math.Max(1, Environment.ProcessorCount);
        }

        private async Task CompileWorkerAsync(CancellationToken cancellationToken)
        {
            await Task.Yield();
            await foreach (var source in _compileQueue.Reader.ReadAllAsync(cancellationToken).ConfigureAwait(false))
            {
                try
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    BuildSyntaxTree(source);
                }
                catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
                {
                    throw;
                }
                catch (Exception ex)
                {
                    _exceptions.Enqueue(new AuroraCompileException(source.FullPath, ex));
                }
                finally
                {
                    CompleteCompileModule();
                }
            }
        }

        private void RegisterCompileModule(ScriptSource source)
        {
            ValidateCompileModule(source);
            var fullPath = NormalizePath(source.FullPath);
            if (!_sourcesByPath.TryAdd(fullPath, source))
            {
                return;
            }

            Interlocked.Increment(ref _pendingModules);
            if (_compileQueue.Writer.TryWrite(source))
            {
                EnsureWorkerCapacity();
                return;
            }

            _sourcesByPath.TryRemove(fullPath, out _);
            CompleteCompileModule();
            throw new InvalidOperationException("The compilation queue was completed before all modules were registered.");
        }

        private void EnsureWorkerCapacity()
        {
            lock (_workerLock)
            {
                var targetWorkerCount = Math.Min(_maxWorkerCount, Math.Max(1, Volatile.Read(ref _pendingModules)));
                while (_workerCount < targetWorkerCount)
                {
                    _workers[_workerCount++] = CompileWorkerAsync(_compilationCancellationToken);
                }
            }
        }

        private async Task AwaitWorkersAsync()
        {
            Exception workerFailure = null;
            var observedWorkerCount = 0;
            while (true)
            {
                Task[] snapshot;
                lock (_workerLock)
                {
                    if (observedWorkerCount == _workerCount)
                    {
                        break;
                    }
                    observedWorkerCount = _workerCount;
                    snapshot = new Task[observedWorkerCount];
                    Array.Copy(_workers, snapshot, observedWorkerCount);
                }

                try
                {
                    await Task.WhenAll(snapshot).ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    workerFailure ??= ex;
                }
            }

            if (workerFailure != null)
            {
                System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(workerFailure).Throw();
            }
        }

        private void CompleteCompileModule()
        {
            if (Interlocked.Decrement(ref _pendingModules) == 0 &&
                Volatile.Read(ref _initialRegistrationCompleted) != 0)
            {
                _compileQueue.Writer.TryComplete();
            }
        }

        private static void ValidateCompileModule(ScriptSource source)
        {
            var fullPath = NormalizePath(source.FullPath);
            if (source is FileSource && !File.Exists(fullPath))
            {
                throw new AuroraException($"Import file source not found {fullPath}");
            }
        }

        private void BuildSyntaxTree(ScriptSource source)
        {
            var fullPath = NormalizePath(source.FullPath);
            var lexer = new AuroraLexer(_baseDirectory, source);
            var parser = new AuroraParser(lexer, _options);
            var syntaxTree = parser.Parse();

            if (!_modulesByPath.TryAdd(fullPath, syntaxTree))
            {
                throw new InvalidOperationException($"Module '{fullPath}' was parsed more than once.");
            }

            var dependencyEncoding = source is FileSource fileSource ? fileSource.Encoding : Encoding.UTF8;
            for (var i = 0; i < syntaxTree.Imports.Count; i++)
            {
                var dependency = syntaxTree.Imports[i];
                RegisterCompileModule(new FileSource(source.BaseDirectory, dependency.FullPath, dependencyEncoding));
            }
        }

        private void LinkModules(ModuleDeclaration[] modules)
        {
            for (var moduleIndex = 0; moduleIndex < modules.Length; moduleIndex++)
            {
                var module = modules[moduleIndex];
                for (var importIndex = 0; importIndex < module.Imports.Count; importIndex++)
                {
                    var import = module.Imports[importIndex];
                    var dependencyPath = NormalizePath(import.FullPath);
                    if (!_modulesByPath.TryGetValue(dependencyPath, out var dependency))
                    {
                        throw new AuroraException($"Imported module was not compiled: {import.FullPath}");
                    }
                    import.Module = dependency;
                    import.ModuleName = dependency.ModuleName;
                }
            }
        }

        private static void ModuleNameConflictCheck(ModuleDeclaration[] modules)
        {
            var modulesByName = new Dictionary<string, List<ModuleDeclaration>>(modules.Length, StringComparer.Ordinal);
            for (var i = 0; i < modules.Length; i++)
            {
                var module = modules[i];
                if (!modulesByName.TryGetValue(module.ModuleName, out var matchingModules))
                {
                    matchingModules = new List<ModuleDeclaration>(1);
                    modulesByName.Add(module.ModuleName, matchingModules);
                }
                matchingModules.Add(module);
            }

            StringBuilder message = null;
            foreach (var pair in modulesByName)
            {
                if (pair.Value.Count < 2)
                {
                    continue;
                }

                message ??= new StringBuilder("Conflicting source names found:");
                message.Append("\nModule '").Append(pair.Key).Append("' conflict in files:");
                for (var i = 0; i < pair.Value.Count; i++)
                {
                    message.Append("\n  - ").Append(pair.Value[i].ModulePath);
                }
            }

            if (message != null)
            {
                throw new AuroraException(message.ToString());
            }
        }

        private static ModuleDeclaration[] ModuleSort(ModuleDeclaration[] modules)
        {
            var moduleCount = modules.Length;
            if (moduleCount < 2)
            {
                return modules;
            }

            var indexByName = new Dictionary<string, int>(moduleCount, StringComparer.Ordinal);
            for (var i = 0; i < moduleCount; i++)
            {
                indexByName.Add(modules[i].ModuleName, i);
            }

            var inDegree = new int[moduleCount];
            var dependents = new List<int>[moduleCount];
            for (var i = 0; i < moduleCount; i++)
            {
                dependents[i] = new List<int>(4);
            }

            for (var moduleIndex = 0; moduleIndex < moduleCount; moduleIndex++)
            {
                var imports = modules[moduleIndex].Imports;
                var uniqueDependencies = new HashSet<int>();
                for (var importIndex = 0; importIndex < imports.Count; importIndex++)
                {
                    if (!indexByName.TryGetValue(imports[importIndex].ModuleName, out var dependencyIndex) ||
                        !uniqueDependencies.Add(dependencyIndex))
                    {
                        continue;
                    }
                    dependents[dependencyIndex].Add(moduleIndex);
                    inDegree[moduleIndex]++;
                }
            }

            var ready = new PriorityQueue<int, string>(StringComparer.Ordinal);
            for (var i = 0; i < moduleCount; i++)
            {
                if (inDegree[i] == 0)
                {
                    ready.Enqueue(i, modules[i].ModulePath);
                }
            }

            var result = new ModuleDeclaration[moduleCount];
            var resultIndex = 0;
            while (ready.TryDequeue(out var current, out _))
            {
                result[resultIndex++] = modules[current];
                var currentDependents = dependents[current];
                for (var i = 0; i < currentDependents.Count; i++)
                {
                    var dependent = currentDependents[i];
                    if (--inDegree[dependent] == 0)
                    {
                        ready.Enqueue(dependent, modules[dependent].ModulePath);
                    }
                }
            }

            if (resultIndex != moduleCount)
            {
                var cycle = new StringBuilder("Circular module dependency detected:");
                for (var i = 0; i < moduleCount; i++)
                {
                    if (inDegree[i] > 0)
                    {
                        cycle.Append("\n  - ").Append(modules[i].ModulePath);
                    }
                }
                throw new AuroraException(cycle.ToString());
            }

            return result;
        }

        private static string NormalizePath(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                throw new AuroraException("Script source paths cannot be empty.");
            }
            return Path.GetFullPath(path);
        }

        private static int CompareModulesByPath(ModuleDeclaration left, ModuleDeclaration right)
        {
            return PathComparer.Compare(left.FullPath, right.FullPath);
        }

        private static int CompareCompileErrors(Exception left, Exception right)
        {
            var leftPath = left is AuroraCompileException leftCompile ? leftCompile.ModulePath : left.Message;
            var rightPath = right is AuroraCompileException rightCompile ? rightCompile.ModulePath : right.Message;
            return PathComparer.Compare(leftPath, rightPath);
        }
    }
}
