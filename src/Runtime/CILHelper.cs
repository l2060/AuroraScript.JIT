using AuroraScript.Runtime.Types;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using AuroraScript.Runtime.Property;

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

        /// <summary>Concatenates a script value with a literal string on the right.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ScriptDatum AddStringRight(ScriptDatum a, string b)
        {
            return ScriptDatum.FromString(ScriptDatum.ToString(a) + b);
        }

        /// <summary>Concatenates a literal string on the left with a script value.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ScriptDatum AddStringLeft(string a, ScriptDatum b)
        {
            return ScriptDatum.FromString(a + ScriptDatum.ToString(b));
        }

        /// <summary>Concatenates a script value, literal string, and script value in one pass.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ScriptDatum AddStringMiddle(ScriptDatum a, string b, ScriptDatum c)
        {
            return ScriptDatum.FromString(string.Concat(ScriptDatum.ToString(a), b, ScriptDatum.ToString(c)));
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
                return obj.GetPropertyDatum(null, key);
            }
        }

        /// <summary>
        /// Gets an element or property from a script value without forcing the emitter to materialize an object first.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ScriptDatum GetElement(ScriptDatum obj, ScriptDatum index)
        {
            return GetElement(ScriptDatum.ToObject(obj), index);
        }

        /// <summary>
        /// Gets the length of arrays and strings without going through the generic property path.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ScriptDatum GetLength(ScriptObject obj, ScriptContext ctx)
        {
            if (obj is ScriptArray array)
            {
                return ScriptDatum.FromNumber(array.Length);
            }
            if (obj is StringValue str)
            {
                return ScriptDatum.FromNumber(str.Value.Length);
            }
            return obj.GetPropertyDatum(ctx, "length");
        }

        /// <summary>
        /// Gets the length of a script value without forcing the emitter to materialize an object first.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ScriptDatum GetLength(ScriptDatum obj, ScriptContext ctx)
        {
            if (obj.Kind == ValueKind.String)
            {
                return ScriptDatum.FromNumber(obj.String.Value.Length);
            }
            return GetLength(ScriptDatum.ToObject(obj), ctx);
        }

        /// <summary>
        /// Reads a fixed property name from a script value without forcing the emitter to materialize an object first.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ScriptDatum GetProperty(ScriptDatum obj, ScriptContext ctx, string name)
        {
            return ScriptDatum.ToObject(obj).GetPropertyDatum(ctx, name);
        }

        /// <summary>
        /// Reads two fixed property names in sequence, keeping the chain in one helper call.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ScriptDatum GetProperty2(ScriptObject obj, ScriptContext ctx, string name0, string name1)
        {
            return ScriptDatum.ToObject(obj.GetPropertyDatum(ctx, name0)).GetPropertyDatum(ctx, name1);
        }

        /// <summary>
        /// Reads two fixed property names from a script value in sequence.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ScriptDatum GetProperty2(ScriptDatum obj, ScriptContext ctx, string name0, string name1)
        {
            return GetProperty2(ScriptDatum.ToObject(obj), ctx, name0, name1);
        }

        /// <summary>
        /// Reads three fixed property names in sequence, keeping the chain in one helper call.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ScriptDatum GetProperty3(ScriptObject obj, ScriptContext ctx, string name0, string name1, string name2)
        {
            return ScriptDatum.ToObject(ScriptDatum.ToObject(obj.GetPropertyDatum(ctx, name0)).GetPropertyDatum(ctx, name1)).GetPropertyDatum(ctx, name2);
        }

        /// <summary>
        /// Reads three fixed property names from a script value in sequence.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ScriptDatum GetProperty3(ScriptDatum obj, ScriptContext ctx, string name0, string name1, string name2)
        {
            return GetProperty3(ScriptDatum.ToObject(obj), ctx, name0, name1, name2);
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
                obj.SetPropertyDatum(null, key, value);
            }
        }

        /// <summary>
        /// Sets an element or property on a script value without forcing the emitter to materialize an object first.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void SetElement(ScriptDatum obj, ScriptDatum index, ScriptDatum value)
        {
            SetElement(ScriptDatum.ToObject(obj), index, value);
        }

        /// <summary>
        /// Applies += to an object element while evaluating the receiver and index once.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ScriptDatum CompoundAddElement(ScriptObject obj, ScriptDatum index, ScriptDatum value)
        {
            var result = Add(GetElement(obj, index), value);
            SetElement(obj, index, result);
            return result;
        }

        /// <summary>
        /// Applies += to an element on a datum receiver while evaluating the receiver and index once.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ScriptDatum CompoundAddElement(ScriptDatum obj, ScriptDatum index, ScriptDatum value)
        {
            return CompoundAddElement(ScriptDatum.ToObject(obj), index, value);
        }

        /// <summary>Creates a plain three-property object literal using a cached hidden class.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ScriptObject CreateObject3(
            string name0,
            ScriptDatum value0,
            string name1,
            ScriptDatum value1,
            string name2,
            ScriptDatum value2)
        {
            var shape = HiddenClass.GetLiteralShape(name0, name1, name2);
            var values = new PropertyDescriptor[Math.Max(shape._maxSlot, (ushort)4)];
            values[0] = new PropertyDescriptor(null, null, value0);
            values[1] = new PropertyDescriptor(null, null, value1);
            values[2] = new PropertyDescriptor(null, null, value2);
            return new ScriptObject(shape, values);
        }

        /// <summary>Invokes a named property as a method without allocating a bound native wrapper.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ScriptDatum InvokeProperty(ScriptObject receiver, ScriptContext ctx, string name, ScriptDatum[] args)
        {
            if (receiver.TryResolveProperty(name, out var property) &&
                property.Getter == null &&
                property.Value is BondingFunction { Target: null } nativeFunction)
            {
                ScriptDatum result = default;
                nativeFunction.DatumMethod.Invoke(ctx, receiver, args, ref result);
                return result;
            }
            var function = receiver.GetPropertyValue(ctx, name);
            return function.Invoke(ctx, args);
        }

        /// <summary>Invokes a named zero-argument method without allocating an argument array for native methods.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ScriptDatum InvokeProperty0(ScriptObject receiver, ScriptContext ctx, string name)
        {
            if (receiver.TryResolveProperty(name, out var property) &&
                property.Getter == null &&
                property.Value is BondingFunction { Target: null } nativeFunction)
            {
                ScriptDatum result = default;
                nativeFunction.DatumMethod.Invoke(ctx, receiver, Span<ScriptDatum>.Empty, ref result);
                return result;
            }
            var function = receiver.GetPropertyValue(ctx, name);
            return function.Invoke(ctx, Array.Empty<ScriptDatum>());
        }

        /// <summary>Invokes a named one-argument method without allocating an argument array for native methods.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ScriptDatum InvokeProperty1(ScriptObject receiver, ScriptContext ctx, string name, ScriptDatum arg0)
        {
            if (receiver.TryResolveProperty(name, out var property) &&
                property.Getter == null &&
                property.Value is BondingFunction { Target: null } nativeFunction)
            {
                var args = MemoryMarshal.CreateSpan(ref arg0, 1);
                ScriptDatum result = default;
                nativeFunction.DatumMethod.Invoke(ctx, receiver, args, ref result);
                return result;
            }
            var function = receiver.GetPropertyValue(ctx, name);
            return function.Invoke(ctx, arg0);
        }

        /// <summary>Invokes a named two-argument method without allocating an argument array for native methods or closures.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ScriptDatum InvokeProperty2(ScriptObject receiver, ScriptContext ctx, string name, ScriptDatum arg0, ScriptDatum arg1)
        {
            if (receiver.TryResolveProperty(name, out var property) &&
                property.Getter == null &&
                property.Value is BondingFunction { Target: null } nativeFunction)
            {
                DatumBuffer2 args = default;
                args[0] = arg0;
                args[1] = arg1;
                ScriptDatum result = default;
                nativeFunction.DatumMethod.Invoke(ctx, receiver, args, ref result);
                return result;
            }
            var function = receiver.GetPropertyValue(ctx, name);
            return Invoke2(function, ctx, arg0, arg1);
        }

        /// <summary>Invokes a named three-argument method without allocating an argument array for native methods or closures.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ScriptDatum InvokeProperty3(ScriptObject receiver, ScriptContext ctx, string name, ScriptDatum arg0, ScriptDatum arg1, ScriptDatum arg2)
        {
            if (receiver.TryResolveProperty(name, out var property) &&
                property.Getter == null &&
                property.Value is BondingFunction { Target: null } nativeFunction)
            {
                DatumBuffer3 args = default;
                args[0] = arg0;
                args[1] = arg1;
                args[2] = arg2;
                ScriptDatum result = default;
                nativeFunction.DatumMethod.Invoke(ctx, receiver, args, ref result);
                return result;
            }
            var function = receiver.GetPropertyValue(ctx, name);
            return Invoke3(function, ctx, arg0, arg1, arg2);
        }

        /// <summary>Invokes a named four-argument method without allocating an argument array for native methods or closures.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ScriptDatum InvokeProperty4(ScriptObject receiver, ScriptContext ctx, string name, ScriptDatum arg0, ScriptDatum arg1, ScriptDatum arg2, ScriptDatum arg3)
        {
            if (receiver.TryResolveProperty(name, out var property) &&
                property.Getter == null &&
                property.Value is BondingFunction { Target: null } nativeFunction)
            {
                DatumBuffer4 args = default;
                args[0] = arg0;
                args[1] = arg1;
                args[2] = arg2;
                args[3] = arg3;
                ScriptDatum result = default;
                nativeFunction.DatumMethod.Invoke(ctx, receiver, args, ref result);
                return result;
            }
            var function = receiver.GetPropertyValue(ctx, name);
            return Invoke4(function, ctx, arg0, arg1, arg2, arg3);
        }

        /// <summary>Invokes a named five-argument method, using closure-specific fast paths when possible.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ScriptDatum InvokeProperty5(ScriptObject receiver, ScriptContext ctx, string name, ScriptDatum arg0, ScriptDatum arg1, ScriptDatum arg2, ScriptDatum arg3, ScriptDatum arg4)
        {
            if (receiver.TryResolveProperty(name, out var property) &&
                property.Getter == null &&
                property.Value is BondingFunction { Target: null } nativeFunction)
            {
                DatumBuffer5 args = default;
                args[0] = arg0;
                args[1] = arg1;
                args[2] = arg2;
                args[3] = arg3;
                args[4] = arg4;
                ScriptDatum result = default;
                nativeFunction.DatumMethod.Invoke(ctx, receiver, args, ref result);
                return result;
            }
            var function = receiver.GetPropertyValue(ctx, name);
            return Invoke5(function, ctx, arg0, arg1, arg2, arg3, arg4);
        }

        /// <summary>Invokes a named six-argument method, using closure-specific fast paths when possible.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ScriptDatum InvokeProperty6(ScriptObject receiver, ScriptContext ctx, string name, ScriptDatum arg0, ScriptDatum arg1, ScriptDatum arg2, ScriptDatum arg3, ScriptDatum arg4, ScriptDatum arg5)
        {
            if (receiver.TryResolveProperty(name, out var property) &&
                property.Getter == null &&
                property.Value is BondingFunction { Target: null } nativeFunction)
            {
                DatumBuffer6 args = default;
                args[0] = arg0;
                args[1] = arg1;
                args[2] = arg2;
                args[3] = arg3;
                args[4] = arg4;
                args[5] = arg5;
                ScriptDatum result = default;
                nativeFunction.DatumMethod.Invoke(ctx, receiver, args, ref result);
                return result;
            }
            var function = receiver.GetPropertyValue(ctx, name);
            return Invoke6(function, ctx, arg0, arg1, arg2, arg3, arg4, arg5);
        }

        /// <summary>Invokes a named seven-argument method, using closure-specific fast paths when possible.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ScriptDatum InvokeProperty7(ScriptObject receiver, ScriptContext ctx, string name, ScriptDatum arg0, ScriptDatum arg1, ScriptDatum arg2, ScriptDatum arg3, ScriptDatum arg4, ScriptDatum arg5, ScriptDatum arg6)
        {
            if (receiver.TryResolveProperty(name, out var property) &&
                property.Getter == null &&
                property.Value is BondingFunction { Target: null } nativeFunction)
            {
                DatumBuffer7 args = default;
                args[0] = arg0;
                args[1] = arg1;
                args[2] = arg2;
                args[3] = arg3;
                args[4] = arg4;
                args[5] = arg5;
                args[6] = arg6;
                ScriptDatum result = default;
                nativeFunction.DatumMethod.Invoke(ctx, receiver, args, ref result);
                return result;
            }
            var function = receiver.GetPropertyValue(ctx, name);
            return Invoke7(function, ctx, arg0, arg1, arg2, arg3, arg4, arg5, arg6);
        }

        /// <summary>Invokes a zero-argument script object, using closure-specific fast paths when possible.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ScriptDatum Invoke0(ScriptObject function, ScriptContext ctx)
        {
            if (function is ClosureFunction closure)
            {
                return closure.Invoke0(ctx);
            }
            return function.Invoke(ctx, Array.Empty<ScriptDatum>());
        }

        /// <summary>Invokes a one-argument script object, using closure-specific fast paths when possible.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ScriptDatum Invoke1(ScriptObject function, ScriptContext ctx, ScriptDatum arg0)
        {
            if (function is ClosureFunction closure)
            {
                return closure.Invoke1(ctx, arg0);
            }
            return function.Invoke(ctx, arg0);
        }

        /// <summary>Invokes a two-argument script object, using closure-specific fast paths when possible.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ScriptDatum Invoke2(ScriptObject function, ScriptContext ctx, ScriptDatum arg0, ScriptDatum arg1)
        {
            if (function is ClosureFunction closure)
            {
                return closure.Invoke2(ctx, arg0, arg1);
            }
            return function.Invoke(ctx, arg0, arg1);
        }

        /// <summary>Invokes a three-argument script object, using closure-specific fast paths when possible.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ScriptDatum Invoke3(ScriptObject function, ScriptContext ctx, ScriptDatum arg0, ScriptDatum arg1, ScriptDatum arg2)
        {
            if (function is ClosureFunction closure)
            {
                return closure.Invoke3(ctx, arg0, arg1, arg2);
            }
            return function.Invoke(ctx, arg0, arg1, arg2);
        }

        /// <summary>Invokes a four-argument script object, using closure-specific fast paths when possible.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ScriptDatum Invoke4(ScriptObject function, ScriptContext ctx, ScriptDatum arg0, ScriptDatum arg1, ScriptDatum arg2, ScriptDatum arg3)
        {
            if (function is ClosureFunction closure)
            {
                return closure.Invoke4(ctx, arg0, arg1, arg2, arg3);
            }
            return function.Invoke(ctx, arg0, arg1, arg2, arg3);
        }

        /// <summary>Invokes a five-argument script object, using closure-specific fast paths when possible.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ScriptDatum Invoke5(ScriptObject function, ScriptContext ctx, ScriptDatum arg0, ScriptDatum arg1, ScriptDatum arg2, ScriptDatum arg3, ScriptDatum arg4)
        {
            if (function is ClosureFunction closure)
            {
                return closure.Invoke5(ctx, arg0, arg1, arg2, arg3, arg4);
            }
            return function.Invoke(ctx, arg0, arg1, arg2, arg3, arg4);
        }

        /// <summary>Invokes a six-argument script object, using closure-specific fast paths when possible.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ScriptDatum Invoke6(ScriptObject function, ScriptContext ctx, ScriptDatum arg0, ScriptDatum arg1, ScriptDatum arg2, ScriptDatum arg3, ScriptDatum arg4, ScriptDatum arg5)
        {
            if (function is ClosureFunction closure)
            {
                return closure.Invoke6(ctx, arg0, arg1, arg2, arg3, arg4, arg5);
            }
            return function.Invoke(ctx, arg0, arg1, arg2, arg3, arg4, arg5);
        }

        /// <summary>Invokes a seven-argument script object, using closure-specific fast paths when possible.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ScriptDatum Invoke7(ScriptObject function, ScriptContext ctx, ScriptDatum arg0, ScriptDatum arg1, ScriptDatum arg2, ScriptDatum arg3, ScriptDatum arg4, ScriptDatum arg5, ScriptDatum arg6)
        {
            if (function is ClosureFunction closure)
            {
                return closure.Invoke7(ctx, arg0, arg1, arg2, arg3, arg4, arg5, arg6);
            }
            return function.Invoke(ctx, arg0, arg1, arg2, arg3, arg4, arg5, arg6);
        }

        /// <summary>Creates a direct-call context for a known module-local function.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ScriptContext EnterDirect(ScriptContext ctx, string name)
        {
            var closure = (ClosureFunction)ScriptDatum.ToObject(ctx.Module.GetPropertyDatum(ctx, name));
            return ctx.With(ctx.Module, closure);
        }

        /// <summary>Creates a direct-call context for a cached module-local function.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ScriptContext EnterDirect(ScriptContext ctx, ClosureFunction closure)
        {
            return ctx.With(closure);
        }

        /// <summary>Releases a direct-call context and returns the script result.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ScriptDatum LeaveDirect(ScriptContext context, ScriptDatum result)
        {
            return ReturnDirect(context, result);
        }

        private static ScriptDatum ReturnDirect(ScriptContext context, ScriptDatum result)
        {
            while (context.Next != null)
            {
                context.Next.ReleaseLinked();
            }
            context.Release();
            return result;
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
            // string  has special behavior for 'in' operator, it checks if the substring exists in the string
            if (collection is StringValue stringValue && ScriptDatum.TryGetString(in value, out var str) && str.Value.Length > 1)
            {
                return stringValue.Value.IndexOf(str.Value) > -1;
            }
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
                for (var i = 0; i < source.Length; i++)
                {
                    array.Push(source._items[i]);
                }
            }
            else
            {
                array.Push(ScriptDatum.FromObject(val));
            }
        }

        /// <summary>
        /// Spreads an object's elements or properties into a list.
        /// Optimized for the spread operator (...) in function calls.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void SpreadIntoList(List<ScriptDatum> list, ScriptObject val)
        {
            if (val is ScriptArray source)
            {
                for (var i = 0; i < source.Length; i++)
                {
                    list.Add(source._items[i]);
                }
            }
            else
            {
                list.Add(ScriptDatum.FromObject(val));
            }
        }

        /// <summary>
        /// Creates a new instance of a script type with the given arguments.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ScriptDatum New(ScriptObject type, ScriptContext ctx, Span<ScriptDatum> args)
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

        /// <summary>Creates a new script object with no constructor arguments.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ScriptDatum New0(ScriptObject type, ScriptContext ctx)
        {
            if (type is ScriptType typed)
            {
                ScriptDatum result = default;
                typed.Construct(ctx, Span<ScriptDatum>.Empty, ref result);
                return result;
            }
            ThrowHelper.ThrowNotConstructor(type?.ToString() ?? "null");
            return default;
        }

        /// <summary>Creates a new script object with one constructor argument.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ScriptDatum New1(ScriptObject type, ScriptContext ctx, ScriptDatum arg0)
        {
            if (type is ScriptType typed)
            {
                ScriptDatum result = default;
                DatumBuffer1 args = default;
                args[0] = arg0;
                typed.Construct(ctx, args, ref result);
                return result;
            }
            ThrowHelper.ThrowNotConstructor(type?.ToString() ?? "null");
            return default;
        }

        /// <summary>Creates a new script object with two constructor arguments.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ScriptDatum New2(ScriptObject type, ScriptContext ctx, ScriptDatum arg0, ScriptDatum arg1)
        {
            if (type is ScriptType typed)
            {
                ScriptDatum result = default;
                DatumBuffer2 args = default;
                args[0] = arg0;
                args[1] = arg1;
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
        public static ScriptDatum TryGetArg(Span<ScriptDatum> args, int index, ScriptDatum defaultValue)
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
        public static ScriptDatum GetArg(Span<ScriptDatum> args, int index)
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
        /// Resolves a compiled zero-argument method delegate by its ID from the global registry.
        /// </summary>
        /// <param name="module">The module context (currently unused in resolution).</param>
        /// <param name="id">The unique ID of the compiled method.</param>
        /// <returns>The resolved zero-argument script function delegate.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ScriptFunctionDelegate0 ResolveDelegate0(ScriptModule module, int id)
        {
            return DynamicMethodRegistry.Resolve0(id);
        }

        /// <summary>
        /// Resolves a compiled one-argument method delegate by its ID from the global registry.
        /// </summary>
        /// <param name="module">The module context (currently unused in resolution).</param>
        /// <param name="id">The unique ID of the compiled method.</param>
        /// <returns>The resolved one-argument script function delegate.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ScriptFunctionDelegate1 ResolveDelegate1(ScriptModule module, int id)
        {
            return DynamicMethodRegistry.Resolve1(id);
        }

        /// <summary>
        /// Resolves a compiled two-argument method delegate by its ID from the global registry.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ScriptFunctionDelegate2 ResolveDelegate2(ScriptModule module, int id)
        {
            return DynamicMethodRegistry.Resolve2(id);
        }

        /// <summary>
        /// Resolves a compiled three-argument method delegate by its ID from the global registry.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ScriptFunctionDelegate3 ResolveDelegate3(ScriptModule module, int id)
        {
            return DynamicMethodRegistry.Resolve3(id);
        }

        /// <summary>
        /// Resolves a compiled four-argument method delegate by its ID from the global registry.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ScriptFunctionDelegate4 ResolveDelegate4(ScriptModule module, int id)
        {
            return DynamicMethodRegistry.Resolve4(id);
        }

        /// <summary>
        /// Resolves a compiled five-argument method delegate by its ID from the global registry.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ScriptFunctionDelegate5 ResolveDelegate5(ScriptModule module, int id)
        {
            return DynamicMethodRegistry.Resolve5(id);
        }

        /// <summary>
        /// Resolves a compiled six-argument method delegate by its ID from the global registry.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ScriptFunctionDelegate6 ResolveDelegate6(ScriptModule module, int id)
        {
            return DynamicMethodRegistry.Resolve6(id);
        }

        /// <summary>
        /// Resolves a compiled seven-argument method delegate by its ID from the global registry.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ScriptFunctionDelegate7 ResolveDelegate7(ScriptModule module, int id)
        {
            return DynamicMethodRegistry.Resolve7(id);
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
