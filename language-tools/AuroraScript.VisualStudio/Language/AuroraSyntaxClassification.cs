using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Classification;
using Microsoft.VisualStudio.Text.Tagging;
using Microsoft.VisualStudio.Utilities;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.IO;
using System.Linq;
using System.Windows.Media;

namespace AuroraScript.VisualStudio.Language;

internal static class AuroraSyntaxClassificationTypes
{
    public const string Object = "AuroraScript.Object";
    public const string Type = "AuroraScript.Type";
    public const string FunctionCall = "AuroraScript.FunctionCall";
    public const string MethodCall = "AuroraScript.MethodCall";
    public const string Property = "AuroraScript.Property";
    public const string MapKey = "AuroraScript.MapKey";
    public const string BuiltinVariable = "AuroraScript.BuiltinVariable";
    public const string DeclaredGlobal = "AuroraScript.DeclaredGlobal";
    public const string DeclaredGlobalFunction = "AuroraScript.DeclaredGlobalFunction";
    public const string ControlFlow = "AuroraScript.ControlFlow";
    public const string Return = "AuroraScript.Return";
    public const string Throw = "AuroraScript.Throw";
    public const string Exception = "AuroraScript.Exception";
    public const string ImportExport = "AuroraScript.ImportExport";
    public const string Enum = "AuroraScript.Enum";
    public const string EnumMember = "AuroraScript.EnumMember";
    public const string String = "AuroraScript.String";
    public const string Character = "AuroraScript.Character";
    public const string Number = "AuroraScript.Number";
    public const string Keyword = "AuroraScript.Keyword";
}

[Export(typeof(ITaggerProvider))]
[ContentType(AuroraContentTypeDefinition.ContentTypeName)]
[TagType(typeof(ClassificationTag))]
internal sealed class AuroraSyntaxTaggerProvider : ITaggerProvider
{
    [Import]
    internal IClassificationTypeRegistryService ClassificationTypes = null!;

    [Import]
    internal ITextDocumentFactoryService TextDocuments = null!;

    public ITagger<T>? CreateTagger<T>(ITextBuffer buffer)
        where T : ITag
    {
        return new AuroraSyntaxTagger(buffer, ClassificationTypes, TextDocuments) as ITagger<T>;
    }
}

internal sealed class AuroraSyntaxTagger : ITagger<ClassificationTag>
{
    private static readonly HashSet<string> ControlFlowKeywords = new(StringComparer.Ordinal)
    {
        "if",
        "else",
        "for",
        "while",
        "break",
        "continue"
    };

    private static readonly HashSet<string> ExceptionKeywords = new(StringComparer.Ordinal)
    {
        "try",
        "catch",
        "finally"
    };

    private static readonly HashSet<string> ImportExportKeywords = new(StringComparer.Ordinal)
    {
        "import",
        "include",
        "from",
        "export"
    };

    private static readonly HashSet<string> DeclarationKeywords = new(StringComparer.Ordinal)
    {
        "const",
        "var",
        "func",
        "function",
        "enum",
        "declare",
        "new",
        "typeof",
        "delete",
        "debugger",
        "in"
    };

    private static readonly HashSet<string> BuiltinVariables = new(StringComparer.Ordinal)
    {
        "global",
        "$arg",
        "$args",
        "$state"
    };

    private static readonly HashSet<string> BuiltinObjects = new(StringComparer.Ordinal)
    {
        "console",
        "HotPatch",
        "JSON",
        "Math"
    };

    private static readonly HashSet<string> BuiltinTypes = new(StringComparer.Ordinal)
    {
        "Array",
        "Boolean",
        "BooleanArray",
        "Date",
        "Error",
        "Function",
        "Float64Array",
        "HashMap",
        "Int32Array",
        "Int8Array",
        "Number",
        "Object",
        "Path",
        "Proxy",
        "Regex",
        "String",
        "StringBuffer"
    };

    private const int MaxWorkspaceDeclareFiles = 2000;
    private static readonly IReadOnlyDictionary<string, LightweightSymbolKind> EmptyAmbientDeclarations =
        new Dictionary<string, LightweightSymbolKind>(StringComparer.Ordinal);
    private static readonly AmbientDeclareCache WorkspaceAmbientDeclarations = new();

    private readonly ITextBuffer _buffer;
    private readonly ITextDocumentFactoryService? _textDocuments;
    private readonly Dictionary<string, ClassificationTag> _tags;

    private enum LightweightSymbolKind
    {
        Local,
        DeclaredGlobal,
        DeclaredGlobalFunction
    }

    private sealed class LightweightScope
    {
        public LightweightScope(int start, LightweightScope? parent)
        {
            Start = start;
            End = int.MaxValue;
            Parent = parent;
        }

        public int Start { get; }

        public int End { get; set; }

        public LightweightScope? Parent { get; }

        public List<LightweightScope> Children { get; } = new();

        public Dictionary<string, LightweightSymbolKind> Symbols { get; } = new(StringComparer.Ordinal);

        public void Declare(string name, LightweightSymbolKind kind)
        {
            if (!string.IsNullOrEmpty(name))
            {
                Symbols[name] = kind;
            }
        }

        public LightweightScope FindInnermost(int position)
        {
            for (var i = 0; i < Children.Count; i++)
            {
                var child = Children[i];
                if (position >= child.Start && position < child.End)
                {
                    return child.FindInnermost(position);
                }
            }

            return this;
        }
    }

    private sealed class NameSpan
    {
        public NameSpan(int start, int end)
        {
            Start = start;
            End = end;
        }

        public int Start { get; }

        public int End { get; }
    }

    private sealed class LightweightSymbolIndex
    {
        private readonly LightweightScope _root;
        private readonly List<NameSpan> _localDeclarationSpans = new();
        private readonly Dictionary<string, LightweightSymbolKind> _ambientSymbols = new(StringComparer.Ordinal);

        public LightweightSymbolIndex(LightweightScope root)
        {
            _root = root;
        }

        public void AddLocalDeclarationSpan(int start, int end)
        {
            if (end > start)
            {
                _localDeclarationSpans.Add(new NameSpan(start, end));
            }
        }

        public bool IsLocalDeclarationSpan(int start, int end)
        {
            for (var i = 0; i < _localDeclarationSpans.Count; i++)
            {
                var span = _localDeclarationSpans[i];
                if (span.Start == start && span.End == end)
                {
                    return true;
                }
            }

            return false;
        }

        public bool TryResolveExternal(int position, string name, out string classification)
        {
            var shadowed = false;
            for (var scope = _root.FindInnermost(position); scope != null; scope = scope.Parent)
            {
                if (!scope.Symbols.TryGetValue(name, out var kind))
                {
                    continue;
                }

                if (TryGetExternalClassification(kind, out classification))
                {
                    return true;
                }

                shadowed = true;
                break;
            }

            if (!shadowed && TryResolveAmbient(name, out classification))
            {
                return true;
            }

            classification = string.Empty;
            return false;
        }

        public bool TryResolveAmbient(string name, out string classification)
        {
            if (_ambientSymbols.TryGetValue(name, out var kind) &&
                TryGetExternalClassification(kind, out classification))
            {
                return true;
            }

            classification = string.Empty;
            return false;
        }

        public bool IsDeclared(int position, string name)
        {
            for (var scope = _root.FindInnermost(position); scope != null; scope = scope.Parent)
            {
                if (scope.Symbols.ContainsKey(name))
                {
                    return true;
                }
            }

            return false;
        }

        public void AddAmbientDeclarations(IReadOnlyDictionary<string, LightweightSymbolKind> declarations)
        {
            foreach (var pair in declarations)
            {
                DeclareAmbient(pair.Key, pair.Value);
            }
        }

        public void DeclareAmbient(string name, LightweightSymbolKind kind)
        {
            if (!string.IsNullOrEmpty(name) &&
                (kind == LightweightSymbolKind.DeclaredGlobal ||
                    kind == LightweightSymbolKind.DeclaredGlobalFunction))
            {
                _ambientSymbols[name] = kind;
            }
        }

        private static bool TryGetExternalClassification(LightweightSymbolKind kind, out string classification)
        {
            if (kind == LightweightSymbolKind.DeclaredGlobal)
            {
                classification = AuroraSyntaxClassificationTypes.DeclaredGlobal;
                return true;
            }

            if (kind == LightweightSymbolKind.DeclaredGlobalFunction)
            {
                classification = AuroraSyntaxClassificationTypes.DeclaredGlobalFunction;
                return true;
            }

            classification = string.Empty;
            return false;
        }
    }

    private sealed class AmbientDeclareCache
    {
        private static readonly TimeSpan RefreshInterval = TimeSpan.FromSeconds(1);
        private readonly object _gate = new();
        private readonly Dictionary<string, AmbientDeclareCacheEntry> _entries = new(StringComparer.OrdinalIgnoreCase);

        public IReadOnlyDictionary<string, LightweightSymbolKind> Get(string? filePath)
        {
            if (string.IsNullOrWhiteSpace(filePath))
            {
                return EmptyAmbientDeclarations;
            }

            string fullPath;
            try
            {
                fullPath = Path.GetFullPath(filePath);
            }
            catch (Exception ex) when (IsFileReadFailure(ex))
            {
                return EmptyAmbientDeclarations;
            }

            var root = FindWorkspaceRoot(fullPath);
            if (string.IsNullOrWhiteSpace(root))
            {
                return EmptyAmbientDeclarations;
            }

            var now = DateTime.UtcNow;
            lock (_gate)
            {
                if (_entries.TryGetValue(root, out var cached) &&
                    cached.ExpiresAtUtc > now)
                {
                    return cached.Declarations;
                }
            }

            var declarations = ScanWorkspaceAmbientDeclarations(root);
            lock (_gate)
            {
                _entries[root] = new AmbientDeclareCacheEntry(
                    declarations,
                    DateTime.UtcNow.Add(RefreshInterval));
            }

            return declarations;
        }
    }

    private sealed class AmbientDeclareCacheEntry
    {
        public AmbientDeclareCacheEntry(
            IReadOnlyDictionary<string, LightweightSymbolKind> declarations,
            DateTime expiresAtUtc)
        {
            Declarations = declarations;
            ExpiresAtUtc = expiresAtUtc;
        }

        public IReadOnlyDictionary<string, LightweightSymbolKind> Declarations { get; }

        public DateTime ExpiresAtUtc { get; }
    }

    public AuroraSyntaxTagger(
        ITextBuffer buffer,
        IClassificationTypeRegistryService classificationTypes,
        ITextDocumentFactoryService? textDocuments)
    {
        _buffer = buffer;
        _textDocuments = textDocuments;
        _tags = new Dictionary<string, ClassificationTag>(StringComparer.Ordinal)
        {
            [AuroraSyntaxClassificationTypes.Object] = CreateTag(classificationTypes, AuroraSyntaxClassificationTypes.Object),
            [AuroraSyntaxClassificationTypes.Type] = CreateTag(classificationTypes, AuroraSyntaxClassificationTypes.Type),
            [AuroraSyntaxClassificationTypes.FunctionCall] = CreateTag(classificationTypes, AuroraSyntaxClassificationTypes.FunctionCall),
            [AuroraSyntaxClassificationTypes.MethodCall] = CreateTag(classificationTypes, AuroraSyntaxClassificationTypes.MethodCall),
            [AuroraSyntaxClassificationTypes.Property] = CreateTag(classificationTypes, AuroraSyntaxClassificationTypes.Property),
            [AuroraSyntaxClassificationTypes.MapKey] = CreateTag(classificationTypes, AuroraSyntaxClassificationTypes.MapKey),
            [AuroraSyntaxClassificationTypes.BuiltinVariable] = CreateTag(classificationTypes, AuroraSyntaxClassificationTypes.BuiltinVariable),
            [AuroraSyntaxClassificationTypes.DeclaredGlobal] = CreateTag(classificationTypes, AuroraSyntaxClassificationTypes.DeclaredGlobal),
            [AuroraSyntaxClassificationTypes.DeclaredGlobalFunction] = CreateTag(classificationTypes, AuroraSyntaxClassificationTypes.DeclaredGlobalFunction),
            [AuroraSyntaxClassificationTypes.ControlFlow] = CreateTag(classificationTypes, AuroraSyntaxClassificationTypes.ControlFlow),
            [AuroraSyntaxClassificationTypes.Return] = CreateTag(classificationTypes, AuroraSyntaxClassificationTypes.Return),
            [AuroraSyntaxClassificationTypes.Throw] = CreateTag(classificationTypes, AuroraSyntaxClassificationTypes.Throw),
            [AuroraSyntaxClassificationTypes.Exception] = CreateTag(classificationTypes, AuroraSyntaxClassificationTypes.Exception),
            [AuroraSyntaxClassificationTypes.ImportExport] = CreateTag(classificationTypes, AuroraSyntaxClassificationTypes.ImportExport),
            [AuroraSyntaxClassificationTypes.Enum] = CreateTag(classificationTypes, AuroraSyntaxClassificationTypes.Enum),
            [AuroraSyntaxClassificationTypes.EnumMember] = CreateTag(classificationTypes, AuroraSyntaxClassificationTypes.EnumMember),
            [AuroraSyntaxClassificationTypes.String] = CreateTag(classificationTypes, AuroraSyntaxClassificationTypes.String),
            [AuroraSyntaxClassificationTypes.Character] = CreateTag(classificationTypes, AuroraSyntaxClassificationTypes.Character),
            [AuroraSyntaxClassificationTypes.Number] = CreateTag(classificationTypes, AuroraSyntaxClassificationTypes.Number),
            [AuroraSyntaxClassificationTypes.Keyword] = CreateTag(classificationTypes, AuroraSyntaxClassificationTypes.Keyword)
        };
        _buffer.Changed += OnBufferChanged;
    }

    public event EventHandler<SnapshotSpanEventArgs>? TagsChanged;

    public IEnumerable<ITagSpan<ClassificationTag>> GetTags(NormalizedSnapshotSpanCollection spans)
    {
        if (spans.Count == 0)
        {
            yield break;
        }

        var snapshot = spans[0].Snapshot;
        var text = snapshot.GetText();
        var isTypedDocument = IsTypedDocumentFile();
        var enumNames = CollectEnumNames(text);
        var symbols = CollectSymbolIndex(text, WorkspaceAmbientDeclarations.Get(GetBufferFilePath()));
        var lastIdentifier = string.Empty;
        var lastSignificant = '\0';

        for (var i = 0; i < text.Length;)
        {
            var current = text[i];
            var next = i + 1 < text.Length ? text[i + 1] : '\0';

            if (TryGetBlockStringLine(text, i, out var contentStart, out var contentEnd, out var nextLineStart))
            {
                if (TryCreateSpan(snapshot, spans, contentStart, contentEnd - contentStart, AuroraSyntaxClassificationTypes.String, out var blockStringTag))
                {
                    yield return blockStringTag;
                }

                lastIdentifier = string.Empty;
                lastSignificant = '\0';
                i = nextLineStart;
                continue;
            }

            if (char.IsWhiteSpace(current))
            {
                i++;
                continue;
            }

            if (current == '/' && next == '/')
            {
                i = SkipLineComment(text, i + 2);
                continue;
            }

            if (current == '/' && next == '*')
            {
                i = SkipBlockComment(text, i + 2);
                continue;
            }

            if (current == '"' || current == '\'' || current == '`')
            {
                var end = FindStringEnd(text, i, current);
                foreach (var tag in ScanString(snapshot, spans, text, i, end))
                {
                    yield return tag;
                }

                if (IsMapKey(text, i, end) || (isTypedDocument && IsTDocMapKey(text, i, end)))
                {
                    if (TryCreateSpan(snapshot, spans, i, end - i, AuroraSyntaxClassificationTypes.MapKey, out var mapKeyTag))
                    {
                        yield return mapKeyTag;
                    }
                }

                lastIdentifier = string.Empty;
                lastSignificant = current;
                i = end;
                continue;
            }

            if (IsIdentifierStart(current))
            {
                var start = i;
                i++;
                while (i < text.Length && IsIdentifierPart(text[i]))
                {
                    i++;
                }

                var value = text.Substring(start, i - start);
                var type = GetIdentifierClassification(text, start, i, value, enumNames, symbols, lastIdentifier, lastSignificant, isTypedDocument);
                if (!string.IsNullOrEmpty(type))
                {
                    if (TryCreateSpan(snapshot, spans, start, i - start, type, out var tag))
                    {
                        yield return tag;
                    }
                }

                lastIdentifier = value;
                lastSignificant = 'i';
                continue;
            }

            if (char.IsDigit(current))
            {
                var start = i;
                i = ScanNumber(text, i);
                var type = IsMapKey(text, start, i) || (isTypedDocument && IsTDocMapKey(text, start, i))
                    ? AuroraSyntaxClassificationTypes.MapKey
                    : AuroraSyntaxClassificationTypes.Number;
                if (TryCreateSpan(snapshot, spans, start, i - start, type, out var tag))
                {
                    yield return tag;
                }
                lastIdentifier = string.Empty;
                lastSignificant = '0';
                continue;
            }

            if (!char.IsWhiteSpace(current))
            {
                lastSignificant = current;
                if (current != '.')
                {
                    lastIdentifier = string.Empty;
                }
            }

            i++;
        }
    }

    private static ClassificationTag CreateTag(
        IClassificationTypeRegistryService classificationTypes,
        string name)
    {
        return new ClassificationTag(classificationTypes.GetClassificationType(name));
    }

    private bool TryCreateSpan(
        ITextSnapshot snapshot,
        NormalizedSnapshotSpanCollection spans,
        int start,
        int length,
        string type,
        out ITagSpan<ClassificationTag> tag)
    {
        if (length <= 0 || !Intersects(spans, start, length))
        {
            tag = null!;
            return false;
        }

        tag = new TagSpan<ClassificationTag>(new SnapshotSpan(snapshot, start, length), _tags[type]);
        return true;
    }

    private IEnumerable<ITagSpan<ClassificationTag>> ScanString(
        ITextSnapshot snapshot,
        NormalizedSnapshotSpanCollection spans,
        string text,
        int start,
        int end)
    {
        var segmentStart = start;
        var i = start + 1;
        while (i < end)
        {
            var current = text[i];
            if (current == '\\' && i + 1 < end)
            {
                if (i > segmentStart)
                {
                    if (TryCreateSpan(snapshot, spans, segmentStart, i - segmentStart, AuroraSyntaxClassificationTypes.String, out var stringTag))
                    {
                        yield return stringTag;
                    }
                }

                if (TryCreateSpan(snapshot, spans, i, 2, AuroraSyntaxClassificationTypes.Character, out var characterTag))
                {
                    yield return characterTag;
                }
                i += 2;
                segmentStart = i;
                continue;
            }

            i++;
        }

        if (i > segmentStart)
        {
            if (TryCreateSpan(snapshot, spans, segmentStart, i - segmentStart, AuroraSyntaxClassificationTypes.String, out var stringTag))
            {
                yield return stringTag;
            }
        }
    }

    private string? GetBufferFilePath()
    {
        if (_textDocuments == null)
        {
            return null;
        }

        return _textDocuments.TryGetTextDocument(_buffer, out var document)
            ? document.FilePath
            : null;
    }

    private bool IsTypedDocumentFile()
    {
        var path = GetBufferFilePath();
        return !string.IsNullOrWhiteSpace(path) &&
            string.Equals(Path.GetExtension(path), ".tdoc", StringComparison.OrdinalIgnoreCase);
    }

    private static Dictionary<string, bool> CollectEnumNames(string text)
    {
        var result = new Dictionary<string, bool>(StringComparer.Ordinal);
        for (var i = 0; i < text.Length;)
        {
            var current = text[i];
            var next = i + 1 < text.Length ? text[i + 1] : '\0';

            if (TryGetBlockStringLine(text, i, out _, out _, out var nextLineStart))
            {
                i = nextLineStart;
                continue;
            }

            if (current == '/' && next == '/')
            {
                i = SkipLineComment(text, i + 2);
                continue;
            }

            if (current == '/' && next == '*')
            {
                i = SkipBlockComment(text, i + 2);
                continue;
            }

            if (current == '"' || current == '\'' || current == '`')
            {
                i = SkipString(text, i, current);
                continue;
            }

            if (!IsIdentifierStart(current))
            {
                i++;
                continue;
            }

            var start = i;
            i++;
            while (i < text.Length && IsIdentifierPart(text[i]))
            {
                i++;
            }

            if (!string.Equals(text.Substring(start, i - start), "enum", StringComparison.Ordinal))
            {
                continue;
            }

            i = SkipWhitespace(text, i);
            if (i >= text.Length || !IsIdentifierStart(text[i]))
            {
                continue;
            }

            var enumStart = i;
            i++;
            while (i < text.Length && IsIdentifierPart(text[i]))
            {
                i++;
            }

            result[text.Substring(enumStart, i - enumStart)] = true;
        }

        return result;
    }

    private static LightweightSymbolIndex CollectSymbolIndex(
        string text,
        IReadOnlyDictionary<string, LightweightSymbolKind> ambientDeclarations)
    {
        var root = new LightweightScope(0, null)
        {
            End = text.Length
        };
        var index = new LightweightSymbolIndex(root);
        index.AddAmbientDeclarations(ambientDeclarations);
        var isGlobalDeclarationFile = IsGlobalDeclarationFileText(text);
        var currentScope = root;
        var bodyScopeSymbols = new Dictionary<int, List<string>>();

        for (var i = 0; i < text.Length;)
        {
            var current = text[i];
            var next = i + 1 < text.Length ? text[i + 1] : '\0';

            if (TryGetBlockStringLine(text, i, out _, out _, out var nextLineStart))
            {
                i = nextLineStart;
                continue;
            }

            if (char.IsWhiteSpace(current))
            {
                i++;
                continue;
            }

            if (current == '/' && next == '/')
            {
                i = SkipLineComment(text, i + 2);
                continue;
            }

            if (current == '/' && next == '*')
            {
                i = SkipBlockComment(text, i + 2);
                continue;
            }

            if (current == '"' || current == '\'' || current == '`')
            {
                i = SkipString(text, i, current);
                continue;
            }

            if (current == '{')
            {
                var child = new LightweightScope(i, currentScope);
                currentScope.Children.Add(child);
                if (bodyScopeSymbols.TryGetValue(i, out var names))
                {
                    for (var nameIndex = 0; nameIndex < names.Count; nameIndex++)
                    {
                        child.Declare(names[nameIndex], LightweightSymbolKind.Local);
                    }
                }

                currentScope = child;
                i++;
                continue;
            }

            if (current == '}')
            {
                currentScope.End = Math.Min(i + 1, text.Length);
                currentScope = currentScope.Parent ?? currentScope;
                i++;
                continue;
            }

            if (!IsIdentifierStart(current))
            {
                i++;
                continue;
            }

            var start = i;
            i++;
            while (i < text.Length && IsIdentifierPart(text[i]))
            {
                i++;
            }

            var value = text.Substring(start, i - start);
            switch (value)
            {
                case "declare":
                    if (isGlobalDeclarationFile)
                    {
                        CollectDeclareSymbol(text, i, index);
                    }
                    break;
                case "func":
                case "function":
                    if (!PreviousSignificantIdentifierIs(text, start, "declare"))
                    {
                        CollectFunctionSymbol(text, i, currentScope, bodyScopeSymbols, index);
                    }
                    break;
                case "var":
                case "const":
                    if (!PreviousSignificantIdentifierIs(text, start, "declare"))
                    {
                        CollectVariableSymbol(text, i, currentScope, index);
                    }
                    break;
                case "enum":
                    CollectSimpleNamedSymbol(text, i, currentScope, index);
                    break;
                case "import":
                    CollectImportSymbol(text, i, currentScope, index);
                    break;
                case "catch":
                    CollectCatchSymbol(text, i, bodyScopeSymbols, index);
                    break;
            }
        }

        for (var scope = currentScope; scope != null; scope = scope.Parent)
        {
            if (scope.End == int.MaxValue)
            {
                scope.End = text.Length;
            }
        }

        return index;
    }

    private static void CollectDeclareSymbol(string text, int start, LightweightSymbolIndex index)
    {
        if (TryReadDeclareSymbol(text, start, out var name, out var kind))
        {
            index.DeclareAmbient(name, kind);
        }
    }

    private static bool TryReadDeclareSymbol(
        string text,
        int start,
        out string name,
        out LightweightSymbolKind kind)
    {
        name = string.Empty;
        kind = LightweightSymbolKind.Local;

        var keywordStart = SkipTrivia(text, start);
        if (!TryReadIdentifier(text, keywordStart, out _, out var keywordEnd, out var keyword))
        {
            return false;
        }

        if (string.Equals(keyword, "func", StringComparison.Ordinal) ||
            string.Equals(keyword, "function", StringComparison.Ordinal))
        {
            var nameStart = SkipTrivia(text, keywordEnd);
            if (TryReadIdentifier(text, nameStart, out _, out _, out var declaredName))
            {
                name = declaredName;
                kind = LightweightSymbolKind.DeclaredGlobalFunction;
                return true;
            }

            return false;
        }

        if (string.Equals(keyword, "var", StringComparison.Ordinal) ||
            string.Equals(keyword, "const", StringComparison.Ordinal))
        {
            var nameStart = SkipTrivia(text, keywordEnd);
            if (TryReadIdentifier(text, nameStart, out _, out _, out var declaredName))
            {
                name = declaredName;
                kind = LightweightSymbolKind.DeclaredGlobal;
                return true;
            }
        }

        return false;
    }

    private static void CollectFunctionSymbol(
        string text,
        int start,
        LightweightScope scope,
        Dictionary<int, List<string>> bodyScopeSymbols,
        LightweightSymbolIndex index)
    {
        var nameStart = SkipTrivia(text, start);
        if (!TryReadIdentifier(text, nameStart, out var nameTokenStart, out var nameEnd, out var name))
        {
            return;
        }

        scope.Declare(name, LightweightSymbolKind.Local);
        index.AddLocalDeclarationSpan(nameTokenStart, nameEnd);

        var parameterStart = SkipTrivia(text, nameEnd);
        if (parameterStart >= text.Length || text[parameterStart] != '(')
        {
            return;
        }

        var parameterEnd = FindMatchingDelimiter(text, parameterStart, '(', ')');
        if (parameterEnd <= parameterStart)
        {
            return;
        }

        var parameters = CollectParameterSymbols(text, parameterStart + 1, parameterEnd, index);
        var bodyStart = SkipTrivia(text, parameterEnd + 1);
        if (bodyStart < text.Length && text[bodyStart] == '{')
        {
            AddBodyScopeSymbols(bodyScopeSymbols, bodyStart, parameters);
        }
    }

    private static void CollectVariableSymbol(
        string text,
        int start,
        LightweightScope scope,
        LightweightSymbolIndex index)
    {
        var nameStart = SkipTrivia(text, start);
        if (nameStart >= text.Length)
        {
            return;
        }

        if (TryReadIdentifier(text, nameStart, out var tokenStart, out var tokenEnd, out var name))
        {
            scope.Declare(name, LightweightSymbolKind.Local);
            index.AddLocalDeclarationSpan(tokenStart, tokenEnd);
            return;
        }

        if (text[nameStart] == '{' || text[nameStart] == '[')
        {
            CollectDestructuringSymbols(text, nameStart, scope, index);
        }
    }

    private static void CollectDestructuringSymbols(
        string text,
        int start,
        LightweightScope scope,
        LightweightSymbolIndex index)
    {
        var close = text[start] == '{' ? '}' : ']';
        var end = FindMatchingDelimiter(text, start, text[start], close);
        if (end <= start)
        {
            return;
        }

        for (var i = start + 1; i < end;)
        {
            var current = text[i];
            var next = i + 1 < text.Length ? text[i + 1] : '\0';
            if (char.IsWhiteSpace(current))
            {
                i++;
                continue;
            }

            if (current == '/' && next == '/')
            {
                i = SkipLineComment(text, i + 2);
                continue;
            }

            if (current == '/' && next == '*')
            {
                i = SkipBlockComment(text, i + 2);
                continue;
            }

            if (current == '"' || current == '\'' || current == '`')
            {
                i = SkipString(text, i, current);
                continue;
            }

            if (IsIdentifierStart(current))
            {
                var tokenStart = i;
                i++;
                while (i < end && IsIdentifierPart(text[i]))
                {
                    i++;
                }

                var name = text.Substring(tokenStart, i - tokenStart);
                scope.Declare(name, LightweightSymbolKind.Local);
                index.AddLocalDeclarationSpan(tokenStart, i);
                continue;
            }

            i++;
        }
    }

    private static void CollectSimpleNamedSymbol(
        string text,
        int start,
        LightweightScope scope,
        LightweightSymbolIndex index)
    {
        var nameStart = SkipTrivia(text, start);
        if (TryReadIdentifier(text, nameStart, out var tokenStart, out var tokenEnd, out var name))
        {
            scope.Declare(name, LightweightSymbolKind.Local);
            index.AddLocalDeclarationSpan(tokenStart, tokenEnd);
        }
    }

    private static void CollectImportSymbol(
        string text,
        int start,
        LightweightScope scope,
        LightweightSymbolIndex index)
    {
        var nameStart = SkipTrivia(text, start);
        if (TryReadIdentifier(text, nameStart, out var tokenStart, out var tokenEnd, out var name))
        {
            scope.Declare(name, LightweightSymbolKind.Local);
            index.AddLocalDeclarationSpan(tokenStart, tokenEnd);
        }
    }

    private static void CollectCatchSymbol(
        string text,
        int start,
        Dictionary<int, List<string>> bodyScopeSymbols,
        LightweightSymbolIndex index)
    {
        var parenStart = SkipTrivia(text, start);
        if (parenStart >= text.Length || text[parenStart] != '(')
        {
            return;
        }

        var nameStart = SkipTrivia(text, parenStart + 1);
        if (!TryReadIdentifier(text, nameStart, out var tokenStart, out var tokenEnd, out var name))
        {
            return;
        }

        index.AddLocalDeclarationSpan(tokenStart, tokenEnd);
        var parenEnd = FindMatchingDelimiter(text, parenStart, '(', ')');
        if (parenEnd <= parenStart)
        {
            return;
        }

        var bodyStart = SkipTrivia(text, parenEnd + 1);
        if (bodyStart < text.Length && text[bodyStart] == '{')
        {
            AddBodyScopeSymbols(bodyScopeSymbols, bodyStart, new List<string> { name });
        }
    }

    private static List<string> CollectParameterSymbols(
        string text,
        int start,
        int end,
        LightweightSymbolIndex index)
    {
        var names = new List<string>();
        var expectName = true;
        var depth = 0;

        for (var i = start; i < end;)
        {
            var current = text[i];
            var next = i + 1 < text.Length ? text[i + 1] : '\0';

            if (char.IsWhiteSpace(current))
            {
                i++;
                continue;
            }

            if (current == '/' && next == '/')
            {
                i = SkipLineComment(text, i + 2);
                continue;
            }

            if (current == '/' && next == '*')
            {
                i = SkipBlockComment(text, i + 2);
                continue;
            }

            if (current == '"' || current == '\'' || current == '`')
            {
                i = SkipString(text, i, current);
                continue;
            }

            if (current == '(' || current == '[' || current == '{')
            {
                depth++;
                i++;
                continue;
            }

            if (current == ')' || current == ']' || current == '}')
            {
                if (depth > 0)
                {
                    depth--;
                }

                i++;
                continue;
            }

            if (depth == 0 && current == ',')
            {
                expectName = true;
                i++;
                continue;
            }

            if (depth == 0 && expectName)
            {
                while (i < end && text[i] == '.')
                {
                    i++;
                }

                if (i < end && IsIdentifierStart(text[i]))
                {
                    var tokenStart = i;
                    i++;
                    while (i < end && IsIdentifierPart(text[i]))
                    {
                        i++;
                    }

                    names.Add(text.Substring(tokenStart, i - tokenStart));
                    index.AddLocalDeclarationSpan(tokenStart, i);
                    expectName = false;
                    continue;
                }
            }

            i++;
        }

        return names;
    }

    private static void AddBodyScopeSymbols(
        Dictionary<int, List<string>> bodyScopeSymbols,
        int bodyStart,
        List<string> names)
    {
        if (!bodyScopeSymbols.TryGetValue(bodyStart, out var existing))
        {
            existing = new List<string>();
            bodyScopeSymbols[bodyStart] = existing;
        }

        existing.AddRange(names);
    }

    private static string GetIdentifierClassification(
        string text,
        int start,
        int end,
        string value,
        IReadOnlyDictionary<string, bool> enumNames,
        LightweightSymbolIndex symbols,
        string lastIdentifier,
        char lastSignificant,
        bool isTypedDocument)
    {
        if (IsMapKey(text, start, end) || (isTypedDocument && IsTDocMapKey(text, start, end)))
        {
            return AuroraSyntaxClassificationTypes.MapKey;
        }

        if (isTypedDocument && (string.Equals(value, "readonly", StringComparison.Ordinal) ||
            string.Equals(value, "tdoc", StringComparison.Ordinal)))
        {
            return AuroraSyntaxClassificationTypes.Keyword;
        }

        if (lastSignificant == '.' && enumNames.ContainsKey(lastIdentifier))
        {
            return AuroraSyntaxClassificationTypes.EnumMember;
        }

        if (lastSignificant == '.' &&
            string.Equals(lastIdentifier, "global", StringComparison.Ordinal) &&
            !symbols.IsDeclared(start, "global") &&
            symbols.TryResolveAmbient(value, out var ambientClassification))
        {
            return ambientClassification;
        }

        if (lastSignificant == '.')
        {
            return NextNonWhitespaceIs(text, end, '(')
                ? AuroraSyntaxClassificationTypes.MethodCall
                : AuroraSyntaxClassificationTypes.Property;
        }

        if (symbols.IsLocalDeclarationSpan(start, end))
        {
            return string.Empty;
        }

        if (symbols.TryResolveExternal(start, value, out var externalClassification))
        {
            return externalClassification;
        }

        if (BuiltinVariables.Contains(value))
        {
            return AuroraSyntaxClassificationTypes.BuiltinVariable;
        }

        if (BuiltinObjects.Contains(value))
        {
            return AuroraSyntaxClassificationTypes.Object;
        }

        if (BuiltinTypes.Contains(value))
        {
            return AuroraSyntaxClassificationTypes.Type;
        }

        if (enumNames.ContainsKey(value))
        {
            return AuroraSyntaxClassificationTypes.Enum;
        }

        if (ControlFlowKeywords.Contains(value))
        {
            return AuroraSyntaxClassificationTypes.ControlFlow;
        }

        if (string.Equals(value, "return", StringComparison.Ordinal))
        {
            return AuroraSyntaxClassificationTypes.Return;
        }

        if (string.Equals(value, "throw", StringComparison.Ordinal))
        {
            return AuroraSyntaxClassificationTypes.Throw;
        }

        if (ExceptionKeywords.Contains(value))
        {
            return AuroraSyntaxClassificationTypes.Exception;
        }

        if (ImportExportKeywords.Contains(value))
        {
            return AuroraSyntaxClassificationTypes.ImportExport;
        }

        if (NextNonWhitespaceIs(text, end, '(') && !DeclarationKeywords.Contains(value))
        {
            return AuroraSyntaxClassificationTypes.FunctionCall;
        }

        return string.Empty;
    }

    private static string FindWorkspaceRoot(string filePath)
    {
        var directory = Path.GetDirectoryName(filePath);
        if (string.IsNullOrWhiteSpace(directory))
        {
            return string.Empty;
        }

        var current = directory;
        while (!string.IsNullOrWhiteSpace(current))
        {
            if (HasWorkspaceMarker(current))
            {
                return current;
            }

            var parent = Path.GetDirectoryName(current);
            if (string.IsNullOrWhiteSpace(parent) ||
                string.Equals(parent, current, StringComparison.OrdinalIgnoreCase))
            {
                break;
            }

            current = parent;
        }

        return directory;
    }

    private static bool HasWorkspaceMarker(string directory)
    {
        try
        {
            return ContainsFile(directory, "*.sln") ||
                ContainsFile(directory, "*.csproj") ||
                ContainsFile(directory, "*.asproj") ||
                ContainsDirectory(directory, ".git");
        }
        catch (Exception ex) when (IsFileReadFailure(ex))
        {
            return false;
        }
    }

    private static bool ContainsFile(string directory, string pattern)
    {
        foreach (var _ in Directory.EnumerateFiles(directory, pattern, SearchOption.TopDirectoryOnly))
        {
            return true;
        }

        return false;
    }

    private static bool ContainsDirectory(string directory, string name)
    {
        foreach (var _ in Directory.EnumerateDirectories(directory, name, SearchOption.TopDirectoryOnly))
        {
            return true;
        }

        return false;
    }

    private static IReadOnlyDictionary<string, LightweightSymbolKind> ScanWorkspaceAmbientDeclarations(string root)
    {
        if (string.IsNullOrWhiteSpace(root) || !Directory.Exists(root))
        {
            return EmptyAmbientDeclarations;
        }

        var declarations = new Dictionary<string, LightweightSymbolKind>(StringComparer.Ordinal);
        var count = 0;
        var pending = new Queue<string>();
        pending.Enqueue(root);
        while (pending.Count != 0)
        {
            var directory = pending.Dequeue();
            IEnumerable<string> childDirectories;
            try
            {
                childDirectories = Directory.EnumerateDirectories(directory).ToArray();
            }
            catch (Exception ex) when (IsFileReadFailure(ex))
            {
                continue;
            }

            foreach (var child in childDirectories)
            {
                if (!ShouldSkipWorkspaceDirectory(child))
                {
                    pending.Enqueue(child);
                }
            }

            IEnumerable<string> files;
            try
            {
                files = Directory.EnumerateFiles(directory, "*.as", SearchOption.TopDirectoryOnly).ToArray();
            }
            catch (Exception ex) when (IsFileReadFailure(ex))
            {
                continue;
            }

            foreach (var file in files)
            {
                if (++count > MaxWorkspaceDeclareFiles)
                {
                    return declarations.Count == 0 ? EmptyAmbientDeclarations : declarations;
                }

                try
                {
                    CollectAmbientDeclarationsFromText(File.ReadAllText(file), declarations);
                }
                catch (Exception ex) when (IsFileReadFailure(ex))
                {
                }
            }
        }

        return declarations.Count == 0 ? EmptyAmbientDeclarations : declarations;
    }

    private static void CollectAmbientDeclarationsFromText(
        string text,
        Dictionary<string, LightweightSymbolKind> declarations)
    {
        if (!IsGlobalDeclarationFileText(text))
        {
            return;
        }

        for (var i = 0; i < text.Length;)
        {
            var current = text[i];
            var next = i + 1 < text.Length ? text[i + 1] : '\0';

            if (TryGetBlockStringLine(text, i, out _, out _, out var nextLineStart))
            {
                i = nextLineStart;
                continue;
            }

            if (char.IsWhiteSpace(current))
            {
                i++;
                continue;
            }

            if (current == '/' && next == '/')
            {
                i = SkipLineComment(text, i + 2);
                continue;
            }

            if (current == '/' && next == '*')
            {
                i = SkipBlockComment(text, i + 2);
                continue;
            }

            if (current == '"' || current == '\'' || current == '`')
            {
                i = SkipString(text, i, current);
                continue;
            }

            if (!IsIdentifierStart(current))
            {
                i++;
                continue;
            }

            var start = i;
            i++;
            while (i < text.Length && IsIdentifierPart(text[i]))
            {
                i++;
            }

            if (string.Equals(text.Substring(start, i - start), "declare", StringComparison.Ordinal) &&
                TryReadDeclareSymbol(text, i, out var name, out var kind) &&
                !declarations.ContainsKey(name))
            {
                declarations.Add(name, kind);
            }
        }
    }

    private static bool IsGlobalDeclarationFileText(string text)
    {
        var i = SkipTrivia(text, 0);
        if (i >= text.Length || text[i] != '@')
        {
            return false;
        }

        i++;
        if (!TryReadIdentifier(text, i, out _, out var nameEnd, out var name) ||
            !string.Equals(name, "global", StringComparison.Ordinal))
        {
            return false;
        }

        i = SkipTrivia(text, nameEnd);
        if (i >= text.Length || text[i] != '(')
        {
            return false;
        }

        var close = FindMatchingDelimiter(text, i, '(', ')');
        if (close <= i)
        {
            return false;
        }

        i = SkipTrivia(text, close + 1);
        return i < text.Length && text[i] == ';';
    }

    private static bool ShouldSkipWorkspaceDirectory(string path)
    {
        var name = Path.GetFileName(path);
        return string.Equals(name, ".git", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(name, ".vs", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(name, "bin", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(name, "obj", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(name, "node_modules", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsFileReadFailure(Exception exception)
    {
        return exception is IOException
            or UnauthorizedAccessException
            or DirectoryNotFoundException
            or FileNotFoundException
            or PathTooLongException
            or NotSupportedException
            or ArgumentException;
    }

    private static bool TryReadIdentifier(
        string text,
        int start,
        out int tokenStart,
        out int tokenEnd,
        out string value)
    {
        tokenStart = start;
        tokenEnd = start;
        value = string.Empty;

        if (start >= text.Length || !IsIdentifierStart(text[start]))
        {
            return false;
        }

        tokenEnd = start + 1;
        while (tokenEnd < text.Length && IsIdentifierPart(text[tokenEnd]))
        {
            tokenEnd++;
        }

        value = text.Substring(start, tokenEnd - start);
        return true;
    }

    private static bool PreviousSignificantIdentifierIs(string text, int start, string expected)
    {
        var i = SkipBackwardTrivia(text, start - 1);

        if (i < 0 || !IsIdentifierPart(text[i]))
        {
            return false;
        }

        var end = i + 1;
        while (i >= 0 && IsIdentifierPart(text[i]))
        {
            i--;
        }

        var previous = text.Substring(i + 1, end - i - 1);
        return string.Equals(previous, expected, StringComparison.Ordinal);
    }

    private static int SkipBackwardTrivia(string text, int start)
    {
        var i = start;
        while (i >= 0)
        {
            while (i >= 0 && char.IsWhiteSpace(text[i]))
            {
                i--;
            }

            if (i > 0 && text[i] == '/' && text[i - 1] == '*')
            {
                i -= 2;
                while (i > 0 && !(text[i - 1] == '/' && text[i] == '*'))
                {
                    i--;
                }

                if (i > 0)
                {
                    i -= 2;
                    continue;
                }
            }

            break;
        }

        return i;
    }

    private void OnBufferChanged(object sender, TextContentChangedEventArgs e)
    {
        var snapshot = e.After;
        if (snapshot.Length == 0)
        {
            return;
        }

        TagsChanged?.Invoke(this, new SnapshotSpanEventArgs(new SnapshotSpan(snapshot, 0, snapshot.Length)));
    }

    private static bool IsMapKey(string text, int start, int end)
    {
        var next = SkipWhitespace(text, end);
        if (next >= text.Length || text[next] != ':')
        {
            return false;
        }

        for (var i = start - 1; i >= 0; i--)
        {
            if (char.IsWhiteSpace(text[i]))
            {
                continue;
            }

            return text[i] == '{' || text[i] == ',';
        }

        return false;
    }

    private static bool IsTDocMapKey(string text, int start, int end)
    {
        var valueStart = SkipWhitespace(text, end);
        if (valueStart >= text.Length || !IsTDocValueStart(text, valueStart))
        {
            return false;
        }

        var value = text.Substring(start, end - start);
        var previous = PreviousSignificant(text, start - 1);
        if (previous < 0)
        {
            return false;
        }

        if (text[previous] == '{' || text[previous] == ',')
        {
            return !BuiltinTypes.Contains(value);
        }

        if (IsIdentifierPart(text[previous]))
        {
            var previousStart = previous;
            while (previousStart > 0 && IsIdentifierPart(text[previousStart - 1]))
            {
                previousStart--;
            }

            var previousValue = text.Substring(previousStart, previous - previousStart + 1);
            return (BuiltinTypes.Contains(previousValue) ||
                string.Equals(previousValue, "readonly", StringComparison.Ordinal)) &&
                !BuiltinTypes.Contains(value);
        }

        return false;
    }

    private static bool IsTDocValueStart(string text, int start)
    {
        if (start >= text.Length)
        {
            return false;
        }

        var value = text[start];
        return value == '"' || value == '\'' || value == '{' || value == '[' ||
            value == '-' || value == '$' || char.IsDigit(value) ||
            StartsWithIdentifier(text, start, "true") ||
            StartsWithIdentifier(text, start, "false") ||
            StartsWithIdentifier(text, start, "null");
    }

    private static bool StartsWithIdentifier(string text, int start, string value)
    {
        if (start + value.Length > text.Length ||
            !string.Equals(text.Substring(start, value.Length), value, StringComparison.Ordinal))
        {
            return false;
        }

        return start + value.Length >= text.Length || !IsIdentifierPart(text[start + value.Length]);
    }

    private static int PreviousSignificant(string text, int start)
    {
        for (var i = start; i >= 0; i--)
        {
            if (!char.IsWhiteSpace(text[i]))
            {
                return i;
            }
        }

        return -1;
    }

    private static bool NextNonWhitespaceIs(string text, int start, char expected)
    {
        var next = SkipWhitespace(text, start);
        return next < text.Length && text[next] == expected;
    }

    private static int SkipTrivia(string text, int start)
    {
        while (start < text.Length)
        {
            if (char.IsWhiteSpace(text[start]))
            {
                start++;
                continue;
            }

            if (start + 1 < text.Length && text[start] == '/' && text[start + 1] == '/')
            {
                start = SkipLineComment(text, start + 2);
                continue;
            }

            if (start + 1 < text.Length && text[start] == '/' && text[start + 1] == '*')
            {
                start = SkipBlockComment(text, start + 2);
                continue;
            }

            break;
        }

        return start;
    }

    private static int SkipWhitespace(string text, int start)
    {
        while (start < text.Length && char.IsWhiteSpace(text[start]))
        {
            start++;
        }

        return start;
    }

    private static int FindMatchingDelimiter(string text, int start, char open, char close)
    {
        var depth = 0;
        for (var i = start; i < text.Length;)
        {
            var current = text[i];
            var next = i + 1 < text.Length ? text[i + 1] : '\0';

            if (current == '/' && next == '/')
            {
                i = SkipLineComment(text, i + 2);
                continue;
            }

            if (current == '/' && next == '*')
            {
                i = SkipBlockComment(text, i + 2);
                continue;
            }

            if (current == '"' || current == '\'' || current == '`')
            {
                i = SkipString(text, i, current);
                continue;
            }

            if (current == open)
            {
                depth++;
                i++;
                continue;
            }

            if (current == close)
            {
                depth--;
                if (depth == 0)
                {
                    return i;
                }
            }

            i++;
        }

        return -1;
    }

    private static bool TryGetBlockStringLine(
        string text,
        int start,
        out int contentStart,
        out int contentEnd,
        out int nextLineStart)
    {
        contentStart = 0;
        contentEnd = 0;
        nextLineStart = start;

        if (!IsLineStart(text, start))
        {
            return false;
        }

        var markerStart = start;
        while (markerStart < text.Length && (text[markerStart] == ' ' || text[markerStart] == '\t'))
        {
            markerStart++;
        }

        if (markerStart + 2 >= text.Length ||
            text[markerStart] != '|' ||
            text[markerStart + 1] != '>' ||
            text[markerStart + 2] != ' ')
        {
            return false;
        }

        contentStart = markerStart + 3;
        contentEnd = contentStart;
        while (contentEnd < text.Length && text[contentEnd] != '\r' && text[contentEnd] != '\n')
        {
            contentEnd++;
        }

        nextLineStart = contentEnd;
        if (nextLineStart < text.Length)
        {
            if (text[nextLineStart] == '\r' && nextLineStart + 1 < text.Length && text[nextLineStart + 1] == '\n')
            {
                nextLineStart += 2;
            }
            else
            {
                nextLineStart++;
            }
        }

        return true;
    }

    private static bool IsLineStart(string text, int position)
    {
        return position == 0 ||
            (position > 0 && (text[position - 1] == '\n' || text[position - 1] == '\r'));
    }

    private static int ScanNumber(string text, int start)
    {
        var i = start;
        if (i + 1 < text.Length && text[i] == '0' && (text[i + 1] == 'x' || text[i + 1] == 'X'))
        {
            i += 2;
            while (i < text.Length && IsHexDigitOrSeparator(text[i]))
            {
                i++;
            }

            return i;
        }

        while (i < text.Length && (char.IsDigit(text[i]) || text[i] == '_'))
        {
            i++;
        }

        if (i < text.Length && text[i] == '.')
        {
            i++;
            while (i < text.Length && (char.IsDigit(text[i]) || text[i] == '_'))
            {
                i++;
            }
        }

        return i;
    }

    private static bool IsIdentifierStart(char value)
    {
        return (value >= 'a' && value <= 'z') ||
            (value >= 'A' && value <= 'Z') ||
            value == '_' ||
            value == '$' ||
            (value >= 0x4e00 && value <= 0x9fbb);
    }

    private static bool IsIdentifierPart(char value)
    {
        return IsIdentifierStart(value) || (value >= '0' && value <= '9');
    }

    private static bool IsHexDigitOrSeparator(char value)
    {
        return (value >= '0' && value <= '9') ||
            (value >= 'a' && value <= 'f') ||
            (value >= 'A' && value <= 'F') ||
            value == '_';
    }

    private static int SkipLineComment(string text, int start)
    {
        while (start < text.Length && text[start] != '\r' && text[start] != '\n')
        {
            start++;
        }

        return start;
    }

    private static int SkipBlockComment(string text, int start)
    {
        while (start + 1 < text.Length)
        {
            if (text[start] == '*' && text[start + 1] == '/')
            {
                return start + 2;
            }

            start++;
        }

        return text.Length;
    }

    private static int SkipString(string text, int start, char quote)
    {
        return FindStringEnd(text, start, quote);
    }

    private static int FindStringEnd(string text, int start, char quote)
    {
        start++;
        while (start < text.Length)
        {
            if (text[start] == '\\' && start + 1 < text.Length)
            {
                start += 2;
                continue;
            }

            if (text[start] == quote)
            {
                return start + 1;
            }

            if (quote != '`' && (text[start] == '\r' || text[start] == '\n'))
            {
                return start;
            }

            start++;
        }

        return start;
    }

    private static bool Intersects(NormalizedSnapshotSpanCollection spans, int start, int length)
    {
        var end = start + length;
        for (var i = 0; i < spans.Count; i++)
        {
            var span = spans[i];
            if (start < span.End.Position && end > span.Start.Position)
            {
                return true;
            }

            if (end <= span.Start.Position)
            {
                return false;
            }
        }

        return false;
    }
}

[System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE0051:Remove unused private members", Justification = "MEF composition fields are discovered by Visual Studio.")]
internal static class AuroraSyntaxClassificationDefinitions
{
    [Export(typeof(ClassificationTypeDefinition))]
    [Name(AuroraSyntaxClassificationTypes.Object)]
    [BaseDefinition("text")]
    internal static ClassificationTypeDefinition? ObjectClassificationType;

    [Export(typeof(ClassificationTypeDefinition))]
    [Name(AuroraSyntaxClassificationTypes.Type)]
    [BaseDefinition("text")]
    internal static ClassificationTypeDefinition? TypeClassificationType;

    [Export(typeof(ClassificationTypeDefinition))]
    [Name(AuroraSyntaxClassificationTypes.FunctionCall)]
    [BaseDefinition("text")]
    internal static ClassificationTypeDefinition? FunctionCallClassificationType;

    [Export(typeof(ClassificationTypeDefinition))]
    [Name(AuroraSyntaxClassificationTypes.MethodCall)]
    [BaseDefinition("text")]
    internal static ClassificationTypeDefinition? MethodCallClassificationType;

    [Export(typeof(ClassificationTypeDefinition))]
    [Name(AuroraSyntaxClassificationTypes.Property)]
    [BaseDefinition("text")]
    internal static ClassificationTypeDefinition? PropertyClassificationType;

    [Export(typeof(ClassificationTypeDefinition))]
    [Name(AuroraSyntaxClassificationTypes.MapKey)]
    [BaseDefinition("text")]
    internal static ClassificationTypeDefinition? MapKeyClassificationType;

    [Export(typeof(ClassificationTypeDefinition))]
    [Name(AuroraSyntaxClassificationTypes.BuiltinVariable)]
    [BaseDefinition("text")]
    internal static ClassificationTypeDefinition? BuiltinVariableClassificationType;

    [Export(typeof(ClassificationTypeDefinition))]
    [Name(AuroraSyntaxClassificationTypes.DeclaredGlobal)]
    [BaseDefinition("text")]
    internal static ClassificationTypeDefinition? DeclaredGlobalClassificationType;

    [Export(typeof(ClassificationTypeDefinition))]
    [Name(AuroraSyntaxClassificationTypes.DeclaredGlobalFunction)]
    [BaseDefinition("text")]
    internal static ClassificationTypeDefinition? DeclaredGlobalFunctionClassificationType;

    [Export(typeof(ClassificationTypeDefinition))]
    [Name(AuroraSyntaxClassificationTypes.ControlFlow)]
    [BaseDefinition("text")]
    internal static ClassificationTypeDefinition? ControlFlowClassificationType;

    [Export(typeof(ClassificationTypeDefinition))]
    [Name(AuroraSyntaxClassificationTypes.Return)]
    [BaseDefinition("text")]
    internal static ClassificationTypeDefinition? ReturnClassificationType;

    [Export(typeof(ClassificationTypeDefinition))]
    [Name(AuroraSyntaxClassificationTypes.Throw)]
    [BaseDefinition("text")]
    internal static ClassificationTypeDefinition? ThrowClassificationType;

    [Export(typeof(ClassificationTypeDefinition))]
    [Name(AuroraSyntaxClassificationTypes.Exception)]
    [BaseDefinition("text")]
    internal static ClassificationTypeDefinition? ExceptionClassificationType;

    [Export(typeof(ClassificationTypeDefinition))]
    [Name(AuroraSyntaxClassificationTypes.ImportExport)]
    [BaseDefinition("text")]
    internal static ClassificationTypeDefinition? ImportExportClassificationType;

    [Export(typeof(ClassificationTypeDefinition))]
    [Name(AuroraSyntaxClassificationTypes.Enum)]
    [BaseDefinition("text")]
    internal static ClassificationTypeDefinition? EnumClassificationType;

    [Export(typeof(ClassificationTypeDefinition))]
    [Name(AuroraSyntaxClassificationTypes.EnumMember)]
    [BaseDefinition("text")]
    internal static ClassificationTypeDefinition? EnumMemberClassificationType;

    [Export(typeof(ClassificationTypeDefinition))]
    [Name(AuroraSyntaxClassificationTypes.String)]
    [BaseDefinition("text")]
    internal static ClassificationTypeDefinition? StringClassificationType;

    [Export(typeof(ClassificationTypeDefinition))]
    [Name(AuroraSyntaxClassificationTypes.Character)]
    [BaseDefinition("text")]
    internal static ClassificationTypeDefinition? CharacterClassificationType;

    [Export(typeof(ClassificationTypeDefinition))]
    [Name(AuroraSyntaxClassificationTypes.Number)]
    [BaseDefinition("text")]
    internal static ClassificationTypeDefinition? NumberClassificationType;

    [Export(typeof(ClassificationTypeDefinition))]
    [Name(AuroraSyntaxClassificationTypes.Keyword)]
    [BaseDefinition("text")]
    internal static ClassificationTypeDefinition? KeywordClassificationType;
}

internal abstract class AuroraSyntaxFormatDefinition : ClassificationFormatDefinition
{
    protected AuroraSyntaxFormatDefinition(string displayName, byte red, byte green, byte blue)
    {
        DisplayName = displayName;
        ForegroundColor = Color.FromRgb(red, green, blue);
    }
}

[Export(typeof(EditorFormatDefinition))]
[ClassificationType(ClassificationTypeNames = AuroraSyntaxClassificationTypes.Object)]
[Name(AuroraSyntaxClassificationTypes.Object)]
[UserVisible(true)]
internal sealed class AuroraObjectFormat : AuroraSyntaxFormatDefinition
{
    public AuroraObjectFormat() : base("AuroraScript Object", 0x4E, 0xC9, 0xB0) { }
}

[Export(typeof(EditorFormatDefinition))]
[ClassificationType(ClassificationTypeNames = AuroraSyntaxClassificationTypes.Type)]
[Name(AuroraSyntaxClassificationTypes.Type)]
[UserVisible(true)]
internal sealed class AuroraTypeFormat : AuroraSyntaxFormatDefinition
{
    public AuroraTypeFormat() : base("AuroraScript Type", 0x4E, 0xC9, 0xB0) { }
}

[Export(typeof(EditorFormatDefinition))]
[ClassificationType(ClassificationTypeNames = AuroraSyntaxClassificationTypes.FunctionCall)]
[Name(AuroraSyntaxClassificationTypes.FunctionCall)]
[UserVisible(true)]
internal sealed class AuroraFunctionCallFormat : AuroraSyntaxFormatDefinition
{
    public AuroraFunctionCallFormat() : base("AuroraScript Function Call", 0xDC, 0xDC, 0xAA) { }
}

[Export(typeof(EditorFormatDefinition))]
[ClassificationType(ClassificationTypeNames = AuroraSyntaxClassificationTypes.MethodCall)]
[Name(AuroraSyntaxClassificationTypes.MethodCall)]
[UserVisible(true)]
internal sealed class AuroraMethodCallFormat : AuroraSyntaxFormatDefinition
{
    public AuroraMethodCallFormat() : base("AuroraScript Method Call", 0xDC, 0xDC, 0xAA) { }
}

[Export(typeof(EditorFormatDefinition))]
[ClassificationType(ClassificationTypeNames = AuroraSyntaxClassificationTypes.Property)]
[Name(AuroraSyntaxClassificationTypes.Property)]
[UserVisible(true)]
internal sealed class AuroraPropertyFormat : AuroraSyntaxFormatDefinition
{
    public AuroraPropertyFormat() : base("AuroraScript Property", 0x9C, 0xDC, 0xFE) { }
}

[Export(typeof(EditorFormatDefinition))]
[ClassificationType(ClassificationTypeNames = AuroraSyntaxClassificationTypes.MapKey)]
[Name(AuroraSyntaxClassificationTypes.MapKey)]
[UserVisible(true)]
internal sealed class AuroraMapKeyFormat : AuroraSyntaxFormatDefinition
{
    public AuroraMapKeyFormat() : base("AuroraScript Map Key", 0x9C, 0xDC, 0xFE) { }
}

[Export(typeof(EditorFormatDefinition))]
[ClassificationType(ClassificationTypeNames = AuroraSyntaxClassificationTypes.BuiltinVariable)]
[Name(AuroraSyntaxClassificationTypes.BuiltinVariable)]
[UserVisible(true)]
internal sealed class AuroraBuiltinVariableFormat : AuroraSyntaxFormatDefinition
{
    public AuroraBuiltinVariableFormat() : base("AuroraScript Built-in Variable", 0xC5, 0x86, 0xC0) { }
}

[Export(typeof(EditorFormatDefinition))]
[ClassificationType(ClassificationTypeNames = AuroraSyntaxClassificationTypes.DeclaredGlobal)]
[Name(AuroraSyntaxClassificationTypes.DeclaredGlobal)]
[UserVisible(true)]
internal sealed class AuroraDeclaredGlobalFormat : AuroraSyntaxFormatDefinition
{
    public AuroraDeclaredGlobalFormat() : base("AuroraScript Declared Global", 0x24, 0xFD, 0xB5) { }
}

[Export(typeof(EditorFormatDefinition))]
[ClassificationType(ClassificationTypeNames = AuroraSyntaxClassificationTypes.DeclaredGlobalFunction)]
[Name(AuroraSyntaxClassificationTypes.DeclaredGlobalFunction)]
[UserVisible(true)]
internal sealed class AuroraDeclaredGlobalFunctionFormat : AuroraSyntaxFormatDefinition
{
    public AuroraDeclaredGlobalFunctionFormat() : base("AuroraScript Declared Global Function", 0xF3, 0x8B, 0x00) { }
}

[Export(typeof(EditorFormatDefinition))]
[ClassificationType(ClassificationTypeNames = AuroraSyntaxClassificationTypes.ControlFlow)]
[Name(AuroraSyntaxClassificationTypes.ControlFlow)]
[UserVisible(true)]
internal sealed class AuroraControlFlowFormat : AuroraSyntaxFormatDefinition
{
    public AuroraControlFlowFormat() : base("AuroraScript Control Flow", 0xC5, 0x86, 0xC0) { }
}

[Export(typeof(EditorFormatDefinition))]
[ClassificationType(ClassificationTypeNames = AuroraSyntaxClassificationTypes.Return)]
[Name(AuroraSyntaxClassificationTypes.Return)]
[UserVisible(true)]
internal sealed class AuroraReturnFormat : AuroraSyntaxFormatDefinition
{
    public AuroraReturnFormat() : base("AuroraScript Return", 0xC5, 0x86, 0xC0) { }
}

[Export(typeof(EditorFormatDefinition))]
[ClassificationType(ClassificationTypeNames = AuroraSyntaxClassificationTypes.Throw)]
[Name(AuroraSyntaxClassificationTypes.Throw)]
[UserVisible(true)]
internal sealed class AuroraThrowFormat : AuroraSyntaxFormatDefinition
{
    public AuroraThrowFormat() : base("AuroraScript Throw", 0xC5, 0x86, 0xC0) { }
}

[Export(typeof(EditorFormatDefinition))]
[ClassificationType(ClassificationTypeNames = AuroraSyntaxClassificationTypes.Exception)]
[Name(AuroraSyntaxClassificationTypes.Exception)]
[UserVisible(true)]
internal sealed class AuroraExceptionFormat : AuroraSyntaxFormatDefinition
{
    public AuroraExceptionFormat() : base("AuroraScript Exception", 0xC5, 0x86, 0xC0) { }
}

[Export(typeof(EditorFormatDefinition))]
[ClassificationType(ClassificationTypeNames = AuroraSyntaxClassificationTypes.ImportExport)]
[Name(AuroraSyntaxClassificationTypes.ImportExport)]
[UserVisible(true)]
internal sealed class AuroraImportExportFormat : AuroraSyntaxFormatDefinition
{
    public AuroraImportExportFormat() : base("AuroraScript Import/Export", 0xC5, 0x86, 0xC0) { }
}

[Export(typeof(EditorFormatDefinition))]
[ClassificationType(ClassificationTypeNames = AuroraSyntaxClassificationTypes.Enum)]
[Name(AuroraSyntaxClassificationTypes.Enum)]
[UserVisible(true)]
internal sealed class AuroraEnumFormat : AuroraSyntaxFormatDefinition
{
    public AuroraEnumFormat() : base("AuroraScript Enum", 0x4E, 0xC9, 0xB0) { }
}

[Export(typeof(EditorFormatDefinition))]
[ClassificationType(ClassificationTypeNames = AuroraSyntaxClassificationTypes.EnumMember)]
[Name(AuroraSyntaxClassificationTypes.EnumMember)]
[UserVisible(true)]
internal sealed class AuroraEnumMemberFormat : AuroraSyntaxFormatDefinition
{
    public AuroraEnumMemberFormat() : base("AuroraScript Enum Member", 0xD7, 0xBA, 0x7D) { }
}

[Export(typeof(EditorFormatDefinition))]
[ClassificationType(ClassificationTypeNames = AuroraSyntaxClassificationTypes.String)]
[Name(AuroraSyntaxClassificationTypes.String)]
[UserVisible(true)]
internal sealed class AuroraStringFormat : AuroraSyntaxFormatDefinition
{
    public AuroraStringFormat() : base("AuroraScript String", 0xCE, 0x91, 0x78) { }
}

[Export(typeof(EditorFormatDefinition))]
[ClassificationType(ClassificationTypeNames = AuroraSyntaxClassificationTypes.Character)]
[Name(AuroraSyntaxClassificationTypes.Character)]
[UserVisible(true)]
internal sealed class AuroraCharacterFormat : AuroraSyntaxFormatDefinition
{
    public AuroraCharacterFormat() : base("AuroraScript Character/Escape", 0xD7, 0xBA, 0x7D) { }
}

[Export(typeof(EditorFormatDefinition))]
[ClassificationType(ClassificationTypeNames = AuroraSyntaxClassificationTypes.Number)]
[Name(AuroraSyntaxClassificationTypes.Number)]
[UserVisible(true)]
internal sealed class AuroraNumberFormat : AuroraSyntaxFormatDefinition
{
    public AuroraNumberFormat() : base("AuroraScript Number", 0xB5, 0xCE, 0xA8) { }
}

[Export(typeof(EditorFormatDefinition))]
[ClassificationType(ClassificationTypeNames = AuroraSyntaxClassificationTypes.Keyword)]
[Name(AuroraSyntaxClassificationTypes.Keyword)]
[UserVisible(true)]
internal sealed class AuroraKeywordFormat : AuroraSyntaxFormatDefinition
{
    public AuroraKeywordFormat() : base("AuroraScript Keyword", 0xC5, 0x86, 0xC0) { }
}
