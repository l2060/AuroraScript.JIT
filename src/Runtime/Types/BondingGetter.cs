namespace AuroraScript.Runtime.Types
{
    /// <summary>
    /// Represents a CLR delegate used as a property getter in AuroraScript.
    /// </summary>
    /// <param name="object">The object whose property is being accessed.</param>
    /// <param name="result">The resulting datum retrieved from the property.</param>
    public delegate void ClrGetterDelegate(ScriptObject @object, ref ScriptDatum result);

    /// <summary>
    /// Represents a property getter that invokes a bonded native CLR method.
    /// </summary>
    public class BondingGetter : ScriptObject
    {
        private readonly ClrGetterDelegate _callback;

        /// <summary> Gets the name of the bonded native getter method. </summary>
        public readonly string Name;

        /// <summary>
        /// Initializes a new instance of the <see cref="BondingGetter"/> class.
        /// </summary>
        /// <param name="callback">The CLR delegate to invoke for property retrieval.</param>
        public BondingGetter(ClrGetterDelegate callback)
        {
            var method = callback.Method;
            Name = method.DeclaringType.Name + "." + method.Name;
            _callback = callback;
        }

        /// <summary> Invokes the bonded native getter. </summary>
        public void Invoke(ScriptObject @object, ref ScriptDatum result)
        {
            _callback.Invoke(@object, ref result);
        }

        /// <summary> Returns a string representation of the bonded getter. </summary>
        public override string ToString()
        {
            return "ClrGetter: " + Name;
        }
    }
}
