using System;
using System.Collections.Generic;
using System.Text;

namespace AuroraScript.LanguageServices.Features.Hover;

/// <summary>
/// Classification assigned to a hover text run.
/// </summary>
public enum HoverRunKind
{
    Text,
    Keyword,
    Type,
    Function,
    Identifier,
    Number,
    String,
    Comment,
    Punctuation,
    Operator
}

/// <summary>
/// A contiguous piece of hover text sharing a single classification.
/// </summary>
public sealed class HoverRun
{
    public HoverRun(HoverRunKind kind, string text)
    {
        Kind = kind;
        Text = text;
    }

    public HoverRunKind Kind { get; }

    public string Text { get; }
}

/// <summary>
/// A single rendered line of hover content.
/// </summary>
public sealed class HoverLine
{
    public HoverLine(bool isCode, IReadOnlyList<HoverRun> runs)
    {
        IsCode = isCode;
        Runs = runs;
    }

    public bool IsCode { get; }

    public IReadOnlyList<HoverRun> Runs { get; }
}

/// <summary>
/// Splits hover markdown into classified lines so hosts that cannot render markdown
/// (most notably Visual Studio, whose LSP client only accepts plain text) can still
/// present colorized signatures.
/// </summary>
public static class HoverMarkup
{
    private static readonly HashSet<string> Keywords = new(StringComparer.Ordinal)
    {
        "declare",
        "type",
        "func",
        "function",
        "constructor",
        "const",
        "var",
        "static",
        "export",
        "native",
        "new",
        "class",
        "enum",
        "module",
        "import",
        "from",
        "return",
        "this",
        "true",
        "false",
        "null",
        "undefined"
    };

    private static readonly HashSet<string> TypeNames = new(StringComparer.Ordinal)
    {
        "void",
        "Object",
        "Number",
        "String",
        "Boolean",
        "Null",
        "Array",
        "Function"
    };

    public static IReadOnlyList<HoverLine> Parse(string markdown)
    {
        var lines = new List<HoverLine>();
        if (string.IsNullOrEmpty(markdown))
        {
            return lines;
        }

        var inCode = false;
        foreach (var raw in markdown.Replace("\r\n", "\n").Split('\n'))
        {
            if (raw.StartsWith("```", StringComparison.Ordinal))
            {
                inCode = !inCode;
                continue;
            }

            if (inCode)
            {
                lines.Add(new HoverLine(true, ClassifyCode(raw)));
                continue;
            }

            if (raw.Length == 0)
            {
                if (lines.Count != 0 && lines[lines.Count - 1].Runs.Count != 0)
                {
                    lines.Add(new HoverLine(false, Array.Empty<HoverRun>()));
                }

                continue;
            }

            lines.Add(new HoverLine(false, new[] { new HoverRun(HoverRunKind.Text, raw) }));
        }

        TrimTrailingBlankLines(lines);
        return lines;
    }

    /// <summary>
    /// Renders hover markdown without fences, for clients that only accept plain text.
    /// </summary>
    public static string ToPlainText(string markdown)
    {
        var builder = new StringBuilder();
        var lines = Parse(markdown);
        for (var i = 0; i < lines.Count; i++)
        {
            if (i > 0)
            {
                builder.Append('\n');
            }

            var runs = lines[i].Runs;
            for (var j = 0; j < runs.Count; j++)
            {
                builder.Append(runs[j].Text);
            }
        }

        return builder.ToString();
    }

    private static void TrimTrailingBlankLines(List<HoverLine> lines)
    {
        while (lines.Count != 0 && lines[lines.Count - 1].Runs.Count == 0)
        {
            lines.RemoveAt(lines.Count - 1);
        }
    }

    private static IReadOnlyList<HoverRun> ClassifyCode(string line)
    {
        var runs = new List<HoverRun>();
        var previousWord = string.Empty;
        var i = 0;
        while (i < line.Length)
        {
            var current = line[i];
            if (char.IsWhiteSpace(current))
            {
                var start = i;
                while (i < line.Length && char.IsWhiteSpace(line[i]))
                {
                    i++;
                }

                runs.Add(new HoverRun(HoverRunKind.Text, line.Substring(start, i - start)));
                continue;
            }

            if (current == '/' && i + 1 < line.Length && (line[i + 1] == '/' || line[i + 1] == '*'))
            {
                runs.Add(new HoverRun(HoverRunKind.Comment, line.Substring(i)));
                break;
            }

            if (current == '*' && i + 1 < line.Length && line[i + 1] == '/')
            {
                runs.Add(new HoverRun(HoverRunKind.Comment, line.Substring(i)));
                break;
            }

            if (current == '"' || current == '\'' || current == '`')
            {
                var start = i;
                i++;
                while (i < line.Length && line[i] != current)
                {
                    i += line[i] == '\\' ? 2 : 1;
                }

                i = Math.Min(i + 1, line.Length);
                runs.Add(new HoverRun(HoverRunKind.String, line.Substring(start, i - start)));
                continue;
            }

            if (char.IsDigit(current))
            {
                var start = i;
                while (i < line.Length && (char.IsLetterOrDigit(line[i]) || line[i] == '.'))
                {
                    i++;
                }

                runs.Add(new HoverRun(HoverRunKind.Number, line.Substring(start, i - start)));
                continue;
            }

            if (IsIdentifierStart(current))
            {
                var start = i;
                while (i < line.Length && IsIdentifierPart(line[i]))
                {
                    i++;
                }

                var word = line.Substring(start, i - start);
                runs.Add(new HoverRun(ClassifyWord(word, previousWord, PeekSignificant(line, i)), word));
                previousWord = word;
                continue;
            }

            if (current == '.' && i + 2 < line.Length && line[i + 1] == '.' && line[i + 2] == '.')
            {
                runs.Add(new HoverRun(HoverRunKind.Operator, "..."));
                i += 3;
                continue;
            }

            runs.Add(new HoverRun(IsOperator(current) ? HoverRunKind.Operator : HoverRunKind.Punctuation, current.ToString()));
            i++;
        }

        return runs;
    }

    private static HoverRunKind ClassifyWord(string word, string previousWord, char next)
    {
        if (Keywords.Contains(word))
        {
            return HoverRunKind.Keyword;
        }

        if (string.Equals(previousWord, "func", StringComparison.Ordinal) ||
            string.Equals(previousWord, "function", StringComparison.Ordinal))
        {
            return HoverRunKind.Function;
        }

        if (string.Equals(previousWord, "type", StringComparison.Ordinal) ||
            string.Equals(previousWord, "class", StringComparison.Ordinal) ||
            string.Equals(previousWord, "new", StringComparison.Ordinal))
        {
            return HoverRunKind.Type;
        }

        if (next == '(')
        {
            return HoverRunKind.Function;
        }

        if (TypeNames.Contains(word) || char.IsUpper(word[0]))
        {
            return HoverRunKind.Type;
        }

        return HoverRunKind.Identifier;
    }

    private static char PeekSignificant(string line, int index)
    {
        for (var i = index; i < line.Length; i++)
        {
            if (!char.IsWhiteSpace(line[i]))
            {
                return line[i];
            }
        }

        return '\0';
    }

    private static bool IsOperator(char value)
    {
        return value is '|' or '&' or '=' or '<' or '>' or '+' or '-' or '*' or '/' or '%' or '!' or '?' or ':';
    }

    private static bool IsIdentifierStart(char value)
    {
        return char.IsLetter(value) || value == '_' || value == '$';
    }

    private static bool IsIdentifierPart(char value)
    {
        return char.IsLetterOrDigit(value) || value == '_' || value == '$';
    }
}
