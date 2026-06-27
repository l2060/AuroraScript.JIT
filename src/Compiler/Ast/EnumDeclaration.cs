using AuroraScript.Compiler.Ast.Statements;
using System;
using System.Collections.Generic;

namespace AuroraScript.Compiler.Ast
{
    internal class EnumElement : AstNode
    {
        public Token Name;
        public Int32 Value;

        public override void Accept(IAstVisitor visitor)
        {
        }
    }

    internal class EnumDeclaration : Statement
    {
        internal EnumDeclaration()
        {
            //this.Access = Symbols.KW_INTERNAL;
        }

        /// <summary>
        /// Function Access
        /// </summary>
        public MemberAccess Access { get; set; }

        public Token Identifier { get; set; }
        public List<EnumElement> Elements { get; set; }

        public override void Accept(IAstVisitor visitor)
        {
            visitor.AcceptEnumDeclaration(this);
        }

        override public string ToString()
        {
            return $"EnumDeclaration: {Identifier?.Value}";
        }
    }
}