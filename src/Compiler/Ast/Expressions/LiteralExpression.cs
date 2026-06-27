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
                ScriptDatum.WriteAsNumber(ref value, numberToken.NumberValue);
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