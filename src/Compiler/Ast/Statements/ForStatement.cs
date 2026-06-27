using AuroraScript.Compiler.Ast.Expressions;


namespace AuroraScript.Compiler.Ast.Statements
{
    internal class ForStatement : Statement
    {
        internal ForStatement(Expression condition, AstNode initializer, Expression incrementor, Statement body)
        {
            Condition = condition;
            Initializer = initializer;
            Incrementor = incrementor;
            Body = body;
            if (condition != null) condition.Parent = this;
            if (initializer != null) initializer.Parent = this;
            if (incrementor != null) incrementor.Parent = this;
            if (body != null) body.Parent = this;
        }

        public readonly Expression Condition;

        public readonly Statement Body;

        /// <summary>
        /// for initializer
        /// may be assignment
        /// may be variable declaration
        /// </summary>
        public readonly AstNode Initializer;

        /// <summary>
        /// for incrementor
        /// contains multiple sentences
        /// </summary>
        public readonly Expression Incrementor;

        public override void Accept(IAstVisitor visitor)
        {
            visitor.AcceptForStatement(this);
        }
    }
}