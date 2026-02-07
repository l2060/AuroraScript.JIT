using AuroraScript.Compiler.Analyzer;
using AuroraScript.Compiler.Ast;
using AuroraScript.Compiler.Emits;
using AuroraScript.Core;
using AuroraScript.Runtime;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace AuroraScript.Compiler
{
    internal delegate ScriptDatum DynamicCallMethod(ScriptContext ctx, ScriptDatum[] args);
    internal class IncrementalCompiler(ScriptDomain _domain, EngineOptions _options, CILEmitter _codeGenerator)
    {
        public readonly ScriptDomain Domain = _domain;
        public readonly EngineOptions Options = _options;
        public readonly CILEmitter CodeGenerator = _codeGenerator;




        // 增加增量编译器,在已有的用户态脚本空间内执行代码来对当前domain脚本实例进行热修复补丁
        // forceImport = false 时自动跳过已导入的依赖模块
        // forceImport = true  时强制编译替换依赖模块
        // 修补已存在的module时需要保持源module的引用。
        // 根据domain 分析哪些模块已经加载，哪些未加载， 哪些需要强制加载
        public DynamicCallMethod BuildPatch(ScriptSource source, HotPatchType patchType)
        {
            var sourcePath = source.SourcePath;
            var moduleMap = Domain.Global.modulePathHash.Values.ToDictionary(k => k.ModulePath, v => v.ModuleName);
            var moduleSyntaxTrees = BuildSyntaxTree(source);
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
            return CodeGenerator.VisitHotPatch(mainModule, dependencies, patchType, keys);
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

        public List<ModuleDeclaration> BuildSyntaxTree(ScriptSource source)
        {
            Queue<ScriptSource> padding = new Queue<ScriptSource>();
            HashSet<String> visited = new HashSet<string>();
            padding.Enqueue(source);
            List<ModuleDeclaration> modules = new List<ModuleDeclaration>();
            while (padding.Count > 0)
            {
                source = padding.Dequeue();
                var lexer = new AuroraLexer(source.BaseDirectory, source);
                var parser = new AuroraParser(lexer, Options);
                var syntaxTree = parser.Parse();
                foreach (var dep in syntaxTree.Imports)
                {
                    if (!visited.Contains(dep.FullPath))
                    {
                        source = new FileSource(source.BaseDirectory, dep.FullPath, Encoding.UTF8);
                        padding.Enqueue(source);
                        visited.Add(dep.FullPath);
                    }
                }
                modules.Add(syntaxTree);
            }
            return modules;
        }
    }
}
