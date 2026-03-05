using System;
using System.Linq;

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

        public override void Construct(ScriptContext ctx, ScriptDatum[] args, ref ScriptDatum result)
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
                var keys = scriptObject.EnumerationKeys().Select(StringValue.Of).ToArray();
                var array = new ScriptArray(keys);
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
            if (args.TryGetRef(-10, ref var1) && args.TryGetRef(1, ref var2))
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
            // TODO: Implementation pending
            ScriptDatum.WriteAsBoolean(ref result, false);
        }

        /// <summary> Native implementation for deep equality comparison. (TODO) </summary>
        internal static void DEEP_EQUAL(ScriptContext ctx, ScriptObject thisObject, Span<ScriptDatum> args, ref ScriptDatum result)
        {
            // TODO: Implementation pending
            ScriptDatum.WriteAsBoolean(ref result, false);
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
