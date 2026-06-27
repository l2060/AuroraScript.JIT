using AuroraScript.LanguageServices;
using AuroraScript.LanguageServices.Builtins;
using System;
using System.IO;
using System.Reflection;

namespace AuroraScript.LanguageServer;

public static class AuroraLanguageServerFactory
{
    internal const string RuntimeApiResourceName = "AuroraScript.LanguageServer.Resources.runtime-api.json";

    public static AuroraLanguageServer CreateDefault()
    {
        var catalog = LoadDefaultBuiltinCatalog();
        return new AuroraLanguageServer(new AuroraLanguageService(new AuroraLanguageServiceOptions(catalog)
        {
            IndexWorkspaceFiles = true
        }));
    }

    public static AuroraLanguageServer Create(AuroraLanguageServiceOptions options)
    {
        return new AuroraLanguageServer(new AuroraLanguageService(options));
    }

    internal static BuiltinApiCatalog LoadDefaultBuiltinCatalog()
    {
        return LoadDefaultBuiltinCatalog(FindRuntimeApiPathOrDefault);
    }

    internal static BuiltinApiCatalog LoadDefaultBuiltinCatalog(Func<string?> findRuntimeApiPath)
    {
        ArgumentNullException.ThrowIfNull(findRuntimeApiPath);
        var runtimeApiPath = findRuntimeApiPath();
        if (runtimeApiPath != null)
        {
            return BuiltinApiLoader.LoadFromFile(runtimeApiPath);
        }

        using var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(RuntimeApiResourceName);
        if (stream == null)
        {
            throw new FileNotFoundException(
                "Unable to locate .language/schema/runtime-api.json or embedded runtime API metadata.",
                RuntimeApiResourceName);
        }

        return BuiltinApiLoader.Load(stream);
    }

    internal static string FindRuntimeApiPath()
    {
        return FindRuntimeApiPathOrDefault() ??
            throw new FileNotFoundException("Unable to locate .language/schema/runtime-api.json.");
    }

    internal static string? FindRuntimeApiPathOrDefault()
    {
        var directory = AppContext.BaseDirectory;
        for (var i = 0; i < 10; i++)
        {
            var candidate = Path.GetFullPath(Path.Combine(directory, ".language", "schema", "runtime-api.json"));
            if (File.Exists(candidate))
            {
                return candidate;
            }

            var parent = Directory.GetParent(directory);
            if (parent == null)
            {
                break;
            }

            directory = parent.FullName;
        }

        var fromCurrent = Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), ".language", "schema", "runtime-api.json"));
        if (File.Exists(fromCurrent))
        {
            return fromCurrent;
        }

        return null;
    }
}
