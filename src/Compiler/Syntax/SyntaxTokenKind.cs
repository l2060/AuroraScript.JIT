namespace AuroraScript.Compiler.Syntax
{
    internal enum SyntaxTokenKind : byte
    {
        Identifier,
        Keyword,
        Punctuator,
        Operator,
        String,
        StringBlock,
        StringTemplate,
        Number,
        Regex,
        Boolean,
        Null,
        EndOfFile
    }
}
