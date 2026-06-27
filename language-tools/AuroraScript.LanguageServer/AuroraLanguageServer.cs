using AuroraScript.LanguageServer.Protocol;
using AuroraScript.LanguageServices;
using System;
using System.Collections.Generic;
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
        return PublishDiagnostics(uri);
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
        return PublishDiagnostics(uri);
    }

    private LspResult HandleDidClose(JsonObject parameters)
    {
        var textDocument = parameters["textDocument"]!.AsObject();
        var uri = textDocument["uri"]!.GetValue<string>();
        _languageService.CloseDocument(SourceName(uri));
        return new LspResult(null, new[]
        {
            new LspNotification("textDocument/publishDiagnostics", new JsonObject
            {
                ["uri"] = uri,
                ["diagnostics"] = new JsonArray()
            })
        }, false);
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

    private LspResult PublishDiagnostics(string uri)
    {
        var diagnostics = _languageService.GetDiagnostics(SourceName(uri));
        return new LspResult(null, new[]
        {
            new LspNotification("textDocument/publishDiagnostics", new JsonObject
            {
                ["uri"] = uri,
                ["diagnostics"] = LspMapper.Diagnostics(diagnostics)
            })
        }, false);
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

}
