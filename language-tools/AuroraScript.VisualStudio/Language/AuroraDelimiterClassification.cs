using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Classification;
using Microsoft.VisualStudio.Text.Tagging;
using Microsoft.VisualStudio.Utilities;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Windows.Media;

namespace AuroraScript.VisualStudio.Language;

internal static class AuroraDelimiterClassificationTypes
{
    public const string BraceLevel1 = "AuroraScript.BraceLevel1";
    public const string BraceLevel2 = "AuroraScript.BraceLevel2";
    public const string BraceLevel3 = "AuroraScript.BraceLevel3";
    public const string BraceLevel4 = "AuroraScript.BraceLevel4";
    public const string BraceLevel5 = "AuroraScript.BraceLevel5";
    public const string BraceLevel6 = "AuroraScript.BraceLevel6";
    public const string Bracket = "AuroraScript.Bracket";
    public const string Parenthesis = "AuroraScript.Parenthesis";
}

[Export(typeof(ITaggerProvider))]
[ContentType(AuroraContentTypeDefinition.ContentTypeName)]
[TagType(typeof(ClassificationTag))]
internal sealed class AuroraDelimiterTaggerProvider : ITaggerProvider
{
    [Import]
    internal IClassificationTypeRegistryService ClassificationTypes = null!;

    public ITagger<T>? CreateTagger<T>(ITextBuffer buffer)
        where T : ITag
    {
        return new AuroraDelimiterTagger(buffer, ClassificationTypes) as ITagger<T>;
    }
}

internal sealed class AuroraDelimiterTagger : ITagger<ClassificationTag>
{
    private readonly ITextBuffer _buffer;
    private readonly ClassificationTag[] _braceTags;
    private readonly ClassificationTag _bracketTag;
    private readonly ClassificationTag _parenthesisTag;

    public AuroraDelimiterTagger(
        ITextBuffer buffer,
        IClassificationTypeRegistryService classificationTypes)
    {
        _buffer = buffer;
        _braceTags = new[]
        {
            CreateTag(classificationTypes, AuroraDelimiterClassificationTypes.BraceLevel1),
            CreateTag(classificationTypes, AuroraDelimiterClassificationTypes.BraceLevel2),
            CreateTag(classificationTypes, AuroraDelimiterClassificationTypes.BraceLevel3),
            CreateTag(classificationTypes, AuroraDelimiterClassificationTypes.BraceLevel4),
            CreateTag(classificationTypes, AuroraDelimiterClassificationTypes.BraceLevel5),
            CreateTag(classificationTypes, AuroraDelimiterClassificationTypes.BraceLevel6)
        };
        _bracketTag = CreateTag(classificationTypes, AuroraDelimiterClassificationTypes.Bracket);
        _parenthesisTag = CreateTag(classificationTypes, AuroraDelimiterClassificationTypes.Parenthesis);
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
        var braceDepth = 0;
        var state = ScannerState.Code;

        for (var i = 0; i < text.Length; i++)
        {
            var current = text[i];
            var next = i + 1 < text.Length ? text[i + 1] : '\0';

            if (TryGetBlockStringLine(text, i, out var nextLineStart))
            {
                i = Math.Max(i, nextLineStart - 1);
                continue;
            }

            if (state != ScannerState.Code)
            {
                AdvanceNonCodeState(text, ref i, ref state);
                continue;
            }

            if (current == '/' && next == '/')
            {
                state = ScannerState.LineComment;
                i++;
                continue;
            }

            if (current == '/' && next == '*')
            {
                state = ScannerState.BlockComment;
                i++;
                continue;
            }

            if (current == '\'')
            {
                state = ScannerState.SingleString;
                continue;
            }

            if (current == '"')
            {
                state = ScannerState.DoubleString;
                continue;
            }

            if (current == '`')
            {
                state = ScannerState.TemplateString;
                continue;
            }

            var tag = GetDelimiterTag(current, ref braceDepth);
            if (tag == null || !Intersects(spans, i))
            {
                continue;
            }

            yield return new TagSpan<ClassificationTag>(
                new SnapshotSpan(snapshot, i, 1),
                tag);
        }
    }

    private static ClassificationTag CreateTag(
        IClassificationTypeRegistryService classificationTypes,
        string name)
    {
        return new ClassificationTag(classificationTypes.GetClassificationType(name));
    }

    private ClassificationTag? GetDelimiterTag(char current, ref int braceDepth)
    {
        switch (current)
        {
            case '{':
                var leftLevel = braceDepth % _braceTags.Length;
                braceDepth++;
                return _braceTags[leftLevel];
            case '}':
                if (braceDepth > 0)
                {
                    braceDepth--;
                }

                return _braceTags[braceDepth % _braceTags.Length];
            case '[':
            case ']':
                return _bracketTag;
            case '(':
            case ')':
                return _parenthesisTag;
            default:
                return null;
        }
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

    private static bool TryGetBlockStringLine(string text, int start, out int nextLineStart)
    {
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

        nextLineStart = markerStart + 3;
        while (nextLineStart < text.Length && text[nextLineStart] != '\r' && text[nextLineStart] != '\n')
        {
            nextLineStart++;
        }

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

    private static void AdvanceNonCodeState(string text, ref int index, ref ScannerState state)
    {
        var current = text[index];
        var next = index + 1 < text.Length ? text[index + 1] : '\0';

        switch (state)
        {
            case ScannerState.LineComment:
                if (current == '\r' || current == '\n')
                {
                    state = ScannerState.Code;
                }
                return;
            case ScannerState.BlockComment:
                if (current == '*' && next == '/')
                {
                    index++;
                    state = ScannerState.Code;
                }
                return;
            case ScannerState.SingleString:
                AdvanceStringState(current, next, '\'', ref index, ref state);
                return;
            case ScannerState.DoubleString:
                AdvanceStringState(current, next, '"', ref index, ref state);
                return;
            case ScannerState.TemplateString:
                AdvanceStringState(current, next, '`', ref index, ref state);
                return;
        }
    }

    private static void AdvanceStringState(
        char current,
        char next,
        char quote,
        ref int index,
        ref ScannerState state)
    {
        if (current == '\\' && next != '\0')
        {
            index++;
            return;
        }

        if (current == quote)
        {
            state = ScannerState.Code;
        }
    }

    private enum ScannerState
    {
        Code,
        LineComment,
        BlockComment,
        SingleString,
        DoubleString,
        TemplateString
    }
}

[System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE0051:Remove unused private members", Justification = "MEF composition fields are discovered by Visual Studio.")]
internal static class AuroraDelimiterClassificationDefinitions
{
    [Export(typeof(ClassificationTypeDefinition))]
    [Name(AuroraDelimiterClassificationTypes.BraceLevel1)]
    [BaseDefinition("text")]
    internal static ClassificationTypeDefinition? AuroraBraceLevel1ClassificationType;

    [Export(typeof(ClassificationTypeDefinition))]
    [Name(AuroraDelimiterClassificationTypes.BraceLevel2)]
    [BaseDefinition("text")]
    internal static ClassificationTypeDefinition? AuroraBraceLevel2ClassificationType;

    [Export(typeof(ClassificationTypeDefinition))]
    [Name(AuroraDelimiterClassificationTypes.BraceLevel3)]
    [BaseDefinition("text")]
    internal static ClassificationTypeDefinition? AuroraBraceLevel3ClassificationType;

    [Export(typeof(ClassificationTypeDefinition))]
    [Name(AuroraDelimiterClassificationTypes.BraceLevel4)]
    [BaseDefinition("text")]
    internal static ClassificationTypeDefinition? AuroraBraceLevel4ClassificationType;

    [Export(typeof(ClassificationTypeDefinition))]
    [Name(AuroraDelimiterClassificationTypes.BraceLevel5)]
    [BaseDefinition("text")]
    internal static ClassificationTypeDefinition? AuroraBraceLevel5ClassificationType;

    [Export(typeof(ClassificationTypeDefinition))]
    [Name(AuroraDelimiterClassificationTypes.BraceLevel6)]
    [BaseDefinition("text")]
    internal static ClassificationTypeDefinition? AuroraBraceLevel6ClassificationType;

    [Export(typeof(ClassificationTypeDefinition))]
    [Name(AuroraDelimiterClassificationTypes.Bracket)]
    [BaseDefinition("text")]
    internal static ClassificationTypeDefinition? AuroraBracketClassificationType;

    [Export(typeof(ClassificationTypeDefinition))]
    [Name(AuroraDelimiterClassificationTypes.Parenthesis)]
    [BaseDefinition("text")]
    internal static ClassificationTypeDefinition? AuroraParenthesisClassificationType;
}

[Export(typeof(EditorFormatDefinition))]
[ClassificationType(ClassificationTypeNames = AuroraDelimiterClassificationTypes.BraceLevel1)]
[Name(AuroraDelimiterClassificationTypes.BraceLevel1)]
[UserVisible(true)]
internal sealed class AuroraBraceLevel1Format : ClassificationFormatDefinition
{
    public AuroraBraceLevel1Format()
    {
        DisplayName = "AuroraScript Brace Level 1";
        ForegroundColor = Color.FromRgb(0xD7, 0xA5, 0x42);
    }
}

[Export(typeof(EditorFormatDefinition))]
[ClassificationType(ClassificationTypeNames = AuroraDelimiterClassificationTypes.BraceLevel2)]
[Name(AuroraDelimiterClassificationTypes.BraceLevel2)]
[UserVisible(true)]
internal sealed class AuroraBraceLevel2Format : ClassificationFormatDefinition
{
    public AuroraBraceLevel2Format()
    {
        DisplayName = "AuroraScript Brace Level 2";
        ForegroundColor = Color.FromRgb(0xC5, 0x86, 0xC0);
    }
}

[Export(typeof(EditorFormatDefinition))]
[ClassificationType(ClassificationTypeNames = AuroraDelimiterClassificationTypes.BraceLevel3)]
[Name(AuroraDelimiterClassificationTypes.BraceLevel3)]
[UserVisible(true)]
internal sealed class AuroraBraceLevel3Format : ClassificationFormatDefinition
{
    public AuroraBraceLevel3Format()
    {
        DisplayName = "AuroraScript Brace Level 3";
        ForegroundColor = Color.FromRgb(0x4E, 0xC9, 0xB0);
    }
}

[Export(typeof(EditorFormatDefinition))]
[ClassificationType(ClassificationTypeNames = AuroraDelimiterClassificationTypes.BraceLevel4)]
[Name(AuroraDelimiterClassificationTypes.BraceLevel4)]
[UserVisible(true)]
internal sealed class AuroraBraceLevel4Format : ClassificationFormatDefinition
{
    public AuroraBraceLevel4Format()
    {
        DisplayName = "AuroraScript Brace Level 4";
        ForegroundColor = Color.FromRgb(0x56, 0x9C, 0xD6);
    }
}

[Export(typeof(EditorFormatDefinition))]
[ClassificationType(ClassificationTypeNames = AuroraDelimiterClassificationTypes.BraceLevel5)]
[Name(AuroraDelimiterClassificationTypes.BraceLevel5)]
[UserVisible(true)]
internal sealed class AuroraBraceLevel5Format : ClassificationFormatDefinition
{
    public AuroraBraceLevel5Format()
    {
        DisplayName = "AuroraScript Brace Level 5";
        ForegroundColor = Color.FromRgb(0xCE, 0x91, 0x78);
    }
}

[Export(typeof(EditorFormatDefinition))]
[ClassificationType(ClassificationTypeNames = AuroraDelimiterClassificationTypes.BraceLevel6)]
[Name(AuroraDelimiterClassificationTypes.BraceLevel6)]
[UserVisible(true)]
internal sealed class AuroraBraceLevel6Format : ClassificationFormatDefinition
{
    public AuroraBraceLevel6Format()
    {
        DisplayName = "AuroraScript Brace Level 6";
        ForegroundColor = Color.FromRgb(0xB5, 0xCE, 0xA8);
    }
}

[Export(typeof(EditorFormatDefinition))]
[ClassificationType(ClassificationTypeNames = AuroraDelimiterClassificationTypes.Bracket)]
[Name(AuroraDelimiterClassificationTypes.Bracket)]
[UserVisible(true)]
internal sealed class AuroraBracketFormat : ClassificationFormatDefinition
{
    public AuroraBracketFormat()
    {
        DisplayName = "AuroraScript Bracket";
        ForegroundColor = Color.FromRgb(0xD4, 0xD4, 0xD4);
    }
}

[Export(typeof(EditorFormatDefinition))]
[ClassificationType(ClassificationTypeNames = AuroraDelimiterClassificationTypes.Parenthesis)]
[Name(AuroraDelimiterClassificationTypes.Parenthesis)]
[UserVisible(true)]
internal sealed class AuroraParenthesisFormat : ClassificationFormatDefinition
{
    public AuroraParenthesisFormat()
    {
        DisplayName = "AuroraScript Parenthesis";
        ForegroundColor = Color.FromRgb(0xD4, 0xD4, 0xD4);
    }
}
