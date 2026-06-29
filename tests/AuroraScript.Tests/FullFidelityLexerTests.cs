using AuroraScript.Compiler;
using AuroraScript.Compiler.Syntax;
using System;
using System.Linq;
using Xunit;

namespace AuroraScript.Tests;

public sealed class FullFidelityLexerTests
{
    [Fact]
    public void PreservesWhitespaceCommentsAndTokensInSourceOrder()
    {
        const string source = "// first\r\n  const value = /a[\\/]b/g; /* block */";

        var elements = AuroraSyntaxScanner.ScanAll(source, "test.as");

        Assert.Contains(elements, element => !element.IsToken && element.Trivia.Kind == SyntaxTriviaKind.LineComment);
        Assert.Contains(elements, element => !element.IsToken && element.Trivia.Kind == SyntaxTriviaKind.WhiteSpace);
        Assert.Contains(elements, element => !element.IsToken && element.Trivia.Kind == SyntaxTriviaKind.NewLine);
        Assert.Contains(elements, element => !element.IsToken && element.Trivia.Kind == SyntaxTriviaKind.BlockComment);
        Assert.Contains(elements, element => element.IsToken && element.Token.Kind == SyntaxTokenKind.Regex);
        Assert.Equal(SyntaxTokenKind.EndOfFile, elements.Last().Token.Kind);
    }

    [Fact]
    public void DoesNotTreatDivisionAsRegexAfterValue()
    {
        const string source = "const value = 8 / 2;";

        var elements = AuroraSyntaxScanner.ScanAll(source, "test.as");

        Assert.DoesNotContain(elements, element => element.IsToken && element.Token.Kind == SyntaxTokenKind.Regex);
        Assert.Contains(elements, element =>
            element.IsToken &&
            element.Token.SymbolId == Symbols.OP_DIVIDE.Id &&
            source.AsSpan(element.Token.Offset, element.Token.Length).SequenceEqual("/"));
    }
}
