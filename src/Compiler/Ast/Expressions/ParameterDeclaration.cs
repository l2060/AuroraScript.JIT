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
        /// Exact runtime type required at the function boundary, or null when
        /// the parameter remains dynamically typed.
        /// </summary>
        public string CheckedTypeName { get; set; }

        public Token CheckedTypeToken { get; set; }

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