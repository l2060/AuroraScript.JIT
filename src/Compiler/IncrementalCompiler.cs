using AuroraScript.Compiler.Analyzer;
using AuroraScript.Compiler.Ast;
using AuroraScript.Compiler.Backend;
using AuroraScript.Compiler.Backend.Builders;
using AuroraScript.Compiler.Backend.Emission;
using AuroraScript.Core;
using AuroraScript.Runtime;
using AuroraScript.Source;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace AuroraScript.Compiler
{
    internal delegate ScriptDatum DynamicCallMethod(ScriptContext ctx, Span<ScriptDatum> args);
    internal class IncrementalCompiler(ScriptDomain _domain, EngineOptions _options, DynamicBuilder _builder)
    {
        public readonly ScriptDomain Domain = _domain;
        public readonly EngineOptions Options = _options;
        public readonly DynamicBuilder Builder = _builder;




        // 增加增量编译器,在已有的用户态脚本空间内执行代码来对当前domain脚本实例进行热修复补丁
        // forceImport = false 时自动跳过已导入的依赖模块
        // forceImport = true  时强制编译替换依赖模块
        // 修补已存在的module时需要保持源module的引用。
        // 根据domain 分析哪些模块已经加载，哪些未加载， 哪些需要强制加载
        public async Task<DynamicCallMethod> BuildPatchAsync(
            ScriptSource source,
            HotPatchType patchType,
            CancellationToken cancellationToken = default)
        {
            var sourcePath = source.SourcePath;
            var moduleMap = Domain.Global.modulePathHash.Values.ToDictionary(k => k.ModulePath, v => v.ModuleName);
            var moduleSyntaxTrees = await BuildSyntaxTreeAsync(source, cancellationToken).ConfigureAwait(false);
            if ((patchType & HotPatchType.IgnoreDepends) != 0)
            {
                moduleSyntaxTrees.RemoveAll(e => e.ModulePath != sourcePath && moduleMap.ContainsKey(e.ModulePath));
            }
            var mainModule = moduleSyntaxTrees.First(e => e.ModulePath == sourcePath);
            moduleSyntaxTrees.Remove(mainModule);
            var dependencies = moduleSyntaxTrees.ToArray();

            var keys = Array.Empty<string>();
            if (Domain.Global.TryGetModule(mainModule.ModuleName, out var existingModule))
            {
                keys = existingModule.EnumerationKeys().ToArray();
            }
            LinkModules(mainModule, dependencies, moduleMap);
            var backend = new BackendCompiler(Builder, Options);
            var session = backend.CreateHotPatchPlans(mainModule, dependencies, keys, out var mainModulePlan);
            var emitter = new HotPatchEmitter(
                new EmissionSession(session, Builder, emitExecutableSkeletons: true, forceModuleDefinitions: true),
                patchType);
            return emitter.Emit(mainModulePlan);
        }



        private void LinkModules(ModuleDeclaration mainModule, ModuleDeclaration[] dependencies, Dictionary<string, string> moduleMap)
        {
            foreach (var dependency in dependencies)
            {
                moduleMap[dependency.ModulePath] = dependency.ModuleName;
            }
            foreach (var import in mainModule.Imports)
            {
                import.ModuleName = moduleMap[import.ModulePath];
            }
            foreach (var module in dependencies)
            {
                foreach (var import in module.Imports)
                {
                    import.ModuleName = moduleMap[import.ModulePath];
                }
            }
        }

        public async Task<List<ModuleDeclaration>> BuildSyntaxTreeAsync(
            ScriptSource source,
            CancellationToken cancellationToken = default)
        {
            Queue<ScriptSource> padding = new Queue<ScriptSource>();
            HashSet<String> visited = new HashSet<string>();
            padding.Enqueue(source);
            List<ModuleDeclaration> modules = new List<ModuleDeclaration>();
            while (padding.Count > 0)
            {
                cancellationToken.ThrowIfCancellationRequested();
                source = padding.Dequeue();
                var lexer = new AuroraLexer(source.BaseDirectory, source);
                var parser = new AuroraParser(lexer, Options);
                var syntaxTree = parser.Parse();
                await ResolveImportsAsync(source, syntaxTree, cancellationToken).ConfigureAwait(false);
                foreach (var dep in syntaxTree.Imports)
                {
                    if (!visited.Contains(dep.FullPath))
                    {
                        var dependencySource = await Options.Compiler.SourceResolver
                            .GetSourceAsync(dep.Reference, cancellationToken)
                            .ConfigureAwait(false);
                        padding.Enqueue(dependencySource);
                        visited.Add(dep.FullPath);
                    }
                }
                modules.Add(syntaxTree);
            }
            return modules;
        }

        private async Task ResolveImportsAsync(
            ScriptSource source,
            ModuleDeclaration syntaxTree,
            CancellationToken cancellationToken)
        {
            if (syntaxTree.Imports.Count == 0)
            {
                return;
            }

            var importer = new ScriptSourceReference(source.BaseDirectory, source.FullPath, source.SourcePath);
            var context = new ScriptResolveContext(Options.Compiler.ExtName, Encoding.UTF8);
            for (var i = 0; i < syntaxTree.Imports.Count; i++)
            {
                var import = syntaxTree.Imports[i];
                var requestedPath = import.File?.Value;
                if (string.IsNullOrWhiteSpace(requestedPath))
                {
                    continue;
                }

                var resolved = await Options.Compiler.SourceResolver
                    .ResolveAsync(importer, requestedPath, context, cancellationToken)
                    .ConfigureAwait(false);
                if (resolved == null)
                {
                    var message = import.Include
                        ? $"include file not found: {requestedPath}"
                        : $"Import file not found: {requestedPath}";
                    throw new AuroraCompilationException(AuroraCompilationStage.Binding, import.File.Range, message);
                }

                import.FullPath = resolved.Value.FullPath;
                import.ModulePath = resolved.Value.ModulePath;
                import.Reference = resolved.Value;
            }
        }
    }
}
