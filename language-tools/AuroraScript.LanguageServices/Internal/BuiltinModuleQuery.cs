using AuroraScript.Compiler.Ast;
using AuroraScript.LanguageServices.Builtins;
using System;

namespace AuroraScript.LanguageServices.Internal;

internal static class BuiltinModuleQuery
{
    public static bool TryResolve(
        BuiltinApiCatalog builtins,
        ModuleDeclaration? declaration,
        string alias,
        out BuiltinApiModule module)
    {
        module = null!;
        if (declaration == null || string.IsNullOrEmpty(alias))
        {
            return false;
        }

        for (var i = 0; i < declaration.Imports.Count; i++)
        {
            var import = declaration.Imports[i];
            if (import.Include ||
                !string.Equals(import.Name?.Value, alias, StringComparison.Ordinal))
            {
                continue;
            }

            var modulePath = import.File?.Value;
            return !string.IsNullOrEmpty(modulePath) &&
                builtins.TryGetModule(modulePath, out module);
        }

        return false;
    }

    public static bool IsImportedName(ModuleDeclaration? declaration, string name)
    {
        if (declaration == null || string.IsNullOrEmpty(name))
        {
            return false;
        }

        for (var i = 0; i < declaration.Imports.Count; i++)
        {
            var import = declaration.Imports[i];
            if (!import.Include &&
                string.Equals(import.Name?.Value, name, StringComparison.Ordinal))
            {
                return true;
            }
        }

        return false;
    }
}
