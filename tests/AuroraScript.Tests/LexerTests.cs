using AuroraScript.Compiler;
using AuroraScript.Compiler.Analyzer;
using AuroraScript.Core;
using AuroraScript.Tokens;
using System;
using System.Collections.Generic;
using System.IO;
using Xunit;

namespace AuroraScript.Tests;

public sealed class LexerTests
{
    [Fact]
    public void TokenizesKeywordsIdentifiersAndEveryOperator()
    {
        const string source = "var name = 1 + 2 - 3 * 4 / 5 % 2; " +
            "if (name >= 1 && name <= 9 || name != 5) name++; else --name; " +
            "name += 1; name -= 1; name *= 2; name /= 2; name %= 2; " +
            "name = (name << 1) >> 1 >>> 1; name = name & 7 | 8 ^ 2;";

        using var lexer = CreateLexer(source);
        var values = ReadValues(lexer);

        Assert.Contains("var", values);
        Assert.Contains("name", values);
        Assert.Contains(">>>", values);
        Assert.Contains("&&", values);
        Assert.Contains("||", values);
        Assert.Contains("%=", values);
        Assert.Equal("END OF FILE", values[^1]);
    }

    [Theory]
    [InlineData("0", 0d)]
    [InlineData("42", 42d)]
    [InlineData("3.1415", 3.1415d)]
    [InlineData("1_000_000", 1_000_000d)]
    [InlineData("0xFF", 255d)]
    [InlineData("0Xabcdef", 11259375d)]
    public void ParsesSupportedNumberFormats(string source, double expected)
    {
        using var lexer = CreateLexer(source);
        var number = Assert.IsType<NumberToken>(lexer.Next());
        Assert.Equal(expected, number.NumberValue);
        Assert.True(lexer.IsAtEnd);
    }

    [Theory]
    [InlineData("'plain'", "plain")]
    [InlineData("\"double\"", "double")]
    [InlineData("'line\\nnext'", "line\nnext")]
    [InlineData("'tab\\tquote\\\''", "tab\tquote'")]
    [InlineData("'slash\\\\end'", "slash\\end")]
    public void DecodesStringEscapes(string source, string expected)
    {
        using var lexer = CreateLexer(source);
        var text = Assert.IsType<StringToken>(lexer.Next());
        Assert.Equal(expected, text.Value);
    }

    [Fact]
    public void DistinguishesRegexFromDivision()
    {
        using var lexer = CreateLexer("var regex = /a[\\/]b+/gim; var value = 8 / 2;");
        var tokens = ReadTokens(lexer);

        var regex = Assert.Single(tokens, token => token is RegexToken) as RegexToken;
        Assert.NotNull(regex);
        Assert.Equal("a[\\/]b+", regex.Pattern);
        Assert.Equal("gim", regex.Flags);
        Assert.Contains(tokens, token => token.Symbol == Symbols.OP_DIVIDE);
    }

    [Fact]
    public void SkipsCommentsWhitespaceAndTracksSourceLocations()
    {
        using var lexer = CreateLexer("// first\r\n  /* second\n line */\n变量 = 1;");
        var identifier = Assert.IsType<IdentifierToken>(lexer.Next());
        var assignment = lexer.Next();
        var number = Assert.IsType<NumberToken>(lexer.Next());

        Assert.Equal("变量", identifier.Value);
        Assert.Equal(4, identifier.Range.StartLine);
        Assert.Equal(1, identifier.Range.StartColumn);
        Assert.Equal(Symbols.OP_ASSIGNMENT, assignment.Symbol);
        Assert.Equal(1d, number.NumberValue);
    }

    [Fact]
    public void SupportsIdentifiersWithDollarUnderscoreDigitsAndChineseCharacters()
    {
        using var lexer = CreateLexer("$state _private value123 中文变量");
        Assert.Equal("$state", Assert.IsType<IdentifierToken>(lexer.Next()).Value);
        Assert.Equal("_private", Assert.IsType<IdentifierToken>(lexer.Next()).Value);
        Assert.Equal("value123", Assert.IsType<IdentifierToken>(lexer.Next()).Value);
        Assert.Equal("中文变量", Assert.IsType<IdentifierToken>(lexer.Next()).Value);
    }

    [Fact]
    public void TokenizesEveryReservedKeyword()
    {
        const string source = "var const func function if else for while break continue return throw try catch finally " +
            "new typeof in delete true false null import from include export declare enum debugger";
        using var lexer = CreateLexer(source);
        var tokens = ReadTokens(lexer);

        Assert.Equal(29, tokens.Count);
        Assert.DoesNotContain(tokens, token => token is IdentifierToken);
    }

    [Fact]
    public void EmptyAndCommentOnlySourcesContainOnlyEndOfFile()
    {
        using var empty = CreateLexer(string.Empty);
        using var comments = CreateLexer("// line\n/* block */\r\n");

        Assert.True(empty.IsAtEnd);
        Assert.Equal(Symbols.KW_EOF, empty.Next().Symbol);
        Assert.True(comments.IsAtEnd);
        Assert.Equal(Symbols.KW_EOF, comments.Next().Symbol);
    }

    [Fact]
    public void PreservesVeryLongIdentifierAndStringPayloads()
    {
        var identifier = "value" + new string('x', 16_384);
        var text = new string('a', 65_536);
        using var lexer = CreateLexer(identifier + " '" + text + "'");

        Assert.Equal(identifier, Assert.IsType<IdentifierToken>(lexer.Next()).Value);
        Assert.Equal(text, Assert.IsType<StringToken>(lexer.Next()).Value);
    }

    [Theory]
    [InlineData("0x")]
    [InlineData("0xGG")]
    [InlineData("1_")]
    [InlineData("1..2")]
    public void RejectsMalformedNumericLiterals(string source)
    {
        Assert.Throws<AuroraCompilationException>(() => CreateLexer(source));
    }

    [Fact]
    public void DotLeadingNumberTokenizesAsDotThenNumber()
    {
        using var lexer = CreateLexer(".5");

        Assert.Equal(Symbols.PT_DOT, lexer.Next().Symbol);
        Assert.Equal(5d, Assert.IsType<NumberToken>(lexer.Next()).NumberValue);
    }

    [Fact]
    public void HexLiteralStopsBeforeUnderscoreSeparator()
    {
        using var lexer = CreateLexer("0x1_2");

        Assert.Equal(1d, Assert.IsType<NumberToken>(lexer.Next()).NumberValue);
        Assert.Equal("_2", Assert.IsType<IdentifierToken>(lexer.Next()).Value);
    }

    [Fact]
    public void RegexLiteralPreservesEscapedCharacterClassesAndFlags()
    {
        using var lexer = CreateLexer("/(?<word>[a-z]+)\\s+\\/path/gim");

        var regex = Assert.IsType<RegexToken>(lexer.Next());
        Assert.Equal("(?<word>[a-z]+)\\s+\\/path", regex.Pattern);
        Assert.Equal("gim", regex.Flags);
    }

    [Fact]
    public void TemplateLiteralTracksMultilineRange()
    {
        using var lexer = CreateLexer("`first\n${1 + 2}\nlast`");

        var template = Assert.IsType<StringTemplateToken>(lexer.Next());

        Assert.Equal(1, template.Range.StartLine);
        Assert.Equal(3, template.Range.EndLine);
        Assert.True(template.Range.EndColumn > 1);
    }

    [Fact]
    public void InvalidRegexFlagIsNotConsumedAsRegexFlag()
    {
        using var lexer = CreateLexer("/a/z");

        var regex = Assert.IsType<RegexToken>(lexer.Next());
        Assert.Equal("a", regex.Pattern);
        Assert.Equal(string.Empty, regex.Flags);
        Assert.Equal("z", Assert.IsType<IdentifierToken>(lexer.Next()).Value);
    }

    [Fact]
    public void DiagnosticsContainFileLineAndColumn()
    {
        var root = Path.GetTempPath();
        var path = Path.Combine(root, "diagnostic-lexer.as");

        var error = Assert.Throws<AuroraCompilationException>(
            () => new AuroraLexer(root, new MemoryScriptSource(root, path, "\n\n  §")));

        Assert.Contains("diagnostic-lexer.as", error.FileName, StringComparison.OrdinalIgnoreCase);
        Assert.Equal(3, error.LineNumber);
        Assert.Equal(3, error.ColumnNumber);
    }

    [Fact]
    public void SnapshotRestoreAndRollbackPreserveTokenPosition()
    {
        using var lexer = CreateLexer("alpha beta gamma");
        var snapshot = lexer.CreateSnapshot();
        Assert.Equal("alpha", lexer.Next().Value);
        Assert.Equal("beta", lexer.Next().Value);
        lexer.RollBack();
        Assert.Equal("beta", lexer.Next().Value);
        lexer.RestoreSnapshot(snapshot);
        Assert.Equal("alpha", lexer.Next().Value);
    }

    [Theory]
    [InlineData("'unterminated")]
    [InlineData("'bad\\q'")]
    [InlineData("/* unterminated")]
    [InlineData("§")]
    [InlineData("1__2")]
    [InlineData("\"bad\\q\"")]
    [InlineData("`bad\\q`")]
    [InlineData("/* nested /* unterminated")]
    public void InvalidLexemesReportLexicalError(string source)
    {
        Assert.Throws<AuroraCompilationException>(() => CreateLexer(source));
    }

    private static AuroraLexer CreateLexer(string source)
    {
        var root = Path.GetTempPath();
        return new AuroraLexer(root, new MemoryScriptSource(root, Path.Combine(root, "lexer-test.as"), source));
    }

    private static List<string> ReadValues(AuroraLexer lexer)
    {
        var values = new List<string>(lexer.TokenCount);
        while (!lexer.IsAtEnd)
        {
            values.Add(lexer.Next().Value);
        }
        values.Add(lexer.Next().Value);
        return values;
    }

    private static List<Token> ReadTokens(AuroraLexer lexer)
    {
        var tokens = new List<Token>(lexer.TokenCount);
        while (!lexer.IsAtEnd)
        {
            tokens.Add(lexer.Next());
        }
        return tokens;
    }
}
