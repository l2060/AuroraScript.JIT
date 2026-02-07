using AuroraScript.Compiler;

namespace AuroraScript
{
    /// <summary>
    /// Represents an error that occurs during the parsing of a script.
    /// </summary>
    public class AuroraParseException : AuroraException
    {
        /// <summary> Gets the name of the file where the parse error occurred. </summary>
        public readonly string FileName;
        /// <summary> Gets the line number where the error occurred. </summary>
        public readonly int LineNumber;
        /// <summary> Gets the column number where the error occurred. </summary>
        public readonly int ColumnNumber;
        /// <summary> Gets the token that caused the parse error. </summary>
        public readonly Token Token;

        /// <summary>
        /// Initializes a new instance of the <see cref="AuroraParseException"/> class.
        /// </summary>
        /// <param name="fileName">The file name.</param>
        /// <param name="token">The token where the error occurred.</param>
        /// <param name="message">The error message.</param>
        internal AuroraParseException(string fileName, Token token, string message) : base(message)
        {
            ColumnNumber = token.ColumnNumber;
            FileName = fileName;
            LineNumber = token.LineNumber;
            Token = token;
        }

        /// <summary>
        /// Returns a formatted string that represents the parse exception, including the token value.
        /// </summary>
        /// <returns>A string representation of the exception.</returns>
        public override string ToString()
        {
            return $"{Message} Token: {Token.Value} Location: {FileName} line:{LineNumber}, column:{ColumnNumber}";
        }
    }
}