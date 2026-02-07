using AuroraScript.Compiler.Analyzer;
using AuroraScript.Compiler.Ast;
using AuroraScript.Compiler.Emits;
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
    internal class ScriptCompiler
    {
        private readonly String _baseDirectory;
        private readonly ConcurrentDictionary<string, ScriptSource> scriptSources = new();
        private readonly ConcurrentDictionary<ScriptSource, ModuleDeclaration> scriptModules = new();
        private readonly Channel<ScriptSource> _compileQueue = Channel.CreateUnbounded<ScriptSource>(new UnboundedChannelOptions { SingleReader = false, SingleWriter = false });
        private readonly CILEmitter codeGenerator;
        private readonly EngineOptions _options;
        private int _pendingModules;
        private ConcurrentBag<Exception> exceptions = new ConcurrentBag<Exception>();

        public ScriptCompiler(EngineOptions options, CILEmitter codeGenerator)
        {
            _options = options;
            _baseDirectory = Path.GetFullPath(_options.BaseDirectory);
            this.codeGenerator = codeGenerator;
        }

        public async Task BuildAsync(ScriptSource[] sources)
        {
            foreach (var source in sources) RegisterCompileModule(source);
            int workerCount = Math.Min(Environment.ProcessorCount, sources.Length);
            var workers = Enumerable.Range(0, workerCount).Select(_ => Task.Factory.StartNew(CompileWorker, this)).ToArray();
            await Task.WhenAll(workers);
            foreach (var worker in workers) worker.Dispose();
            if (exceptions.Any())
            {
                throw new AuroraCompileReportException(exceptions);
            }
            var modules = scriptModules.Values.ToArray();
            // 
            LinkModules(modules);
            ModuleNameConflictCheck(modules);
            // Sort modules by dependency
            modules = ModuleSort(modules);
            codeGenerator.Visit(modules);
        }

        private static async Task CompileWorker(Object state)
        {
            ScriptCompiler compiler = state as ScriptCompiler;
            await foreach (var source in compiler._compileQueue.Reader.ReadAllAsync())
            {
                try
                {
                    compiler.BuildSyntaxTree(source);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch (Exception ex)
                {
                    compiler.exceptions.Add(new AuroraCompileException(source.FullPath, ex));
                    break;
                }
                finally
                {
                    if (Interlocked.Decrement(ref compiler._pendingModules) == 0)
                    {
                        compiler._compileQueue.Writer.Complete();
                    }
                }
            }
        }


        private void RegisterCompileModule(ScriptSource source)
        {
            scriptSources.GetOrAdd(source.FullPath, path =>
            {
                if (source is FileSource && !File.Exists(path))
                {
                    throw new AuroraException($"Import file source not found {path}");
                }
                Interlocked.Increment(ref _pendingModules);
                _compileQueue.Writer.TryWrite(source);
                return source;
            });
        }


        public void BuildSyntaxTree(ScriptSource source)
        {
            scriptModules.GetOrAdd(source, (e) =>
            {
                var lexer = new AuroraLexer(_baseDirectory, source);
                var parser = new AuroraParser(lexer, _options);
                var syntaxTree = parser.Parse();
                foreach (var dep in syntaxTree.Imports)
                {
                    RegisterCompileModule(new FileSource(source.BaseDirectory, dep.FullPath, Encoding.UTF8));
                }
                return syntaxTree;
            });
            return;
        }


        private void LinkModules(ModuleDeclaration[] modules)
        {
            foreach (var module in modules)
            {
                foreach (var import in module.Imports)
                {
                    var source = scriptSources[import.FullPath];
                    import.ModuleName = scriptModules[source].ModuleName;
                }
            }
        }


        private static void ModuleNameConflictCheck(ModuleDeclaration[] modules)
        {
            var conflicts = modules.GroupBy(e => e.ModuleName).Where(g => g.Count() > 1);
            if (conflicts.Any())
            {
                var sb = new StringBuilder("Conflicting source names found:");
                foreach (var conflict in conflicts)
                {
                    sb.Append($"\nModule '{conflict.Key}' conflict in files:");
                    foreach (var m in conflict)
                    {
                        sb.Append($"\n  - {m.ModulePath}");
                    }
                }
                throw new AuroraException(sb.ToString());
            }
        }

        /// <summary>
        /// kahn modules sort
        /// </summary>
        /// <param name="syntaxRefs"></param>
        /// <returns></returns>
        private static ModuleDeclaration[] ModuleSort(ModuleDeclaration[] syntaxRefs)
        {
            var moduleCount = syntaxRefs.Length;
            var indexMap = new Dictionary<string, int>(moduleCount);
            for (int i = 0; i < moduleCount; i++)
            {
                indexMap[syntaxRefs[i].ModuleName] = i;
            }
            var inDegree = new int[moduleCount];
            var graph = new List<int>[moduleCount];

            for (int i = 0; i < moduleCount; i++)
            {
                graph[i] = new List<int>(4);
            }
            foreach (var moduleRef in syntaxRefs)
            {
                var from = indexMap[moduleRef.ModuleName];
                foreach (var import in moduleRef.Imports)
                {
                    if (!indexMap.TryGetValue(import.ModuleName, out var to))
                    {
                        continue;
                    }
                    graph[to].Add(from);
                    inDegree[from]++;
                }
            }
            var queue = new Queue<int>(moduleCount);
            for (int i = 0; i < moduleCount; i++)
            {
                if (inDegree[i] == 0) queue.Enqueue(i);
            }
            var result = new ModuleDeclaration[moduleCount];
            int idx = 0;
            while (queue.Count > 0)
            {
                var current = queue.Dequeue();
                result[idx++] = syntaxRefs[current];
                foreach (var next in graph[current])
                {
                    if (--inDegree[next] == 0) queue.Enqueue(next);
                }
            }
            //if (idx != moduleCount)
            //{
            //    throw new InvalidOperationException("Detected circular module dependency");
            //}
            return idx == moduleCount ? result : result[..idx];
        }
    }
}