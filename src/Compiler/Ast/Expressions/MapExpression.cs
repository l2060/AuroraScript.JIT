using System;
using System.Collections.Generic;

namespace AuroraScript.Compiler.Ast.Expressions
{
    internal class MapExpression : OperatorExpression
    {
        private List<Expression> _entries;

        internal MapExpression(Operator @operator) : base(@operator)
        {
        }

        public IReadOnlyList<Expression> Entries => _entries ?? (IReadOnlyList<Expression>)Array.Empty<Expression>();

        public void AddEntry(Expression expression)
        {
            _entries ??= new List<Expression>();
            _entries.Add(expression);
            AttachParent(expression, this);
        }

        public override void Accept(IAstVisitor visitor)
        {
            visitor.AcceptMapExpression(this);
        }
    }
}
