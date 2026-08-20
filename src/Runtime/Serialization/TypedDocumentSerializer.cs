using System;

namespace AuroraScript.Runtime.Serialization
{
    /// <summary>
    /// Serializes and deserializes standalone AuroraScript typed documents.
    /// </summary>
    public static class TypedDocumentSerializer
    {
        /// <summary>Serializes one script datum as a standalone TDoc document.</summary>
        public static string Serialize(
            AuroraEngine engine,
            ScriptDatum value,
            TypedDocumentOptions options = null)
        {
            return Serialize(engine, value, options, sourceName: null);
        }

        internal static string Serialize(
            AuroraEngine engine,
            ScriptDatum value,
            TypedDocumentOptions options,
            string sourceName)
        {
            ArgumentNullException.ThrowIfNull(engine);
            options ??= TypedDocumentOptions.Default;
            ValidateOptions(options);

            var writer = new TypedDocumentWriter(engine, options, sourceName);
            try
            {
                return writer.Write(value);
            }
            finally
            {
                writer.Dispose();
            }
        }

        /// <summary>Deserializes one standalone TDoc document into a script datum.</summary>
        public static ScriptDatum Deserialize(
            AuroraEngine engine,
            string text,
            TypedDocumentOptions options = null)
        {
            return Deserialize(engine, text, options, sourceName: null);
        }

        internal static ScriptDatum Deserialize(
            AuroraEngine engine,
            string text,
            TypedDocumentOptions options,
            string sourceName)
        {
            ArgumentNullException.ThrowIfNull(engine);
            ArgumentNullException.ThrowIfNull(text);
            options ??= TypedDocumentOptions.Default;
            ValidateOptions(options);

            var reader = new TypedDocumentReader(engine, text, options, sourceName);
            try
            {
                return reader.ReadDocument();
            }
            finally
            {
                reader.Dispose();
            }
        }

        private static void ValidateOptions(TypedDocumentOptions options)
        {
            if (options.MaxDepth <= 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(options),
                    options.MaxDepth,
                    "TypedDocumentOptions.MaxDepth must be greater than zero.");
            }
        }
    }
}
