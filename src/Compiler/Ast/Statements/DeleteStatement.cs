using AuroraScript.Compiler.Ast.Expressions;



namespace AuroraScript.Compiler.Ast.Statements
{
    internal class DeleteStatement : Statement
    {
        internal DeleteStatement(Expression expression)
        {
            this.Expression = expression;
            expression.Parent = this;
        }

        public readonly Expression Expression;

        public override void Accept(IAstVisitor visitor)
        {
            visitor.AcceptDeleteStatement(this);
        }

        public override string ToString()
        {
            return $"delete {Expression}";
        }
    }
}