using AuroraScript.Compiler.Ast.Expressions;
using AuroraScript.Tokens;

namespace AuroraScript.Compiler.Backend.Code
{
    internal static class LiteralTypeFacts
    {
        private static FlowValueType GetNumberLiteralType(NumberToken number)
        {
            return number.Suffix switch
            {
                NumericLiteralSuffix.Number => FlowValueType.Number,
                NumericLiteralSuffix.Int32 => FlowValueType.Int32,
                NumericLiteralSuffix.UInt32 => FlowValueType.UInt32,
                NumericLiteralSuffix.Int64 => FlowValueType.Int64,
                NumericLiteralSuffix.UInt64 => FlowValueType.UInt64,
                _ when number.HasFractionOrExponent => FlowValueType.Number,
                _ => NumericLiteralFacts.IsExactInt32(number.NumberValue)
                    ? FlowValueType.Int32
                    : FlowValueType.Number
            };
        }

        public static FlowValueType GetType(LiteralExpression literal)
        {
            return literal.Token switch
            {
                NullToken => FlowValueType.Null,
                BooleanToken => FlowValueType.Boolean,
                NumberToken number => GetNumberLiteralType(number),
                StringToken => FlowValueType.String,
                RegexToken => FlowValueType.Object,
                _ => FlowValueType.Dynamic
            };
        }
    }
}
