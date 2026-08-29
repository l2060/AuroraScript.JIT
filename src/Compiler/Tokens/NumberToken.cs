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
            if (value.Length > 0 &&
                NumericLiteralFacts.TryConsumeSuffix(
                    value,
                    value.Length - 1,
                    IsHexadecimal,
                    out var suffix))
            {
                Suffix = suffix;
                value = value[..^1];
            }

            if (IsHexadecimal)
            {
                ulong number = 0;
                for (int i = 2; i < value.Length; i++)
                {
                    var c = value[i];
                    int digit = c <= '9' ? c - '0' : (c <= 'F' ? c - 'A' + 10 : c - 'a' + 10);
                    number = (number << 4) + (uint)digit;
                }
                this.NumberValue = number;
            }
            else
            {
                HasFractionOrExponent = ContainsFractionOrExponent(value);
                if (value.IndexOf('_') < 0)
                {
                    this.NumberValue = Double.Parse(value, CultureInfo.InvariantCulture);
                }
                else
                {
                    Span<char> clean = value.Length <= 128 ? stackalloc char[value.Length] : new char[value.Length];
                    int length = 0;
                    for (int i = 0; i < value.Length; i++)
                    {
                        var c = value[i];
                        if (c != '_') clean[length++] = c;
                    }
                    this.NumberValue = Double.Parse(clean.Slice(0, length), CultureInfo.InvariantCulture);
                }
            }

            if (Suffix == NumericLiteralSuffix.Int32 &&
                !NumericLiteralFacts.IsExactInt32(this.NumberValue))
            {
                throw new FormatException("Integer suffix I requires a 32-bit integer literal.");
            }

            if (Suffix == NumericLiteralSuffix.Int64 &&
                !NumericLiteralFacts.IsExactInt64(this.NumberValue))
            {
                throw new FormatException("Integer suffix L requires a 64-bit integer literal.");
            }

            // Only large numbers need their original spelling for TDoc's exact
            // Int64/UInt64 checks. Normal numeric tokens retain the existing lazy
            // Value allocation behavior.
            if (Math.Abs(this.NumberValue) > 9007199254740991d)
            {
                base.Value = value.ToString();
            }
        }

        internal NumberToken(double value)
        {
            this.Type = ValueType.Number;
            this.NumberValue = value;
        }

        public NumericLiteralSuffix Suffix { get; }

        public bool IsHexadecimal { get; }

        public bool HasFractionOrExponent { get; }

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
