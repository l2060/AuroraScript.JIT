using AuroraScript.Compiler.Ast.Expressions;

namespace AuroraScript.Compiler.Ast.Statements
{
    internal class ThrowStatement : Statement
    {
        internal ThrowStatement(Expression expression)
        {
            this.Expression = expression;
            if (expression != null) expression.Parent = this;
        }

        public readonly Expression Expression;


        public override void Accept(IAstVisitor visitor)
        {
            visitor.AcceptThrowStatement(this);
        }

        public override string ToString()
        {
            return $"throw {Expression}";
        }
    }
}
