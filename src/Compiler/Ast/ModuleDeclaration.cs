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
        private List<FunctionTypeDeclaration> _functionTypes;
        private Dictionary<string, FunctionTypeDeclaration> _functionTypesByName;
        private List<TypeDeclaration> _types;
        private Dictionary<string, TypeDeclaration> _typesByName;
        private List<AmbientDeclaration> _ambientDeclarations;
        private Dictionary<string, AmbientDeclaration> _ambientDeclarationsByName;
        private List<ContextDeclaration> _contexts;
        private Dictionary<string, ContextDeclaration> _contextsByName;
        private IReadOnlyDictionary<string, FunctionTypeDeclaration> _ambientFunctionTypes;

        public IReadOnlyList<ImportDeclaration> Imports => _imports ?? (IReadOnlyList<ImportDeclaration>)Array.Empty<ImportDeclaration>();

        public IReadOnlyList<FunctionTypeDeclaration> FunctionTypes =>
            _functionTypes ??
            (IReadOnlyList<FunctionTypeDeclaration>)Array.Empty<FunctionTypeDeclaration>();

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

        public bool AddFunctionType(FunctionTypeDeclaration declaration)
        {
            ArgumentNullException.ThrowIfNull(declaration);
            if (_typesByName != null &&
                _typesByName.ContainsKey(declaration.Name.Value))
            {
                return false;
            }
            _functionTypesByName ??=
                new Dictionary<string, FunctionTypeDeclaration>(
                    StringComparer.Ordinal);
            if (!_functionTypesByName.TryAdd(
                    declaration.Name.Value,
                    declaration))
            {
                return false;
            }
            _functionTypes ??= new List<FunctionTypeDeclaration>();
            _functionTypes.Add(declaration);
            AttachParent(declaration, this);
            return true;
        }

        public bool TryGetFunctionType(
            string name,
            out FunctionTypeDeclaration declaration)
        {
            if (_functionTypesByName != null)
            {
                return _functionTypesByName.TryGetValue(name, out declaration);
            }
            declaration = null;
            return false;
        }

        /// <summary>
        /// 绑定 @global() 声明文件中的方法类型（declare type Name(...)）。
        /// </summary>
        public void SetAmbientFunctionTypes(
            IReadOnlyDictionary<string, FunctionTypeDeclaration> declarations)
        {
            _ambientFunctionTypes = declarations != null && declarations.Count != 0
                ? declarations
                : null;
        }

        public bool TryGetAmbientFunctionType(
            string name,
            out FunctionTypeDeclaration declaration)
        {
            if (_ambientFunctionTypes != null && name != null)
            {
                return _ambientFunctionTypes.TryGetValue(name, out declaration);
            }
            declaration = null;
            return false;
        }

        public bool TryResolveFunctionType(
            TypeReference reference,
            out FunctionTypeDeclaration declaration)
        {
            declaration = null;
            if (reference == null)
            {
                return false;
            }
            if (reference.Qualifier == null)
            {
                return TryGetFunctionType(reference.Name, out declaration) ||
                    TryGetAmbientFunctionType(reference.Name, out declaration);
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
                    !import.Module.TryGetFunctionType(
                        reference.Name,
                        out declaration))
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

        public bool AddType(TypeDeclaration declaration)
        {
            ArgumentNullException.ThrowIfNull(declaration);
            if (_functionTypesByName != null &&
                _functionTypesByName.ContainsKey(
                    declaration.Name.Value))
            {
                return false;
            }
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
                FunctionTypes.Count == 0 &&
                Types.Count == 0 &&
                AmbientDeclarations.Count == 0 &&
                Contexts.Count == 0;
        }
    }

}
