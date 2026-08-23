using AuroraScript.Compiler.Ast.Statements;
using AuroraScript.Core;
using System;
using System.Collections.Generic;

namespace AuroraScript.Compiler.Ast
{
    internal class ModuleDeclaration : BlockStatement
    {
        public readonly ScriptSourceReference Source;


        /// <summary>
        /// 模块元信息，包括模块名， 脚本中使用 @metaname(value?)定义
        /// </summary>
        public Dictionary<String, Object> MetaInfos = new Dictionary<string, object>();

        private List<ImportDeclaration> _imports;

        public IReadOnlyList<ImportDeclaration> Imports => _imports ?? (IReadOnlyList<ImportDeclaration>)Array.Empty<ImportDeclaration>();


        internal ModuleDeclaration(ScriptSourceReference source)
        {
            Source = source;
        }


        /// <summary>
        /// Gets or sets the name of the module associated with this instance.
        /// </summary>
        public String ModuleName { get; set; }


        /// <summary>
        /// Gets or sets whether this file is a compile-time global declaration file.
        /// </summary>
        public Boolean IsGlobalDeclarationFile { get; set; }

        public override void Accept(IAstVisitor visitor)
        {
            visitor.AcceptModule(this);
        }

        public void AddImport(ImportDeclaration import)
        {
            _imports ??= new List<ImportDeclaration>();
            _imports.Add(import);
            AttachParent(import, this);
        }



        public Boolean IsEmpty()
        {
            return Functions.Count == 0 && Statements.Count == 0;
        }
    }

}
