namespace AuroraScript.Core
{
    /// <summary>
    /// Defines the type of hot patch to be applied to a script module at runtime.
    /// </summary>
    public enum HotPatchType
    {
        /// <summary>
        /// Represents a replacement patch where all members of the target module are replaced by the patch content.
        /// </summary>
        Replace = 1,

        /// <summary>
        /// Represents an incremental patch where new members are added and existing members with matching names are updated.
        /// </summary>
        Incremental = 2,

        /// <summary>
        /// Specifies that dependencies should be ignored if the imported modules are already loaded in the environment.
        /// </summary>
        IgnoreDepends = 4,
    }
}
