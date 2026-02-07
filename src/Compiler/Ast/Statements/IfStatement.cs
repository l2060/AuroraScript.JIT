using AuroraScript.Compiler.Ast.Expressions;
using System.Collections.Generic;


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

        public override IEnumerable<AstNode> ChildNodes
        {
            get
            {
                if (Condition != null) yield return Condition;
                if (Body != null) yield return Body;
                if (Else != null) yield return Else;
            }
        }

        public override void Accept(IAstVisitor visitor)
        {
            visitor.AcceptIfStatement(this);
        }
    }
}