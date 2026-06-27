using AuroraScript.LanguageServices.Builtins;
using AuroraScript.LanguageServices.Features.Completion;
using AuroraScript.LanguageServices.Text;
using System;
using System.Linq;
using Xunit;

namespace AuroraScript.LanguageServices.Tests;

public sealed class BuiltinLanguageFeatureTests
{
    [Fact]
    public void HoverReturnsBuiltinMemberDocumentation()
    {
        const string source =
            """
            @module(TEST);
            export func run() {
                return Math.abs(-1);
            }
            """;
        var service = CreateService();

        var hover = service.GetHover("test.as", source, PositionOf(source, "abs"));

        Assert.NotNull(hover);
        Assert.Contains("Math.abs", hover!.Contents, StringComparison.Ordinal);
        Assert.Contains("value: number", hover.Contents, StringComparison.Ordinal);
        Assert.Contains("number", hover.Contents, StringComparison.Ordinal);
    }

    [Fact]
    public void CompletionReturnsBuiltinGlobals()
    {
        const string source =
            """
            @module(TEST);
            export func run() {
                Ma
            }
            """;
        var service = CreateService();

        var completions = service.GetCompletions("test.as", source, PositionOf(source, "Ma"));

        Assert.Contains(completions.Items, item => item.Label == "Math" && item.Kind == CompletionItemKind.Object);
        Assert.Contains(completions.Items, item => item.Label == "console" && item.Kind == CompletionItemKind.Object);
    }

    [Fact]
    public void CompletionReturnsBuiltinMembers()
    {
        const string source =
            """
            @module(TEST);
            export func run() {
                Math.
            }
            """;
        var service = CreateService();

        var completions = service.GetCompletions("test.as", source, new TextPosition(2, 9));

        Assert.Contains(completions.Items, item => item.Label == "abs" && item.Kind == CompletionItemKind.Method);
        Assert.Contains(completions.Items, item => item.Label == "PI" && item.Kind == CompletionItemKind.Constant && item.ReadOnly);
    }

    [Fact]
    public void SignatureHelpReturnsBuiltinMethodSignature()
    {
        const string source =
            """
            @module(TEST);
            export func run() {
                return Math.pow(2, 3);
            }
            """;
        var service = CreateService();

        var signatureHelp = service.GetSignatureHelp("test.as", source, PositionOf(source, "3"));

        Assert.NotNull(signatureHelp);
        var signature = Assert.Single(signatureHelp!.Signatures);
        Assert.Equal("Math.pow(x: number, y: number): number", signature.Label);
        Assert.Equal(1, signatureHelp.ActiveParameter);
        Assert.Equal(2, signature.Parameters.Count);
    }

    private static AuroraLanguageService CreateService()
    {
        var catalog = BuiltinApiLoader.LoadFromFile(BuiltinApiCatalogTests.GetRuntimeApiPath());
        return new AuroraLanguageService(catalog);
    }

    private static TextPosition PositionOf(string source, string needle)
    {
        var offset = source.IndexOf(needle, StringComparison.Ordinal);
        Assert.True(offset >= 0, $"Needle '{needle}' not found.");
        var line = 0;
        var character = 0;
        for (var i = 0; i < offset; i++)
        {
            if (source[i] == '\r')
            {
                if (i + 1 < offset && source[i + 1] == '\n')
                {
                    i++;
                }
                line++;
                character = 0;
            }
            else if (source[i] == '\n')
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
