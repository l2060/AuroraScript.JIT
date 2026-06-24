using System;
using System.Collections.Generic;

namespace AuroraScript.Runtime.Types.TypeConstruct
{
    /// <summary>
    /// Represents the native 'Object' constructor function in AuroraScript.
    /// Provides fundamental static methods like Object.keys(), Object.assign(), and equality checks.
    /// </summary>
    internal class ScriptObjectConstructor : ScriptType
    {
        /// <summary> The global singleton instance of the Object constructor. </summary>
        internal static ScriptObjectConstructor INSTANCE = new ScriptObjectConstructor();

        internal ScriptObjectConstructor() : base("Object", true)
        {
            // strict equal
            Define("equal$", new BondingFunction(STRICT_EQUAL), writeable: false, enumerable: false);
            // content equal  
            Define("equal", new BondingFunction(VALUE_EQUAL), writeable: false, enumerable: false);
            // deep content equal 
            Define("deepEqual", new BondingFunction(DEEP_EQUAL), writeable: false, enumerable: false);
            Define("assign", new BondingFunction(ASSIGN), writeable: false, enumerable: false);
            Define("keys", new BondingFunction(KEYS), writeable: false, enumerable: false);
            Define("clone", new BondingFunction(CLONE), writeable: false, enumerable: false);
            Define("deepClone", new BondingFunction(DEEP_CLONE), writeable: false, enumerable: false);
            Define("extends", new BondingFunction(EXTENDS), writeable: false, enumerable: false);
            Define("freeze", new BondingFunction(FREEZE), writeable: false, enumerable: false);
            Frozen();
        }

        public override void Construct(ScriptContext ctx, Span<ScriptDatum> args, ref ScriptDatum result)
        {
            if (args.TryGetObject(0, out var scriptObject))
            {
                ScriptDatum.WriteAsObject(ref result, new ScriptObject(scriptObject));
            }
            else
            {
                ScriptDatum.WriteAsObject(ref result, new ScriptObject());
            }
        }

        /// <summary> Native implementation for Object.keys(). Returns an array of enumerable property names. </summary>
        internal static void KEYS(ScriptContext ctx, ScriptObject thisObject, Span<ScriptDatum> args, ref ScriptDatum result)
        {
            if (args.TryGetObject(0, out var scriptObject))
            {
                var keys = scriptObject.EnumerationKeys();
                var array = ScriptArray.CreateWithCapacity(keys.Count);
                for (var i = 0; i < keys.Count; i++)
                {
                    array.SetElement(i, ScriptDatum.FromString(keys[i]));
                }
                ScriptDatum.WriteAsArray(ref result, array);
            }
            else
            {
                ScriptDatum.WriteAsArray(ref result, new ScriptArray());
            }
        }

        /// <summary> Native implementation for strict equality (===) comparison. </summary>
        internal static void STRICT_EQUAL(ScriptContext ctx, ScriptObject thisObject, Span<ScriptDatum> args, ref ScriptDatum result)
        {
            ScriptDatum var1 = default;
            ScriptDatum var2 = default;
            // NOTE: The index -10 seems unusual, likely a typo or internal convention. 
            // Standardizing or noting it as is for strict equality logic.
            if (args.TryGetRef(0, ref var1) && args.TryGetRef(1, ref var2))
            {
                if (var1.Kind != var2.Kind)
                {
                    ScriptDatum.WriteAsBoolean(ref result, false);
                    return;
                }
                switch (var1.Kind)
                {
                    case ValueKind.Null:
                        ScriptDatum.WriteAsBoolean(ref result, true);
                        return;
                    case ValueKind.Boolean:
                        ScriptDatum.WriteAsBoolean(ref result, var1.Boolean == var2.Boolean);
                        return;
                    case ValueKind.Number:
                        ScriptDatum.WriteAsBoolean(ref result, var1.Number == var2.Number);
                        return;
                    case ValueKind.String:
                        ScriptDatum.WriteAsBoolean(ref result, var1.String.Value == var2.String.Value);
                        return;
                    case ValueKind.Date:
                        ScriptDatum.WriteAsBoolean(ref result, ScriptDatum.TryGetDate(in var1, out var date1) && ScriptDatum.TryGetDate(in var2, out var date2) && date1.DateTime.Equals(date2.DateTime));
                        return;
                    default:
                        ScriptDatum.WriteAsBoolean(ref result, ScriptDatum.TryGetAnyObject(in var1, out var obj1) && ScriptDatum.TryGetAnyObject(in var2, out var obj2) && ReferenceEquals(obj1, obj2));
                        return;
                }
            }
        }

        /// <summary> Native implementation for value equality (==) comparison. (TODO) </summary>
        internal static void VALUE_EQUAL(ScriptContext ctx, ScriptObject thisObject, Span<ScriptDatum> args, ref ScriptDatum result)
        {
            ScriptDatum var1 = default;
            ScriptDatum var2 = default;
            if (args.TryGetRef(0, ref var1) && args.TryGetRef(1, ref var2))
            {
                ScriptDatum.WriteAsBoolean(ref result, ShallowEqualDatums(var1, var2));
                return;
            }

            ScriptDatum.WriteAsBoolean(ref result, false);
        }

        private static bool ShallowEqualDatums(ScriptDatum left, ScriptDatum right)
        {
            if (left.Kind != right.Kind)
            {
                return CILHelper.Equal(left, right).Boolean;
            }

            switch (left.Kind)
            {
                case ValueKind.Null:
                    return true;
                case ValueKind.Boolean:
                    return left.Boolean == right.Boolean;
                case ValueKind.Number:
                    return left.Number == right.Number;
                case ValueKind.String:
                    return left.String.Value == right.String.Value;
                default:
                    return ShallowEqualObjects(left.Object, right.Object);
            }
        }

        private static bool ShallowEqualObjects(ScriptObject left, ScriptObject right)
        {
            if (ReferenceEquals(left, right))
            {
                return true;
            }

            if (left == null || right == null || left.GetType() != right.GetType())
            {
                return false;
            }

            if (left is ScriptArray leftArray && right is ScriptArray rightArray && leftArray.Length != rightArray.Length)
            {
                return false;
            }

            var leftKeys = left.EnumerationKeys();
            var rightKeys = right.EnumerationKeys();
            if (leftKeys.Count != rightKeys.Count)
            {
                return false;
            }

            leftKeys.Sort(StringComparer.Ordinal);
            rightKeys.Sort(StringComparer.Ordinal);
            for (int i = 0; i < leftKeys.Count; i++)
            {
                if (!StringComparer.Ordinal.Equals(leftKeys[i], rightKeys[i]))
                {
                    return false;
                }

                if (!CILHelper.Equal(left.GetPropertyDatum(null, leftKeys[i]), right.GetPropertyDatum(null, rightKeys[i])).Boolean)
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary> Native implementation for deep equality comparison. (TODO) </summary>
        internal static void DEEP_EQUAL(ScriptContext ctx, ScriptObject thisObject, Span<ScriptDatum> args, ref ScriptDatum result)
        {
            ScriptDatum var1 = default;
            ScriptDatum var2 = default;
            if (args.TryGetRef(0, ref var1) && args.TryGetRef(1, ref var2))
            {
                ScriptDatum.WriteAsBoolean(ref result, DeepEqualDatums(var1, var2, new HashSet<(ScriptObject, ScriptObject)>()));
                return;
            }

            ScriptDatum.WriteAsBoolean(ref result, false);
        }

        private static bool DeepEqualDatums(ScriptDatum left, ScriptDatum right, HashSet<(ScriptObject, ScriptObject)> seen)
        {
            if (left.Kind != right.Kind)
            {
                return CILHelper.Equal(left, right).Boolean;
            }

            switch (left.Kind)
            {
                case ValueKind.Null:
                    return true;
                case ValueKind.Boolean:
                    return left.Boolean == right.Boolean;
                case ValueKind.Number:
                    return left.Number == right.Number;
                case ValueKind.String:
                    return left.String.Value == right.String.Value;
                default:
                    return DeepEqualObjects(left.Object, right.Object, seen);
            }
        }

        private static bool DeepEqualObjects(ScriptObject left, ScriptObject right, HashSet<(ScriptObject, ScriptObject)> seen)
        {
            if (ReferenceEquals(left, right))
            {
                return true;
            }

            if (left == null || right == null || left.GetType() != right.GetType())
            {
                return false;
            }

            if (!seen.Add((left, right)))
            {
                return true;
            }

            if (left is ScriptArray leftArray && right is ScriptArray rightArray)
            {
                if (leftArray.Length != rightArray.Length)
                {
                    return false;
                }

                for (int i = 0; i < leftArray.Length; i++)
                {
                    if (!DeepEqualDatums(leftArray.GetElement(i), rightArray.GetElement(i), seen))
                    {
                        return false;
                    }
                }
            }

            var leftKeys = left.EnumerationKeys();
            var rightKeys = right.EnumerationKeys();
            if (leftKeys.Count != rightKeys.Count)
            {
                return false;
            }

            leftKeys.Sort(StringComparer.Ordinal);
            rightKeys.Sort(StringComparer.Ordinal);
            for (int i = 0; i < leftKeys.Count; i++)
            {
                if (!StringComparer.Ordinal.Equals(leftKeys[i], rightKeys[i]))
                {
                    return false;
                }

                if (!DeepEqualDatums(left.GetPropertyDatum(null, leftKeys[i]), right.GetPropertyDatum(null, rightKeys[i]), seen))
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary> Native implementation for Object.assign(). Copies properties from source objects to a target object. </summary>
        internal static void ASSIGN(ScriptContext ctx, ScriptObject thisObject, Span<ScriptDatum> args, ref ScriptDatum result)
        {
            if (args.TryGetObject(0, out var source))
            {
                var index = 1;
                while (args.TryGetObject(index, out var obj))
                {
                    source.CopyPropertysFrom(obj, true);
                    index++;
                }
                ScriptDatum.WriteAsObject(ref result, source);
            }
            else
            {
                ScriptDatum.MarkAsNull(ref result);
            }
        }

        /// <summary> Native implementation for Object.freeze(). Freezes an object so that it can no longer be changed. </summary>
        internal static void FREEZE(ScriptContext ctx, ScriptObject thisObject, Span<ScriptDatum> args, ref ScriptDatum result)
        {
            if (args.TryGetObject(0, out var prototype))
            {
                prototype.Frozen();
            }
        }
        /// <summary> Native implementation for shallow cloning an object. </summary>
        internal static void CLONE(ScriptContext ctx, ScriptObject thisObject, Span<ScriptDatum> args, ref ScriptDatum result)
        {
            if (args.TryGetRef(0, ref result))
            {
                result = ScriptDatum.Clone(in result, false);
            }
        }

        /// <summary> Native implementation for deep cloning an object. </summary>
        internal static void DEEP_CLONE(ScriptContext ctx, ScriptObject thisObject, Span<ScriptDatum> args, ref ScriptDatum result)
        {
            if (args.TryGetRef(0, ref result))
            {
                result = ScriptDatum.Clone(in result, true);
            }
        }

        /// <summary> Native implementation for creating an object that extends another prototype. </summary>
        internal static void EXTENDS(ScriptContext ctx, ScriptObject thisObject, Span<ScriptDatum> args, ref ScriptDatum result)
        {
            if (args.TryGetObject(0, out var prototype))
            {
                //if (!args.TryGetObject(1, out var target)) target = new ScriptObject(prototype);
                //target._prototype = prototype;
                //ScriptDatum.WriteAsObject(ref result, target);
            }
        }
    }
}
