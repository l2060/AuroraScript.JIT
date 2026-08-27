using Microsoft.VisualStudio.LanguageServer.Client;
using Microsoft.VisualStudio.Threading;
using Microsoft.VisualStudio.Utilities;
using StreamJsonRpc;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace AuroraScript.VisualStudio.Language;

[Export(typeof(ILanguageClient))]
[ContentType(AuroraContentTypeDefinition.ContentTypeName)]
internal sealed class AuroraLanguageServerClient :
    ILanguageClient,
    ILanguageClientCustomMessage2,
    ILanguageClientDocumentManager
{
    private const string ServerRelativePath = "Server\\AuroraScript.LanguageServer.exe";

    private readonly BuiltinDocumentManager _builtinDocuments;
    private readonly JoinableTaskContext _joinableTaskContext;

    [ImportingConstructor]
    public AuroraLanguageServerClient(
        BuiltinDocumentManager builtinDocuments,
        JoinableTaskContext joinableTaskContext)
    {
        _builtinDocuments = builtinDocuments;
        _joinableTaskContext = joinableTaskContext;
    }

    public string Name => "AuroraScript";

    public IEnumerable<string> ConfigurationSections => Array.Empty<string>();

    public object? InitializationOptions => null;

    public IEnumerable<string> FilesToWatch => Array.Empty<string>();

    public bool ShowNotificationOnInitializeFailed => true;

    public event AsyncEventHandler<EventArgs>? StartAsync;

#pragma warning disable CS0067
    public event AsyncEventHandler<EventArgs>? StopAsync;
#pragma warning restore CS0067

    public Task<Connection?> ActivateAsync(CancellationToken token)
    {
        token.ThrowIfCancellationRequested();

        var serverPath = GetLanguageServerPath();
        Log("Activating AuroraScript language server client.");
        Log("Language server path: " + serverPath);
        if (!File.Exists(serverPath))
        {
            Log("Language server executable was not found.");
            throw new FileNotFoundException("AuroraScript language server was not found in the VSIX payload.", serverPath);
        }

        var process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = serverPath,
                WorkingDirectory = Path.GetDirectoryName(serverPath) ?? string.Empty,
                RedirectStandardInput = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            },
            EnableRaisingEvents = true
        };

        process.Start();
        Log("Language server started. Process id: " + process.Id.ToString());
        _ = Task.Run(() => DrainServerErrorsAsync(process.StandardError, token), token);

        return Task.FromResult<Connection?>(new Connection(process.StandardOutput.BaseStream, process.StandardInput.BaseStream));
    }

    public Task OnLoadedAsync()
    {
        Log("AuroraScript language server client loaded.");
        return StartAsync != null
            ? StartAsync.InvokeAsync(this, EventArgs.Empty)
            : Task.CompletedTask;
    }

    public Task OnServerInitializedAsync()
    {
        Log("AuroraScript language server initialized.");
        return Task.CompletedTask;
    }

    public Task<InitializationFailureContext?> OnServerInitializeFailedAsync(ILanguageClientInitializationInfo initializationState)
    {
        Log("AuroraScript language server initialization failed: " + initializationState.StatusMessage);
        return Task.FromResult<InitializationFailureContext?>(new InitializationFailureContext
        {
            FailureMessage = initializationState.StatusMessage
        });
    }

    public object? MiddleLayer => new AuroraLanguageServerMiddleLayer(_builtinDocuments);

    public object? CustomMessageTarget => null;

    public Task AttachForCustomMessageAsync(JsonRpc rpc)
    {
        _builtinDocuments.Attach(rpc);
        _ = _builtinDocuments.PrefetchAllAsync(CancellationToken.None);
        return Task.CompletedTask;
    }

    public Task<string> EnsureFileExistsAsync(Uri documentUri)
    {
        if (!BuiltinDocumentManager.IsBuiltinUri(documentUri.ToString()))
        {
            return Task.FromResult<string>(null!);
        }

        return _builtinDocuments.OpenOrGetDocumentAsync(documentUri.ToString(), CancellationToken.None);
    }

    private static string GetLanguageServerPath()
    {
        return Path.Combine(Path.GetDirectoryName(typeof(AuroraLanguageServerClient).Assembly.Location) ?? string.Empty, ServerRelativePath);
    }

    private static async Task DrainServerErrorsAsync(StreamReader reader, CancellationToken cancellationToken)
    {
        try
        {
            while (!reader.EndOfStream && !cancellationToken.IsCancellationRequested)
            {
                var line = await reader.ReadLineAsync().ConfigureAwait(false);
                if (!string.IsNullOrEmpty(line))
                {
                    Log("server stderr: " + line);
                }
            }
        }
        catch (ObjectDisposedException)
        {
        }
    }

    private static void Log(string message)
    {
        try
        {
            var directory = Path.Combine(Path.GetTempPath(), "AuroraScript.VisualStudio");
            Directory.CreateDirectory(directory);
            var path = Path.Combine(directory, "AuroraScript.VisualStudio.log");
            File.AppendAllText(
                path,
                DateTimeOffset.Now.ToString("u") + " " + message + Environment.NewLine);
        }
        catch (IOException)
        {
        }
        catch (UnauthorizedAccessException)
        {
        }
    }
}
