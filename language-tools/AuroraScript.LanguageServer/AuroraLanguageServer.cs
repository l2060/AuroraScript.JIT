using AuroraScript.LanguageServer.Protocol;
using AuroraScript.LanguageServices;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json.Nodes;
using System.Threading;
using System.Threading.Tasks;

namespace AuroraScript.LanguageServer;

public sealed class AuroraLanguageServer
{
    private readonly AuroraLanguageService _languageService;

    public AuroraLanguageServer(AuroraLanguageService languageService)
    {
        _languageService = languageService ?? throw new ArgumentNullException(nameof(languageService));
    }

    public Task<LspResult> HandleAsync(JsonObject message, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var method = message.TryGetPropertyValue("method", out var methodNode)
            ? methodNode?.GetValue<string>()
            : null;
        var id = message.TryGetPropertyValue("id", out var idNode) ? idNode?.DeepClone() : null;
        var parameters = message.TryGetPropertyValue("params", out var paramsNode) && paramsNode is JsonObject paramsObject
            ? paramsObject
            : new JsonObject();

        if (string.IsNullOrEmpty(method))
        {
            return Task.FromResult(LspResult.Empty);
        }

        var result = id == null
            ? HandleNotification(method, parameters)
            : HandleRequest(id, method, parameters);
        return Task.FromResult(result);
    }

    private LspResult HandleRequest(JsonNode id, string method, JsonObject parameters)
    {
        switch (method)
        {
            case "initialize":
                ConfigureWorkspace(parameters);
                return Respond(id, InitializeResult());
            case "shutdown":
                return Respond(id, null);
            case "textDocument/hover":
                return Respond(id, HandleHover(parameters));
            case "textDocument/completion":
                return Respond(id, HandleCompletion(parameters));
            case "textDocument/signatureHelp":
                return Respond(id, HandleSignatureHelp(parameters));
            case "textDocument/definition":
                return Respond(id, HandleDefinition(parameters));
            case "textDocument/references":
                return Respond(id, HandleReferences(parameters));
            case "textDocument/rename":
                return Respond(id, HandleRename(parameters));
            case "textDocument/semanticTokens/full":
                return Respond(id, HandleSemanticTokens(parameters));
            case "aurora/builtinDocument":
                return Respond(id, HandleBuiltinDocument(parameters));
            default:
                return Respond(id, null);
        }
    }

    private LspResult HandleNotification(string method, JsonObject parameters)
    {
        switch (method)
        {
            case "initialized":
                return LspResult.Empty;
            case "exit":
                return new LspResult(null, Array.Empty<LspNotification>(), shutdown: true);
            case "textDocument/didOpen":
                return HandleDidOpen(parameters);
            case "textDocument/didChange":
                return HandleDidChange(parameters);
            case "textDocument/didClose":
                return HandleDidClose(parameters);
            default:
                return LspResult.Empty;
        }
    }

    private LspResult HandleDidOpen(JsonObject parameters)
    {
        var textDocument = parameters["textDocument"]!.AsObject();
        var uri = textDocument["uri"]!.GetValue<string>();
        var text = textDocument["text"]!.GetValue<string>();
        var path = SourceName(uri);
        _languageService.OpenOrUpdateDocument(path, text);
        return PublishWorkspaceDiagnostics();
    }

    private LspResult HandleDidChange(JsonObject parameters)
    {
        var textDocument = parameters["textDocument"]!.AsObject();
        var uri = textDocument["uri"]!.GetValue<string>();
        var changes = parameters["contentChanges"]!.AsArray();
        if (changes.Count == 0 || changes[changes.Count - 1] is not JsonObject lastChange)
        {
            return LspResult.Empty;
        }

        var text = lastChange["text"]!.GetValue<string>();
        var path = SourceName(uri);
        _languageService.OpenOrUpdateDocument(path, text);
        return PublishWorkspaceDiagnostics();
    }

    private LspResult HandleDidClose(JsonObject parameters)
    {
        var textDocument = parameters["textDocument"]!.AsObject();
        var uri = textDocument["uri"]!.GetValue<string>();
        _languageService.CloseDocument(SourceName(uri));
        return PublishWorkspaceDiagnostics(uri);
    }

    private JsonNode? HandleHover(JsonObject parameters)
    {
        var uri = LspMapper.ReadTextDocumentUri(parameters);
        var hover = _languageService.GetHover(SourceName(uri), LspMapper.ReadPosition(parameters));
        return hover == null ? null : LspMapper.Hover(hover);
    }

    private JsonNode HandleCompletion(JsonObject parameters)
    {
        var uri = LspMapper.ReadTextDocumentUri(parameters);
        var result = _languageService.GetCompletions(SourceName(uri), LspMapper.ReadPosition(parameters));
        return LspMapper.CompletionItems(result);
    }

    private JsonNode? HandleSignatureHelp(JsonObject parameters)
    {
        var uri = LspMapper.ReadTextDocumentUri(parameters);
        var result = _languageService.GetSignatureHelp(SourceName(uri), LspMapper.ReadPosition(parameters));
        return result == null ? null : LspMapper.SignatureHelp(result);
    }

    private JsonNode? HandleDefinition(JsonObject parameters)
    {
        var uri = LspMapper.ReadTextDocumentUri(parameters);
        var definition = _languageService.GetDefinition(SourceName(uri), LspMapper.ReadPosition(parameters));
        return definition == null ? null : LspMapper.Location(definition);
    }

    private JsonNode HandleReferences(JsonObject parameters)
    {
        var uri = LspMapper.ReadTextDocumentUri(parameters);
        var includeDeclaration = true;
        if (parameters.TryGetPropertyValue("context", out var contextNode) &&
            contextNode is JsonObject context &&
            context.TryGetPropertyValue("includeDeclaration", out var includeNode) &&
            includeNode != null)
        {
            includeDeclaration = includeNode.GetValue<bool>();
        }

        var references = _languageService.GetReferences(SourceName(uri), LspMapper.ReadPosition(parameters), includeDeclaration);
        return LspMapper.Locations(references);
    }

    private JsonNode HandleRename(JsonObject parameters)
    {
        var uri = LspMapper.ReadTextDocumentUri(parameters);
        var newName = parameters.TryGetPropertyValue("newName", out var newNameNode) && newNameNode != null
            ? newNameNode.GetValue<string>()
            : string.Empty;
        var result = _languageService.Rename(SourceName(uri), LspMapper.ReadPosition(parameters), newName);
        return LspMapper.WorkspaceEdit(result);
    }

    private JsonNode HandleSemanticTokens(JsonObject parameters)
    {
        var uri = LspMapper.ReadTextDocumentUri(parameters);
        var result = _languageService.GetSemanticTokens(SourceName(uri));
        return LspMapper.SemanticTokens(result);
    }

    private JsonNode? HandleBuiltinDocument(JsonObject parameters)
    {
        var uri = parameters.TryGetPropertyValue("uri", out var uriNode) && uriNode != null
            ? uriNode.GetValue<string>()
            : string.Empty;
        var document = _languageService.GetBuiltinDocument(uri);
        if (document == null)
        {
            return null;
        }

        return new JsonObject
        {
            ["uri"] = document.Uri,
            ["languageId"] = document.LanguageId,
            ["text"] = document.Text
        };
    }

    private void ConfigureWorkspace(JsonObject parameters)
    {
        if (parameters.TryGetPropertyValue("locale", out var localeNode) &&
            localeNode != null)
        {
            _languageService.SetDocumentationLocale(localeNode.GetValue<string>());
        }

        if (TryReadWorkspaceRoot(parameters, out var root))
        {
            _languageService.SetWorkspaceRoot(root);
        }
    }

    private static bool TryReadWorkspaceRoot(JsonObject parameters, out string root)
    {
        root = string.Empty;
        if (parameters.TryGetPropertyValue("workspaceFolders", out var foldersNode) &&
            foldersNode is JsonArray folders &&
            folders.Count > 0)
        {
            for (var i = 0; i < folders.Count; i++)
            {
                if (folders[i] is JsonObject folder &&
                    folder.TryGetPropertyValue("uri", out var uriNode) &&
                    uriNode != null &&
                    TryGetSourceName(uriNode.GetValue<string>(), out root))
                {
                    return true;
                }
            }
        }

        if (parameters.TryGetPropertyValue("rootUri", out var rootUriNode) &&
            rootUriNode != null &&
            TryGetSourceName(rootUriNode.GetValue<string>(), out root))
        {
            return true;
        }

        if (parameters.TryGetPropertyValue("rootPath", out var rootPathNode) &&
            rootPathNode != null)
        {
            root = rootPathNode.GetValue<string>();
            return !string.IsNullOrWhiteSpace(root);
        }

        return false;
    }

    private static bool TryGetSourceName(string value, out string sourceName)
    {
        sourceName = string.Empty;
        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        sourceName = SourceName(value);
        return !string.IsNullOrWhiteSpace(sourceName);
    }

    private LspResult PublishWorkspaceDiagnostics(params string[] clearedUris)
    {
        var notifications = new List<LspNotification>();
        if (clearedUris != null)
        {
            for (var i = 0; i < clearedUris.Length; i++)
            {
                if (string.IsNullOrWhiteSpace(clearedUris[i]))
                {
                    continue;
                }

                notifications.Add(new LspNotification("textDocument/publishDiagnostics", new JsonObject
                {
                    ["uri"] = clearedUris[i],
                    ["diagnostics"] = new JsonArray()
                }));
            }
        }

        foreach (var document in _languageService.Workspace.Documents)
        {
            var diagnostics = _languageService.GetDiagnostics(document.Path);
            notifications.Add(new LspNotification("textDocument/publishDiagnostics", new JsonObject
            {
                ["uri"] = UriFromSourceName(document.Path),
                ["diagnostics"] = LspMapper.Diagnostics(diagnostics)
            }));
        }

        return new LspResult(null, notifications, false);
    }

    private static LspResult Respond(JsonNode id, JsonNode? result)
    {
        return new LspResult(new LspResponse(id, result), Array.Empty<LspNotification>(), false);
    }

    private static JsonObject InitializeResult()
    {
        return new JsonObject
        {
            ["serverInfo"] = new JsonObject
            {
                ["name"] = "AuroraScript Language Server",
                ["version"] = "0.1.0"
            },
            ["capabilities"] = new JsonObject
            {
                ["textDocumentSync"] = 1,
                ["hoverProvider"] = true,
                ["completionProvider"] = new JsonObject
                {
                    ["resolveProvider"] = false,
                    ["triggerCharacters"] = new JsonArray(".")
                },
                ["signatureHelpProvider"] = new JsonObject
                {
                    ["triggerCharacters"] = new JsonArray("(", ",")
                },
                ["definitionProvider"] = true,
                ["referencesProvider"] = true,
                ["renameProvider"] = true,
                ["semanticTokensProvider"] = new JsonObject
                {
                    ["legend"] = LspMapper.SemanticTokenLegend(),
                    ["range"] = false,
                    ["full"] = true
                }
            }
        };
    }

    private static string SourceName(string uri)
    {
        if (Uri.TryCreate(uri, UriKind.Absolute, out var parsed) && parsed.IsFile)
        {
            return parsed.LocalPath;
        }

        return uri;
    }

    private static string UriFromSourceName(string sourceName)
    {
        if (Path.IsPathRooted(sourceName))
        {
            return new Uri(sourceName).AbsoluteUri;
        }

        return sourceName;
    }

}
