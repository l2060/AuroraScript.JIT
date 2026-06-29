namespace AuroraScript.Compiler.Syntax
{
    internal enum SyntaxTriviaKind : byte
    {
        WhiteSpace,
        NewLine,
        LineComment,
        BlockComment,
        SkippedText
    }
}
