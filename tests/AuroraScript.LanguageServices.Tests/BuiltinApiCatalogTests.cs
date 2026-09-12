using AuroraScript.LanguageServices.Builtins;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using Xunit;

namespace AuroraScript.LanguageServices.Tests;

public sealed class BuiltinApiCatalogTests
{
    [Theory]
    [InlineData("Number", true)]
    [InlineData("Boolean", true)]
    [InlineData("String", true)]
    [InlineData("Date", false)]
    [InlineData("Array", false)]
    [InlineData("Object", false)]
    public void ConstructorCallabilityMatchesLanguagePolicy(string name, bool callable)
    {
        Assert.True(LoadCatalog().TryGetGlobal(name, out var type));
        Assert.Equal(callable, type.Callable);
    }

    [Fact]
    public void LoadsRuntimeApiMetadata()
    {
        var catalog = LoadCatalog();

        Assert.True(catalog.TryGetGlobal("Math", out var math));
        Assert.Equal(BuiltinApiKind.Type, math.Kind);
        Assert.True(math.TryGetMember("abs", out var abs));
        Assert.Equal(BuiltinApiKind.Method, abs.Kind);
        Assert.Equal("number", abs.ReturnType);
        Assert.True(abs.ReadOnly);
        var parameter = Assert.Single(abs.Parameters);
        Assert.Equal("value", parameter.Name);
        Assert.Equal("number", parameter.Type);
    }

    [Fact]
    public void LoadsHotPatchCanonicalParameters()
    {
        var catalog = LoadCatalog();

        Assert.True(catalog.TryGetGlobal("HotPatch", out var hotPatch));
        Assert.True(hotPatch.TryGetMember("incremental", out var incremental));
        AssertCanonicalPatchParameters(incremental.Parameters);
        Assert.True(hotPatch.TryGetMember("replace", out var replace));
        AssertCanonicalPatchParameters(replace.Parameters);
    }

    private static void AssertCanonicalPatchParameters(IReadOnlyList<BuiltinApiParameter> parameters)
    {
        Assert.Equal(3, parameters.Count);
        Assert.Equal("modulePath", parameters[0].Name);
        Assert.Equal("string|Path", parameters[0].Type);
        Assert.True(parameters[0].Optional);
        Assert.Equal("script", parameters[1].Name);
        Assert.Equal("string", parameters[1].Type);
        Assert.False(parameters[1].Optional);
        Assert.Equal("ignoreDepends", parameters[2].Name);
        Assert.Equal("boolean", parameters[2].Type);
        Assert.True(parameters[2].Optional);
        Assert.Equal("false", parameters[2].DefaultValue);
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
        Assert.True(global.TryGetMember("getModule", out var getModule));
        Assert.Equal(BuiltinApiKind.Method, getModule.Kind);
        Assert.Equal("object|null", getModule.ReturnType);
        var parameter = Assert.Single(getModule.Parameters);
        Assert.Equal("moduleName", parameter.Name);
        Assert.Equal("string", parameter.Type);
    }

    [Fact]
    public void LoadsOptInBuiltinModules()
    {
        var catalog = LoadCatalog();

        Assert.False(catalog.TryGetGlobal("fs", out _));
        Assert.True(catalog.TryGetModule("fs", out var fileSystem));
        Assert.Equal("fs", fileSystem.Name);
        Assert.True(fileSystem.TryGetMember("readText", out var readText));
        Assert.Equal("string", readText.ReturnType);
        Assert.Equal("string|Path", Assert.Single(readText.Parameters).Type);
        Assert.True(fileSystem.TryGetMember("size", out var size));
        Assert.Equal("number", size.ReturnType);

        Assert.True(catalog.TryGetModule("http", out var http));
        Assert.True(http.TryGetMember("request", out var request));
        Assert.Equal("HttpResponse", request.ReturnType);
        Assert.True(http.TryGetMember("getAsync", out var getAsync));
        Assert.Equal("callback", getAsync.Parameters[^1].Name);
        Assert.Equal("HttpCallback", getAsync.Parameters[^1].Type);
        Assert.Equal("boolean", getAsync.ReturnType);

        Assert.True(catalog.FunctionTypes.TryGetValue("HttpCallback", out var callback));
        Assert.Equal("object", callback.ReturnType);
        Assert.Collection(
            callback.Parameters,
            error => Assert.Equal(("error", "object"), (error.Name, error.Type)),
            response => Assert.Equal(("response", "HttpResponse"), (response.Name, response.Type)));
    }

    [Fact]
    public void LoadsNamedObjectTypeShapes()
    {
        var catalog = LoadCatalog();

        Assert.True(catalog.TryGetObjectType("HttpResponse", out var response));
        Assert.True(response.TryGetMember("status", out var status));
        Assert.Equal("number", status.ReturnType);
        Assert.True(status.ReadOnly);
        Assert.True(response.TryGetMember("bytes", out var bytes));
        Assert.Equal("UInt8Array", bytes.ReturnType);

        Assert.True(catalog.TryGetObjectType("HttpRequestOptions", out var options));
        Assert.True(options.TryGetMember("timeout", out var timeout));
        Assert.Equal("number", timeout.ReturnType);
        Assert.False(timeout.ReadOnly);
        Assert.True(options.TryGetMember("responseHeaders", out _));
    }

    [Fact]
    public void EveryBuiltinTypeNameResolvesToADeclaredType()
    {
        var catalog = LoadCatalog();

        foreach (var module in catalog.Modules.Values)
        {
            AssertResolvableTypeNames(module.Members.Values, catalog);
        }
        foreach (var global in catalog.Globals.Values)
        {
            AssertResolvableTypeNames(global.Members.Values, catalog);
            AssertResolvableTypeNames(global.Constructors, catalog);
        }
        foreach (var prototype in catalog.Prototypes.Values)
        {
            AssertResolvableTypeNames(prototype.Values, catalog);
        }
        foreach (var objectType in catalog.ObjectTypes.Values)
        {
            AssertResolvableTypeNames(objectType.Members.Values, catalog);
        }
        foreach (var functionType in catalog.FunctionTypes.Values)
        {
            foreach (var parameter in functionType.Parameters)
            {
                AssertResolvableTypeName(parameter.Type, functionType.Name, catalog);
            }

            AssertResolvableTypeName(functionType.ReturnType, functionType.Name, catalog);
        }
    }

    private static void AssertResolvableTypeNames(
        IEnumerable<BuiltinApiMember> members,
        BuiltinApiCatalog catalog)
    {
        foreach (var member in members)
        {
            foreach (var parameter in member.Parameters)
            {
                AssertResolvableTypeName(parameter.Type, member.FullName, catalog);
            }

            AssertResolvableTypeName(member.ReturnType, member.FullName, catalog);
        }
    }

    private static void AssertResolvableTypeName(
        string declaredType,
        string owner,
        BuiltinApiCatalog catalog)
    {
        foreach (var part in declaredType.Split(
            '|',
            StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            var typeName = part.EndsWith("[]", StringComparison.Ordinal)
                ? part.Substring(0, part.Length - 2)
                : part;
            if (PrimitiveTypeNames.Contains(typeName))
            {
                continue;
            }

            Assert.True(
                catalog.Globals.ContainsKey(typeName) ||
                    catalog.Prototypes.ContainsKey(typeName) ||
                    catalog.FunctionTypes.ContainsKey(typeName) ||
                    catalog.ObjectTypes.ContainsKey(typeName),
                $"{owner} references '{typeName}', which has no declared type to navigate to.");
        }
    }

    private static readonly HashSet<string> PrimitiveTypeNames = new(StringComparer.Ordinal)
    {
        "number", "string", "boolean", "bool", "array", "date", "object",
        "any", "regex", "regexp", "null", "undefined", "void"
    };

    [Fact]
    public void EveryBuiltinFunctionParameterUsesANamedFunctionType()
    {
        var catalog = LoadCatalog();

        foreach (var module in catalog.Modules.Values)
        {
            AssertNamedFunctionParameters(module.Members.Values, catalog);
        }
        foreach (var global in catalog.Globals.Values)
        {
            AssertNamedFunctionParameters(global.Members.Values, catalog);
            AssertNamedFunctionParameters(global.Constructors, catalog);
        }
        foreach (var prototype in catalog.Prototypes.Values)
        {
            AssertNamedFunctionParameters(prototype.Values, catalog);
        }
    }

    private static void AssertNamedFunctionParameters(
        IEnumerable<BuiltinApiMember> members,
        BuiltinApiCatalog catalog)
    {
        foreach (var member in members)
        {
            foreach (var parameter in member.Parameters)
            {
                Assert.DoesNotMatch(
                    @"(^|\|)\s*(function|func)\s*(\||$)",
                    parameter.Type);
                if (parameter.Name.Contains("callback", StringComparison.OrdinalIgnoreCase))
                {
                    Assert.True(
                        catalog.FunctionTypes.ContainsKey(parameter.Type),
                        $"{member.FullName}.{parameter.Name} must reference a declared function type.");
                }
                foreach (var typeName in parameter.Type.Split(
                    '|',
                    StringSplitOptions.RemoveEmptyEntries |
                    StringSplitOptions.TrimEntries))
                {
                    if (typeName.EndsWith("Callback", StringComparison.Ordinal) ||
                        typeName.EndsWith("Factory", StringComparison.Ordinal))
                    {
                        Assert.True(
                            catalog.FunctionTypes.ContainsKey(typeName),
                            $"{member.FullName}.{parameter.Name} references unknown function type '{typeName}'.");
                    }
                }
            }
        }
    }

    [Fact]
    public void RuntimeApiCatalogCoversShippedBuiltinModuleMembers()
    {
        var catalog = LoadCatalog();
        var expected = new Dictionary<string, string[]>(StringComparer.Ordinal)
        {
            ["fs"] =
            [
                "readText", "readBytes", "writeText", "writeBytes", "appendText", "appendBytes",
                "exist", "isFile", "isDir", "size", "mkDir", "dir", "copy", "move", "delete"
            ],
            ["http"] =
            [
                "request", "requestAsync", "get", "getAsync", "post", "postAsync", "put", "putAsync",
                "patch", "patchAsync", "delete", "deleteAsync", "head", "headAsync"
            ]
        };

        foreach (var pair in expected)
        {
            Assert.True(catalog.TryGetModule(pair.Key, out var module));
            Assert.Equal(pair.Value.Length, module.Members.Count);
            foreach (var memberName in pair.Value)
            {
                Assert.True(
                    module.TryGetMember(memberName, out _),
                    $"runtime-api.json is missing module member '{pair.Key}.{memberName}'.");
            }
        }
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
    public void LoadsPackedArrayConstructorsAndMembers()
    {
        var catalog = LoadCatalog();

        foreach (var name in new[] { "Int32Array", "Int8Array", "Float32Array", "Float64Array", "BooleanArray" })
        {
            Assert.True(catalog.TryGetGlobal(name, out var constructor));
            Assert.Equal(BuiltinApiKind.Constructor, constructor.Kind);
            Assert.False(constructor.Callable);
            var signature = Assert.Single(constructor.Constructors);
            Assert.Equal(name, signature.ReturnType);
            var length = Assert.Single(signature.Parameters);
            Assert.Equal("length", length.Name);
            Assert.Equal("number", length.Type);
            Assert.True(length.Optional);

            Assert.True(catalog.TryGetPrototypeMember(name, "length", out var lengthMember));
            Assert.Equal(BuiltinApiKind.Property, lengthMember.Kind);
            Assert.Equal("number", lengthMember.ReturnType);
            Assert.True(catalog.TryGetPrototypeMember(name, "fill", out var fill));
            Assert.Equal(BuiltinApiKind.Method, fill.Kind);
            Assert.Equal(name, fill.ReturnType);
        }
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
            ["global"] = Path.Combine(runtimeRoot, "ScriptGlobal.cs"),
            ["console"] = Path.Combine(runtimeRoot, "Builtin", "ConsoleSupport.cs"),
            ["JSON"] = Path.Combine(runtimeRoot, "Builtin", "JsonSupport.cs"),
            ["TDoc"] = Path.Combine(runtimeRoot, "Builtin", "TDocSupport.cs"),
            ["Math"] = Path.Combine(runtimeRoot, "Builtin", "MathSupport.cs"),
            ["Env"] = Path.Combine(runtimeRoot, "Builtin", "EnvSupport.cs"),
            ["Conv8"] = Path.Combine(runtimeRoot, "Builtin", "Conv8Support.cs"),
            ["Path"] = Path.Combine(runtimeRoot, "Types", "ScriptPathValue.cs"),
            ["HotPatch"] = Path.Combine(runtimeRoot, "Builtin", "HotPatchSupport.cs"),
            ["Array"] = Path.Combine(runtimeRoot, "Types", "ScriptArray.Static.cs"),
            ["String"] = Path.Combine(runtimeRoot, "Types", "StringValue.Static.cs"),
            ["Boolean"] = Path.Combine(runtimeRoot, "Types", "BooleanValue.Static.cs"),
            ["Object"] = Path.Combine(runtimeRoot, "Types", "TypeConstruct", "ScriptObjectConstructor.cs"),
            ["Number"] = Path.Combine(runtimeRoot, "Types", "NumberValue.Static.cs"),
            ["Date"] = Path.Combine(runtimeRoot, "Types", "ScriptDate.Static.cs")
        };

        foreach (var registration in registrations)
        {
            Assert.True(catalog.TryGetGlobal(registration.Key, out var global), $"runtime-api.json is missing global '{registration.Key}'.");
            var source = File.ReadAllText(registration.Value);
            var memberNames = registration.Key is "console" or "JSON" or "TDoc" or "Math" or "Env" or "Conv8" or "HotPatch" or "String" or "Number" or "Boolean" or "Date" or "Array"
                ? ExtractExportNames(source)
                : ExtractDefineNames(source, null);
            foreach (var memberName in memberNames)
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

    [Fact]
    public void RuntimeApiCatalogCoversGeneratedStringPrototypeMembers()
    {
        // String no longer appears in the handwritten prototype.Define scan above.
        var catalog = LoadCatalog();
        var source = File.ReadAllText(Path.Combine(GetRuntimeRoot(), "Types", "StringValue.g.cs"));
        var count = 0;
        foreach (var name in ExtractExportNames(source))
        {
            Assert.True(catalog.TryGetPrototypeMember("String", name, out _),
                $"runtime-api.json is missing generated String prototype member '{name}'.");
            count++;
        }
        Assert.Equal(21, count);
    }

    [Fact]
    public void RuntimeApiCatalogCoversGeneratedArrayPrototypeMembers()
    {
        var catalog = LoadCatalog();
        var source = File.ReadAllText(Path.Combine(GetRuntimeRoot(), "Types", "ScriptArray.Native.cs"));
        var names = ExtractExportNames(source).ToArray();
        foreach (var name in names)
            Assert.True(catalog.TryGetPrototypeMember("Array", name, out _),
                $"runtime-api.json is missing generated Array prototype member '{name}'.");
        Assert.Equal(23, names.Length);
    }

    private static BuiltinApiCatalog LoadCatalog()
    {
        return BuiltinApiLoader.LoadFromFile(GetRuntimeApiPath());
    }

    [Theory]
    [InlineData("StringBuffer", "StringBuffer.cs", "StringBuffer.g.cs", 7)]
    [InlineData("HashMap", "ScriptHashMap.cs", "ScriptHashMap.g.cs", 9)]
    [InlineData("Regex", "ScriptRegex.cs", "ScriptRegex.Static.cs", 1)]
    [InlineData("Date", "ScriptDate.cs", "ScriptDate.g.cs", 11)]
    public void RuntimeApiCatalogCoversMigratedBuiltinPrototypes(string owner, string first, string second, int expectedCount)
    {
        var catalog = LoadCatalog();
        var names = new HashSet<string>(StringComparer.Ordinal);
        foreach (var file in new[] { first, second })
            foreach (var name in ExtractExportNames(File.ReadAllText(Path.Combine(GetRuntimeRoot(), "Types", file))))
            {
                Assert.True(catalog.TryGetPrototypeMember(owner, name, out _), $"Missing {owner}.{name}");
                names.Add(name);
            }
        Assert.Equal(expectedCount, names.Count);
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

    private static IEnumerable<string> ExtractExportNames(string source)
    {
        var seen = new HashSet<string>(StringComparer.Ordinal);
        foreach (Match match in ExportPattern.Matches(source))
        {
            var name = match.Groups["name"].Value;
            if (seen.Add(name))
            {
                yield return name;
            }
        }
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
    private static readonly Regex ExportPattern = new("\\[(?:Receiver)?Export\\(\"(?<name>[^\"]+)\"", RegexOptions.Compiled);
    private static readonly Regex PrototypeDefinePattern = new("\\b(?<prototype>[A-Za-z0-9_]+Prototype)\\.Define\\(\"(?<name>[^\"]+)\"", RegexOptions.Compiled);
}
