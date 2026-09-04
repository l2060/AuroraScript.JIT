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
        /// <summary> Standard identifier for signed 64-bit integers. </summary>
        public static readonly ScriptDatum Int64 = ScriptDatum.FromString(new StringValue("int64"));
        /// <summary> Standard identifier for unsigned 64-bit integers. </summary>
        public static readonly ScriptDatum UInt64 = ScriptDatum.FromString(new StringValue("uint64"));
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
        /// <summary> Standard identifier for StringBuffer. </summary>
        public static readonly ScriptDatum StringBuffer = ScriptDatum.FromString(new StringValue("StringBuffer"));
        /// <summary> Standard identifier for Path. </summary>
        public static readonly ScriptDatum Path = ScriptDatum.FromString(new StringValue("Path"));
        /// <summary> Standard identifier for HashMap. </summary>
        public static readonly ScriptDatum HashMap = ScriptDatum.FromString(new StringValue("HashMap"));
        /// <summary> Standard identifier for Int32Array. </summary>
        public static readonly ScriptDatum Int32Array = ScriptDatum.FromString(new StringValue("Int32Array"));
        /// <summary> Standard identifier for Int8Array. </summary>
        public static readonly ScriptDatum Int8Array = ScriptDatum.FromString(new StringValue("Int8Array"));
        /// <summary> Standard identifier for Float32Array. </summary>
        public static readonly ScriptDatum Float32Array = ScriptDatum.FromString(new StringValue("Float32Array"));
        /// <summary> Standard identifier for Float64Array. </summary>
        public static readonly ScriptDatum Float64Array = ScriptDatum.FromString(new StringValue("Float64Array"));
        /// <summary> Standard identifier for BooleanArray. </summary>
        public static readonly ScriptDatum BooleanArray = ScriptDatum.FromString(new StringValue("BooleanArray"));
        /// <summary> Standard identifier for UInt8Array. </summary>
        public static readonly ScriptDatum UInt8Array = ScriptDatum.FromString(new StringValue("UInt8Array"));
        /// <summary> Standard identifier for Int16Array. </summary>
        public static readonly ScriptDatum Int16Array = ScriptDatum.FromString(new StringValue("Int16Array"));
        /// <summary> Standard identifier for UInt16Array. </summary>
        public static readonly ScriptDatum UInt16Array = ScriptDatum.FromString(new StringValue("UInt16Array"));
        /// <summary> Standard identifier for UInt32Array. </summary>
        public static readonly ScriptDatum UInt32Array = ScriptDatum.FromString(new StringValue("UInt32Array"));
        /// <summary> Standard identifier for Int64Array. </summary>
        public static readonly ScriptDatum Int64Array = ScriptDatum.FromString(new StringValue("Int64Array"));
        /// <summary> Standard identifier for UInt64Array. </summary>
        public static readonly ScriptDatum UInt64Array = ScriptDatum.FromString(new StringValue("UInt64Array"));
    }
}
