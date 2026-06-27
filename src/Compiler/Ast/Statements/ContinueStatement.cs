namespace AuroraScript.Compiler.Ast.Statements
{
    internal class ContinueStatement : Statement
    {
        internal ContinueStatement()
        {
        }

        public override void Accept(IAstVisitor visitor)
        {
            visitor.AcceptContinueExpression(this);
        }
    }
}