using System;
using System.IO;
using System.Text;

namespace AuroraScript.Runtime.Serialization
{
    /// <summary>
    /// Provides engine-bound TDoc serialization helpers for text, files, and streams.
    /// </summary>
    public sealed class AuroraTypedDocument
    {
        private static readonly UTF8Encoding Utf8WithoutBom = new(false);

        /// <summary>
        /// Creates a TDoc helper bound to one AuroraScript engine.
        /// </summary>
        /// <param name="engine">Engine used for CLR type registration and runtime formatting options.</param>
        /// <param name="options">Default options used when an operation does not supply its own options.</param>
        public AuroraTypedDocument(AuroraEngine engine, TypedDocumentOptions options = null)
        {
            Engine = engine ?? throw new ArgumentNullException(nameof(engine));
            Options = options ?? TypedDocumentOptions.Default;
        }

        /// <summary>Gets the engine used by this helper.</summary>
        public AuroraEngine Engine { get; }

        /// <summary>Gets the default options used by this helper.</summary>
        public TypedDocumentOptions Options { get; }

        /// <summary>Serializes a script datum to TDoc text.</summary>
        public string Serialize(ScriptDatum value, TypedDocumentOptions options = null)
        {
            return TypedDocumentSerializer.Serialize(Engine, value, options ?? Options);
        }

        /// <summary>Deserializes TDoc text to a script datum.</summary>
        public ScriptDatum Deserialize(string text, TypedDocumentOptions options = null)
        {
            return TypedDocumentSerializer.Deserialize(Engine, text, options ?? Options);
        }

        /// <summary>Serializes a script datum and writes the resulting TDoc text to a file.</summary>
        public void WriteFile(
            string path,
            ScriptDatum value,
            TypedDocumentOptions options = null,
            Encoding encoding = null)
        {
            ArgumentNullException.ThrowIfNull(path);
            using var writer = new StreamWriter(path, append: false, encoding ?? Utf8WithoutBom);
            TypedDocumentSerializer.SerializeTo(writer, Engine, value, options ?? Options, path);
        }

        /// <summary>Reads a TDoc file and deserializes it to a script datum.</summary>
        public ScriptDatum ReadFile(
            string path,
            TypedDocumentOptions options = null,
            Encoding encoding = null)
        {
            ArgumentNullException.ThrowIfNull(path);
            var text = File.ReadAllText(path, encoding ?? Utf8WithoutBom);
            return TypedDocumentSerializer.Deserialize(Engine, text, options ?? Options, path);
        }

        /// <summary>Serializes a script datum and writes the resulting TDoc text to a stream.</summary>
        /// <remarks>The stream remains open by default.</remarks>
        public void WriteStream(
            Stream stream,
            ScriptDatum value,
            TypedDocumentOptions options = null,
            Encoding encoding = null,
            bool leaveOpen = true)
        {
            ArgumentNullException.ThrowIfNull(stream);
            using var writer = new StreamWriter(stream, encoding ?? Utf8WithoutBom, 1024, leaveOpen);
            TypedDocumentSerializer.SerializeTo(writer, Engine, value, options ?? Options, sourceName: null);
        }

        /// <summary>Reads TDoc text from a stream and deserializes it to a script datum.</summary>
        /// <remarks>The stream remains open by default.</remarks>
        public ScriptDatum ReadStream(
            Stream stream,
            TypedDocumentOptions options = null,
            Encoding encoding = null,
            bool leaveOpen = true)
        {
            ArgumentNullException.ThrowIfNull(stream);
            using var reader = new StreamReader(
                stream,
                encoding ?? Utf8WithoutBom,
                detectEncodingFromByteOrderMarks: true,
                bufferSize: 1024,
                leaveOpen: leaveOpen);
            return Deserialize(reader.ReadToEnd(), options);
        }

    }
}
