using System.Collections.Generic;


namespace AuroraScript.Compiler.Ast.Expressions
{
    internal class GroupExpression : OperatorExpression
    {
        internal GroupExpression(Operator @operator) : base(@operator)
        {
        }



        public Expression Expression => (Expression)_children[0];

        public override IEnumerable<AstNode> ChildNodes
        {
            get
            {
                foreach (var item in _children) yield return item;
            }
        }

        public override void Accept(IAstVisitor visitor)
        {
            visitor.AcceptGroupingExpression(this);
        }

        public override string ToString()
        {
            return $"({Expression})";
        }
    }
}