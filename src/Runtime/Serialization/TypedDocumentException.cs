using System;

namespace AuroraScript.Runtime.Serialization
{
    /// <summary>
    /// Represents a TDoc syntax, binding, or serialization error.
    /// </summary>
    public sealed class TypedDocumentException : AuroraException
    {
        /// <summary>Gets the source name associated with the error.</summary>
        public string SourceName { get; }

        /// <summary>Gets the one-based source line, or zero for a runtime value error.</summary>
        public int Line { get; }

        /// <summary>Gets the one-based source column, or zero for a runtime value error.</summary>
        public int Column { get; }

        /// <summary>Gets the data path associated with the error.</summary>
        public string DataPath { get; }

        internal TypedDocumentException(
            string message,
            string sourceName,
            int line,
            int column,
            string dataPath,
            Exception innerException = null)
            : base(FormatMessage(message, sourceName, line, column, dataPath), innerException)
        {
            SourceName = string.IsNullOrWhiteSpace(sourceName) ? "<tdoc>" : sourceName;
            Line = line;
            Column = column;
            DataPath = string.IsNullOrEmpty(dataPath) ? "$" : dataPath;
        }

        private static string FormatMessage(
            string message,
            string sourceName,
            int line,
            int column,
            string dataPath)
        {
            sourceName = string.IsNullOrWhiteSpace(sourceName) ? "<tdoc>" : sourceName;
            dataPath = string.IsNullOrEmpty(dataPath) ? "$" : dataPath;
            return line > 0
                ? $"{sourceName}({line},{column}) {dataPath}: {message}"
                : $"{sourceName} {dataPath}: {message}";
        }
    }
}
