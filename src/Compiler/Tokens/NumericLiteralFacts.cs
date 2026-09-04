using System;

namespace AuroraScript.Tokens
{
    internal enum NumericLiteralSuffix : byte
    {
        None,
        Int32,
        UInt32,
        Int64,
        UInt64,
        Number
    }

    internal static class NumericLiteralFacts
    {
        public static bool TryConsumeSuffix(
            ReadOnlySpan<char> span,
            int index,
            bool hexadecimal,
            out NumericLiteralSuffix suffix,
            out int length)
        {
            suffix = NumericLiteralSuffix.None;
            length = 0;
            if ((uint)index >= (uint)span.Length)
            {
                return false;
            }

            var first = span[index];
            var found = first switch
            {
                'L' or 'l' => NumericLiteralSuffix.Int64,
                'I' or 'i' => NumericLiteralSuffix.Int32,
                'U' or 'u' => NumericLiteralSuffix.UInt32,
                'D' or 'd' when !hexadecimal => NumericLiteralSuffix.Number,
                _ => NumericLiteralSuffix.None
            };
            if (found == NumericLiteralSuffix.None)
            {
                return false;
            }

            var next = index + 1;
            if ((first == 'U' || first == 'u') &&
                next < span.Length &&
                (span[next] == 'L' || span[next] == 'l'))
            {
                found = NumericLiteralSuffix.UInt64;
                next++;
            }
            else if ((first == 'L' || first == 'l') &&
                next < span.Length &&
                (span[next] == 'U' || span[next] == 'u'))
            {
                found = NumericLiteralSuffix.UInt64;
                next++;
            }

            if (next < span.Length && IsIdentifierPart(span[next]))
            {
                return false;
            }

            suffix = found;
            length = next - index;
            return true;
        }

        public static bool TryConsumeSuffix(
            ReadOnlySpan<char> span,
            int index,
            bool hexadecimal,
            out NumericLiteralSuffix suffix)
        {
            return TryConsumeSuffix(
                span,
                index,
                hexadecimal,
                out suffix,
                out _);
        }

        public static ReadOnlySpan<char> WithoutSuffix(ReadOnlySpan<char> source)
        {
            var hexadecimal = IsHexadecimal(source);
            var start = Math.Max(0, source.Length - 2);
            for (; start < source.Length; start++)
            {
                if (TryConsumeSuffix(
                        source,
                        start,
                        hexadecimal,
                        out _,
                        out var length) &&
                    start + length == source.Length)
                {
                    return source[..start];
                }
            }
            return source;
        }

        public static bool IsHexadecimal(ReadOnlySpan<char> source)
        {
            return source.Length > 2 &&
                source[0] == '0' &&
                (source[1] == 'x' || source[1] == 'X');
        }

        public static bool IsExactInt32(double value)
        {
            return value >= int.MinValue && value <= int.MaxValue &&
                value == Math.Truncate(value) &&
                (value != 0d || BitConverter.DoubleToInt64Bits(value) >= 0);
        }

        public static bool IsExactUInt32(double value)
        {
            return value >= uint.MinValue && value <= uint.MaxValue &&
                value == Math.Truncate(value) &&
                (value != 0d || BitConverter.DoubleToInt64Bits(value) >= 0);
        }

        public static bool IsExactInt64(double value)
        {
            return value >= -9007199254740991d &&
                value <= 9007199254740991d &&
                value == Math.Truncate(value) &&
                (value != 0d || BitConverter.DoubleToInt64Bits(value) >= 0);
        }

        private static bool IsIdentifierPart(char c)
        {
            return (c >= 'a' && c <= 'z') ||
                (c >= 'A' && c <= 'Z') ||
                (c >= '0' && c <= '9') ||
                c == '_' ||
                c == '$' ||
                (c >= 0x4e00 && c <= 0x9fbb);
        }
    }
}
