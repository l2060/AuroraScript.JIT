using AuroraScript.Compiler.Ast;
using AuroraScript.Compiler.Ast.Expressions;
using AuroraScript.Compiler.Ast.Statements;
using AuroraScript.LanguageServices.Text;
using System.Collections.Generic;

namespace AuroraScript.LanguageServices.Internal;

internal static class AstQuery
{
    public static AstQueryContext? Find(AstNode root, TextPosition position)
    {
        var state = new QueryState(position);
        Visit(root, state);
        if (state.Name == null && state.PropertyAccess == null && state.Call == null)
        {
            return null;
        }

        return new AstQueryContext
        {
            Expression = state.Expression,
            Name = state.Name,
            PropertyAccess = state.PropertyAccess,
            Call = state.Call,
            IsOnPropertyOwner = state.IsOnPropertyOwner,
            IsOnPropertyName = state.IsOnPropertyName,
            IsAfterMemberAccessDot = state.IsAfterMemberAccessDot
        };
    }

    private static void Visit(AstNode? node, QueryState state)
    {
        if (node == null)
        {
            return;
        }

        if (node.Range.IsValid() && !node.Range.Contains(state.Position))
        {
            return;
        }

        switch (node)
        {
            case ModuleDeclaration module:
                VisitModule(module, state);
                return;
            case BlockStatement block:
                VisitBlock(block, state);
                return;
            case FunctionDeclaration function:
                VisitFunction(function, state);
                return;
            case VariableDeclaration variable:
                Visit(variable.Initializer, state);
                return;
            case ExpressionStatement expressionStatement:
                Visit(expressionStatement.Expression, state);
                return;
            case NameExpression name:
                state.Expression = name;
                state.Name = name;
                return;
            case GetPropertyExpression getProperty:
                state.Expression = getProperty;
                VisitGetProperty(getProperty, state);
                return;
            case FunctionCallExpression call:
                VisitFunctionCall(call, state);
                return;
            case NewExpression newExpression:
                Visit(newExpression.Expression, state);
                return;
            case AssignmentExpression assignment:
                Visit(assignment.Left, state);
                Visit(assignment.Right, state);
                return;
            case CompoundExpression compound:
                Visit(compound.Left, state);
                Visit(compound.Right, state);
                return;
            case BinaryExpression binary:
                Visit(binary.Left, state);
                Visit(binary.Right, state);
                return;
            case IncludedExpression included:
                Visit(included.Left, state);
                Visit(included.Right, state);
                return;
            case InExpression inExpression:
                Visit(inExpression.Left, state);
                Visit(inExpression.Right, state);
                return;
            case CastTypeExpression cast:
                Visit(cast.Expression, state);
                Visit(cast.Typed, state);
                return;
            case PrefixUnaryExpression prefixUnary:
                Visit(prefixUnary.Expression, state);
                return;
            case GetElementExpression getElement:
                Visit(getElement.Object, state);
                Visit(getElement.Index, state);
                return;
            case SetPropertyExpression setProperty:
                Visit(setProperty.Object, state);
                Visit(setProperty.Property, state);
                Visit(setProperty.Value, state);
                return;
            case SetElementExpression setElement:
                Visit(setElement.Object, state);
                Visit(setElement.Index, state);
                Visit(setElement.Value, state);
                return;
            case GroupExpression group:
                VisitList(group.Expressions, state);
                return;
            case ArrayLiteralExpression array:
                VisitList(array.Elements, state);
                return;
            case MapExpression map:
                VisitList(map.Entries, state);
                return;
            case MapKeyValueExpression mapEntry:
                Visit(mapEntry.Value, state);
                return;
            case TemplateStringExpression template:
                for (var i = 0; i < template.Parts.Count; i++)
                {
                    Visit(template.Parts[i].Expression, state);
                }
                return;
            case LambdaExpression lambda:
                VisitFunction(lambda.Function, state);
                return;
            case ReturnStatement returnStatement:
                Visit(returnStatement.Expression, state);
                return;
            case ThrowStatement throwStatement:
                Visit(throwStatement.Expression, state);
                return;
            case DeleteStatement deleteStatement:
                Visit(deleteStatement.Expression, state);
                return;
            case IfStatement ifStatement:
                Visit(ifStatement.Condition, state);
                Visit(ifStatement.Body, state);
                Visit(ifStatement.Else, state);
                return;
            case WhileStatement whileStatement:
                Visit(whileStatement.Condition, state);
                Visit(whileStatement.Body, state);
                return;
            case ForStatement forStatement:
                Visit(forStatement.Initializer, state);
                Visit(forStatement.Condition, state);
                Visit(forStatement.Incrementor, state);
                Visit(forStatement.Body, state);
                return;
            case ForInStatement forInStatement:
                Visit(forInStatement.Initializer, state);
                Visit(forInStatement.Iterator, state);
                Visit(forInStatement.Body, state);
                return;
            case TryStatement tryStatement:
                Visit(tryStatement.Body, state);
                Visit(tryStatement.CatchBody, state);
                Visit(tryStatement.FinallyBody, state);
                return;
        }
    }

    private static void VisitModule(ModuleDeclaration module, QueryState state)
    {
        VisitList(module.Statements, state);
        VisitList(module.Functions, state);
    }

    private static void VisitBlock(BlockStatement block, QueryState state)
    {
        VisitList(block.Functions, state);
        VisitList(block.Statements, state);
    }

    private static void VisitFunction(FunctionDeclaration function, QueryState state)
    {
        for (var i = 0; i < function.Parameters.Count; i++)
        {
            Visit(function.Parameters[i], state);
        }
        Visit(function.Body, state);
    }

    private static void VisitGetProperty(GetPropertyExpression node, QueryState state)
    {
        state.PropertyAccess = node;
        var propertyRange = node.Property is NameExpression name
            ? name.Identifier.Range
            : node.Property.Range;
        var onPropertyName = propertyRange.Contains(state.Position);
        if (onPropertyName)
        {
            state.IsOnPropertyName = true;
        }

        if (node.Object is NameExpression objectName &&
            objectName.Identifier.Range.Contains(state.Position))
        {
            state.IsOnPropertyOwner = true;
        }

        var dotColumn = node.Object.Range.EndColumn;
        var positionLine = state.Position.Line + 1;
        var positionColumn = state.Position.Character + 1;
        if (positionLine == node.Object.Range.EndLine && positionColumn >= dotColumn)
        {
            state.IsAfterMemberAccessDot = true;
        }

        Visit(node.Object, state);
        Visit(node.Property, state);
    }

    private static void VisitFunctionCall(FunctionCallExpression node, QueryState state)
    {
        state.Call = node;
        Visit(node.Target, state);
        VisitList(node.Arguments, state);
    }

    private static void VisitList<T>(IReadOnlyList<T> nodes, QueryState state) where T : AstNode
    {
        for (var i = 0; i < nodes.Count; i++)
        {
            Visit(nodes[i], state);
        }
    }

    private sealed class QueryState
    {
        public QueryState(TextPosition position)
        {
            Position = position;
        }

        public TextPosition Position { get; }
        public Expression? Expression { get; set; }
        public NameExpression? Name { get; set; }
        public GetPropertyExpression? PropertyAccess { get; set; }
        public FunctionCallExpression? Call { get; set; }
        public bool IsOnPropertyOwner { get; set; }
        public bool IsOnPropertyName { get; set; }
        public bool IsAfterMemberAccessDot { get; set; }
    }
}
