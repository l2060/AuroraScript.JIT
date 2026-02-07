namespace AuroraScript.Compiler.Ast.Expressions
{
    internal class CompoundExpression : OperatorExpression
    {
        internal CompoundExpression(Operator @operator, Expression left, Expression right) : base(@operator)
        {
            Left = left;
            Right = right;
            Left.Parent = this;
            Right.Parent = this;
        }

        public readonly Expression Left;
        public readonly Expression Right;

        public override void Accept(IAstVisitor visitor)
        {
            visitor.AcceptCompoundExpression(this);
        }
    }
}
