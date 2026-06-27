using AuroraScript.LanguageServices;
using AuroraScript.LanguageServices.Builtins;
using System;
using System.IO;

namespace AuroraScript.LanguageServer;

public static class AuroraLanguageServerFactory
{
    public static AuroraLanguageServer CreateDefault()
    {
        var catalog = BuiltinApiLoader.LoadFromFile(FindRuntimeApiPath());
        return new AuroraLanguageServer(new AuroraLanguageService(catalog));
    }

    public static AuroraLanguageServer Create(AuroraLanguageServiceOptions options)
    {
        return new AuroraLanguageServer(new AuroraLanguageService(options));
    }

    internal static string FindRuntimeApiPath()
    {
        var directory = AppContext.BaseDirectory;
        for (var i = 0; i < 10; i++)
        {
            var candidate = Path.GetFullPath(Path.Combine(directory, "ai-language-pack", "schema", "runtime-api.json"));
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

        var fromCurrent = Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), "ai-language-pack", "schema", "runtime-api.json"));
        if (File.Exists(fromCurrent))
        {
            return fromCurrent;
        }

        throw new FileNotFoundException("Unable to locate ai-language-pack/schema/runtime-api.json.");
    }
}
