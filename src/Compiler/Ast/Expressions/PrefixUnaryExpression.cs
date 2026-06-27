namespace AuroraScript.Compiler.Ast.Expressions
{
    /// <summary>
    /// PrefixUnary Expression
    /// ++i
    /// --i
    /// </summary>
    internal abstract class PrefixUnaryExpression : OperatorExpression
    {
        internal PrefixUnaryExpression(Operator @operator, Expression expression) : base(@operator)
        {
            Expression = expression;
            Expression.Parent = this;
        }

        public readonly Expression Expression;

    }
}