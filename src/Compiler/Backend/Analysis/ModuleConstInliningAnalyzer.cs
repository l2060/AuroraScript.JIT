using AuroraScript.Compiler.Ast;
using AuroraScript.Compiler.Ast.Expressions;
using AuroraScript.Compiler.Backend.Binding;
using AuroraScript.Compiler.Backend.Plans;
using AuroraScript.Runtime;
using AuroraScript.Tokens;
using System;

namespace AuroraScript.Compiler.Backend.Analysis
{
    internal static class ModuleConstInliningAnalyzer
    {
        public static void Apply(CompileSession session, ModulePlan modulePlan)
        {
            ArgumentNullException.ThrowIfNull(session);
            ArgumentNullException.ThrowIfNull(modulePlan);

            if (!session.Options.Optimization.EnableModuleConstInlining)
            {
                return;
            }

            var evaluator = new Evaluator(session, modulePlan);
            for (var i = 0; i < modulePlan.Declaration.Statements.Count; i++)
            {
                if (modulePlan.Declaration.Statements[i] is not VariableDeclaration variable ||
                    variable.IsDeclare ||
                    !variable.IsConst ||
                    variable.Name == null ||
                    !modulePlan.TryGetSymbol(variable.Name.Value, out var symbolId) ||
                    !IsOwnModuleConst(session, modulePlan, symbolId, variable))
                {
                    continue;
                }

                var value = default(ScriptDatum);
                if (variable.Initializer == null)
                {
                    modulePlan.SetInlineConstant(symbolId, ScriptDatum.Null);
                    continue;
                }

                if (evaluator.TryEvaluate(variable.Initializer, ref value))
                {
                    modulePlan.SetInlineConstant(symbolId, value);
                }
            }
        }

        public static LiteralExpression CreateLiteralExpression(ScriptDatum value, SourceSpan range)
        {
            var expression = new LiteralExpression(CreateToken(value))
            {
                Range = range
            };
            return expression;
        }

        private static ValueToken CreateToken(ScriptDatum value)
        {
            switch (value.Kind)
            {
                case ValueKind.Null:
                    return new NullToken();
                case ValueKind.Boolean:
                    return new BooleanToken(value.Boolean);
                case ValueKind.Number:
                    return new NumberToken(value.Number);
                case ValueKind.String:
                    return new StringToken { Value = value.StringText ?? string.Empty };
                default:
                    throw new ArgumentException("Only primitive const values can be inlined.", nameof(value));
            }
        }

        private static bool IsOwnModuleConst(
            CompileSession session,
            ModulePlan modulePlan,
            SymbolId symbolId,
            VariableDeclaration declaration)
        {
            var symbol = session.Symbols[symbolId];
            return symbol.Module.Equals(modulePlan.Id) &&
                symbol.Kind == BackendSymbolKind.ModuleProperty &&
                symbol.HasFlag(BackendSymbolFlags.Const) &&
                !symbol.HasFlag(BackendSymbolFlags.DeclaredOnly) &&
                !symbol.HasFlag(BackendSymbolFlags.Imported) &&
                ReferenceEquals(symbol.Declaration, declaration);
        }

        private readonly struct Evaluator
        {
            private readonly CompileSession _session;
            private readonly ModulePlan _modulePlan;

            public Evaluator(CompileSession session, ModulePlan modulePlan)
            {
                _session = session;
                _modulePlan = modulePlan;
            }

            public bool TryEvaluate(Expression expression, ref ScriptDatum value)
            {
                switch (expression)
                {
                    case null:
                        value = ScriptDatum.Null;
                        return true;
                    case TypedDocumentExpression tdoc:
                        return TryEvaluate(tdoc.Value, ref value);
                    case GroupExpression group:
                        return TryEvaluate(group.Expression, ref value);
                    case LiteralExpression literal:
                        return TryEvaluateLiteral(literal, ref value);
                    case TemplateStringExpression template:
                        return TryEvaluateTemplateString(template, ref value);
                    case NameExpression name:
                        return TryEvaluateName(name, ref value);
                    case UnaryExpression unary:
                        return TryEvaluateUnary(unary, ref value);
                    case BinaryExpression binary:
                        return TryEvaluateBinary(binary, ref value);
                    default:
                        return false;
                }
            }

            private static bool TryEvaluateLiteral(LiteralExpression literal, ref ScriptDatum value)
            {
                switch (literal.Token)
                {
                    case NumberToken number:
                        value = ScriptDatum.FromNumber(number.NumberValue);
                        return true;
                    case StringToken text:
                        value = ScriptDatum.FromString(text.Value);
                        return true;
                    case BooleanToken boolean:
                        value = ScriptDatum.FromBoolean(boolean.BoolValue);
                        return true;
                    case NullToken:
                        value = ScriptDatum.Null;
                        return true;
                    default:
                        return false;
                }
            }

            private bool TryEvaluateName(NameExpression name, ref ScriptDatum value)
            {
                var identifier = name.Identifier?.Value;
                if (string.IsNullOrEmpty(identifier) ||
                    !_modulePlan.TryGetSymbol(identifier, out var symbolId) ||
                    !_modulePlan.TryGetInlineConstant(symbolId, out value))
                {
                    return false;
                }

                var symbol = _session.Symbols[symbolId];
                return symbol.Module.Equals(_modulePlan.Id) &&
                    symbol.Kind == BackendSymbolKind.ModuleProperty &&
                    symbol.HasFlag(BackendSymbolFlags.Const) &&
                    !symbol.HasFlag(BackendSymbolFlags.Imported);
            }

            private bool TryEvaluateUnary(UnaryExpression unary, ref ScriptDatum value)
            {
                var inner = default(ScriptDatum);
                if (!TryEvaluate(unary.Expression, ref inner))
                {
                    return false;
                }

                if (unary.Operator == Operator.Negate)
                {
                    value = ValueOps.Negate(inner);
                    return true;
                }

                if (unary.Operator == Operator.LogicalNot)
                {
                    value = ValueOps.Not(inner);
                    return true;
                }

                return false;
            }

            private bool TryEvaluateBinary(BinaryExpression binary, ref ScriptDatum value)
            {
                if (binary.Operator == Operator.LogicalAnd)
                {
                    return TryEvaluateLogical(binary.Left, binary.Right, branchWhenTrue: false, ref value);
                }

                if (binary.Operator == Operator.LogicalOr)
                {
                    return TryEvaluateLogical(binary.Left, binary.Right, branchWhenTrue: true, ref value);
                }

                var left = default(ScriptDatum);
                var right = default(ScriptDatum);
                if (!TryEvaluate(binary.Left, ref left) ||
                    !TryEvaluate(binary.Right, ref right))
                {
                    return false;
                }

                if (binary.Operator == Operator.Add)
                {
                    value = ValueOps.Add(left, right);
                    return true;
                }

                if (binary.Operator == Operator.Subtract)
                {
                    value = ValueOps.Subtract(left, right);
                    return true;
                }
                if (binary.Operator == Operator.Multiply)
                {
                    value = ValueOps.Multiply(left, right);
                    return true;
                }
                if (binary.Operator == Operator.Divide)
                {
                    value = ValueOps.Divide(left, right);
                    return true;
                }
                if (binary.Operator == Operator.Modulo)
                {
                    value = ValueOps.Modulo(left, right);
                    return true;
                }

                return false;
            }

            private bool TryEvaluateTemplateString(TemplateStringExpression expression, ref ScriptDatum value)
            {
                var builder = new System.Text.StringBuilder();
                for (var i = 0; i < expression.PartCount; i++)
                {
                    var part = expression.Parts[i];
                    if (part.IsLiteral)
                    {
                        builder.Append(part.Literal);
                        continue;
                    }

                    var inner = default(ScriptDatum);
                    if (!TryEvaluate(part.Expression, ref inner))
                    {
                        return false;
                    }

                    builder.Append(ScriptDatum.ToString(inner));
                }

                value = ScriptDatum.FromString(builder.ToString());
                return true;
            }

            private bool TryEvaluateLogical(
                Expression leftExpression,
                Expression rightExpression,
                bool branchWhenTrue,
                ref ScriptDatum value)
            {
                var left = default(ScriptDatum);
                if (!TryEvaluate(leftExpression, ref left))
                {
                    return false;
                }

                if (ScriptDatum.IsTrue(left) == branchWhenTrue)
                {
                    value = left;
                    return true;
                }

                return TryEvaluate(rightExpression, ref value);
            }
        }
    }
}


