namespace AuroraScript.Compiler.Ast.Expressions
{
    internal class MapKeyValueExpression : OperatorExpression
    {
        internal MapKeyValueExpression(Token key, Expression value, bool readOnly = false, Token readOnlyToken = null) : base(Operator.SetMember)
        {
            this.Key = key;
            this.Value = value;
            this.ReadOnly = readOnly;
            this.ReadOnlyToken = readOnlyToken;
            if (value != null) value.Parent = this;
        }


        public readonly Token Key;
        public readonly Expression Value;
        public readonly bool ReadOnly;
        public readonly Token ReadOnlyToken;

        public override void Accept(IAstVisitor visitor)
        {

        }
    }
}
