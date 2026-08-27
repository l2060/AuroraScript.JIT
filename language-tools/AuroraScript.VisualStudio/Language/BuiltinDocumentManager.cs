using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Threading;
using Newtonsoft.Json.Linq;
using StreamJsonRpc;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace AuroraScript.VisualStudio.Language;

[Export]
internal sealed class BuiltinDocumentManager
{
    public const string BuiltinScheme = "aurora-builtin";

    private readonly JoinableTaskContext _joinableTaskContext;
    private readonly Dictionary<string, string> _filePathsByBuiltinUri = new(StringComparer.Ordinal);
    private readonly Dictionary<string, string> _builtinUrisByFilePath = new(StringComparer.OrdinalIgnoreCase);
    private readonly object _gate = new();
    private JsonRpc? _rpc;
    private Task? _prefetch;

    [ImportingConstructor]
    public BuiltinDocumentManager(JoinableTaskContext joinableTaskContext)
    {
        _joinableTaskContext = joinableTaskContext;
    }

    public void Attach(JsonRpc rpc)
    {
        lock (_gate)
        {
            _rpc = rpc;
        }
    }

    public Task PrefetchAllAsync(CancellationToken cancellationToken)
    {
        lock (_gate)
        {
            _prefetch ??= PrefetchCoreAsync(cancellationToken);
            return _prefetch;
        }
    }

    public async Task<string> OpenOrGetDocumentAsync(string builtinUri, CancellationToken cancellationToken)
    {
        if (!IsBuiltinUri(builtinUri))
        {
            throw new ArgumentException("URI is not an AuroraScript built-in document URI.", nameof(builtinUri));
        }

        var existing = TryGetCachedPath(builtinUri);
        if (existing != null)
        {
            return existing;
        }

        var prefetch = Volatile.Read(ref _prefetch);
        if (prefetch != null)
        {
            try
            {
                await prefetch.ConfigureAwait(false);
            }
            catch (Exception)
            {
            }

            existing = TryGetCachedPath(builtinUri);
            if (existing != null)
            {
                return existing;
            }
        }

        var text = await RequestBuiltinDocumentTextAsync(builtinUri, cancellationToken).ConfigureAwait(false);
        return CacheDocument(builtinUri, text);
    }

    public async Task OpenDocumentAsync(string builtinUri, CancellationToken cancellationToken)
    {
        var path = await OpenOrGetDocumentAsync(builtinUri, cancellationToken).ConfigureAwait(false);
        await _joinableTaskContext.Factory.SwitchToMainThreadAsync(cancellationToken);
        VsShellUtilities.OpenDocument(ServiceProvider.GlobalProvider, path);
    }

    public bool TryGetBuiltinUriForFilePath(string filePath, out string builtinUri)
    {
        lock (_gate)
        {
            if (_builtinUrisByFilePath.TryGetValue(filePath, out builtinUri!))
            {
                return true;
            }
        }

        builtinUri = string.Empty;
        return false;
    }

    public static bool IsBuiltinUri(string uri)
    {
        return uri.StartsWith(BuiltinScheme + ":", StringComparison.Ordinal);
    }

    private async Task PrefetchCoreAsync(CancellationToken cancellationToken)
    {
        JsonRpc? rpc;
        lock (_gate)
        {
            rpc = _rpc;
        }

        if (rpc == null)
        {
            return;
        }

        var documents = await rpc.InvokeWithParameterObjectAsync<JArray>(
            "aurora/builtinDocuments",
            new JObject(),
            cancellationToken).ConfigureAwait(false);
        if (documents == null)
        {
            return;
        }

        await Task.Run(() =>
        {
            foreach (var item in documents)
            {
                if (item is not JObject document ||
                    document["uri"]?.Value<string>() is not { } uri ||
                    document["text"]?.Value<string>() is not { } text ||
                    !IsBuiltinUri(uri))
                {
                    continue;
                }

                CacheDocument(uri, text);
            }
        }, cancellationToken).ConfigureAwait(false);
    }

    private string? TryGetCachedPath(string builtinUri)
    {
        lock (_gate)
        {
            if (_filePathsByBuiltinUri.TryGetValue(builtinUri, out var existingPath) && File.Exists(existingPath))
            {
                return existingPath;
            }
        }

        return null;
    }

    private string CacheDocument(string builtinUri, string text)
    {
        var path = BuiltinUriToCachePath(builtinUri);
        var directory = Path.GetDirectoryName(path)!;
        Directory.CreateDirectory(directory);
        var tempPath = path + ".tmp";
        File.WriteAllText(tempPath, text, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
        if (File.Exists(path))
        {
            File.Delete(path);
        }

        File.Move(tempPath, path);

        lock (_gate)
        {
            _filePathsByBuiltinUri[builtinUri] = path;
            _builtinUrisByFilePath[path] = builtinUri;
        }

        return path;
    }

    private async Task<string> RequestBuiltinDocumentTextAsync(string builtinUri, CancellationToken cancellationToken)
    {
        JsonRpc? rpc;
        lock (_gate)
        {
            rpc = _rpc;
        }

        if (rpc == null)
        {
            throw new InvalidOperationException("AuroraScript language server RPC connection is not initialized.");
        }

        var result = await rpc.InvokeWithParameterObjectAsync<JObject>(
            "aurora/builtinDocument",
            new JObject { ["uri"] = builtinUri },
            cancellationToken).ConfigureAwait(false);

        return result["text"]?.Value<string>() ?? string.Empty;
    }

    private static string BuiltinUriToCachePath(string builtinUri)
    {
        var parsed = new Uri(builtinUri);
        var name = parsed.AbsolutePath.Trim('/').Replace('/', Path.DirectorySeparatorChar);
        if (string.IsNullOrWhiteSpace(name))
        {
            name = "builtin.as";
        }

        return Path.Combine(
            Path.GetTempPath(),
            "AuroraScript.VisualStudio",
            "Builtins",
            name);
    }
}
