using System;
using System.Globalization;

namespace AuroraScript.Tokens
{
    internal class NumberToken : ValueToken
    {
        public readonly static NumberToken Zero = new NumberToken("0");
        public readonly static NumberToken One = new NumberToken("1");

        internal NumberToken(String value)
            : this(value.AsSpan())
        {
            Value = value;
        }

        internal NumberToken(ReadOnlySpan<char> value)
        {
            this.Type = ValueType.Number;
            IsHexadecimal = NumericLiteralFacts.IsHexadecimal(value);
            var suffixStart = Math.Max(0, value.Length - 2);
            for (; suffixStart < value.Length; suffixStart++)
            {
                if (!NumericLiteralFacts.TryConsumeSuffix(
                        value,
                        suffixStart,
                        IsHexadecimal,
                        out var suffix,
                        out var suffixLength) ||
                    suffixStart + suffixLength != value.Length)
                {
                    continue;
                }

                Suffix = suffix;
                value = value[..suffixStart];
                break;
            }

            HasFractionOrExponent = !IsHexadecimal && ContainsFractionOrExponent(value);
            if (!HasFractionOrExponent && TryParseInteger(value, IsHexadecimal, out var integer))
            {
                HasIntegerValue = true;
                IntegerValue = integer;
                this.NumberValue = integer;
            }
            else
            {
                this.NumberValue = IsHexadecimal
                    ? ParseHexadecimal(value)
                    : ParseDecimal(value);
            }

            ValidateSuffix();

            // Only large numbers need their original spelling for TDoc's exact
            // Int64/UInt64 checks. Normal numeric tokens retain the existing lazy
            // Value allocation behavior.
            if (Math.Abs(this.NumberValue) > 9007199254740991d)
            {
                base.Value = value.ToString();
            }
        }

        internal NumberToken(double value)
            : this(value, NumericLiteralSuffix.None)
        {
        }

        internal NumberToken(double value, NumericLiteralSuffix suffix)
        {
            this.Type = ValueType.Number;
            this.NumberValue = value;
            Suffix = suffix;
            InitializeIntegerValue(value);
            ValidateSuffix();
        }

        internal NumberToken(long value, NumericLiteralSuffix suffix)
        {
            Type = ValueType.Number;
            NumberValue = value;
            Suffix = suffix;
            HasIntegerValue = true;
            IntegerIsNegative = value < 0;
            IntegerValue = value < 0
                ? unchecked((ulong)(-(value + 1))) + 1UL
                : (ulong)value;
            ValidateSuffix();
        }

        internal NumberToken(ulong value, NumericLiteralSuffix suffix)
        {
            Type = ValueType.Number;
            NumberValue = value;
            Suffix = suffix;
            HasIntegerValue = true;
            IntegerValue = value;
            ValidateSuffix();
        }

        public NumericLiteralSuffix Suffix { get; }

        public bool IsHexadecimal { get; }

        public bool HasFractionOrExponent { get; }

        public bool HasIntegerValue { get; private set; }

        public ulong IntegerValue { get; private set; }

        internal bool IntegerIsNegative { get; private set; }

        internal bool IsInt64MinMagnitude =>
            Suffix == NumericLiteralSuffix.Int64 &&
            HasIntegerValue &&
            !IntegerIsNegative &&
            IntegerValue == 0x8000_0000_0000_0000UL;

        internal bool TryGetInt64(out long value)
        {
            if (!HasIntegerValue)
            {
                value = 0;
                return false;
            }
            if (IntegerIsNegative)
            {
                if (IntegerValue > 0x8000_0000_0000_0000UL)
                {
                    value = 0;
                    return false;
                }
                value = IntegerValue == 0x8000_0000_0000_0000UL
                    ? long.MinValue
                    : -(long)IntegerValue;
                return true;
            }
            if (IntegerValue <= long.MaxValue)
            {
                value = (long)IntegerValue;
                return true;
            }
            value = 0;
            return false;
        }

        internal bool TryGetNegatedInt64(out long value)
        {
            if (!HasIntegerValue || IntegerIsNegative ||
                IntegerValue > 0x8000_0000_0000_0000UL)
            {
                value = 0;
                return false;
            }
            value = IntegerValue == 0x8000_0000_0000_0000UL
                ? long.MinValue
                : -(long)IntegerValue;
            return true;
        }

        internal bool TryGetUInt64(out ulong value)
        {
            value = IntegerValue;
            return HasIntegerValue && !IntegerIsNegative;
        }

        private void ValidateSuffix()
        {
            if (Suffix == NumericLiteralSuffix.Int32 &&
                (!TryGetInt64(out var int32) || int32 < int.MinValue || int32 > int.MaxValue))
            {
                throw new FormatException("Integer suffix I requires a 32-bit integer literal.");
            }

            if (Suffix == NumericLiteralSuffix.Int64 &&
                (!HasIntegerValue || IntegerIsNegative
                    ? !TryGetInt64(out _)
                    : IntegerValue > 0x8000_0000_0000_0000UL))
            {
                throw new FormatException("Integer suffix L requires a 64-bit integer literal.");
            }

            if (Suffix == NumericLiteralSuffix.UInt32 &&
                (!TryGetUInt64(out var uint32) || uint32 > uint.MaxValue))
            {
                throw new FormatException("Integer suffix U requires an unsigned 32-bit integer literal.");
            }

            if (Suffix == NumericLiteralSuffix.UInt64 && !TryGetUInt64(out _))
            {
                throw new FormatException("Integer suffix UL requires an unsigned 64-bit integer literal.");
            }
        }

        private void InitializeIntegerValue(double value)
        {
            if (!double.IsFinite(value) || value != Math.Truncate(value) ||
                (value == 0d && BitConverter.DoubleToInt64Bits(value) < 0))
            {
                return;
            }
            if (value < 0d)
            {
                if (value < -9223372036854775808d) return;
                var signed = (long)value;
                HasIntegerValue = true;
                IntegerIsNegative = true;
                IntegerValue = unchecked((ulong)(-(signed + 1))) + 1UL;
                return;
            }
            if (value >= 18446744073709551616d) return;
            HasIntegerValue = true;
            IntegerValue = (ulong)value;
        }

        private static bool TryParseInteger(
            ReadOnlySpan<char> value,
            bool hexadecimal,
            out ulong result)
        {
            result = 0;
            var start = hexadecimal ? 2 : 0;
            for (var i = start; i < value.Length; i++)
            {
                var c = value[i];
                if (c == '_') continue;
                var digit = hexadecimal
                    ? c <= '9' ? c - '0' : c <= 'F' ? c - 'A' + 10 : c - 'a' + 10
                    : c - '0';
                var radix = hexadecimal ? 16UL : 10UL;
                if (result > (ulong.MaxValue - (uint)digit) / radix)
                {
                    result = 0;
                    return false;
                }
                result = result * radix + (uint)digit;
            }
            return true;
        }

        private static double ParseHexadecimal(ReadOnlySpan<char> value)
        {
            var result = 0d;
            for (var i = 2; i < value.Length; i++)
            {
                var c = value[i];
                if (c == '_') continue;
                var digit = c <= '9' ? c - '0' : c <= 'F' ? c - 'A' + 10 : c - 'a' + 10;
                result = result * 16d + digit;
            }
            return result;
        }

        private static double ParseDecimal(ReadOnlySpan<char> value)
        {
            if (value.IndexOf('_') < 0)
            {
                return Double.Parse(value, CultureInfo.InvariantCulture);
            }
            Span<char> clean = value.Length <= 128 ? stackalloc char[value.Length] : new char[value.Length];
            var length = 0;
            for (var i = 0; i < value.Length; i++)
            {
                var c = value[i];
                if (c != '_') clean[length++] = c;
            }
            return Double.Parse(clean[..length], CultureInfo.InvariantCulture);
        }

        private static bool ContainsFractionOrExponent(ReadOnlySpan<char> value)
        {
            for (var i = 0; i < value.Length; i++)
            {
                var c = value[i];
                if (c == '.' || c == 'e' || c == 'E')
                {
                    return true;
                }
            }
            return false;
        }

        public override string Value
        {
            get
            {
                var value = base.Value;
                if (value == null)
                {
                    value = NumberValue.ToString(CultureInfo.InvariantCulture);
                    base.Value = value;
                }
                return value;
            }
            internal set => base.Value = value;
        }

        public override string ToString()
        {
            return NumberValue.ToString(CultureInfo.InvariantCulture);
        }
        public override string ToValue()
        {
            return NumberValue.ToString(CultureInfo.InvariantCulture);
        }
        public Double NumberValue { get; private set; }
    }

}
