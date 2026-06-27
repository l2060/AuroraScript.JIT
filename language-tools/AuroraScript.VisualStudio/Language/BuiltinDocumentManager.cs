using Microsoft.VisualStudio.LanguageServer.Client;
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

    public async Task<string> OpenOrGetDocumentAsync(string builtinUri, CancellationToken cancellationToken)
    {
        if (!IsBuiltinUri(builtinUri))
        {
            throw new ArgumentException("URI is not an AuroraScript built-in document URI.", nameof(builtinUri));
        }

        lock (_gate)
        {
            if (_filePathsByBuiltinUri.TryGetValue(builtinUri, out var existingPath) && File.Exists(existingPath))
            {
                return existingPath;
            }
        }

        var text = await RequestBuiltinDocumentTextAsync(builtinUri, cancellationToken).ConfigureAwait(false);
        var path = BuiltinUriToCachePath(builtinUri);
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllText(path, text, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));

        lock (_gate)
        {
            _filePathsByBuiltinUri[builtinUri] = path;
            _builtinUrisByFilePath[path] = builtinUri;
        }

        return path;
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
