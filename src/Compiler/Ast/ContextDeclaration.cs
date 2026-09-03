using AuroraScript.Compiler.Ast.Statements;

namespace AuroraScript.Compiler.Ast
{
    /// <summary>
    /// A module-level alias for the current <c>ScriptContext.UserState</c>.
    /// A typed declaration proves that object as a host NativeType.
    /// </summary>
    internal sealed class ContextDeclaration : Statement, INamedStatement
    {
        internal ContextDeclaration(Token name, TypeReference declaredType)
        {
            Name = name;
            DeclaredType = declaredType;
        }

        public Token Name { get; }

        public TypeReference DeclaredType { get; }

        public bool IsTyped => DeclaredType != null;

        public override void Accept(IAstVisitor visitor)
        {
            visitor.AcceptContextDeclaration(this);
        }
    }
}
