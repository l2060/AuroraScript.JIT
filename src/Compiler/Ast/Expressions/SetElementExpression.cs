namespace AuroraScript.Compiler.Ast.Expressions
{
    internal class SetElementExpression : OperatorExpression
    {
        public SetElementExpression(Expression obj, Expression index, Expression value) : base(Operator.Assignment)
        {
            Object = obj;
            Index = index;
            Value = value;
            Object.Parent = this;
            Index.Parent = this;
            Value.Parent = this;
        }

        public readonly Expression Object;
        public readonly Expression Index;
        public readonly Expression Value;

        public override void Accept(IAstVisitor visitor)
        {
            visitor.AcceptSetElementExpression(this);
        }
    }
}
