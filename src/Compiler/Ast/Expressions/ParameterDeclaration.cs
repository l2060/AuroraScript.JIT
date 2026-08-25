using System;


namespace AuroraScript.Compiler.Ast.Expressions
{
    /// <summary>
    /// function parameter declaration
    /// </summary>
    internal class ParameterDeclaration : VariableDeclaration
    {
        internal ParameterDeclaration(Byte index, Token name, Expression initializer) : base(MemberAccess.Internal, false, name, initializer)
        {
            Name = name;
            Index = index;
        }

        public Byte Index { get; set; }

        /// <summary>
        /// Optional source-level type contract at the function boundary.
        /// </summary>
        public TypeReference DeclaredType { get; set; }

        public string CheckedTypeName => DeclaredType?.Name;

        public Token CheckedTypeToken => DeclaredType?.Token;

        /// <summary>
        /// 扩展运算符（Spread Operator）
        /// </summary>
        public Boolean IsSpreadOperator { get; set; } = false;

        public override void Accept(IAstVisitor visitor)
        {
            visitor.AcceptParameterDeclaration(this);
        }
    }
}