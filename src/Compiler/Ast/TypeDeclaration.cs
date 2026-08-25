using System;
using System.Collections.Generic;
using AuroraScript.Compiler.Ast.Statements;

namespace AuroraScript.Compiler.Ast
{
    /// <summary>
    /// A named compile-time shape used to derive native field facts.
    /// It is not a runtime-checked object contract.
    /// </summary>
    internal sealed class TypeDeclaration : Statement, INamedStatement
    {
        internal TypeDeclaration(
            MemberAccess access,
            Token name,
            IReadOnlyList<TypeFieldDeclaration> fields)
        {
            Access = access;
            Name = name;
            Fields = fields ?? Array.Empty<TypeFieldDeclaration>();
            for (var i = 0; i < Fields.Count; i++)
            {
                Fields[i].Parent = this;
            }
        }

        public MemberAccess Access { get; }

        public Token Name { get; }

        public IReadOnlyList<TypeFieldDeclaration> Fields { get; }

        public override void Accept(IAstVisitor visitor)
        {
            visitor.AcceptTypeDeclaration(this);
        }
    }

    internal sealed class TypeFieldDeclaration : AstNode, INamedStatement
    {
        internal TypeFieldDeclaration(TypeReference type, Token name)
        {
            Type = type;
            Name = name;
        }

        public TypeReference Type { get; }

        public Token Name { get; }

        public override void Accept(IAstVisitor visitor)
        {
            visitor.AcceptTypeFieldDeclaration(this);
        }
    }
}
