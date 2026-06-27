using AuroraScript.Compiler.Ast.Expressions;


namespace AuroraScript.Compiler.Ast.Statements
{
    internal class ExpressionStatement : Statement
    {
        public readonly Expression Expression;

        internal ExpressionStatement(Expression expression)
        {
            this.Expression = expression;
            if (expression != null) expression.Parent = this;
        }
        public override void Accept(IAstVisitor visitor)
        {
            visitor.AcceptExpressionStatement(this);
        }
    }
}