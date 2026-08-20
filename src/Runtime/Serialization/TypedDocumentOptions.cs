namespace AuroraScript.Runtime.Serialization
{
    /// <summary>
    /// Controls textual TDoc serialization.
    /// </summary>
    public sealed record TypedDocumentOptions
    {
        /// <summary>Default TDoc options.</summary>
        public static readonly TypedDocumentOptions Default = new();

        internal static readonly TypedDocumentOptions Compact = new() { Indented = false };

        internal static readonly TypedDocumentOptions Explicit = new() { EmitTypeNames = true };

        internal static readonly TypedDocumentOptions CompactExplicit = new()
        {
            Indented = false,
            EmitTypeNames = true
        };

        /// <summary>Gets whether serialized TDoc is formatted across multiple lines.</summary>
        public bool Indented { get; init; } = true;

        /// <summary>
        /// Gets whether serialization writes explicit type names whenever a value has one.
        /// The default writes only type names that cannot be inferred from a raw TDoc
        /// literal. Set this to true to force every available type name. Typed arrays,
        /// built-in object-like values, and registered CLR types always retain their type
        /// name because their raw shapes are not uniquely inferable.
        /// </summary>
        public bool EmitTypeNames { get; init; }

        /// <summary>Gets the maximum nested value depth accepted by the reader and writer.</summary>
        public int MaxDepth { get; init; } = 128;

        internal static TypedDocumentOptions GetFormattingOptions(bool indented, bool emitTypeNames)
        {
            if (indented)
            {
                return emitTypeNames ? Explicit : Default;
            }
            return emitTypeNames ? CompactExplicit : Compact;
        }
    }
}
