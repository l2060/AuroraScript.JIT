using AuroraScript.Compiler;
using AuroraScript.Compiler.Ast;
using AuroraScript.Compiler.Ast.Expressions;
using AuroraScript.Compiler.Ast.Statements;
using AuroraScript.LanguageServices.Builtins;
using AuroraScript.LanguageServices.Diagnostics;
using AuroraScript.LanguageServices.Parsing;
using AuroraScript.LanguageServices.Text;
using System;
using System.Collections.Generic;

namespace AuroraScript.LanguageServices.Semantics;

public sealed class AuroraSemanticAnalyzer
{
    private readonly BuiltinApiCatalog _builtins;

    public AuroraSemanticAnalyzer(BuiltinApiCatalog builtins)
    {
        _builtins = builtins ?? throw new ArgumentNullException(nameof(builtins));
    }

    public AuroraSemanticAnalysis Analyze(AuroraParseResult parseResult)
    {
        ArgumentNullException.ThrowIfNull(parseResult);

        var diagnostics = new List<LanguageDiagnostic>();
        if (parseResult.Diagnostics.Count != 0)
        {
            diagnostics.AddRange(parseResult.Diagnostics);
        }

        if (parseResult.Module != null)
        {
            var walker = new BuiltinReadonlyAssignmentWalker(_builtins, diagnostics);
            walker.Visit(parseResult.Module);
        }

        return new AuroraSemanticAnalysis(diagnostics);
    }

    public bool TryResolveBuiltinGlobal(string name, out SemanticSymbol symbol)
    {
        if (_builtins.TryGetGlobal(name, out var builtin))
        {
            symbol = SemanticSymbol.FromBuiltinGlobal(builtin);
            return true;
        }

        symbol = null!;
        return false;
    }

    public bool TryResolveBuiltinMember(string ownerName, string memberName, out SemanticSymbol symbol)
    {
        if (_builtins.TryGetGlobalMember(ownerName, memberName, out var member))
        {
            symbol = SemanticSymbol.FromBuiltinMember(member);
            return true;
        }

        symbol = null!;
        return false;
    }

    private sealed class BuiltinReadonlyAssignmentWalker
    {
        private readonly BuiltinApiCatalog _builtins;
        private readonly List<LanguageDiagnostic> _diagnostics;
        private readonly Stack<HashSet<string>> _scopes = new();
        private readonly HashSet<string> _importAliases = new(StringComparer.Ordinal);
        private readonly Dictionary<string, BuiltinApiModule> _builtinModuleAliases = new(StringComparer.Ordinal);

        public BuiltinReadonlyAssignmentWalker(BuiltinApiCatalog builtins, List<LanguageDiagnostic> diagnostics)
        {
            _builtins = builtins;
            _diagnostics = diagnostics;
        }

        public void Visit(AstNode? node)
        {
            if (node == null)
            {
                return;
            }

            switch (node)
            {
                case ModuleDeclaration module:
                    VisitModule(module);
                    return;
                case BlockStatement block:
                    VisitBlock(block);
                    return;
                case FunctionDeclaration function:
                    for (var i = 0; i < function.Parameters.Count; i++)
                    {
                        Visit(function.Parameters[i].Initializer);
                    }
                    Visit(function.Body);
                    return;
                case VariableDeclaration variable:
                    Visit(variable.Initializer);
                    return;
                case ExpressionStatement expressionStatement:
                    Visit(expressionStatement.Expression);
                    return;
                case TypedDocumentExpression typedDocument:
                    Visit(typedDocument.Value);
                    return;
                case CheckExpression check:
                    Visit(check.Value);
                    return;
                case AssignmentExpression assignment:
                    ValidateAssignmentTarget(assignment.Left);
                    Visit(assignment.Right);
                    return;
                case CompoundExpression compound:
                    ValidateAssignmentTarget(compound.Left);
                    Visit(compound.Right);
                    return;
                case UnaryExpression unary:
                    if (IsIncrementOrDecrement(unary.Operator))
                    {
                        ValidateAssignmentTarget(unary.Expression);
                    }
                    else
                    {
                        Visit(unary.Expression);
                    }
                    return;
                case FunctionCallExpression call:
                    Visit(call.Target);
                    for (var i = 0; i < call.Arguments.Count; i++)
                    {
                        Visit(call.Arguments[i]);
                    }
                    return;
                case NewExpression newExpression:
                    Visit(newExpression.Expression);
                    return;
                case GetPropertyExpression getProperty:
                    Visit(getProperty.Object);
                    Visit(getProperty.Property);
                    return;
                case SetPropertyExpression setProperty:
                    ValidateAssignmentTarget(setProperty);
                    Visit(setProperty.Value);
                    return;
                case SetElementExpression setElement:
                    Visit(setElement.Object);
                    Visit(setElement.Index);
                    Visit(setElement.Value);
                    return;
                case BinaryExpression binary:
                    Visit(binary.Left);
                    Visit(binary.Right);
                    return;
                case InExpression inExpression:
                    Visit(inExpression.Left);
                    Visit(inExpression.Right);
                    return;
                case GroupExpression group:
                    for (var i = 0; i < group.Expressions.Count; i++)
                    {
                        Visit(group.Expressions[i]);
                    }
                    return;
                case ArrayLiteralExpression array:
                    for (var i = 0; i < array.Elements.Count; i++)
                    {
                        Visit(array.Elements[i]);
                    }
                    return;
                case MapExpression map:
                    for (var i = 0; i < map.Entries.Count; i++)
                    {
                        Visit(map.Entries[i]);
                    }
                    return;
                case MapKeyValueExpression mapEntry:
                    Visit(mapEntry.Value);
                    return;
                case TemplateStringExpression template:
                    for (var i = 0; i < template.Parts.Count; i++)
                    {
                        Visit(template.Parts[i].Expression);
                    }
                    return;
                case ReturnStatement returnStatement:
                    Visit(returnStatement.Expression);
                    return;
                case ThrowStatement throwStatement:
                    Visit(throwStatement.Expression);
                    return;
                case DeleteStatement deleteStatement:
                    ValidateAssignmentTarget(deleteStatement.Expression, "Cannot delete readonly builtin member '{0}'.");
                    return;
                case IfStatement ifStatement:
                    Visit(ifStatement.Condition);
                    Visit(ifStatement.Body);
                    Visit(ifStatement.Else);
                    return;
                case WhileStatement whileStatement:
                    Visit(whileStatement.Condition);
                    Visit(whileStatement.Body);
                    return;
                case ForStatement forStatement:
                    Visit(forStatement.Initializer);
                    Visit(forStatement.Condition);
                    Visit(forStatement.Incrementor);
                    Visit(forStatement.Body);
                    return;
                case ForInStatement forInStatement:
                    Visit(forInStatement.Initializer);
                    Visit(forInStatement.Iterator);
                    Visit(forInStatement.Body);
                    return;
                case TryStatement tryStatement:
                    Visit(tryStatement.Body);
                    Visit(tryStatement.CatchBody);
                    Visit(tryStatement.FinallyBody);
                    return;
            }
        }

        private void VisitModule(ModuleDeclaration module)
        {
            IndexImports(module);
            PushScope();
            try
            {
                for (var i = 0; i < module.Functions.Count; i++)
                {
                    Declare(function: module.Functions[i]);
                }

                for (var i = 0; i < module.Statements.Count; i++)
                {
                    Declare(module.Statements[i]);
                }

                for (var i = 0; i < module.Statements.Count; i++)
                {
                    Visit(module.Statements[i]);
                }

                for (var i = 0; i < module.Functions.Count; i++)
                {
                    Visit(module.Functions[i]);
                }
            }
            finally
            {
                PopScope();
            }
        }

        private void IndexImports(ModuleDeclaration module)
        {
            _importAliases.Clear();
            _builtinModuleAliases.Clear();
            for (var i = 0; i < module.Imports.Count; i++)
            {
                var import = module.Imports[i];
                var alias = import.Name?.Value;
                if (import.Include || string.IsNullOrEmpty(alias))
                {
                    continue;
                }

                _importAliases.Add(alias);
                var modulePath = import.File?.Value;
                if (!string.IsNullOrEmpty(modulePath) &&
                    _builtins.TryGetModule(modulePath, out var builtinModule))
                {
                    _builtinModuleAliases[alias] = builtinModule;
                }
            }
        }

        private void VisitBlock(BlockStatement block)
        {
            PushScope();
            try
            {
                for (var i = 0; i < block.Functions.Count; i++)
                {
                    Declare(function: block.Functions[i]);
                }

                for (var i = 0; i < block.Statements.Count; i++)
                {
                    Declare(block.Statements[i]);
                }

                for (var i = 0; i < block.Functions.Count; i++)
                {
                    Visit(block.Functions[i]);
                }

                for (var i = 0; i < block.Statements.Count; i++)
                {
                    Visit(block.Statements[i]);
                }
            }
            finally
            {
                PopScope();
            }
        }

        private void ValidateAssignmentTarget(Expression? target)
        {
            ValidateAssignmentTarget(target, "Cannot assign to readonly builtin member '{0}'.");
        }

        private void ValidateAssignmentTarget(Expression? target, string messageFormat)
        {
            if (target is GroupExpression group)
            {
                ValidateAssignmentTarget(group.Expression, messageFormat);
                return;
            }

            if (TryResolveReadonlyBuiltinMember(target, out var member))
            {
                _diagnostics.Add(new LanguageDiagnostic(
                    "AURORA-BUILTIN-READONLY",
                    string.Format(messageFormat, member.FullName),
                    TextRange.FromSourceSpan(target!.Range),
                    LanguageDiagnosticSeverity.Error));
                return;
            }

            VisitAssignmentTargetChildren(target);
        }

        private void VisitAssignmentTargetChildren(Expression? target)
        {
            switch (target)
            {
                case SetPropertyExpression setProperty:
                    Visit(setProperty.Object);
                    Visit(setProperty.Property);
                    return;
                case GetPropertyExpression getProperty:
                    Visit(getProperty.Object);
                    Visit(getProperty.Property);
                    return;
                case SetElementExpression setElement:
                    Visit(setElement.Object);
                    Visit(setElement.Index);
                    return;
                default:
                    Visit(target);
                    return;
            }
        }

        private bool TryResolveReadonlyBuiltinMember(Expression? target, out BuiltinApiMember member)
        {
            member = null!;

            if (target is SetPropertyExpression setProperty)
            {
                return TryResolveReadonlyBuiltinMember(setProperty.Object, setProperty.Property, out member);
            }

            if (target is GetPropertyExpression getProperty)
            {
                return TryResolveReadonlyBuiltinMember(getProperty.Object, getProperty.Property, out member);
            }

            return false;
        }

        private bool TryResolveReadonlyBuiltinMember(Expression ownerExpression, Expression propertyExpression, out BuiltinApiMember member)
        {
            member = null!;
            if (ownerExpression is not NameExpression owner ||
                propertyExpression is not NameExpression property)
            {
                return false;
            }

            var ownerName = owner.Identifier.Value;
            if (IsDeclared(ownerName))
            {
                return false;
            }

            if (_builtinModuleAliases.TryGetValue(ownerName, out var builtinModule))
            {
                return builtinModule.TryGetMember(property.Identifier.Value, out member) &&
                    member.ReadOnly;
            }

            return !_importAliases.Contains(ownerName) &&
                _builtins.TryGetGlobalMember(ownerName, property.Identifier.Value, out member) &&
                member.ReadOnly;
        }

        private void PushScope()
        {
            _scopes.Push(new HashSet<string>(StringComparer.Ordinal));
        }

        private void PopScope()
        {
            _scopes.Pop();
        }

        private void Declare(Statement statement)
        {
            if (statement is VariableDeclaration variable && variable.Name != null)
            {
                Declare(variable.Name.Value);
            }
            else if (statement is FunctionDeclaration function)
            {
                Declare(function);
            }
        }

        private void Declare(FunctionDeclaration function)
        {
            if (function.Name != null)
            {
                Declare(function.Name.Value);
            }
        }

        private void Declare(string name)
        {
            if (!string.IsNullOrEmpty(name) && _scopes.Count != 0)
            {
                _scopes.Peek().Add(name);
            }
        }

        private bool IsDeclared(string name)
        {
            foreach (var scope in _scopes)
            {
                if (scope.Contains(name))
                {
                    return true;
                }
            }

            return false;
        }

        private static bool IsIncrementOrDecrement(Operator op)
        {
            return op == Operator.PreIncrement ||
                op == Operator.PostIncrement ||
                op == Operator.PreDecrement ||
                op == Operator.PostDecrement;
        }
    }
}
