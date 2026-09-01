using AuroraScript.LanguageServices.Diagnostics;
using AuroraScript.LanguageServices.Features.Completion;
using AuroraScript.LanguageServices.Features.Definition;
using AuroraScript.LanguageServices.Features.Formatting;
using AuroraScript.LanguageServices.Features.Hover;
using AuroraScript.LanguageServices.Features.References;
using AuroraScript.LanguageServices.Features.Rename;
using AuroraScript.LanguageServices.Features.SemanticTokens;
using AuroraScript.LanguageServices.Features.SignatureHelp;
using AuroraScript.LanguageServices.Text;
using System;
using System.Collections.Generic;
using System.Text.Json.Nodes;

namespace AuroraScript.LanguageServer.Protocol;

internal static class LspMapper
{
    private const int ContainerElementStyleStacked = 1;
    private const string AuroraTypeClassification = "AuroraScript.Type";
    private const string AuroraFunctionClassification = "AuroraScript.FunctionCall";

    public static JsonObject Position(TextPosition position)
    {
        return new JsonObject
        {
            ["line"] = position.Line,
            ["character"] = position.Character
        };
    }

    public static TextPosition ReadPosition(JsonObject parameters)
    {
        var position = parameters["position"]!.AsObject();
        return new TextPosition(
            position["line"]!.GetValue<int>(),
            position["character"]!.GetValue<int>());
    }

    public static string ReadTextDocumentUri(JsonObject parameters)
    {
        return parameters["textDocument"]!.AsObject()["uri"]!.GetValue<string>();
    }

    public static FormattingOptions ReadFormattingOptions(JsonObject parameters)
    {
        var tabSize = 4;
        var insertSpaces = true;
        if (parameters.TryGetPropertyValue("options", out var optionsNode) &&
            optionsNode is JsonObject options)
        {
            if (options.TryGetPropertyValue("tabSize", out var tabSizeNode) &&
                tabSizeNode != null &&
                tabSizeNode.GetValueKind() == System.Text.Json.JsonValueKind.Number)
            {
                tabSize = Math.Max(1, tabSizeNode.GetValue<int>());
            }

            if (options.TryGetPropertyValue("insertSpaces", out var insertSpacesNode) &&
                insertSpacesNode != null &&
                (insertSpacesNode.GetValueKind() == System.Text.Json.JsonValueKind.True ||
                 insertSpacesNode.GetValueKind() == System.Text.Json.JsonValueKind.False))
            {
                insertSpaces = insertSpacesNode.GetValue<bool>();
            }
        }

        return new FormattingOptions(tabSize, insertSpaces);
    }

    public static JsonObject Range(TextRange range)
    {
        return new JsonObject
        {
            ["start"] = Position(range.Start),
            ["end"] = Position(range.End)
        };
    }

    public static JsonObject Diagnostic(LanguageDiagnostic diagnostic)
    {
        return new JsonObject
        {
            ["range"] = Range(diagnostic.Range),
            ["severity"] = Severity(diagnostic.Severity),
            ["code"] = diagnostic.Code,
            ["source"] = "AuroraScript",
            ["message"] = diagnostic.Message
        };
    }

    public static JsonArray Diagnostics(IReadOnlyList<LanguageDiagnostic> diagnostics)
    {
        var array = new JsonArray();
        for (var i = 0; i < diagnostics.Count; i++)
        {
            array.Add(Diagnostic(diagnostics[i]));
        }
        return array;
    }

    public static JsonObject Hover(HoverResult hover)
    {
        return new JsonObject
        {
            ["contents"] = new JsonObject
            {
                ["kind"] = "markdown",
                ["value"] = hover.Contents
            },
            ["range"] = Range(hover.Range),
            ["_vs_rawContent"] = VisualStudioHoverContent(hover.Contents)
        };
    }

    /// <summary>
    /// Visual Studio's LSP client renders hover as plain text and only colorizes content supplied
    /// through the <c>_vs_rawContent</c> extension, so the markdown is republished as classified runs.
    /// </summary>
    private static JsonObject VisualStudioHoverContent(string markdown)
    {
        var elements = new JsonArray();
        var lines = HoverMarkup.Parse(markdown);
        for (var i = 0; i < lines.Count; i++)
        {
            var runs = new JsonArray();
            for (var j = 0; j < lines[i].Runs.Count; j++)
            {
                var run = lines[i].Runs[j];
                runs.Add(new JsonObject
                {
                    ["ClassificationTypeName"] = ClassificationName(run.Kind),
                    ["Text"] = run.Text,
                    ["MarkerTagType"] = null,
                    ["Style"] = 0,
                    ["Tooltip"] = null,
                    ["NavigationAction"] = null,
                    ["_vs_type"] = "ClassifiedTextRun"
                });
            }

            elements.Add(new JsonObject
            {
                ["Runs"] = runs,
                ["_vs_type"] = "ClassifiedTextElement"
            });
        }

        return new JsonObject
        {
            ["Elements"] = elements,
            ["Style"] = ContainerElementStyleStacked,
            ["_vs_type"] = "ContainerElement"
        };
    }

    private static string ClassificationName(HoverRunKind kind)
    {
        return kind switch
        {
            HoverRunKind.Keyword => "keyword",
            HoverRunKind.Type => AuroraTypeClassification,
            HoverRunKind.Function => AuroraFunctionClassification,
            HoverRunKind.Identifier => "identifier",
            HoverRunKind.Number => "number",
            HoverRunKind.String => "string",
            HoverRunKind.Comment => "comment",
            HoverRunKind.Operator => "operator",
            _ => "text"
        };
    }

    public static JsonArray CompletionItems(CompletionResult result)
    {
        var array = new JsonArray();
        for (var i = 0; i < result.Items.Count; i++)
        {
            var item = result.Items[i];
            array.Add(new JsonObject
            {
                ["label"] = item.Label,
                ["kind"] = CompletionKind(item.Kind),
                ["detail"] = item.Detail,
                ["documentation"] = new JsonObject
                {
                    ["kind"] = "markdown",
                    ["value"] = item.Documentation
                },
                ["data"] = new JsonObject
                {
                    ["readonly"] = item.ReadOnly
                }
            });
        }
        return array;
    }

    public static JsonObject SignatureHelp(SignatureHelpResult result)
    {
        var signatures = new JsonArray();
        for (var i = 0; i < result.Signatures.Count; i++)
        {
            signatures.Add(Signature(result.Signatures[i]));
        }

        return new JsonObject
        {
            ["signatures"] = signatures,
            ["activeSignature"] = result.ActiveSignature,
            ["activeParameter"] = result.ActiveParameter
        };
    }

    public static JsonObject Location(DefinitionLocation location)
    {
        return new JsonObject
        {
            ["uri"] = UriFromPath(location.Path),
            ["range"] = Range(location.Range)
        };
    }

    public static JsonObject Location(ReferenceLocation location)
    {
        return new JsonObject
        {
            ["uri"] = UriFromPath(location.Path),
            ["range"] = Range(location.Range)
        };
    }

    public static JsonArray Locations(IReadOnlyList<ReferenceLocation> locations)
    {
        var array = new JsonArray();
        for (var i = 0; i < locations.Count; i++)
        {
            array.Add(Location(locations[i]));
        }
        return array;
    }

    public static JsonObject WorkspaceEdit(RenameResult result)
    {
        var changes = new JsonObject();
        if (result.Success)
        {
            for (var i = 0; i < result.Changes.Count; i++)
            {
                var change = result.Changes[i];
                var edits = new JsonArray();
                for (var j = 0; j < change.Edits.Count; j++)
                {
                    var edit = change.Edits[j];
                    edits.Add(new JsonObject
                    {
                        ["range"] = Range(edit.Range),
                        ["newText"] = edit.NewText
                    });
                }

                changes[UriFromPath(change.Path)] = edits;
            }
        }

        var editObject = new JsonObject
        {
            ["changes"] = changes
        };
        if (!result.Success && !string.IsNullOrEmpty(result.ErrorMessage))
        {
            editObject["failureReason"] = result.ErrorMessage;
        }

        return editObject;
    }

    public static JsonArray TextEdits(IReadOnlyList<TextEdit> edits)
    {
        var array = new JsonArray();
        for (var i = 0; i < edits.Count; i++)
        {
            var edit = edits[i];
            array.Add(new JsonObject
            {
                ["range"] = Range(edit.Range),
                ["newText"] = edit.NewText
            });
        }

        return array;
    }

    public static JsonObject SemanticTokenLegend()
    {
        var tokenTypes = new JsonArray();
        for (var i = 0; i < AuroraSemanticTokenTypes.Legend.Length; i++)
        {
            tokenTypes.Add(AuroraSemanticTokenTypes.Legend[i]);
        }

        return new JsonObject
        {
            ["tokenTypes"] = tokenTypes,
            ["tokenModifiers"] = new JsonArray()
        };
    }

    public static JsonObject SemanticTokens(SemanticTokensResult result)
    {
        var data = new JsonArray();
        var previousLine = 0;
        var previousCharacter = 0;
        for (var i = 0; i < result.Tokens.Count; i++)
        {
            var token = result.Tokens[i];
            var deltaLine = token.Line - previousLine;
            var deltaStart = deltaLine == 0 ? token.Character - previousCharacter : token.Character;
            data.Add(deltaLine);
            data.Add(deltaStart);
            data.Add(token.Length);
            data.Add(token.Type);
            data.Add(token.Modifiers);
            previousLine = token.Line;
            previousCharacter = token.Character;
        }

        return new JsonObject
        {
            ["data"] = data
        };
    }

    private static string UriFromPath(string path)
    {
        if (System.IO.Path.IsPathRooted(path))
        {
            return new System.Uri(path).AbsoluteUri;
        }

        return path;
    }

    private static JsonObject Signature(SignatureInformation signature)
    {
        var parameters = new JsonArray();
        for (var i = 0; i < signature.Parameters.Count; i++)
        {
            parameters.Add(new JsonObject
            {
                ["label"] = signature.Parameters[i].Label,
                ["documentation"] = signature.Parameters[i].Documentation
            });
        }

        return new JsonObject
        {
            ["label"] = signature.Label,
            ["documentation"] = new JsonObject
            {
                ["kind"] = "markdown",
                ["value"] = signature.Documentation
            },
            ["parameters"] = parameters
        };
    }

    private static int Severity(LanguageDiagnosticSeverity severity)
    {
        return severity switch
        {
            LanguageDiagnosticSeverity.Error => 1,
            LanguageDiagnosticSeverity.Warning => 2,
            LanguageDiagnosticSeverity.Information => 3,
            _ => 3
        };
    }

    private static int CompletionKind(CompletionItemKind kind)
    {
        return kind switch
        {
            CompletionItemKind.Text => 1,
            CompletionItemKind.Method => 2,
            CompletionItemKind.Function => 3,
            CompletionItemKind.Constructor => 4,
            CompletionItemKind.Variable => 6,
            CompletionItemKind.Property => 10,
            CompletionItemKind.Module => 9,
            CompletionItemKind.Enum => 13,
            CompletionItemKind.Type => 7,
            CompletionItemKind.Constant => 21,
            CompletionItemKind.Object => 23,
            _ => 1
        };
    }
}
