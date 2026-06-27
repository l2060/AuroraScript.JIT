using AuroraScript.Compiler.Ast.Statements;
using System;
using System.Collections.Generic;

namespace AuroraScript.Compiler.Ast
{
    internal class ModuleDeclaration : BlockStatement
    {
        public readonly String Directory;


        /// <summary>
        /// 模块元信息，包括模块名， 脚本中使用 @metaname(value?)定义
        /// </summary>
        public Dictionary<String, Object> MetaInfos = new Dictionary<string, object>();

        private List<ImportDeclaration> _imports;

        public IReadOnlyList<ImportDeclaration> Imports => _imports ?? (IReadOnlyList<ImportDeclaration>)Array.Empty<ImportDeclaration>();


        internal ModuleDeclaration(String directory)
        {
            Directory = directory;
        }


        /// <summary>
        /// Gets or sets the name of the module associated with this instance.
        /// </summary>
        public String ModuleName { get; set; }


        /// <summary>
        /// Gets or sets the file system path to the module associated with this instance.
        /// </summary>
        public String ModulePath { get; set; }

        /// <summary>
        /// Gets or sets the fully qualified path of the file or directory.
        /// </summary>
        public String FullPath { get; set; }

        public override void Accept(IAstVisitor visitor)
        {
            visitor.AcceptModule(this);
        }

        public void AddImport(ImportDeclaration import)
        {
            _imports ??= new List<ImportDeclaration>();
            _imports.Add(import);
        }



        public Boolean IsEmpty()
        {
            return Functions.Count == 0 && Length == 0;
        }



        public override string ToString()
        {
            return $"ModuleDeclaration: {ModuleName}";
        }
    }

}
