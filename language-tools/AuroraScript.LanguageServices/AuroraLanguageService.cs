using AuroraScript.Core;
using AuroraScript.Compiler.GlobalDeclarations;
using AuroraScript.Compiler.Ast.Expressions;
using AuroraScript.LanguageServices.Builtins;
using AuroraScript.LanguageServices.Diagnostics;
using AuroraScript.LanguageServices.Features.Completion;
using AuroraScript.LanguageServices.Features.Definition;
using AuroraScript.LanguageServices.Features.Formatting;
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
    private GlobalDeclarationIndex _globalDeclarationIndex = GlobalDeclarationIndex.Empty;
    private long _globalDeclarationIndexSignature;

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
            _globalDeclarationIndex = GlobalDeclarationIndex.Empty;
            _globalDeclarationIndexSignature = 0;
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

    public void WarmWorkspaceIndex()
    {
        lock (_indexLock)
        {
            var snapshot = CreateIndexSnapshot();
            GetGlobalDeclarationIndex(snapshot);
            AuroraWorkspaceIndex.Build(_parseService, snapshot, _workspace.BaseDirectory, _workspaceIndexCache);
        }
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
        return AppendGlobalDiagnostics(analyzer.Analyze(parseResult).Diagnostics, baseDirectory, sourceName, sourceText, null);
    }

    public IReadOnlyList<LanguageDiagnostic> GetDiagnostics(string path)
    {
        if (!TryGetWorkspaceText(path, out var normalizedPath, out _))
        {
            return Array.Empty<LanguageDiagnostic>();
        }

        var parseResult = ParseDocument(path);
        var analyzer = new AuroraSemanticAnalyzer(_builtins);
        var diagnostics = analyzer.Analyze(parseResult).Diagnostics;
        var snapshot = CreateIndexSnapshot();
        var globalIndex = GetGlobalDeclarationIndex(snapshot);
        return AppendGlobalDiagnostics(diagnostics, globalIndex, normalizedPath);
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

        if (BuiltinQuery.TryGetHover(_builtins, parseResult.Module, context, DocumentationLocale, out var hover))
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

        if (BuiltinQuery.TryGetHover(_builtins, parseResult.Module, context, DocumentationLocale, out var hover))
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
        var completionSourceText = GetCompletionSourceText(sourceText, position);
        if (LightweightCompletionQuery.TryGetMemberOwner(sourceText, position, out var ownerName))
        {
            var completionParseResult = ParseText(sourceName, completionSourceText, baseDirectory);
            var memberCompletions = BuiltinQuery.GetMemberCompletions(
                _builtins,
                completionParseResult.Module,
                ownerName,
                DocumentationLocale);
            var scriptMemberCompletions = GetScriptMemberCompletions(sourceName, completionSourceText, ownerName, baseDirectory);
            var mergedMemberCompletions = MergeCompletions(memberCompletions, scriptMemberCompletions);
            if (mergedMemberCompletions.Items.Count != 0)
            {
                return mergedMemberCompletions;
            }
        }

        var parseResult = ParseText(sourceName, sourceText, baseDirectory);
        if (parseResult.Module == null && !string.Equals(completionSourceText, sourceText, StringComparison.Ordinal))
        {
            parseResult = ParseText(sourceName, completionSourceText, baseDirectory);
        }

        if (parseResult.Module == null)
        {
            return BuiltinQuery.GetCompletions(_builtins, null, null, DocumentationLocale);
        }

        var context = AstQuery.Find(parseResult.Module, position);
        var builtinCompletions = BuiltinQuery.GetCompletions(
            _builtins,
            parseResult.Module,
            context,
            DocumentationLocale);
        var scriptCompletions = GetScriptCompletions(sourceName, completionSourceText, position, baseDirectory);
        return MergeCompletions(scriptCompletions, builtinCompletions);
    }

    public CompletionResult GetCompletions(string path, TextPosition position)
    {
        if (!TryGetWorkspaceText(path, out var normalizedPath, out var text))
        {
            return BuiltinQuery.GetCompletions(_builtins, null, null, DocumentationLocale);
        }

        var completionText = GetCompletionSourceText(text, position);
        if (LightweightCompletionQuery.TryGetMemberOwner(text, position, out var ownerName))
        {
            var completionParseResult = _parseService.ParseText(
                normalizedPath,
                completionText,
                _workspace.BaseDirectory,
                _workspace.CreateSnapshot());
            var memberCompletions = BuiltinQuery.GetMemberCompletions(
                _builtins,
                completionParseResult.Module,
                ownerName,
                DocumentationLocale);
            var index = GetWorkspaceCompletionIndex(normalizedPath, completionText);
            var scriptMemberCompletions = AuroraCompletionResolver.GetMemberCompletions(index, normalizedPath, ownerName);
            var mergedMemberCompletions = MergeCompletions(memberCompletions, scriptMemberCompletions);
            if (mergedMemberCompletions.Items.Count != 0)
            {
                return mergedMemberCompletions;
            }
        }

        var parseResult = _parseService.ParseText(normalizedPath, text, _workspace.BaseDirectory, _workspace.CreateSnapshot());
        if (parseResult.Module == null && !string.Equals(completionText, text, StringComparison.Ordinal))
        {
            parseResult = _parseService.ParseText(normalizedPath, completionText, _workspace.BaseDirectory, _workspace.CreateSnapshot());
        }

        if (parseResult.Module == null)
        {
            return BuiltinQuery.GetCompletions(_builtins, null, null, DocumentationLocale);
        }

        var context = AstQuery.Find(parseResult.Module, position);
        var builtinCompletions = BuiltinQuery.GetCompletions(
            _builtins,
            parseResult.Module,
            context,
            DocumentationLocale);
        var workspaceIndex = GetWorkspaceCompletionIndex(normalizedPath, completionText);
        var scriptCompletions = AuroraCompletionResolver.GetCompletions(workspaceIndex, normalizedPath, position);
        return MergeCompletions(scriptCompletions, builtinCompletions);
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

        return BuiltinQuery.GetSignatureHelp(
            _builtins,
            parseResult.Module,
            context,
            position,
            DocumentationLocale);
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
        var globalIndex = BuildGlobalDeclarationIndex(snapshot);
        return ResolveBuiltinDefinition(normalizedSource, sourceText, position, snapshot) ??
            AuroraDefinitionResolver.Resolve(index, normalizedSource, position, globalIndex);
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

        AuroraWorkspaceSnapshot snapshot;
        AuroraWorkspaceIndex index;
        GlobalDeclarationIndex globalIndex;
        lock (_indexLock)
        {
            snapshot = CreateIndexSnapshot();
            globalIndex = GetGlobalDeclarationIndex(snapshot);
            index = AuroraWorkspaceIndex.Build(_parseService, snapshot, normalizedPath, _workspaceIndexCache);
        }

        return ResolveBuiltinDefinition(normalizedPath, text, position, snapshot) ??
            AuroraDefinitionResolver.Resolve(index, normalizedPath, position, globalIndex);
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
        if (!TryGetWorkspaceText(path, out var normalizedPath, out var text))
        {
            return new SemanticTokensResult(Array.Empty<SemanticToken>());
        }

        GlobalDeclarationIndex globalIndex;
        lock (_indexLock)
        {
            globalIndex = GetGlobalDeclarationIndex(CreateIndexSnapshot());
        }

        var externalSymbols = SemanticExternalSymbols.FromGlobalDeclarationIndex(globalIndex);
        return SemanticTokenScanner.Scan(normalizedPath, text, _workspace.BaseDirectory, _builtins, externalSymbols);
    }

    public FormattingResult FormatDocument(string sourceName, string sourceText, FormattingOptions options)
    {
        return AuroraDocumentFormatter.Format(sourceName, sourceText, options);
    }

    public FormattingResult FormatDocument(string path, FormattingOptions options)
    {
        if (!TryGetWorkspaceText(path, out var normalizedPath, out var text))
        {
            return FormattingResult.Empty;
        }

        return FormatDocument(normalizedPath, text, options);
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

    private CompletionResult GetScriptCompletions(
        string sourceName,
        string sourceText,
        TextPosition position,
        string? baseDirectory)
    {
        var snapshot = CreateWorkspaceSnapshot(sourceName, sourceText, null, out var normalizedSource, baseDirectory);
        var index = AuroraWorkspaceIndex.Build(_parseService, snapshot, normalizedSource);
        return AuroraCompletionResolver.GetCompletions(index, normalizedSource, position);
    }

    private CompletionResult GetScriptMemberCompletions(
        string sourceName,
        string sourceText,
        string ownerName,
        string? baseDirectory)
    {
        var snapshot = CreateWorkspaceSnapshot(sourceName, sourceText, null, out var normalizedSource, baseDirectory);
        var index = AuroraWorkspaceIndex.Build(_parseService, snapshot, normalizedSource);
        return AuroraCompletionResolver.GetMemberCompletions(index, normalizedSource, ownerName);
    }

    private static CompletionResult MergeCompletions(params CompletionResult[] results)
    {
        var items = new List<CompletionItem>();
        var seen = new HashSet<string>(StringComparer.Ordinal);
        foreach (var result in results)
        {
            if (result == null)
            {
                continue;
            }

            for (var i = 0; i < result.Items.Count; i++)
            {
                var item = result.Items[i];
                if (seen.Add(item.Label))
                {
                    items.Add(item);
                }
            }
        }

        return new CompletionResult(items);
    }

    private static string GetCompletionSourceText(string sourceText, TextPosition position)
    {
        var offset = TextPositionMapper.ToOffset(sourceText, position);
        if (offset < 0 || offset > sourceText.Length)
        {
            return sourceText;
        }

        var previous = PreviousNonWhitespace(sourceText, offset);
        if (previous >= 0 && sourceText[previous] == '.')
        {
            return sourceText.Insert(offset, "__completion__;");
        }

        if (previous >= 0 &&
            sourceText[previous] != ';' &&
            sourceText[previous] != '{' &&
            sourceText[previous] != '}')
        {
            return sourceText.Insert(offset, ";");
        }

        return sourceText;
    }

    private static int PreviousNonWhitespace(string text, int offset)
    {
        for (var i = Math.Min(offset, text.Length) - 1; i >= 0; i--)
        {
            if (!char.IsWhiteSpace(text[i]))
            {
                return i;
            }
        }

        return -1;
    }

    private static AuroraWorkspaceSnapshot CreateWorkspaceSnapshot(
        string sourceName,
        string sourceText,
        IEnumerable<AuroraWorkspaceDocument>? workspaceDocuments,
        out string normalizedSource,
        string? baseDirectory = null)
    {
        var documents = new List<AuroraWorkspaceDocument>();
        normalizedSource = string.IsNullOrWhiteSpace(baseDirectory)
            ? AuroraWorkspaceIndex.NormalizePath(sourceName)
            : AuroraWorkspaceSnapshot.NormalizePath(sourceName, baseDirectory);
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
            GetGlobalDeclarationIndex(snapshot);
            return AuroraWorkspaceIndex.Build(_parseService, snapshot, rootPath, _workspaceIndexCache);
        }
    }

    private AuroraWorkspaceIndex GetWorkspaceCompletionIndex(string rootPath, string sourceText)
    {
        var snapshot = CreateCompletionSnapshot(rootPath, sourceText);
        return AuroraWorkspaceIndex.Build(_parseService, snapshot, rootPath);
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

    private GlobalDeclarationIndex GetGlobalDeclarationIndex(AuroraWorkspaceSnapshot snapshot)
    {
        var signature = ComputeGlobalDeclarationSnapshotSignature(snapshot);
        if (_globalDeclarationIndexSignature == signature)
        {
            return _globalDeclarationIndex;
        }

        _globalDeclarationIndex = BuildGlobalDeclarationIndex(snapshot);
        _globalDeclarationIndexSignature = signature;
        return _globalDeclarationIndex;
    }

    private static long ComputeGlobalDeclarationSnapshotSignature(AuroraWorkspaceSnapshot snapshot)
    {
        unchecked
        {
            var hash = 1469598103934665603L;
            hash = AddHash(hash, snapshot.BaseDirectory);
            hash = (hash ^ snapshot.Version) * 1099511628211L;
            foreach (var document in snapshot.Documents.Values.OrderBy(document => document.Path, AuroraWorkspaceSnapshot.PathComparer))
            {
                hash = AddHash(hash, document.Path);
                hash = (hash ^ document.Version) * 1099511628211L;
                hash = (hash ^ document.Text.Length) * 1099511628211L;
                hash = (hash ^ GetLastWriteTimeTicks(document.Path)) * 1099511628211L;
            }

            return hash == 0 ? 1 : hash;
        }
    }

    private static long GetLastWriteTimeTicks(string path)
    {
        try
        {
            return File.Exists(path) ? File.GetLastWriteTimeUtc(path).Ticks : 0;
        }
        catch (Exception ex) when (IsFileReadFailure(ex))
        {
            return 0;
        }
    }

    private static long AddHash(long hash, string? value)
    {
        unchecked
        {
            if (value == null)
            {
                return (hash ^ -1) * 1099511628211L;
            }

            for (var i = 0; i < value.Length; i++)
            {
                hash = (hash ^ value[i]) * 1099511628211L;
            }

            return hash;
        }
    }

    private static GlobalDeclarationIndex BuildGlobalDeclarationIndex(AuroraWorkspaceSnapshot snapshot)
    {
        var builder = new GlobalDeclarationWorkspaceIndexBuilder();
        foreach (var document in snapshot.Documents.Values)
        {
            if (!GlobalDeclarationScanner.IsProjectSource(snapshot.BaseDirectory, document.Path))
            {
                continue;
            }

            builder.AddFile(document.Path, document.Text);
        }

        return builder.ToIndex();
    }

    private IReadOnlyList<LanguageDiagnostic> AppendGlobalDiagnostics(
        IReadOnlyList<LanguageDiagnostic> diagnostics,
        string? baseDirectory,
        string sourceName,
        string sourceText,
        IEnumerable<AuroraWorkspaceDocument>? workspaceDocuments)
    {
        var snapshot = CreateWorkspaceSnapshot(sourceName, sourceText, workspaceDocuments, out _, baseDirectory);
        return AppendGlobalDiagnostics(diagnostics, BuildGlobalDeclarationIndex(snapshot));
    }

    private static IReadOnlyList<LanguageDiagnostic> AppendGlobalDiagnostics(
        IReadOnlyList<LanguageDiagnostic> diagnostics,
        GlobalDeclarationIndex globalIndex)
    {
        return AppendGlobalDiagnostics(diagnostics, globalIndex, pathFilter: null);
    }

    private static IReadOnlyList<LanguageDiagnostic> AppendGlobalDiagnostics(
        IReadOnlyList<LanguageDiagnostic> diagnostics,
        GlobalDeclarationIndex globalIndex,
        string? pathFilter)
    {
        if (globalIndex.Diagnostics.Count == 0)
        {
            return diagnostics;
        }

        var result = new List<LanguageDiagnostic>(diagnostics.Count + globalIndex.Diagnostics.Count);
        result.AddRange(diagnostics);
        for (var i = 0; i < globalIndex.Diagnostics.Count; i++)
        {
            var diagnostic = globalIndex.Diagnostics[i];
            if (!string.IsNullOrEmpty(pathFilter) &&
                !string.Equals(
                    AuroraWorkspaceIndex.NormalizePath(diagnostic.FileName),
                    AuroraWorkspaceIndex.NormalizePath(pathFilter),
                    OperatingSystem.IsWindows() ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal))
            {
                continue;
            }

            result.Add(new LanguageDiagnostic(
                "AURORA-GLOBAL",
                diagnostic.Message,
                TextRange.FromSourceSpan(diagnostic.Location),
                LanguageDiagnosticSeverity.Error));
        }

        return result.Count == diagnostics.Count ? diagnostics : result;
    }

    private AuroraWorkspaceSnapshot CreateCompletionSnapshot(string rootPath, string sourceText)
    {
        var workspaceSnapshot = _workspace.CreateSnapshot();
        var documents = new List<AuroraWorkspaceDocument>();
        var normalizedRoot = AuroraWorkspaceIndex.NormalizePath(rootPath);
        var foundRoot = false;
        foreach (var document in workspaceSnapshot.Documents.Values)
        {
            if (string.Equals(document.Path, normalizedRoot, OperatingSystem.IsWindows() ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal))
            {
                documents.Add(new AuroraWorkspaceDocument(document.Path, sourceText, document.Version));
                foundRoot = true;
            }
            else
            {
                documents.Add(document);
            }
        }

        if (!foundRoot)
        {
            documents.Add(new AuroraWorkspaceDocument(normalizedRoot, sourceText));
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

        if (context.TypeReference != null &&
            _builtinDocuments.TryGetGlobalLocation(context.TypeReference.Value, out var typeLocation))
        {
            return typeLocation;
        }

        if (context.PropertyAccess != null &&
            context.PropertyAccess.Object is NameExpression moduleOwner &&
            BuiltinModuleQuery.TryResolve(
                _builtins,
                parseResult.Module,
                moduleOwner.Identifier.Value,
                out var builtinModule))
        {
            if (context.IsOnPropertyOwner &&
                moduleOwner.Identifier.Range.Contains(position) &&
                _builtinDocuments.TryGetModuleLocation(builtinModule.ModulePath, out var moduleLocation))
            {
                return moduleLocation;
            }

            if (context.IsOnPropertyName &&
                context.PropertyAccess.Property is NameExpression moduleMember &&
                _builtinDocuments.TryGetModuleMemberLocation(
                    builtinModule.ModulePath,
                    moduleMember.Identifier.Value,
                    out var moduleMemberLocation))
            {
                return moduleMemberLocation;
            }
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
