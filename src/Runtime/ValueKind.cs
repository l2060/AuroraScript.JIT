using System;

namespace AuroraScript.Runtime
{
    /// <summary>
    /// Specifies the various kinds of values that can be stored in a <see cref="ScriptDatum"/>.
    /// This enum uses a bit-flag layout to distinguish between primitive types and object-based types.
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
