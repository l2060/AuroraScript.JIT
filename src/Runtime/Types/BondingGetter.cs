namespace AuroraScript.Runtime.Types
{
    /// <summary>
    /// Represents a CLR delegate used as a property getter in AuroraScript.
    /// </summary>
    /// <param name="object">The object whose property is being accessed.</param>
    /// <param name="result">The resulting datum retrieved from the property.</param>
    public delegate void ClrGetterDelegate(ScriptObject @object, ref ScriptDatum result);

    /// <summary>Represents a CLR delegate used as a property setter in AuroraScript.</summary>
    public delegate void ClrSetterDelegate(
        ScriptContext context,
        ScriptObject @object,
        ScriptDatum value);

    /// <summary>Represents a property accessor backed by native CLR delegates.</summary>
    public class BondingAccessor : ScriptObject
    {
        /// <summary>Initializes a native property accessor.</summary>
        public BondingAccessor(ClrGetterDelegate getter, ClrSetterDelegate setter)
        {
            Getter = getter;
            Setter = setter;
        }

        /// <summary>The native getter, or null for a write-only accessor.</summary>
        public ClrGetterDelegate Getter { get; }

        /// <summary>The native setter, or null for a read-only accessor.</summary>
        public ClrSetterDelegate Setter { get; }
    }

    /// <summary>
    /// Represents a property getter that invokes a bonded native CLR method.
    /// </summary>
    public class BondingGetter : BondingAccessor
    {
        private readonly ClrGetterDelegate _callback;

        /// <summary> Gets the name of the bonded native getter method. </summary>
        public readonly string Name;

        /// <summary>
        /// Initializes a new instance of the <see cref="BondingGetter"/> class.
        /// </summary>
        /// <param name="callback">The CLR delegate to invoke for property retrieval.</param>
        public BondingGetter(ClrGetterDelegate callback)
            : base(callback, null)
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
