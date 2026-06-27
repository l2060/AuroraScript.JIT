namespace AuroraScript
{
    /// <summary>
    /// Identifies the compiler stage that produced a diagnostic.
    /// </summary>
    internal enum AuroraCompilationStage
    {
        /// <summary>Source text could not be tokenized.</summary>
        Lexing,

        /// <summary>Tokens could not be parsed into a valid syntax tree.</summary>
        Parsing,

        /// <summary>Symbols, declarations, or module references could not be bound.</summary>
        Binding,

        /// <summary>The syntax tree could not be lowered into the compiler's executable form.</summary>
        Lowering,

        /// <summary>The executable form could not be emitted.</summary>
        Emission,

        /// <summary>Modules or dependencies could not be linked into a valid program.</summary>
        Linking
    }
}
