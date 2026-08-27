using AuroraScript.Compiler;
using AuroraScript.Compiler.Ast;
using AuroraScript.Compiler.Ast.Expressions;
using AuroraScript.Compiler.Ast.Statements;
using AuroraScript.LanguageServices.Features.Definition;
using AuroraScript.LanguageServices.Text;
using System;
using System.Collections.Generic;

namespace AuroraScript.LanguageServices.Internal.SymbolIndex;

internal sealed class AuroraObjectMemberIndex
{
    internal readonly record struct ObjectMemberInfo(string Name, TextRange Range, bool IsMethod);

    private readonly AuroraModuleIndex _module;
    private readonly Dictionary<string, Dictionary<string, ObjectMemberInfo>> _membersByObject = new(System.StringComparer.Ordinal);

    private AuroraObjectMemberIndex(AuroraModuleIndex module)
    {
        _module = module;
    }

    public static AuroraObjectMemberIndex Build(AuroraModuleIndex module)
    {
        var index = new AuroraObjectMemberIndex(module);
        index.VisitModule(module.Module);
        return index;
    }

    public static AuroraObjectMemberIndex Build(AuroraModuleIndex module, AstNode root)
    {
        var index = new AuroraObjectMemberIndex(module);
        index.VisitRoot(root);
        return index;
    }

    public bool TryGetDefinition(GetPropertyExpression propertyAccess, out DefinitionLocation definition)
    {
        definition = null!;
        if (propertyAccess.Object is not NameExpression owner ||
            propertyAccess.Property is not NameExpression property ||
            !TryGetMember(owner.Identifier.Value, property.Identifier.Value, out var range))
        {
            return false;
        }

        definition = new DefinitionLocation(_module.Path, range);
        return true;
    }

    public bool TryGetMember(string ownerName, string memberName, out TextRange range)
    {
        if (_membersByObject.TryGetValue(ownerName, out var members) &&
            members.TryGetValue(memberName, out var member))
        {
            range = member.Range;
            return true;
        }

        range = default;
        return false;
    }

    public IReadOnlyList<ObjectMemberInfo> GetMembers(string ownerName)
    {
        if (!_membersByObject.TryGetValue(ownerName, out var members) || members.Count == 0)
        {
            return Array.Empty<ObjectMemberInfo>();
        }

        var result = new List<ObjectMemberInfo>(members.Count);
        foreach (var pair in members)
        {
            result.Add(pair.Value);
        }

        return result;
    }

    private void VisitModule(ModuleDeclaration module)
    {
        for (var i = 0; i < module.Statements.Count; i++)
        {
            VisitStatement(module.Statements[i]);
        }

        for (var i = 0; i < module.Functions.Count; i++)
        {
            VisitFunction(module.Functions[i]);
        }
    }

    private void VisitRoot(AstNode root)
    {
        switch (root)
        {
            case ModuleDeclaration module:
                VisitModule(module);
                break;
            case FunctionDeclaration function:
                VisitFunction(function);
                break;
            case BlockStatement block:
                VisitBlock(block);
                break;
            case Statement statement:
                VisitStatement(statement);
                break;
            case Expression expression:
                VisitExpression(expression);
                break;
        }
    }

    private void VisitFunction(FunctionDeclaration function)
    {
        if (function.Body is BlockStatement body)
        {
            VisitBlock(body);
        }
    }

    private void VisitBlock(BlockStatement block)
    {
        for (var i = 0; i < block.Statements.Count; i++)
        {
            VisitStatement(block.Statements[i]);
        }

        for (var i = 0; i < block.Functions.Count; i++)
        {
            VisitFunction(block.Functions[i]);
        }
    }

    private void VisitStatement(Statement? statement)
    {
        switch (statement)
        {
            case VariableDeclaration variable:
                VisitVariable(variable);
                break;
            case ExpressionStatement expressionStatement:
                VisitExpression(expressionStatement.Expression);
                break;
            case ReturnStatement returnStatement:
                VisitExpression(returnStatement.Expression);
                break;
            case ThrowStatement throwStatement:
                VisitExpression(throwStatement.Expression);
                break;
            case DeleteStatement deleteStatement:
                VisitExpression(deleteStatement.Expression);
                break;
            case IfStatement ifStatement:
                VisitExpression(ifStatement.Condition);
                VisitStatement(ifStatement.Body);
                VisitNode(ifStatement.Else);
                break;
            case WhileStatement whileStatement:
                VisitExpression(whileStatement.Condition);
                VisitStatement(whileStatement.Body);
                break;
            case ForStatement forStatement:
                VisitNode(forStatement.Initializer);
                VisitExpression(forStatement.Condition);
                VisitExpression(forStatement.Incrementor);
                VisitStatement(forStatement.Body);
                break;
            case ForInStatement forInStatement:
                VisitStatement(forInStatement.Initializer);
                VisitExpression(forInStatement.Iterator);
                VisitStatement(forInStatement.Body);
                break;
            case TryStatement tryStatement:
                VisitStatement(tryStatement.Body);
                VisitStatement(tryStatement.CatchBody);
                VisitStatement(tryStatement.FinallyBody);
                break;
            case BlockStatement block:
                VisitBlock(block);
                break;
        }
    }

    private void VisitVariable(VariableDeclaration variable)
    {
        if (variable.Name != null && variable.Initializer is MapExpression map)
        {
            AddObjectMembers(variable.Name.Value, map);
        }

        VisitExpression(variable.Initializer);
    }

    private void VisitNode(AstNode? node)
    {
        switch (node)
        {
            case Statement statement:
                VisitStatement(statement);
                break;
            case Expression expression:
                VisitExpression(expression);
                break;
        }
    }

    private void VisitExpression(Expression? expression)
    {
        switch (expression)
        {
            case AssignmentExpression assignment:
                VisitAssignment(assignment);
                VisitExpression(assignment.Right);
                return;
            case SetPropertyExpression setProperty:
                VisitSetProperty(setProperty);
                VisitExpression(setProperty.Value);
                return;
            case FunctionCallExpression call:
                VisitExpression(call.Target);
                for (var i = 0; i < call.Arguments.Count; i++)
                {
                    VisitExpression(call.Arguments[i]);
                }
                return;
            case MapExpression map:
                for (var i = 0; i < map.Entries.Count; i++)
                {
                    VisitExpression(map.Entries[i]);
                }
                return;
            case MapKeyValueExpression mapEntry:
                VisitExpression(mapEntry.Value);
                return;
            case LambdaExpression lambda:
                VisitFunction(lambda.Function);
                return;
            case GetPropertyExpression property:
                VisitExpression(property.Object);
                return;
            case SetElementExpression setElement:
                VisitExpression(setElement.Object);
                VisitExpression(setElement.Index);
                VisitExpression(setElement.Value);
                return;
            case GetElementExpression getElement:
                VisitExpression(getElement.Object);
                VisitExpression(getElement.Index);
                return;
            case GroupExpression group:
                for (var i = 0; i < group.Expressions.Count; i++)
                {
                    VisitExpression(group.Expressions[i]);
                }
                return;
            case ArrayLiteralExpression array:
                for (var i = 0; i < array.Elements.Count; i++)
                {
                    VisitExpression(array.Elements[i]);
                }
                return;
            case TemplateStringExpression template:
                for (var i = 0; i < template.Parts.Count; i++)
                {
                    VisitExpression(template.Parts[i].Expression);
                }
                return;
            case NewExpression newExpression:
                VisitExpression(newExpression.Expression);
                return;
            case OperatorExpression operatorExpression:
                VisitOperatorExpression(operatorExpression);
                return;
        }
    }

    private void VisitOperatorExpression(OperatorExpression expression)
    {
        switch (expression)
        {
            case BinaryExpression binary:
                VisitExpression(binary.Left);
                VisitExpression(binary.Right);
                break;
            case IncludedExpression included:
                VisitExpression(included.Left);
                VisitExpression(included.Right);
                break;
            case InExpression inExpression:
                VisitExpression(inExpression.Left);
                VisitExpression(inExpression.Right);
                break;
            case CompoundExpression compound:
                VisitExpression(compound.Left);
                VisitExpression(compound.Right);
                break;
            case PrefixUnaryExpression unary:
                VisitExpression(unary.Expression);
                break;
        }
    }

    private void VisitAssignment(AssignmentExpression assignment)
    {
        if (assignment.Left is GetPropertyExpression property)
        {
            VisitSetProperty(new SetPropertyExpression(property.Object, property.Property, assignment.Right));
        }
        else
        {
            VisitExpression(assignment.Left);
        }
    }

    private void VisitSetProperty(SetPropertyExpression property)
    {
        if (property.Object is NameExpression owner &&
            property.Property is NameExpression propertyName)
        {
            AddMember(
                owner.Identifier.Value,
                propertyName.Identifier.Value,
                TextRange.FromSourceSpan(propertyName.Identifier.Range),
                overwrite: false,
                isMethod: property.Value is LambdaExpression);
        }

        VisitExpression(property.Object);
    }

    private void AddObjectMembers(string ownerName, MapExpression map)
    {
        for (var i = 0; i < map.Entries.Count; i++)
        {
            if (map.Entries[i] is MapKeyValueExpression entry &&
                !string.IsNullOrEmpty(entry.Key?.Value))
            {
                AddMember(
                    ownerName,
                    entry.Key.Value,
                    TextRange.FromSourceSpan(entry.Key.Range),
                    overwrite: true,
                    isMethod: entry.Value is LambdaExpression);
            }
        }
    }

    private void AddMember(string ownerName, string memberName, TextRange range, bool overwrite, bool isMethod)
    {
        if (!_membersByObject.TryGetValue(ownerName, out var members))
        {
            members = new Dictionary<string, ObjectMemberInfo>(System.StringComparer.Ordinal);
            _membersByObject.Add(ownerName, members);
        }

        if (overwrite || !members.ContainsKey(memberName))
        {
            members[memberName] = new ObjectMemberInfo(memberName, range, isMethod);
        }
    }
}
