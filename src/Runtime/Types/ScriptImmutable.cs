namespace AuroraScript.Runtime.Types
{
    /// <summary>
    /// Represents an immutable script object. Once created, its properties cannot be modified, 
    /// deleted, or redefined. Foundations for primitive wrapper types (Boolean, Number, String).
    /// </summary>
    public abstract class ScriptImmutable : ScriptObject
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ScriptImmutable"/> class.
        /// </summary>
        /// <param name="prototype">The prototype object for this instance.</param>
        internal ScriptImmutable(ScriptObject prototype) : base(prototype, true)
        {
            Frozen();
        }

        /// <summary> Overridden to prevent property modification on immutable objects. </summary>
        internal sealed override void SetPropertyValue(ScriptContext ctx, string key, ScriptObject value)
        {
            // Immutable objects do not allow property settings.
        }

        /// <inheritdoc />
        protected internal sealed override void SetPropertyDatum(ScriptContext ctx, string key, ScriptDatum value)
        {
            // Immutable objects do not allow property settings.
        }

        /// <summary> Overridden to prevent property deletion on immutable objects. Always returns false. </summary>
        /// <inheritdoc />
        protected internal sealed override bool DeletePropertyValue(ScriptContext ctx, string key)
        {
            return false;
        }

        /// <summary> Overridden to prevent property definition on immutable objects. </summary>
        public sealed override void Define(string key, ScriptObject value, bool writeable = true, bool enumerable = true)
        {
            // Immutable objects do not allow property definitions.
        }

        /// <summary> Prevents defining datum properties on immutable objects. </summary>
        public sealed override void Define(string key, ScriptDatum value, bool writeable = true, bool enumerable = true)
        {
            // Immutable objects do not allow property definitions.
        }
    }
}
