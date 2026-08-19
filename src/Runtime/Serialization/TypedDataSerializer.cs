using System;

namespace AuroraScript.Runtime.Serialization
{
    /// <summary>
    /// Serializes and deserializes standalone AuroraScript typed-data documents.
    /// </summary>
    public static class TypedDataSerializer
    {
        /// <summary>Serializes one script datum as a standalone ATD document.</summary>
        public static string Serialize(
            AuroraEngine engine,
            ScriptDatum value,
            TypedDataOptions options = null)
        {
            ArgumentNullException.ThrowIfNull(engine);
            options ??= TypedDataOptions.Default;
            ValidateOptions(options);

            var writer = new TypedDataWriter(engine, options);
            try
            {
                return writer.Write(value);
            }
            finally
            {
                writer.Dispose();
            }
        }

        /// <summary>Deserializes one standalone ATD document into a script datum.</summary>
        public static ScriptDatum Deserialize(
            AuroraEngine engine,
            string text,
            TypedDataOptions options = null)
        {
            ArgumentNullException.ThrowIfNull(engine);
            ArgumentNullException.ThrowIfNull(text);
            options ??= TypedDataOptions.Default;
            ValidateOptions(options);

            var reader = new TypedDataReader(engine, text, options);
            try
            {
                return reader.ReadDocument();
            }
            finally
            {
                reader.Dispose();
            }
        }

        private static void ValidateOptions(TypedDataOptions options)
        {
            if (options.MaxDepth <= 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(options),
                    options.MaxDepth,
                    "TypedDataOptions.MaxDepth must be greater than zero.");
            }
        }
    }
}
