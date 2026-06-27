using System;
using System.Collections.Generic;

namespace AuroraScript.Compiler.Ast.Expressions
{
    internal class GroupExpression : OperatorExpression
    {
        private List<Expression> _expressions;

        internal GroupExpression(Operator @operator) : base(@operator)
        {
        }

        public IReadOnlyList<Expression> Expressions => _expressions ?? (IReadOnlyList<Expression>)Array.Empty<Expression>();

        public Expression Expression => _expressions == null || _expressions.Count == 0 ? null : _expressions[0];

        public void AddExpression(Expression expression)
        {
            _expressions ??= new List<Expression>();
            _expressions.Add(expression);
            AttachParent(expression, this);
        }

        public override void Accept(IAstVisitor visitor)
        {
            visitor.AcceptGroupingExpression(this);
        }
    }
}
