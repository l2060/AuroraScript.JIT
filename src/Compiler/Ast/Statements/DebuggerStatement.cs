namespace AuroraScript.Compiler.Ast.Statements
{
    internal class DebuggerStatement : Statement
    {
        internal DebuggerStatement()
        {
        }

        public override void Accept(IAstVisitor visitor)
        {
            visitor.AcceptDebuggerExpression(this);
        }
    }
}
