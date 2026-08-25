using AuroraScript.Compiler.Ast;
using AuroraScript.Compiler.Ast.Expressions;
using AuroraScript.Compiler.Ast.Statements;
using AuroraScript.LanguageServices.Features.Hover;
using AuroraScript.LanguageServices.Text;
using System;
using System.Collections.Generic;
using System.Text;

namespace AuroraScript.LanguageServices.Internal;

internal static class ScriptDocumentationQuery
{
    private const string MarkdownLanguageId = "aurorascript";

    public static bool TryGetHover(ModuleDeclaration module, string sourceName, string sourceText, TextPosition position, out HoverResult hover)
    {
        hover = null!;
        if (TryGetModuleHover(sourceText, position, out hover))
        {
            return true;
        }

        for (var i = 0; i < module.Functions.Count; i++)
        {
            var function = module.Functions[i];
            if (TryGetFunctionHover(sourceName, sourceText, function, position, out hover))
            {
                return true;
            }
        }

        for (var i = 0; i < module.Statements.Count; i++)
        {
            if (TryGetStatementHover(sourceName, sourceText, module.Statements[i], position, out hover))
            {
                return true;
            }
        }

        return false;
    }

    public static bool TryGetHoverAtDefinition(
        ModuleDeclaration module,
        string sourceName,
        string sourceText,
        TextRange definitionRange,
        TextRange hoverRange,
        out HoverResult hover)
    {
        hover = null!;
        if (TryFindFunction(module, definitionRange, out var function))
        {
            return TryBuildFunctionHover(sourceName, sourceText, function, hoverRange, out hover);
        }

        if (TryFindMemberDeclaration(module, sourceName, sourceText, definitionRange, hoverRange, out hover))
        {
            return true;
        }

        if (TryFindDeclaration(module, sourceText, definitionRange, hoverRange, out hover))
        {
            return true;
        }

        return false;
    }

    private static bool TryGetStatementHover(
        string sourceName,
        string sourceText,
        Compiler.Ast.Statements.Statement statement,
        TextPosition position,
        out HoverResult hover)
    {
        hover = null!;
        switch (statement)
        {
            case FunctionDeclaration function:
                return TryGetFunctionHover(sourceName, sourceText, function, position, out hover);
            case VariableDeclaration variable when variable.Name != null:
                if (!Contains(TextRange.FromSourceSpan(variable.Name.Range), position))
                {
                    return false;
                }

                return TryBuildDeclarationHover(
                    sourceName,
                    sourceText,
                    variable.Name.Value,
                    variable.IsConst ? "const" : "var",
                    variable.Name.Range.StartLine,
                    TextRange.FromSourceSpan(variable.Name.Range),
                    out hover);
            case EnumDeclaration enumDeclaration when enumDeclaration.Identifier != null:
                if (!Contains(TextRange.FromSourceSpan(enumDeclaration.Identifier.Range), position))
                {
                    return false;
                }

                return TryBuildDeclarationHover(
                    sourceName,
                    sourceText,
                    enumDeclaration.Identifier.Value,
                    "enum",
                    enumDeclaration.Identifier.Range.StartLine,
                    TextRange.FromSourceSpan(enumDeclaration.Identifier.Range),
                    out hover);
            case Compiler.Ast.Statements.BlockStatement block:
                for (var i = 0; i < block.Functions.Count; i++)
                {
                    if (TryGetFunctionHover(sourceName, sourceText, block.Functions[i], position, out hover))
                    {
                        return true;
                    }
                }

                for (var i = 0; i < block.Statements.Count; i++)
                {
                    if (TryGetStatementHover(sourceName, sourceText, block.Statements[i], position, out hover))
                    {
                        return true;
                    }
                }
                return false;
            default:
                return false;
        }
    }

    private static bool TryGetFunctionHover(
        string sourceName,
        string sourceText,
        FunctionDeclaration function,
        TextPosition position,
        out HoverResult hover)
    {
        hover = null!;
        if (function.Name == null ||
            !Contains(TextRange.FromSourceSpan(function.Name.Range), position))
        {
            return false;
        }

        return TryBuildFunctionHover(sourceName, sourceText, function, TextRange.FromSourceSpan(function.Name.Range), out hover);
    }

    private static bool TryBuildFunctionHover(
        string sourceName,
        string sourceText,
        FunctionDeclaration function,
        TextRange hoverRange,
        out HoverResult hover)
    {
        hover = null!;
        var documentationLine = GetDocumentationAnchorLine(function);
        var comments = ReadLeadingComments(sourceText, documentationLine);
        if (comments.Count == 0)
        {
            return false;
        }

        var builder = new StringBuilder();
        builder.Append("```").Append(MarkdownLanguageId).Append('\n');
        if (function.Access == MemberAccess.Export)
        {
            builder.Append("export ");
        }

        AppendFunctionSignature(
            builder,
            function.Name.Value,
            function.Parameters,
            function.ReturnType);
        builder.Append("\n```");
        AppendComments(builder, comments);
        hover = new HoverResult(builder.ToString(), hoverRange);
        return true;
    }

    private static bool TryFindFunction(ModuleDeclaration module, TextRange definitionRange, out FunctionDeclaration function)
    {
        function = null!;
        for (var i = 0; i < module.Functions.Count; i++)
        {
            if (TryFindFunction(module.Functions[i], definitionRange, out function))
            {
                return true;
            }
        }

        for (var i = 0; i < module.Statements.Count; i++)
        {
            if (TryFindFunction(module.Statements[i], definitionRange, out function))
            {
                return true;
            }
        }

        return false;
    }

    private static bool TryFindMemberDeclaration(
        ModuleDeclaration module,
        string sourceName,
        string sourceText,
        TextRange definitionRange,
        TextRange hoverRange,
        out HoverResult hover)
    {
        hover = null!;
        for (var i = 0; i < module.Functions.Count; i++)
        {
            if (TryFindMemberDeclaration(module.Functions[i], sourceName, sourceText, definitionRange, hoverRange, out hover))
            {
                return true;
            }
        }

        for (var i = 0; i < module.Statements.Count; i++)
        {
            if (TryFindMemberDeclaration(module.Statements[i], sourceName, sourceText, definitionRange, hoverRange, out hover))
            {
                return true;
            }
        }

        return false;
    }

    private static bool TryFindMemberDeclaration(
        Compiler.Ast.AstNode? node,
        string sourceName,
        string sourceText,
        TextRange definitionRange,
        TextRange hoverRange,
        out HoverResult hover)
    {
        hover = null!;
        switch (node)
        {
            case MapKeyValueExpression entry when entry.Key != null &&
                SameRange(TextRange.FromSourceSpan(entry.Key.Range), definitionRange):
                return TryBuildObjectMemberHover(
                    sourceName,
                    sourceText,
                    entry.Key.Value,
                    entry.Value,
                    entry.Key.Range.StartLine,
                    hoverRange,
                    out hover);
            case MapKeyValueExpression entry:
                return TryFindMemberDeclaration(entry.Value, sourceName, sourceText, definitionRange, hoverRange, out hover);
            case SetPropertyExpression setProperty when setProperty.Property is NameExpression property &&
                SameRange(TextRange.FromSourceSpan(property.Identifier.Range), definitionRange):
                return TryBuildObjectMemberHover(
                    sourceName,
                    sourceText,
                    property.Identifier.Value,
                    setProperty.Value,
                    property.Identifier.Range.StartLine,
                    hoverRange,
                    out hover);
            case SetPropertyExpression setProperty:
                return TryFindMemberDeclaration(setProperty.Object, sourceName, sourceText, definitionRange, hoverRange, out hover) ||
                    TryFindMemberDeclaration(setProperty.Value, sourceName, sourceText, definitionRange, hoverRange, out hover);
            case VariableDeclaration variable:
                return TryFindMemberDeclaration(variable.Initializer, sourceName, sourceText, definitionRange, hoverRange, out hover);
            case FunctionDeclaration function:
                return TryFindMemberDeclaration(function.Body, sourceName, sourceText, definitionRange, hoverRange, out hover);
            case Compiler.Ast.Statements.BlockStatement block:
                for (var i = 0; i < block.Functions.Count; i++)
                {
                    if (TryFindMemberDeclaration(block.Functions[i], sourceName, sourceText, definitionRange, hoverRange, out hover))
                    {
                        return true;
                    }
                }

                for (var i = 0; i < block.Statements.Count; i++)
                {
                    if (TryFindMemberDeclaration(block.Statements[i], sourceName, sourceText, definitionRange, hoverRange, out hover))
                    {
                        return true;
                    }
                }
                return false;
            case Compiler.Ast.Statements.ExpressionStatement expressionStatement:
                return TryFindMemberDeclaration(expressionStatement.Expression, sourceName, sourceText, definitionRange, hoverRange, out hover);
            case ReturnStatement returnStatement:
                return TryFindMemberDeclaration(returnStatement.Expression, sourceName, sourceText, definitionRange, hoverRange, out hover);
            case ThrowStatement throwStatement:
                return TryFindMemberDeclaration(throwStatement.Expression, sourceName, sourceText, definitionRange, hoverRange, out hover);
            case DeleteStatement deleteStatement:
                return TryFindMemberDeclaration(deleteStatement.Expression, sourceName, sourceText, definitionRange, hoverRange, out hover);
            case IfStatement ifStatement:
                return TryFindMemberDeclaration(ifStatement.Condition, sourceName, sourceText, definitionRange, hoverRange, out hover) ||
                    TryFindMemberDeclaration(ifStatement.Body, sourceName, sourceText, definitionRange, hoverRange, out hover) ||
                    TryFindMemberDeclaration(ifStatement.Else, sourceName, sourceText, definitionRange, hoverRange, out hover);
            case WhileStatement whileStatement:
                return TryFindMemberDeclaration(whileStatement.Condition, sourceName, sourceText, definitionRange, hoverRange, out hover) ||
                    TryFindMemberDeclaration(whileStatement.Body, sourceName, sourceText, definitionRange, hoverRange, out hover);
            case ForStatement forStatement:
                return TryFindMemberDeclaration(forStatement.Initializer, sourceName, sourceText, definitionRange, hoverRange, out hover) ||
                    TryFindMemberDeclaration(forStatement.Condition, sourceName, sourceText, definitionRange, hoverRange, out hover) ||
                    TryFindMemberDeclaration(forStatement.Incrementor, sourceName, sourceText, definitionRange, hoverRange, out hover) ||
                    TryFindMemberDeclaration(forStatement.Body, sourceName, sourceText, definitionRange, hoverRange, out hover);
            case ForInStatement forInStatement:
                return TryFindMemberDeclaration(forInStatement.Initializer, sourceName, sourceText, definitionRange, hoverRange, out hover) ||
                    TryFindMemberDeclaration(forInStatement.Iterator, sourceName, sourceText, definitionRange, hoverRange, out hover) ||
                    TryFindMemberDeclaration(forInStatement.Body, sourceName, sourceText, definitionRange, hoverRange, out hover);
            case TryStatement tryStatement:
                return TryFindMemberDeclaration(tryStatement.Body, sourceName, sourceText, definitionRange, hoverRange, out hover) ||
                    TryFindMemberDeclaration(tryStatement.CatchBody, sourceName, sourceText, definitionRange, hoverRange, out hover) ||
                    TryFindMemberDeclaration(tryStatement.FinallyBody, sourceName, sourceText, definitionRange, hoverRange, out hover);
            case AssignmentExpression assignment:
                return TryFindMemberDeclaration(assignment.Left, sourceName, sourceText, definitionRange, hoverRange, out hover) ||
                    TryFindMemberDeclaration(assignment.Right, sourceName, sourceText, definitionRange, hoverRange, out hover);
            case FunctionCallExpression call:
                if (TryFindMemberDeclaration(call.Target, sourceName, sourceText, definitionRange, hoverRange, out hover))
                {
                    return true;
                }

                for (var i = 0; i < call.Arguments.Count; i++)
                {
                    if (TryFindMemberDeclaration(call.Arguments[i], sourceName, sourceText, definitionRange, hoverRange, out hover))
                    {
                        return true;
                    }
                }
                return false;
            case MapExpression map:
                for (var i = 0; i < map.Entries.Count; i++)
                {
                    if (TryFindMemberDeclaration(map.Entries[i], sourceName, sourceText, definitionRange, hoverRange, out hover))
                    {
                        return true;
                    }
                }
                return false;
            case LambdaExpression lambda:
                return TryFindMemberDeclaration(lambda.Function, sourceName, sourceText, definitionRange, hoverRange, out hover);
            case NewExpression newExpression:
                return TryFindMemberDeclaration(newExpression.Expression, sourceName, sourceText, definitionRange, hoverRange, out hover);
            case GroupExpression group:
                for (var i = 0; i < group.Expressions.Count; i++)
                {
                    if (TryFindMemberDeclaration(group.Expressions[i], sourceName, sourceText, definitionRange, hoverRange, out hover))
                    {
                        return true;
                    }
                }
                return false;
            case ArrayLiteralExpression array:
                for (var i = 0; i < array.Elements.Count; i++)
                {
                    if (TryFindMemberDeclaration(array.Elements[i], sourceName, sourceText, definitionRange, hoverRange, out hover))
                    {
                        return true;
                    }
                }
                return false;
            default:
                return false;
        }
    }

    private static bool TryBuildObjectMemberHover(
        string sourceName,
        string sourceText,
        string name,
        Expression? value,
        int declarationStartLine,
        TextRange range,
        out HoverResult hover)
    {
        hover = null!;
        var comments = ReadLeadingComments(sourceText, declarationStartLine);
        if (comments.Count == 0)
        {
            return false;
        }

        var builder = new StringBuilder();
        builder.Append("```").Append(MarkdownLanguageId).Append('\n');
        if (value is LambdaExpression lambda)
        {
            AppendFunctionSignature(
                builder,
                name,
                lambda.Function.Parameters,
                lambda.Function.ReturnType);
        }
        else
        {
            builder.Append("property ").Append(name);
        }

        builder.Append("\n```");
        AppendComments(builder, comments);
        hover = new HoverResult(builder.ToString(), range);
        return true;
    }

    private static bool TryFindFunction(Compiler.Ast.AstNode? node, TextRange definitionRange, out FunctionDeclaration function)
    {
        function = null!;
        switch (node)
        {
            case FunctionDeclaration candidate when candidate.Name != null &&
                SameRange(TextRange.FromSourceSpan(candidate.Name.Range), definitionRange):
                function = candidate;
                return true;
            case FunctionDeclaration candidate:
                return TryFindFunction(candidate.Body, definitionRange, out function);
            case Compiler.Ast.Statements.BlockStatement block:
                for (var i = 0; i < block.Functions.Count; i++)
                {
                    if (TryFindFunction(block.Functions[i], definitionRange, out function))
                    {
                        return true;
                    }
                }

                for (var i = 0; i < block.Statements.Count; i++)
                {
                    if (TryFindFunction(block.Statements[i], definitionRange, out function))
                    {
                        return true;
                    }
                }
                return false;
            default:
                return false;
        }
    }

    private static void AppendFunctionSignature(
        StringBuilder builder,
        string name,
        IReadOnlyList<ParameterDeclaration> parameters,
        TypeReference returnType)
    {
        builder.Append("func ").Append(name).Append('(');
        for (var i = 0; i < parameters.Count; i++)
        {
            if (i > 0)
            {
                builder.Append(", ");
            }

            if (parameters[i].DeclaredType != null)
            {
                builder
                    .Append(parameters[i].DeclaredType.DisplayName)
                    .Append(' ');
            }

            if (parameters[i].IsSpreadOperator)
            {
                builder.Append("...");
            }

            builder.Append(parameters[i].Name.Value);
        }

        builder.Append(')');
        if (returnType != null)
        {
            builder.Append(' ').Append(returnType.DisplayName);
        }
    }

    private static bool TryFindDeclaration(
        ModuleDeclaration module,
        string sourceText,
        TextRange definitionRange,
        TextRange hoverRange,
        out HoverResult hover)
    {
        hover = null!;
        for (var i = 0; i < module.Statements.Count; i++)
        {
            if (TryFindDeclaration(module.Statements[i], sourceText, definitionRange, hoverRange, out hover))
            {
                return true;
            }
        }

        return false;
    }

    private static bool TryFindDeclaration(
        Compiler.Ast.AstNode? node,
        string sourceText,
        TextRange definitionRange,
        TextRange hoverRange,
        out HoverResult hover)
    {
        hover = null!;
        switch (node)
        {
            case VariableDeclaration variable when variable.Name != null &&
                SameRange(TextRange.FromSourceSpan(variable.Name.Range), definitionRange):
                return TryBuildDeclarationHover(
                    definitionRange.FileName,
                    sourceText,
                    variable.Name.Value,
                    variable.IsConst ? "const" : "var",
                    variable.Name.Range.StartLine,
                    hoverRange,
                    out hover);
            case EnumDeclaration enumDeclaration when enumDeclaration.Identifier != null &&
                SameRange(TextRange.FromSourceSpan(enumDeclaration.Identifier.Range), definitionRange):
                return TryBuildDeclarationHover(
                    definitionRange.FileName,
                    sourceText,
                    enumDeclaration.Identifier.Value,
                    "enum",
                    enumDeclaration.Identifier.Range.StartLine,
                    hoverRange,
                    out hover);
            case Compiler.Ast.Statements.BlockStatement block:
                for (var i = 0; i < block.Statements.Count; i++)
                {
                    if (TryFindDeclaration(block.Statements[i], sourceText, definitionRange, hoverRange, out hover))
                    {
                        return true;
                    }
                }
                return false;
            default:
                return false;
        }
    }

    private static bool TryBuildDeclarationHover(
        string sourceName,
        string sourceText,
        string name,
        string kind,
        int declarationStartLine,
        TextRange range,
        out HoverResult hover)
    {
        hover = null!;
        var comments = ReadLeadingComments(sourceText, declarationStartLine);
        if (comments.Count == 0)
        {
            return false;
        }

        var builder = new StringBuilder();
        builder.Append("```").Append(MarkdownLanguageId).Append('\n').Append(kind).Append(' ').Append(name).Append("\n```");
        AppendComments(builder, comments);
        hover = new HoverResult(builder.ToString(), range);
        return true;
    }

    private static bool TryGetModuleHover(string sourceText, TextPosition position, out HoverResult hover)
    {
        hover = null!;
        var module = FindModuleAnnotation(sourceText);
        if (!module.HasValue || !Contains(module.Value.Range, position))
        {
            return false;
        }

        var comments = ReadLeadingComments(sourceText, module.Value.StartLine + 1);
        if (comments.Count == 0)
        {
            return false;
        }

        var builder = new StringBuilder();
        builder.Append("```").Append(MarkdownLanguageId).Append("\n@module");
        if (!string.IsNullOrEmpty(module.Value.Argument))
        {
            builder.Append('(').Append(module.Value.Argument).Append(')');
        }
        builder.Append("\n```");
        AppendComments(builder, comments);
        hover = new HoverResult(builder.ToString(), module.Value.Range);
        return true;
    }

    private static int GetDocumentationAnchorLine(FunctionDeclaration function)
    {
        var line = function.Range.StartLine;
        for (var i = 0; i < function.Annotations.Count; i++)
        {
            var annotationLine = function.Annotations[i].Range.StartLine > 0
                ? function.Annotations[i].Range.StartLine
                : function.Annotations[i].Name.Range.StartLine;
            if (annotationLine > 0 && annotationLine < line)
            {
                line = annotationLine;
            }
        }

        return line;
    }

    private static IReadOnlyList<string> ReadLeadingComments(string sourceText, int declarationStartLine)
    {
        var lines = SplitLines(sourceText);
        var lineIndex = declarationStartLine - 2;
        while (lineIndex >= 0 && string.IsNullOrWhiteSpace(lines[lineIndex]))
        {
            lineIndex--;
        }

        if (lineIndex < 0)
        {
            return Array.Empty<string>();
        }

        var singleLine = new List<string>();
        while (lineIndex >= 0)
        {
            var trimmed = lines[lineIndex].TrimStart();
            if (!trimmed.StartsWith("//", StringComparison.Ordinal))
            {
                break;
            }

            singleLine.Add(CleanSingleLineComment(trimmed));
            lineIndex--;
        }

        if (singleLine.Count != 0)
        {
            singleLine.Reverse();
            return singleLine;
        }

        return ReadBlockCommentEndingAt(lines, lineIndex);
    }

    private static IReadOnlyList<string> ReadBlockCommentEndingAt(IReadOnlyList<string> lines, int lineIndex)
    {
        var line = lines[lineIndex];
        if (!line.TrimEnd().EndsWith("*/", StringComparison.Ordinal))
        {
            return Array.Empty<string>();
        }

        var blockLines = new List<string>();
        while (lineIndex >= 0)
        {
            blockLines.Add(lines[lineIndex]);
            if (lines[lineIndex].Contains("/*", StringComparison.Ordinal))
            {
                break;
            }

            lineIndex--;
        }

        if (lineIndex < 0)
        {
            return Array.Empty<string>();
        }

        blockLines.Reverse();
        var comments = new List<string>(blockLines.Count);
        for (var i = 0; i < blockLines.Count; i++)
        {
            var text = blockLines[i].Trim();
            if (i == 0)
            {
                var start = text.IndexOf("/*", StringComparison.Ordinal);
                text = start >= 0 ? text.Substring(start + 2) : text;
                if (text.StartsWith("*", StringComparison.Ordinal))
                {
                    text = text.Substring(1);
                }
            }

            if (i == blockLines.Count - 1)
            {
                var end = text.LastIndexOf("*/", StringComparison.Ordinal);
                text = end >= 0 ? text.Substring(0, end) : text;
            }

            text = text.Trim();
            if (text.StartsWith("*", StringComparison.Ordinal))
            {
                text = text.Substring(1).TrimStart();
            }

            if (!string.IsNullOrWhiteSpace(text))
            {
                comments.Add(text);
            }
        }

        return comments;
    }

    private static string CleanSingleLineComment(string trimmedLine)
    {
        var text = trimmedLine.Substring(2).TrimStart();
        if (text.StartsWith("/", StringComparison.Ordinal))
        {
            text = text.Substring(1).TrimStart();
        }

        return text;
    }

    private static void AppendComments(StringBuilder builder, IReadOnlyList<string> comments)
    {
        for (var i = 0; i < comments.Count; i++)
        {
            if (string.IsNullOrWhiteSpace(comments[i]))
            {
                continue;
            }

            builder.Append("\n\n").Append(comments[i]);
        }
    }

    private static ModuleAnnotation? FindModuleAnnotation(string sourceText)
    {
        var offset = 0;
        var line = 0;
        foreach (var textLine in SplitLines(sourceText))
        {
            var trimmed = textLine.TrimStart();
            var leading = textLine.Length - trimmed.Length;
            if (trimmed.StartsWith("//", StringComparison.Ordinal) ||
                trimmed.StartsWith("/*", StringComparison.Ordinal) ||
                string.IsNullOrWhiteSpace(trimmed))
            {
                offset += textLine.Length + 1;
                line++;
                continue;
            }

            if (!trimmed.StartsWith("@module", StringComparison.Ordinal))
            {
                return null;
            }

            var start = offset + leading;
            var end = start + "@module".Length;
            var argument = string.Empty;
            var open = trimmed.IndexOf('(');
            var close = trimmed.IndexOf(')', open + 1);
            if (open >= 0 && close > open)
            {
                argument = trimmed.Substring(open + 1, close - open - 1).Trim();
                end = start + close + 1;
            }

            return new ModuleAnnotation(
                new TextRange(string.Empty, new TextPosition(line, leading), new TextPosition(line, end - offset)),
                line,
                argument);
        }

        return null;
    }

    private static IReadOnlyList<string> SplitLines(string sourceText)
    {
        return sourceText.Replace("\r\n", "\n", StringComparison.Ordinal).Replace('\r', '\n').Split('\n');
    }

    private static bool Contains(TextRange range, TextPosition position)
    {
        if (position.Line < range.Start.Line || position.Line > range.End.Line)
        {
            return false;
        }

        if (position.Line == range.Start.Line && position.Character < range.Start.Character)
        {
            return false;
        }

        if (position.Line == range.End.Line && position.Character > range.End.Character)
        {
            return false;
        }

        return true;
    }

    private static bool SameRange(TextRange left, TextRange right)
    {
        return left.Start.Equals(right.Start) && left.End.Equals(right.End);
    }

    private readonly record struct ModuleAnnotation(TextRange Range, int StartLine, string Argument);
}
