namespace AuroraScript.Compiler.Ast.Expressions
{

    internal class NewExpression : OperatorExpression
    {
        internal NewExpression(Operator @operator, FunctionCallExpression expression) : base(@operator)
        {
            Expression = expression;
            Expression.Parent = this;
        }

        public readonly FunctionCallExpression Expression;

        public override void Accept(IAstVisitor visitor)
        {
            visitor.AcceptNewExpression(this);
        }
    }
}
