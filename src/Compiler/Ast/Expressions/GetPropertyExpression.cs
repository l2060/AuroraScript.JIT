namespace AuroraScript.Compiler.Ast.Expressions
{
    /// <summary>
    /// 成员函数表达式
    /// </summary>
    internal class GetPropertyExpression : OperatorExpression
    {
        internal GetPropertyExpression(Operator @operator, Expression objectExp, Expression propertyExp) : base(@operator)
        {
            Object = objectExp;
            Property = propertyExp;
            Object.Parent = this;
            Property.Parent = this;
        }

        public readonly Expression Object;
        public readonly Expression Property;


        public override void Accept(IAstVisitor visitor)
        {
            visitor.AcceptGetPropertyExpression(this);
        }
    }
}