namespace AuroraScript.Runtime.Serialization
{
    /// <summary>
    /// Controls textual ATD serialization and identifies input in diagnostics.
    /// </summary>
    public sealed record TypedDataOptions
    {
        /// <summary>Default ATD options.</summary>
        public static readonly TypedDataOptions Default = new();

        /// <summary>Gets the source name included in parse and binding diagnostics.</summary>
        public string SourceName { get; init; } = "<atd>";

        /// <summary>Gets whether serialized ATD is formatted across multiple lines.</summary>
        public bool Indented { get; init; } = true;

        /// <summary>Gets the maximum nested value depth accepted by the reader and writer.</summary>
        public int MaxDepth { get; init; } = 128;
    }
}
