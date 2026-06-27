using AuroraScript.Compiler.Ast.Expressions;


namespace AuroraScript.Compiler.Ast.Statements
{
    internal class IfStatement : Statement
    {
        internal IfStatement(Expression condition, Statement body, Statement else1)
        {
            Condition = condition;
            Body = body;
            Else = else1;
            if (condition != null) condition.Parent = this;
            if (body != null) body.Parent = this;
            if (else1 != null) else1.Parent = this;
        }

        public readonly Expression Condition;
        public readonly Statement Body;
        public readonly Statement Else;

        public override void Accept(IAstVisitor visitor)
        {
            visitor.AcceptIfStatement(this);
        }
    }
}