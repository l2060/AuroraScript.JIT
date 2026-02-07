using System.Collections.Generic;

namespace AuroraScript.Compiler.Ast.Expressions
{
    /// <summary>
    /// 二元表达式
    /// </summary>
    internal class IncludedExpression : OperatorExpression
    {


        internal IncludedExpression(Operator @operator, Expression left, Expression right) : base(@operator)
        {
            Left = left;
            Right = right;
            Left.Parent = this;
            Right.Parent = this;
        }

        public readonly Expression Left;
        public readonly Expression Right;


        public override IEnumerable<AstNode> ChildNodes
        {
            get
            {
                if (Left != null) yield return Left;
                if (Right != null) yield return Right;
            }
        }

        public override void Accept(IAstVisitor visitor)
        {
            visitor.AcceptIncludedExpression(this);
        }


        public override string ToString()
        {
            var isPriority = false;
            if (this.Parent is BinaryExpression parent)
            {
                isPriority = parent.Operator.Precedence > this.Operator.Precedence;
            }
            var value = $"{Left} {Operator} {Right}";
            if (isPriority) return $"({value})";
            return value;
        }

    }
}