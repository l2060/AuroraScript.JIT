namespace AuroraScript.Compiler.Ast.Expressions
{
    internal class MapKeyValueExpression : OperatorExpression
    {
        internal MapKeyValueExpression(Token key, Expression value) : base(Operator.SetMember)
        {
            this.Key = key;
            this.Value = value;
            if (value != null) value.Parent = this;
        }


        public readonly Token Key;
        public readonly Expression Value;

        public override void Accept(IAstVisitor visitor)
        {

        }


        public override string ToString()
        {
            if (Key != null)
            {
                return $"{Key.Value}: {Value}";
            }
            else
            {
                return Value.ToString();
            }
        }
    }
}