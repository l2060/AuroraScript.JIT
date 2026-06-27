using AuroraScript.Core;
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

namespace AuroraScript.LanguageServices;

public sealed class AuroraLanguageService
{
    private readonly AuroraLanguageServiceOptions _options;
    private readonly AuroraParseService _parseService;
    private readonly AuroraWorkspace _workspace;
    private readonly BuiltinApiCatalog _builtins;
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
    }

    public AuroraWorkspace Workspace => _workspace;

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
            return null;
        }

        var context = AstQuery.Find(parseResult.Module, position);
        if (context == null)
        {
            return null;
        }

        if (BuiltinQuery.TryGetHover(_builtins, context, out var hover))
        {
            return hover;
        }

        return null;
    }

    public HoverResult? GetHover(string path, TextPosition position)
    {
        if (!TryGetWorkspaceText(path, out _, out var text))
        {
            return null;
        }

        return GetHover(path, text, position, _workspace.BaseDirectory);
    }

    public CompletionResult GetCompletions(string sourceName, string sourceText, TextPosition position, string? baseDirectory = null)
    {
        if (LightweightCompletionQuery.TryGetMemberOwner(sourceText, position, out var ownerName))
        {
            var memberCompletions = BuiltinQuery.GetMemberCompletions(_builtins, ownerName);
            if (memberCompletions.Items.Count != 0)
            {
                return memberCompletions;
            }
        }

        var parseResult = ParseText(sourceName, sourceText, baseDirectory);
        if (parseResult.Module == null)
        {
            return BuiltinQuery.GetCompletions(_builtins, null);
        }

        var context = AstQuery.Find(parseResult.Module, position);
        return BuiltinQuery.GetCompletions(_builtins, context);
    }

    public CompletionResult GetCompletions(string path, TextPosition position)
    {
        if (!TryGetWorkspaceText(path, out _, out var text))
        {
            return BuiltinQuery.GetCompletions(_builtins, null);
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

        return BuiltinQuery.GetSignatureHelp(_builtins, context, position);
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
        var snapshot = CreateWorkspaceSnapshot(sourceName, sourceText, workspaceDocuments, out var normalizedSource);
        var index = AuroraWorkspaceIndex.Build(_parseService, snapshot, normalizedSource);
        return AuroraDefinitionResolver.Resolve(index, normalizedSource, position);
    }

    public DefinitionLocation? GetDefinition(string path, TextPosition position)
    {
        if (!TryGetWorkspaceText(path, out var normalizedPath, out _))
        {
            return null;
        }

        var index = GetWorkspaceIndex(normalizedPath);
        return AuroraDefinitionResolver.Resolve(index, normalizedPath, position);
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
        return SemanticTokenScanner.Scan(sourceName, sourceText, baseDirectory);
    }

    public SemanticTokensResult GetSemanticTokens(string path)
    {
        if (!TryGetWorkspaceText(path, out _, out var text))
        {
            return new SemanticTokensResult(Array.Empty<SemanticToken>());
        }

        return GetSemanticTokens(path, text, _workspace.BaseDirectory);
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
            var snapshot = _workspace.CreateSnapshot();
            return AuroraWorkspaceIndex.Build(_parseService, snapshot, rootPath, _workspaceIndexCache);
        }
    }
}
