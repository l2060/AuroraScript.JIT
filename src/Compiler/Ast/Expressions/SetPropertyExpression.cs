namespace AuroraScript.Compiler.Ast.Expressions
{
    internal class SetPropertyExpression : OperatorExpression
    {

        public SetPropertyExpression(Expression obj, Expression property, Expression value) : base(Operator.Assignment)
        {
            Object = obj;
            Property = property;
            Value = value;
            Object.Parent = this;
            Property.Parent = this;
            Value.Parent = this;
        }

        public readonly Expression Object;
        public readonly Expression Property;
        public readonly Expression Value;

        public override void Accept(IAstVisitor visitor)
        {
            visitor.AcceptSetPropertyExpression(this);
        }

        public override string ToString()
        {
            return $"{Object}.{Property} = {Value}";
        }
    }
}
