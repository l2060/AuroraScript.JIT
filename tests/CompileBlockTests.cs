using AuroraScript.Runtime;
using AuroraScript.Tests.Infrastructure;
using System;
using Xunit;

namespace AuroraScript.Tests;

public sealed class CompileBlockTests
{
    [Fact]
    public void CompilesAndInvokesBlockWithNamedParametersAndLocalFunction()
    {
        using var workspace = new TestWorkspace();
        var engine = workspace.CreateEngine();
        var block = engine.CompileBlock(
            """
            function clamp(value, min, max) {
                if (value < min) return min;
                if (value > max) return max;
                return value;
            }
            return clamp(input, 0, 100) + offset;
            """,
            new CompileBlockOptions
            {
                Parameters = ["input", "offset"],
                SourceName = "blocks/clamp.as"
            });

        var result = block.Invoke(ScriptDatum.FromNumber(125), ScriptDatum.FromNumber(3));
        ScriptAssert.Equal(103, result);
    }

    [Fact]
    public void CanInvokeBlockAgainstAnExistingDomain()
    {
        using var workspace = new TestWorkspace();
        var engine = workspace.CreateEngine();
        var block = engine.CompileBlock("return value * 2;", new CompileBlockOptions { Parameters = ["value"] });
        var domain = engine.CreateEmptyDomain(null);

        ScriptAssert.Equal(42, block.Invoke(domain, ScriptDatum.FromNumber(21)));
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("1value")]
    [InlineData("bad-name")]
    [InlineData("global")]
    [InlineData("$args")]
    [InlineData("$state")]
    public void RejectsInvalidParameterNames(string parameter)
    {
        using var workspace = new TestWorkspace();
        var engine = workspace.CreateEngine();
        var options = new CompileBlockOptions { Parameters = [parameter] };

        Assert.Throws<ArgumentException>(() => engine.CompileBlock("return 1;", options));
    }

    [Fact]
    public void RejectsDuplicateParameterNames()
    {
        using var workspace = new TestWorkspace();
        var engine = workspace.CreateEngine();
        var options = new CompileBlockOptions { Parameters = ["value", "value"] };

        Assert.Throws<ArgumentException>(() => engine.CompileBlock("return value;", options));
    }

    [Fact]
    public void RejectsNullSource()
    {
        using var workspace = new TestWorkspace();
        var engine = workspace.CreateEngine();

        Assert.Throws<ArgumentNullException>(() => engine.CompileBlock(null!));
    }

    [Theory]
    [InlineData("@module(TEST);")]
    [InlineData("import value from 'value';")]
    [InlineData("include 'value';")]
    [InlineData("export func run() { }")]
    [InlineData("export var value = 1;")]
    [InlineData("declare func HOST();")]
    public void RejectsModuleOnlyStatements(string source)
    {
        using var workspace = new TestWorkspace();
        var engine = workspace.CreateEngine();

        var error = Assert.Throws<AuroraParseException>(() => engine.CompileBlock(source));
        Assert.Contains("CompileBlock", error.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void DiagnosticUsesConfiguredSourceName()
    {
        using var workspace = new TestWorkspace();
        var engine = workspace.CreateEngine();

        var error = Assert.Throws<AuroraParseException>(() => engine.CompileBlock(
            "return (1 +;",
            new CompileBlockOptions { SourceName = "virtual/release-regression.as" }));

        Assert.Contains("release-regression.as", error.FileName, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("break;")]
    [InlineData("continue;")]
    [InlineData("return 1 2;")]
    public void RejectsInvalidTopLevelBlockSyntax(string source)
    {
        using var workspace = new TestWorkspace();
        var engine = workspace.CreateEngine();

        Assert.IsAssignableFrom<AuroraException>(
            Record.Exception(() => engine.CompileBlock(source)));
    }
}
