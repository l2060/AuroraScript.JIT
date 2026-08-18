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
        /// <summary> The prototype for boolean primitive values. </summary>
        public static readonly ScriptObject BooleanValuePrototype = new ScriptObject(ObjectPrototype);
        /// <summary> The prototype for callable objects (functions). </summary>
        public static readonly ScriptObject CallablePrototype = new ScriptObject(Prototypes.ObjectPrototype);
        /// <summary> The prototype for null values. </summary>
        public static readonly ScriptObject NullValuePrototype = new ScriptObject(null);
        /// <summary> The prototype for number primitive values. </summary>
        public static readonly ScriptObject NumberValuePrototype = new ScriptObject(Prototypes.ObjectPrototype);
        /// <summary> The prototype for script arrays. </summary>
        public static readonly ScriptObject ScriptArrayPrototype = new ScriptObject(Prototypes.ObjectPrototype);
        /// <summary> The shared prototype for fixed-length primitive arrays. </summary>
        public static readonly ScriptObject ScriptPackedArrayPrototype = new ScriptObject(Prototypes.ObjectPrototype);
        /// <summary> The prototype for string primitive values. </summary>
        public static readonly ScriptObject StringValuePrototype = new ScriptObject(Prototypes.ObjectPrototype);
        /// <summary> The prototype for regular expression objects. </summary>
        public static readonly ScriptObject RegexPrototype = new ScriptObject(Prototypes.ObjectPrototype);
        /// <summary> The prototype for date objects. </summary>
        public static readonly ScriptObject DatePrototype = new ScriptObject(Prototypes.ObjectPrototype);
        /// <summary> The prototype for hash map objects. </summary>
        public static readonly ScriptObject HashMapPrototype = new ScriptObject(Prototypes.ObjectPrototype);
        /// <summary> The prototype for hash StringBuffer objects. </summary>
        public static readonly ScriptObject StringBufferPrototype = new ScriptObject(Prototypes.ObjectPrototype);
        /// <summary> The prototype for path value objects. </summary>
        public static readonly ScriptObject PathPrototype = new ScriptObject(Prototypes.ObjectPrototype);
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

            // --- Boolean ---
            BooleanValuePrototype.Define("toString", ScriptDatum.FromBonding(BooleanValue.TOSTRING), writeable: false, enumerable: false);
            BooleanValuePrototype.Frozen();

            // --- Regex ---
            RegexPrototype.Define("test", ScriptDatum.FromBonding(ScriptRegex.TEST), writeable: false, enumerable: false);
            RegexPrototype.Frozen();

            // --- HashMap ---
            HashMapPrototype.Define("has", ScriptDatum.FromBonding(ScriptHashMap.HAS), writeable: false, enumerable: false);
            HashMapPrototype.Define("set", ScriptDatum.FromBonding(ScriptHashMap.SET), writeable: false, enumerable: false);
            HashMapPrototype.Define("get", ScriptDatum.FromBonding(ScriptHashMap.GET), writeable: false, enumerable: false);
            HashMapPrototype.Define("getOrInsert", ScriptDatum.FromBonding(ScriptHashMap.OGETORINSERT), writeable: false, enumerable: false);
            HashMapPrototype.Define("delete", ScriptDatum.FromBonding(ScriptHashMap.DELETE), writeable: false, enumerable: false);
            HashMapPrototype.Define("clear", ScriptDatum.FromBonding(ScriptHashMap.CLEAR), writeable: false, enumerable: false);
            HashMapPrototype.Define("keys", ScriptDatum.FromBondingGetter(ScriptHashMap.KEYS), writeable: false, enumerable: false);
            HashMapPrototype.Define("values", ScriptDatum.FromBondingGetter(ScriptHashMap.VALUES), writeable: false, enumerable: false);
            HashMapPrototype.Define("size", ScriptDatum.FromBondingGetter(ScriptHashMap.SIZE), writeable: false, enumerable: false);
            HashMapPrototype.Frozen();

            // --- DATE ---
            DatePrototype.Define("year", ScriptDatum.FromBondingGetter(ScriptDate.YEAR), writeable: false, enumerable: false);
            DatePrototype.Define("month", ScriptDatum.FromBondingGetter(ScriptDate.MONTH), writeable: false, enumerable: false);
            DatePrototype.Define("day", ScriptDatum.FromBondingGetter(ScriptDate.DAY), writeable: false, enumerable: false);
            DatePrototype.Define("hour", ScriptDatum.FromBondingGetter(ScriptDate.HOUR), writeable: false, enumerable: false);
            DatePrototype.Define("minute", ScriptDatum.FromBondingGetter(ScriptDate.MINUTE), writeable: false, enumerable: false);
            DatePrototype.Define("second", ScriptDatum.FromBondingGetter(ScriptDate.SECOND), writeable: false, enumerable: false);
            DatePrototype.Define("millisecond", ScriptDatum.FromBondingGetter(ScriptDate.MILLISECCOND), writeable: false, enumerable: false);
            DatePrototype.Define("dayOfWeek", ScriptDatum.FromBondingGetter(ScriptDate.DAYOFWEEK), writeable: false, enumerable: false);
            DatePrototype.Define("dayOfYear", ScriptDatum.FromBondingGetter(ScriptDate.DAYOFYEAR), writeable: false, enumerable: false);
            DatePrototype.Define("ticks", ScriptDatum.FromBondingGetter(ScriptDate.TICKS), writeable: false, enumerable: false);
            DatePrototype.Define("toString", ScriptDatum.FromBonding(ScriptDateConstructor.TOSTRING), writeable: false, enumerable: false);
            DatePrototype.Frozen();

            // --- Callable ---
            CallablePrototype.Frozen();

            // --- Null ---
            NullValuePrototype.Define("toString", ScriptDatum.FromBonding(NullValue.TOSTRING), writeable: false, enumerable: false);
            NullValuePrototype.Frozen();

            // --- Number ---
            NumberValuePrototype.Define("toString", ScriptDatum.FromBonding(NumberValue.TOSTRING), writeable: false, enumerable: false);
            NumberValuePrototype.Frozen();

            // --- Array ---
            ScriptArrayPrototype.Define("has", ScriptDatum.FromBonding(ScriptArray.HAS), writeable: false, enumerable: false);
            ScriptArrayPrototype.Define("indexOf", ScriptDatum.FromBonding(ScriptArray.INDEXOF), writeable: false, enumerable: false);
            ScriptArrayPrototype.Define("lastIndexOf", ScriptDatum.FromBonding(ScriptArray.LASTINDEXOF), writeable: false, enumerable: false);
            ScriptArrayPrototype.Define("length", ScriptDatum.FromBondingGetter(ScriptArray.LENGTH), writeable: false, enumerable: false);
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



            // --- String ---
            StringValuePrototype.Define("length", ScriptDatum.FromBondingGetter(StringValue.LENGTH), writeable: false, enumerable: false);
            StringValuePrototype.Define("contains", ScriptDatum.FromBonding(StringValue.CONTANINS), writeable: false, enumerable: false);
            StringValuePrototype.Define("indexOf", ScriptDatum.FromBonding(StringValue.INDEXOF), writeable: false, enumerable: false);
            StringValuePrototype.Define("lastIndexOf", ScriptDatum.FromBonding(StringValue.LASTINDEXOF), writeable: false, enumerable: false);
            StringValuePrototype.Define("startsWith", ScriptDatum.FromBonding(StringValue.STARTSWITH), writeable: false, enumerable: false);
            StringValuePrototype.Define("endsWith", ScriptDatum.FromBonding(StringValue.ENDSWITH), writeable: false, enumerable: false);
            StringValuePrototype.Define("substring", ScriptDatum.FromBonding(StringValue.SUBSTRING), writeable: false, enumerable: false);
            StringValuePrototype.Define("split", ScriptDatum.FromBonding(StringValue.SPLIT), writeable: false, enumerable: false);
            StringValuePrototype.Define("match", ScriptDatum.FromBonding(StringValue.MATCH), writeable: false, enumerable: false);
            StringValuePrototype.Define("matchAll", ScriptDatum.FromBonding(StringValue.MATCHALL), writeable: false, enumerable: false);
            StringValuePrototype.Define("replace", ScriptDatum.FromBonding(StringValue.REPLACE), writeable: false, enumerable: false);
            StringValuePrototype.Define("padLeft", ScriptDatum.FromBonding(StringValue.PADLEFT), writeable: false, enumerable: false);
            StringValuePrototype.Define("padRight", ScriptDatum.FromBonding(StringValue.PADRIGHT), writeable: false, enumerable: false);
            StringValuePrototype.Define("trim", ScriptDatum.FromBonding(StringValue.TRIM), writeable: false, enumerable: false);
            StringValuePrototype.Define("trimLeft", ScriptDatum.FromBonding(StringValue.TRIMLEFT), writeable: false, enumerable: false);
            StringValuePrototype.Define("trimRight", ScriptDatum.FromBonding(StringValue.TRIMRIGHT), writeable: false, enumerable: false);
            StringValuePrototype.Define("slice", ScriptDatum.FromBonding(StringValue.SUBSTRING), writeable: false, enumerable: false);
            StringValuePrototype.Define("toString", ScriptDatum.FromBonding(StringValue.TOSTRING), writeable: false, enumerable: false);
            StringValuePrototype.Define("charCodeAt", ScriptDatum.FromBonding(StringValue.CHARCODEAT), writeable: false, enumerable: false);
            StringValuePrototype.Define("toLowerCase", ScriptDatum.FromBonding(StringValue.TOLOWERCASE), writeable: false, enumerable: false);
            StringValuePrototype.Define("toUpperCase", ScriptDatum.FromBonding(StringValue.TOUPPERCASE), writeable: false, enumerable: false);
            StringValuePrototype.Frozen();




            // --- StringBuffer ---
            StringBufferPrototype.Define("toString", ScriptDatum.FromBonding(StringBuffer.TO_STRING), writeable: false, enumerable: false);
            StringBufferPrototype.Define("append", ScriptDatum.FromBonding(StringBuffer.APPEND), writeable: false, enumerable: false);
            StringBufferPrototype.Define("insert", ScriptDatum.FromBonding(StringBuffer.INSERT), writeable: false, enumerable: false);
            StringBufferPrototype.Define("appendLine", ScriptDatum.FromBonding(StringBuffer.APPEND_LINE), writeable: false, enumerable: false);
            StringBufferPrototype.Define("clear", ScriptDatum.FromBonding(StringBuffer.CLEAR), writeable: false, enumerable: false);
            StringBufferPrototype.Define("release", ScriptDatum.FromBonding(StringBuffer.RELEASE), writeable: false, enumerable: false);
            StringBufferPrototype.Define("stringAndRelease", ScriptDatum.FromBonding(StringBuffer.STRINGANDRELEASE), writeable: false, enumerable: false);
            StringBufferPrototype.Frozen();

            // --- Path ---
            PathPrototype.Define("toString", ScriptDatum.FromBonding(ScriptPathValue.TO_STRING), writeable: false, enumerable: false);
            PathPrototype.Define("append", ScriptDatum.FromBonding(ScriptPathValue.APPEND), writeable: false, enumerable: false);
            PathPrototype.Define("reset", ScriptDatum.FromBonding(ScriptPathValue.RESET), writeable: false, enumerable: false);
            PathPrototype.Define("changeExt", ScriptDatum.FromBonding(ScriptPathValue.CHANGE_EXT), writeable: false, enumerable: false);
            PathPrototype.Define("directoryName", ScriptDatum.FromBonding(ScriptPathValue.DIRECTORY_NAME), writeable: false, enumerable: false);
            PathPrototype.Define("fileName", ScriptDatum.FromBonding(ScriptPathValue.FILE_NAME), writeable: false, enumerable: false);
            PathPrototype.Define("extName", ScriptDatum.FromBonding(ScriptPathValue.EXT_NAME), writeable: false, enumerable: false);
            PathPrototype.Define("protocol", ScriptDatum.FromBonding(ScriptPathValue.PROTOCOL), writeable: false, enumerable: false);
            PathPrototype.Define("clone", ScriptDatum.FromBonding(ScriptPathValue.CLONE), writeable: false, enumerable: false);
            PathPrototype.Frozen();
        }
    }
}
