using AuroraScript.Runtime.Types.TypeConstruct;

namespace AuroraScript.Runtime.Types
{
    /// <summary>
    /// Provides global access to built-in prototypes and constructors for script types.
    /// This class initializes the default behavior and methods for all script-side objects and primitives.
    /// </summary>
    internal static class Prototypes
    {
        /// <summary> The base prototype for all objects. </summary>
        public static readonly ScriptObject ObjectPrototype = new ScriptObject(null);
        /// <summary> The prototype for callable objects (functions). </summary>
        public static readonly ScriptObject CallablePrototype = new ScriptObject(Prototypes.ObjectPrototype);
        /// <summary> The prototype for null values. </summary>
        public static readonly ScriptObject NullValuePrototype = new ScriptObject(null);
        /// <summary> The prototype for script arrays. </summary>
        public static readonly ScriptObject ScriptArrayPrototype = new ScriptObject(Prototypes.ObjectPrototype);
        /// <summary> The shared prototype for fixed-length primitive arrays. </summary>
        public static readonly ScriptObject ScriptPackedArrayPrototype = new ScriptObject(Prototypes.ObjectPrototype);
        /// <summary>
        /// Forces pre-loading of prototypes.
        /// </summary>
        internal static void Preload()
        {
        }

        static Prototypes()
        {
            // --- ScriptObject ---
            ObjectPrototype.Define("toString", ScriptDatum.FromBonding(ScriptObject.TOSTRING), writeable: false, enumerable: false);
            ObjectPrototype.Define("length", ScriptDatum.FromBondingGetter(ScriptObject.LENGTH), writeable: false, enumerable: false);
            ObjectPrototype.Frozen();

            // --- Callable ---
            CallablePrototype.Frozen();

            // --- Null ---
            NullValuePrototype.Define("toString", ScriptDatum.FromBonding(NullValue.TOSTRING), writeable: false, enumerable: false);
            NullValuePrototype.Frozen();

            // --- Array ---
            ScriptArrayPrototype.Define("has", ScriptDatum.FromBonding(ScriptArray.HAS), writeable: false, enumerable: false);
            ScriptArrayPrototype.Define("indexOf", ScriptDatum.FromBonding(ScriptArray.INDEXOF), writeable: false, enumerable: false);
            ScriptArrayPrototype.Define("lastIndexOf", ScriptDatum.FromBonding(ScriptArray.LASTINDEXOF), writeable: false, enumerable: false);
            ScriptArrayPrototype.Define("length", ScriptDatum.FromBondingAccessor(ScriptArray.GET_LENGTH, ScriptArray.SET_LENGTH), writeable: false, enumerable: false);
            ScriptArrayPrototype.Define("push", ScriptDatum.FromBonding(ScriptArray.PUSH), writeable: false, enumerable: false);
            ScriptArrayPrototype.Define("pop", ScriptDatum.FromBonding(ScriptArray.POP), writeable: false, enumerable: false);
            ScriptArrayPrototype.Define("sort", ScriptDatum.FromBonding(ScriptArray.SORT), writeable: false, enumerable: false);
            ScriptArrayPrototype.Define("join", ScriptDatum.FromBonding(ScriptArray.JOIN), writeable: false, enumerable: false);
            ScriptArrayPrototype.Define("slice", ScriptDatum.FromBonding(ScriptArray.SLICE), writeable: false, enumerable: false);
            ScriptArrayPrototype.Define("reverse", ScriptDatum.FromBonding(ScriptArray.REVERSE), writeable: false, enumerable: false);
            ScriptArrayPrototype.Define("unshift", ScriptDatum.FromBonding(ScriptArray.UNSHIFT), writeable: false, enumerable: false);
            ScriptArrayPrototype.Define("shift", ScriptDatum.FromBonding(ScriptArray.SHIFT), writeable: false, enumerable: false);
            ScriptArrayPrototype.Define("concat", ScriptDatum.FromBonding(ScriptArray.CONCAT), writeable: false, enumerable: false);
            ScriptArrayPrototype.Define("find", ScriptDatum.FromBonding(ScriptArray.FIND), writeable: false, enumerable: false);
            ScriptArrayPrototype.Define("findIndex", ScriptDatum.FromBonding(ScriptArray.FINDINDEX), writeable: false, enumerable: false);
            ScriptArrayPrototype.Define("findLast", ScriptDatum.FromBonding(ScriptArray.FINDLAST), writeable: false, enumerable: false);
            ScriptArrayPrototype.Define("findLastIndex", ScriptDatum.FromBonding(ScriptArray.FINDLASTINDEX), writeable: false, enumerable: false);
            ScriptArrayPrototype.Define("map", ScriptDatum.FromBonding(ScriptArray.MAP), writeable: false, enumerable: false);
            ScriptArrayPrototype.Define("filter", ScriptDatum.FromBonding(ScriptArray.FILTER), writeable: false, enumerable: false);
            ScriptArrayPrototype.Define("some", ScriptDatum.FromBonding(ScriptArray.SOME), writeable: false, enumerable: false);
            ScriptArrayPrototype.Define("every", ScriptDatum.FromBonding(ScriptArray.EVERY), writeable: false, enumerable: false);
            ScriptArrayPrototype.Define("flat", ScriptDatum.FromBonding(ScriptArray.FLAT), writeable: false, enumerable: false);
            ScriptArrayPrototype.Define("reduce", ScriptDatum.FromBonding(ScriptArray.REDUCE), writeable: false, enumerable: false);
            ScriptArrayPrototype.Frozen();

            // --- Fixed-length primitive arrays ---
            ScriptPackedArrayPrototype.Define("length", ScriptDatum.FromBondingGetter(ScriptPackedArray.LENGTH), writeable: false, enumerable: false);
            ScriptPackedArrayPrototype.Define("fill", ScriptDatum.FromBonding(ScriptPackedArray.FILL), writeable: false, enumerable: false);
            ScriptPackedArrayPrototype.Frozen();

        }
    }
}
