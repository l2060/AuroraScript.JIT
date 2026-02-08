using System.Collections.Generic;

namespace AuroraScript.Compiler.Ast.Statements
{
    internal class TryStatement : Statement
    {
        internal TryStatement(Statement body, string catchVariable, Statement catchBody, Statement finallyBody)
        {
            Body = body;
            CatchVariable = catchVariable;
            CatchBody = catchBody;
            FinallyBody = finallyBody;
            if (body != null) body.Parent = this;
            if (catchBody != null) catchBody.Parent = this;
            if (finallyBody != null) finallyBody.Parent = this;
        }

        public readonly Statement Body;
        public readonly string CatchVariable;
        public readonly Statement CatchBody;
        public readonly Statement FinallyBody;

        public override IEnumerable<AstNode> ChildNodes
        {
            get
            {
                if (Body != null) yield return Body;
                if (CatchBody != null) yield return CatchBody;
                if (FinallyBody != null) yield return FinallyBody;
            }
        }

        public override void Accept(IAstVisitor visitor)
        {
            visitor.AcceptTryStatement(this);
        }
    }
}
