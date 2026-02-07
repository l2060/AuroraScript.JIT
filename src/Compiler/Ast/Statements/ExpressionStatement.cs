using AuroraScript.Compiler.Ast.Expressions;
using System.Collections.Generic;


namespace AuroraScript.Compiler.Ast.Statements
{
    internal class ExpressionStatement : Statement
    {
        public readonly Expression Expression;

        internal ExpressionStatement(Expression expression)
        {
            this.Expression = expression;
            if (expression != null) expression.Parent = this;
        }

        public override IEnumerable<AstNode> ChildNodes
        {
            get
            {
                if (Expression != null) yield return Expression;
            }
        }

        public override void Accept(IAstVisitor visitor)
        {
            visitor.AcceptExpressionStatement(this);
        }
    }
}