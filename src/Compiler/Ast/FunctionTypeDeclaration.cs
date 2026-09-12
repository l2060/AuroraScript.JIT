using AuroraScript.Compiler.Ast.Expressions;
using AuroraScript.Compiler.Ast.Statements;
using System;
using System.Collections.Generic;

namespace AuroraScript.Compiler.Ast
{
    /// <summary>
    /// Declares a compile-time callable contract. Function types do not create
    /// runtime values; <c>func</c> declarations remain the only function bodies.
    /// </summary>
    internal sealed class FunctionTypeDeclaration : Statement, INamedStatement
    {
        internal FunctionTypeDeclaration(
            MemberAccess access,
            Token name,
            IReadOnlyList<ParameterDeclaration> parameters,
            TypeReference returnType,
            bool isDeclare = false)
        {
            Access = access;
            Name = name ?? throw new ArgumentNullException(nameof(name));
            Parameters = parameters ?? Array.Empty<ParameterDeclaration>();
            ReturnType = returnType;
            IsDeclare = isDeclare;
            for (var i = 0; i < Parameters.Count; i++)
            {
                Parameters[i].Parent = this;
            }
        }

        public MemberAccess Access { get; }

        public Token Name { get; }

        public IReadOnlyList<ParameterDeclaration> Parameters { get; }

        /// <summary>
        /// A missing return type makes this a weak callable contract.
        /// </summary>
        public TypeReference ReturnType { get; }

        public bool IsDeclare { get; }

        public bool IsStrong =>
            ReturnType != null ||
            HasTypedParameter();

        private bool HasTypedParameter()
        {
            for (var i = 0; i < Parameters.Count; i++)
            {
                if (Parameters[i].DeclaredType != null)
                {
                    return true;
                }
            }
            return false;
        }

        public override void Accept(IAstVisitor visitor)
        {
            visitor.AcceptFunctionTypeDeclaration(this);
        }
    }
}
