using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Classification;
using Microsoft.VisualStudio.Text.Tagging;
using Microsoft.VisualStudio.Utilities;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
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
}

[Export(typeof(ITaggerProvider))]
[ContentType(AuroraContentTypeDefinition.ContentTypeName)]
[TagType(typeof(ClassificationTag))]
internal sealed class AuroraSyntaxTaggerProvider : ITaggerProvider
{
    [Import]
    internal IClassificationTypeRegistryService ClassificationTypes = null!;

    public ITagger<T>? CreateTagger<T>(ITextBuffer buffer)
        where T : ITag
    {
        return new AuroraSyntaxTagger(buffer, ClassificationTypes) as ITagger<T>;
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
        "Date",
        "Error",
        "Function",
        "HashMap",
        "Number",
        "Object",
        "Path",
        "Proxy",
        "Regex",
        "String",
        "StringBuffer"
    };

    private readonly ITextBuffer _buffer;
    private readonly Dictionary<string, ClassificationTag> _tags;

    public AuroraSyntaxTagger(
        ITextBuffer buffer,
        IClassificationTypeRegistryService classificationTypes)
    {
        _buffer = buffer;
        _tags = new Dictionary<string, ClassificationTag>(StringComparer.Ordinal)
        {
            [AuroraSyntaxClassificationTypes.Object] = CreateTag(classificationTypes, AuroraSyntaxClassificationTypes.Object),
            [AuroraSyntaxClassificationTypes.Type] = CreateTag(classificationTypes, AuroraSyntaxClassificationTypes.Type),
            [AuroraSyntaxClassificationTypes.FunctionCall] = CreateTag(classificationTypes, AuroraSyntaxClassificationTypes.FunctionCall),
            [AuroraSyntaxClassificationTypes.MethodCall] = CreateTag(classificationTypes, AuroraSyntaxClassificationTypes.MethodCall),
            [AuroraSyntaxClassificationTypes.Property] = CreateTag(classificationTypes, AuroraSyntaxClassificationTypes.Property),
            [AuroraSyntaxClassificationTypes.MapKey] = CreateTag(classificationTypes, AuroraSyntaxClassificationTypes.MapKey),
            [AuroraSyntaxClassificationTypes.BuiltinVariable] = CreateTag(classificationTypes, AuroraSyntaxClassificationTypes.BuiltinVariable),
            [AuroraSyntaxClassificationTypes.ControlFlow] = CreateTag(classificationTypes, AuroraSyntaxClassificationTypes.ControlFlow),
            [AuroraSyntaxClassificationTypes.Return] = CreateTag(classificationTypes, AuroraSyntaxClassificationTypes.Return),
            [AuroraSyntaxClassificationTypes.Throw] = CreateTag(classificationTypes, AuroraSyntaxClassificationTypes.Throw),
            [AuroraSyntaxClassificationTypes.Exception] = CreateTag(classificationTypes, AuroraSyntaxClassificationTypes.Exception),
            [AuroraSyntaxClassificationTypes.ImportExport] = CreateTag(classificationTypes, AuroraSyntaxClassificationTypes.ImportExport),
            [AuroraSyntaxClassificationTypes.Enum] = CreateTag(classificationTypes, AuroraSyntaxClassificationTypes.Enum),
            [AuroraSyntaxClassificationTypes.EnumMember] = CreateTag(classificationTypes, AuroraSyntaxClassificationTypes.EnumMember),
            [AuroraSyntaxClassificationTypes.String] = CreateTag(classificationTypes, AuroraSyntaxClassificationTypes.String),
            [AuroraSyntaxClassificationTypes.Character] = CreateTag(classificationTypes, AuroraSyntaxClassificationTypes.Character),
            [AuroraSyntaxClassificationTypes.Number] = CreateTag(classificationTypes, AuroraSyntaxClassificationTypes.Number)
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
        var enumNames = CollectEnumNames(text);
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

                if (IsMapKey(text, i, end))
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
                var type = GetIdentifierClassification(text, start, i, value, enumNames, lastIdentifier, lastSignificant);
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
                var type = IsMapKey(text, start, i)
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
        if (length <= 0 || !Intersects(spans, start))
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

    private static string GetIdentifierClassification(
        string text,
        int start,
        int end,
        string value,
        IReadOnlyDictionary<string, bool> enumNames,
        string lastIdentifier,
        char lastSignificant)
    {
        if (IsMapKey(text, start, end))
        {
            return AuroraSyntaxClassificationTypes.MapKey;
        }

        if (lastSignificant == '.' && enumNames.ContainsKey(lastIdentifier))
        {
            return AuroraSyntaxClassificationTypes.EnumMember;
        }

        if (lastSignificant == '.')
        {
            return NextNonWhitespaceIs(text, end, '(')
                ? AuroraSyntaxClassificationTypes.MethodCall
                : AuroraSyntaxClassificationTypes.Property;
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

    private static bool NextNonWhitespaceIs(string text, int start, char expected)
    {
        var next = SkipWhitespace(text, start);
        return next < text.Length && text[next] == expected;
    }

    private static int SkipWhitespace(string text, int start)
    {
        while (start < text.Length && char.IsWhiteSpace(text[start]))
        {
            start++;
        }

        return start;
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

    private static bool Intersects(NormalizedSnapshotSpanCollection spans, int position)
    {
        for (var i = 0; i < spans.Count; i++)
        {
            var span = spans[i];
            if (position >= span.Start.Position && position < span.End.Position)
            {
                return true;
            }

            if (position < span.Start.Position)
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
    public AuroraReturnFormat() : base("AuroraScript Return", 0xD1, 0x69, 0x69) { }
}

[Export(typeof(EditorFormatDefinition))]
[ClassificationType(ClassificationTypeNames = AuroraSyntaxClassificationTypes.Throw)]
[Name(AuroraSyntaxClassificationTypes.Throw)]
[UserVisible(true)]
internal sealed class AuroraThrowFormat : AuroraSyntaxFormatDefinition
{
    public AuroraThrowFormat() : base("AuroraScript Throw", 0xD1, 0x69, 0x69) { }
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
