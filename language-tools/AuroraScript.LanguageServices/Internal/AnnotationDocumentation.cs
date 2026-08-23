using AuroraScript.LanguageServices.Features.Hover;
using AuroraScript.LanguageServices.Text;
using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;

namespace AuroraScript.LanguageServices.Internal;

internal static class AnnotationDocumentation
{
    private const string MarkdownLanguageId = "aurorascript";

    private static readonly Regex AnnotationPattern = new(
        @"(?<![$_\p{L}\p{Nd}])@(?<name>module|directCall)\b",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

    public static bool TryGetHover(string sourceText, TextPosition position, string? locale, out HoverResult hover)
    {
        hover = null!;
        var offset = TextPositionMapper.ToOffset(sourceText, position);
        foreach (Match match in AnnotationPattern.Matches(sourceText))
        {
            var name = match.Groups["name"];
            var start = match.Index;
            var end = match.Index + match.Length;
            if (offset < start || offset > end)
            {
                continue;
            }

            hover = new HoverResult(Format(name.Value, locale), RangeFromOffsets(sourceText, start, end));
            return true;
        }

        return false;
    }

    private static string Format(string name, string? locale)
    {
        var builder = new StringBuilder();
        if (string.Equals(name, "module", StringComparison.Ordinal))
        {
            builder.Append("```").Append(MarkdownLanguageId).Append("\n@module(NAME);\n```");
            AppendNotes(builder, locale,
                "Declares the optional explicit lookup name used by host module APIs and global.getModule. It must be the first effective statement; omitting it leaves the module anonymous without affecting path-based imports.",
                "声明供宿主模块 API 和 global.getModule 使用的可选显式查询名称。它必须是第一个有效语句；省略时模块保持匿名，但不影响基于路径的导入。");
            return builder.ToString();
        }

        builder.Append("```").Append(MarkdownLanguageId).Append("\n@directCall\n@directCall(true)\n@directCall(false)\n```");
        AppendNotes(builder, locale,
            "Marks a function as a direct-call candidate, or disables the directive with false.",
            "将函数标记为直接调用候选；传入 false 可禁用该指令。");
        return builder.ToString();
    }

    private static void AppendNotes(StringBuilder builder, string? locale, string en, string zh)
    {
        builder.Append("\n\n");
        builder.Append(IsChinese(locale) ? zh : en);
    }

    private static bool IsChinese(string? locale)
    {
        return !string.IsNullOrWhiteSpace(locale) &&
            locale.StartsWith("zh", StringComparison.OrdinalIgnoreCase);
    }

    private static TextRange RangeFromOffsets(string sourceText, int start, int end)
    {
        return new TextRange(string.Empty, PositionAtOffset(sourceText, start), PositionAtOffset(sourceText, end));
    }

    private static TextPosition PositionAtOffset(string sourceText, int offset)
    {
        var line = 0;
        var character = 0;
        for (var i = 0; i < offset && i < sourceText.Length; i++)
        {
            if (sourceText[i] == '\r')
            {
                if (i + 1 < offset && i + 1 < sourceText.Length && sourceText[i + 1] == '\n')
                {
                    i++;
                }
                line++;
                character = 0;
            }
            else if (sourceText[i] == '\n')
            {
                line++;
                character = 0;
            }
            else
            {
                character++;
            }
        }

        return new TextPosition(line, character);
    }
}
