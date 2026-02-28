using AuroraScript.Runtime.Types;
using System;
using System.Runtime.CompilerServices;

namespace AuroraScript.Runtime
{
    /// <summary>
    /// Provides helper methods used by the CIL JIT emitter to perform runtime operations.
    /// These methods handle type conversions, operator overloading, and property/element access.
    /// </summary>
    public static class CILHelper
    {
        /// <summary>
        /// Performs the addition operation (+) between two script values.
        /// Handles numeric addition, string concatenation, and type coercion.
        /// </summary>
        /// <param name="a">The left operand.</param>
        /// <param name="b">The right operand.</param>
        /// <returns>The result of the addition or concatenation.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ScriptDatum Add(ScriptDatum a, ScriptDatum b)
        {
            if (a.Kind == ValueKind.Number && b.Kind == ValueKind.Number)
            {
                return ScriptDatum.FromNumber(a.Number + b.Number);
            }
            if (a.Kind == ValueKind.String || b.Kind == ValueKind.String)
            {
                return ScriptDatum.FromString(ScriptDatum.ToString(a) + ScriptDatum.ToString(b));
            }
            if (ScriptDatum.TryToNumber(a, out var na) && ScriptDatum.TryToNumber(b, out var nb))
            {
                return ScriptDatum.FromNumber(na + nb);
            }
            else
            {
                return ScriptDatum.FromString(ScriptDatum.ToString(a) + ScriptDatum.ToString(b));
            }
        }

        /// <summary>
        /// Performs the subtraction operation (-) between two script values.
        /// Coerces operands to numbers; returns NaN if conversion fails.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ScriptDatum Subtract(ScriptDatum a, ScriptDatum b)
        {
            if (a.Kind == ValueKind.Number && b.Kind == ValueKind.Number)
            {
                return ScriptDatum.FromNumber(a.Number - b.Number);
            }
            else if (ScriptDatum.TryToNumber(a, out var na) && ScriptDatum.TryToNumber(b, out var nb))
            {
                return ScriptDatum.FromNumber(na - nb);
            }
            else
            {
                return ScriptDatum.FromNumber(double.NaN);
            }
        }

        /// <summary>
        /// Performs the multiplication operation (*) between two script values.
        /// Coerces operands to numbers; returns NaN if conversion fails.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ScriptDatum Multiply(ScriptDatum a, ScriptDatum b)
        {
            if (a.Kind == ValueKind.Number && b.Kind == ValueKind.Number)
            {
                return ScriptDatum.FromNumber(a.Number * b.Number);
            }
            else if (ScriptDatum.TryToNumber(a, out var na) && ScriptDatum.TryToNumber(b, out var nb))
            {
                return ScriptDatum.FromNumber(na * nb);
            }
            else
            {
                return ScriptDatum.FromNumber(double.NaN);
            }
        }

        /// <summary>
        /// Performs the division operation (/) between two script values.
        /// Coerces operands to numbers; returns NaN if conversion fails.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ScriptDatum Divide(ScriptDatum a, ScriptDatum b)
        {
            if (a.Kind == ValueKind.Number && b.Kind == ValueKind.Number)
            {
                return ScriptDatum.FromNumber(a.Number / b.Number);
            }
            else if (ScriptDatum.TryToNumber(a, out var na) && ScriptDatum.TryToNumber(b, out var nb))
            {
                return ScriptDatum.FromNumber(na / nb);
            }
            else
            {
                return ScriptDatum.FromNumber(double.NaN);
            }
        }

        /// <summary>
        /// Performs the modulo operation (%) between two script values.
        /// Coerces operands to numbers; returns NaN if conversion fails.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ScriptDatum Modulo(ScriptDatum a, ScriptDatum b)
        {
            if (a.Kind == ValueKind.Number && b.Kind == ValueKind.Number)
            {
                return ScriptDatum.FromNumber(a.Number % b.Number);
            }
            else if (ScriptDatum.TryToNumber(a, out var na) && ScriptDatum.TryToNumber(b, out var nb))
            {
                return ScriptDatum.FromNumber(na % nb);
            }
            else
            {
                return ScriptDatum.FromNumber(double.NaN);
            }
        }

        /// <summary>
        /// Checks if two script values are equal (==).
        /// Handles primitive equality and object reference equality.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ScriptDatum Equal(ScriptDatum a, ScriptDatum b)
        {
            if (a.Kind == b.Kind)
            {
                switch (a.Kind)
                {
                    case ValueKind.Null: return ScriptDatum.FromBoolean(true);
                    case ValueKind.Boolean: return ScriptDatum.FromBoolean(a.Boolean == b.Boolean);
                    case ValueKind.Number: return ScriptDatum.FromBoolean(a.Number == b.Number);
                    case ValueKind.String: return ScriptDatum.FromBoolean(a.String.Value == b.String.Value);
                    default: return ScriptDatum.FromBoolean(ReferenceEquals(a.Object, b.Object));
                }
            }
            if (ScriptDatum.TryToNumber(a, out var na) && ScriptDatum.TryToNumber(b, out var nb))
            {
                return ScriptDatum.FromBoolean(na == nb);
            }
            return ScriptDatum.FromBoolean(false);
        }

        /// <summary>
        /// Checks if two script values are not equal (!=).
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ScriptDatum NotEqual(ScriptDatum a, ScriptDatum b)
        {
            return ScriptDatum.FromBoolean(!Equal(a, b).Boolean);
        }

        /// <summary>
        /// Checks if the left value is less than the right value (&lt;).
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ScriptDatum Less(ScriptDatum a, ScriptDatum b)
        {
            if (a.Kind == ValueKind.Number && b.Kind == ValueKind.Number)
            {
                return ScriptDatum.FromBoolean(a.Number < b.Number);
            }
            else if (ScriptDatum.TryToNumber(a, out var na) && ScriptDatum.TryToNumber(b, out var nb))
            {
                return ScriptDatum.FromBoolean(na < nb);
            }
            return ScriptDatum.FromBoolean(false);
        }

        /// <summary>
        /// Checks if the left value is less than or equal to the right value (&lt;=).
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ScriptDatum LessEqual(ScriptDatum a, ScriptDatum b)
        {
            if (a.Kind == ValueKind.Number && b.Kind == ValueKind.Number)
            {
                return ScriptDatum.FromBoolean(a.Number <= b.Number);
            }
            else if (ScriptDatum.TryToNumber(a, out var na) && ScriptDatum.TryToNumber(b, out var nb))
            {
                return ScriptDatum.FromBoolean(na <= nb);
            }
            return ScriptDatum.FromBoolean(false);
        }

        /// <summary>
        /// Checks if the left value is greater than the right value (&gt;).
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ScriptDatum Greater(ScriptDatum a, ScriptDatum b)
        {
            if (a.Kind == ValueKind.Number && b.Kind == ValueKind.Number)
            {
                return ScriptDatum.FromBoolean(a.Number > b.Number);
            }
            else if (ScriptDatum.TryToNumber(a, out var na) && ScriptDatum.TryToNumber(b, out var nb))
            {
                return ScriptDatum.FromBoolean(na > nb);
            }
            return ScriptDatum.FromBoolean(false);
        }

        /// <summary>
        /// Checks if the left value is greater than or equal to the right value (&gt;=).
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ScriptDatum GreaterEqual(ScriptDatum a, ScriptDatum b)
        {
            if (a.Kind == ValueKind.Number && b.Kind == ValueKind.Number)
            {
                return ScriptDatum.FromBoolean(a.Number >= b.Number);
            }
            else if (ScriptDatum.TryToNumber(a, out var na) && ScriptDatum.TryToNumber(b, out var nb))
            {
                return ScriptDatum.FromBoolean(na >= nb);
            }
            return ScriptDatum.FromBoolean(false);
        }

        /// <summary>
        /// Performs the bitwise AND operation (&amp;) between two script values.
        /// Treats operands as 32-bit integers.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ScriptDatum BitwiseAnd(ScriptDatum a, ScriptDatum b)
        {
            if (a.Kind == ValueKind.Number && b.Kind == ValueKind.Number)
            {
                var v = unchecked((Int32)(Int64)a.Number) & unchecked((Int32)(Int64)b.Number);
                return ScriptDatum.FromNumber(v);
            }
            else if (a.Kind == ValueKind.Null || b.Kind == ValueKind.Null)
            {
                return ScriptDatum.FromNumber(0);
            }
            return ScriptDatum.FromNumber(Double.NaN);
        }

        /// <summary>
        /// Performs the bitwise OR operation (|) between two script values.
        /// Treats operands as 32-bit integers.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ScriptDatum BitwiseOr(ScriptDatum a, ScriptDatum b)
        {
            if (a.Kind == ValueKind.Number && b.Kind == ValueKind.Number)
            {
                var v = unchecked((Int32)(Int64)a.Number) | unchecked((Int32)(Int64)b.Number);
                return ScriptDatum.FromNumber(v);
            }
            else if (a.Kind == ValueKind.Null)
            {
                return b;
            }
            return ScriptDatum.FromNumber(Double.NaN);
        }

        /// <summary>
        /// Performs the bitwise XOR operation (^) between two script values.
        /// Treats operands as 32-bit integers.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ScriptDatum BitwiseXor(ScriptDatum a, ScriptDatum b)
        {
            if (a.Kind == ValueKind.Number && b.Kind == ValueKind.Number)
            {
                var v = unchecked((Int32)(Int64)a.Number) ^ unchecked((Int32)(Int64)b.Number);
                return ScriptDatum.FromNumber(v);
            }
            return ScriptDatum.FromNumber(Double.NaN);
        }

        /// <summary>
        /// Performs the left shift operation (&lt;&lt;) between two script values.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ScriptDatum LeftShift(ScriptDatum a, ScriptDatum b)
        {
            if (a.Kind == ValueKind.Number && b.Kind == ValueKind.Number)
            {
                var value = (double)((int)a.Number << (int)b.Number);
                return ScriptDatum.FromNumber(value);
            }
            return ScriptDatum.FromNumber(Double.NaN);
        }

        /// <summary>
        /// Performs the signed right shift operation (&gt;&gt;) between two script values.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ScriptDatum RightShift(ScriptDatum a, ScriptDatum b)
        {
            if (a.Kind == ValueKind.Number && b.Kind == ValueKind.Number)
            {
                var value = (double)((int)a.Number >> (int)b.Number);
                return ScriptDatum.FromNumber(value);
            }
            return ScriptDatum.FromNumber(Double.NaN);
        }

        /// <summary>
        /// Performs the unsigned right shift operation (&gt;&gt;&gt;) between two script values.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ScriptDatum UnsignedRightShift(ScriptDatum a, ScriptDatum b)
        {
            if (a.Kind == ValueKind.Number && b.Kind == ValueKind.Number)
            {
                var value = (double)((int)a.Number >>> (int)b.Number);
                return ScriptDatum.FromNumber(value);
            }
            return ScriptDatum.FromNumber(Double.NaN);
        }

        /// <summary>
        /// Performs the logical NOT operation (!).
        /// Returns a boolean based on the truthiness of the value.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ScriptDatum Not(ScriptDatum a)
        {
            if (a.Kind == ValueKind.Boolean) return ScriptDatum.FromBoolean(!a.Boolean);
            if (a.Kind == ValueKind.Null) return ScriptDatum.FromBoolean(true);
            if (a.Kind == ValueKind.Number) return ScriptDatum.FromBoolean(a.Number == 0);
            if (a.Kind == ValueKind.String) return ScriptDatum.FromBoolean(String.IsNullOrEmpty(a.String.Value));
            return ScriptDatum.FromBoolean(!a.Object.IsTrue());
        }

        /// <summary>
        /// Performs numeric negation (-).
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ScriptDatum Negate(ScriptDatum a)
        {
            if (ScriptDatum.TryToNumber(in a, out var value))
            {
                return ScriptDatum.FromNumber(-value);
            }
            else
            {
                return ScriptDatum.FromNumber(Double.NaN);
            }
        }

        /// <summary>
        /// Performs bitwise NOT operation (~).
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ScriptDatum BitwiseNot(ScriptDatum a)
        {
            if (ScriptDatum.TryToInteger(in a, out var value))
            {
                return ScriptDatum.FromNumber(~(int)value);
            }
            else
            {
                return ScriptDatum.FromNumber(Double.NaN);
            }
        }

        /// <summary>
        /// Gets the type name string of a script value.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ScriptDatum TypeOf(ScriptDatum a)
        {
            return ScriptDatum.TypeOf(a);
        }

        /// <summary>
        /// Gets an element or property from an object using a script value as the index/key.
        /// Handles specialized access for arrays and generic access for objects.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ScriptDatum GetElement(ScriptObject obj, ScriptDatum index)
        {
            if (obj is ScriptArray array)
            {
                ScriptDatum datum = default;
                array.GetElement((int)index.Number, ref datum);
                return datum;
            }
            else
            {
                string key = ScriptDatum.ToString(index);
                ScriptObject value = obj.GetPropertyValue(key);
                return ScriptDatum.FromObject(value);
            }
        }

        /// <summary>
        /// Sets an element or property on an object using a script value as the index/key.
        /// Handles specialized storage for arrays and generic property storage for objects.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void SetElement(ScriptObject obj, ScriptDatum index, ScriptDatum value)
        {
            if (obj is ScriptArray array)
            {
                array.SetElement((int)index.Number, value);
            }
            else
            {
                string key = ScriptDatum.ToString(index);
                obj.SetPropertyValue(key, ScriptDatum.ToObject(value));
            }
        }

        /// <summary>
        /// Performs prefix increment (++val).
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ScriptDatum IncrementPrefix(ref ScriptDatum val)
        {
            if (val.Kind == ValueKind.Number)
            {
                val.Number += 1;
            }
            else if (ScriptDatum.TryToNumber(val, out var n))
            {
                val = ScriptDatum.FromNumber(n + 1);
            }
            else
            {
                val = ScriptDatum.FromNumber(double.NaN);
            }
            return val;
        }

        /// <summary>
        /// Performs postfix increment (val++). Returns the value before incrementing.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ScriptDatum IncrementPostfix(ref ScriptDatum val)
        {
            ScriptDatum result = val;
            if (val.Kind == ValueKind.Number)
            {
                val.Number += 1;
            }
            else if (ScriptDatum.TryToNumber(val, out var n))
            {
                val = ScriptDatum.FromNumber(n + 1);
            }
            else
            {
                val = ScriptDatum.FromNumber(double.NaN);
            }
            return result;
        }

        /// <summary>
        /// Performs prefix decrement (--val).
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ScriptDatum DecrementPrefix(ref ScriptDatum val)
        {
            if (val.Kind == ValueKind.Number)
            {
                val.Number -= 1;
            }
            else if (ScriptDatum.TryToNumber(val, out var n))
            {
                val = ScriptDatum.FromNumber(n - 1);
            }
            else
            {
                val = ScriptDatum.FromNumber(double.NaN);
            }
            return val;
        }

        /// <summary>
        /// Performs postfix decrement (val--). Returns the value before decrementing.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ScriptDatum DecrementPostfix(ref ScriptDatum val)
        {
            ScriptDatum result = val;
            if (val.Kind == ValueKind.Number)
            {
                val.Number -= 1;
            }
            else if (ScriptDatum.TryToNumber(val, out var n))
            {
                val = ScriptDatum.FromNumber(n - 1);
            }
            else
            {
                val = ScriptDatum.FromNumber(double.NaN);
            }
            return result;
        }

        /// <summary>
        /// Performs prefix increment on an object element (++obj[index]).
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ScriptDatum IncrementElementPrefix(ScriptObject obj, ScriptDatum index)
        {
            if (obj is ScriptArray array)
            {
                int idx = (int)index.Number;
                ScriptDatum val = default;
                array.GetElement(idx, ref val);
                if (ScriptDatum.TryToNumber(val, out var n))
                    val = ScriptDatum.FromNumber(n + 1);
                else
                    val = ScriptDatum.FromNumber(double.NaN);
                array.SetElement(idx, val);
                return val;
            }
            else
            {
                string key = ScriptDatum.ToString(index);
                ScriptDatum val = ScriptDatum.FromObject(obj.GetPropertyValue(key));
                if (ScriptDatum.TryToNumber(val, out var n))
                    val = ScriptDatum.FromNumber(n + 1);
                else
                    val = ScriptDatum.FromNumber(double.NaN);
                obj.SetPropertyValue(key, ScriptDatum.ToObject(val));
                return val;
            }
        }

        /// <summary>
        /// Performs postfix increment on an object element (obj[index]++).
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ScriptDatum IncrementElementPostfix(ScriptObject obj, ScriptDatum index)
        {
            if (obj is ScriptArray array)
            {
                int idx = (int)index.Number;
                ScriptDatum val = default;
                array.GetElement(idx, ref val);
                ScriptDatum result = val;
                if (ScriptDatum.TryToNumber(val, out var n))
                    val = ScriptDatum.FromNumber(n + 1);
                else
                    val = ScriptDatum.FromNumber(double.NaN);
                array.SetElement(idx, val);
                return result;
            }
            else
            {
                string key = ScriptDatum.ToString(index);
                ScriptDatum val = ScriptDatum.FromObject(obj.GetPropertyValue(key));
                ScriptDatum result = val;
                if (ScriptDatum.TryToNumber(val, out var n))
                    val = ScriptDatum.FromNumber(n + 1);
                else
                    val = ScriptDatum.FromNumber(double.NaN);
                obj.SetPropertyValue(key, ScriptDatum.ToObject(val));
                return result;
            }
        }

        /// <summary>
        /// Performs prefix decrement on an object element (--obj[index]).
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ScriptDatum DecrementElementPrefix(ScriptObject obj, ScriptDatum index)
        {
            if (obj is ScriptArray array)
            {
                int idx = (int)index.Number;
                ScriptDatum val = default;
                array.GetElement(idx, ref val);
                if (ScriptDatum.TryToNumber(val, out var n))
                    val = ScriptDatum.FromNumber(n - 1);
                else
                    val = ScriptDatum.FromNumber(double.NaN);
                array.SetElement(idx, val);
                return val;
            }
            else
            {
                string key = ScriptDatum.ToString(index);
                ScriptDatum val = ScriptDatum.FromObject(obj.GetPropertyValue(key));
                if (ScriptDatum.TryToNumber(val, out var n))
                    val = ScriptDatum.FromNumber(n - 1);
                else
                    val = ScriptDatum.FromNumber(double.NaN);
                obj.SetPropertyValue(key, ScriptDatum.ToObject(val));
                return val;
            }
        }

        /// <summary>
        /// Performs postfix decrement on an object element (obj[index]--).
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ScriptDatum DecrementElementPostfix(ScriptObject obj, ScriptDatum index)
        {
            if (obj is ScriptArray array)
            {
                int idx = (int)index.Number;
                ScriptDatum val = default;
                array.GetElement(idx, ref val);
                ScriptDatum result = val;
                if (ScriptDatum.TryToNumber(val, out var n))
                    val = ScriptDatum.FromNumber(n - 1);
                else
                    val = ScriptDatum.FromNumber(double.NaN);
                array.SetElement(idx, val);
                return result;
            }
            else
            {
                string key = ScriptDatum.ToString(index);
                ScriptDatum val = ScriptDatum.FromObject(obj.GetPropertyValue(key));
                ScriptDatum result = val;
                if (ScriptDatum.TryToNumber(val, out var n))
                    val = ScriptDatum.FromNumber(n - 1);
                else
                    val = ScriptDatum.FromNumber(double.NaN);
                obj.SetPropertyValue(key, ScriptDatum.ToObject(val));
                return result;
            }
        }

        /// <summary>
        /// Performs prefix increment on an object property (++obj.name).
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ScriptDatum IncrementPropertyPrefix(ScriptObject obj, string name)
        {
            ScriptDatum val = ScriptDatum.FromObject(obj.GetPropertyValue(name));
            if (ScriptDatum.TryToNumber(val, out var n))
                val = ScriptDatum.FromNumber(n + 1);
            else
                val = ScriptDatum.FromNumber(double.NaN);
            obj.SetPropertyValue(name, ScriptDatum.ToObject(val));
            return val;
        }

        /// <summary>
        /// Performs postfix increment on an object property (obj.name++).
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ScriptDatum IncrementPropertyPostfix(ScriptObject obj, string name)
        {
            ScriptDatum val = ScriptDatum.FromObject(obj.GetPropertyValue(name));
            ScriptDatum result = val;
            if (ScriptDatum.TryToNumber(val, out var n))
                val = ScriptDatum.FromNumber(n + 1);
            else
                val = ScriptDatum.FromNumber(double.NaN);
            obj.SetPropertyValue(name, ScriptDatum.ToObject(val));
            return result;
        }

        /// <summary>
        /// Performs prefix decrement on an object property (--obj.name).
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ScriptDatum DecrementPropertyPrefix(ScriptObject obj, string name)
        {
            ScriptDatum val = ScriptDatum.FromObject(obj.GetPropertyValue(name));
            if (ScriptDatum.TryToNumber(val, out var n))
                val = ScriptDatum.FromNumber(n - 1);
            else
                val = ScriptDatum.FromNumber(double.NaN);
            obj.SetPropertyValue(name, ScriptDatum.ToObject(val));
            return val;
        }

        /// <summary>
        /// Performs postfix decrement on an object property (obj.name--).
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ScriptDatum DecrementPropertyPostfix(ScriptObject obj, string name)
        {
            ScriptDatum val = ScriptDatum.FromObject(obj.GetPropertyValue(name));
            ScriptDatum result = val;

            if (ScriptDatum.TryToNumber(val, out var n))
                val = ScriptDatum.FromNumber(n - 1);
            else
                val = ScriptDatum.FromNumber(double.NaN);
            obj.SetPropertyValue(name, ScriptDatum.ToObject(val));
            return result;
        }

        /// <summary>
        /// Coerces a script value to a boolean.
        /// Falsy values: null, false, 0, NaN, empty string.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool ToBoolean(ScriptDatum a)
        {
            return a.Kind switch
            {
                ValueKind.Null => false,
                ValueKind.Boolean => a.Boolean,
                ValueKind.Number => a.Number != 0 && !double.IsNaN(a.Number),
                ValueKind.String => !string.IsNullOrEmpty(a.String?.Value),
                _ => a.Object != ScriptObject.Null,
            };
        }

        /// <summary>
        /// Coerces a script object to a boolean.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool ToBoolean(ScriptObject obj)
        {
            return obj != null && obj != ScriptObject.Null && obj.IsTrue();
        }

        /// <summary>
        /// Deletes a property from an object.
        /// Handles specialized removal for array indices.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void DeleteProperty(ScriptContext ctx, ScriptObject obj, string name)
        {
            if (obj is ScriptArray array && Int32.TryParse(name, out var indenum))
            {
                array.Remove((int)indenum);
            }
            else
            {
                obj.DeletePropertyValue(ctx, name);
            }
        }

        /// <summary>
        /// Deletes an element from an object using a script value as index.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void DeleteElement(ScriptContext ctx, ScriptObject obj, ScriptDatum index)
        {
            if (obj is ScriptArray array && ScriptDatum.TryToInteger(in index, out var indenum))
            {
                array.Remove((int)indenum);
            }
            else
            {
                string key = ScriptDatum.ToString(index);
                obj.DeletePropertyValue(key);
            }
        }

        /// <summary>
        /// Checks if a value is included in a collection (the 'in' operator).
        /// </summary>
        /// <param name="collection">The collection to search in (object, array, or string).</param>
        /// <param name="value">The value to search for.</param>
        /// <returns>A <see cref="ScriptDatum"/> containing true if found; otherwise false.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ScriptDatum Included(ScriptObject collection, ScriptDatum value)
        {
            if (collection == null) return ScriptDatum.FromBoolean(false);
            var enumerator = collection.GetEnumerator();
            ScriptDatum current = default;
            while (enumerator.NextValue(out current))
            {
                if (current.Equals(value)) return ScriptDatum.FromBoolean(true);
            }
            return ScriptDatum.FromBoolean(false);
        }

        /// <summary>
        /// Spreads an object's elements or properties into an array.
        /// Commonly used for the spread operator (...) in array literals.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void SpreadInto(ScriptArray array, ScriptObject val)
        {
            if (val is ScriptArray source)
            {
                if (source.Length > 0)
                {
                    foreach (var item in source._items)
                    {
                        array.Push(item);
                    }
                }
            }
            else
            {
                array.Push(ScriptDatum.FromObject(val));
            }
        }

        /// <summary>
        /// Creates a new instance of a script type with the given arguments.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ScriptDatum New(ScriptObject type, ScriptContext ctx, ScriptDatum[] args)
        {
            if (type is ScriptType typed)
            {
                ScriptDatum result = default;
                typed.Construct(ctx, args, ref result);
                return result;
            }
            ThrowHelper.ThrowNotConstructor(type?.ToString() ?? "null");
            return default;
        }

        /// <summary>
        /// Attempts to get an argument at the specified index, returning a default value if missing.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ScriptDatum TryGetArg(ScriptDatum[] args, int index, ScriptDatum defaultValue)
        {
            if (index >= 0 && index < args.Length)
            {
                return args[index];
            }
            return defaultValue;
        }

        /// <summary>
        /// Gets an argument at the specified index. Returns default (null) if missing.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ScriptDatum GetArg(ScriptDatum[] args, int index)
        {
            if (index >= 0 && index < args.Length)
            {
                return args[index];
            }
            return default;
        }

        /// <summary>
        /// Resolves a compiled method delegate by its ID from the global registry.
        /// </summary>
        /// <param name="module">The module context (currently unused in resolution).</param>
        /// <param name="id">The unique ID of the compiled method.</param>
        /// <returns>The resolved script function delegate.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ScriptFunctionDelegate ResolveDelegate(ScriptModule module, int id)
        {
            return DynamicMethodRegistry.Resolve(id);
        }

        /// <summary>
        /// Throws a script-level exception. If the value is already an exception, it is thrown directly;
        /// otherwise, it is wrapped in an <see cref="AuroraRuntimeException"/>.
        /// </summary>
        /// <param name="datum">The value to throw.</param>
        public static void Throw(ScriptDatum datum)
        {
            if (ScriptDatum.TryGetError(in datum, out var error))
            {
                throw new AuroraRuntimeException(error);
            }


            if (datum.Kind == ValueKind.Object && datum.Object is Interop.ClrInstanceObject clrInstance && clrInstance.Instance is Exception ex)
            {
                throw ex;
            }
            throw new AuroraRuntimeException(datum.ToString());
        }



        /// <summary>
        /// Converts a .NET <see cref="Exception"/> into a <see cref="ScriptDatum"/> wrapping a <see cref="ScriptError"/>.
        /// This is used during <c>catch</c> blocks to expose the exception to script code.
        /// </summary>
        /// <param name="exception">The exception to convert.</param>
        /// <returns>A <see cref="ScriptDatum"/> containing the <see cref="ScriptError"/> representation.</returns>
        public static ScriptDatum ExceptionToError(Exception exception)
        {
            if (exception is AuroraRuntimeException auroraRuntimeException && auroraRuntimeException.internalError != null)
            {
                return ScriptDatum.FromError(auroraRuntimeException.internalError);
            }
            var error = new ScriptError(exception.Message, []);
            return ScriptDatum.FromError(error);
        }

    }
}
