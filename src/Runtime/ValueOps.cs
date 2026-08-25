using AuroraScript.Runtime.Types;
using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace AuroraScript.Runtime
{
    /// <summary>
    /// Defines the dynamic value semantics used at typed-CIL boundaries.
    /// Statically proven numeric and Boolean operations are emitted directly and
    /// do not call this class.
    /// </summary>
    [EditorBrowsable(EditorBrowsableState.Never)]
    public static class ValueOps
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static bool TryToArithmeticNumber(ScriptDatum value, out double number)
        {
            if (value.Kind == ValueKind.Null)
            {
                number = 0d;
                return true;
            }

            return ScriptDatum.TryToNumber(value, out number);
        }

        /// <summary>Converts an arithmetic operand to a native number, returning NaN on failure.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static double ToArithmeticNumber(ScriptDatum value)
        {
            return TryToArithmeticNumber(value, out var number) ? number : double.NaN;
        }

        /// <summary>Tries the comparison/equality numeric coercion used by the language.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool TryToNumber(ScriptDatum value, out double number)
        {
            return ScriptDatum.TryToNumber(value, out number);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool IsTruthyNumber(double value)
        {
            return value != 0d && !double.IsNaN(value);
        }

        /// <summary>Converts a native script number to its truthiness.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool ToBoolean(double value)
        {
            return IsTruthyNumber(value);
        }

        /// <summary>Converts a dynamic value to its script truthiness.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool ToBoolean(ScriptDatum value)
        {
            return value.Kind switch
            {
                ValueKind.Null => false,
                ValueKind.Boolean => value.Boolean,
                ValueKind.Number => IsTruthyNumber(value.Number),
                ValueKind.String => !string.IsNullOrEmpty(value.StringText),
                _ => value.Object != ScriptObject.Null,
            };
        }

        /// <summary>Converts an object value to its script truthiness.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool ToBoolean(ScriptObject value)
        {
            return value != null && value.IsTrue();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static string ToStringForConcat(ScriptDatum value)
        {
            return value.Kind switch
            {
                ValueKind.Null => "null",
                ValueKind.Boolean => value.Boolean.ToString(),
                ValueKind.Number => value.Number.ToString(),
                ValueKind.String => value.StringText,
                _ => value.Reference?.ToString() ?? "null",
            };
        }

        /// <summary>Implements dynamic addition and string concatenation.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ScriptDatum Add(ScriptDatum left, ScriptDatum right)
        {
            if (left.Kind == ValueKind.Number && right.Kind == ValueKind.Number)
            {
                return ScriptDatum.FromNumber(left.Number + right.Number);
            }

            if (left.Kind == ValueKind.String || right.Kind == ValueKind.String)
            {
                return ScriptDatum.FromString(string.Concat(ToStringForConcat(left), ToStringForConcat(right)));
            }

            if (TryToArithmeticNumber(left, out var leftNumber) &&
                TryToArithmeticNumber(right, out var rightNumber))
            {
                return ScriptDatum.FromNumber(leftNumber + rightNumber);
            }

            return ScriptDatum.FromString(string.Concat(ToStringForConcat(left), ToStringForConcat(right)));
        }

        /// <summary>Computes the truthiness of a dynamic addition result.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool AddBoolean(ScriptDatum left, ScriptDatum right)
        {
            if (left.Kind != ValueKind.String &&
                right.Kind != ValueKind.String &&
                TryToArithmeticNumber(left, out var leftNumber) &&
                TryToArithmeticNumber(right, out var rightNumber))
            {
                return IsTruthyNumber(leftNumber + rightNumber);
            }

            return ToBoolean(Add(left, right));
        }

        /// <summary>
        /// Adds a native number to a dynamic right operand and coerces the sum,
        /// equivalent to <c>ToArithmeticNumber(Add(left, right))</c>. Only a
        /// string right operand can turn the addition into a concatenation, so
        /// the number kind is checked before taking the native path.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static double AddToNumberLeft(double left, ScriptDatum right)
        {
            return right.Kind == ValueKind.Number
                ? left + right.Number
                : ToArithmeticNumber(Add(ScriptDatum.FromNumber(left), right));
        }

        /// <summary>
        /// Adds a dynamic left operand to a native number and coerces the sum,
        /// equivalent to <c>ToArithmeticNumber(Add(left, right))</c>.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static double AddToNumberRight(ScriptDatum left, double right)
        {
            return left.Kind == ValueKind.Number
                ? left.Number + right
                : ToArithmeticNumber(Add(left, ScriptDatum.FromNumber(right)));
        }

        /// <summary>Concatenates a dynamic value with a literal suffix.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ScriptDatum AddStringRight(ScriptDatum left, string right)
        {
            return ScriptDatum.FromString(string.Concat(ToStringForConcat(left), right));
        }

        /// <summary>Concatenates a literal prefix with a dynamic value.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ScriptDatum AddStringLeft(string left, ScriptDatum right)
        {
            return ScriptDatum.FromString(string.Concat(left, ToStringForConcat(right)));
        }

        /// <summary>Concatenates two dynamic values separated by a literal.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ScriptDatum AddStringMiddle(ScriptDatum left, string middle, ScriptDatum right)
        {
            return ScriptDatum.FromString(string.Concat(ToStringForConcat(left), middle, ToStringForConcat(right)));
        }

        /// <summary>Returns a string length without requiring a null check in generated CIL.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int GetStringLength(string value)
        {
            return value?.Length ?? 0;
        }

        /// <summary>Implements dynamic subtraction.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ScriptDatum Subtract(ScriptDatum left, ScriptDatum right)
        {
            return ScriptDatum.FromNumber(
                TryToArithmeticNumber(left, out var leftNumber) &&
                TryToArithmeticNumber(right, out var rightNumber)
                    ? leftNumber - rightNumber
                    : double.NaN);
        }

        /// <summary>Computes the truthiness of a dynamic subtraction result.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool SubtractBoolean(ScriptDatum left, ScriptDatum right)
        {
            return TryToArithmeticNumber(left, out var leftNumber) &&
                TryToArithmeticNumber(right, out var rightNumber) &&
                IsTruthyNumber(leftNumber - rightNumber);
        }

        /// <summary>Implements dynamic multiplication.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ScriptDatum Multiply(ScriptDatum left, ScriptDatum right)
        {
            return ScriptDatum.FromNumber(
                TryToArithmeticNumber(left, out var leftNumber) &&
                TryToArithmeticNumber(right, out var rightNumber)
                    ? leftNumber * rightNumber
                    : double.NaN);
        }

        /// <summary>Computes the truthiness of a dynamic multiplication result.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool MultiplyBoolean(ScriptDatum left, ScriptDatum right)
        {
            return TryToArithmeticNumber(left, out var leftNumber) &&
                TryToArithmeticNumber(right, out var rightNumber) &&
                IsTruthyNumber(leftNumber * rightNumber);
        }

        /// <summary>Implements dynamic division.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ScriptDatum Divide(ScriptDatum left, ScriptDatum right)
        {
            return ScriptDatum.FromNumber(
                TryToArithmeticNumber(left, out var leftNumber) &&
                TryToArithmeticNumber(right, out var rightNumber)
                    ? leftNumber / rightNumber
                    : double.NaN);
        }

        /// <summary>Computes the truthiness of a dynamic division result.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool DivideBoolean(ScriptDatum left, ScriptDatum right)
        {
            return TryToArithmeticNumber(left, out var leftNumber) &&
                TryToArithmeticNumber(right, out var rightNumber) &&
                IsTruthyNumber(leftNumber / rightNumber);
        }

        /// <summary>Implements dynamic remainder.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ScriptDatum Modulo(ScriptDatum left, ScriptDatum right)
        {
            return ScriptDatum.FromNumber(
                TryToArithmeticNumber(left, out var leftNumber) &&
                TryToArithmeticNumber(right, out var rightNumber)
                    ? leftNumber % rightNumber
                    : double.NaN);
        }

        /// <summary>Computes the truthiness of a dynamic remainder result.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool ModuloBoolean(ScriptDatum left, ScriptDatum right)
        {
            return TryToArithmeticNumber(left, out var leftNumber) &&
                TryToArithmeticNumber(right, out var rightNumber) &&
                IsTruthyNumber(leftNumber % rightNumber);
        }

        /// <summary>Implements script equality.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool EqualBoolean(ScriptDatum left, ScriptDatum right)
        {
            if (left.Kind == right.Kind)
            {
                switch (left.Kind)
                {
                    case ValueKind.Null:
                        return true;
                    case ValueKind.Boolean:
                        return left.Boolean == right.Boolean;
                    case ValueKind.Number:
                        return left.Number == right.Number;
                    case ValueKind.String:
                        return string.Equals(left.StringText, right.StringText, StringComparison.Ordinal);
                    default:
                        var leftObject = left.Reference as ScriptObject;
                        var rightObject = right.Reference as ScriptObject;
                        return ReferenceEquals(leftObject, rightObject) ||
                            (leftObject != null && leftObject.HasValueEquality && leftObject.ValueEquals(rightObject));
                }
            }

            return ScriptDatum.TryToNumber(left, out var leftNumber) &&
                ScriptDatum.TryToNumber(right, out var rightNumber) &&
                leftNumber == rightNumber;
        }

        /// <summary>Implements script equality and returns a dynamic Boolean.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ScriptDatum Equal(ScriptDatum left, ScriptDatum right)
        {
            return ScriptDatum.FromBoolean(EqualBoolean(left, right));
        }

        /// <summary>Implements script inequality.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool NotEqualBoolean(ScriptDatum left, ScriptDatum right)
        {
            return !EqualBoolean(left, right);
        }

        /// <summary>Implements script inequality and returns a dynamic Boolean.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ScriptDatum NotEqual(ScriptDatum left, ScriptDatum right)
        {
            return ScriptDatum.FromBoolean(!EqualBoolean(left, right));
        }

        /// <summary>Implements dynamic less-than comparison.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool LessBoolean(ScriptDatum left, ScriptDatum right)
        {
            return ScriptDatum.TryToNumber(left, out var leftNumber) &&
                ScriptDatum.TryToNumber(right, out var rightNumber) &&
                leftNumber < rightNumber;
        }

        /// <summary>Implements dynamic less-than comparison.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ScriptDatum Less(ScriptDatum left, ScriptDatum right)
        {
            return ScriptDatum.FromBoolean(LessBoolean(left, right));
        }

        /// <summary>Implements dynamic less-than-or-equal comparison.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool LessEqualBoolean(ScriptDatum left, ScriptDatum right)
        {
            return ScriptDatum.TryToNumber(left, out var leftNumber) &&
                ScriptDatum.TryToNumber(right, out var rightNumber) &&
                leftNumber <= rightNumber;
        }

        /// <summary>Implements dynamic less-than-or-equal comparison.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ScriptDatum LessEqual(ScriptDatum left, ScriptDatum right)
        {
            return ScriptDatum.FromBoolean(LessEqualBoolean(left, right));
        }

        /// <summary>Implements dynamic greater-than comparison.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool GreaterBoolean(ScriptDatum left, ScriptDatum right)
        {
            return ScriptDatum.TryToNumber(left, out var leftNumber) &&
                ScriptDatum.TryToNumber(right, out var rightNumber) &&
                leftNumber > rightNumber;
        }

        /// <summary>Implements dynamic greater-than comparison.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ScriptDatum Greater(ScriptDatum left, ScriptDatum right)
        {
            return ScriptDatum.FromBoolean(GreaterBoolean(left, right));
        }

        /// <summary>Implements dynamic greater-than-or-equal comparison.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool GreaterEqualBoolean(ScriptDatum left, ScriptDatum right)
        {
            return ScriptDatum.TryToNumber(left, out var leftNumber) &&
                ScriptDatum.TryToNumber(right, out var rightNumber) &&
                leftNumber >= rightNumber;
        }

        /// <summary>Implements dynamic greater-than-or-equal comparison.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ScriptDatum GreaterEqual(ScriptDatum left, ScriptDatum right)
        {
            return ScriptDatum.FromBoolean(GreaterEqualBoolean(left, right));
        }

        /// <summary>Implements 32-bit dynamic bitwise AND.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ScriptDatum BitwiseAnd(ScriptDatum left, ScriptDatum right)
        {
            if (left.Kind == ValueKind.Number && right.Kind == ValueKind.Number)
            {
                return ScriptDatum.FromNumber(unchecked((int)(long)left.Number) & unchecked((int)(long)right.Number));
            }

            return left.Kind == ValueKind.Null || right.Kind == ValueKind.Null
                ? ScriptDatum.FromNumber(0d)
                : ScriptDatum.NaN;
        }

        /// <summary>Computes the truthiness of 32-bit dynamic bitwise AND.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool BitwiseAndBoolean(ScriptDatum left, ScriptDatum right)
        {
            return left.Kind == ValueKind.Number &&
                right.Kind == ValueKind.Number &&
                (unchecked((int)(long)left.Number) & unchecked((int)(long)right.Number)) != 0;
        }

        /// <summary>Implements 32-bit dynamic bitwise OR.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ScriptDatum BitwiseOr(ScriptDatum left, ScriptDatum right)
        {
            if (left.Kind == ValueKind.Number && right.Kind == ValueKind.Number)
            {
                return ScriptDatum.FromNumber(unchecked((int)(long)left.Number) | unchecked((int)(long)right.Number));
            }

            return left.Kind == ValueKind.Null ? right : ScriptDatum.NaN;
        }

        /// <summary>Computes the truthiness of 32-bit dynamic bitwise OR.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool BitwiseOrBoolean(ScriptDatum left, ScriptDatum right)
        {
            if (left.Kind == ValueKind.Number && right.Kind == ValueKind.Number)
            {
                return (unchecked((int)(long)left.Number) | unchecked((int)(long)right.Number)) != 0;
            }

            return left.Kind == ValueKind.Null && ToBoolean(right);
        }

        /// <summary>Implements 32-bit dynamic bitwise XOR.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ScriptDatum BitwiseXor(ScriptDatum left, ScriptDatum right)
        {
            return left.Kind == ValueKind.Number && right.Kind == ValueKind.Number
                ? ScriptDatum.FromNumber(unchecked((int)(long)left.Number) ^ unchecked((int)(long)right.Number))
                : ScriptDatum.NaN;
        }

        /// <summary>Computes the truthiness of 32-bit dynamic bitwise XOR.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool BitwiseXorBoolean(ScriptDatum left, ScriptDatum right)
        {
            return left.Kind == ValueKind.Number &&
                right.Kind == ValueKind.Number &&
                (unchecked((int)(long)left.Number) ^ unchecked((int)(long)right.Number)) != 0;
        }

        /// <summary>Implements 32-bit left shift.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ScriptDatum LeftShift(ScriptDatum left, ScriptDatum right)
        {
            return left.Kind == ValueKind.Number && right.Kind == ValueKind.Number
                ? ScriptDatum.FromNumber((int)left.Number << (int)right.Number)
                : ScriptDatum.NaN;
        }

        /// <summary>Computes the truthiness of a 32-bit left shift.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool LeftShiftBoolean(ScriptDatum left, ScriptDatum right)
        {
            return left.Kind == ValueKind.Number &&
                right.Kind == ValueKind.Number &&
                ((int)left.Number << (int)right.Number) != 0;
        }

        /// <summary>Implements signed 32-bit right shift.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ScriptDatum RightShift(ScriptDatum left, ScriptDatum right)
        {
            return left.Kind == ValueKind.Number && right.Kind == ValueKind.Number
                ? ScriptDatum.FromNumber((int)left.Number >> (int)right.Number)
                : ScriptDatum.NaN;
        }

        /// <summary>Computes the truthiness of a signed 32-bit right shift.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool RightShiftBoolean(ScriptDatum left, ScriptDatum right)
        {
            return left.Kind == ValueKind.Number &&
                right.Kind == ValueKind.Number &&
                ((int)left.Number >> (int)right.Number) != 0;
        }

        /// <summary>Implements unsigned 32-bit right shift.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ScriptDatum UnsignedRightShift(ScriptDatum left, ScriptDatum right)
        {
            return left.Kind == ValueKind.Number && right.Kind == ValueKind.Number
                ? ScriptDatum.FromNumber(unchecked((uint)(int)left.Number) >> (int)right.Number)
                : ScriptDatum.NaN;
        }

        /// <summary>Computes the truthiness of an unsigned 32-bit right shift.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool UnsignedRightShiftBoolean(ScriptDatum left, ScriptDatum right)
        {
            return left.Kind == ValueKind.Number &&
                right.Kind == ValueKind.Number &&
                ((int)left.Number >>> (int)right.Number) != 0;
        }

        /// <summary>Implements logical negation.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ScriptDatum Not(ScriptDatum value)
        {
            return ScriptDatum.FromBoolean(!ToBoolean(value));
        }

        /// <summary>Implements numeric negation.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ScriptDatum Negate(ScriptDatum value)
        {
            return ScriptDatum.FromNumber(TryToArithmeticNumber(value, out var number) ? -number : double.NaN);
        }

        /// <summary>Implements 32-bit bitwise negation.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ScriptDatum BitwiseNot(ScriptDatum value)
        {
            return ScriptDatum.TryToInteger(value, out var number)
                ? ScriptDatum.FromNumber(~(int)number)
                : ScriptDatum.NaN;
        }

        /// <summary>Applies script increment/decrement numeric coercion.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ScriptDatum ChangeByOne(ScriptDatum value, double delta)
        {
            return ScriptDatum.FromNumber(
                ScriptDatum.TryToNumber(value, out var number)
                    ? number + delta
                    : double.NaN);
        }

        /// <summary>Returns the script type name for a dynamic value.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ScriptDatum TypeOf(ScriptDatum value)
        {
            return ScriptDatum.TypeOf(value);
        }
    }
}
