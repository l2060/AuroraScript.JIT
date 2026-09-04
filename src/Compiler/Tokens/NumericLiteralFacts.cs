using System;

namespace AuroraScript.Tokens
{
    internal enum NumericLiteralSuffix : byte
    {
        None,
        Int32,
        UInt32,
        Int64,
        Number
    }

    internal static class NumericLiteralFacts
    {
        public static bool TryConsumeSuffix(
            ReadOnlySpan<char> span,
            int index,
            bool hexadecimal,
            out NumericLiteralSuffix suffix)
        {
            suffix = NumericLiteralSuffix.None;
            if ((uint)index >= (uint)span.Length)
            {
                return false;
            }

            var found = span[index] switch
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
            if (next < span.Length && IsIdentifierPart(span[next]))
            {
                return false;
            }

            suffix = found;
            return true;
        }

        public static ReadOnlySpan<char> WithoutSuffix(ReadOnlySpan<char> source)
        {
            var hexadecimal = IsHexadecimal(source);
            return source.Length > 0 &&
                TryConsumeSuffix(source, source.Length - 1, hexadecimal, out _)
                    ? source[..^1]
                    : source;
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
