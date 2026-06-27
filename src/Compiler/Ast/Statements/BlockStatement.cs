using System;
using System.Collections.Generic;


namespace AuroraScript.Compiler.Ast.Statements
{
    internal class BlockStatement : Statement
    {
        public Boolean IsFunction { get; set; }

        private List<Statement> _statements;
        private List<FunctionDeclaration> _functions;

        public IReadOnlyList<Statement> Statements => _statements ?? (IReadOnlyList<Statement>)Array.Empty<Statement>();
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
            AttachParent(function, this);
        }

        public void AddStatement(Statement statement)
        {
            _statements ??= new List<Statement>();
            _statements.Add(statement);
            AttachParent(statement, this);
        }
    }
}
