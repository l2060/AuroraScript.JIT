using AuroraScript.Compiler.Ast;
using AuroraScript.Compiler.Backend;

namespace AuroraScript.Compiler.Backend.Code
{
    /// <summary>
    /// Resolves source contracts into the coarse facts used by the typed
    /// backend. Custom types stay Object facts; their fields supply native
    /// Number/Boolean/packed-array proofs at compile time. Host NativeType
    /// names resolve through the engine catalog.
    /// </summary>
    internal static class TypeReferenceFacts
    {
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

        public static bool IsVoid(TypeReference type)
        {
            return type != null &&
                type.Qualifier == null &&
                string.Equals(type.Name, "void", System.StringComparison.Ordinal);
        }
    }
}
