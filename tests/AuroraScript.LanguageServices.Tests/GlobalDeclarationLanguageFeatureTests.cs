using AuroraScript.LanguageServices;
using AuroraScript.LanguageServices.Builtins;
using AuroraScript.LanguageServices.Features.SemanticTokens;
using AuroraScript.LanguageServices.Text;
using System;
using System.IO;
using System.Linq;
using Xunit;

namespace AuroraScript.LanguageServices.Tests;

public sealed class GlobalDeclarationLanguageFeatureTests : IDisposable
{
    private readonly string _root;

    public GlobalDeclarationLanguageFeatureTests()
    {
        _root = Path.Combine(Path.GetTempPath(), "aurora-ambient-ls-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_root);
    }

    [Fact]
    public void TypedContextProvidesTypeAliasAndMemberHover()
    {
        var service = CreateService();
        var mainPath = Path.Combine(_root, "main.as");
        var globalsPath = Path.Combine(_root, "globals.as");
        var globals =
            """
            @global();
            declare type UserState {
                String name;
            }
            """;
        var main =
            """
            @module(MAIN);
            context user as UserState;
            export func player() UserState {
                return user;
            }
            export func name() {
                return user.name;
            }
            """;
        service.OpenOrUpdateDocument(globalsPath, globals);
        service.OpenOrUpdateDocument(mainPath, main);

        var typeHover = service.GetHover(mainPath, PositionOf(main, "UserState"));
        var aliasHover = service.GetHover(mainPath, PositionOfLast(main, "user"));
        var memberHover = service.GetHover(mainPath, PositionOf(main, "name;"));

        Assert.NotNull(typeHover);
        Assert.Contains("declare type UserState", typeHover!.Contents, StringComparison.Ordinal);
        Assert.NotNull(aliasHover);
        Assert.Contains("context user as UserState;", aliasHover!.Contents, StringComparison.Ordinal);
        Assert.NotNull(memberHover);
        Assert.Contains("String name", memberHover!.Contents, StringComparison.Ordinal);
    }

    [Fact]
    public void AmbientTypesDriveEditorFeaturesWithoutShadowingLocals()
    {
        var service = CreateService();
        var mainPath = Path.Combine(_root, "main.as");
        var globalsPath = Path.Combine(_root, "globals.as");
        var globals =
            """
            @global();
            declare type Stats {
                static const Number PI;
                static func mean(Number a, Number b) Number;
            }
            declare type Vec2 {
                constructor(Number x, Number y);
                Number x;
                Number y;
                func length() Number;
                static const Number DIMENSIONS;
                static func from(Number x, Number y) Vec2;
            }
            """;
        var main =
            """
            @module(MAIN);
            export func run() {
                Stats.mean(1, 2);
                var vec = new Vec2(3, 4);
                vec.length();
                Vec2.from(5, 6);
                var inferred = Vec2.from(7, 8);
                inferred.length();
                return vec.x;
            }
            """;
        service.OpenOrUpdateDocument(globalsPath, globals);
        service.OpenOrUpdateDocument(mainPath, main);

        var completions = service.GetCompletions(mainPath, PositionOf(main, "var vec"));
        Assert.Contains(completions.Items, item => item.Label == "Stats");
        Assert.Contains(completions.Items, item => item.Label == "Vec2");

        var moduleMembers = service.GetCompletions(mainPath, PositionAfter(main, "Stats."));
        Assert.Contains(moduleMembers.Items, item => item.Label == "mean");
        Assert.Contains(moduleMembers.Items, item => item.Label == "PI");
        Assert.DoesNotContain(moduleMembers.Items, item => item.Label == "length");

        var instanceMembers = service.GetCompletions(mainPath, PositionAfter(main, "vec."));
        Assert.Contains(instanceMembers.Items, item => item.Label == "length");
        Assert.Contains(instanceMembers.Items, item => item.Label == "x");
        Assert.DoesNotContain(instanceMembers.Items, item => item.Label == "from");

        var inferredMembers = service.GetCompletions(
            mainPath,
            PositionAfter(main, "inferred."));
        Assert.Contains(inferredMembers.Items, item => item.Label == "length");
        Assert.Contains(inferredMembers.Items, item => item.Label == "x");

        var staticMembers = service.GetCompletions(mainPath, PositionAfter(main, "Vec2."));
        Assert.Contains(staticMembers.Items, item => item.Label == "from");
        Assert.Contains(staticMembers.Items, item => item.Label == "DIMENSIONS");
        Assert.DoesNotContain(staticMembers.Items, item => item.Label == "x");

        var meanHover = service.GetHover(mainPath, PositionOf(main, "mean"));
        Assert.NotNull(meanHover);
        Assert.Contains("func mean(Number a, Number b) Number", meanHover!.Contents, StringComparison.Ordinal);

        var ctorHelp = service.GetSignatureHelp(mainPath, PositionOf(main, "3, 4"));
        Assert.NotNull(ctorHelp);
        Assert.Contains("constructor(Number x, Number y)", ctorHelp!.Signatures[0].Label, StringComparison.Ordinal);

        var meanHelp = service.GetSignatureHelp(mainPath, PositionOf(main, "1, 2"));
        Assert.NotNull(meanHelp);
        Assert.Contains("func mean(Number a, Number b) Number", meanHelp!.Signatures[0].Label, StringComparison.Ordinal);

        var lengthHelp = service.GetSignatureHelp(mainPath, PositionAfter(main, "vec.length("));
        Assert.NotNull(lengthHelp);
        Assert.Contains("func length()", lengthHelp!.Signatures[0].Label, StringComparison.Ordinal);

        var inferredHover = service.GetHover(
            mainPath,
            PositionOfLast(main, "length"));
        Assert.NotNull(inferredHover);
        Assert.Contains("func length()", inferredHover!.Contents, StringComparison.Ordinal);

        var inferredDefinition = service.GetDefinition(
            mainPath,
            PositionOfLast(main, "length"));
        Assert.NotNull(inferredDefinition);
        Assert.Equal(Path.GetFullPath(globalsPath), Path.GetFullPath(inferredDefinition!.Path));

        var meanDefinition = service.GetDefinition(mainPath, PositionOf(main, "mean"));
        Assert.NotNull(meanDefinition);
        Assert.Equal(Path.GetFullPath(globalsPath), Path.GetFullPath(meanDefinition!.Path));
        Assert.Equal(PositionOf(globals, "mean"), meanDefinition.Range.Start);

        var xDefinition = service.GetDefinition(
            mainPath,
            PositionAtOffset(main, main.IndexOf("return vec.x", StringComparison.Ordinal) + "return vec.".Length));
        Assert.NotNull(xDefinition);
        Assert.Equal(PositionOf(globals, "x;"), xDefinition!.Range.Start);

        var tokens = service.GetSemanticTokens(mainPath);
        AssertToken(main, tokens, "Stats.mean", "Stats", AuroraSemanticTokenTypes.Type);
        AssertToken(main, tokens, "Stats.mean", "mean", AuroraSemanticTokenTypes.MethodCall);
        AssertToken(main, tokens, "new Vec2", "Vec2", AuroraSemanticTokenTypes.Type);
        AssertToken(main, tokens, "vec.length", "length", AuroraSemanticTokenTypes.MethodCall);
        AssertToken(main, tokens, "return vec.x", "x", AuroraSemanticTokenTypes.Property);

        var globalTokens = service.GetSemanticTokens(globalsPath);
        AssertToken(globals, globalTokens, "declare type Stats", "Stats", AuroraSemanticTokenTypes.Type);
        AssertToken(globals, globalTokens, "declare type Vec2", "Vec2", AuroraSemanticTokenTypes.Type);
    }

    [Fact]
    public void AmbientRootsAreHiddenWhenALocalNameShadowsThem()
    {
        var service = CreateService();
        var mainPath = Path.Combine(_root, "main.as");
        var globalsPath = Path.Combine(_root, "globals.as");
        service.OpenOrUpdateDocument(globalsPath, "@global();\ndeclare type Stats { static func mean(Number a, Number b) Number; }");
        var main =
            """
            @module(MAIN);
            export func run() {
                var Stats = { mean: 1 };
                return Stats.mean;
            }
            """;
        service.OpenOrUpdateDocument(mainPath, main);

        var completions = service.GetCompletions(mainPath, PositionOf(main, "return Stats"));
        Assert.DoesNotContain(completions.Items, item => item.Detail == "declared type");

        var hover = service.GetHover(mainPath, PositionOfLast(main, "mean"));
        Assert.True(hover == null || !hover.Contents.Contains("func mean(Number a, Number b)", StringComparison.Ordinal));
    }

    public void Dispose()
    {
        if (Directory.Exists(_root))
        {
            Directory.Delete(_root, recursive: true);
        }
    }

    private AuroraLanguageService CreateService()
    {
        return new AuroraLanguageService(new AuroraLanguageServiceOptions(
            BuiltinApiLoader.LoadFromFile(BuiltinApiCatalogTests.GetRuntimeApiPath()))
        {
            BaseDirectory = _root
        });
    }

    private static void AssertToken(string source, SemanticTokensResult result, string context, string text, int type)
    {
        var contextIndex = source.IndexOf(context, StringComparison.Ordinal);
        Assert.True(contextIndex >= 0, $"Context '{context}' was not found.");
        Assert.Contains(result.Tokens, token =>
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

    private static TextPosition PositionOf(string source, string needle)
    {
        var offset = source.IndexOf(needle, StringComparison.Ordinal);
        Assert.True(offset >= 0, $"Needle '{needle}' not found.");
        return PositionAtOffset(source, offset);
    }

    private static TextPosition PositionOfLast(string source, string needle)
    {
        var offset = source.LastIndexOf(needle, StringComparison.Ordinal);
        Assert.True(offset >= 0, $"Needle '{needle}' not found.");
        return PositionAtOffset(source, offset);
    }

    private static TextPosition PositionAfter(string source, string needle)
    {
        var offset = source.IndexOf(needle, StringComparison.Ordinal);
        Assert.True(offset >= 0, $"Needle '{needle}' not found.");
        return PositionAtOffset(source, offset + needle.Length);
    }

    private static TextPosition PositionAtOffset(string source, int offset)
    {
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
