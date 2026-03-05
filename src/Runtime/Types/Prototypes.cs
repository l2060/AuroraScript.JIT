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
        /// <summary>
        /// Forces pre-loading of prototypes.
        /// </summary>
        internal static void Preload()
        {
        }

        static Prototypes()
        {
            // --- ScriptObject ---
            ObjectPrototype.Define("toString", new BondingFunction(ScriptObject.TOSTRING), writeable: false, enumerable: false);
            ObjectPrototype.Define("length", new BondingGetter(ScriptObject.LENGTH), writeable: false, enumerable: false);
            ObjectPrototype.Frozen();

            // --- Boolean ---
            BooleanValuePrototype.Define("toString", new BondingFunction(BooleanValue.TOSTRING), writeable: false, enumerable: false);
            BooleanValuePrototype.Frozen();

            // --- Regex ---
            RegexPrototype.Define("test", new BondingFunction(ScriptRegex.TEST), writeable: false, enumerable: false);
            RegexPrototype.Frozen();

            // --- HashMap ---
            HashMapPrototype.Define("has", new BondingFunction(ScriptHashMap.HAS), writeable: false, enumerable: false);
            HashMapPrototype.Define("set", new BondingFunction(ScriptHashMap.SET), writeable: false, enumerable: false);
            HashMapPrototype.Define("get", new BondingFunction(ScriptHashMap.GET), writeable: false, enumerable: false);
            HashMapPrototype.Define("getOrInsert", new BondingFunction(ScriptHashMap.OGETORINSERT), writeable: false, enumerable: false);
            HashMapPrototype.Define("delete", new BondingFunction(ScriptHashMap.DELETE), writeable: false, enumerable: false);
            HashMapPrototype.Define("clear", new BondingFunction(ScriptHashMap.CLEAR), writeable: false, enumerable: false);
            HashMapPrototype.Define("keys", new BondingGetter(ScriptHashMap.KEYS), writeable: false, enumerable: false);
            HashMapPrototype.Define("values", new BondingGetter(ScriptHashMap.VALUES), writeable: false, enumerable: false);
            HashMapPrototype.Define("size", new BondingGetter(ScriptHashMap.SIZE), writeable: false, enumerable: false);
            HashMapPrototype.Frozen();

            // --- DATE ---
            DatePrototype.Define("year", new BondingGetter(ScriptDate.YEAR), writeable: false, enumerable: false);
            DatePrototype.Define("month", new BondingGetter(ScriptDate.MONTH), writeable: false, enumerable: false);
            DatePrototype.Define("day", new BondingGetter(ScriptDate.DAY), writeable: false, enumerable: false);
            DatePrototype.Define("hour", new BondingGetter(ScriptDate.HOUR), writeable: false, enumerable: false);
            DatePrototype.Define("minute", new BondingGetter(ScriptDate.MINUTE), writeable: false, enumerable: false);
            DatePrototype.Define("second", new BondingGetter(ScriptDate.SECOND), writeable: false, enumerable: false);
            DatePrototype.Define("millisecond", new BondingGetter(ScriptDate.MILLISECCOND), writeable: false, enumerable: false);
            DatePrototype.Define("dayOfWeek", new BondingGetter(ScriptDate.DAYOFWEEK), writeable: false, enumerable: false);
            DatePrototype.Define("dayOfYear", new BondingGetter(ScriptDate.DAYOFYEAR), writeable: false, enumerable: false);
            DatePrototype.Define("ticks", new BondingGetter(ScriptDate.TICKS), writeable: false, enumerable: false);
            DatePrototype.Define("toString", new BondingFunction(ScriptDateConstructor.TOSTRING), writeable: false, enumerable: false);
            DatePrototype.Frozen();

            // --- Callable ---
            CallablePrototype.Frozen();

            // --- Null ---
            NullValuePrototype.Define("toString", new BondingFunction(NullValue.TOSTRING), writeable: false, enumerable: false);
            NullValuePrototype.Frozen();

            // --- Number ---
            NumberValuePrototype.Define("toString", new BondingFunction(NumberValue.TOSTRING), writeable: false, enumerable: false);
            NumberValuePrototype.Frozen();

            // --- Array ---
            ScriptArrayPrototype.Define("has", new BondingFunction(ScriptArray.HAS), writeable: false, enumerable: false);
            ScriptArrayPrototype.Define("indexOf", new BondingFunction(ScriptArray.INDEXOF), writeable: false, enumerable: false);
            ScriptArrayPrototype.Define("lastIndexOf", new BondingFunction(ScriptArray.LASTINDEXOF), writeable: false, enumerable: false);
            ScriptArrayPrototype.Define("length", new BondingGetter(ScriptArray.LENGTH), writeable: false, enumerable: false);
            ScriptArrayPrototype.Define("push", new BondingFunction(ScriptArray.PUSH), writeable: false, enumerable: false);
            ScriptArrayPrototype.Define("pop", new BondingFunction(ScriptArray.POP), writeable: false, enumerable: false);
            ScriptArrayPrototype.Define("sort", new BondingFunction(ScriptArray.SORT), writeable: false, enumerable: false);
            ScriptArrayPrototype.Define("join", new BondingFunction(ScriptArray.JOIN), writeable: false, enumerable: false);
            ScriptArrayPrototype.Define("slice", new BondingFunction(ScriptArray.SLICE), writeable: false, enumerable: false);
            ScriptArrayPrototype.Define("reverse", new BondingFunction(ScriptArray.REVERSE), writeable: false, enumerable: false);
            ScriptArrayPrototype.Define("unshift", new BondingFunction(ScriptArray.UNSHIFT), writeable: false, enumerable: false);
            ScriptArrayPrototype.Define("shift", new BondingFunction(ScriptArray.SHIFT), writeable: false, enumerable: false);
            ScriptArrayPrototype.Define("concat", new BondingFunction(ScriptArray.CONCAT), writeable: false, enumerable: false);
            ScriptArrayPrototype.Define("find", new BondingFunction(ScriptArray.FIND), writeable: false, enumerable: false);
            ScriptArrayPrototype.Define("findIndex", new BondingFunction(ScriptArray.FINDINDEX), writeable: false, enumerable: false);
            ScriptArrayPrototype.Define("findLast", new BondingFunction(ScriptArray.FINDLAST), writeable: false, enumerable: false);
            ScriptArrayPrototype.Define("findLastIndex", new BondingFunction(ScriptArray.FINDLASTINDEX), writeable: false, enumerable: false);
            ScriptArrayPrototype.Define("map", new BondingFunction(ScriptArray.MAP), writeable: false, enumerable: false);
            ScriptArrayPrototype.Define("filter", new BondingFunction(ScriptArray.FILTER), writeable: false, enumerable: false);
            ScriptArrayPrototype.Define("some", new BondingFunction(ScriptArray.SOME), writeable: false, enumerable: false);
            ScriptArrayPrototype.Define("every", new BondingFunction(ScriptArray.EVERY), writeable: false, enumerable: false);
            ScriptArrayPrototype.Define("flat", new BondingFunction(ScriptArray.FLAT), writeable: false, enumerable: false);
            ScriptArrayPrototype.Define("reduce", new BondingFunction(ScriptArray.REDUCE), writeable: false, enumerable: false);
            ScriptArrayPrototype.Frozen();



            // --- String ---
            StringValuePrototype.Define("length", new BondingGetter(StringValue.LENGTH), writeable: false, enumerable: false);
            StringValuePrototype.Define("contains", new BondingFunction(StringValue.CONTANINS), writeable: false, enumerable: false);
            StringValuePrototype.Define("indexOf", new BondingFunction(StringValue.INDEXOF), writeable: false, enumerable: false);
            StringValuePrototype.Define("lastIndexOf", new BondingFunction(StringValue.LASTINDEXOF), writeable: false, enumerable: false);
            StringValuePrototype.Define("startsWith", new BondingFunction(StringValue.STARTSWITH), writeable: false, enumerable: false);
            StringValuePrototype.Define("endsWith", new BondingFunction(StringValue.ENDSWITH), writeable: false, enumerable: false);
            StringValuePrototype.Define("substring", new BondingFunction(StringValue.SUBSTRING), writeable: false, enumerable: false);
            StringValuePrototype.Define("split", new BondingFunction(StringValue.SPLIT), writeable: false, enumerable: false);
            StringValuePrototype.Define("match", new BondingFunction(StringValue.MATCH), writeable: false, enumerable: false);
            StringValuePrototype.Define("matchAll", new BondingFunction(StringValue.MATCHALL), writeable: false, enumerable: false);
            StringValuePrototype.Define("replace", new BondingFunction(StringValue.REPLACE), writeable: false, enumerable: false);
            StringValuePrototype.Define("padLeft", new BondingFunction(StringValue.PADLEFT), writeable: false, enumerable: false);
            StringValuePrototype.Define("padRight", new BondingFunction(StringValue.PADRIGHT), writeable: false, enumerable: false);
            StringValuePrototype.Define("trim", new BondingFunction(StringValue.TRIM), writeable: false, enumerable: false);
            StringValuePrototype.Define("trimLeft", new BondingFunction(StringValue.TRIMLEFT), writeable: false, enumerable: false);
            StringValuePrototype.Define("trimRight", new BondingFunction(StringValue.TRIMRIGHT), writeable: false, enumerable: false);
            StringValuePrototype.Define("slice", new BondingFunction(StringValue.SUBSTRING), writeable: false, enumerable: false);
            StringValuePrototype.Define("toString", new BondingFunction(StringValue.TOSTRING), writeable: false, enumerable: false);
            StringValuePrototype.Define("charCodeAt", new BondingFunction(StringValue.CHARCODEAT), writeable: false, enumerable: false);
            StringValuePrototype.Define("toLowerCase", new BondingFunction(StringValue.TOLOWERCASE), writeable: false, enumerable: false);
            StringValuePrototype.Define("toUpperCase", new BondingFunction(StringValue.TOUPPERCASE), writeable: false, enumerable: false);
            StringValuePrototype.Frozen();




            // --- StringBuffer ---
            StringBufferPrototype.Define("toString", new BondingFunction(StringBuffer.TO_STRING), writeable: false, enumerable: false);
            StringBufferPrototype.Define("append", new BondingFunction(StringBuffer.APPEND), writeable: false, enumerable: false);
            StringBufferPrototype.Define("insert", new BondingFunction(StringBuffer.INSERT), writeable: false, enumerable: false);
            StringBufferPrototype.Define("appendLine", new BondingFunction(StringBuffer.APPEND_LINE), writeable: false, enumerable: false);
            StringBufferPrototype.Define("clear", new BondingFunction(StringBuffer.CLEAR), writeable: false, enumerable: false);
            StringBufferPrototype.Define("release", new BondingFunction(StringBuffer.RELEASE), writeable: false, enumerable: false);
            StringBufferPrototype.Define("stringAndRelease", new BondingFunction(StringBuffer.STRINGANDRELEASE), writeable: false, enumerable: false);
            StringBufferPrototype.Frozen();
        }
    }
}
