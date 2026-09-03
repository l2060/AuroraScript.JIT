using AuroraScript.Compiler.Ast;
using AuroraScript.Compiler.Ast.Expressions;
using AuroraScript.Compiler.GlobalDeclarations;
using AuroraScript.Core;
using AuroraScript.LanguageServices.Parsing;
using AuroraScript.LanguageServices.Text;
using AuroraScript.LanguageServices.Workspace;
using AuroraScript.Source;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace AuroraScript.LanguageServices.Internal.SymbolIndex;

internal sealed class AuroraWorkspaceIndex
{
    private readonly AuroraParseService _parseService;
    private readonly AuroraWorkspaceSnapshot _snapshot;
    private readonly AuroraWorkspaceIndexCache? _cache;
    private readonly Dictionary<string, AuroraModuleIndex> _modules = new(PathComparer);
    private readonly HashSet<string> _visiting = new(PathComparer);

    private AuroraWorkspaceIndex(
        AuroraParseService parseService,
        AuroraWorkspaceSnapshot snapshot,
        AuroraWorkspaceIndexCache? cache)
    {
        _parseService = parseService;
        _snapshot = snapshot ?? new AuroraWorkspaceSnapshot(Array.Empty<AuroraWorkspaceDocument>());
        _cache = cache;
    }

    public IReadOnlyDictionary<string, AuroraModuleIndex> Modules => _modules;

    public bool ContainsWorkspaceDocument(string path)
    {
        path = NormalizePath(path);
        return _snapshot.Documents.ContainsKey(path) &&
            ScriptPath.IsWithinNormalizedRoot(_snapshot.BaseDirectory, path);
    }

    private static StringComparer PathComparer => OperatingSystem.IsWindows()
        ? StringComparer.OrdinalIgnoreCase
        : StringComparer.Ordinal;

    public static AuroraWorkspaceIndex Build(
        AuroraParseService parseService,
        IEnumerable<AuroraWorkspaceDocument> documents,
        string rootPath)
    {
        return Build(parseService, new AuroraWorkspaceSnapshot(documents), rootPath);
    }

    public static AuroraWorkspaceIndex Build(
        AuroraParseService parseService,
        AuroraWorkspaceSnapshot snapshot,
        string rootPath)
    {
        return Build(parseService, snapshot, rootPath, cache: null);
    }

    public static AuroraWorkspaceIndex Build(
        AuroraParseService parseService,
        AuroraWorkspaceSnapshot snapshot,
        string rootPath,
        AuroraWorkspaceIndexCache? cache)
    {
        var index = new AuroraWorkspaceIndex(parseService, snapshot, cache);
        index.EnsureModule(rootPath);
        foreach (var path in index._snapshot.Documents.Keys)
        {
            index.EnsureModule(path);
        }
        return index;
    }

    public AuroraModuleIndex? TryGetModule(string path)
    {
        _modules.TryGetValue(NormalizePath(path), out var module);
        return module;
    }

    private AuroraModuleIndex? EnsureModule(string path)
    {
        path = NormalizePath(path);
        if (_modules.TryGetValue(path, out var existing))
        {
            return existing;
        }

        if (!_visiting.Add(path))
        {
            return null;
        }

        try
        {
            if (!TryReadDocument(path, out var text))
            {
                return null;
            }

            if (GlobalDeclarationScanner.IsGlobalFile(text))
            {
                return null;
            }

            if (_cache != null && _cache.TryGet(path, text, out var cachedModule))
            {
                _modules.Add(path, cachedModule);
                EnsureImportedModules(cachedModule);
                return cachedModule;
            }

            var parseResult = _parseService.ParseText(path, text, _snapshot.BaseDirectory, _snapshot);
            if (parseResult.Module == null)
            {
                return null;
            }

            var module = new AuroraModuleIndex(path, text, parseResult.Module);
            _modules.Add(path, module);
            CollectModule(module);
            CollectImports(module, parseResult.Module.Imports);
            _cache?.Store(path, text, module);
            EnsureImportedModules(module);

            return module;
        }
        finally
        {
            _visiting.Remove(path);
        }
    }

    private void CollectModule(AuroraModuleIndex module)
    {
        var declaration = module.Module;
        for (var i = 0; i < declaration.Contexts.Count; i++)
        {
            var context = declaration.Contexts[i];
            module.AddSymbol(new AuroraSymbolInfo(
                context.Name.Value,
                AuroraSymbolKind.Variable,
                declaration.Source.ModulePath,
                module.Path,
                TextRange.FromSourceSpan(context.Name.Range),
                exported: false));
        }

        for (var i = 0; i < declaration.Types.Count; i++)
        {
            var type = declaration.Types[i];
            module.AddSymbol(new AuroraSymbolInfo(
                type.Name.Value,
                AuroraSymbolKind.Type,
                declaration.Source.ModulePath,
                module.Path,
                TextRange.FromSourceSpan(type.Name.Range),
                type.Access == MemberAccess.Export));
        }

        for (var i = 0; i < declaration.Statements.Count; i++)
        {
            switch (declaration.Statements[i])
            {
                case VariableDeclaration variable when variable.Name != null:
                    module.AddSymbol(new AuroraSymbolInfo(
                        variable.Name.Value,
                        variable.IsConst ? AuroraSymbolKind.Constant : AuroraSymbolKind.Variable,
                        declaration.Source.ModulePath,
                        module.Path,
                        TextRange.FromSourceSpan(variable.Name.Range),
                        variable.Access == MemberAccess.Export,
                        variable.IsDeclare));
                    break;
                case EnumDeclaration enumDeclaration when enumDeclaration.Identifier != null:
                    module.AddSymbol(new AuroraSymbolInfo(
                        enumDeclaration.Identifier.Value,
                        AuroraSymbolKind.Enum,
                        declaration.Source.ModulePath,
                        module.Path,
                        TextRange.FromSourceSpan(enumDeclaration.Identifier.Range),
                        enumDeclaration.Access == MemberAccess.Export));
                    break;
            }
        }

        for (var i = 0; i < declaration.Functions.Count; i++)
        {
            var function = declaration.Functions[i];
            if (function.Name == null)
            {
                continue;
            }

            module.AddSymbol(new AuroraSymbolInfo(
                function.Name.Value,
                AuroraSymbolKind.Function,
                declaration.Source.ModulePath,
                module.Path,
                TextRange.FromSourceSpan(function.Name.Range),
                function.Access == MemberAccess.Export,
                (function.Flags & FunctionFlags.Declare) != 0));
        }
    }

    private void CollectImports(AuroraModuleIndex module, IReadOnlyList<ImportDeclaration> imports)
    {
        for (var i = 0; i < imports.Count; i++)
        {
            var import = imports[i];
            if (string.IsNullOrWhiteSpace(import.Reference.FullPath))
            {
                continue;
            }

            var targetPath = NormalizePath(import.Reference.FullPath);
            var alias = import.Name?.Value ?? string.Empty;
            module.AddImport(new AuroraImportInfo(
                alias,
                targetPath,
                TextRange.FromSourceSpan(import.Name?.Range ?? import.Range),
                TextRange.FromSourceSpan(import.File?.Range ?? import.Range),
                import.Include));
        }
    }

    private void EnsureImportedModules(AuroraModuleIndex module)
    {
        foreach (var import in module.ImportsByAlias.Values)
        {
            EnsureModule(import.TargetPath);
        }

        for (var i = 0; i < module.Includes.Count; i++)
        {
            EnsureModule(module.Includes[i].TargetPath);
        }
    }

    private bool TryReadDocument(string path, out string text)
    {
        path = NormalizePath(path);
        if (_snapshot.TryGetDocument(path, out var document))
        {
            text = document.Text;
            return true;
        }

        if (File.Exists(path))
        {
            text = File.ReadAllText(path);
            return true;
        }

        if (!ReferenceEquals(_parseService.SourceResolver, FileScriptSourceResolver.Instance))
        {
            try
            {
                var source = _parseService.SourceResolver
                    .GetSourceAsync(new ScriptSourceReference(_snapshot.BaseDirectory, path))
                    .AsTask()
                    .GetAwaiter()
                    .GetResult();
                text = source.ReadSource();
                return true;
            }
            catch (Exception ex) when (IsSourceReadFailure(ex))
            {
            }
        }

        text = string.Empty;
        return false;
    }

    private static bool IsSourceReadFailure(Exception exception)
    {
        return exception is FileNotFoundException
            or DirectoryNotFoundException
            or IOException
            or UnauthorizedAccessException
            or ArgumentException
            or NotSupportedException
            or KeyNotFoundException;
    }

    internal static string NormalizePath(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return string.Empty;
        }

        return ScriptPath.NormalizeFullPath(path);
    }
}
