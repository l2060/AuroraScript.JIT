namespace AuroraScript.Runtime.Types
{
    /// <summary>
    /// Host-defined instantiable script object. Native fields and methods are
    /// dispatched from generated overrides; the instance uses the ordinary
    /// <c>Object</c> prototype.
    /// </summary>
    public abstract class AuroraNativeObject : ScriptObject
    {
        /// <summary>
        /// Creates an instance that uses the ordinary script <c>Object</c> prototype.
        /// </summary>
        protected AuroraNativeObject()
        {
        }
    }
}
