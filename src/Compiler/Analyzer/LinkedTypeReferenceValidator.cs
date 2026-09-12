using AuroraScript.Compiler.Ast;
using AuroraScript.Compiler.Ast.Expressions;
using AuroraScript.Compiler.Backend;
using AuroraScript.Compiler.Backend.Code;
using AuroraScript.Core;
using AuroraScript.Runtime;
using System;
using System.Collections.Generic;

namespace AuroraScript.Compiler.Analyzer
{
    internal sealed class LinkedTypeReferenceValidator : IAstVisitor
    {
        private readonly ModuleDeclaration _module;
        private readonly HostExportCatalog _hostExports;

        private LinkedTypeReferenceValidator(
            ModuleDeclaration module,
            HostExportCatalog hostExports)
        {
            _module = module ?? throw new ArgumentNullException(nameof(module));
            _hostExports = hostExports ?? throw new ArgumentNullException(nameof(hostExports));
        }

        public static void Validate(
            IReadOnlyList<ModuleDeclaration> modules,
            IReadOnlyList<Type> nativeTypes,
            RuntimeOptions runtimeOptions = null)
        {
            ArgumentNullException.ThrowIfNull(modules);
            var hostExports = new HostExportCatalog(
                nativeTypes ?? Array.Empty<Type>(),
                Array.Empty<NativePackageDefinition>(),
                runtimeOptions ?? RuntimeOptions.Default);
            for (var i = 0; i < modules.Count; i++)
            {
                new LinkedTypeReferenceValidator(modules[i], hostExports).Apply();
            }
        }

        private void Apply()
        {
            _module.Accept(this);
        }

        protected override void VisitFunction(FunctionDeclaration node)
        {
            if (!TypeReferenceFacts.IsVoid(node.ReturnType))
            {
                ValidateReference(node.ReturnType);
            }
            for (var i = 0; i < node.Parameters.Count; i++)
            {
                ValidateReference(node.Parameters[i].DeclaredType);
            }
            base.VisitFunction(node);
        }

        protected override void VisitFunctionTypeDeclaration(
            FunctionTypeDeclaration node)
        {
            if (!TypeReferenceFacts.IsVoid(node.ReturnType))
            {
                ValidateReference(node.ReturnType);
            }
            for (var i = 0; i < node.Parameters.Count; i++)
            {
                ValidateReference(node.Parameters[i].DeclaredType);
            }
        }

        protected override void VisitCheckExpression(CheckExpression node)
        {
            ValidateReference(node.AssertedType);
            base.VisitCheckExpression(node);
        }

        protected override void VisitTypeFieldDeclaration(
            TypeFieldDeclaration node)
        {
            ValidateReference(node.Type);
            base.VisitTypeFieldDeclaration(node);
        }

        protected override void VisitContextDeclaration(ContextDeclaration node)
        {
            if (node.DeclaredType == null)
            {
                return;
            }

            if (!TypeReferenceFacts.TryGetNativeObject(
                _hostExports,
                node.DeclaredType,
                out _))
            {
                throw new AuroraCompilationException(
                    AuroraCompilationStage.Linking,
                    _module.Source.FullPath,
                    node.DeclaredType.Token,
                    $"Context '{node.Name.Value}' requires a host NativeType, not '{node.DeclaredType.DisplayName}'.");
            }

            base.VisitContextDeclaration(node);
        }

        protected override void VisitGetPropertyExpression(GetPropertyExpression node)
        {
            RejectTypeUsedAsValue(node.Object, node.Property);
            base.VisitGetPropertyExpression(node);
        }

        protected override void VisitSetPropertyExpression(SetPropertyExpression node)
        {
            RejectTypeUsedAsValue(node.Object, node.Property);
            base.VisitSetPropertyExpression(node);
        }

        protected override void VisitName(NameExpression node)
        {
            if ((_module.TryGetFunctionType(
                        node.Identifier.Value,
                        out var declaration) ||
                    _module.TryGetAmbientFunctionType(
                        node.Identifier.Value,
                        out declaration)) &&
                !HasValueExport(_module, node.Identifier.Value))
            {
                throw new AuroraCompilationException(
                    AuroraCompilationStage.Linking,
                    _module.Source.FullPath,
                    node.Identifier,
                    $"Function type '{declaration.Name.Value}' is compile-time only and cannot be used as a value.");
            }
            base.VisitName(node);
        }

        private void ValidateReference(TypeReference reference)
        {
            if (reference == null ||
                IsBuiltin(reference) ||
                TypeReferenceFacts.TryGetNativeObject(_hostExports, reference, out _) ||
                TypeReferenceFacts.TryGetClrType(_hostExports, reference, out _) ||
                _module.TryResolveFunctionType(reference, out _) ||
                _module.TryResolveType(reference, out _))
            {
                return;
            }

            throw new AuroraCompilationException(
                AuroraCompilationStage.Linking,
                _module.Source.FullPath,
                reference.Qualifier ?? reference.Token,
                $"Unknown or inaccessible type '{reference.DisplayName}'.");
        }

        private void RejectTypeUsedAsValue(Expression owner, Expression property)
        {
            if (owner is not NameExpression alias ||
                property is not NameExpression member)
            {
                return;
            }

            var imported = FindImportedModule(alias.Identifier.Value);
            if (imported == null)
            {
                return;
            }
            var isExportedType =
                imported.TryGetType(member.Identifier.Value, out var declaration) &&
                declaration.Access == MemberAccess.Export;
            var isExportedFunctionType =
                imported.TryGetFunctionType(
                    member.Identifier.Value,
                    out var functionType) &&
                functionType.Access == MemberAccess.Export;
            if ((!isExportedType && !isExportedFunctionType) ||
                HasValueExport(imported, member.Identifier.Value))
            {
                return;
            }

            throw new AuroraCompilationException(
                AuroraCompilationStage.Linking,
                _module.Source.FullPath,
                member.Identifier,
                $"Type '{alias.Identifier.Value}.{member.Identifier.Value}' is compile-time only and cannot be used as a value.");
        }

        private ModuleDeclaration FindImportedModule(string alias)
        {
            var imports = _module.Imports;
            for (var i = 0; i < imports.Count; i++)
            {
                var import = imports[i];
                if (import.Include ||
                    import.Name == null ||
                    import.Module == null ||
                    !StringComparer.Ordinal.Equals(import.Name.Value, alias))
                {
                    continue;
                }

                return import.Module;
            }

            return null;
        }

        private static bool HasValueExport(ModuleDeclaration module, string name)
        {
            for (var i = 0; i < module.Functions.Count; i++)
            {
                var function = module.Functions[i];
                if (function.Access == MemberAccess.Export &&
                    function.Flags != FunctionFlags.Declare &&
                    function.Name != null &&
                    StringComparer.Ordinal.Equals(function.Name.Value, name))
                {
                    return true;
                }
            }

            for (var i = 0; i < module.Statements.Count; i++)
            {
                switch (module.Statements[i])
                {
                    case VariableDeclaration variable
                        when variable.Access == MemberAccess.Export &&
                            variable.Name != null &&
                            StringComparer.Ordinal.Equals(variable.Name.Value, name):
                        return true;
                    case EnumDeclaration enumeration
                        when enumeration.Access == MemberAccess.Export &&
                            enumeration.Identifier != null &&
                            StringComparer.Ordinal.Equals(enumeration.Identifier.Value, name):
                        return true;
                }
            }

            return false;
        }

        private static bool IsBuiltin(TypeReference reference)
        {
            return reference.Qualifier == null &&
                FlowValueTypeFacts.IsCheckedTypeName(reference.Name);
        }

    }
}
