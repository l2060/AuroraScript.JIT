namespace AuroraScript.Hosting
{
    /// <summary>Failure behavior when a generated Datum adapter cannot coerce arguments.</summary>
    public enum AuroraExportFailure
    {
        /// <summary>Infer from the core return type (number to NaN, otherwise null).</summary>
        Default,

        /// <summary>Return the script numeric value NaN.</summary>
        ReturnNaN,

        /// <summary>Return the script null value.</summary>
        ReturnNull,

        /// <summary>Throw an <c>AuroraRuntimeException</c>.</summary>
        Throw
    }
}
