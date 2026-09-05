namespace AuroraScript.Hosting
{
    /// <summary>Identifies the script surface that owns an exported CLR member.</summary>
    public enum AuroraExportTarget : byte
    {
        /// <summary>Infer the target from whether the CLR member is static or instance.</summary>
        Auto,

        /// <summary>Expose the member on the script type object.</summary>
        Type,

        /// <summary>Expose the member on script instances.</summary>
        Instance
    }
}
