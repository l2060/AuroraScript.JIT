using AuroraScript.Runtime;
using System;
using System.Collections.Generic;
using System.Text;

namespace AuroraScript.Compiler.Ast.Expressions
{
    internal sealed class TemplateStringExpression : Expression
    {
        private readonly TemplateStringPart[] _parts;

        public TemplateStringExpression(IEnumerable<TemplateStringPart> parts)
        {
            ArgumentNullException.ThrowIfNull(parts);

            _parts = parts is TemplateStringPart[] array
                ? array
                : new List<TemplateStringPart>(parts).ToArray();

            for (var i = 0; i < _parts.Length; i++)
            {
                if (_parts[i].Expression != null)
                {
                    _parts[i].Expression.Parent = this;
                }
            }
        }

        public IReadOnlyList<TemplateStringPart> Parts => _parts;
        public int PartCount => _parts.Length;

        public override void Accept(IAstVisitor visitor)
        {
            visitor.AcceptTemplateStringExpression(this);
        }
        public override bool TryEvalConst(EvaluationContext ctx, ref ScriptDatum value)
        {
            var builder = new StringBuilder();
            for (var i = 0; i < _parts.Length; i++)
            {
                var part = _parts[i];
                if (part.IsLiteral)
                {
                    builder.Append(part.Literal);
                    continue;
                }

                var inner = default(ScriptDatum);
                if (!part.Expression.TryEvalConst(ctx, ref inner))
                {
                    return false;
                }

                builder.Append(ScriptDatum.ToString(inner));
            }

            ScriptDatum.WriteAsString(ref value, builder.ToString());
            return true;
        }
    }

    internal readonly struct TemplateStringPart
    {
        public TemplateStringPart(string literal)
        {
            Literal = literal ?? string.Empty;
            Expression = null;
        }

        public TemplateStringPart(Expression expression)
        {
            ArgumentNullException.ThrowIfNull(expression);

            Literal = null;
            Expression = expression;
        }

        public string Literal { get; }
        public Expression Expression { get; }
        public bool IsLiteral => Expression == null;
    }
}
