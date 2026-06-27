using AuroraScript.LanguageServices.Builtins;
using System;
using System.IO;
using Xunit;

namespace AuroraScript.LanguageServices.Tests;

public sealed class BuiltinApiCatalogTests
{
    [Fact]
    public void LoadsRuntimeApiMetadata()
    {
        var catalog = LoadCatalog();

        Assert.True(catalog.TryGetGlobal("Math", out var math));
        Assert.Equal(BuiltinApiKind.Object, math.Kind);
        Assert.True(math.TryGetMember("abs", out var abs));
        Assert.Equal(BuiltinApiKind.Method, abs.Kind);
        Assert.Equal("number", abs.ReturnType);
        Assert.True(abs.ReadOnly);
        var parameter = Assert.Single(abs.Parameters);
        Assert.Equal("value", parameter.Name);
        Assert.Equal("number", parameter.Type);
    }

    [Fact]
    public void LoadsPrototypeMembers()
    {
        var catalog = LoadCatalog();

        Assert.True(catalog.TryGetPrototypeMember("String", "substring", out var substring));
        Assert.Equal(BuiltinApiKind.Method, substring.Kind);
        Assert.Equal("string", substring.ReturnType);
        Assert.Equal(2, substring.Parameters.Count);
        Assert.True(substring.Parameters[1].Optional);
    }

    private static BuiltinApiCatalog LoadCatalog()
    {
        return BuiltinApiLoader.LoadFromFile(GetRuntimeApiPath());
    }

    internal static string GetRuntimeApiPath()
    {
        var directory = AppContext.BaseDirectory;
        for (var i = 0; i < 8; i++)
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

        throw new FileNotFoundException("runtime-api.json was not found from test output path.", directory);
    }
}
