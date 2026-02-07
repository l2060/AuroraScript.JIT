namespace AuroraScript.Compiler.Ast.Expressions
{
    /// <summary>
    /// 赋值
    /// </summary>
    internal class AssignmentExpression : OperatorExpression
    {
        internal AssignmentExpression(Operator @operator, Expression left, Expression right) : base(@operator)
        {
            Left = left;
            Right = right;
            Left.Parent = this;
            Right.Parent = this;
        }

        public readonly Expression Left;
        public readonly Expression Right;


        public override void AddNode(AstNode node)
        {
            throw new System.Exception();
        }

        public override void Accept(IAstVisitor visitor)
        {
            visitor.AcceptAssignmentExpression(this);
        }


        public override string ToString()
        {
            return $"{this.Left} {this.Operator.Symbol.Name} {this.Right}";
        }
    }
}