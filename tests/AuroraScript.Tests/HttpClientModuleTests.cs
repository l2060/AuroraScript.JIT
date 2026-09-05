using AuroraScript.Core;
using AuroraScript.Runtime;
using AuroraScript.Runtime.Types;
using AuroraScript.Tests.Infrastructure;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace AuroraScript.Tests;

public sealed class HttpClientModuleTests
{
    [Fact]
    public void HttpClientModuleIsOptInAndExposesSyncAndCallbackApis()
    {
        using var workspace = new TestWorkspace();
        var defaultEngine = new AuroraEngine(CreateOptions(workspace.Root, enableHttpClient: false));
        var enabledEngine = new AuroraEngine(CreateOptions(workspace.Root, enableHttpClient: true));
        using var defaultDomain = defaultEngine.CreateEmptyDomain(null);
        using var enabledDomain = enabledEngine.CreateEmptyDomain(null);

        Assert.Same(ScriptObject.Null, defaultDomain.GetModule("http"));
        var module = enabledDomain.GetModule("http");
        Assert.NotSame(ScriptObject.Null, module);

        string[] methods =
        {
            "request", "requestAsync",
            "get", "getAsync",
            "post", "postAsync",
            "put", "putAsync",
            "patch", "patchAsync",
            "delete", "deleteAsync",
            "head", "headAsync"
        };
        for (var i = 0; i < methods.Length; i++)
        {
            Assert.NotSame(ScriptObject.Null, module.GetPropertyValue(methods[i]));
        }
    }

    [Fact]
    public async Task SynchronousApisSendTextAndByteRequestsAndReturnBufferedResponses()
    {
        await using var server = new LoopbackHttpServer(
            expectedRequests: 2,
            request => Task.FromResult(request.Method == "POST"
                ? new LoopbackResponse(
                    201,
                    "Created",
                    Encoding.UTF8.GetBytes("accepted"),
                    new Dictionary<string, string> { ["X-Reply"] = "sync" })
                : new LoopbackResponse(
                    404,
                    "Not Found",
                    Encoding.UTF8.GetBytes("missing"))));
        using var workspace = new TestWorkspace();
        workspace.WriteSource(
            "main.as",
            """
            @module(TEST);
            import http from 'http';

            export func run(baseUrl) {
                var headers = {};
                headers['X-Test'] = 'sync';
                var posted = http.post(baseUrl + 'post', 'payload', {
                    headers: headers,
                    responseHeaders: true,
                    contentType: 'text/custom; charset=utf-8',
                    timeout: 5000
                });

                var bytes = new UInt8Array(3);
                bytes[0] = 1;
                bytes[1] = 2;
                bytes[2] = 255;
                var stored = http.request('put', baseUrl + 'put', {
                    body: bytes,
                    contentType: 'application/octet-stream'
                });

                return [
                    posted.status,
                    posted.statusText,
                    posted.ok,
                    posted.body,
                    posted.text,
                    posted.bytes.length,
                    posted.headers['x-reply'],
                    posted.url,
                    stored.status,
                    stored.ok,
                    stored.body,
                    stored.headers
                ];
            }
            """);
        var engine = new AuroraEngine(CreateOptions(workspace.Root, enableHttpClient: true));

        await engine.BuildAsync("main.as");
        using var domain = engine.CreateDomain();
        var result = TestWorkspace.Execute(
            domain,
            "run",
            arguments: ScriptDatum.FromString(server.BaseAddress.ToString()));

        ScriptAssert.Equal(
            new object?[]
            {
                201,
                "Created",
                true,
                "accepted",
                "accepted",
                8,
                "sync",
                new Uri(server.BaseAddress, "post").ToString(),
                404,
                false,
                "missing",
                null
            },
            result);

        await server.Completion.WaitAsync(TimeSpan.FromSeconds(5));
        Assert.Equal(2, server.Requests.Count);
        Assert.Equal("POST", server.Requests[0].Method);
        Assert.Equal("payload", Encoding.UTF8.GetString(server.Requests[0].Body));
        Assert.Equal("sync", server.Requests[0].Headers["X-Test"]);
        Assert.Equal("text/custom; charset=utf-8", server.Requests[0].Headers["Content-Type"]);
        Assert.Equal("PUT", server.Requests[1].Method);
        Assert.Equal(new byte[] { 1, 2, 255 }, server.Requests[1].Body);
        Assert.Equal("application/octet-stream", server.Requests[1].Headers["Content-Type"]);
    }

    [Theory]
    [InlineData(CompilationMode.Dynamic)]
    [InlineData(CompilationMode.OnlyRun)]
#if NET9_0_OR_GREATER
    [InlineData(CompilationMode.Persistence)]
#endif
    public async Task CallbackApiRunsDetachedAndUsesErrorFirstResultConvention(CompilationMode mode)
    {
        await using var server = new LoopbackHttpServer(
            expectedRequests: 1,
            _ => Task.FromResult(new LoopbackResponse(
                200,
                "OK",
                Encoding.UTF8.GetBytes("async response"),
                new Dictionary<string, string> { ["X-Reply"] = "async" })));
        using var workspace = new TestWorkspace();
        workspace.WriteSource(
            "main.as",
            """
            @module(TEST);
            import http from 'http';

            export func begin(url) {
                var headers = {};
                headers['X-Test'] = 'callback';
                return http.getAsync(url, { headers: headers, responseHeaders: true, timeout: 5000 }, (error, response) => {
                    if (error != null) {
                        HOST_COMPLETE('error:' + error.message);
                        return;
                    }
                    HOST_COMPLETE(
                        response.status + '|' + response.body + '|' + response.headers['x-reply']);
                });
            }
            """);
        var engine = new AuroraEngine(CreateOptions(workspace.Root, enableHttpClient: true, mode));
        await engine.BuildAsync("main.as");
        var completion = new TaskCompletionSource<string>(TaskCreationOptions.RunContinuationsAsynchronously);
        using var domain = engine.CreateDomain(global => global.Define(
            "HOST_COMPLETE",
            (Action<string>)(value => completion.TrySetResult(value)),
            writeable: false,
            enumerable: false));

        ScriptAssert.Equal(
            true,
            TestWorkspace.Execute(
                domain,
                "begin",
                arguments: ScriptDatum.FromString(new Uri(server.BaseAddress, "async").ToString())));

        Assert.Equal(
            "200|async response|async",
            await completion.Task.WaitAsync(TimeSpan.FromSeconds(5)));
        await server.Completion.WaitAsync(TimeSpan.FromSeconds(5));
        Assert.Equal("callback", server.Requests[0].Headers["X-Test"]);
    }

    [Fact]
    public async Task CallbackApiReportsTransportTimeoutAsScriptError()
    {
        await using var server = new LoopbackHttpServer(
            expectedRequests: 1,
            async _ =>
            {
                await Task.Delay(300);
                return new LoopbackResponse(200, "OK", Encoding.UTF8.GetBytes("late"));
            });
        using var workspace = new TestWorkspace();
        workspace.WriteSource(
            "main.as",
            """
            @module(TEST);
            import http from 'http';

            export func begin(url) {
                return http.getAsync(url, { timeout: 50 }, (error, response) => {
                    if (error != null) {
                        HOST_COMPLETE(error.message);
                        return;
                    }
                    HOST_COMPLETE('unexpected success');
                });
            }
            """);
        var engine = new AuroraEngine(CreateOptions(workspace.Root, enableHttpClient: true));
        await engine.BuildAsync("main.as");
        var completion = new TaskCompletionSource<string>(TaskCreationOptions.RunContinuationsAsynchronously);
        using var domain = engine.CreateDomain(global => global.Define(
            "HOST_COMPLETE",
            (Action<string>)(value => completion.TrySetResult(value)),
            writeable: false,
            enumerable: false));

        ScriptAssert.Equal(
            true,
            TestWorkspace.Execute(
                domain,
                "begin",
                arguments: ScriptDatum.FromString(new Uri(server.BaseAddress, "slow").ToString())));

        var message = await completion.Task.WaitAsync(TimeSpan.FromSeconds(5));
        Assert.Contains("http.getAsync failed", message, StringComparison.Ordinal);
        Assert.Contains("timed out after 50 milliseconds", message, StringComparison.Ordinal);
        await server.Completion.WaitAsync(TimeSpan.FromSeconds(5));
    }

    [Fact]
    public async Task HttpClientModuleValidatesUrlOptionsBodyAndCallback()
    {
        using var workspace = new TestWorkspace();
        workspace.WriteSource(
            "main.as",
            """
            @module(TEST);
            import http from 'http';

            export func invalidUrl() { return http.get('file:///tmp/value'); }
            export func invalidTimeout() { return http.get('http://127.0.0.1/', { timeout: 0 }); }
            export func invalidBody() { return http.post('http://127.0.0.1/', [], null); }
            export func invalidCallback() { return http.getAsync('http://127.0.0.1/', 42); }
            """);
        var engine = new AuroraEngine(CreateOptions(workspace.Root, enableHttpClient: true));
        await engine.BuildAsync("main.as");
        using var domain = engine.CreateDomain();

        var url = Assert.Throws<AuroraRuntimeException>(() => TestWorkspace.Execute(domain, "invalidUrl"));
        Assert.Contains("absolute http or https url", url.Message, StringComparison.Ordinal);
        var timeout = Assert.Throws<AuroraRuntimeException>(() => TestWorkspace.Execute(domain, "invalidTimeout"));
        Assert.Contains("positive integer", timeout.Message, StringComparison.Ordinal);
        var body = Assert.Throws<AuroraRuntimeException>(() => TestWorkspace.Execute(domain, "invalidBody"));
        Assert.Contains("string, UInt8Array, or null", body.Message, StringComparison.Ordinal);
        var callback = Assert.Throws<AuroraRuntimeException>(() => TestWorkspace.Execute(domain, "invalidCallback"));
        Assert.Contains("callback function", callback.Message, StringComparison.Ordinal);
    }

    private static EngineOptions CreateOptions(
        string root,
        bool enableHttpClient,
        CompilationMode mode = CompilationMode.Dynamic)
    {
        var options = EngineOptions.Default
            .WithCompiler(compiler => compiler.SourceResolver = ScriptSources.FileSystem(root))
            .WithCompiler(compiler => compiler.Mode = mode)
            .WithRuntime(runtime => runtime.ConsoleStdOut = TextWriter.Null)
            .WithRuntime(runtime => runtime.ConsoleErrorOut = TextWriter.Null);

        return enableHttpClient
            ? options.WithBuiltIns(builtIns => builtIns.Add(BuiltInModules.HttpClient))
            : options;
    }

    private sealed class LoopbackHttpServer : IAsyncDisposable
    {
        private readonly TcpListener _listener;
        private readonly CancellationTokenSource _shutdown = new();
        private readonly int _expectedRequests;
        private readonly Func<LoopbackRequest, Task<LoopbackResponse>> _handler;

        public LoopbackHttpServer(
            int expectedRequests,
            Func<LoopbackRequest, Task<LoopbackResponse>> handler)
        {
            _expectedRequests = expectedRequests;
            _handler = handler;
            _listener = new TcpListener(IPAddress.Loopback, 0);
            _listener.Start();
            var endpoint = (IPEndPoint)_listener.LocalEndpoint;
            BaseAddress = new Uri($"http://127.0.0.1:{endpoint.Port}/");
            Completion = RunAsync();
        }

        public Uri BaseAddress { get; }

        public List<LoopbackRequest> Requests { get; } = new();

        public Task Completion { get; }

        public async ValueTask DisposeAsync()
        {
            _shutdown.Cancel();
            _listener.Stop();
            try
            {
                await Completion.ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
            }
            catch (ObjectDisposedException)
            {
            }
            finally
            {
                _shutdown.Dispose();
            }
        }

        private async Task RunAsync()
        {
            for (var i = 0; i < _expectedRequests; i++)
            {
                using var client = await _listener
                    .AcceptTcpClientAsync(_shutdown.Token)
                    .ConfigureAwait(false);
                using var stream = client.GetStream();
                var request = await ReadRequestAsync(stream, _shutdown.Token).ConfigureAwait(false);
                Requests.Add(request);
                var response = await _handler(request).ConfigureAwait(false);
                try
                {
                    await WriteResponseAsync(stream, response, _shutdown.Token).ConfigureAwait(false);
                }
                catch (IOException)
                {
                    // A timeout test intentionally closes the client before the delayed response is written.
                }
            }
        }

        private static async Task<LoopbackRequest> ReadRequestAsync(
            NetworkStream stream,
            CancellationToken cancellationToken)
        {
            using var headerBuffer = new MemoryStream();
            var current = new byte[1];
            var terminator = 0;
            while (terminator < 4)
            {
                var read = await stream.ReadAsync(current, cancellationToken).ConfigureAwait(false);
                if (read == 0)
                {
                    throw new EndOfStreamException("The HTTP request ended before its headers were complete.");
                }

                headerBuffer.WriteByte(current[0]);
                terminator = (terminator, current[0]) switch
                {
                    (0, (byte)'\r') => 1,
                    (1, (byte)'\n') => 2,
                    (2, (byte)'\r') => 3,
                    (3, (byte)'\n') => 4,
                    (_, (byte)'\r') => 1,
                    _ => 0
                };
                if (headerBuffer.Length > 64 * 1024)
                {
                    throw new InvalidDataException("The loopback HTTP request headers were too large.");
                }
            }

            var headerText = Encoding.ASCII.GetString(headerBuffer.ToArray());
            var lines = headerText.Split(new[] { "\r\n" }, StringSplitOptions.None);
            var requestLine = lines[0].Split(' ', StringSplitOptions.RemoveEmptyEntries);
            var headers = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            for (var i = 1; i < lines.Length; i++)
            {
                var separator = lines[i].IndexOf(':');
                if (separator <= 0)
                {
                    continue;
                }

                headers[lines[i][..separator].Trim()] = lines[i][(separator + 1)..].Trim();
            }

            var contentLength = headers.TryGetValue("Content-Length", out var lengthText)
                ? int.Parse(lengthText, System.Globalization.CultureInfo.InvariantCulture)
                : 0;
            var body = new byte[contentLength];
            var offset = 0;
            while (offset < body.Length)
            {
                var read = await stream
                    .ReadAsync(body.AsMemory(offset), cancellationToken)
                    .ConfigureAwait(false);
                if (read == 0)
                {
                    throw new EndOfStreamException("The HTTP request body ended early.");
                }

                offset += read;
            }

            return new LoopbackRequest(requestLine[0], requestLine[1], headers, body);
        }

        private static async Task WriteResponseAsync(
            NetworkStream stream,
            LoopbackResponse response,
            CancellationToken cancellationToken)
        {
            var headers = new StringBuilder()
                .Append("HTTP/1.1 ")
                .Append(response.StatusCode)
                .Append(' ')
                .Append(response.ReasonPhrase)
                .Append("\r\nContent-Type: text/plain; charset=utf-8")
                .Append("\r\nContent-Length: ")
                .Append(response.Body.Length)
                .Append("\r\nConnection: close\r\n");
            foreach (var header in response.Headers)
            {
                headers.Append(header.Key).Append(": ").Append(header.Value).Append("\r\n");
            }

            headers.Append("\r\n");
            var headerBytes = Encoding.ASCII.GetBytes(headers.ToString());
            await stream.WriteAsync(headerBytes, cancellationToken).ConfigureAwait(false);
            await stream.WriteAsync(response.Body, cancellationToken).ConfigureAwait(false);
        }
    }

    private sealed record LoopbackRequest(
        string Method,
        string Target,
        Dictionary<string, string> Headers,
        byte[] Body);

    private sealed class LoopbackResponse
    {
        public LoopbackResponse(
            int statusCode,
            string reasonPhrase,
            byte[] body,
            Dictionary<string, string>? headers = null)
        {
            StatusCode = statusCode;
            ReasonPhrase = reasonPhrase;
            Body = body;
            Headers = headers ?? new Dictionary<string, string>();
        }

        public int StatusCode { get; }

        public string ReasonPhrase { get; }

        public byte[] Body { get; }

        public Dictionary<string, string> Headers { get; }
    }
}
