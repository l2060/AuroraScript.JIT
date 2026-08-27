using AuroraScript.Compiler.Ast;
using AuroraScript.Compiler.Ast.Expressions;
using AuroraScript.LanguageServices.Features.SignatureHelp;
using AuroraScript.LanguageServices.Internal.SymbolIndex;
using AuroraScript.LanguageServices.Text;
using System.Collections.Generic;
using System.Text;

namespace AuroraScript.LanguageServices.Internal;

internal static class ScriptSignatureQuery
{
    public static SignatureHelpResult? TryGetSignatureHelp(
        AuroraWorkspaceIndex index,
        string path,
        ModuleDeclaration module,
        string sourceText,
        AstQueryContext context,
        TextPosition position)
    {
        var call = context.Call;
        if (call == null)
        {
            return null;
        }

        if (!TryGetCallTargetPosition(call, out var targetPosition))
        {
            return null;
        }

        var definition = AuroraDefinitionResolver.Resolve(index, path, targetPosition);
        if (definition == null || BuiltinDefinitionDocuments.IsBuiltinUri(definition.Path))
        {
            return null;
        }

        var targetModule = index.TryGetModule(definition.Path);
        if (targetModule == null)
        {
            return null;
        }

        if (!TryGetCallable(targetModule.Module, definition.Range, out var name, out var parameters, out var returnType))
        {
            return null;
        }

        var label = FormatSignature(name, parameters, returnType);
        var signatureParameters = new List<SignatureParameter>(parameters.Count);
        for (var i = 0; i < parameters.Count; i++)
        {
            signatureParameters.Add(new SignatureParameter(FormatParameter(parameters[i]), GetParameterType(parameters[i])));
        }

        ScriptDocumentationQuery.TryGetHoverAtDefinition(
            targetModule.Module,
            targetModule.Path,
            targetModule.Text,
            definition.Range,
            definition.Range,
            out var hover);

        var signature = new SignatureInformation(label, hover?.Contents ?? label, signatureParameters);
        return new SignatureHelpResult(
            new[] { signature },
            0,
            BuiltinQuery.GetActiveParameter(call, position));
    }

    private static bool TryGetCallTargetPosition(FunctionCallExpression call, out TextPosition position)
    {
        position = default;
        if (call.Target is NameExpression name)
        {
            position = TextRange.FromSourceSpan(name.Identifier.Range).Start;
            return true;
        }

        if (call.Target is GetPropertyExpression { Property: NameExpression property })
        {
            position = TextRange.FromSourceSpan(property.Identifier.Range).Start;
            return true;
        }

        return false;
    }

    private static bool TryGetCallable(
        ModuleDeclaration module,
        TextRange definitionRange,
        out string name,
        out IReadOnlyList<ParameterDeclaration> parameters,
        out TypeReference? returnType)
    {
        name = string.Empty;
        parameters = [];
        returnType = null;
        if (TryFindFunction(module, definitionRange, out var function) && function.Name != null)
        {
            name = function.Name.Value;
            parameters = function.Parameters;
            returnType = function.ReturnType;
            return true;
        }

        return TryFindLambdaFunctions(module, definitionRange, out name, out parameters, out returnType);
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

    private static bool TryFindFunction(AstNode? node, TextRange definitionRange, out FunctionDeclaration function)
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

    private static bool TryFindLambda(
        AstNode? node,
        TextRange definitionRange,
        out string name,
        out IReadOnlyList<ParameterDeclaration> parameters,
        out TypeReference? returnType)
    {
        name = string.Empty;
        parameters = [];
        returnType = null;
        switch (node)
        {
            case MapKeyValueExpression entry when entry.Key != null &&
                SameRange(TextRange.FromSourceSpan(entry.Key.Range), definitionRange) &&
                entry.Value is LambdaExpression lambda:
                name = entry.Key.Value;
                parameters = lambda.Function.Parameters;
                returnType = lambda.Function.ReturnType;
                return true;
            case MapKeyValueExpression entry:
                return TryFindLambda(entry.Value, definitionRange, out name, out parameters, out returnType);
            case SetPropertyExpression setProperty when setProperty.Property is NameExpression property &&
                SameRange(TextRange.FromSourceSpan(property.Identifier.Range), definitionRange) &&
                setProperty.Value is LambdaExpression assigned:
                name = property.Identifier.Value;
                parameters = assigned.Function.Parameters;
                returnType = assigned.Function.ReturnType;
                return true;
            case FunctionDeclaration function:
                return TryFindLambda(function.Body, definitionRange, out name, out parameters, out returnType);
            case Compiler.Ast.Statements.BlockStatement block:
                for (var i = 0; i < block.Functions.Count; i++)
                {
                    if (TryFindLambda(block.Functions[i], definitionRange, out name, out parameters, out returnType))
                    {
                        return true;
                    }
                }

                for (var i = 0; i < block.Statements.Count; i++)
                {
                    if (TryFindLambda(block.Statements[i], definitionRange, out name, out parameters, out returnType))
                    {
                        return true;
                    }
                }

                return false;
            case VariableDeclaration variable:
                return TryFindLambda(variable.Initializer, definitionRange, out name, out parameters, out returnType);
            case Compiler.Ast.Statements.ExpressionStatement expression:
                return TryFindLambda(expression.Expression, definitionRange, out name, out parameters, out returnType);
            case Compiler.Ast.Statements.ReturnStatement returnStatement:
                return TryFindLambda(returnStatement.Expression, definitionRange, out name, out parameters, out returnType);
            case MapExpression map:
                for (var i = 0; i < map.Entries.Count; i++)
                {
                    if (TryFindLambda(map.Entries[i], definitionRange, out name, out parameters, out returnType))
                    {
                        return true;
                    }
                }

                return false;
            default:
                return false;
        }
    }

    private static bool TryFindLambdaFunctions(
        ModuleDeclaration module,
        TextRange definitionRange,
        out string name,
        out IReadOnlyList<ParameterDeclaration> parameters,
        out TypeReference? returnType)
    {
        for (var i = 0; i < module.Functions.Count; i++)
        {
            if (TryFindLambda(module.Functions[i], definitionRange, out name, out parameters, out returnType))
            {
                return true;
            }
        }

        for (var i = 0; i < module.Statements.Count; i++)
        {
            if (TryFindLambda(module.Statements[i], definitionRange, out name, out parameters, out returnType))
            {
                return true;
            }
        }

        name = string.Empty;
        parameters = [];
        returnType = null;
        return false;
    }

    private static string FormatSignature(
        string name,
        IReadOnlyList<ParameterDeclaration> parameters,
        TypeReference? returnType)
    {
        var builder = new StringBuilder();
        builder.Append("func ").Append(name).Append('(');
        for (var i = 0; i < parameters.Count; i++)
        {
            if (i > 0)
            {
                builder.Append(", ");
            }

            builder.Append(FormatParameter(parameters[i]));
        }

        builder.Append(')');
        if (returnType != null)
        {
            builder.Append(' ').Append(returnType.DisplayName);
        }

        return builder.ToString();
    }

    private static string FormatParameter(ParameterDeclaration parameter)
    {
        var builder = new StringBuilder();
        if (parameter.DeclaredType != null)
        {
            builder.Append(parameter.DeclaredType.DisplayName).Append(' ');
        }

        if (parameter.IsSpreadOperator)
        {
            builder.Append("...");
        }

        builder.Append(parameter.Name.Value);
        return builder.ToString();
    }

    private static string GetParameterType(ParameterDeclaration parameter)
    {
        return parameter.DeclaredType?.DisplayName ?? string.Empty;
    }

    private static bool SameRange(TextRange left, TextRange right)
    {
        return left.Start.Equals(right.Start) && left.End.Equals(right.End);
    }
}
