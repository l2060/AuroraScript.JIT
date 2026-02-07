namespace AuroraScript
{
    /// <summary>
    /// Represents an error that occurs during the lexical analysis (scanning) of a script.
    /// </summary>
    public class AuroraLexicalException : AuroraException
    {
        /// <summary> Gets the name of the file where the lexical error occurred. </summary>
        public readonly string FileName;
        /// <summary> Gets the line number where the error occurred. </summary>
        public readonly int LineNumber;
        /// <summary> Gets the column number where the error occurred. </summary>
        public readonly int ColumnNumber;

        /// <summary>
        /// Initializes a new instance of the <see cref="AuroraLexicalException"/> class.
        /// </summary>
        /// <param name="fileName">The file name.</param>
        /// <param name="lineNumber">The line number.</param>
        /// <param name="columnNumber">The column number.</param>
        /// <param name="message">The error message.</param>
        internal AuroraLexicalException(string fileName, int lineNumber, int columnNumber, string message) : base(message)
        {
            ColumnNumber = columnNumber;
            FileName = fileName;
            LineNumber = lineNumber;
        }

        /// <summary>
        /// Returns a formatted string that represents the lexical exception.
        /// </summary>
        /// <returns>A string representation of the exception.</returns>
        public override string ToString()
        {
            return $"{Message} Location: {FileName} line:{LineNumber}, column:{ColumnNumber}";
        }
    }
}