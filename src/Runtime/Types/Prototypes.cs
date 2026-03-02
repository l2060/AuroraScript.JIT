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
        /// <summary> The prototype for the ScriptObject constructor. </summary>
        public static readonly ScriptObject ScriptObjectConstructorPrototype = new ScriptObject(ObjectPrototype);
        /// <summary> The prototype for the Boolean constructor. </summary>
        public static readonly ScriptObject BooleanConstructorPrototype = new ScriptObject(ObjectPrototype);
        /// <summary> The prototype for boolean primitive values. </summary>
        public static readonly ScriptObject BooleanValuePrototype = new ScriptObject(ObjectPrototype);
        /// <summary> The prototype for callable objects (functions). </summary>
        public static readonly ScriptObject CallablePrototype = new ScriptObject(Prototypes.ObjectPrototype);
        /// <summary> The prototype for null values. </summary>
        public static readonly ScriptObject NullValuePrototype = new ScriptObject(null);
        /// <summary> The prototype for the Number constructor. </summary>
        public static readonly ScriptObject NumberConstructorPrototype = new ScriptObject(Prototypes.ObjectPrototype);
        /// <summary> The prototype for number primitive values. </summary>
        public static readonly ScriptObject NumberValuePrototype = new ScriptObject(Prototypes.ObjectPrototype);
        /// <summary> The prototype for script arrays. </summary>
        public static readonly ScriptObject ScriptArrayPrototype = new ScriptObject(Prototypes.ObjectPrototype);
        /// <summary> The prototype for the Array constructor. </summary>
        public static readonly ScriptObject ArrayConstructorPrototype = new ScriptObject(Prototypes.ObjectPrototype);
        /// <summary> The prototype for the String constructor. </summary>
        public static readonly ScriptObject StringConstructorPrototype = new ScriptObject(Prototypes.ObjectPrototype);
        /// <summary> The prototype for string primitive values. </summary>
        public static readonly ScriptObject StringValuePrototype = new ScriptObject(Prototypes.ObjectPrototype);
        /// <summary> The prototype for regular expression objects. </summary>
        public static readonly ScriptObject RegexPrototype = new ScriptObject(Prototypes.ObjectPrototype);
        /// <summary> The prototype for the Date constructor. </summary>
        public static readonly ScriptObject DateConstructorPrototype = new ScriptObject(Prototypes.ObjectPrototype);
        /// <summary> The prototype for date objects. </summary>
        public static readonly ScriptObject DatePrototype = new ScriptObject(Prototypes.ObjectPrototype);
        /// <summary> The prototype for hash map objects. </summary>
        public static readonly ScriptObject HashMapPrototype = new ScriptObject(Prototypes.ObjectPrototype);

        /// <summary>
        /// Forces pre-loading of prototypes.
        /// </summary>
        internal static void Preload()
        {
        }

        static Prototypes()
        {
            // --- ScriptObject ---
            // strict equal
            ScriptObjectConstructorPrototype.Define("equal$", new BondingFunction(ScriptObjectConstructor.STRICT_EQUAL), writeable: false, enumerable: false);
            // content equal  
            ScriptObjectConstructorPrototype.Define("equal", new BondingFunction(ScriptObjectConstructor.VALUE_EQUAL), writeable: false, enumerable: false);
            // deep content equal 
            ScriptObjectConstructorPrototype.Define("deepEqual", new BondingFunction(ScriptObjectConstructor.DEEP_EQUAL), writeable: false, enumerable: false);
            ScriptObjectConstructorPrototype.Define("assign", new BondingFunction(ScriptObjectConstructor.ASSIGN), writeable: false, enumerable: false);
            ScriptObjectConstructorPrototype.Define("keys", new BondingFunction(ScriptObjectConstructor.KEYS), writeable: false, enumerable: false);

            ScriptObjectConstructorPrototype.Define("clone", new BondingFunction(ScriptObjectConstructor.CLONE), writeable: false, enumerable: false);
            ScriptObjectConstructorPrototype.Define("deepClone", new BondingFunction(ScriptObjectConstructor.DEEP_CLONE), writeable: false, enumerable: false);
            ScriptObjectConstructorPrototype.Define("extends", new BondingFunction(ScriptObjectConstructor.EXTENDS), writeable: false, enumerable: false);

            ScriptObjectConstructorPrototype.Frozen();

            // Object instance methods
            ObjectPrototype.Define("toString", new BondingFunction(ScriptObject.TOSTRING), writeable: false, enumerable: false);
            ObjectPrototype.Define("length", new BondingGetter(ScriptObject.LENGTH), writeable: false, enumerable: false);
            ObjectPrototype.Frozen();

            // --- Boolean ---
            BooleanConstructorPrototype.Define("true", BooleanValue.True, writeable: false, enumerable: false);
            BooleanConstructorPrototype.Define("false", BooleanValue.False, writeable: false, enumerable: false);
            BooleanConstructorPrototype.Define("valueOf", new BondingFunction(BooleanConstructor.PARSE), writeable: false, enumerable: false);
            BooleanConstructorPrototype.Frozen();

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

            // --- Date ---
            DateConstructorPrototype.Define("now", new BondingFunction(ScriptDateConstructor.NOW), writeable: false, enumerable: false);
            DateConstructorPrototype.Define("utcNow", new BondingFunction(ScriptDateConstructor.UTC_NOW), writeable: false, enumerable: false);
            DateConstructorPrototype.Define("parse", new BondingFunction(ScriptDateConstructor.PARSE), writeable: false, enumerable: false);
            DateConstructorPrototype.Frozen();

            // --- DATA ---
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
            NumberConstructorPrototype.Define("MAX_VALUE", NumberConstructor.MAX_VALUE, writeable: false, enumerable: false);
            NumberConstructorPrototype.Define("MIN_VALUE", NumberConstructor.MIN_VALUE, writeable: false, enumerable: false);

            NumberConstructorPrototype.Define("MAX_SAFE_INTEGER", NumberConstructor.MAX_SAFE_INTEGER, writeable: false, enumerable: false);
            NumberConstructorPrototype.Define("MIN_SAFE_INTEGER", NumberConstructor.MIN_SAFE_INTEGER, writeable: false, enumerable: false);

            NumberConstructorPrototype.Define("NaN", NumberConstructor.NaN, writeable: false, enumerable: false);

            NumberConstructorPrototype.Define("POSITIVE_INFINITY", NumberConstructor.POSITIVE_INFINITY, writeable: false, enumerable: false);
            NumberConstructorPrototype.Define("NEGATIVE_INFINITY", NumberConstructor.NEGATIVE_INFINITY, writeable: false, enumerable: false);

            NumberConstructorPrototype.Define("isNaN", new BondingFunction(NumberConstructor.IS_NAN), writeable: false, enumerable: false);
            NumberConstructorPrototype.Define("isInteger", new BondingFunction(NumberConstructor.IS_INTEGER), writeable: false, enumerable: false);
            NumberConstructorPrototype.Define("isInfinity", new BondingFunction(NumberConstructor.IS_INFINITY), writeable: false, enumerable: false);

            NumberConstructorPrototype.Define("parseFloat", new BondingFunction(NumberConstructor.PARSE_FLOAT), writeable: false, enumerable: false);
            NumberConstructorPrototype.Define("parseInt", new BondingFunction(NumberConstructor.PARSE_INTEGER), writeable: false, enumerable: false);

            NumberConstructorPrototype.Frozen();

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
            ScriptArrayPrototype.Frozen();

            ArrayConstructorPrototype.Define("from", new BondingFunction(ArrayConstructor.FROM), writeable: false, enumerable: false);
            ArrayConstructorPrototype.Define("isArray", new BondingFunction(ArrayConstructor.IS_ARRAY), writeable: false, enumerable: false);
            ArrayConstructorPrototype.Define("of", new BondingFunction(ArrayConstructor.OF), writeable: false, enumerable: false);
            ArrayConstructorPrototype.Frozen();

            // --- String ---
            StringConstructorPrototype.Define("fromCharCode", new BondingFunction(StringConstructor.FROMCHARCODE), writeable: false, enumerable: false);
            StringConstructorPrototype.Define("valueOf", new BondingFunction(StringConstructor.CONSTRUCTOR), writeable: false, enumerable: false);
            StringConstructorPrototype.Define("compare", new BondingFunction(StringConstructor.COMPARE), writeable: false, enumerable: false);
            StringConstructorPrototype.Frozen();

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
        }
    }
}
