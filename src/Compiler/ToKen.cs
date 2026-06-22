using AuroraScript.Tokens;

namespace AuroraScript.Compiler
{
    /// <summary>
    /// Represents a discrete, categorized unit of text (token) extracted from the script source code during lexical analysis.
    /// This base class provides the structure for all token types, including keywords, literals, and punctuation.
    /// </summary>
    public abstract class Token
    {
        /// <summary> A singleton representing the end of the source file. </summary>
        public static Token EOF = new EndOfFileToken();

        /// <summary>
        /// Gets or sets the <see cref="Symbols"/> object associated with this token, 
        /// which defines its category and identity (e.g., a specific keyword or operator).
        /// </summary>
        internal Symbols Symbol
        {
            get;
            set;
        }

        /// <summary>
        /// Gets the literal string value of the token as it appeared in the source code.
        /// </summary>
        public virtual string Value
        {
            get;
            internal set;
        }

        internal int NameId
        {
            get;
            set;
        }


        /// <summary>
        /// Gets a unique string identifier for the token, combining its value and exact source location.
        /// Useful for caching or diagnostic purposes.
        /// </summary>
        public string UniqueValue => $"{Value}_{Range.StartLine}_{Range.StartColumn}";

        /// <summary>
        /// Gets or sets the source range (span) where this token is located in the original script.
        /// </summary>
        public SourceSpan Range { get; set; }


        /// <summary>
        /// Gets or sets the line number where the token begins.
        /// This is a convenience property that wraps <see cref="Range"/>.
        /// </summary>
        internal int LineNumber
        {
            get => Range.StartLine;
            set
            {
                var range = Range;
                range.StartLine = value;
                Range = range;
            }
        }

        /// <summary>
        /// Gets or sets the column number where the token begins.
        /// This is a convenience property that wraps <see cref="Range"/>.
        /// </summary>
        internal int ColumnNumber
        {
            get => Range.StartColumn;
            set
            {
                var range = Range;
                range.StartColumn = value;
                Range = range;
            }
        }

        /// <summary>
        /// Gets or sets the name of the source file containing this token.
        /// This is a convenience property that wraps <see cref="Range"/>.
        /// </summary>
        public string FileName
        {
            get => Range.FileName;
            internal set
            {
                var range = Range;
                range.FileName = value;
                Range = range;
            }
        }

        /// <summary>
        /// Returns a string representation of the token, including its range, symbol, and value.
        /// </summary>
        public override string ToString()
        {
            return $"Range: {Range} Symbol: {Symbol} Value: {Value}";
        }
    }
}
