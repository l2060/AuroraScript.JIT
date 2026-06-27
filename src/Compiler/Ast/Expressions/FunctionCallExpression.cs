using System;
using System.Collections.Generic;


namespace AuroraScript.Compiler.Ast.Expressions
{
    /// <summary>
    /// 函数调用
    /// </summary>
    internal class FunctionCallExpression : OperatorExpression
    {
        internal FunctionCallExpression(Operator @operator, Expression target) : base(@operator)
        {
            Target = target;
            Target.Parent = this;
        }

        private List<Expression> _arguments;

        public IReadOnlyList<Expression> Arguments => _arguments ?? (IReadOnlyList<Expression>)Array.Empty<Expression>();


        public void AddArgument(Expression expression)
        {
            this._arguments ??= new List<Expression>();
            this._arguments.Add(expression);
            expression.Parent = this;
        }

        public readonly Expression Target;


        public override void Accept(IAstVisitor visitor)
        {
            visitor.AcceptCallExpression(this);
        }
    }
}
