using AuroraScript.Compiler.Ast;
using AuroraScript.Compiler.Ast.Expressions;
using AuroraScript.Compiler.Ast.Statements;
using AuroraScript.Compiler.Backend.Code;
using AuroraScript.Compiler.Backend.Plans;
using System;
using System.Collections.Generic;

namespace AuroraScript.Compiler.Backend.Emission
{
    /// <summary>Collects backend diagnostics directly from the bound AST.</summary>
    internal sealed class FunctionReportCollector
    {
        private readonly ModulePlan _module;
        private readonly Dictionary<FunctionDeclaration, FunctionPlan> _functions;
        private FunctionEmissionContext _context;
        private TypedFunctionCode _code;

        public FunctionReportCollector(ModulePlan module)
        {
            _module = module ?? throw new ArgumentNullException(nameof(module));
            _functions = new Dictionary<FunctionDeclaration, FunctionPlan>(ReferenceEqualityComparer.Instance);
            for (var i = 0; i < module.Functions.Count; i++)
            {
                var declaration = module.Functions[i].Declaration;
                if (declaration != null) _functions[declaration] = module.Functions[i];
            }
        }

        public void Collect(FunctionEmissionContext context)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _code = TypedFunctionBuilder.Build(_module, context.Function);
            try
            {
                VisitStatement(context.Function.Declaration?.Body as Statement);
            }
            finally
            {
                _context = null;
                _code = null;
            }
        }

        private void VisitStatement(Statement statement)
        {
            if (statement == null) return;
            _context.RecordStatement(statement);
            switch (statement)
            {
                case BlockStatement block:
                    for (var i = 0; i < block.Functions.Count; i++) VisitStatement(block.Functions[i]);
                    for (var i = 0; i < block.Statements.Count; i++) VisitStatement(block.Statements[i]);
                    return;
                case FunctionDeclaration function:
                    if (function.Flags != FunctionFlags.Declare)
                    {
                        RecordNestedFunction(function);
                        RecordDeclarationLocals(function);
                    }
                    return;
                case VariableDeclaration variable:
                    if (!variable.IsDeclare) RecordDeclarationLocals(variable);
                    VisitExpression(variable.Initializer);
                    return;
                case ExpressionStatement expression:
                    VisitExpression(expression.Expression);
                    return;
                case ReturnStatement @return:
                    VisitExpression(@return.Expression);
                    return;
                case IfStatement @if:
                    VisitExpression(@if.Condition);
                    VisitStatement(@if.Body);
                    VisitStatement(@if.Else);
                    return;
                case WhileStatement @while:
                    VisitExpression(@while.Condition);
                    VisitStatement(@while.Body);
                    return;
                case ForStatement @for:
                    if (@for.Initializer is Statement initializerStatement) VisitStatement(initializerStatement);
                    else if (@for.Initializer is Expression initializerExpression) VisitExpression(initializerExpression);
                    VisitExpression(@for.Condition);
                    VisitExpression(@for.Incrementor);
                    VisitStatement(@for.Body);
                    return;
                case ForInStatement forIn:
                    VisitStatement(forIn.Initializer);
                    VisitExpression(forIn.Iterator);
                    VisitStatement(forIn.Body);
                    return;
                case TryStatement @try:
                    RecordCatchSlot(@try);
                    VisitStatement(@try.Body);
                    VisitStatement(@try.CatchBody);
                    VisitStatement(@try.FinallyBody);
                    return;
                case ThrowStatement @throw:
                    VisitExpression(@throw.Expression);
                    return;
                case DeleteStatement delete:
                    VisitExpression(delete.Expression);
                    return;
                case BreakStatement:
                case ContinueStatement:
                case DebuggerStatement:
                    return;
                default:
                    throw new UnsupportedEmissionException(_context.Function, statement);
            }
        }

        private void VisitExpression(Expression expression)
        {
            if (expression == null) return;
            _context.RecordExpression(expression);
            switch (expression)
            {
                case LiteralExpression:
                    return;
                case NameExpression name:
                    RecordName(name);
                    return;
                case BinaryExpression binary:
                    VisitExpression(binary.Left);
                    VisitExpression(binary.Right);
                    return;
                case AssignmentExpression assignment:
                    VisitExpression(assignment.Left);
                    VisitExpression(assignment.Right);
                    return;
                case CompoundExpression compound:
                    VisitExpression(compound.Left);
                    VisitExpression(compound.Right);
                    return;
                case UnaryExpression unary:
                    VisitExpression(unary.Expression);
                    return;
                case FunctionCallExpression call:
                    VisitExpression(call.Target);
                    for (var i = 0; i < call.Arguments.Count; i++) VisitExpression(call.Arguments[i]);
                    return;
                case TemplateStringExpression template:
                    for (var i = 0; i < template.Parts.Count; i++)
                    {
                        if (!template.Parts[i].IsLiteral) VisitExpression(template.Parts[i].Expression);
                    }
                    return;
                case IncludedExpression included:
                    VisitExpression(included.Left);
                    VisitExpression(included.Right);
                    return;
                case InExpression @in:
                    VisitExpression(@in.Left);
                    VisitExpression(@in.Right);
                    return;
                case GetPropertyExpression property:
                    VisitExpression(property.Object);
                    VisitExpression(property.Property);
                    return;
                case SetPropertyExpression property:
                    VisitExpression(property.Object);
                    VisitExpression(property.Property);
                    VisitExpression(property.Value);
                    return;
                case GetElementExpression element:
                    VisitExpression(element.Object);
                    VisitExpression(element.Index);
                    return;
                case SetElementExpression element:
                    VisitExpression(element.Object);
                    VisitExpression(element.Index);
                    VisitExpression(element.Value);
                    return;
                case ArrayLiteralExpression array:
                    for (var i = 0; i < array.Elements.Count; i++) VisitExpression(array.Elements[i]);
                    return;
                case MapExpression map:
                    for (var i = 0; i < map.Entries.Count; i++) VisitExpression(map.Entries[i]);
                    return;
                case MapKeyValueExpression entry:
                    VisitExpression(entry.Value);
                    return;
                case SpreadExpression spread:
                    VisitExpression(spread.Expression);
                    return;
                case NewExpression @new:
                    VisitExpression(@new.Expression);
                    return;
                case LambdaExpression lambda:
                    RecordNestedFunction(lambda.Function);
                    return;
                case GroupExpression group:
                    for (var i = 0; i < group.Expressions.Count; i++) VisitExpression(group.Expressions[i]);
                    return;
                default:
                    throw new UnsupportedEmissionException(_context.Function, expression);
            }
        }

        private void RecordName(NameExpression name)
        {
            var binding = _code.GetName(name);
            _context.RecordLocal(binding.Local);
            _context.RecordUpvalue(binding.Upvalue);
            _context.RecordModuleSymbol(binding.ModuleSymbol);
        }

        private void RecordDeclarationLocals(AstNode declaration)
        {
            for (var i = 0; i < _context.Function.LocalSlots.Length; i++)
            {
                var slot = _context.Function.LocalSlots[i];
                if (ReferenceEquals(slot.Declaration, declaration)) _context.RecordLocal(slot.Id);
            }
        }

        private void RecordCatchSlot(TryStatement statement)
        {
            if (string.IsNullOrEmpty(statement.CatchVariable)) return;
            for (var i = 0; i < _context.Function.LocalSlots.Length; i++)
            {
                var slot = _context.Function.LocalSlots[i];
                if (ReferenceEquals(slot.Declaration, statement) &&
                    StringComparer.Ordinal.Equals(slot.Name, statement.CatchVariable))
                {
                    _context.RecordCatchSlot(slot.Id);
                    return;
                }
            }
        }

        private void RecordNestedFunction(FunctionDeclaration declaration)
        {
            if (declaration != null && _functions.TryGetValue(declaration, out var function))
            {
                _context.RecordNestedFunction(function.Id);
            }
        }
    }
}
