namespace AuroraScript.Compiler.Ast.Expressions
{
    /// <summary>
    /// Runtime type assertion that also supplies an exact flow fact to the
    /// typed backend.
    /// </summary>
    internal class CheckExpression : Expression
    {
        internal CheckExpression(
            Expression value,
            Token asToken,
            Token typeToken)
            : this(value, asToken, null, typeToken)
        {
        }

        internal CheckExpression(
            Expression value,
            Token asToken,
            Token typeQualifier,
            Token typeToken)
        {
            Value = value;
            AssertedType = new TypeReference(typeQualifier, typeToken);
            AsToken = asToken;
            if (value != null) value.Parent = this;
        }

        public Expression Value { get; }
        public TypeReference AssertedType { get; }
        public string TypeName => AssertedType.Name;
        public Token AsToken { get; }
        public Token TypeToken => AssertedType.Token;

        public override void Accept(IAstVisitor visitor)
        {
            visitor.AcceptCheckExpression(this);
        }
    }

    /// <summary>An explicit, checked numeric conversion, distinct from an assertion.</summary>
    internal sealed class NumericCastExpression : CheckExpression
    {
        internal NumericCastExpression(Expression value, Token typeToken)
            : base(value, typeToken, typeToken)
        {
        }
    }
}
