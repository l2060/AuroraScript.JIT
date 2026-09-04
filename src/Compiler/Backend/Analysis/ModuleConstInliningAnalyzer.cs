using AuroraScript.Compiler.Ast;
using AuroraScript.Compiler.Ast.Expressions;
using AuroraScript.Compiler.Backend.Binding;
using AuroraScript.Compiler.Backend.Plans;
using AuroraScript.Runtime;
using AuroraScript.Tokens;
using System;
using System.Collections.Generic;

namespace AuroraScript.Compiler.Backend.Analysis
{
    internal static class ModuleConstInliningAnalyzer
    {
        public static bool TryEvaluateConstant(
            CompileSession session,
            ModulePlan modulePlan,
            Expression expression,
            out ScriptDatum value)
        {
            ArgumentNullException.ThrowIfNull(session);
            ArgumentNullException.ThrowIfNull(modulePlan);

            value = default;
            return new Evaluator(
                session,
                modulePlan,
                resolveConstantDeclarations: true).TryEvaluate(
                expression,
                ref value);
        }

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
                    modulePlan.SetInlineConstant(
                        symbolId,
                        value,
                        GetNumericHint(session, modulePlan, variable.Initializer));
                }
            }
        }

        public static LiteralExpression CreateLiteralExpression(ScriptDatum value, SourceSpan range)
        {
            return CreateLiteralExpression(new InlineConstant(value), range);
        }

        public static LiteralExpression CreateLiteralExpression(
            InlineConstant constant,
            SourceSpan range)
        {
            var expression = new LiteralExpression(CreateToken(constant))
            {
                Range = range
            };
            return expression;
        }

        private static ValueToken CreateToken(InlineConstant constant)
        {
            var value = constant.Value;
            switch (value.Kind)
            {
                case ValueKind.Null:
                    return new NullToken();
                case ValueKind.Boolean:
                    return new BooleanToken(value.Boolean);
                case ValueKind.Number:
                    return new NumberToken(value.Number, constant.NumericHint);
                case ValueKind.String:
                    return new StringToken { Value = value.StringText ?? string.Empty };
                default:
                    throw new ArgumentException("Only primitive const values can be inlined.", nameof(value));
            }
        }

        private static NumericLiteralSuffix GetNumericHint(
            CompileSession session,
            ModulePlan modulePlan,
            Expression expression)
        {
            switch (expression)
            {
                case TypedDocumentExpression tdoc:
                    return GetNumericHint(session, modulePlan, tdoc.Value);
                case GroupExpression group:
                    return GetNumericHint(session, modulePlan, group.Expression);
                case LiteralExpression { Token: NumberToken number }:
                    return number.Suffix != NumericLiteralSuffix.None
                        ? number.Suffix
                        : number.HasFractionOrExponent
                            ? NumericLiteralSuffix.Number
                            : NumericLiteralSuffix.None;
                case NameExpression name
                    when !string.IsNullOrEmpty(name.Identifier?.Value) &&
                        modulePlan.TryGetSymbol(name.Identifier.Value, out var symbolId) &&
                        modulePlan.TryGetInlineConstantInfo(symbolId, out var constant):
                    return constant.NumericHint;
                case GetPropertyExpression property
                    when TryResolvePropertyConstantInfo(
                        session,
                        modulePlan,
                        property,
                        out var propertyConstant):
                    return propertyConstant.NumericHint;
                default:
                    return NumericLiteralSuffix.None;
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
            private readonly HashSet<SymbolId> _evaluating;
            private readonly bool _resolveConstantDeclarations;

            public Evaluator(
                CompileSession session,
                ModulePlan modulePlan,
                bool resolveConstantDeclarations = false)
            {
                _session = session;
                _modulePlan = modulePlan;
                _evaluating = new HashSet<SymbolId>();
                _resolveConstantDeclarations = resolveConstantDeclarations;
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
                    case CheckExpression:
                        // Runtime assertions must not disappear during constant
                        // inlining, including mismatches that are required to throw.
                        return false;
                    case GroupExpression group:
                        return TryEvaluate(group.Expression, ref value);
                    case LiteralExpression literal:
                        return TryEvaluateLiteral(literal, ref value);
                    case TemplateStringExpression template:
                        return TryEvaluateTemplateString(template, ref value);
                    case NameExpression name:
                        return TryEvaluateName(name, ref value);
                    case GetPropertyExpression property:
                        return TryResolvePropertyConstant(
                            _session,
                            _modulePlan,
                            property,
                            out value);
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
                    !_modulePlan.TryGetSymbol(identifier, out var symbolId))
                {
                    return false;
                }

                var symbol = _session.Symbols[symbolId];
                if (!symbol.Module.Equals(_modulePlan.Id) ||
                    symbol.Kind != BackendSymbolKind.ModuleProperty ||
                    !symbol.HasFlag(BackendSymbolFlags.Const) ||
                    symbol.HasFlag(BackendSymbolFlags.Imported))
                {
                    return false;
                }
                if (_modulePlan.TryGetInlineConstant(symbolId, out value))
                {
                    return true;
                }
                if (!_resolveConstantDeclarations ||
                    symbol.Declaration is not VariableDeclaration
                    {
                        IsConst: true,
                        Initializer: not null
                    } declaration ||
                    !_evaluating.Add(symbolId))
                {
                    return false;
                }

                try
                {
                    return TryEvaluate(declaration.Initializer, ref value);
                }
                finally
                {
                    _evaluating.Remove(symbolId);
                }
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

        public static bool TryResolvePropertyConstant(
            CompileSession session,
            ModulePlan modulePlan,
            GetPropertyExpression property,
            out ScriptDatum value)
        {
            if (TryResolvePropertyConstantInfo(
                session,
                modulePlan,
                property,
                out var constant))
            {
                value = constant.Value;
                return true;
            }

            value = default;
            return false;
        }

        public static bool TryResolvePropertyConstantInfo(
            CompileSession session,
            ModulePlan modulePlan,
            GetPropertyExpression property,
            out InlineConstant constant)
        {
            constant = default;
            if (property?.Property is not NameExpression member ||
                string.IsNullOrEmpty(member.Identifier?.Value))
            {
                return false;
            }

            if (property.Object is NameExpression owner)
            {
                var ownerName = owner.Identifier?.Value;
                if (TryResolveEnumMember(
                        session,
                        modulePlan,
                        ownerName,
                        member.Identifier.Value,
                        requireExport: false,
                        out var enumValue))
                {
                    constant = new InlineConstant(enumValue);
                    return true;
                }

                if (TryGetImportedModule(
                        session,
                        modulePlan,
                        ownerName,
                        out var imported) &&
                    imported.TryGetSymbol(member.Identifier.Value, out var symbolId))
                {
                    var symbol = session.Symbols[symbolId];
                    return symbol.Module.Equals(imported.Id) &&
                        symbol.Kind == BackendSymbolKind.ModuleProperty &&
                        symbol.HasFlag(BackendSymbolFlags.Const) &&
                        symbol.HasFlag(BackendSymbolFlags.Exported) &&
                        imported.TryGetInlineConstantInfo(symbolId, out constant);
                }
                return false;
            }

            if (property.Object is GetPropertyExpression
                {
                    Object: NameExpression alias,
                    Property: NameExpression enumeration
                } &&
                TryGetImportedModule(
                    session,
                    modulePlan,
                    alias.Identifier?.Value,
                    out var enumModule))
            {
                if (TryResolveEnumMember(
                    session,
                    enumModule,
                    enumeration.Identifier?.Value,
                    member.Identifier.Value,
                    requireExport: true,
                    out var enumValue))
                {
                    constant = new InlineConstant(enumValue);
                    return true;
                }
            }

            return false;
        }

        private static bool TryResolveEnumMember(
            CompileSession session,
            ModulePlan modulePlan,
            string enumName,
            string memberName,
            bool requireExport,
            out ScriptDatum value)
        {
            value = default;
            if (string.IsNullOrEmpty(enumName) ||
                !modulePlan.TryGetSymbol(enumName, out var symbolId))
            {
                return false;
            }

            var symbol = session.Symbols[symbolId];
            if (!symbol.Module.Equals(modulePlan.Id) ||
                symbol.Kind != BackendSymbolKind.Enum ||
                requireExport && !symbol.HasFlag(BackendSymbolFlags.Exported) ||
                symbol.Declaration is not EnumDeclaration declaration)
            {
                return false;
            }

            for (var i = 0; i < declaration.Elements.Count; i++)
            {
                var element = declaration.Elements[i];
                if (StringComparer.Ordinal.Equals(element.Name?.Value, memberName))
                {
                    value = ScriptDatum.FromNumber(element.Value);
                    return true;
                }
            }
            return false;
        }

        private static bool TryGetImportedModule(
            CompileSession session,
            ModulePlan modulePlan,
            string alias,
            out ModulePlan imported)
        {
            imported = null;
            if (string.IsNullOrEmpty(alias))
            {
                return false;
            }

            ImportDeclaration import = null;
            for (var i = 0; i < modulePlan.Declaration.Imports.Count; i++)
            {
                var candidate = modulePlan.Declaration.Imports[i];
                if (!candidate.Include &&
                    StringComparer.Ordinal.Equals(candidate.Name?.Value, alias))
                {
                    import = candidate;
                    break;
                }
            }
            if (import?.Module == null)
            {
                return false;
            }

            for (var i = 0; i < session.Modules.Length; i++)
            {
                if (ReferenceEquals(session.Modules[i].Declaration, import.Module))
                {
                    imported = session.Modules[i];
                    return true;
                }
            }
            return false;
        }
    }
}


