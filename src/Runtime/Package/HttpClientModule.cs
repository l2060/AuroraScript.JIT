using AuroraScript.Runtime.Types;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace AuroraScript.Runtime.Package
{
    internal static class HttpClientModule
    {
        private static readonly HttpClient Client = CreateClient();

        internal static void Configure(ScriptModule module)
        {
            module.Define("request", ScriptDatum.FromBonding(REQUEST), writeable: false, enumerable: false);
            module.Define("requestAsync", ScriptDatum.FromBonding(REQUEST_ASYNC), writeable: false, enumerable: false);
            module.Define("get", ScriptDatum.FromBonding(GET), writeable: false, enumerable: false);
            module.Define("getAsync", ScriptDatum.FromBonding(GET_ASYNC), writeable: false, enumerable: false);
            module.Define("post", ScriptDatum.FromBonding(POST), writeable: false, enumerable: false);
            module.Define("postAsync", ScriptDatum.FromBonding(POST_ASYNC), writeable: false, enumerable: false);
            module.Define("put", ScriptDatum.FromBonding(PUT), writeable: false, enumerable: false);
            module.Define("putAsync", ScriptDatum.FromBonding(PUT_ASYNC), writeable: false, enumerable: false);
            module.Define("patch", ScriptDatum.FromBonding(PATCH), writeable: false, enumerable: false);
            module.Define("patchAsync", ScriptDatum.FromBonding(PATCH_ASYNC), writeable: false, enumerable: false);
            module.Define("delete", ScriptDatum.FromBonding(DELETE), writeable: false, enumerable: false);
            module.Define("deleteAsync", ScriptDatum.FromBonding(DELETE_ASYNC), writeable: false, enumerable: false);
            module.Define("head", ScriptDatum.FromBonding(HEAD), writeable: false, enumerable: false);
            module.Define("headAsync", ScriptDatum.FromBonding(HEAD_ASYNC), writeable: false, enumerable: false);
        }

        public static void REQUEST(ScriptContext ctx, ScriptObject thisObject, Span<ScriptDatum> args, ref ScriptDatum result)
        {
            ExecuteSynchronously(ParseRequest(args, "request"), ref result);
        }

        public static void REQUEST_ASYNC(ScriptContext ctx, ScriptObject thisObject, Span<ScriptDatum> args, ref ScriptDatum result)
        {
            var callback = RequireCallback(args, "requestAsync");
            StartAsynchronous(ctx, ParseRequest(args[..^1], "requestAsync"), callback, ref result);
        }

        public static void GET(ScriptContext ctx, ScriptObject thisObject, Span<ScriptDatum> args, ref ScriptDatum result)
        {
            ExecuteSynchronously(ParseVerb(args, HttpMethod.Get, "get", acceptsBody: false), ref result);
        }

        public static void GET_ASYNC(ScriptContext ctx, ScriptObject thisObject, Span<ScriptDatum> args, ref ScriptDatum result)
        {
            var callback = RequireCallback(args, "getAsync");
            StartAsynchronous(
                ctx,
                ParseVerb(args[..^1], HttpMethod.Get, "getAsync", acceptsBody: false),
                callback,
                ref result);
        }

        public static void POST(ScriptContext ctx, ScriptObject thisObject, Span<ScriptDatum> args, ref ScriptDatum result)
        {
            ExecuteSynchronously(ParseVerb(args, HttpMethod.Post, "post", acceptsBody: true), ref result);
        }

        public static void POST_ASYNC(ScriptContext ctx, ScriptObject thisObject, Span<ScriptDatum> args, ref ScriptDatum result)
        {
            var callback = RequireCallback(args, "postAsync");
            StartAsynchronous(
                ctx,
                ParseVerb(args[..^1], HttpMethod.Post, "postAsync", acceptsBody: true),
                callback,
                ref result);
        }

        public static void PUT(ScriptContext ctx, ScriptObject thisObject, Span<ScriptDatum> args, ref ScriptDatum result)
        {
            ExecuteSynchronously(ParseVerb(args, HttpMethod.Put, "put", acceptsBody: true), ref result);
        }

        public static void PUT_ASYNC(ScriptContext ctx, ScriptObject thisObject, Span<ScriptDatum> args, ref ScriptDatum result)
        {
            var callback = RequireCallback(args, "putAsync");
            StartAsynchronous(
                ctx,
                ParseVerb(args[..^1], HttpMethod.Put, "putAsync", acceptsBody: true),
                callback,
                ref result);
        }

        public static void PATCH(ScriptContext ctx, ScriptObject thisObject, Span<ScriptDatum> args, ref ScriptDatum result)
        {
            ExecuteSynchronously(ParseVerb(args, HttpMethod.Patch, "patch", acceptsBody: true), ref result);
        }

        public static void PATCH_ASYNC(ScriptContext ctx, ScriptObject thisObject, Span<ScriptDatum> args, ref ScriptDatum result)
        {
            var callback = RequireCallback(args, "patchAsync");
            StartAsynchronous(
                ctx,
                ParseVerb(args[..^1], HttpMethod.Patch, "patchAsync", acceptsBody: true),
                callback,
                ref result);
        }

        public static void DELETE(ScriptContext ctx, ScriptObject thisObject, Span<ScriptDatum> args, ref ScriptDatum result)
        {
            ExecuteSynchronously(ParseVerb(args, HttpMethod.Delete, "delete", acceptsBody: false), ref result);
        }

        public static void DELETE_ASYNC(ScriptContext ctx, ScriptObject thisObject, Span<ScriptDatum> args, ref ScriptDatum result)
        {
            var callback = RequireCallback(args, "deleteAsync");
            StartAsynchronous(
                ctx,
                ParseVerb(args[..^1], HttpMethod.Delete, "deleteAsync", acceptsBody: false),
                callback,
                ref result);
        }

        public static void HEAD(ScriptContext ctx, ScriptObject thisObject, Span<ScriptDatum> args, ref ScriptDatum result)
        {
            ExecuteSynchronously(ParseVerb(args, HttpMethod.Head, "head", acceptsBody: false), ref result);
        }

        public static void HEAD_ASYNC(ScriptContext ctx, ScriptObject thisObject, Span<ScriptDatum> args, ref ScriptDatum result)
        {
            var callback = RequireCallback(args, "headAsync");
            StartAsynchronous(
                ctx,
                ParseVerb(args[..^1], HttpMethod.Head, "headAsync", acceptsBody: false),
                callback,
                ref result);
        }

        private static HttpRequestSpec ParseRequest(Span<ScriptDatum> args, string apiName)
        {
            if (args.Length is < 2 or > 3)
            {
                throw new AuroraRuntimeException(
                    $"http.{apiName} requires method, url, and an optional options object.");
            }

            var methodText = RequireString(args[0], apiName, "method", nonEmpty: true);
            HttpMethod method;
            try
            {
                method = new HttpMethod(methodText.Trim().ToUpperInvariant());
            }
            catch (Exception exception) when (exception is ArgumentException or FormatException)
            {
                throw new AuroraRuntimeException(
                    $"http.{apiName} received an invalid HTTP method: {exception.Message}");
            }

            var options = args.Length == 3 ? RequireOptions(args[2], apiName) : null;
            return CreateSpec(method, args[1], options, default, hasExplicitBody: false, apiName);
        }

        private static HttpRequestSpec ParseVerb(
            Span<ScriptDatum> args,
            HttpMethod method,
            string apiName,
            bool acceptsBody)
        {
            if (args.Length == 0)
            {
                throw new AuroraRuntimeException($"http.{apiName} requires a url string.");
            }

            if (!acceptsBody)
            {
                if (args.Length > 2)
                {
                    throw new AuroraRuntimeException(
                        $"http.{apiName} accepts a url and an optional options object.");
                }

                var options = args.Length == 2 ? RequireOptions(args[1], apiName) : null;
                return CreateSpec(method, args[0], options, default, hasExplicitBody: false, apiName);
            }

            if (args.Length > 3)
            {
                throw new AuroraRuntimeException(
                    $"http.{apiName} accepts a url, optional body, and optional options object.");
            }

            ScriptObject requestOptions = null;
            ScriptDatum explicitBody = default;
            var hasExplicitBody = false;
            if (args.Length >= 2)
            {
                if (args.Length == 2 && IsOptions(args[1]))
                {
                    requestOptions = RequireOptions(args[1], apiName);
                }
                else
                {
                    explicitBody = args[1];
                    hasExplicitBody = true;
                }
            }

            if (args.Length == 3)
            {
                requestOptions = RequireOptions(args[2], apiName);
            }

            return CreateSpec(method, args[0], requestOptions, explicitBody, hasExplicitBody, apiName);
        }

        private static HttpRequestSpec CreateSpec(
            HttpMethod method,
            ScriptDatum urlDatum,
            ScriptObject options,
            ScriptDatum explicitBody,
            bool hasExplicitBody,
            string apiName)
        {
            var url = RequireString(urlDatum, apiName, "url", nonEmpty: true);
            if (!Uri.TryCreate(url, UriKind.Absolute, out var uri) ||
                (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps))
            {
                throw new AuroraRuntimeException(
                    $"http.{apiName} requires an absolute http or https url.");
            }

            var headers = ReadHeaders(options, apiName);
            var timeoutMilliseconds = ReadTimeout(options, apiName);
            var configuredContentType = ReadOptionalString(options, "contentType", apiName);
            var contentType = configuredContentType;
            var body = hasExplicitBody
                ? ReadBody(explicitBody, apiName, ref contentType)
                : ReadOptionsBody(options, apiName, ref contentType);
            if (configuredContentType == null && ContainsHeader(headers, "Content-Type"))
            {
                contentType = null;
            }

            return new HttpRequestSpec(
                apiName,
                method,
                uri,
                headers,
                body,
                contentType,
                timeoutMilliseconds);
        }

        private static List<RequestHeader> ReadHeaders(ScriptObject options, string apiName)
        {
            var result = new List<RequestHeader>();
            if (options == null)
            {
                return result;
            }

            var datum = options.GetPropertyDatum(null, "headers");
            if (datum.Kind == ValueKind.Null)
            {
                return result;
            }

            if (datum.Kind != ValueKind.Object || datum.Object == null || datum.Object == ScriptObject.Null)
            {
                throw new AuroraRuntimeException(
                    $"http.{apiName} requires options.headers to be an object.");
            }

            var headers = datum.Object;
            var names = headers.EnumerationKeys();
            for (var i = 0; i < names.Count; i++)
            {
                var name = names[i];
                if (string.IsNullOrWhiteSpace(name))
                {
                    throw new AuroraRuntimeException(
                        $"http.{apiName} requires every header name to be non-empty.");
                }

                if (string.Equals(name, "Content-Length", StringComparison.OrdinalIgnoreCase))
                {
                    throw new AuroraRuntimeException(
                        $"http.{apiName} manages the Content-Length header automatically.");
                }

                var value = headers.GetPropertyDatum(null, name);
                result.Add(new RequestHeader(name, ReadHeaderValues(value, apiName, name)));
            }

            return result;
        }

        private static bool ContainsHeader(List<RequestHeader> headers, string name)
        {
            for (var i = 0; i < headers.Count; i++)
            {
                if (string.Equals(headers[i].Name, name, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            return false;
        }

        private static string[] ReadHeaderValues(ScriptDatum datum, string apiName, string headerName)
        {
            if (datum.Kind == ValueKind.String)
            {
                return new[] { datum.StringText };
            }

            if (datum.Kind == ValueKind.Array && datum.Object is ScriptArray array)
            {
                if (array.Length == 0)
                {
                    throw new AuroraRuntimeException(
                        $"http.{apiName} requires header '{headerName}' to contain at least one value.");
                }

                var values = new string[array.Length];
                for (var i = 0; i < array.Length; i++)
                {
                    var item = array.GetElement(i);
                    if (item.Kind != ValueKind.String)
                    {
                        throw new AuroraRuntimeException(
                            $"http.{apiName} requires header '{headerName}' values to be strings.");
                    }

                    values[i] = item.StringText;
                }

                return values;
            }

            throw new AuroraRuntimeException(
                $"http.{apiName} requires header '{headerName}' to be a string or string array.");
        }

        private static int? ReadTimeout(ScriptObject options, string apiName)
        {
            if (options == null)
            {
                return null;
            }

            var timeout = options.GetPropertyDatum(null, "timeout");
            if (timeout.Kind == ValueKind.Null)
            {
                return null;
            }

            if (timeout.Kind != ValueKind.Number ||
                !double.IsFinite(timeout.Number) ||
                timeout.Number != Math.Truncate(timeout.Number) ||
                timeout.Number <= 0 ||
                timeout.Number > int.MaxValue)
            {
                throw new AuroraRuntimeException(
                    $"http.{apiName} requires options.timeout to be a positive integer number of milliseconds.");
            }

            return (int)timeout.Number;
        }

        private static string ReadOptionalString(ScriptObject options, string name, string apiName)
        {
            if (options == null)
            {
                return null;
            }

            var value = options.GetPropertyDatum(null, name);
            if (value.Kind == ValueKind.Null)
            {
                return null;
            }

            if (value.Kind != ValueKind.String || string.IsNullOrWhiteSpace(value.StringText))
            {
                throw new AuroraRuntimeException(
                    $"http.{apiName} requires options.{name} to be a non-empty string.");
            }

            return value.StringText;
        }

        private static byte[] ReadOptionsBody(
            ScriptObject options,
            string apiName,
            ref string contentType)
        {
            return options == null
                ? null
                : ReadBody(options.GetPropertyDatum(null, "body"), apiName, ref contentType);
        }

        private static byte[] ReadBody(ScriptDatum body, string apiName, ref string contentType)
        {
            if (body.Kind == ValueKind.Null)
            {
                return null;
            }

            if (body.Kind == ValueKind.String)
            {
                contentType ??= "text/plain; charset=utf-8";
                return Encoding.UTF8.GetBytes(body.StringText);
            }

            if (body.Object is ScriptUInt8Array bytes)
            {
                contentType ??= "application/octet-stream";
                return (byte[])bytes._items.Clone();
            }

            throw new AuroraRuntimeException(
                $"http.{apiName} requires the request body to be a string, UInt8Array, or null.");
        }

        private static ScriptObject RequireOptions(ScriptDatum datum, string apiName)
        {
            if (datum.Kind == ValueKind.Null)
            {
                return null;
            }

            if (!IsOptions(datum))
            {
                throw new AuroraRuntimeException(
                    $"http.{apiName} requires options to be an object when provided.");
            }

            return datum.Object;
        }

        private static bool IsOptions(ScriptDatum datum)
        {
            return datum.Kind == ValueKind.Object &&
                datum.Object != null &&
                datum.Object != ScriptObject.Null &&
                datum.Object is not ScriptUInt8Array;
        }

        private static string RequireString(
            ScriptDatum datum,
            string apiName,
            string parameter,
            bool nonEmpty)
        {
            if (datum.Kind != ValueKind.String ||
                (nonEmpty && string.IsNullOrWhiteSpace(datum.StringText)))
            {
                throw new AuroraRuntimeException(
                    $"http.{apiName} requires '{parameter}' to be a non-empty string.");
            }

            return datum.StringText;
        }

        private static ClosureFunction RequireCallback(Span<ScriptDatum> args, string apiName)
        {
            if (args.Length == 0 || args[^1].Object is not ClosureFunction callback)
            {
                throw new AuroraRuntimeException(
                    $"http.{apiName} requires a callback function as its final argument.");
            }

            return callback;
        }

        private static void ExecuteSynchronously(HttpRequestSpec spec, ref ScriptDatum result)
        {
            try
            {
                var response = SendAsync(spec).GetAwaiter().GetResult();
                ScriptDatum.WriteAsObject(ref result, response);
            }
            catch (Exception exception) when (IsRequestException(exception))
            {
                throw CreateRequestError(spec, exception);
            }
        }

        private static void StartAsynchronous(
            ScriptContext context,
            HttpRequestSpec spec,
            ClosureFunction callback,
            ref ScriptDatum result)
        {
            if (context == null)
            {
                throw new AuroraRuntimeException(
                    $"http.{spec.ApiName} requires an active script context.");
            }

            _ = SendWithCallbackAsync(
                spec,
                callback,
                context.UserState ?? ScriptObject.Null,
                context.Engine?.Options.Runtime.ConsoleErrorOut);
            ScriptDatum.WriteAsBoolean(ref result, true);
        }

        private static async Task SendWithCallbackAsync(
            HttpRequestSpec spec,
            ClosureFunction callback,
            ScriptObject userState,
            TextWriter errorOutput)
        {
            await Task.Yield();

            ScriptDatum error = ScriptDatum.Null;
            ScriptDatum response = ScriptDatum.Null;
            try
            {
                response = ScriptDatum.FromObject(await SendAsync(spec).ConfigureAwait(false));
            }
            catch (Exception exception) when (IsRequestException(exception))
            {
                error = ScriptDatum.FromError(new ScriptError(
                    CreateRequestErrorMessage(spec, exception),
                    Array.Empty<AuroraStackTrace>()));
            }

            try
            {
                callback.InvokeClrDetached(userState, error, response);
            }
            catch (Exception exception)
            {
                WriteCallbackError(errorOutput, spec.ApiName, exception);
            }
        }

        private static async Task<ScriptObject> SendAsync(HttpRequestSpec spec)
        {
            using var request = CreateRequest(spec);
            using var timeout = CreateTimeout(spec.TimeoutMilliseconds);
            var cancellationToken = timeout?.Token ?? CancellationToken.None;
            using var response = await Client.SendAsync(
                request,
                HttpCompletionOption.ResponseHeadersRead,
                cancellationToken).ConfigureAwait(false);
            var bytes = await response.Content
                .ReadAsByteArrayAsync(cancellationToken)
                .ConfigureAwait(false);
            return CreateResponse(spec, response, bytes);
        }

        private static HttpRequestMessage CreateRequest(HttpRequestSpec spec)
        {
            var request = new HttpRequestMessage(spec.Method, spec.Uri);
            if (spec.Body != null)
            {
                request.Content = new ByteArrayContent(spec.Body);
            }

            for (var i = 0; i < spec.Headers.Count; i++)
            {
                var header = spec.Headers[i];
                if (request.Headers.TryAddWithoutValidation(header.Name, header.Values))
                {
                    continue;
                }

                request.Content ??= new ByteArrayContent(Array.Empty<byte>());
                if (!request.Content.Headers.TryAddWithoutValidation(header.Name, header.Values))
                {
                    throw new FormatException($"Invalid HTTP header name '{header.Name}'.");
                }
            }

            if (spec.ContentType != null)
            {
                request.Content ??= new ByteArrayContent(Array.Empty<byte>());
                request.Content.Headers.Remove("Content-Type");
                if (!request.Content.Headers.TryAddWithoutValidation("Content-Type", spec.ContentType))
                {
                    throw new FormatException("Invalid Content-Type header value.");
                }
            }

            return request;
        }

        private static ScriptObject CreateResponse(
            HttpRequestSpec spec,
            HttpResponseMessage response,
            byte[] bytes)
        {
            var headers = CreateHeaders(response);
            var text = DecodeBody(bytes, response.Content.Headers);
            var result = new ScriptObject();
            result.Define("status", ScriptDatum.FromNumber((int)response.StatusCode), writeable: false, enumerable: true);
            result.Define("statusText", ScriptDatum.FromString(response.ReasonPhrase ?? string.Empty), writeable: false, enumerable: true);
            result.Define("ok", ScriptDatum.FromBoolean(response.IsSuccessStatusCode), writeable: false, enumerable: true);
            result.Define(
                "url",
                ScriptDatum.FromString(response.RequestMessage?.RequestUri?.ToString() ?? spec.Uri.ToString()),
                writeable: false,
                enumerable: true);
            result.Define("headers", ScriptDatum.FromObject(headers), writeable: false, enumerable: true);
            result.Define("body", ScriptDatum.FromString(text), writeable: false, enumerable: true);
            result.Define("text", ScriptDatum.FromString(text), writeable: false, enumerable: true);
            result.Define(
                "bytes",
                ScriptDatum.FromObject(new ScriptUInt8Array(bytes)),
                writeable: false,
                enumerable: true);
            result.Frozen();
            return result;
        }

        private static ScriptObject CreateHeaders(HttpResponseMessage response)
        {
            var values = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);
            AddHeaders(values, response.Headers);
            AddHeaders(values, response.Content.Headers);

            var names = new List<string>(values.Keys);
            names.Sort(StringComparer.Ordinal);
            var result = new ScriptObject();
            for (var i = 0; i < names.Count; i++)
            {
                var name = names[i];
                result.Define(
                    name.ToLowerInvariant(),
                    ScriptDatum.FromString(string.Join(", ", values[name])),
                    writeable: false,
                    enumerable: true);
            }

            result.Frozen();
            return result;
        }

        private static void AddHeaders(
            Dictionary<string, List<string>> destination,
            HttpHeaders headers)
        {
            foreach (var header in headers)
            {
                if (!destination.TryGetValue(header.Key, out var values))
                {
                    values = new List<string>();
                    destination.Add(header.Key, values);
                }

                values.AddRange(header.Value);
            }
        }

        private static string DecodeBody(byte[] bytes, HttpContentHeaders headers)
        {
            if (bytes.Length == 0)
            {
                return string.Empty;
            }

            var encoding = Encoding.UTF8;
            var charset = headers.ContentType?.CharSet;
            if (!string.IsNullOrWhiteSpace(charset))
            {
                charset = charset.Trim().Trim('"');
                try
                {
                    encoding = Encoding.GetEncoding(charset);
                }
                catch (Exception exception) when (exception is ArgumentException or NotSupportedException)
                {
                }
            }

            using var stream = new MemoryStream(bytes, writable: false);
            using var reader = new StreamReader(
                stream,
                encoding,
                detectEncodingFromByteOrderMarks: true,
                bufferSize: 1024,
                leaveOpen: false);
            return reader.ReadToEnd();
        }

        private static CancellationTokenSource CreateTimeout(int? timeoutMilliseconds)
        {
            if (!timeoutMilliseconds.HasValue)
            {
                return null;
            }

            var source = new CancellationTokenSource();
            source.CancelAfter(timeoutMilliseconds.Value);
            return source;
        }

        private static HttpClient CreateClient()
        {
            var handler = new HttpClientHandler
            {
                AutomaticDecompression = DecompressionMethods.All,
                UseCookies = false
            };
            return new HttpClient(handler, disposeHandler: true);
        }

        private static bool IsRequestException(Exception exception)
        {
            return exception is HttpRequestException or
                OperationCanceledException or
                IOException or
                InvalidOperationException or
                ArgumentException or
                FormatException or
                NotSupportedException;
        }

        private static AuroraRuntimeException CreateRequestError(
            HttpRequestSpec spec,
            Exception exception)
        {
            return new AuroraRuntimeException(CreateRequestErrorMessage(spec, exception));
        }

        private static string CreateRequestErrorMessage(
            HttpRequestSpec spec,
            Exception exception)
        {
            var detail = exception is OperationCanceledException && spec.TimeoutMilliseconds.HasValue
                ? $"The request timed out after {spec.TimeoutMilliseconds.Value} milliseconds."
                : exception.Message;
            return $"http.{spec.ApiName} failed for '{spec.Uri}': {detail}";
        }

        private static void WriteCallbackError(
            TextWriter errorOutput,
            string apiName,
            Exception exception)
        {
            try
            {
                errorOutput?.WriteLine($"http.{apiName} callback failed: {exception}");
            }
            catch
            {
            }
        }

        private sealed class HttpRequestSpec
        {
            public HttpRequestSpec(
                string apiName,
                HttpMethod method,
                Uri uri,
                List<RequestHeader> headers,
                byte[] body,
                string contentType,
                int? timeoutMilliseconds)
            {
                ApiName = apiName;
                Method = method;
                Uri = uri;
                Headers = headers;
                Body = body;
                ContentType = contentType;
                TimeoutMilliseconds = timeoutMilliseconds;
            }

            public string ApiName { get; }

            public HttpMethod Method { get; }

            public Uri Uri { get; }

            public List<RequestHeader> Headers { get; }

            public byte[] Body { get; }

            public string ContentType { get; }

            public int? TimeoutMilliseconds { get; }
        }

        private readonly struct RequestHeader
        {
            public RequestHeader(string name, string[] values)
            {
                Name = name;
                Values = values;
            }

            public string Name { get; }

            public string[] Values { get; }
        }
    }
}
