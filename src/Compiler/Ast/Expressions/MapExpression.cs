namespace AuroraScript.Compiler.Ast.Expressions
{
    internal class MapExpression : OperatorExpression
    {
        internal MapExpression(Operator @operator) : base(@operator)
        {
        }

        public override void Accept(IAstVisitor visitor)
        {
            visitor.AcceptMapExpression(this);
        }
    }
}