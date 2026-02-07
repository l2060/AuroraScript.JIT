namespace AuroraScript.Compiler.Ast.Statements
{
    internal class BreakStatement : Statement
    {
        internal BreakStatement()
        {
        }

        public override void Accept(IAstVisitor visitor)
        {
            visitor.AcceptBreakExpression(this);
        }
    }
}