namespace AuroraScript.Runtime.Types
{
    /// <summary>
    /// Provides integer indexing for native script objects. The compiler binds a
    /// proven native receiver directly to its implementation; dynamic receivers
    /// use the same contract through the runtime element protocol.
    /// </summary>
    public interface IAuroraNativeIndexer
    {
        /// <summary>Gets or assigns a script value at an integer index.</summary>
        ScriptDatum this[int index] { get; set; }
    }
}
