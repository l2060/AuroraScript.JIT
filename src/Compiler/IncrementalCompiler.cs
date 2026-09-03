using AuroraScript.Compiler.Analyzer;
using AuroraScript.Compiler.Ast;
using AuroraScript.Compiler.Backend;
using AuroraScript.Compiler.Backend.Builders;
using AuroraScript.Compiler.Backend.Emission;
using AuroraScript.Compiler.GlobalDeclarations;
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

        // A patch preserves the loaded module object at the same absolute source path.
        public async Task<DynamicCallMethod> BuildPatchAsync(
            ScriptSource source,
            HotPatchType patchType,
            CancellationToken cancellationToken = default)
        {
            var sourcePath = ScriptPath.NormalizeFullPath(source.FullPath);
            var moduleSyntaxTrees = await BuildSyntaxTreeAsync(source, cancellationToken).ConfigureAwait(false);
            var globalDeclarations = await BuildGlobalDeclarationIndexAsync(cancellationToken).ConfigureAwait(false);
            LinkModules(moduleSyntaxTrees);
            ValidateLinkedTypeReferences(moduleSyntaxTrees);

            var mainModule = moduleSyntaxTrees.First(
                module => ScriptPath.Comparer.Equals(module.Source.FullPath, sourcePath));
            if ((patchType & HotPatchType.IgnoreDepends) != 0)
            {
                moduleSyntaxTrees.RemoveAll(module =>
                    !ReferenceEquals(module, mainModule) &&
                    Domain.Global.TryGetModuleByPath(module.Source.FullPath, out _));
            }
            ValidateNoNativeHotPatch(moduleSyntaxTrees);

            ValidateExplicitModuleNames(moduleSyntaxTrees);

            moduleSyntaxTrees.Remove(mainModule);
            var dependencies = moduleSyntaxTrees.ToArray();

            var keys = Array.Empty<string>();
            if (Domain.Global.TryGetModuleByPath(mainModule.Source.FullPath, out var existingModule))
            {
                keys = existingModule.GetOwnMemberNames();
            }

            var backend = new BackendCompiler(Builder, Options, globalDeclarations);
            var session = backend.CreateHotPatchPlans(mainModule, dependencies, keys, out var mainModulePlan);
            var emitter = new HotPatchEmitter(
                new EmissionSession(session, Builder, emitExecutableCode: true, forceModuleDefinitions: true),
                patchType);
            return emitter.Emit(mainModulePlan);
        }

        private void ValidateNoNativeHotPatch(
            IReadOnlyList<ModuleDeclaration> modules)
        {
            for (var moduleIndex = 0; moduleIndex < modules.Count; moduleIndex++)
            {
                var module = modules[moduleIndex];
                Domain.Global.TryGetModuleByPath(
                    module.Source.FullPath,
                    out var existing);
                for (var functionIndex = 0;
                    functionIndex < module.Functions.Count;
                    functionIndex++)
                {
                    var function = module.Functions[functionIndex];
                    if (!function.IsNative &&
                        existing?.IsNativeFunction(function.Name.Value) != true)
                    {
                        continue;
                    }

                    throw new AuroraCompilationException(
                        AuroraCompilationStage.Binding,
                        function,
                        $"Native function '{function.Name.Value}' cannot be hot updated.");
                }
            }
        }

        private static void LinkModules(IReadOnlyList<ModuleDeclaration> modules)
        {
            var modulesByPath = new Dictionary<string, ModuleDeclaration>(modules.Count, ScriptPath.Comparer);
            for (var i = 0; i < modules.Count; i++)
            {
                modulesByPath.Add(modules[i].Source.FullPath, modules[i]);
            }

            for (var moduleIndex = 0; moduleIndex < modules.Count; moduleIndex++)
            {
                var module = modules[moduleIndex];
                for (var importIndex = 0; importIndex < module.Imports.Count; importIndex++)
                {
                    var import = module.Imports[importIndex];
                    if (!modulesByPath.TryGetValue(import.Reference.FullPath, out var dependency))
                    {
                        throw new AuroraCompilationException(
                            AuroraCompilationStage.Linking,
                            import.Reference.FullPath,
                            1,
                            1,
                            $"Imported module was not compiled: {import.Reference.FullPath}");
                    }

                    import.Module = dependency;
                }
            }
        }

        private void ValidateLinkedTypeReferences(
            IReadOnlyList<ModuleDeclaration> modules)
        {
            LinkedTypeReferenceValidator.Validate(modules, _options.Compiler.NativeTypes);
        }

        private void ValidateExplicitModuleNames(IReadOnlyList<ModuleDeclaration> modules)
        {
            var sourceByName = new Dictionary<string, string>(StringComparer.Ordinal);
            for (var i = 0; i < modules.Count; i++)
            {
                var module = modules[i];
                var name = module.ModuleName;
                if (string.IsNullOrEmpty(name))
                {
                    continue;
                }

                if (sourceByName.TryGetValue(name, out var firstPath) &&
                    !ScriptPath.Comparer.Equals(firstPath, module.Source.FullPath))
                {
                    throw ModuleNameConflict(name, firstPath, module.Source.FullPath);
                }
                sourceByName[name] = module.Source.FullPath;

                if (Domain.Global.TryGetModule(name, out var namedModule) &&
                    !ScriptPath.Comparer.Equals(namedModule.Source.FullPath, module.Source.FullPath))
                {
                    throw ModuleNameConflict(name, namedModule.Source.FullPath, module.Source.FullPath);
                }

                if (Domain.Global.TryGetModuleByPath(module.Source.FullPath, out var pathModule) &&
                    !string.Equals(pathModule.Name, name, StringComparison.Ordinal))
                {
                    throw new AuroraCompilationException(
                        AuroraCompilationStage.Linking,
                        module.Source.FullPath,
                        1,
                        1,
                        $"Module source '{module.Source.FullPath}' is already loaded with a different explicit name.");
                }
            }
        }

        private static AuroraCompilationException ModuleNameConflict(string name, string firstPath, string secondPath)
        {
            return new AuroraCompilationException(
                AuroraCompilationStage.Linking,
                secondPath,
                1,
                1,
                $"Module name '{name}' conflicts in files:\n  - {firstPath}\n  - {secondPath}");
        }

        public async Task<List<ModuleDeclaration>> BuildSyntaxTreeAsync(
            ScriptSource source,
            CancellationToken cancellationToken = default)
        {
            Queue<ScriptSource> padding = new Queue<ScriptSource>();
            HashSet<String> visited = new HashSet<string>(ScriptPath.Comparer);
            visited.Add(source.FullPath);
            padding.Enqueue(source);
            List<ModuleDeclaration> modules = new List<ModuleDeclaration>();
            var globalDeclarations = new GlobalDeclarationWorkspaceIndexBuilder();
            while (padding.Count > 0)
            {
                cancellationToken.ThrowIfCancellationRequested();
                source = padding.Dequeue();
                var sourceText = source.ReadSource();
                globalDeclarations.AddFile(source.FullPath, sourceText);
                if (GlobalDeclarationScanner.IsGlobalFile(sourceText))
                {
                    if (modules.Count == 0)
                    {
                        throw new AuroraCompilationException(
                            AuroraCompilationStage.Parsing,
                            source.FullPath,
                            1,
                            1,
                            "@global() declaration files cannot be compiled as modules.");
                    }

                    continue;
                }

                var lexer = new AuroraLexer(source.BaseDirectory, source);
                var parser = new AuroraParser(lexer, Options);
                var syntaxTree = parser.Parse();
                if (syntaxTree.IsGlobalDeclarationFile)
                {
                    continue;
                }

                await ResolveImportsAsync(source, syntaxTree, cancellationToken).ConfigureAwait(false);
                foreach (var dep in syntaxTree.Imports)
                {
                    if (visited.Add(dep.Reference.FullPath))
                    {
                        var dependencySource = await Options.Compiler.SourceResolver
                            .GetSourceAsync(dep.Reference, cancellationToken)
                            .ConfigureAwait(false);
                        padding.Enqueue(dependencySource);
                    }
                }
                modules.Add(syntaxTree);
            }

            if (globalDeclarations.Diagnostics.Count != 0)
            {
                throw new AuroraCompilationException(globalDeclarations.Diagnostics);
            }

            return modules;
        }

        private async Task<GlobalDeclarationIndex> BuildGlobalDeclarationIndexAsync(CancellationToken cancellationToken)
        {
            var globalDeclarations = await GlobalDeclarationScanner
                .BuildIndexAsync(Options.Compiler.SourceResolver, Options.Compiler.ExtName, cancellationToken)
                .ConfigureAwait(false);

            if (globalDeclarations.Diagnostics.Count != 0)
            {
                throw new AuroraCompilationException(globalDeclarations.Diagnostics);
            }

            return globalDeclarations;
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

                import.Reference = resolved.Value;

                var resolvedSource = await Options.Compiler.SourceResolver
                    .GetSourceAsync(resolved.Value, cancellationToken)
                    .ConfigureAwait(false);
                if (GlobalDeclarationScanner.IsGlobalFile(resolvedSource.ReadSource()))
                {
                    var message = import.Include
                        ? "@global() declaration files cannot be included."
                        : "@global() declaration files cannot be imported.";
                    throw new AuroraCompilationException(AuroraCompilationStage.Binding, import.File.Range, message);
                }
            }
        }
    }
}
