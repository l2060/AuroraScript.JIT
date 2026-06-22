using System;
using System.Collections.Generic;


namespace AuroraScript.Compiler.Ast.Statements
{
    internal class BlockStatement : Statement
    {
        public Boolean IsFunction { get; set; }

        private List<FunctionDeclaration> _functions;

        public IReadOnlyList<FunctionDeclaration> Functions => _functions ?? (IReadOnlyList<FunctionDeclaration>)Array.Empty<FunctionDeclaration>();

        internal BlockStatement()
        {
        }

        public override void Accept(IAstVisitor visitor)
        {
            visitor.AcceptBlock(this);
        }

        public void AddFunction(FunctionDeclaration function)
        {
            _functions ??= new List<FunctionDeclaration>();
            _functions.Add(function);
        }
    }
}
