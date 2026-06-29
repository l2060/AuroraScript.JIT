using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.RegularExpressions;
using Xunit;

namespace AuroraScript.LanguageServices.Tests;

public sealed class TextMateGrammarTests
{
    [Fact]
    public void AuroraGrammarContainsCoreScopes()
    {
        using var document = JsonDocument.Parse(File.ReadAllText(GetGrammarPath()));
        var repository = document.RootElement.GetProperty("repository");

        Assert.Equal("source.aurora", document.RootElement.GetProperty("scopeName").GetString());
        AssertPattern(repository, "builtins", "support.type.aurora", "Path");
        AssertPattern(repository, "builtins", "support.type.builtin.object.aurora", "JSON console Math HotPatch");
        AssertPattern(repository, "builtins", "variable.language.aurora", "$args $state global");
        AssertPattern(repository, "function-calls", "entity.name.function.member.aurora", ".log(");
        AssertPattern(repository, "function-calls", "variable.other.property.aurora", ".PI");
        AssertPattern(repository, "function-calls", "entity.name.function.aurora", " abc(");
        AssertPattern(repository, "comments", "comment.line.double-slash.aurora", "// comment");
        AssertBeginPattern(repository, "comments", "comment.block.aurora", "/* comment */");
        AssertPattern(repository, "keywords", "keyword.operator.word.aurora", "typeof value in obj");
        AssertBlockStringPattern(repository, "  |> text", "  |>错误");
        AssertBeginPattern(repository, "strings", "meta.string.template.aurora", "`hello ${abc()}`");
        AssertNestedBeginPattern(repository, "strings", "meta.embedded.expression.aurora", "${abc()}");
    }

    [Fact]
    public void AuroraGrammarRegularExpressionsCompile()
    {
        using var document = JsonDocument.Parse(File.ReadAllText(GetGrammarPath()));

        foreach (var (propertyName, pattern) in EnumerateRegexPatterns(document.RootElement))
        {
            _ = new Regex(pattern, RegexOptions.CultureInvariant);
            Assert.True(pattern.Length > 0, propertyName);
        }
    }

    private static void AssertPattern(JsonElement repository, string sectionName, string scopeName, string sample)
    {
        var pattern = FindPattern(repository.GetProperty(sectionName), scopeName);
        Assert.NotNull(pattern);
        var match = Regex.Match(sample, pattern!.Value.GetProperty("match").GetString()!, RegexOptions.CultureInvariant);
        Assert.True(match.Success, $"{scopeName} did not match '{sample}'.");
    }

    private static void AssertBeginPattern(JsonElement repository, string sectionName, string scopeName, string sample)
    {
        var pattern = FindPattern(repository.GetProperty(sectionName), scopeName);
        Assert.NotNull(pattern);
        var match = Regex.Match(sample, pattern!.Value.GetProperty("begin").GetString()!, RegexOptions.CultureInvariant);
        Assert.True(match.Success, $"{scopeName} did not begin-match '{sample}'.");
    }

    private static void AssertNestedBeginPattern(JsonElement repository, string sectionName, string scopeName, string sample)
    {
        var pattern = FindPatternRecursive(repository.GetProperty(sectionName), scopeName);
        Assert.NotNull(pattern);
        var match = Regex.Match(sample, pattern!.Value.GetProperty("begin").GetString()!, RegexOptions.CultureInvariant);
        Assert.True(match.Success, $"{scopeName} did not begin-match '{sample}'.");
    }

    private static void AssertBlockStringPattern(JsonElement repository, string validSample, string invalidSample)
    {
        var strings = repository.GetProperty("strings");
        var blockPattern = strings.GetProperty("patterns").EnumerateArray().First(pattern =>
            pattern.TryGetProperty("begin", out var begin) &&
            begin.GetString()!.Contains("\\|>"));
        var beginPattern = blockPattern.GetProperty("begin").GetString()!;
        Assert.True(Regex.Match(validSample, beginPattern, RegexOptions.CultureInvariant).Success);
        Assert.False(Regex.Match(invalidSample, beginPattern, RegexOptions.CultureInvariant).Success);

        var captures = blockPattern.GetProperty("beginCaptures");
        Assert.Equal("punctuation.definition.string.block.aurora", captures.GetProperty("2").GetProperty("name").GetString());

        var prefixPattern = blockPattern.GetProperty("patterns").EnumerateArray().First(pattern =>
            pattern.TryGetProperty("captures", out var patternCaptures) &&
            patternCaptures.TryGetProperty("1", out var capture) &&
            capture.GetProperty("name").GetString() == "punctuation.definition.string.block.aurora");
        var prefixCaptures = prefixPattern.GetProperty("captures");
        Assert.Equal("punctuation.definition.string.block.aurora", prefixCaptures.GetProperty("1").GetProperty("name").GetString());
        Assert.Equal("|>", Regex.Match(validSample, prefixPattern.GetProperty("match").GetString()!, RegexOptions.CultureInvariant).Groups[1].Value);

        var contentPattern = blockPattern.GetProperty("patterns").EnumerateArray().First(pattern =>
            pattern.TryGetProperty("name", out var name) &&
            name.GetString() == "string.quoted.block.aurora");
        Assert.True(Regex.Match(validSample, contentPattern.GetProperty("match").GetString()!, RegexOptions.CultureInvariant).Success);
    }

    private static JsonElement? FindPattern(JsonElement section, string scopeName)
    {
        if (!section.TryGetProperty("patterns", out var patterns))
        {
            return null;
        }

        foreach (var pattern in patterns.EnumerateArray())
        {
            if (pattern.TryGetProperty("name", out var name) &&
                string.Equals(name.GetString(), scopeName, StringComparison.Ordinal))
            {
                return pattern;
            }

            foreach (var capturePropertyName in new[] { "captures", "beginCaptures", "endCaptures" })
            {
                if (!pattern.TryGetProperty(capturePropertyName, out var captures))
                {
                    continue;
                }

                foreach (var capture in captures.EnumerateObject())
                {
                    if (capture.Value.TryGetProperty("name", out var captureName) &&
                        string.Equals(captureName.GetString(), scopeName, StringComparison.Ordinal))
                    {
                        return pattern;
                    }
                }
            }
        }

        return null;
    }

    private static JsonElement? FindPatternRecursive(JsonElement section, string scopeName)
    {
        var direct = FindPattern(section, scopeName);
        if (direct.HasValue)
        {
            return direct;
        }

        if (!section.TryGetProperty("patterns", out var patterns))
        {
            return null;
        }

        foreach (var pattern in patterns.EnumerateArray())
        {
            var nested = FindPatternRecursive(pattern, scopeName);
            if (nested.HasValue)
            {
                return nested;
            }
        }

        return null;
    }

    private static IEnumerable<(string PropertyName, string Pattern)> EnumerateRegexPatterns(JsonElement element)
    {
        if (element.ValueKind == JsonValueKind.Object)
        {
            foreach (var property in element.EnumerateObject())
            {
                if ((property.NameEquals("match") ||
                    property.NameEquals("begin") ||
                    property.NameEquals("end")) &&
                    property.Value.ValueKind == JsonValueKind.String)
                {
                    yield return (property.Name, property.Value.GetString()!);
                }

                foreach (var nested in EnumerateRegexPatterns(property.Value))
                {
                    yield return nested;
                }
            }
        }
        else if (element.ValueKind == JsonValueKind.Array)
        {
            foreach (var item in element.EnumerateArray())
            {
                foreach (var nested in EnumerateRegexPatterns(item))
                {
                    yield return nested;
                }
            }
        }
    }

    private static string GetGrammarPath()
    {
        var directory = AppContext.BaseDirectory;
        for (var i = 0; i < 10; i++)
        {
            var candidate = Path.GetFullPath(Path.Combine(
                directory,
                "visualstudio-extension",
                "AuroraScript.VisualStudio",
                "Grammars",
                "AuroraScript.tmLanguage.json"));
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

        throw new FileNotFoundException("AuroraScript.tmLanguage.json was not found from test output path.", directory);
    }
}
