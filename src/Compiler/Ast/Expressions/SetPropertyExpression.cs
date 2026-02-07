using System.Collections.Generic;

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

        public override IEnumerable<AstNode> ChildNodes
        {
            get
            {
                if (Object != null) yield return Object;
                if (Property != null) yield return Property;
                if (Value != null) yield return Value;
            }
        }

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
