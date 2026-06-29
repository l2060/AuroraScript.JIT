using AuroraScript.LanguageServices;
using AuroraScript.LanguageServices.Builtins;
using AuroraScript.LanguageServices.Features.SemanticTokens;
using System.Linq;
using Xunit;

namespace AuroraScript.LanguageServices.Tests;

public sealed class SemanticTokensFeatureTests
{
    [Fact]
    public void ScansLexerTokensForHighlighting()
    {
        const string source =
            """
            @module(TEST);
            enum Status { Ready, Failed = 2 }
            export func run() {
                const string = 1;
                const number = 2;
                const label = "text";
                const escaped = "a\nb";
                const local = { log: 1, "path": 2, 3: "number-key" };
                if (local.log) {
                    while (number in local) {
                        continue;
                        break;
                    }
                } else {
                    local.log();
                }
                try {
                    throw label;
                } catch (err) {
                    return Status.Ready;
                } finally {
                    debugger;
                }
                console.log($arg, $state);
                global.modules;
                JSON.stringify(Math.PI);
                HotPatch.apply();
                String.fromCharCode(65);
                local.log();
                const indexed = local["path"];
                return `total: ${10}`;
            }
            """;
        var service = new AuroraLanguageService(BuiltinApiLoader.LoadFromFile(BuiltinApiCatalogTests.GetRuntimeApiPath()));

        var result = service.GetSemanticTokens("main.as", source);

        Assert.Contains(result.Tokens, token => token.Type == AuroraSemanticTokenTypes.Keyword);
        AssertToken(source, result, "\"text\"", AuroraSemanticTokenTypes.String);
        AssertToken(source, result, "\\n", AuroraSemanticTokenTypes.Character);
        Assert.Contains(result.Tokens, token => token.Type == AuroraSemanticTokenTypes.Number);
        AssertToken(source, result, "console", AuroraSemanticTokenTypes.Object);
        AssertToken(source, result, "global", AuroraSemanticTokenTypes.BuiltinVariable);
        AssertToken(source, result, "JSON", AuroraSemanticTokenTypes.Object);
        AssertToken(source, result, "Math", AuroraSemanticTokenTypes.Object);
        AssertToken(source, result, "HotPatch", AuroraSemanticTokenTypes.Object);
        AssertToken(source, result, "$arg", AuroraSemanticTokenTypes.BuiltinVariable);
        AssertToken(source, result, "$state", AuroraSemanticTokenTypes.BuiltinVariable);
        AssertToken(source, result, "String", AuroraSemanticTokenTypes.Type);
        AssertToken(source, result, "fromCharCode", AuroraSemanticTokenTypes.MethodCall);
        AssertToken(source, result, "log()", "log", AuroraSemanticTokenTypes.MethodCall);
        AssertToken(source, result, "modules", AuroraSemanticTokenTypes.Property);
        AssertToken(source, result, "PI", AuroraSemanticTokenTypes.Property);
        AssertToken(source, result, "Status", AuroraSemanticTokenTypes.Enum);
        AssertToken(source, result, "Ready", AuroraSemanticTokenTypes.EnumMember);
        AssertToken(source, result, "Failed", AuroraSemanticTokenTypes.EnumMember);
        AssertToken(source, result, "log: 1", "log", AuroraSemanticTokenTypes.MapKey);
        AssertToken(source, result, "\"path\"", AuroraSemanticTokenTypes.MapKey);
        AssertToken(source, result, "3", AuroraSemanticTokenTypes.MapKey);
        AssertToken(source, result, "if", AuroraSemanticTokenTypes.ControlFlow);
        AssertToken(source, result, "while", AuroraSemanticTokenTypes.ControlFlow);
        AssertToken(source, result, "break", AuroraSemanticTokenTypes.ControlFlow);
        AssertToken(source, result, "continue", AuroraSemanticTokenTypes.ControlFlow);
        AssertToken(source, result, "return", AuroraSemanticTokenTypes.Return);
        AssertToken(source, result, "throw", AuroraSemanticTokenTypes.Throw);
        AssertToken(source, result, "try", AuroraSemanticTokenTypes.Exception);
        AssertToken(source, result, "catch", AuroraSemanticTokenTypes.Exception);
        AssertToken(source, result, "finally", AuroraSemanticTokenTypes.Exception);
        AssertToken(source, result, "export", AuroraSemanticTokenTypes.ImportExport);
        Assert.Contains(result.Tokens, token => token.Type == AuroraSemanticTokenTypes.Parenthesis);
        Assert.Contains(result.Tokens, token => token.Type == AuroraSemanticTokenTypes.Bracket);
        Assert.Contains(result.Tokens, token => token.Type == AuroraSemanticTokenTypes.BraceLevel1);
        Assert.Contains(result.Tokens, token => token.Type == AuroraSemanticTokenTypes.BraceLevel2);
        AssertNoToken(source, result, "string", AuroraSemanticTokenTypes.Type);
        AssertNoToken(source, result, "number", AuroraSemanticTokenTypes.Type);
        AssertNoToken(source, result, "`total: ${10}`", AuroraSemanticTokenTypes.String);
        Assert.True(result.Tokens.SequenceEqual(result.Tokens.OrderBy(token => token.Line).ThenBy(token => token.Character)));
    }

    [Fact]
    public void SemanticTokenPositionsDoNotDriftAfterStrings()
    {
        const string source =
            """
            expectTrue(ctx, typeof timer.reset == "function", "Timer exposes reset function", typeof timer.reset, "function");
            const block =
            |> line 1
            |> line 2
            ;
            /* comment */ typeof block;
            """;
        var service = new AuroraLanguageService(BuiltinApiLoader.LoadFromFile(BuiltinApiCatalogTests.GetRuntimeApiPath()));

        var result = service.GetSemanticTokens("main.as", source);

        Assert.Equal(3, result.Tokens.Count(token => token.Type == AuroraSemanticTokenTypes.Keyword && TokenText(source, token) == "typeof"));
        AssertToken(source, result, "line 1", AuroraSemanticTokenTypes.String);
        AssertToken(source, result, "line 2", AuroraSemanticTokenTypes.String);
        AssertNoToken(source, result, "|>", AuroraSemanticTokenTypes.String);
        Assert.DoesNotContain(result.Tokens, token => TokenText(source, token) == ", type");
    }

    [Fact]
    public void ReturnsEmptyTokensForIncompleteSource()
    {
        var service = new AuroraLanguageService(BuiltinApiLoader.LoadFromFile(BuiltinApiCatalogTests.GetRuntimeApiPath()));

        var result = service.GetSemanticTokens("main.as", "export func run(){ return \"unterminated");

        Assert.Empty(result.Tokens);
    }

    private static void AssertToken(string source, SemanticTokensResult result, string text, int type)
    {
        Assert.Contains(result.Tokens, token => token.Type == type && TokenText(source, token) == text);
    }

    private static void AssertToken(string source, SemanticTokensResult result, string context, string text, int type)
    {
        var contextIndex = source.IndexOf(context, System.StringComparison.Ordinal);
        Assert.True(contextIndex >= 0, $"Context '{context}' was not found.");
        Assert.Contains(result.Tokens, token =>
            token.Type == type &&
            TokenText(source, token) == text &&
            TokenOffset(source, token) >= contextIndex &&
            TokenOffset(source, token) < contextIndex + context.Length);
    }

    private static void AssertNoToken(string source, SemanticTokensResult result, string text, int type)
    {
        Assert.DoesNotContain(result.Tokens, token => token.Type == type && TokenText(source, token) == text);
    }

    private static string TokenText(string source, SemanticToken token)
    {
        var offset = TokenOffset(source, token);
        return offset + token.Length <= source.Length
            ? source.Substring(offset, token.Length)
            : string.Empty;
    }

    private static int TokenOffset(string source, SemanticToken token)
    {
        var offset = 0;
        var line = 0;
        var character = 0;
        while (offset < source.Length && (line < token.Line || character < token.Character))
        {
            if (source[offset] == '\r')
            {
                if (offset + 1 < source.Length && source[offset + 1] == '\n')
                {
                    offset++;
                }

                line++;
                character = 0;
            }
            else if (source[offset] == '\n')
            {
                line++;
                character = 0;
            }
            else
            {
                character++;
            }

            offset++;
        }

        return offset;
    }
}
