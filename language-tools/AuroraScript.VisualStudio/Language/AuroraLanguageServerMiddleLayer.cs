using Microsoft.VisualStudio.LanguageServer.Client;
using Microsoft.VisualStudio.Threading;
using Newtonsoft.Json.Linq;
using System;
using System.Threading.Tasks;

namespace AuroraScript.VisualStudio.Language;

internal sealed class AuroraLanguageServerMiddleLayer : ILanguageClientMiddleLayer
{
    private readonly BuiltinDocumentManager _builtinDocuments;
    private readonly JoinableTaskContext _joinableTaskContext;

    public AuroraLanguageServerMiddleLayer(
        BuiltinDocumentManager builtinDocuments,
        JoinableTaskContext joinableTaskContext)
    {
        _builtinDocuments = builtinDocuments;
        _joinableTaskContext = joinableTaskContext;
    }

    public bool CanHandle(string methodName)
    {
        return string.Equals(methodName, "textDocument/definition", StringComparison.Ordinal) ||
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
        if (!string.Equals(methodName, "textDocument/definition", StringComparison.Ordinal))
        {
            return result;
        }

        return await RewriteBuiltinDefinitionAsync(result).ConfigureAwait(false);
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

        var filePath = await _joinableTaskContext.Factory.RunAsync(
            async () => await _builtinDocuments.OpenOrGetDocumentAsync(uri, default).ConfigureAwait(false)).Task.ConfigureAwait(false);
        location["uri"] = new Uri(filePath).AbsoluteUri;
    }
}
