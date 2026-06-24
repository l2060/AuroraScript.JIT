using System;
using System.Collections.Generic;

namespace AuroraScript.Compiler.Ast
{
    internal sealed class FunctionAnnotation : AstNode
    {
        internal FunctionAnnotation(Token name, IReadOnlyList<Token> arguments)
        {
            Name = name;
            Arguments = arguments ?? Array.Empty<Token>();
        }

        public Token Name { get; }
        public IReadOnlyList<Token> Arguments { get; }

        public override void Accept(IAstVisitor visitor)
        {
        }
    }
}
