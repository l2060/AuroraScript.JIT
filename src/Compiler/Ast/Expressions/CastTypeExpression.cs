namespace AuroraScript.Compiler.Ast.Expressions
{
    internal class CastTypeExpression : PrefixUnaryExpression
    {
        internal CastTypeExpression(Operator @operator, Expression expression) : base(@operator, expression)
        {
        }

        public Expression Typed
        {
            get;
            set
            {
                field = value;
                field.Parent = this;
            }
        }

        public override void Accept(IAstVisitor visitor)
        {

        }
    }
}