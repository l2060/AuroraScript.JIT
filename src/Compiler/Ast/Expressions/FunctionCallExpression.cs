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
            this.Arguments = new List<Expression>();
            Target = target;
            Target.Parent = this;
        }

        public List<Expression> Arguments;


        public void AddArgument(Expression expression)
        {
            this.Arguments.Add(expression);
            expression.Parent = this;
        }

        public readonly Expression Target;


        public override void Accept(IAstVisitor visitor)
        {
            visitor.AcceptCallExpression(this);
        }


        public override string ToString()
        {
            return $"{Target}({String.Join(", ", Arguments)})";
        }

    }
}