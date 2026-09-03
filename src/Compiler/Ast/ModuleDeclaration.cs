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
        private List<TypeDeclaration> _types;
        private Dictionary<string, TypeDeclaration> _typesByName;
        private List<AmbientDeclaration> _ambientDeclarations;
        private Dictionary<string, AmbientDeclaration> _ambientDeclarationsByName;
        private List<ContextDeclaration> _contexts;
        private Dictionary<string, ContextDeclaration> _contextsByName;

        public IReadOnlyList<ImportDeclaration> Imports => _imports ?? (IReadOnlyList<ImportDeclaration>)Array.Empty<ImportDeclaration>();

        public IReadOnlyList<TypeDeclaration> Types =>
            _types ?? (IReadOnlyList<TypeDeclaration>)Array.Empty<TypeDeclaration>();

        public IReadOnlyList<AmbientDeclaration> AmbientDeclarations =>
            _ambientDeclarations ?? (IReadOnlyList<AmbientDeclaration>)Array.Empty<AmbientDeclaration>();

        public IReadOnlyList<ContextDeclaration> Contexts =>
            _contexts ?? (IReadOnlyList<ContextDeclaration>)Array.Empty<ContextDeclaration>();


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

        public bool AddType(TypeDeclaration declaration)
        {
            ArgumentNullException.ThrowIfNull(declaration);
            _typesByName ??= new Dictionary<string, TypeDeclaration>(StringComparer.Ordinal);
            if (!_typesByName.TryAdd(declaration.Name.Value, declaration))
            {
                return false;
            }
            _types ??= new List<TypeDeclaration>();
            _types.Add(declaration);
            AttachParent(declaration, this);
            return true;
        }

        public bool TryGetType(string name, out TypeDeclaration declaration)
        {
            if (_typesByName != null)
            {
                return _typesByName.TryGetValue(name, out declaration);
            }
            declaration = null;
            return false;
        }

        public bool AddAmbientDeclaration(AmbientDeclaration declaration)
        {
            ArgumentNullException.ThrowIfNull(declaration);
            _ambientDeclarationsByName ??= new Dictionary<string, AmbientDeclaration>(StringComparer.Ordinal);
            if (!_ambientDeclarationsByName.TryAdd(declaration.Name.Value, declaration))
            {
                return false;
            }
            _ambientDeclarations ??= new List<AmbientDeclaration>();
            _ambientDeclarations.Add(declaration);
            AttachParent(declaration, this);
            return true;
        }

        public bool TryResolveType(
            TypeReference reference,
            out TypeDeclaration declaration)
        {
            declaration = null;
            if (reference == null)
            {
                return false;
            }
            if (reference.Qualifier == null)
            {
                return TryGetType(reference.Name, out declaration);
            }

            for (var i = 0; i < Imports.Count; i++)
            {
                var import = Imports[i];
                if (import.Include ||
                    import.Name == null ||
                    !StringComparer.Ordinal.Equals(
                        import.Name.Value,
                        reference.QualifierName) ||
                    import.Module == null ||
                    !import.Module.TryGetType(reference.Name, out declaration))
                {
                    continue;
                }
                if (declaration.Access == MemberAccess.Export)
                {
                    return true;
                }
                declaration = null;
                return false;
            }
            return false;
        }



        public bool AddContext(ContextDeclaration declaration)
        {
            ArgumentNullException.ThrowIfNull(declaration);
            _contextsByName ??= new Dictionary<string, ContextDeclaration>(StringComparer.Ordinal);
            if (!_contextsByName.TryAdd(declaration.Name.Value, declaration))
            {
                return false;
            }
            _contexts ??= new List<ContextDeclaration>();
            _contexts.Add(declaration);
            AttachParent(declaration, this);
            return true;
        }

        public bool TryGetContext(string name, out ContextDeclaration declaration)
        {
            if (_contextsByName != null)
            {
                return _contextsByName.TryGetValue(name, out declaration);
            }
            declaration = null;
            return false;
        }

        public Boolean IsEmpty()
        {
            return Functions.Count == 0 &&
                Statements.Count == 0 &&
                Types.Count == 0 &&
                AmbientDeclarations.Count == 0 &&
                Contexts.Count == 0;
        }
    }

}
