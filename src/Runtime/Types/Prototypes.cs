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

            // --- Fixed-length primitive arrays ---
            ScriptPackedArrayPrototype.Define("length", ScriptDatum.FromBondingGetter(ScriptPackedArray.LENGTH), writeable: false, enumerable: false);
            ScriptPackedArrayPrototype.Define("fill", ScriptDatum.FromBonding(ScriptPackedArray.FILL), writeable: false, enumerable: false);
            ScriptPackedArrayPrototype.Frozen();

        }
    }
}
