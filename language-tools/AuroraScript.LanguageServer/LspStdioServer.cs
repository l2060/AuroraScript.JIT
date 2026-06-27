using AuroraScript.LanguageServer.Protocol;
using System;
using System.Buffers;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading;
using System.Threading.Tasks;

namespace AuroraScript.LanguageServer;

internal sealed class LspStdioServer
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = null
    };

    private readonly AuroraLanguageServer _server;
    private readonly Stream _input;
    private readonly Stream _output;

    public LspStdioServer(AuroraLanguageServer server, Stream input, Stream output)
    {
        _server = server;
        _input = input;
        _output = output;
    }

    public async Task RunAsync(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            var message = await ReadMessageAsync(cancellationToken).ConfigureAwait(false);
            if (message == null)
            {
                return;
            }

            var result = await _server.HandleAsync(message, cancellationToken).ConfigureAwait(false);
            for (var i = 0; i < result.Notifications.Count; i++)
            {
                await WriteNotificationAsync(result.Notifications[i], cancellationToken).ConfigureAwait(false);
            }

            if (result.Response != null)
            {
                await WriteResponseAsync(result.Response, cancellationToken).ConfigureAwait(false);
            }

            if (result.Shutdown)
            {
                return;
            }
        }
    }

    private async Task<JsonObject?> ReadMessageAsync(CancellationToken cancellationToken)
    {
        var header = await ReadHeaderAsync(cancellationToken).ConfigureAwait(false);
        if (header < 0)
        {
            return null;
        }

        var buffer = ArrayPool<byte>.Shared.Rent(header);
        try
        {
            var read = 0;
            while (read < header)
            {
                var count = await _input.ReadAsync(buffer.AsMemory(read, header - read), cancellationToken).ConfigureAwait(false);
                if (count == 0)
                {
                    return null;
                }
                read += count;
            }

            var json = Encoding.UTF8.GetString(buffer, 0, header);
            return JsonNode.Parse(json) as JsonObject;
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(buffer);
        }
    }

    private async Task<int> ReadHeaderAsync(CancellationToken cancellationToken)
    {
        var contentLength = -1;
        while (true)
        {
            var line = await ReadAsciiLineAsync(cancellationToken).ConfigureAwait(false);
            if (line == null)
            {
                return -1;
            }

            if (line.Length == 0)
            {
                return contentLength;
            }

            const string prefix = "Content-Length:";
            if (line.StartsWith(prefix, StringComparison.OrdinalIgnoreCase) &&
                int.TryParse(line.Substring(prefix.Length).Trim(), out var length))
            {
                contentLength = length;
            }
        }
    }

    private async Task<string?> ReadAsciiLineAsync(CancellationToken cancellationToken)
    {
        var bytes = new MemoryStream(64);
        while (true)
        {
            var buffer = new byte[1];
            var read = await _input.ReadAsync(buffer.AsMemory(0, 1), cancellationToken).ConfigureAwait(false);
            if (read == 0)
            {
                return bytes.Length == 0 ? null : Encoding.ASCII.GetString(bytes.ToArray());
            }

            if (buffer[0] == (byte)'\n')
            {
                var lineBytes = bytes.ToArray();
                var length = lineBytes.Length;
                if (length > 0 && lineBytes[length - 1] == (byte)'\r')
                {
                    length--;
                }
                return Encoding.ASCII.GetString(lineBytes, 0, length);
            }

            bytes.WriteByte(buffer[0]);
        }
    }

    private Task WriteResponseAsync(LspResponse response, CancellationToken cancellationToken)
    {
        var json = new JsonObject
        {
            ["jsonrpc"] = "2.0",
            ["id"] = response.Id?.DeepClone(),
            ["result"] = response.Result?.DeepClone()
        };
        return WriteJsonAsync(json, cancellationToken);
    }

    private Task WriteNotificationAsync(LspNotification notification, CancellationToken cancellationToken)
    {
        var json = new JsonObject
        {
            ["jsonrpc"] = "2.0",
            ["method"] = notification.Method,
            ["params"] = notification.Parameters.DeepClone()
        };
        return WriteJsonAsync(json, cancellationToken);
    }

    private async Task WriteJsonAsync(JsonObject json, CancellationToken cancellationToken)
    {
        var payload = JsonSerializer.SerializeToUtf8Bytes(json, JsonOptions);
        var header = Encoding.ASCII.GetBytes("Content-Length: " + payload.Length + "\r\n\r\n");
        await _output.WriteAsync(header, cancellationToken).ConfigureAwait(false);
        await _output.WriteAsync(payload, cancellationToken).ConfigureAwait(false);
        await _output.FlushAsync(cancellationToken).ConfigureAwait(false);
    }
}
