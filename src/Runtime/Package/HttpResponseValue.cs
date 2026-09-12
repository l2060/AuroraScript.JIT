using AuroraScript.Hosting;
using AuroraScript.Runtime.Types;

namespace AuroraScript.Runtime.Package
{
    /// <summary>
    /// Buffered HTTP response with fixed native storage for hot-path member reads.
    /// </summary>
    [NativeType("HttpResponse")]
    public sealed partial class HttpResponseValue : ScriptObject
    {
        private readonly ScriptObject _headers;
        private readonly ScriptUInt8Array _bytes;

        internal HttpResponseValue(
            int status,
            string statusText,
            bool ok,
            string url,
            ScriptObject headers,
            string text,
            ScriptUInt8Array bytes)
        {
            Status = status;
            StatusText = statusText ?? string.Empty;
            Ok = ok;
            Url = url ?? string.Empty;
            _headers = headers;
            Body = text ?? string.Empty;
            Text = Body;
            _bytes = bytes;
            Frozen();
        }

        /// <summary>Numeric HTTP status code.</summary>
        [Export("status", Enumerable = true)]
        public readonly int Status;

        /// <summary>HTTP reason phrase.</summary>
        [Export("statusText", Enumerable = true)]
        public readonly string StatusText;

        /// <summary>True for a successful HTTP status code.</summary>
        [Export("ok", Enumerable = true)]
        public readonly bool Ok;

        /// <summary>Final response URL.</summary>
        [Export("url", Enumerable = true)]
        public readonly string Url;

        /// <summary>Gets lowercase response headers when requested.</summary>
        [Export("headers", IsGetter = true, Enumerable = true)]
        public ScriptObject GetHeadersCore() => _headers;

        /// <summary>Decoded response body.</summary>
        [Export("body", Enumerable = true)]
        public readonly string Body;

        /// <summary>Decoded response text.</summary>
        [Export("text", Enumerable = true)]
        public readonly string Text;

        /// <summary>Gets the buffered response bytes.</summary>
        [Export("bytes", IsGetter = true, Enumerable = true)]
        public ScriptUInt8Array GetBytesCore() => _bytes;
    }
}
