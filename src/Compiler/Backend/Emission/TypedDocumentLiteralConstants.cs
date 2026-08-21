using AuroraScript.Compiler.Ast.Expressions;
using AuroraScript.Runtime.Serialization;
using AuroraScript.Tokens;
using System;
using System.Buffers;

namespace AuroraScript.Compiler.Backend.Emission
{
    /// <summary>
    /// Reads the original spelling of numeric TDoc literals when their target has
    /// a wider integer domain than <see cref="double"/>.  Script values remain
    /// doubles; this is only for compile-time literals whose lexical integer value
    /// is known before a ScriptDatum is created.
    /// </summary>
    internal static class TypedDocumentLiteralConstants
    {
        internal static bool TryGetNumber(Expression expression, out double value)
        {
            if (!TryGetNumberToken(expression, out var token, out var negative))
            {
                value = 0d;
                return false;
            }

            value = negative ? -token.NumberValue : token.NumberValue;
            return true;
        }

        internal static bool TryGetBoolean(Expression expression, out bool value)
        {
            while (expression is TypedDocumentExpression { IsInterpolation: false } typed)
            {
                expression = typed.Value;
            }

            if (expression is LiteralExpression { Token: BooleanToken boolean })
            {
                value = boolean.BoolValue;
                return true;
            }

            if (TryGetNumber(expression, out var number) &&
                double.IsFinite(number) && (number == 0d || number == 1d))
            {
                value = number == 1d;
                return true;
            }

            value = false;
            return false;
        }

        internal static bool TryGetInt64(Expression expression, out long value)
        {
            if (!TryGetNumberToken(expression, out var token, out var negative))
            {
                value = 0;
                return false;
            }

            var number = negative ? -token.NumberValue : token.NumberValue;
            if (double.IsFinite(number) && Math.Truncate(number) == number &&
                number >= -9007199254740991d && number <= 9007199254740991d)
            {
                value = (long)number;
                return true;
            }

            return TryParseInt64(token.Value.AsSpan(), negative, out value);
        }

        internal static bool TryGetUInt64(Expression expression, out ulong value)
        {
            if (!TryGetNumberToken(expression, out var token, out var negative) || negative)
            {
                value = 0;
                return false;
            }

            var number = token.NumberValue;
            if (double.IsFinite(number) && Math.Truncate(number) == number &&
                number >= 0d && number <= 9007199254740991d)
            {
                value = (ulong)number;
                return true;
            }

            return TryParseUInt64(token.Value.AsSpan(), out value);
        }

        internal static bool TryGetDateTicks(Expression expression, out long ticks)
        {
            return TryGetInt64(expression, out ticks) &&
                ticks >= DateTimeOffset.MinValue.Ticks &&
                ticks <= DateTimeOffset.MaxValue.Ticks;
        }

        private static bool TryGetNumberToken(
            Expression expression,
            out NumberToken token,
            out bool negative)
        {
            while (expression is TypedDocumentExpression { IsInterpolation: false } typed)
            {
                expression = typed.Value;
            }

            if (expression is LiteralExpression { Token: NumberToken number })
            {
                token = number;
                negative = false;
                return true;
            }
            if (expression is UnaryExpression unary && unary.Operator == Operator.Negate &&
                unary.Expression is LiteralExpression { Token: NumberToken numberToken })
            {
                token = numberToken;
                negative = true;
                return true;
            }

            token = null;
            negative = false;
            return false;
        }

        private static bool TryParseInt64(
            ReadOnlySpan<char> source,
            bool negative,
            out long value)
        {
            if (!negative && source.IndexOf('_') < 0)
            {
                return TypedDocumentScanner.TryParseInt64Exact(source, out value);
            }

            var capacity = source.Length + (negative ? 1 : 0);
            char[] rented = null;
            Span<char> clean = capacity <= 128
                ? stackalloc char[capacity]
                : (rented = ArrayPool<char>.Shared.Rent(capacity));
            try
            {
                var length = 0;
                if (negative) clean[length++] = '-';
                for (var i = 0; i < source.Length; i++)
                {
                    if (source[i] != '_') clean[length++] = source[i];
                }
                return TypedDocumentScanner.TryParseInt64Exact(clean[..length], out value);
            }
            finally
            {
                if (rented != null) ArrayPool<char>.Shared.Return(rented);
            }
        }

        private static bool TryParseUInt64(ReadOnlySpan<char> source, out ulong value)
        {
            if (source.IndexOf('_') < 0)
            {
                return TypedDocumentScanner.TryParseUInt64Exact(source, out value);
            }

            char[] rented = null;
            Span<char> clean = source.Length <= 128
                ? stackalloc char[source.Length]
                : (rented = ArrayPool<char>.Shared.Rent(source.Length));
            try
            {
                var length = 0;
                for (var i = 0; i < source.Length; i++)
                {
                    if (source[i] != '_') clean[length++] = source[i];
                }
                return TypedDocumentScanner.TryParseUInt64Exact(clean[..length], out value);
            }
            finally
            {
                if (rented != null) ArrayPool<char>.Shared.Return(rented);
            }
        }
    }
}
