using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace AuroraScript.LanguageServices.Builtins;

public static class BuiltinApiLoader
{
    public static BuiltinApiCatalog LoadFromFile(string path)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        using var stream = File.OpenRead(path);
        return Load(stream);
    }

    public static BuiltinApiCatalog Load(Stream stream)
    {
        ArgumentNullException.ThrowIfNull(stream);
        using var document = JsonDocument.Parse(stream);

        var root = document.RootElement;
        var version = root.TryGetProperty("versionTarget", out var versionElement)
            ? versionElement.GetString() ?? string.Empty
            : string.Empty;

        var globals = new Dictionary<string, BuiltinApiSymbol>(StringComparer.Ordinal);
        if (root.TryGetProperty("globals", out var globalsElement))
        {
            foreach (var globalProperty in globalsElement.EnumerateObject())
            {
                globals.Add(globalProperty.Name, ReadGlobal(globalProperty.Name, globalProperty.Value));
            }
        }

        var modules = new Dictionary<string, BuiltinApiModule>(StringComparer.Ordinal);
        if (root.TryGetProperty("modules", out var modulesElement))
        {
            foreach (var moduleProperty in modulesElement.EnumerateObject())
            {
                var module = ReadModule(moduleProperty.Name, moduleProperty.Value);
                modules.Add(module.ModulePath, module);
            }
        }

        var prototypes = new Dictionary<string, IReadOnlyDictionary<string, BuiltinApiMember>>(StringComparer.Ordinal);
        if (root.TryGetProperty("prototypes", out var prototypesElement))
        {
            foreach (var prototypeProperty in prototypesElement.EnumerateObject())
            {
                prototypes.Add(prototypeProperty.Name, ReadMembers(prototypeProperty.Name, prototypeProperty.Value));
            }
        }

        return new BuiltinApiCatalog(version, globals, prototypes, modules);
    }

    private static BuiltinApiModule ReadModule(string name, JsonElement element)
    {
        var modulePath = element.TryGetProperty("modulePath", out var modulePathElement)
            ? modulePathElement.GetString() ?? name
            : name;
        var documentation = ReadDocumentation(element);
        var members = element.TryGetProperty("members", out var membersElement)
            ? ReadMembers(name, membersElement)
            : new Dictionary<string, BuiltinApiMember>(StringComparer.Ordinal);

        return new BuiltinApiModule(name, modulePath, documentation, members);
    }

    private static BuiltinApiSymbol ReadGlobal(string name, JsonElement element)
    {
        var kind = ReadKind(element, BuiltinApiKind.Object);
        var readOnly = ReadReadOnly(element, defaultValue: true);
        var callable = element.TryGetProperty("callable", out var callableElement) &&
            callableElement.ValueKind == JsonValueKind.True;
        var constructors = element.TryGetProperty("constructors", out var constructorsElement)
            ? ReadConstructors(name, constructorsElement)
            : Array.Empty<BuiltinApiMember>();
        var documentation = ReadDocumentation(element);
        var members = element.TryGetProperty("members", out var membersElement)
            ? ReadMembers(name, membersElement)
            : new Dictionary<string, BuiltinApiMember>(StringComparer.Ordinal);

        return new BuiltinApiSymbol(name, kind, readOnly, callable, constructors, documentation, members);
    }

    private static IReadOnlyList<BuiltinApiMember> ReadConstructors(string ownerName, JsonElement element)
    {
        if (element.ValueKind != JsonValueKind.Array)
        {
            return Array.Empty<BuiltinApiMember>();
        }

        var constructors = new List<BuiltinApiMember>();
        foreach (var constructorElement in element.EnumerateArray())
        {
            var returnType = constructorElement.TryGetProperty("returns", out var returnsElement)
                ? returnsElement.GetString() ?? ownerName
                : ownerName;
            constructors.Add(new BuiltinApiMember(
                string.Empty,
                ownerName,
                BuiltinApiKind.Constructor,
                returnType,
                readOnly: true,
                ReadParameters(constructorElement),
                ReadDocumentation(constructorElement)));
        }

        return constructors;
    }

    private static IReadOnlyDictionary<string, BuiltinApiMember> ReadMembers(string ownerName, JsonElement element)
    {
        var members = new Dictionary<string, BuiltinApiMember>(StringComparer.Ordinal);
        foreach (var memberProperty in element.EnumerateObject())
        {
            members.Add(memberProperty.Name, ReadMember(ownerName, memberProperty.Name, memberProperty.Value));
        }

        return members;
    }

    private static BuiltinApiMember ReadMember(string ownerName, string name, JsonElement element)
    {
        var kind = ReadKind(element, BuiltinApiKind.Property);
        var returnType = element.TryGetProperty("returns", out var returnsElement)
            ? returnsElement.GetString() ?? "any"
            : "any";
        var readOnly = ReadReadOnly(element, defaultValue: true);
        var parameters = ReadParameters(element);
        var documentation = ReadDocumentation(element);
        return new BuiltinApiMember(ownerName, name, kind, returnType, readOnly, parameters, documentation);
    }

    private static bool ReadReadOnly(JsonElement element, bool defaultValue)
    {
        if (element.TryGetProperty("readonly", out var readOnlyElement))
        {
            return readOnlyElement.ValueKind == JsonValueKind.True;
        }

        if (element.TryGetProperty("writable", out var writableElement))
        {
            return writableElement.ValueKind == JsonValueKind.False;
        }

        return defaultValue;
    }

    private static IReadOnlyList<BuiltinApiParameter> ReadParameters(JsonElement element)
    {
        if (!element.TryGetProperty("parameters", out var parametersElement) ||
            parametersElement.ValueKind != JsonValueKind.Array)
        {
            return Array.Empty<BuiltinApiParameter>();
        }

        var parameters = new List<BuiltinApiParameter>();
        foreach (var parameterElement in parametersElement.EnumerateArray())
        {
            var name = parameterElement.TryGetProperty("name", out var nameElement)
                ? nameElement.GetString() ?? string.Empty
                : string.Empty;
            var type = parameterElement.TryGetProperty("type", out var typeElement)
                ? typeElement.GetString() ?? "any"
                : "any";
            var optional = parameterElement.TryGetProperty("optional", out var optionalElement) &&
                optionalElement.ValueKind == JsonValueKind.True;
            var variadic = parameterElement.TryGetProperty("variadic", out var variadicElement) &&
                variadicElement.ValueKind == JsonValueKind.True;
            parameters.Add(new BuiltinApiParameter(name, type, optional, variadic));
        }

        return parameters;
    }

    private static BuiltinApiDocumentation ReadDocumentation(JsonElement element)
    {
        if (!element.TryGetProperty("notes", out var notesElement))
        {
            return BuiltinApiDocumentation.Empty;
        }

        if (notesElement.ValueKind == JsonValueKind.Array)
        {
            return new BuiltinApiDocumentation(new Dictionary<string, IReadOnlyList<string>>(StringComparer.OrdinalIgnoreCase)
            {
                ["en"] = ReadNoteArray(notesElement)
            });
        }

        if (notesElement.ValueKind != JsonValueKind.Object)
        {
            return BuiltinApiDocumentation.Empty;
        }

        var notesByLanguage = new Dictionary<string, IReadOnlyList<string>>(StringComparer.OrdinalIgnoreCase);
        foreach (var property in notesElement.EnumerateObject())
        {
            if (property.Value.ValueKind == JsonValueKind.Array)
            {
                notesByLanguage[property.Name] = ReadNoteArray(property.Value);
            }
        }

        return new BuiltinApiDocumentation(notesByLanguage);
    }

    private static IReadOnlyList<string> ReadNoteArray(JsonElement notesElement)
    {
        var notes = new List<string>();
        foreach (var noteElement in notesElement.EnumerateArray())
        {
            var note = noteElement.GetString();
            if (!string.IsNullOrWhiteSpace(note))
            {
                notes.Add(note);
            }
        }

        return notes;
    }

    private static BuiltinApiKind ReadKind(JsonElement element, BuiltinApiKind fallback)
    {
        if (!element.TryGetProperty("kind", out var kindElement))
        {
            return fallback;
        }

        return kindElement.GetString() switch
        {
            "constructor" => BuiltinApiKind.Constructor,
            "type" => BuiltinApiKind.Type,
            "object" => BuiltinApiKind.Object,
            "function" => BuiltinApiKind.Function,
            "method" => BuiltinApiKind.Method,
            "property" => BuiltinApiKind.Property,
            "constant" => BuiltinApiKind.Constant,
            _ => fallback
        };
    }
}
