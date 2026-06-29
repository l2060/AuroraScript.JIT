namespace AuroraScript.Compiler.Syntax
{
    internal readonly struct SyntaxTrivia
    {
        public SyntaxTrivia(
            SyntaxTriviaKind kind,
            int offset,
            int length,
            int startLine,
            int startColumn,
            int endLine,
            int endColumn)
        {
            Kind = kind;
            Offset = offset;
            Length = length;
            StartLine = startLine;
            StartColumn = startColumn;
            EndLine = endLine;
            EndColumn = endColumn;
        }

        public SyntaxTriviaKind Kind { get; }
        public int Offset { get; }
        public int Length { get; }
        public int StartLine { get; }
        public int StartColumn { get; }
        public int EndLine { get; }
        public int EndColumn { get; }
    }
}
