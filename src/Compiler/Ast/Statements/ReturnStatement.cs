using AuroraScript.Compiler.Ast.Expressions;


namespace AuroraScript.Compiler.Ast.Statements
{
    internal class ReturnStatement : Statement
    {
        internal ReturnStatement(Expression expression)
        {
            this.Expression = expression;
            if (expression != null) expression.Parent = this;
        }

        public readonly Expression Expression;

        public override void Accept(IAstVisitor visitor)
        {
            visitor.AcceptReturnStatement(this);
        }

        public override string ToString()
        {
            return $"return {Expression}";
        }
    }
}