namespace AuroraScript.Compiler.Ast.Expressions
{
    internal class GroupExpression : OperatorExpression
    {
        internal GroupExpression(Operator @operator) : base(@operator)
        {
        }



        public Expression Expression => _children == null || _children.Count == 0 ? null : (Expression)_children[0];

        public override void Accept(IAstVisitor visitor)
        {
            visitor.AcceptGroupingExpression(this);
        }

        public override string ToString()
        {
            return $"({Expression})";
        }
    }
}
