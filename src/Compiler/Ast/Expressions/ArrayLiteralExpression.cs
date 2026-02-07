using System;

namespace AuroraScript.Compiler.Ast.Expressions
{
    internal class ArrayLiteralExpression : OperatorExpression
    {
        internal ArrayLiteralExpression() : base(Operator.ArrayLiteral)
        {
        }

        public override void Accept(IAstVisitor visitor)
        {
            visitor.AcceptArrayExpression(this);
        }

        public override string ToString()
        {
            return $"[{String.Join(", ", ChildNodes)}]";
        }


    }
}