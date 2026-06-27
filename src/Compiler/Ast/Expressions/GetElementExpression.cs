namespace AuroraScript.Compiler.Ast.Expressions
{
    internal class GetElementExpression : OperatorExpression
    {
        internal GetElementExpression(Operator @operator, Expression objectExp, Expression indexExp) : base(@operator)
        {
            Object = objectExp;
            Index = indexExp;
            Object.Parent = this;
            Index.Parent = this;
        }

        public readonly Expression Object;
        public readonly Expression Index;


        public override void Accept(IAstVisitor visitor)
        {
            visitor.AcceptGetElementExpression(this);
        }
    }
}