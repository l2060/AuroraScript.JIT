using AuroraScript.Compiler.Ast.Statements;
using AuroraScript.Core;
using System;


namespace AuroraScript.Compiler.Ast
{
    internal class ImportDeclaration : Statement
    {
        internal ImportDeclaration()
        {
        }

        /// <summary>
        /// 导入的模块名称
        /// </summary>
        public Token Name { get; set; }

        /// <summary>
        /// 模块URL
        /// </summary>
        public Token File { get; set; }

        /// <summary>
        /// Resolved source reference for the imported module.
        /// </summary>
        public ScriptSourceReference Reference { get; set; }

        public ModuleDeclaration Module { get; set; }
        public Boolean Include { get; set; }


        public override void Accept(IAstVisitor visitor)
        {
            visitor.AcceptImportDeclaration(this);
        }
    }
}
