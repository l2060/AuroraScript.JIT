namespace AuroraScript.Compiler.Ast.Expressions
{

    /// <summary>
    /// 展开运算符
    /// </summary>
    internal class SpreadExpression : PrefixUnaryExpression
    {
        internal SpreadExpression(Expression expression) : base(Operator.PreSpread, expression)
        {
        }


        public override void Accept(IAstVisitor visitor)
        {
            visitor.AcceptSpreadExpression(this);
        }
    }
}