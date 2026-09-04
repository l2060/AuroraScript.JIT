using System;

namespace AuroraScript.Runtime
{
    /// <summary>
    /// Storage and hot-path dispatch tags for a <see cref="ScriptDatum"/>.
    /// This enum is not the script type-name registry: native objects such as
    /// <c>Int8Array</c> and <c>StringBuffer</c> stay <see cref="Object"/> and
    /// report their identity through <see cref="Types.ScriptObject.TypeOfValue"/>.
    /// </summary>
    [Flags]
    public enum ValueKind : short
    {
        /// <summary> Represents a null value. </summary>
        Null = 0,
        /// <summary> Represents a boolean value. </summary>
        Boolean = 1 << 0,
        /// <summary> Represents a numeric value (Double). </summary>
        Number = 1 << 1,
        /// <summary> Represents a string value. </summary>
        String = 1 << 2,
        /// <summary> Represents a base object-based value. </summary>
        Object = 1 << 3,

        /// <summary> Represents an exact signed 64-bit integer. </summary>
        Int64 = 1 << 12,

        /// <summary> Represents an exact unsigned 64-bit integer. </summary>
        UInt64 = 1 << 13,

        /// <summary> Represents a script array object. </summary>
        Array = Object | (1 << 4),

        /// <summary> Represents a date object. </summary>
        Date = Object | (1 << 5),

        /// <summary> Represents a regular expression object. </summary>
        Regex = Object | (1 << 6),

        /// <summary>
        /// Represents a native script function (Closure).
        /// </summary>
        Function = Object | (1 << 7),

        /// <summary>
        /// Represents a CLR native type wrapped for script usage.
        /// </summary>
        Type = Object | (1 << 8),

        /// <summary>
        /// Represents a native CLR method wrapped as a function.
        /// </summary>
        ClrFunction = Object | (1 << 9),

        /// <summary>
        /// Represents a CLR-side bonding function for prototype method binding.
        /// </summary>
        ClrBonding = Object | (1 << 10),

        /// <summary>
        /// Represents a native script Error.
        /// </summary>
        Error = Object | (1 << 11)
    }
}
