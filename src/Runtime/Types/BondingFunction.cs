using System;
using System.Diagnostics.CodeAnalysis;

namespace AuroraScript.Runtime.Types
{
    /// <summary>
    /// Represents a CLR delegate that can be called from AuroraScript.
    /// Used for bonding native C# methods to the script runtime.
    /// </summary>
    /// <param name="ctx">The current script execution context.</param>
    /// <param name="module">The script object representing the module or 'this' context.</param>
    /// <param name="args">The arguments passed from the script.</param>
    /// <param name="result">The result to be returned to the script.</param>
    public delegate void ClrDatumDelegate([NotNull] ScriptContext ctx, ScriptObject module, [NotNull] Span<ScriptDatum> args, ref ScriptDatum result);

    /// <summary>
    /// Represents a function object that invokes a bonded native CLR method.
    /// </summary>
    public class BondingFunction : ScriptObject
    {
        /// <summary> Gets or sets the target object used as the 'this' context for the call. </summary>
        public readonly ScriptObject Target;

        /// <summary> Gets the underlying CLR delegate. </summary>
        public readonly ClrDatumDelegate DatumMethod;

        /// <summary>
        /// Initializes a new instance of the <see cref="BondingFunction"/> class.
        /// </summary>
        /// <param name="callback">The CLR delegate to invoke.</param>
        public BondingFunction(ClrDatumDelegate callback) : base()
        {
            DatumMethod = callback ?? throw new ArgumentNullException(nameof(callback));
        }

        private BondingFunction(ClrDatumDelegate callback, ScriptObject target, ScriptObject prototype) : base(prototype)
        {
            DatumMethod = callback ?? throw new ArgumentNullException(nameof(callback));
            Target = target;
        }

        /// <summary> Invokes the bonded native function with the provided arguments. </summary>
        internal override ScriptDatum Invoke(ScriptContext ctx, params ScriptDatum[] args)
        {
            var target = Target;
            ScriptDatum result = default;
            DatumMethod.Invoke(ctx, target, args, ref result);
            return result;
        }

        /// <summary> Binds the function to a specific target object, creating a new bonded function. </summary>
        public BondingFunction Bind(ScriptObject target)
        {
            var bind = new BondingFunction(DatumMethod, target, Prototype);
            return bind;
        }

        /// <summary> Gets the fully qualified name of the bonded native method. </summary>
        public string Name
        {
            get
            {
                return DatumMethod.Method.DeclaringType.Name + "." + DatumMethod.Method.Name;
            }
        }

        /// <summary> Returns a string representation of the bonded function. </summary>
        public override string ToString()
        {
            return "ClrFunction: " + Name;
        }
    }
}
