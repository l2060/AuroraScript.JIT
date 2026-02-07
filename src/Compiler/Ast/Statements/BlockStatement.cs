using System;
using System.Collections.Generic;


namespace AuroraScript.Compiler.Ast.Statements
{
    internal class BlockStatement : Statement
    {
        public Boolean IsFunction { get; set; }

        public readonly List<FunctionDeclaration> Functions = new List<FunctionDeclaration>();

        internal BlockStatement()
        {
        }

        public override void Accept(IAstVisitor visitor)
        {
            visitor.AcceptBlock(this);
        }
    }
}