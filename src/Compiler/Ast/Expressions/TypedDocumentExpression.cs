namespace AuroraScript.Compiler.Ast.Expressions
{
    /// <summary>
    /// Compile-time wrapper for a value written in the native TDoc literal
    /// syntax. The wrapper carries the optional static type name; the runtime
    /// value is emitted directly from <see cref="Value"/> and this node is not
    /// retained by the runtime.
    /// </summary>
    internal sealed class TypedDocumentExpression : Expression
    {
        internal TypedDocumentExpression(Expression value, string typeName, bool interpolation, Token typeToken = null)
        {
            Value = value;
            TypeName = typeName;
            IsInterpolation = interpolation;
            TypeToken = typeToken;
            if (value != null) value.Parent = this;
        }

        public readonly Expression Value;
        public readonly string TypeName;
        public readonly bool IsInterpolation;
        public readonly Token TypeToken;

        public override void Accept(IAstVisitor visitor)
        {
            visitor.AcceptTypedDocumentExpression(this);
        }
    }
}
