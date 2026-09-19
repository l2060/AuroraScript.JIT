using AuroraScript.Compiler.Ast;
using AuroraScript.Compiler.Ast.Expressions;
using AuroraScript.Compiler.Backend;
using AuroraScript.Compiler.Backend.Plans;
using System;
using System.Collections.Generic;

namespace AuroraScript.Compiler.Backend.Code
{
    /// <summary>
    /// Resolves source contracts into the coarse facts used by the typed
    /// backend. Custom types stay Object facts; their fields supply native
    /// Number/Boolean/packed-array facts after a checked boundary. Host NativeType
    /// names resolve through the engine catalog.
    /// </summary>
    internal static class TypeReferenceFacts
    {
        public static bool TryGetCallableType(
            ModuleDeclaration module,
            FunctionPlan function,
            IReadOnlyDictionary<NameExpression, BoundName> names,
            Expression expression,
            out FunctionTypeDeclaration declaration,
            out ModuleDeclaration declarationModule)
        {
            declaration = null;
            declarationModule = null;
            while (expression is GroupExpression group && group.Expressions.Count == 1)
                expression = group.Expressions[0];
            if (expression is not NameExpression name ||
                !names.TryGetValue(name, out var binding) || !binding.IsLocal ||
                (uint)binding.Local.Value >= (uint)function.LocalSlots.Length ||
                function.LocalSlots[binding.Local.Value].Declaration is not ParameterDeclaration parameter ||
                !TryGetFunctionType(module, parameter.DeclaredType, out declaration))
                return false;
            declarationModule = declaration.Parent as ModuleDeclaration ?? module;
            return true;
        }

        public static FlowValueType GetFlowType(
            ModuleDeclaration module,
            TypeReference type,
            HostExportCatalog hostExports = null)
        {
            if (type == null)
            {
                return FlowValueType.None;
            }

            if (IsVoid(type))
            {
                return FlowValueType.Null;
            }

            var builtin = FlowValueTypeFacts.FromCheckedTypeName(type.Name);
            if (builtin != FlowValueType.None)
            {
                return builtin;
            }

            if (TryGetNativeObject(hostExports, type, out _))
            {
                return FlowValueType.Object;
            }

            if (TryGetClrType(hostExports, type, out _))
            {
                return FlowValueType.Object;
            }

            if (TryGetFunctionType(module, type, out _))
            {
                return FlowValueType.Object;
            }

            return module != null && module.TryResolveType(type, out _)
                ? FlowValueType.Object
                : FlowValueType.None;
        }

        public static bool TryGetCustomType(
            ModuleDeclaration module,
            TypeReference type,
            out TypeDeclaration declaration)
        {
            declaration = null;
            return type != null &&
                module != null &&
                module.TryResolveType(type, out declaration);
        }

        public static bool TryGetFunctionType(
            ModuleDeclaration module,
            TypeReference type,
            out FunctionTypeDeclaration declaration)
        {
            declaration = null;
            return type != null &&
                module != null &&
                module.TryResolveFunctionType(type, out declaration);
        }

        public static bool TryGetNativeObject(
            HostExportCatalog hostExports,
            TypeReference type,
            out HostNativeObjectDescriptor descriptor)
        {
            descriptor = null;
            return hostExports != null &&
                type != null &&
                type.Qualifier == null &&
                hostExports.TryGetNativeObject(type.Name, out descriptor);
        }

        public static bool TryGetClrType(
            HostExportCatalog hostExports,
            TypeReference type,
            out Type clrType)
        {
            clrType = null;
            return hostExports != null &&
                type != null &&
                type.Qualifier == null &&
                hostExports.ClrTypes.TryGetType(type.Name, 0, out clrType);
        }

        public static bool IsVoid(TypeReference type)
        {
            return type != null &&
                type.Qualifier == null &&
                string.Equals(type.Name, "void", System.StringComparison.Ordinal);
        }
    }
}
