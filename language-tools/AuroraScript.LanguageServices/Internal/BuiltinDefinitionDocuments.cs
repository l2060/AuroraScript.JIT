using AuroraScript.LanguageServices.Builtins;
using AuroraScript.LanguageServices.Features.Definition;
using AuroraScript.LanguageServices.Text;
using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;

namespace AuroraScript.LanguageServices.Internal;

internal sealed class BuiltinDefinitionDocuments
{
    public const string Scheme = "aurora-builtin";

    private static readonly Regex TypeTokenPattern = new("[A-Za-z_$][A-Za-z0-9_$]*", RegexOptions.Compiled);
    private static readonly string[] SyntheticTypeNames =
    {
        "Function"
    };

    private readonly Dictionary<string, DocumentInfo> _documentsByOwner = new(StringComparer.Ordinal);
    private readonly Dictionary<string, DocumentInfo> _documentsByModulePath = new(StringComparer.Ordinal);
    private readonly Dictionary<string, DocumentInfo> _documentsByTypeName = new(StringComparer.Ordinal);
    private readonly Dictionary<string, DocumentInfo> _documentsByUri = new(StringComparer.Ordinal);
    private readonly HashSet<string> _knownTypes = new(StringComparer.Ordinal);

    private readonly string? _locale;

    public BuiltinDefinitionDocuments(BuiltinApiCatalog catalog, string? locale = null)
    {
        _locale = locale;
        if (catalog == null)
        {
            return;
        }

        foreach (var pair in catalog.Globals)
        {
            _knownTypes.Add(pair.Key);
        }

        for (var i = 0; i < SyntheticTypeNames.Length; i++)
        {
            _knownTypes.Add(SyntheticTypeNames[i]);
        }

        foreach (var pair in catalog.Globals)
        {
            catalog.Prototypes.TryGetValue(pair.Key, out var prototypeMembers);
            var document = BuildDocument(pair.Value, prototypeMembers);
            _documentsByOwner[pair.Key] = document;
            _documentsByTypeName[pair.Key] = document;
            _documentsByUri[document.Uri] = document;
        }

        foreach (var pair in catalog.Modules)
        {
            var document = BuildModuleDocument(pair.Value);
            _documentsByModulePath[pair.Key] = document;
            _documentsByUri[document.Uri] = document;
        }

        for (var i = 0; i < SyntheticTypeNames.Length; i++)
        {
            var typeName = SyntheticTypeNames[i];
            if (_documentsByTypeName.ContainsKey(typeName))
            {
                continue;
            }

            var document = BuildSyntheticTypeDocument(typeName);
            _documentsByTypeName[typeName] = document;
            _documentsByUri[document.Uri] = document;
        }
    }

    public bool TryGetGlobalLocation(string name, out DefinitionLocation location)
    {
        location = null!;
        if (!_documentsByOwner.TryGetValue(name, out var document))
        {
            return false;
        }

        location = new DefinitionLocation(document.Uri, document.GlobalRange);
        return true;
    }

    public bool TryGetMemberLocation(string ownerName, string memberName, out DefinitionLocation location)
    {
        location = null!;
        if (!_documentsByOwner.TryGetValue(ownerName, out var document) ||
            !document.MemberRanges.TryGetValue(memberName, out var range))
        {
            return false;
        }

        location = new DefinitionLocation(document.Uri, range);
        return true;
    }

    public bool TryGetModuleLocation(string modulePath, out DefinitionLocation location)
    {
        location = null!;
        if (!_documentsByModulePath.TryGetValue(modulePath, out var document))
        {
            return false;
        }

        location = new DefinitionLocation(document.Uri, document.GlobalRange);
        return true;
    }

    public bool TryGetModuleMemberLocation(
        string modulePath,
        string memberName,
        out DefinitionLocation location)
    {
        location = null!;
        if (!_documentsByModulePath.TryGetValue(modulePath, out var document) ||
            !document.MemberRanges.TryGetValue(memberName, out var range))
        {
            return false;
        }

        location = new DefinitionLocation(document.Uri, range);
        return true;
    }

    public bool TryGetDocumentDefinition(string uri, TextPosition position, out DefinitionLocation location)
    {
        location = null!;
        if (!_documentsByUri.TryGetValue(uri, out var document))
        {
            return false;
        }

        for (var i = 0; i < document.BuiltinReferences.Count; i++)
        {
            var reference = document.BuiltinReferences[i];
            if (!Contains(reference.Range, position) ||
                !_documentsByTypeName.TryGetValue(reference.TargetName, out var targetDocument))
            {
                continue;
            }

            location = new DefinitionLocation(targetDocument.Uri, targetDocument.GlobalRange);
            return true;
        }

        return false;
    }

    public bool TryGetDocument(string uri, out BuiltinDocument document)
    {
        document = null!;
        if (!_documentsByUri.TryGetValue(uri, out var info))
        {
            return false;
        }

        document = new BuiltinDocument(info.Uri, info.Text);
        return true;
    }

    public static bool IsBuiltinUri(string uri)
    {
        return uri != null && uri.StartsWith(Scheme + ":", StringComparison.Ordinal);
    }

    public IReadOnlyList<BuiltinDocument> GetDocuments()
    {
        var documents = new List<BuiltinDocument>(_documentsByUri.Count);
        foreach (var pair in _documentsByUri)
        {
            documents.Add(new BuiltinDocument(pair.Value.Uri, pair.Value.Text));
        }

        return documents;
    }

    private DocumentInfo BuildDocument(
        BuiltinApiSymbol symbol,
        IReadOnlyDictionary<string, BuiltinApiMember>? prototypeMembers)
    {
        var uri = Uri(symbol.Name);
        var builder = new DocumentTextBuilder();
        var memberRanges = new Dictionary<string, TextRange>(StringComparer.Ordinal);
        var builtinReferences = new List<BuiltinReference>();

        builder.AppendLine("// AuroraScript built-in declaration document.");
        builder.AppendLine("// Generated from the runtime API catalog for editor navigation.");
        builder.AppendLine();

        AppendDocumentation(builder, uri, symbol.Documentation.GetNotes(_locale), null, null, builtinReferences);
        builder.Append("declare type ");
        var globalRange = builder.AppendToken(uri, symbol.Name);
        builder.AppendLine(" {");

        for (var i = 0; i < symbol.Constructors.Count; i++)
        {
            AppendConstructor(builder, uri, symbol.Constructors[i], builtinReferences);
        }

        if (prototypeMembers != null)
        {
            foreach (var memberPair in prototypeMembers)
            {
                AppendMember(builder, uri, memberPair.Value, instanceMember: true, memberRanges, builtinReferences);
            }
        }

        foreach (var memberPair in symbol.Members)
        {
            AppendMember(builder, uri, memberPair.Value, instanceMember: false, memberRanges, builtinReferences);
        }

        builder.AppendLine("}");

        return new DocumentInfo(uri, builder.ToString(), globalRange, memberRanges, builtinReferences);
    }

    private DocumentInfo BuildModuleDocument(BuiltinApiModule module)
    {
        var uri = Uri(module.Name);
        var builder = new DocumentTextBuilder();
        var memberRanges = new Dictionary<string, TextRange>(StringComparer.Ordinal);
        var builtinReferences = new List<BuiltinReference>();

        builder.AppendLine("// AuroraScript built-in module declaration document.");
        builder.AppendLine("// Generated from the runtime API catalog for editor navigation.");
        builder.AppendLine();

        AppendDocumentation(builder, uri, module.Documentation.GetNotes(_locale), null, null, builtinReferences);
        builder.Append("import ");
        var moduleRange = builder.AppendToken(uri, module.Name);
        builder.Append(" from \"").Append(module.ModulePath).AppendLine("\";");

        if (module.Members.Count != 0)
        {
            builder.AppendLine();
            builder.Append("declare type ");
            builder.AppendToken(uri, module.Name);
            builder.AppendLine(" {");
        }

        foreach (var memberPair in module.Members)
        {
            AppendMember(
                builder,
                uri,
                memberPair.Value,
                instanceMember: false,
                memberRanges,
                builtinReferences);
        }

        if (module.Members.Count != 0)
        {
            builder.AppendLine("}");
        }

        return new DocumentInfo(
            uri,
            builder.ToString(),
            moduleRange,
            memberRanges,
            builtinReferences);
    }

    private static DocumentInfo BuildSyntheticTypeDocument(string typeName)
    {
        var uri = Uri(typeName);
        var builder = new DocumentTextBuilder();
        var memberRanges = new Dictionary<string, TextRange>(StringComparer.Ordinal);
        var builtinReferences = new List<BuiltinReference>();

        builder.AppendLine("// AuroraScript built-in declaration document.");
        builder.AppendLine("// Generated for declaration-file type navigation.");
        builder.AppendLine();
        builder.AppendLine("/**");
        builder.Append("* Built-in ").Append(typeName).AppendLine(" type used by runtime API declarations.");
        builder.AppendLine("*/");
        builder.Append("declare type ");
        var globalRange = builder.AppendToken(uri, typeName);
        builder.AppendLine(";");

        return new DocumentInfo(uri, builder.ToString(), globalRange, memberRanges, builtinReferences);
    }

    private void AppendConstructor(
        DocumentTextBuilder builder,
        string uri,
        BuiltinApiMember constructor,
        List<BuiltinReference> builtinReferences)
    {
        AppendDocumentation(builder, uri, constructor.Documentation.GetNotes(_locale), constructor.Parameters, constructor.ReturnType, builtinReferences);
        builder.Append("    constructor(");
        AppendParameters(builder, uri, constructor.Parameters, builtinReferences);
        builder.AppendLine(");");
    }

    private void AppendMember(
        DocumentTextBuilder builder,
        string uri,
        BuiltinApiMember member,
        bool instanceMember,
        Dictionary<string, TextRange>? memberRanges,
        List<BuiltinReference> builtinReferences)
    {
        AppendDocumentation(builder, uri, member.Documentation.GetNotes(_locale), member.Parameters, member.ReturnType, builtinReferences);
        builder.Append("    ");
        if (!instanceMember)
        {
            builder.Append("static ");
        }

        if (member.Kind == BuiltinApiKind.Method || member.Kind == BuiltinApiKind.Function)
        {
            builder.Append("func ");
            var memberRange = builder.AppendToken(uri, member.Name);
            RecordMemberRange(memberRanges, member.Name, memberRange);
            builder.Append("(");
            AppendParameters(builder, uri, member.Parameters, builtinReferences);
            builder.Append(") ");
            AppendType(builder, uri, member.ReturnType, BuiltinTypeFormatter.TypeUsage.Return, optional: false, variadic: false, builtinReferences);
            builder.AppendLine(";");
            return;
        }

        if (member.Kind == BuiltinApiKind.Constant || member.ReadOnly)
        {
            builder.Append("const ");
        }

        AppendType(builder, uri, member.ReturnType, BuiltinTypeFormatter.TypeUsage.Value, optional: false, variadic: false, builtinReferences);
        builder.Append(" ");
        var fieldRange = builder.AppendToken(uri, member.Name);
        RecordMemberRange(memberRanges, member.Name, fieldRange);
        builder.AppendLine(";");
    }

    private static void RecordMemberRange(
        Dictionary<string, TextRange>? memberRanges,
        string name,
        TextRange range)
    {
        if (memberRanges != null && !memberRanges.ContainsKey(name))
        {
            memberRanges[name] = range;
        }
    }

    private void AppendParameters(
        DocumentTextBuilder builder,
        string uri,
        IReadOnlyList<BuiltinApiParameter> parameters,
        List<BuiltinReference> builtinReferences)
    {
        for (var i = 0; i < parameters.Count; i++)
        {
            if (i > 0)
            {
                builder.Append(", ");
            }

            var parameter = parameters[i];
            if (parameter.Variadic)
            {
                builder.Append("...");
            }

            AppendType(builder, uri, parameter.Type, BuiltinTypeFormatter.TypeUsage.Value, parameter.Optional, variadic: false, builtinReferences);
            builder.Append(" ").Append(BuiltinTypeFormatter.SafeParameterName(parameter.Name, i));
        }
    }

    private void AppendDocumentation(
        DocumentTextBuilder builder,
        string uri,
        IReadOnlyList<string> notes,
        IReadOnlyList<BuiltinApiParameter>? parameters,
        string? returnType,
        List<BuiltinReference> builtinReferences)
    {
        var hasParameters = parameters != null && parameters.Count != 0;
        var hasReturnType = !string.IsNullOrWhiteSpace(returnType);
        if (notes.Count == 0 && !hasParameters && !hasReturnType)
        {
            return;
        }

        builder.AppendLine("/**");
        for (var i = 0; i < notes.Count; i++)
        {
            builder.Append("* ").AppendLine(notes[i]);
        }

        if (hasParameters)
        {
            for (var i = 0; i < parameters!.Count; i++)
            {
                var parameter = parameters[i];
                var type = BuiltinTypeFormatter.FormatType(parameter.Type, BuiltinTypeFormatter.TypeUsage.Value, parameter.Optional, parameter.Variadic);
                builder
                    .Append("* @param ")
                    .Append(BuiltinTypeFormatter.SafeParameterName(parameter.Name, i))
                    .Append(" ");
                AppendFormattedType(builder, uri, type, builtinReferences);
                builder.AppendLine(".");
            }
        }

        if (hasReturnType)
        {
            var type = BuiltinTypeFormatter.FormatType(returnType!, BuiltinTypeFormatter.TypeUsage.Return, optional: false, variadic: false);
            if (!string.Equals(type, "void", StringComparison.Ordinal))
            {
                builder.Append("* @returns ");
                AppendFormattedType(builder, uri, type, builtinReferences);
                builder.AppendLine(".");
            }
        }

        builder.AppendLine("*/");
    }

    private void AppendFormattedType(
        DocumentTextBuilder builder,
        string uri,
        string type,
        List<BuiltinReference> builtinReferences)
    {
        var startLine = builder.Line;
        var startCharacter = builder.Character;
        builder.Append(type);

        foreach (Match match in TypeTokenPattern.Matches(type))
        {
            var token = match.Value;
            if (!_knownTypes.Contains(token))
            {
                continue;
            }

            builtinReferences.Add(new BuiltinReference(
                token,
                Range(uri, startLine, startCharacter + match.Index, match.Length)));
        }
    }

    private void AppendType(
        DocumentTextBuilder builder,
        string uri,
        string rawType,
        BuiltinTypeFormatter.TypeUsage usage,
        bool optional,
        bool variadic,
        List<BuiltinReference> builtinReferences)
    {
        var type = BuiltinTypeFormatter.FormatType(rawType, usage, optional, variadic);
        AppendFormattedType(builder, uri, type, builtinReferences);
    }

    private static bool Contains(TextRange range, TextPosition position)
    {
        if (position.Line < range.Start.Line || position.Line > range.End.Line)
        {
            return false;
        }

        if (position.Line == range.Start.Line && position.Character < range.Start.Character)
        {
            return false;
        }

        if (position.Line == range.End.Line && position.Character >= range.End.Character)
        {
            return false;
        }

        return true;
    }

    private static TextRange Range(string uri, int line, int character, int length)
    {
        if (character < 0)
        {
            character = 0;
        }

        return new TextRange(
            uri,
            new TextPosition(line, character),
            new TextPosition(line, character + length));
    }

    private static string Uri(string ownerName)
    {
        return Scheme + ":/" + ownerName + ".as";
    }

    private readonly record struct BuiltinReference(string TargetName, TextRange Range);

    private sealed class DocumentInfo
    {
        public DocumentInfo(
            string uri,
            string text,
            TextRange globalRange,
            IReadOnlyDictionary<string, TextRange> memberRanges,
            IReadOnlyList<BuiltinReference> builtinReferences)
        {
            Uri = uri;
            Text = text;
            GlobalRange = globalRange;
            MemberRanges = memberRanges;
            BuiltinReferences = builtinReferences;
        }

        public string Uri { get; }
        public string Text { get; }
        public TextRange GlobalRange { get; }
        public IReadOnlyDictionary<string, TextRange> MemberRanges { get; }
        public IReadOnlyList<BuiltinReference> BuiltinReferences { get; }
    }

    private sealed class DocumentTextBuilder
    {
        private readonly StringBuilder _builder = new();

        public int Line { get; private set; }
        public int Character { get; private set; }

        public DocumentTextBuilder Append(string text)
        {
            if (string.IsNullOrEmpty(text))
            {
                return this;
            }

            _builder.Append(text);
            for (var i = 0; i < text.Length; i++)
            {
                if (text[i] == '\n')
                {
                    Line++;
                    Character = 0;
                }
                else
                {
                    Character++;
                }
            }

            return this;
        }

        public DocumentTextBuilder AppendLine()
        {
            return Append("\n");
        }

        public DocumentTextBuilder AppendLine(string text)
        {
            return Append(text).Append("\n");
        }

        public TextRange AppendToken(string uri, string token)
        {
            var line = Line;
            var character = Character;
            Append(token);
            return Range(uri, line, character, token.Length);
        }

        public override string ToString()
        {
            return _builder.ToString();
        }
    }
}
