using AuroraScript.Compiler.Ast.Expressions;

namespace AuroraScript.Compiler.Ast.Statements
{

    internal class ForInStatement : Statement
    {
        internal ForInStatement(VariableDeclaration initializer, InExpression iterator, Statement body)
        {
            Initializer = initializer;
            Iterator = iterator;
            Body = body;
            if (initializer != null) initializer.Parent = this;
            if (iterator != null) iterator.Parent = this;
            if (body != null) body.Parent = this;
        }

        /// <summary>
        /// for in initializer
        /// may be assignment
        /// may be variable declaration
        /// </summary>
        public readonly VariableDeclaration Initializer;

        public readonly InExpression Iterator;

        public readonly Statement Body;


        public override void Accept(IAstVisitor visitor)
        {
            visitor.AcceptForInStatement(this);
        }
    }
}
