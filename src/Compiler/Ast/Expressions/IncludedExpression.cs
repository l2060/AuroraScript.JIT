namespace AuroraScript.Compiler.Ast.Expressions
{
    /// <summary>
    /// 二元表达式
    /// </summary>
    internal class IncludedExpression : OperatorExpression
    {


        internal IncludedExpression(Operator @operator, Expression left, Expression right) : base(@operator)
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
            visitor.AcceptIncludedExpression(this);
        }
    }
}