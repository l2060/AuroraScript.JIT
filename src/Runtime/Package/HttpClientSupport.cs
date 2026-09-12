using AuroraScript.Hosting;
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
#pragma warning disable CS1591 // 缺少对公共可见类型或成员的 XML 注释
namespace AuroraScript.Runtime.Package
{
    [NativeType("http")]
    [NativePackage("http")]
    public sealed partial class HttpClientSupport : ScriptObject
    {
        private static readonly HttpClient Client = CreateClient();

        [Export("request", DynamicAdapter = nameof(REQUEST))]
        public static HttpResponseValue RequestCore(string method, string url)
            => ExecuteObject(CreateSimpleRequest(method, url, "request"));

        [AuroraCallbackArgument(0, 1, typeof(HttpResponseValue))]
        [Export("requestAsync", DynamicAdapter = nameof(REQUEST_ASYNC))]
        public static bool RequestAsyncCore(ScriptContext ctx, string method, string url, ScriptObject callback)
            => StartAsync(ctx, CreateSimpleRequest(method, url, "requestAsync"), callback, "requestAsync");

        [Export("get", DynamicAdapter = nameof(GET))]
        public static HttpResponseValue GetCore(string url)
            => ExecuteObject(CreateSimpleSpec(HttpMethod.Get, url, "get"));

        [AuroraCallbackArgument(0, 1, typeof(HttpResponseValue))]
        [Export("getAsync", DynamicAdapter = nameof(GET_ASYNC))]
        public static bool GetAsyncCore(ScriptContext ctx, string url, ScriptObject callback)
            => StartAsync(ctx, CreateSimpleSpec(HttpMethod.Get, url, "getAsync"), callback, "getAsync");

        [Export("post", DynamicAdapter = nameof(POST))]
        public static HttpResponseValue PostCore(string url)
            => ExecuteObject(CreateSimpleSpec(HttpMethod.Post, url, "post"));

        [Export("post", DynamicAdapter = nameof(POST))]
        public static HttpResponseValue PostCore(string url, string body)
            => ExecuteObject(CreateSimpleSpec(HttpMethod.Post, url, "post", EncodeTextBody(body),
                "text/plain; charset=utf-8"));

        [Export("post", DynamicAdapter = nameof(POST))]
        public static HttpResponseValue PostCore(string url, ScriptUInt8Array body)
            => ExecuteObject(CreateSimpleSpec(HttpMethod.Post, url, "post", CopyBytes(body),
                "application/octet-stream"));

        [AuroraCallbackArgument(0, 1, typeof(HttpResponseValue))]
        [Export("postAsync", DynamicAdapter = nameof(POST_ASYNC))]
        public static bool PostAsyncCore(ScriptContext ctx, string url, ScriptObject callback)
            => StartAsync(ctx, CreateSimpleSpec(HttpMethod.Post, url, "postAsync"), callback, "postAsync");

        [Export("put", DynamicAdapter = nameof(PUT))]
        public static HttpResponseValue PutCore(string url)
            => ExecuteObject(CreateSimpleSpec(HttpMethod.Put, url, "put"));

        [Export("put", DynamicAdapter = nameof(PUT))]
        public static HttpResponseValue PutCore(string url, string body)
            => ExecuteObject(CreateSimpleSpec(HttpMethod.Put, url, "put", EncodeTextBody(body),
                "text/plain; charset=utf-8"));

        [AuroraCallbackArgument(0, 1, typeof(HttpResponseValue))]
        [Export("putAsync", DynamicAdapter = nameof(PUT_ASYNC))]
        public static bool PutAsyncCore(ScriptContext ctx, string url, ScriptObject callback)
            => StartAsync(ctx, CreateSimpleSpec(HttpMethod.Put, url, "putAsync"), callback, "putAsync");

        [Export("patch", DynamicAdapter = nameof(PATCH))]
        public static HttpResponseValue PatchCore(string url)
            => ExecuteObject(CreateSimpleSpec(HttpMethod.Patch, url, "patch"));

        [Export("patch", DynamicAdapter = nameof(PATCH))]
        public static HttpResponseValue PatchCore(string url, string body)
            => ExecuteObject(CreateSimpleSpec(HttpMethod.Patch, url, "patch", EncodeTextBody(body),
                "text/plain; charset=utf-8"));

        [AuroraCallbackArgument(0, 1, typeof(HttpResponseValue))]
        [Export("patchAsync", DynamicAdapter = nameof(PATCH_ASYNC))]
        public static bool PatchAsyncCore(ScriptContext ctx, string url, ScriptObject callback)
            => StartAsync(ctx, CreateSimpleSpec(HttpMethod.Patch, url, "patchAsync"), callback, "patchAsync");

        [Export("delete", DynamicAdapter = nameof(DELETE))]
        public static HttpResponseValue DeleteCore(string url)
            => ExecuteObject(CreateSimpleSpec(HttpMethod.Delete, url, "delete"));

        [AuroraCallbackArgument(0, 1, typeof(HttpResponseValue))]
        [Export("deleteAsync", DynamicAdapter = nameof(DELETE_ASYNC))]
        public static bool DeleteAsyncCore(ScriptContext ctx, string url, ScriptObject callback)
            => StartAsync(ctx, CreateSimpleSpec(HttpMethod.Delete, url, "deleteAsync"), callback, "deleteAsync");

        [Export("head", DynamicAdapter = nameof(HEAD))]
        public static HttpResponseValue HeadCore(string url)
            => ExecuteObject(CreateSimpleSpec(HttpMethod.Head, url, "head"));

        [AuroraCallbackArgument(0, 1, typeof(HttpResponseValue))]
        [Export("headAsync", DynamicAdapter = nameof(HEAD_ASYNC))]
        public static bool HeadAsyncCore(ScriptContext ctx, string url, ScriptObject callback)
            => StartAsync(ctx, CreateSimpleSpec(HttpMethod.Head, url, "headAsync"), callback, "headAsync");

        private static HttpResponseValue ExecuteObject(HttpRequestSpec spec)
        {
            var result = default(ScriptDatum);
            ExecuteSynchronously(spec, ref result);
            return (HttpResponseValue)result.Object;
        }

        private static bool StartAsync(
            ScriptContext ctx,
            HttpRequestSpec spec,
            ScriptObject callback,
            string apiName)
        {
            if (callback is not ClosureFunction function)
            {
                throw new AuroraRuntimeException(
                    $"http.{apiName} requires a callback function as its final argument.");
            }

            var result = default(ScriptDatum);
            StartAsynchronous(ctx, spec, function, ref result);
            return result.Boolean;
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

        private static HttpRequestSpec CreateSimpleRequest(
            string methodText,
            string url,
            string apiName)
        {
            if (string.IsNullOrWhiteSpace(methodText))
            {
                throw new AuroraRuntimeException(
                    $"http.{apiName} requires 'method' to be a non-empty string.");
            }
            HttpMethod method;
            try
            {
                method = new HttpMethod(methodText.Trim().ToUpperInvariant());
            }
            catch (Exception exception) when (
                exception is ArgumentException or FormatException)
            {
                throw new AuroraRuntimeException(
                    $"http.{apiName} received an invalid HTTP method: {exception.Message}");
            }
            return CreateSimpleSpec(method, url, apiName);
        }

        private static HttpRequestSpec CreateSimpleSpec(
            HttpMethod method,
            string url,
            string apiName,
            byte[] body = null,
            string contentType = null)
        {
            if (string.IsNullOrWhiteSpace(url) ||
                !Uri.TryCreate(url, UriKind.Absolute, out var uri) ||
                (uri.Scheme != Uri.UriSchemeHttp &&
                    uri.Scheme != Uri.UriSchemeHttps))
            {
                throw new AuroraRuntimeException(
                    $"http.{apiName} requires an absolute http or https url.");
            }
            return new HttpRequestSpec(
                apiName,
                method,
                uri,
                Array.Empty<RequestHeader>(),
                body,
                contentType,
                responseHeader: false,
                timeoutMilliseconds: null);
        }

        private static byte[] EncodeTextBody(string body) =>
            Encoding.UTF8.GetBytes(body ?? string.Empty);

        private static byte[] CopyBytes(ScriptUInt8Array body) =>
            body == null
                ? throw new AuroraRuntimeException(
                    "http request body requires a UInt8Array.")
                : (byte[])body._items.Clone();

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
            var responseHeaders = ReadBoolean(options, "responseHeaders", false);

            return new HttpRequestSpec(
                apiName,
                method,
                uri,
                headers,
                body,
                contentType,
                responseHeaders,
                timeoutMilliseconds);
        }

        private static IReadOnlyList<RequestHeader> ReadHeaders(
            ScriptObject options,
            string apiName)
        {
            if (options == null)
            {
                return Array.Empty<RequestHeader>();
            }

            var datum = options.GetPropertyDatum(null, "headers");
            if (datum.Kind == ValueKind.Null)
            {
                return Array.Empty<RequestHeader>();
            }

            if (datum.Kind != ValueKind.Object || datum.Object == null || datum.Object == ScriptObject.Null)
            {
                throw new AuroraRuntimeException(
                    $"http.{apiName} requires options.headers to be an object.");
            }

            var headers = datum.Object;
            var names = headers.EnumerationKeys();
            var result = new List<RequestHeader>(names.Count);
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

        private static bool ContainsHeader(
            IReadOnlyList<RequestHeader> headers,
            string name)
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


        private static Boolean ReadBoolean(ScriptObject options, string nameKey, bool defValue = false)
        {
            if (options == null)
            {
                return defValue;
            }
            var timeout = options.GetPropertyDatum(null, nameKey);
            if (timeout.Kind == ValueKind.Null)
            {
                return defValue;
            }
            return ScriptDatum.IsTrue(timeout);
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

        private static async Task<HttpResponseValue> SendAsync(HttpRequestSpec spec)
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

        private static HttpResponseValue CreateResponse(HttpRequestSpec spec, HttpResponseMessage response, byte[] bytes)
        {
            var text = DecodeBody(bytes, response.Content.Headers);
            ScriptObject headers = null;
            if (spec.ResponseHeader)
            {
                headers = CreateHeaders(response);
            }
            return new HttpResponseValue(
                (int)response.StatusCode,
                response.ReasonPhrase,
                response.IsSuccessStatusCode,
                response.RequestMessage?.RequestUri?.ToString() ?? spec.Uri.ToString(),
                headers,
                text,
                new ScriptUInt8Array(bytes));
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
                IReadOnlyList<RequestHeader> headers,
                byte[] body,
                string contentType,
                bool responseHeader,
                int? timeoutMilliseconds)
            {
                ApiName = apiName;
                Method = method;
                Uri = uri;
                Headers = headers;
                Body = body;
                ContentType = contentType;
                ResponseHeader = responseHeader;
                TimeoutMilliseconds = timeoutMilliseconds;
            }

            public string ApiName { get; }

            public HttpMethod Method { get; }

            public Uri Uri { get; }

            public IReadOnlyList<RequestHeader> Headers { get; }

            public byte[] Body { get; }

            public string ContentType { get; }

            public int? TimeoutMilliseconds { get; }

            public bool ResponseHeader { get; }
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
