namespace AuroraScript.Compiler.Ast
{
    /// <summary>
    /// A source-level type contract. Type references are intentionally kept
    /// separate from backend flow facts so user-defined types do not expand
    /// the native representation enum.
    /// </summary>
    internal sealed class TypeReference
    {
        internal TypeReference(Token token)
            : this(null, token)
        {
        }

        internal TypeReference(Token qualifier, Token token)
        {
            Qualifier = qualifier;
            Token = token;
        }

        public string Name => Token.Value;

        public string QualifierName => Qualifier?.Value;

        public string DisplayName => Qualifier == null
            ? Name
            : Qualifier.Value + "." + Name;

        public Token Qualifier { get; }

        public Token Token { get; }
    }
}
