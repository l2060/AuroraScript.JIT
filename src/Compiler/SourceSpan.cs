namespace AuroraScript.Compiler
{
    /// <summary>
    /// Represents a range or span of text within a source file.
    /// Used for mapping compiler elements (tokens, AST nodes) back to their original source locations.
    /// </summary>
    public struct SourceSpan
    {
        /// <summary> Gets or sets the 1-based start line number. </summary>
        public int StartLine { get; set; }
        /// <summary> Gets or sets the 1-based start column number. </summary>
        public int StartColumn { get; set; }
        /// <summary> Gets or sets the 1-based end line number. </summary>
        public int EndLine { get; set; }
        /// <summary> Gets or sets the 1-based end column number. </summary>
        public int EndColumn { get; set; }
        /// <summary> Gets or sets the start offset of the span in characters from the beginning of the file. </summary>
        public int Offset { get; set; }
        /// <summary> Gets or sets the length of the span in characters. </summary>
        public int Length { get; set; }
        /// <summary> Gets or sets the name/path of the source file. </summary>
        public string FileName { get; set; }

        /// <summary> Represents an empty or invalid source span. </summary>
        public static readonly SourceSpan None = new SourceSpan { StartLine = -1, EndLine = -1, Offset = -1, Length = 0 };

        /// <summary>
        /// Sets the file name for this span.
        /// </summary>
        /// <param name="filename">The name of the file.</param>
        internal void SetFileName(string filename)
        {
            FileName = filename;
        }

        /// <summary>
        /// Returns a string that represents the current source span.
        /// </summary>
        /// <returns>A formatted string indicating the file name and line/column range.</returns>
        public override string ToString()
        {
            if (StartLine == -1) return "None";
            return $"{FileName} ({StartLine},{StartColumn})-({EndLine},{EndColumn})";
        }
    }
}
