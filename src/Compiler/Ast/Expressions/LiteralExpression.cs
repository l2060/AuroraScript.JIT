using AuroraScript.Runtime;
using AuroraScript.Tokens;

namespace AuroraScript.Compiler.Ast.Expressions
{
    internal class LiteralExpression : Expression
    {
        internal LiteralExpression(ValueToken token)
        {
            this.Token = token;
        }

        /// <summary>
        /// 字面量的内容
        /// </summary>
        public readonly ValueToken Token;


        public override void Accept(IAstVisitor visitor)
        {
            visitor.AcceptLiteralExpression(this);
        }
        public override bool TryEvalConst(EvaluationContext ctx, ref ScriptDatum value)
        {
            if (Token is NumberToken numberToken)
            {
                if (numberToken.Suffix == NumericLiteralSuffix.Int64 &&
                    numberToken.TryGetInt64(out var int64))
                {
                    ScriptDatum.WriteAsInt64(ref value, int64);
                }
                else if (numberToken.Suffix == NumericLiteralSuffix.UInt64 &&
                    numberToken.TryGetUInt64(out var uint64))
                {
                    ScriptDatum.WriteAsUInt64(ref value, uint64);
                }
                else
                {
                    ScriptDatum.WriteAsNumber(ref value, numberToken.NumberValue);
                }
                return true;
            }
            else if (Token is NullToken)
            {
                value = default;
                return true;
            }
            else if (Token is BooleanToken booleanToken)
            {
                ScriptDatum.WriteAsBoolean(ref value, booleanToken.BoolValue);
                return true;
            }
            else if (Token is StringToken stringToken)
            {
                ScriptDatum.WriteAsString(ref value, stringToken.Value);
                return true;
            }
            return false;
        }

    }
}
