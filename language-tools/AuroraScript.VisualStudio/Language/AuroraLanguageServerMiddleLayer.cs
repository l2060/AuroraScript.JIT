using Microsoft.VisualStudio.LanguageServer.Client;
using Newtonsoft.Json.Linq;
using System;
using System.Threading.Tasks;

namespace AuroraScript.VisualStudio.Language;

internal sealed class AuroraLanguageServerMiddleLayer : ILanguageClientMiddleLayer
{
    private const string DefinitionMethodName = "textDocument/definition";
    private const string SemanticTokensFullMethodName = "textDocument/semanticTokens/full";
    private const string SemanticTokensRangeMethodName = "textDocument/semanticTokens/range";
    private const int SemanticTokenDataStride = 5;
    private const int NumberSemanticTokenType = 12;

    private readonly BuiltinDocumentManager _builtinDocuments;

    public AuroraLanguageServerMiddleLayer(BuiltinDocumentManager builtinDocuments)
    {
        _builtinDocuments = builtinDocuments;
    }

    public bool CanHandle(string methodName)
    {
        return string.Equals(methodName, DefinitionMethodName, StringComparison.Ordinal) ||
            IsSemanticTokensRequest(methodName) ||
            string.Equals(methodName, "textDocument/didOpen", StringComparison.Ordinal) ||
            string.Equals(methodName, "textDocument/didChange", StringComparison.Ordinal) ||
            string.Equals(methodName, "textDocument/didClose", StringComparison.Ordinal);
    }

    public async Task HandleNotificationAsync(string methodName, JToken methodParam, Func<JToken, Task> sendNotification)
    {
        if (IsBuiltinCacheDocument(methodParam))
        {
            return;
        }

        await sendNotification(methodParam).ConfigureAwait(false);
    }

    public async Task<JToken?> HandleRequestAsync(
        string methodName,
        JToken methodParam,
        Func<JToken, Task<JToken?>> sendRequest)
    {
        RewriteBuiltinCacheRequest(methodParam);
        var result = await sendRequest(methodParam).ConfigureAwait(false);
        if (IsSemanticTokensRequest(methodName))
        {
            return RemoveNumberSemanticTokens(result);
        }

        if (!string.Equals(methodName, DefinitionMethodName, StringComparison.Ordinal))
        {
            return result;
        }

        return await RewriteBuiltinDefinitionAsync(result).ConfigureAwait(false);
    }

    private static bool IsSemanticTokensRequest(string methodName)
    {
        return string.Equals(methodName, SemanticTokensFullMethodName, StringComparison.Ordinal) ||
            string.Equals(methodName, SemanticTokensRangeMethodName, StringComparison.Ordinal);
    }

    private static JToken? RemoveNumberSemanticTokens(JToken? result)
    {
        if (result is not JObject response ||
            response["data"] is not JArray data ||
            data.Count == 0 ||
            data.Count % SemanticTokenDataStride != 0)
        {
            return result;
        }

        var kept = new JArray();
        var line = 0;
        var character = 0;
        var previousKeptLine = 0;
        var previousKeptCharacter = 0;

        for (var i = 0; i < data.Count; i += SemanticTokenDataStride)
        {
            var deltaLine = data[i]!.Value<int>();
            var deltaStart = data[i + 1]!.Value<int>();
            var length = data[i + 2]!.Value<int>();
            var tokenType = data[i + 3]!.Value<int>();
            var modifiers = data[i + 4]!.Value<int>();

            if (deltaLine == 0)
            {
                character += deltaStart;
            }
            else
            {
                line += deltaLine;
                character = deltaStart;
            }

            if (tokenType == NumberSemanticTokenType)
            {
                continue;
            }

            var keptDeltaLine = line - previousKeptLine;
            kept.Add(keptDeltaLine);
            kept.Add(keptDeltaLine == 0 ? character - previousKeptCharacter : character);
            kept.Add(length);
            kept.Add(tokenType);
            kept.Add(modifiers);

            previousKeptLine = line;
            previousKeptCharacter = character;
        }

        response["data"] = kept;
        return response;
    }

    private void RewriteBuiltinCacheRequest(JToken methodParam)
    {
        if (methodParam is not JObject request ||
            request["textDocument"] is not JObject textDocument ||
            textDocument["uri"]?.Value<string>() is not { } uri ||
            !TryGetBuiltinUri(uri, out var builtinUri))
        {
            return;
        }

        textDocument["uri"] = builtinUri;
    }

    private bool IsBuiltinCacheDocument(JToken methodParam)
    {
        if (methodParam is not JObject request)
        {
            return false;
        }

        if (request["textDocument"] is JObject textDocument &&
            textDocument["uri"]?.Value<string>() is { } documentUri &&
            TryGetBuiltinUri(documentUri, out _))
        {
            return true;
        }

        return false;
    }

    private bool TryGetBuiltinUri(string documentUri, out string builtinUri)
    {
        builtinUri = string.Empty;
        if (!Uri.TryCreate(documentUri, UriKind.Absolute, out var parsed) || !parsed.IsFile)
        {
            return false;
        }

        return _builtinDocuments.TryGetBuiltinUriForFilePath(parsed.LocalPath, out builtinUri);
    }

    private async Task<JToken?> RewriteBuiltinDefinitionAsync(JToken? result)
    {
        if (result == null || result.Type == JTokenType.Null)
        {
            return result;
        }

        if (result is JArray array)
        {
            foreach (var item in array)
            {
                await RewriteLocationAsync(item).ConfigureAwait(false);
            }

            return array;
        }

        await RewriteLocationAsync(result).ConfigureAwait(false);
        return result;
    }

    private async Task RewriteLocationAsync(JToken token)
    {
        if (token is not JObject location ||
            location["uri"]?.Value<string>() is not { } uri ||
            !BuiltinDocumentManager.IsBuiltinUri(uri))
        {
            return;
        }

        var filePath = await _builtinDocuments.OpenOrGetDocumentAsync(uri, default).ConfigureAwait(false);
        location["uri"] = new Uri(filePath).AbsoluteUri;
    }
}
