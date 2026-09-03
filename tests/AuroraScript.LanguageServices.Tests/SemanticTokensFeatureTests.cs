using AuroraScript.LanguageServices;
using AuroraScript.LanguageServices.Builtins;
using AuroraScript.LanguageServices.Features.SemanticTokens;
using System;
using System.IO;
using System.Linq;
using Xunit;

namespace AuroraScript.LanguageServices.Tests;

public sealed class SemanticTokensFeatureTests : IDisposable
{
    private readonly string _root;

    public SemanticTokensFeatureTests()
    {
        _root = Path.Combine(Path.GetTempPath(), "aurora-semantic-tokens-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_root);
    }

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
                const $args = 3;
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
                console.log($arg, $args);
                global.modules;
                JSON.stringify(Math.PI);
                HotPatch.apply();
                String.fromCharCode(65);
                const hex = 0xFDE5380C;
                const integer = 123;
                const decimal = 12.34;
                local.log();
                const array = [1, 2.5, 0x10];
                const indexed = local["path"];
                return `total: ${10}`;
            }
            """;
        var service = CreateService(_root);

        var result = service.GetSemanticTokens("main.as", source);

        Assert.Contains(result.Tokens, token => token.Type == AuroraSemanticTokenTypes.Keyword);
        AssertToken(source, result, "\"text\"", AuroraSemanticTokenTypes.String);
        AssertToken(source, result, "\\n", AuroraSemanticTokenTypes.Character);
        Assert.Contains(result.Tokens, token => token.Type == AuroraSemanticTokenTypes.Number);
        AssertToken(source, result, "console", AuroraSemanticTokenTypes.Type);
        AssertToken(source, result, "global", AuroraSemanticTokenTypes.BuiltinVariable);
        AssertToken(source, result, "JSON", AuroraSemanticTokenTypes.Type);
        AssertToken(source, result, "Math", AuroraSemanticTokenTypes.Type);
        AssertToken(source, result, "HotPatch", AuroraSemanticTokenTypes.Type);
        AssertToken(source, result, "$arg", AuroraSemanticTokenTypes.BuiltinVariable);
        AssertNoToken(source, result, "$state", AuroraSemanticTokenTypes.BuiltinVariable);
        AssertNoToken(source, result, "$args", AuroraSemanticTokenTypes.BuiltinVariable);
        AssertToken(source, result, "String", AuroraSemanticTokenTypes.Type);
        AssertToken(source, result, "fromCharCode", AuroraSemanticTokenTypes.MethodCall);
        AssertToken(source, result, "0xFDE5380C", AuroraSemanticTokenTypes.Number);
        AssertToken(source, result, "123", AuroraSemanticTokenTypes.Number);
        AssertToken(source, result, "12.34", AuroraSemanticTokenTypes.Number);
        AssertToken(source, result, "2.5", AuroraSemanticTokenTypes.Number);
        AssertToken(source, result, "0x10", AuroraSemanticTokenTypes.Number);
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
        AssertToken(source, result, "[1, 2.5, 0x10]", "[", AuroraSemanticTokenTypes.Bracket);
        AssertToken(source, result, "[1, 2.5, 0x10]", "]", AuroraSemanticTokenTypes.Bracket);
        AssertToken(source, result, "local[\"path\"]", "[", AuroraSemanticTokenTypes.Bracket);
        AssertToken(source, result, "local[\"path\"]", "]", AuroraSemanticTokenTypes.Bracket);
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
    public void HighlightsContextualNativeFunctionModifier()
    {
        const string source =
            "export native func add(Number a, Number b) Number { return a + b; }";
        var service = CreateService(_root);

        var result = service.GetSemanticTokens("main.as", source);

        AssertToken(
            source,
            result,
            "native",
            AuroraSemanticTokenTypes.Keyword);
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
    public void DeclaredExternalSymbolsUseDedicatedTokensAndRespectShadowing()
    {
        const string globals =
            """
            @global();

            declare const APP_VERSION;
            declare var ONLINE_TOTAL;
            declare func INPUT_NUMBER(title, label, type, callback);
            """;
        const string source =
            """
            @module(TEST);

            export func run() {
                INPUT_NUMBER("title", "label", "number", null);
                console.log(APP_VERSION, ONLINE_TOTAL);
                {
                    var APP_VERSION = "local";
                    var ONLINE_TOTAL = 0;
                    var INPUT_NUMBER = console.log;
                    INPUT_NUMBER("local");
                    console.log(APP_VERSION, ONLINE_TOTAL);
                }
                INPUT_NUMBER("title", "label", "number", null);
            }

            export func shadow(INPUT_NUMBER) {
                INPUT_NUMBER("param");
            }
            """;
        var service = CreateService(_root);
        service.OpenOrUpdateDocument(Path.Combine(_root, "globals.as"), globals);
        service.OpenOrUpdateDocument(Path.Combine(_root, "main.as"), source);

        var globalResult = service.GetSemanticTokens(Path.Combine(_root, "globals.as"));
        var result = service.GetSemanticTokens(Path.Combine(_root, "main.as"));

        AssertToken(globals, globalResult, "declare const APP_VERSION", "APP_VERSION", AuroraSemanticTokenTypes.DeclaredGlobal);
        AssertToken(globals, globalResult, "declare var ONLINE_TOTAL", "ONLINE_TOTAL", AuroraSemanticTokenTypes.DeclaredGlobal);
        AssertToken(globals, globalResult, "declare func INPUT_NUMBER", "INPUT_NUMBER", AuroraSemanticTokenTypes.DeclaredGlobalFunction);
        AssertToken(source, result, "INPUT_NUMBER(\"title\", \"label\", \"number\", null)", "INPUT_NUMBER", AuroraSemanticTokenTypes.DeclaredGlobalFunction);
        AssertToken(source, result, "console.log(APP_VERSION, ONLINE_TOTAL)", "APP_VERSION", AuroraSemanticTokenTypes.DeclaredGlobal);
        AssertToken(source, result, "console.log(APP_VERSION, ONLINE_TOTAL)", "ONLINE_TOTAL", AuroraSemanticTokenTypes.DeclaredGlobal);
        AssertNoToken(source, result, "var APP_VERSION = \"local\"", "APP_VERSION", AuroraSemanticTokenTypes.DeclaredGlobal);
        AssertNoToken(source, result, "var ONLINE_TOTAL = 0", "ONLINE_TOTAL", AuroraSemanticTokenTypes.DeclaredGlobal);
        AssertNoToken(source, result, "var INPUT_NUMBER = console.log", "INPUT_NUMBER", AuroraSemanticTokenTypes.DeclaredGlobalFunction);
        AssertNoToken(source, result, "INPUT_NUMBER(\"local\")", "INPUT_NUMBER", AuroraSemanticTokenTypes.DeclaredGlobalFunction);
        AssertNoToken(source, result, "shadow(INPUT_NUMBER)", "INPUT_NUMBER", AuroraSemanticTokenTypes.DeclaredGlobalFunction);
        AssertNoToken(source, result, "INPUT_NUMBER(\"param\")", "INPUT_NUMBER", AuroraSemanticTokenTypes.DeclaredGlobalFunction);
    }

    [Fact]
    public void DeclaredExternalSymbolsResolveFromWorkspaceGlobalDeclarations()
    {
        var mainPath = Path.Combine(_root, "main.as");
        var globalsPath = Path.Combine(_root, "globals.as");
        var main =
            """
            @module(MAIN);

            export func run() {
                INPUT_NUMBER("title", "label", "number", null);
                console.log(APP_VERSION, ONLINE_TOTAL);
                global.INPUT_NUMBER("title", "label", "number", null);
                console.log(global.APP_VERSION, global.ONLINE_TOTAL);
                var host = {};
                host.INPUT_NUMBER("title", "label", "number", null);
                console.log(host.APP_VERSION, host.ONLINE_TOTAL);
                {
                    var INPUT_NUMBER = console.log;
                    var APP_VERSION = "local";
                    var ONLINE_TOTAL = 0;
                    INPUT_NUMBER("local");
                    console.log(APP_VERSION, ONLINE_TOTAL);
                }
            }
            """;
        var globals =
            """
            @global();
            declare const APP_VERSION;
            declare var ONLINE_TOTAL;
            declare func INPUT_NUMBER(title, label, type, callback);
            """;
        var service = CreateService(_root);
        service.OpenOrUpdateDocument(mainPath, main);
        service.OpenOrUpdateDocument(globalsPath, globals);

        var result = service.GetSemanticTokens(mainPath);

        AssertToken(main, result, "INPUT_NUMBER(\"title\", \"label\", \"number\", null)", "INPUT_NUMBER", AuroraSemanticTokenTypes.DeclaredGlobalFunction);
        AssertToken(main, result, "console.log(APP_VERSION, ONLINE_TOTAL)", "APP_VERSION", AuroraSemanticTokenTypes.DeclaredGlobal);
        AssertToken(main, result, "console.log(APP_VERSION, ONLINE_TOTAL)", "ONLINE_TOTAL", AuroraSemanticTokenTypes.DeclaredGlobal);
        AssertToken(main, result, "global.INPUT_NUMBER", "INPUT_NUMBER", AuroraSemanticTokenTypes.DeclaredGlobalFunction);
        AssertToken(main, result, "global.APP_VERSION", "APP_VERSION", AuroraSemanticTokenTypes.DeclaredGlobal);
        AssertToken(main, result, "global.ONLINE_TOTAL", "ONLINE_TOTAL", AuroraSemanticTokenTypes.DeclaredGlobal);
        AssertNoToken(main, result, "host.INPUT_NUMBER", "INPUT_NUMBER", AuroraSemanticTokenTypes.DeclaredGlobalFunction);
        AssertNoToken(main, result, "host.APP_VERSION", "APP_VERSION", AuroraSemanticTokenTypes.DeclaredGlobal);
        AssertNoToken(main, result, "host.ONLINE_TOTAL", "ONLINE_TOTAL", AuroraSemanticTokenTypes.DeclaredGlobal);
        AssertNoToken(main, result, "INPUT_NUMBER(\"local\")", "INPUT_NUMBER", AuroraSemanticTokenTypes.DeclaredGlobalFunction);
        AssertNoTokenInLast(main, result, "console.log(APP_VERSION, ONLINE_TOTAL);", "APP_VERSION", AuroraSemanticTokenTypes.DeclaredGlobal);
        AssertNoTokenInLast(main, result, "console.log(APP_VERSION, ONLINE_TOTAL);", "ONLINE_TOTAL", AuroraSemanticTokenTypes.DeclaredGlobal);
    }

    [Fact]
    public void DeclaredExternalSymbolsResolveFromIndexedWorkspaceFiles()
    {
        var mainPath = Path.Combine(_root, "main.as");
        var globalsPath = Path.Combine(_root, "globals.as");
        var main =
            """
            @module(MAIN);
            export func run() {
                return APP_VERSION;
            }
            """;
        var globals =
            """
            @global();
            declare const APP_VERSION;
            """;
        File.WriteAllText(globalsPath, globals);
        var service = CreateService(_root, indexWorkspaceFiles: true);
        service.OpenOrUpdateDocument(mainPath, main);

        var result = service.GetSemanticTokens(mainPath);

        AssertToken(main, result, "APP_VERSION", AuroraSemanticTokenTypes.DeclaredGlobal);
    }

    [Fact]
    public void ReturnsEmptyTokensForIncompleteSource()
    {
        var service = new AuroraLanguageService(BuiltinApiLoader.LoadFromFile(BuiltinApiCatalogTests.GetRuntimeApiPath()));

        var result = service.GetSemanticTokens("main.as", "export func run(){ return \"unterminated");

        Assert.Empty(result.Tokens);
    }

    public void Dispose()
    {
        if (Directory.Exists(_root))
        {
            Directory.Delete(_root, recursive: true);
        }
    }

    private static AuroraLanguageService CreateService(string baseDirectory, bool indexWorkspaceFiles = false)
    {
        return new AuroraLanguageService(new AuroraLanguageServiceOptions(
            BuiltinApiLoader.LoadFromFile(BuiltinApiCatalogTests.GetRuntimeApiPath()))
        {
            BaseDirectory = baseDirectory,
            IndexWorkspaceFiles = indexWorkspaceFiles
        });
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

    private static void AssertNoToken(string source, SemanticTokensResult result, string context, string text, int type)
    {
        var contextIndex = source.IndexOf(context, System.StringComparison.Ordinal);
        Assert.True(contextIndex >= 0, $"Context '{context}' was not found.");
        Assert.DoesNotContain(result.Tokens, token =>
            token.Type == type &&
            TokenText(source, token) == text &&
            TokenOffset(source, token) >= contextIndex &&
            TokenOffset(source, token) < contextIndex + context.Length);
    }

    private static void AssertNoTokenInLast(string source, SemanticTokensResult result, string context, string text, int type)
    {
        var contextIndex = source.LastIndexOf(context, System.StringComparison.Ordinal);
        Assert.True(contextIndex >= 0, $"Context '{context}' was not found.");
        Assert.DoesNotContain(result.Tokens, token =>
            token.Type == type &&
            TokenText(source, token) == text &&
            TokenOffset(source, token) >= contextIndex &&
            TokenOffset(source, token) < contextIndex + context.Length);
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
