namespace AuroraScript.Hosting
{
    /// <summary>Parameter coercion contract for generated Datum adapters.</summary>
    public enum AuroraParamCoercion
    {
        /// <summary>Uses TryGet* helpers (for example string to number parsing).</summary>
        Weak,

        /// <summary>Uses exact CheckedType validation and throws on mismatch.</summary>
        Exact,

        /// <summary>Accepts only the exact primitive kind without weak parsing.</summary>
        Strict
    }
}
