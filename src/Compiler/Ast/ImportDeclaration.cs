using AuroraScript.Compiler.Ast.Statements;
using System;
using System.Collections.Generic;


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
        /// 模块名
        /// </summary>
        public String ModuleName { get; set; }

        /// <summary>
        /// 模块相对于根目录的路径
        /// </summary>
        public String ModulePath { get; set; }

        /// <summary>
        /// 模块路径
        /// </summary>
        public String FullPath { get; set; }

        public ModuleDeclaration Module { get; set; }



        public Boolean Include { get; set; }


        public override IEnumerable<AstNode> ChildNodes
        {
            get { return base.ChildNodes; }
        }

        public override void Accept(IAstVisitor visitor)
        {
            visitor.AcceptImportDeclaration(this);
        }

        override public string ToString()
        {
            if (Include)
            {
                return $"ImportDeclaration: {FullPath}";
            }
            else
            {
                return $"ImportDeclaration: {Name.Value} {FullPath}";
            }

        }
    }
}
