using AuroraScript.LanguageServices.Builtins;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
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

    [Fact]
    public void LoadsCompilerProvidedSpecialGlobals()
    {
        var catalog = LoadCatalog();

        Assert.True(catalog.TryGetGlobal("global", out var global));
        Assert.Equal(BuiltinApiKind.Object, global.Kind);
        Assert.True(global.TryGetMember("modules", out var modules));
        Assert.Equal(BuiltinApiKind.Property, modules.Kind);
        Assert.Equal("object", modules.ReturnType);
    }

    [Fact]
    public void LoadsConstructorSignatures()
    {
        var catalog = LoadCatalog();

        Assert.True(catalog.TryGetGlobal("Path", out var path));
        Assert.Equal(BuiltinApiKind.Constructor, path.Kind);
        Assert.False(path.Callable);
        var constructor = Assert.Single(path.Constructors);
        Assert.Equal("Path", constructor.ReturnType);
        Assert.Equal(2, constructor.Parameters.Count);
        Assert.Equal("root", constructor.Parameters[0].Name);
        Assert.Equal("string|Path", constructor.Parameters[0].Type);
        Assert.True(constructor.Parameters[0].Optional);
        Assert.Equal("segments", constructor.Parameters[1].Name);
        Assert.Equal("string|Path", constructor.Parameters[1].Type);
        Assert.True(constructor.Parameters[1].Variadic);

        Assert.True(catalog.TryGetGlobal("String", out var stringConstructor));
        Assert.True(stringConstructor.Callable);
        Assert.Single(stringConstructor.Constructors);
    }

    [Fact]
    public void RuntimeApiCatalogCoversRuntimeRegisteredGlobals()
    {
        var catalog = LoadCatalog();
        var runtimeRoot = GetRuntimeRoot();
        var engineSource = File.ReadAllText(Path.Combine(runtimeRoot, "..", "AuroraEngine.cs"));

        foreach (var name in ExtractDefineNames(engineSource, "Global"))
        {
            Assert.True(catalog.TryGetGlobal(name, out _), $"runtime-api.json is missing global '{name}'.");
        }
    }

    [Fact]
    public void RuntimeApiCatalogCoversRuntimeRegisteredObjectMembers()
    {
        var catalog = LoadCatalog();
        var runtimeRoot = GetRuntimeRoot();
        var registrations = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["console"] = Path.Combine(runtimeRoot, "Extensions", "ConsoleSupport.cs"),
            ["JSON"] = Path.Combine(runtimeRoot, "Extensions", "JsonSupport.cs"),
            ["Math"] = Path.Combine(runtimeRoot, "Extensions", "MathSupport.cs"),
            ["Path"] = Path.Combine(runtimeRoot, "Types", "TypeConstruct", "PathConstructor.cs"),
            ["HotPatch"] = Path.Combine(runtimeRoot, "Extensions", "HotPatchSupport.cs"),
            ["Array"] = Path.Combine(runtimeRoot, "Types", "TypeConstruct", "ArrayConstructor.cs"),
            ["String"] = Path.Combine(runtimeRoot, "Types", "TypeConstruct", "StringConstructor.cs"),
            ["Boolean"] = Path.Combine(runtimeRoot, "Types", "TypeConstruct", "BooleanConstructor.cs"),
            ["Object"] = Path.Combine(runtimeRoot, "Types", "TypeConstruct", "ScriptObjectConstructor.cs"),
            ["Number"] = Path.Combine(runtimeRoot, "Types", "TypeConstruct", "NumberConstructor.cs"),
            ["Date"] = Path.Combine(runtimeRoot, "Types", "TypeConstruct", "ScriptDateConstructor.cs")
        };

        foreach (var registration in registrations)
        {
            Assert.True(catalog.TryGetGlobal(registration.Key, out var global), $"runtime-api.json is missing global '{registration.Key}'.");
            var source = File.ReadAllText(registration.Value);
            foreach (var memberName in ExtractDefineNames(source, null))
            {
                Assert.True(
                    global.TryGetMember(memberName, out _),
                    $"runtime-api.json is missing member '{registration.Key}.{memberName}' from {Path.GetFileName(registration.Value)}.");
            }
        }
    }

    [Fact]
    public void RuntimeApiCatalogCoversRuntimeRegisteredPrototypeMembers()
    {
        var catalog = LoadCatalog();
        var runtimeRoot = GetRuntimeRoot();
        var source = File.ReadAllText(Path.Combine(runtimeRoot, "Types", "Prototypes.cs"));
        var prototypeOwners = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["ObjectPrototype"] = "Object",
            ["BooleanValuePrototype"] = "Boolean",
            ["RegexPrototype"] = "Regex",
            ["HashMapPrototype"] = "HashMap",
            ["DatePrototype"] = "Date",
            ["NumberValuePrototype"] = "Number",
            ["ScriptArrayPrototype"] = "Array",
            ["StringValuePrototype"] = "String",
            ["StringBufferPrototype"] = "StringBuffer",
            ["PathPrototype"] = "Path"
        };

        foreach (Match match in PrototypeDefinePattern.Matches(source))
        {
            var prototypeName = match.Groups["prototype"].Value;
            if (!prototypeOwners.TryGetValue(prototypeName, out var ownerName))
            {
                continue;
            }

            var memberName = match.Groups["name"].Value;
            Assert.True(
                catalog.TryGetPrototypeMember(ownerName, memberName, out _),
                $"runtime-api.json is missing prototype member '{ownerName}.prototype.{memberName}'.");
        }
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
            var candidate = Path.GetFullPath(Path.Combine(directory, "documents", "schema", "runtime-api.json"));
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

    private static string GetRuntimeRoot()
    {
        var directory = AppContext.BaseDirectory;
        for (var i = 0; i < 10; i++)
        {
            var candidate = Path.GetFullPath(Path.Combine(directory, "src", "Runtime"));
            if (Directory.Exists(candidate))
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

        throw new DirectoryNotFoundException("src/Runtime was not found from test output path.");
    }

    private static IEnumerable<string> ExtractDefineNames(string source, string? receiver)
    {
        var seen = new HashSet<string>(StringComparer.Ordinal);
        var pattern = receiver == null
            ? DefinePattern
            : new Regex(Regex.Escape(receiver) + "\\.Define\\(\"(?<name>[^\"]+)\"", RegexOptions.Compiled);
        foreach (Match match in pattern.Matches(source))
        {
            var name = match.Groups["name"].Value;
            if (seen.Add(name))
            {
                yield return name;
            }
        }
    }

    private static readonly Regex DefinePattern = new("\\bDefine\\(\"(?<name>[^\"]+)\"", RegexOptions.Compiled);
    private static readonly Regex PrototypeDefinePattern = new("\\b(?<prototype>[A-Za-z0-9_]+Prototype)\\.Define\\(\"(?<name>[^\"]+)\"", RegexOptions.Compiled);
}
