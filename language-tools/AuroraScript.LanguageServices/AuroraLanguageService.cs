using AuroraScript.Core;
using AuroraScript.Compiler.Ast.Expressions;
using AuroraScript.LanguageServices.Builtins;
using AuroraScript.LanguageServices.Diagnostics;
using AuroraScript.LanguageServices.Features.Completion;
using AuroraScript.LanguageServices.Features.Definition;
using AuroraScript.LanguageServices.Features.Hover;
using AuroraScript.LanguageServices.Features.References;
using AuroraScript.LanguageServices.Features.Rename;
using AuroraScript.LanguageServices.Features.SemanticTokens;
using AuroraScript.LanguageServices.Features.SignatureHelp;
using AuroraScript.LanguageServices.Internal;
using AuroraScript.LanguageServices.Internal.SymbolIndex;
using AuroraScript.LanguageServices.Parsing;
using AuroraScript.LanguageServices.Semantics;
using AuroraScript.LanguageServices.Text;
using AuroraScript.LanguageServices.Workspace;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace AuroraScript.LanguageServices;

public sealed class AuroraLanguageService
{
    private readonly AuroraLanguageServiceOptions _options;
    private AuroraParseService _parseService;
    private AuroraWorkspace _workspace;
    private readonly BuiltinApiCatalog _builtins;
    private BuiltinDefinitionDocuments _builtinDocuments;
    private readonly object _indexLock = new();
    private readonly AuroraWorkspaceIndexCache _workspaceIndexCache = new();

    public AuroraLanguageService(BuiltinApiCatalog builtins)
        : this(new AuroraLanguageServiceOptions(builtins))
    {
    }

    public AuroraLanguageService(AuroraLanguageServiceOptions options)
    {
        _options = options ?? throw new ArgumentNullException(nameof(options));
        _builtins = options.Builtins;
        _parseService = new AuroraParseService(options.ToEngineOptions());
        _workspace = new AuroraWorkspace(options.BaseDirectory);
        DocumentationLocale = options.DocumentationLocale;
        _builtinDocuments = new BuiltinDefinitionDocuments(_builtins, DocumentationLocale);
    }

    public AuroraWorkspace Workspace => _workspace;

    public string DocumentationLocale { get; private set; }

    public void SetDocumentationLocale(string? locale)
    {
        DocumentationLocale = AuroraLanguageServiceOptions.NormalizeDocumentationLocale(locale);
        _builtinDocuments = new BuiltinDefinitionDocuments(_builtins, DocumentationLocale);
    }

    public void SetWorkspaceRoot(string baseDirectory)
    {
        if (string.IsNullOrWhiteSpace(baseDirectory))
        {
            return;
        }

        var normalized = ScriptPath.NormalizeBaseDirectory(baseDirectory);
        lock (_indexLock)
        {
            if (string.Equals(_workspace.BaseDirectory, normalized, OperatingSystem.IsWindows() ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal))
            {
                return;
            }

            _workspace = _workspace.Rebase(normalized);
            if (!_options.HasExplicitSourceResolver)
            {
                _parseService = new AuroraParseService(_options.ToEngineOptions(normalized));
            }
            _workspaceIndexCache.Clear();
        }
    }

    public void OpenOrUpdateDocument(string path, string text, int version = 0)
    {
        _workspace.OpenOrUpdate(path, text, version);
    }

    public void CloseDocument(string path)
    {
        _workspace.Close(path);
    }

    public AuroraParseResult ParseText(string sourceName, string sourceText, string? baseDirectory = null)
    {
        return _parseService.ParseText(sourceName, sourceText, baseDirectory);
    }

    public AuroraParseResult ParseDocument(string path)
    {
        if (!TryGetWorkspaceText(path, out var normalizedPath, out var text))
        {
            return new AuroraParseResult(null, Array.Empty<LanguageDiagnostic>());
        }

        return _parseService.ParseText(normalizedPath, text, _workspace.BaseDirectory, _workspace.CreateSnapshot());
    }

    public IReadOnlyList<LanguageDiagnostic> GetDiagnostics(string sourceName, string sourceText, string? baseDirectory = null)
    {
        var parseResult = ParseText(sourceName, sourceText, baseDirectory);
        var analyzer = new AuroraSemanticAnalyzer(_builtins);
        return analyzer.Analyze(parseResult).Diagnostics;
    }

    public IReadOnlyList<LanguageDiagnostic> GetDiagnostics(string path)
    {
        var parseResult = ParseDocument(path);
        var analyzer = new AuroraSemanticAnalyzer(_builtins);
        return analyzer.Analyze(parseResult).Diagnostics;
    }

    public HoverResult? GetHover(string sourceName, string sourceText, TextPosition position, string? baseDirectory = null)
    {
        var parseResult = ParseText(sourceName, sourceText, baseDirectory);
        if (parseResult.Module == null)
        {
            if (AnnotationDocumentation.TryGetHover(sourceText, position, DocumentationLocale, out var fallbackAnnotationHover))
            {
                return fallbackAnnotationHover;
            }

            return LightweightBuiltinHoverQuery.TryResolve(_builtins, sourceText, position, DocumentationLocale, out var fallbackBuiltinHover)
                ? fallbackBuiltinHover
                : null;
        }

        if (ScriptDocumentationQuery.TryGetHover(parseResult.Module, sourceName, sourceText, position, out var scriptHover))
        {
            return scriptHover;
        }

        if (AnnotationDocumentation.TryGetHover(sourceText, position, DocumentationLocale, out var annotationHover))
        {
            return annotationHover;
        }

        var context = AstQuery.Find(parseResult.Module, position);
        if (context == null)
        {
            return LightweightBuiltinHoverQuery.TryResolve(_builtins, sourceText, position, DocumentationLocale, out var fallbackBuiltinHover)
                ? fallbackBuiltinHover
                : null;
        }

        if (BuiltinQuery.TryGetHover(_builtins, context, DocumentationLocale, out var hover))
        {
            return hover;
        }

        var snapshot = CreateWorkspaceSnapshot(sourceName, sourceText, null, out var normalizedSource);
        var index = AuroraWorkspaceIndex.Build(_parseService, snapshot, normalizedSource);
        if (TryGetUserSymbolHover(index, normalizedSource, sourceText, position, context, out var userHover))
        {
            return userHover;
        }

        return LightweightBuiltinHoverQuery.TryResolve(_builtins, sourceText, position, DocumentationLocale, out var lightweightHover)
            ? lightweightHover
            : null;
    }

    public HoverResult? GetHover(string path, TextPosition position)
    {
        if (!TryGetWorkspaceText(path, out var normalizedPath, out var text))
        {
            return null;
        }

        var parseResult = _parseService.ParseText(normalizedPath, text, _workspace.BaseDirectory, _workspace.CreateSnapshot());
        if (parseResult.Module == null)
        {
            if (AnnotationDocumentation.TryGetHover(text, position, DocumentationLocale, out var fallbackAnnotationHover))
            {
                return fallbackAnnotationHover;
            }

            return LightweightBuiltinHoverQuery.TryResolve(_builtins, text, position, DocumentationLocale, out var fallbackBuiltinHover)
                ? fallbackBuiltinHover
                : null;
        }

        if (ScriptDocumentationQuery.TryGetHover(parseResult.Module, normalizedPath, text, position, out var scriptHover))
        {
            return scriptHover;
        }

        if (AnnotationDocumentation.TryGetHover(text, position, DocumentationLocale, out var annotationHover))
        {
            return annotationHover;
        }

        var context = AstQuery.Find(parseResult.Module, position);
        if (context == null)
        {
            return LightweightBuiltinHoverQuery.TryResolve(_builtins, text, position, DocumentationLocale, out var fallbackBuiltinHover)
                ? fallbackBuiltinHover
                : null;
        }

        if (BuiltinQuery.TryGetHover(_builtins, context, DocumentationLocale, out var hover))
        {
            return hover;
        }

        var index = GetWorkspaceIndex(normalizedPath);
        if (TryGetUserSymbolHover(index, normalizedPath, text, position, context, out var userHover))
        {
            return userHover;
        }

        return LightweightBuiltinHoverQuery.TryResolve(_builtins, text, position, DocumentationLocale, out var lightweightHover)
            ? lightweightHover
            : null;
    }

    public CompletionResult GetCompletions(string sourceName, string sourceText, TextPosition position, string? baseDirectory = null)
    {
        if (LightweightCompletionQuery.TryGetMemberOwner(sourceText, position, out var ownerName))
        {
            var memberCompletions = BuiltinQuery.GetMemberCompletions(_builtins, ownerName, DocumentationLocale);
            if (memberCompletions.Items.Count != 0)
            {
                return memberCompletions;
            }
        }

        var parseResult = ParseText(sourceName, sourceText, baseDirectory);
        if (parseResult.Module == null)
        {
            return BuiltinQuery.GetCompletions(_builtins, null, DocumentationLocale);
        }

        var context = AstQuery.Find(parseResult.Module, position);
        return BuiltinQuery.GetCompletions(_builtins, context, DocumentationLocale);
    }

    public CompletionResult GetCompletions(string path, TextPosition position)
    {
        if (!TryGetWorkspaceText(path, out _, out var text))
        {
            return BuiltinQuery.GetCompletions(_builtins, null, DocumentationLocale);
        }

        return GetCompletions(path, text, position, _workspace.BaseDirectory);
    }

    public SignatureHelpResult? GetSignatureHelp(string sourceName, string sourceText, TextPosition position, string? baseDirectory = null)
    {
        var parseResult = ParseText(sourceName, sourceText, baseDirectory);
        if (parseResult.Module == null)
        {
            return null;
        }

        var context = AstQuery.Find(parseResult.Module, position);
        if (context == null)
        {
            return null;
        }

        return BuiltinQuery.GetSignatureHelp(_builtins, context, position, DocumentationLocale);
    }

    public SignatureHelpResult? GetSignatureHelp(string path, TextPosition position)
    {
        if (!TryGetWorkspaceText(path, out _, out var text))
        {
            return null;
        }

        return GetSignatureHelp(path, text, position, _workspace.BaseDirectory);
    }

    public DefinitionLocation? GetDefinition(
        string sourceName,
        string sourceText,
        TextPosition position,
        IEnumerable<AuroraWorkspaceDocument>? workspaceDocuments = null)
    {
        if (BuiltinDefinitionDocuments.IsBuiltinUri(sourceName))
        {
            return _builtinDocuments.TryGetDocumentDefinition(sourceName, position, out var builtinDefinition)
                ? builtinDefinition
                : null;
        }

        var snapshot = CreateWorkspaceSnapshot(sourceName, sourceText, workspaceDocuments, out var normalizedSource);
        var index = AuroraWorkspaceIndex.Build(_parseService, snapshot, normalizedSource);
        return ResolveBuiltinDefinition(normalizedSource, sourceText, position, snapshot) ??
            AuroraDefinitionResolver.Resolve(index, normalizedSource, position);
    }

    public DefinitionLocation? GetDefinition(string path, TextPosition position)
    {
        if (BuiltinDefinitionDocuments.IsBuiltinUri(path))
        {
            return _builtinDocuments.TryGetDocumentDefinition(path, position, out var builtinDefinition)
                ? builtinDefinition
                : null;
        }

        if (!TryGetWorkspaceText(path, out var normalizedPath, out var text))
        {
            return null;
        }

        var snapshot = CreateIndexSnapshot();
        var index = GetWorkspaceIndex(normalizedPath);
        return ResolveBuiltinDefinition(normalizedPath, text, position, snapshot) ??
            AuroraDefinitionResolver.Resolve(index, normalizedPath, position);
    }

    public BuiltinDocument? GetBuiltinDocument(string uri)
    {
        return _builtinDocuments.TryGetDocument(uri, out var document)
            ? document
            : null;
    }

    public IReadOnlyList<ReferenceLocation> GetReferences(
        string sourceName,
        string sourceText,
        TextPosition position,
        bool includeDeclaration,
        IEnumerable<AuroraWorkspaceDocument>? workspaceDocuments = null)
    {
        var snapshot = CreateWorkspaceSnapshot(sourceName, sourceText, workspaceDocuments, out var normalizedSource);
        var index = AuroraWorkspaceIndex.Build(_parseService, snapshot, normalizedSource);
        return AuroraReferenceResolver.Resolve(index, normalizedSource, position, includeDeclaration);
    }

    public IReadOnlyList<ReferenceLocation> GetReferences(string path, TextPosition position, bool includeDeclaration)
    {
        if (!TryGetWorkspaceText(path, out var normalizedPath, out _))
        {
            return Array.Empty<ReferenceLocation>();
        }

        var index = GetWorkspaceIndex(normalizedPath);
        return AuroraReferenceResolver.Resolve(index, normalizedPath, position, includeDeclaration);
    }

    public RenameResult Rename(
        string sourceName,
        string sourceText,
        TextPosition position,
        string newName,
        IEnumerable<AuroraWorkspaceDocument>? workspaceDocuments = null)
    {
        var snapshot = CreateWorkspaceSnapshot(sourceName, sourceText, workspaceDocuments, out var normalizedSource);
        var index = AuroraWorkspaceIndex.Build(_parseService, snapshot, normalizedSource);
        return AuroraRenameResolver.Rename(index, normalizedSource, position, newName);
    }

    public RenameResult Rename(string path, TextPosition position, string newName)
    {
        if (!TryGetWorkspaceText(path, out var normalizedPath, out _))
        {
            return RenameResult.Fail("Document is not open in the workspace.");
        }

        var index = GetWorkspaceIndex(normalizedPath);
        return AuroraRenameResolver.Rename(index, normalizedPath, position, newName);
    }

    public SemanticTokensResult GetSemanticTokens(string sourceName, string sourceText, string? baseDirectory = null)
    {
        return SemanticTokenScanner.Scan(sourceName, sourceText, baseDirectory, _builtins);
    }

    public SemanticTokensResult GetSemanticTokens(string path)
    {
        if (!TryGetWorkspaceText(path, out _, out var text))
        {
            return new SemanticTokensResult(Array.Empty<SemanticToken>());
        }

        return GetSemanticTokens(path, text, _workspace.BaseDirectory);
    }

    private bool TryGetUserSymbolHover(
        AuroraWorkspaceIndex index,
        string normalizedSource,
        string sourceText,
        TextPosition position,
        AstQueryContext context,
        out HoverResult hover)
    {
        hover = null!;
        var definition = AuroraDefinitionResolver.Resolve(index, normalizedSource, position);
        if (definition == null || BuiltinDefinitionDocuments.IsBuiltinUri(definition.Path))
        {
            return false;
        }

        var hoverRange = GetHoverRange(context);
        var normalizedDefinitionPath = AuroraWorkspaceIndex.NormalizePath(definition.Path);
        var targetModule = index.TryGetModule(normalizedDefinitionPath);
        if (targetModule != null)
        {
            return ScriptDocumentationQuery.TryGetHoverAtDefinition(
                targetModule.Module,
                targetModule.Path,
                targetModule.Text,
                definition.Range,
                hoverRange,
                out hover);
        }

        if (!TryGetWorkspaceText(definition.Path, out normalizedDefinitionPath, out var definitionText))
        {
            return false;
        }

        var parseResult = _parseService.ParseText(normalizedDefinitionPath, definitionText, _workspace.BaseDirectory, _workspace.CreateSnapshot());
        if (parseResult.Module == null)
        {
            return false;
        }

        return ScriptDocumentationQuery.TryGetHoverAtDefinition(
            parseResult.Module,
            normalizedDefinitionPath,
            definitionText,
            definition.Range,
            hoverRange,
            out hover);
    }

    private static TextRange GetHoverRange(AstQueryContext context)
    {
        if (context.PropertyAccess != null &&
            context.IsOnPropertyName &&
            context.PropertyAccess.Property is NameExpression propertyName)
        {
            return TextRange.FromSourceSpan(propertyName.Identifier.Range);
        }

        if (context.PropertyAccess != null &&
            context.IsOnPropertyOwner &&
            context.PropertyAccess.Object is NameExpression ownerName)
        {
            return TextRange.FromSourceSpan(ownerName.Identifier.Range);
        }

        if (context.Name != null)
        {
            return TextRange.FromSourceSpan(context.Name.Identifier.Range);
        }

        return default;
    }

    private static AuroraWorkspaceSnapshot CreateWorkspaceSnapshot(
        string sourceName,
        string sourceText,
        IEnumerable<AuroraWorkspaceDocument>? workspaceDocuments,
        out string normalizedSource)
    {
        var documents = new List<AuroraWorkspaceDocument>();
        normalizedSource = AuroraWorkspaceIndex.NormalizePath(sourceName);
        documents.Add(new AuroraWorkspaceDocument(normalizedSource, sourceText));
        if (workspaceDocuments != null)
        {
            foreach (var document in workspaceDocuments)
            {
                if (document == null)
                {
                    continue;
                }

                var normalized = AuroraWorkspaceIndex.NormalizePath(document.Path);
                if (string.Equals(normalized, normalizedSource, OperatingSystem.IsWindows() ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal))
                {
                    continue;
                }

                documents.Add(new AuroraWorkspaceDocument(normalized, document.Text));
            }
        }

        return new AuroraWorkspaceSnapshot(documents);
    }

    private bool TryGetWorkspaceText(string path, out string normalizedPath, out string text)
    {
        normalizedPath = ScriptPath.GetFullPath(_workspace.BaseDirectory, path);
        if (_workspace.TryGetDocument(normalizedPath, out var document))
        {
            text = document.Text;
            return true;
        }

        if (File.Exists(normalizedPath))
        {
            text = File.ReadAllText(normalizedPath);
            return true;
        }

        text = string.Empty;
        return false;
    }

    private AuroraWorkspaceIndex GetWorkspaceIndex(string rootPath)
    {
        lock (_indexLock)
        {
            var snapshot = CreateIndexSnapshot();
            return AuroraWorkspaceIndex.Build(_parseService, snapshot, rootPath, _workspaceIndexCache);
        }
    }

    private AuroraWorkspaceSnapshot CreateIndexSnapshot()
    {
        var workspaceSnapshot = _workspace.CreateSnapshot();
        if (!_options.IndexWorkspaceFiles)
        {
            return workspaceSnapshot;
        }

        var documents = new List<AuroraWorkspaceDocument>(workspaceSnapshot.Documents.Values);
        var seen = new HashSet<string>(workspaceSnapshot.Documents.Keys, AuroraWorkspaceSnapshot.PathComparer);
        foreach (var path in EnumerateWorkspaceScriptFiles(_workspace.BaseDirectory))
        {
            if (!seen.Add(path))
            {
                continue;
            }

            try
            {
                documents.Add(new AuroraWorkspaceDocument(path, File.ReadAllText(path)));
            }
            catch (Exception ex) when (IsFileReadFailure(ex))
            {
            }
        }

        return new AuroraWorkspaceSnapshot(documents, _workspace.BaseDirectory, workspaceSnapshot.Version);
    }

    private IEnumerable<string> EnumerateWorkspaceScriptFiles(string baseDirectory)
    {
        if (string.IsNullOrWhiteSpace(baseDirectory) ||
            !Directory.Exists(baseDirectory))
        {
            yield break;
        }

        var count = 0;
        var pending = new Queue<string>();
        pending.Enqueue(baseDirectory);
        while (pending.Count != 0)
        {
            var directory = pending.Dequeue();
            IEnumerable<string> childDirectories;
            try
            {
                childDirectories = Directory.EnumerateDirectories(directory).ToArray();
            }
            catch (Exception ex) when (IsFileReadFailure(ex))
            {
                continue;
            }

            foreach (var child in childDirectories)
            {
                if (!ShouldSkipWorkspaceDirectory(child))
                {
                    pending.Enqueue(child);
                }
            }

            IEnumerable<string> files;
            try
            {
                files = Directory.EnumerateFiles(directory, "*" + _options.Extension, SearchOption.TopDirectoryOnly).ToArray();
            }
            catch (Exception ex) when (IsFileReadFailure(ex))
            {
                continue;
            }

            foreach (var file in files)
            {
                if (++count > _options.MaxWorkspaceIndexFiles)
                {
                    yield break;
                }

                yield return ScriptPath.NormalizeFullPath(file);
            }
        }
    }

    private static bool ShouldSkipWorkspaceDirectory(string path)
    {
        var name = Path.GetFileName(path);
        return string.Equals(name, ".git", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(name, ".vs", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(name, "bin", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(name, "obj", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(name, "node_modules", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsFileReadFailure(Exception exception)
    {
        return exception is IOException
            or UnauthorizedAccessException
            or DirectoryNotFoundException
            or FileNotFoundException
            or PathTooLongException
            or NotSupportedException;
    }

    private DefinitionLocation? ResolveBuiltinDefinition(
        string sourceName,
        string sourceText,
        TextPosition position,
        AuroraWorkspaceSnapshot snapshot)
    {
        var parseResult = _parseService.ParseText(sourceName, sourceText, snapshot.BaseDirectory, snapshot);
        if (parseResult.Module == null)
        {
            return LightweightBuiltinDefinitionQuery.TryResolve(_builtinDocuments, sourceText, position, out var fallbackLocation)
                ? fallbackLocation
                : null;
        }

        var context = AstQuery.Find(parseResult.Module, position);
        if (context == null)
        {
            return LightweightBuiltinDefinitionQuery.TryResolve(_builtinDocuments, sourceText, position, out var fallbackLocation)
                ? fallbackLocation
                : null;
        }

        var localIndex = AuroraLocalSymbolIndex.Build(new AuroraModuleIndex(sourceName, sourceText, parseResult.Module));
        if (context.PropertyAccess != null &&
            context.IsOnPropertyOwner &&
            context.PropertyAccess.Object is NameExpression propertyOwner &&
            propertyOwner.Identifier.Range.Contains(position) &&
            !IsScriptDefinedName(parseResult.Module, localIndex, propertyOwner) &&
            _builtinDocuments.TryGetGlobalLocation(propertyOwner.Identifier.Value, out var ownerLocation))
        {
            return ownerLocation;
        }

        if (context.PropertyAccess != null &&
            context.IsOnPropertyName &&
            context.PropertyAccess.Object is NameExpression owner &&
            context.PropertyAccess.Property is NameExpression property &&
            !IsScriptDefinedName(parseResult.Module, localIndex, owner) &&
            _builtinDocuments.TryGetMemberLocation(owner.Identifier.Value, property.Identifier.Value, out var memberLocation))
        {
            return memberLocation;
        }

        if (context.Name != null &&
            !IsScriptDefinedName(parseResult.Module, localIndex, context.Name) &&
            _builtinDocuments.TryGetGlobalLocation(context.Name.Identifier.Value, out var globalLocation))
        {
            return globalLocation;
        }

        if (ShouldSuppressLightweightBuiltinFallback(parseResult.Module, localIndex, context))
        {
            return null;
        }

        return LightweightBuiltinDefinitionQuery.TryResolve(_builtinDocuments, sourceText, position, out var lightweightLocation)
            ? lightweightLocation
            : null;
    }

    private static bool ShouldSuppressLightweightBuiltinFallback(
        Compiler.Ast.ModuleDeclaration module,
        AuroraLocalSymbolIndex localIndex,
        AstQueryContext context)
    {
        if (context.PropertyAccess != null &&
            (context.IsOnPropertyOwner || context.IsOnPropertyName) &&
            context.PropertyAccess.Object is NameExpression owner &&
            IsScriptDefinedName(module, localIndex, owner))
        {
            return true;
        }

        return context.Name != null && IsScriptDefinedName(module, localIndex, context.Name);
    }

    private static bool IsScriptDefinedName(
        Compiler.Ast.ModuleDeclaration module,
        AuroraLocalSymbolIndex localIndex,
        NameExpression name)
    {
        if (localIndex.IsLocalReference(name))
        {
            return true;
        }

        var value = name.Identifier.Value;
        if (module.Imports != null)
        {
            for (var i = 0; i < module.Imports.Count; i++)
            {
                var importName = module.Imports[i].Name?.Value;
                if (!string.IsNullOrEmpty(importName) &&
                    string.Equals(importName, value, StringComparison.Ordinal))
                {
                    return true;
                }
            }
        }

        for (var i = 0; i < module.Statements.Count; i++)
        {
            switch (module.Statements[i])
            {
                case VariableDeclaration variable when variable.Name != null &&
                    string.Equals(variable.Name.Value, value, StringComparison.Ordinal):
                    return true;
                case Compiler.Ast.EnumDeclaration enumDeclaration when enumDeclaration.Identifier != null &&
                    string.Equals(enumDeclaration.Identifier.Value, value, StringComparison.Ordinal):
                    return true;
            }
        }

        for (var i = 0; i < module.Functions.Count; i++)
        {
            var functionName = module.Functions[i].Name?.Value;
            if (!string.IsNullOrEmpty(functionName) &&
                string.Equals(functionName, value, StringComparison.Ordinal))
            {
                return true;
            }
        }

        return false;
    }
}
