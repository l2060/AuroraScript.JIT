using System;

namespace AuroraScript.Runtime.Types
{

    /// <summary>
    /// Represents a base class for script type definitions that support instance construction.
    /// </summary>
    public abstract class ScriptType : ScriptObject
    {
        /// <summary> Gets the name of the script type. </summary>
        public readonly string Name;
        private readonly Boolean Callable;
        /// <summary>
        /// Initializes a new instance of the <see cref="ScriptType"/> class.
        /// </summary>
        /// <param name="name">The name of the type.</param>
        /// <param name="callable">Can the type be called as a method?</param>
        protected ScriptType(string name, Boolean callable = false) : base(Prototypes.ObjectPrototype)
        {
            Name = name;
            Callable = callable;
        }

        internal override ScriptDatum Invoke(ScriptContext ctx, params ScriptDatum[] args)
        {
            if (Callable)
            {
                ScriptDatum result = default;
                Construct(ctx, args, ref result);
                return result;
            }
            return base.Invoke(ctx, args);
        }


        /// <summary>
        /// Concrete types must implement this to handle instance construction.
        /// </summary>
        /// <param name="ctx">The execution context.</param>
        /// <param name="args">Arguments passed during construction.</param>
        /// <param name="result">The result of the construction.</param>
        public abstract void Construct(ScriptContext ctx, ScriptDatum[] args, ref ScriptDatum result);

        /// <summary> Returns a string representation of the script type. </summary>
        public override string ToString()
        {
            return "Type: " + Name;
        }
    }
}
