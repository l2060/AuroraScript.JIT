using AuroraScript.Compiler.Ast.Expressions;


namespace AuroraScript.Compiler.Ast.Statements
{
    internal class WhileStatement : Statement
    {
        internal WhileStatement(Expression condition, Statement body)
        {
            Condition = condition;
            Body = body;
            if (condition != null) condition.Parent = this;
            if (body != null) body.Parent = this;
        }

        public readonly Expression Condition;

        public readonly Statement Body;


        public override void Accept(IAstVisitor visitor)
        {
            visitor.AcceptWhileStatement(this);
        }

        public override string ToString()
        {
            return $"whele ({this.Condition}) {this.Body}";
        }


    }
}