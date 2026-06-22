using AuroraScript.Compiler;
using AuroraScript.Compiler.Ast;

namespace AuroraScript
{
    /// <summary>
    /// Represents an error that occurs during the CIL emission phase of the compiler.
    /// Includes location information from the AST or Token.
    /// </summary>
    public class AuroraEmitException : AuroraException
    {
        /// <summary> Gets the name of the file where the error occurred. </summary>
        public readonly string FileName;
        /// <summary> Gets the starting line number of the error. </summary>
        public readonly int StartLine;
        /// <summary> Gets the starting column number of the error. </summary>
        public readonly int StartColumn;
        private readonly string _message;

        /// <summary> Gets the error message. </summary>
        public override string Message => _message;

        /// <summary>
        /// Initializes a new instance of the <see cref="AuroraEmitException"/> class from an AST node.
        /// </summary>
        /// <param name="node">The AST node where the emit error occurred.</param>
        /// <param name="message">The error message.</param>
        internal AuroraEmitException(AstNode node, string message)
        {
            FileName = node.Range.FileName;
            StartLine = node.Range.StartLine;
            StartColumn = node.Range.StartColumn;
            _message = message;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="AuroraEmitException"/> class from a Token.
        /// </summary>
        /// <param name="node">The token where the emit error occurred.</param>
        /// <param name="message">The error message.</param>
        internal AuroraEmitException(Token node, string message)
        {
            _message = message;
            FileName = node.Range.FileName;
            StartLine = node.Range.StartLine;
            StartColumn = node.Range.StartColumn;
            _message = message;
        }

        internal AuroraEmitException(SourceSpan range, string message)
        {
            FileName = range.FileName;
            StartLine = range.StartLine;
            StartColumn = range.StartColumn;
            _message = message;
        }

        /// <summary>
        /// Returns a formatted string that represents the exception, including location details.
        /// </summary>
        /// <returns>A string representation of the exception.</returns>
        public override string ToString()
        {
            return $"{_message} Location: {FileName} line:{StartLine}, column:{StartColumn}";
        }
    }
}
