using AuroraScript.Runtime.Types;

namespace AuroraScript.Runtime
{
    /// <summary>
    /// Provides standardized string identifiers for all runtime types in AuroraScript.
    /// Used for introspection, debugging, and type-checking operations.
    /// </summary>
    internal static class TypeNames
    {
        /// <summary> Standard identifier for objects. </summary>
        public static readonly ScriptDatum Object = ScriptDatum.FromString(new StringValue("object"));
        /// <summary> Standard identifier for arrays. </summary>
        public static readonly ScriptDatum Array = ScriptDatum.FromString(new StringValue("array"));
        /// <summary> Standard identifier for dates. </summary>
        public static readonly ScriptDatum Date = ScriptDatum.FromString(new StringValue("date"));
        /// <summary> Standard identifier for strings. </summary>
        public static readonly ScriptDatum String = ScriptDatum.FromString(new StringValue("string"));
        /// <summary> Standard identifier for numbers. </summary>
        public static readonly ScriptDatum Number = ScriptDatum.FromString(new StringValue("number"));
        /// <summary> Standard identifier for boolean values. </summary>
        public static readonly ScriptDatum Boolean = ScriptDatum.FromString(new StringValue("boolean"));
        /// <summary> Standard identifier for null. </summary>
        public static readonly ScriptDatum Null = ScriptDatum.FromString(new StringValue("null"));
        /// <summary> Standard identifier for regular expressions. </summary>
        public static readonly ScriptDatum Regex = ScriptDatum.FromString(new StringValue("regex"));
        /// <summary> Standard identifier for script functions. </summary>
        public static readonly ScriptDatum Function = ScriptDatum.FromString(new StringValue("function"));
        /// <summary> Standard identifier for CLR native functions. </summary>
        public static readonly ScriptDatum ClrFunction = ScriptDatum.FromString(new StringValue("clr:function"));
        /// <summary> Standard identifier for CLR bonding functions. </summary>
        public static readonly ScriptDatum ClrBonding = ScriptDatum.FromString(new StringValue("clr:bonding"));
        /// <summary> Standard identifier for CLR types. </summary>
        public static readonly ScriptDatum Type = ScriptDatum.FromString(new StringValue("type"));
        /// <summary> Standard identifier for error. </summary>
        public static readonly ScriptDatum Error = ScriptDatum.FromString(new StringValue("error"));
    }
}