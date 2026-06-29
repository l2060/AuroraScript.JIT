namespace AuroraScript.Compiler.Syntax
{
    internal readonly struct SyntaxToken
    {
        public SyntaxToken(
            SyntaxTokenKind kind,
            int offset,
            int length,
            int startLine,
            int startColumn,
            int endLine,
            int endColumn,
            int symbolId = -1)
        {
            Kind = kind;
            Offset = offset;
            Length = length;
            StartLine = startLine;
            StartColumn = startColumn;
            EndLine = endLine;
            EndColumn = endColumn;
            SymbolId = symbolId;
        }

        public SyntaxTokenKind Kind { get; }
        public int Offset { get; }
        public int Length { get; }
        public int StartLine { get; }
        public int StartColumn { get; }
        public int EndLine { get; }
        public int EndColumn { get; }
        public int SymbolId { get; }
    }
}
