using AuroraScript.LanguageServices.Features.Hover;
using System;
using System.Linq;
using System.Text;
using Xunit;

namespace AuroraScript.LanguageServices.Tests;

public sealed class HoverMarkupTests
{
    [Fact]
    public void ParseSeparatesCodeFencesFromNotes()
    {
        const string markdown =
            "```aurorascript\ndeclare type Math {\n    static func abs(Number value) Number;\n}\n```\n\nReturns the absolute value.";

        var lines = HoverMarkup.Parse(markdown);

        Assert.Equal(5, lines.Count);
        Assert.True(lines[0].IsCode);
        Assert.False(lines[4].IsCode);
        Assert.Equal("Returns the absolute value.", Flatten(lines[4]));
        Assert.DoesNotContain(lines, line => Flatten(line).Contains("```", StringComparison.Ordinal));
    }

    [Fact]
    public void ParseClassifiesDeclarationSignatures()
    {
        const string markdown = "```aurorascript\nstatic func abs(Number value) Number\n```";

        var runs = HoverMarkup.Parse(markdown).Single().Runs;

        Assert.Equal(HoverRunKind.Keyword, KindOf(runs, "static"));
        Assert.Equal(HoverRunKind.Keyword, KindOf(runs, "func"));
        Assert.Equal(HoverRunKind.Function, KindOf(runs, "abs"));
        Assert.Equal(HoverRunKind.Type, KindOf(runs, "Number"));
        Assert.Equal(HoverRunKind.Identifier, KindOf(runs, "value"));
    }

    [Fact]
    public void ParseClassifiesVariadicAndUnionParameters()
    {
        const string markdown = "```aurorascript\nconstructor(String | Path | Null root, ...String segments)\n```";

        var runs = HoverMarkup.Parse(markdown).Single().Runs;

        Assert.Equal(HoverRunKind.Keyword, KindOf(runs, "constructor"));
        Assert.Equal(HoverRunKind.Type, KindOf(runs, "Path"));
        Assert.Equal(HoverRunKind.Operator, KindOf(runs, "|"));
        Assert.Equal(HoverRunKind.Operator, KindOf(runs, "..."));
        Assert.Equal(HoverRunKind.Identifier, KindOf(runs, "segments"));
    }

    [Fact]
    public void ToPlainTextRoundTripsContentWithoutFences()
    {
        const string markdown = "```aurorascript\nexport native func createAStar() Object\n```\n\nCreates a solver.";

        var text = HoverMarkup.ToPlainText(markdown);

        Assert.Equal("export native func createAStar() Object\n\nCreates a solver.", text);
    }

    private static string Flatten(HoverLine line)
    {
        var builder = new StringBuilder();
        foreach (var run in line.Runs)
        {
            builder.Append(run.Text);
        }

        return builder.ToString();
    }

    private static HoverRunKind KindOf(System.Collections.Generic.IReadOnlyList<HoverRun> runs, string text)
    {
        return runs.First(run => string.Equals(run.Text, text, StringComparison.Ordinal)).Kind;
    }
}
