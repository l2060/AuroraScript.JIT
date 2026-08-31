using System;
using System.Collections.Generic;
using AuroraScript.Compiler.Ast.Expressions;
using AuroraScript.Compiler.Ast.Statements;

namespace AuroraScript.Compiler.Ast
{
    internal enum AmbientDeclarationKind
    {
        Type
    }

    internal enum AmbientMemberKind
    {
        Const,
        Var,
        Function,
        Constructor
    }

    /// <summary>
    /// A compile-time description of a host-provided object. Ambient declarations
    /// are deliberately separate from structural type declarations.
    /// </summary>
    internal sealed class AmbientDeclaration : Statement, INamedStatement
    {
        internal AmbientDeclaration(
            AmbientDeclarationKind kind,
            Token name,
            IReadOnlyList<AmbientMemberDeclaration> members)
        {
            Kind = kind;
            Name = name;
            Members = members ?? Array.Empty<AmbientMemberDeclaration>();
            for (var i = 0; i < Members.Count; i++)
            {
                Members[i].Parent = this;
            }
        }

        public AmbientDeclarationKind Kind { get; }
        public Token Name { get; }
        public IReadOnlyList<AmbientMemberDeclaration> Members { get; }

        public override void Accept(IAstVisitor visitor)
        {
            visitor.AcceptAmbientDeclaration(this);
        }
    }

    internal sealed class AmbientMemberDeclaration : AstNode, INamedStatement
    {
        internal AmbientMemberDeclaration(
            AmbientMemberKind kind,
            Token name,
            bool isStatic,
            IReadOnlyList<ParameterDeclaration> parameters = null,
            TypeReference returnType = null)
        {
            Kind = kind;
            Name = name;
            IsStatic = isStatic;
            Parameters = parameters ?? Array.Empty<ParameterDeclaration>();
            ReturnType = returnType;
            for (var i = 0; i < Parameters.Count; i++)
            {
                Parameters[i].Parent = this;
            }
        }

        public AmbientMemberKind Kind { get; }
        public Token Name { get; }
        public bool IsStatic { get; }
        public IReadOnlyList<ParameterDeclaration> Parameters { get; }
        public TypeReference ReturnType { get; }

        public override void Accept(IAstVisitor visitor)
        {
            visitor.AcceptAmbientMemberDeclaration(this);
        }
    }
}
