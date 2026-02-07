using AuroraScript.Compiler.Ast.Expressions;
using System.Collections.Generic;

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

        public override IEnumerable<AstNode> ChildNodes
        {
            get
            {
                if (Initializer != null) yield return Initializer;
                if (Iterator != null) yield return Iterator;
                if (Body != null) yield return Body;
            }
        }


        public override void Accept(IAstVisitor visitor)
        {
            visitor.AcceptForInStatement(this);
        }
    }
}
