namespace AuroraScript.Compiler.Ast.Expressions
{
    /// <summary>
    /// Runtime type assertion that also supplies an exact flow fact to the
    /// typed backend.
    /// </summary>
    internal sealed class CheckExpression : Expression
    {
        internal CheckExpression(
            Expression value,
            string typeName,
            Token asToken,
            Token typeToken)
        {
            Value = value;
            TypeName = typeName;
            AsToken = asToken;
            TypeToken = typeToken;
            if (value != null) value.Parent = this;
        }

        public Expression Value { get; }
        public string TypeName { get; }
        public Token AsToken { get; }
        public Token TypeToken { get; }

        public override void Accept(IAstVisitor visitor)
        {
            visitor.AcceptCheckExpression(this);
        }
    }
}
