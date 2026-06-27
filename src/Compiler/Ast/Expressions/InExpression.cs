namespace AuroraScript.Compiler.Ast.Expressions
{
    /// <summary>
    /// 二元表达式
    /// </summary>
    internal class InExpression : OperatorExpression
    {


        internal InExpression(Operator @operator, NameExpression left, Expression right) : base(@operator)
        {
            Left = left;
            Right = right;
            Left.Parent = this;
            Right.Parent = this;
        }

        public readonly NameExpression Left;
        public readonly Expression Right;


        public override void Accept(IAstVisitor visitor)
        {
            visitor.AcceptInExpression(this);
        }
    }
}