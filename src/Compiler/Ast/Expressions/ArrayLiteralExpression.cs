using System;
using System.Collections.Generic;

namespace AuroraScript.Compiler.Ast.Expressions
{
    internal class ArrayLiteralExpression : OperatorExpression
    {
        private List<Expression> _elements;

        internal ArrayLiteralExpression() : base(Operator.ArrayLiteral)
        {
        }

        public IReadOnlyList<Expression> Elements => _elements ?? (IReadOnlyList<Expression>)Array.Empty<Expression>();

        public void AddElement(Expression expression)
        {
            _elements ??= new List<Expression>();
            _elements.Add(expression);
            AttachParent(expression, this);
        }

        public override void Accept(IAstVisitor visitor)
        {
            visitor.AcceptArrayExpression(this);
        }
    }
}
