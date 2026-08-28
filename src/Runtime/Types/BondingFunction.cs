using System;

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
    public delegate void ClrDatumDelegate(ScriptContext ctx, ScriptObject module, Span<ScriptDatum> args, ref ScriptDatum result);



    /// <summary>
    /// Represents a function object that invokes a bonded native CLR method.
    /// </summary>
    public class BondingFunction : ScriptObject
    {
        /// <summary> Gets or sets the target object used as the 'this' context for the call. </summary>
        public readonly ScriptObject Target;

        /// <inheritdoc />
        protected internal override ScriptDatum TypeOfValue => TypeNames.ClrBonding;

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
        internal override ScriptDatum Invoke(ScriptContext ctx, Span<ScriptDatum> args)
        {
            var target = Target;
            ScriptDatum result = default;
            DatumMethod.Invoke(ctx, target, args, ref result);
            return result;
        }

        internal override ScriptDatum Invoke(ScriptContext ctx)
        {
            var target = Target;
            ScriptDatum result = default;
            DatumMethod.Invoke(ctx, target, Span<ScriptDatum>.Empty, ref result);
            return result;
        }

        internal override ScriptDatum Invoke(ScriptContext ctx, ScriptDatum arg1)
        {
            var target = Target;
            ScriptDatum result = default;
            DatumBuffer1 buf = default;
            buf[0] = arg1;
            DatumMethod.Invoke(ctx, target, buf, ref result);
            return result;
        }

        internal override ScriptDatum Invoke(ScriptContext ctx, ScriptDatum arg1, ScriptDatum arg2)
        {
            var target = Target;
            ScriptDatum result = default;
            DatumBuffer2 buf = default;
            buf[0] = arg1;
            buf[1] = arg2;
            DatumMethod.Invoke(ctx, target, buf, ref result);
            return result;
        }

        internal override ScriptDatum Invoke(ScriptContext ctx, ScriptDatum arg1, ScriptDatum arg2, ScriptDatum arg3)
        {
            var target = Target;
            ScriptDatum result = default;
            DatumBuffer3 buf = default;
            buf[0] = arg1;
            buf[1] = arg2;
            buf[2] = arg3;
            DatumMethod.Invoke(ctx, target, buf, ref result);
            return result;
        }

        internal override ScriptDatum Invoke(ScriptContext ctx, ScriptDatum arg1, ScriptDatum arg2, ScriptDatum arg3, ScriptDatum arg4)
        {
            var target = Target;
            ScriptDatum result = default;
            DatumBuffer4 buf = default;
            buf[0] = arg1;
            buf[1] = arg2;
            buf[2] = arg3;
            buf[3] = arg4;
            DatumMethod.Invoke(ctx, target, buf, ref result);
            return result;
        }

        internal override ScriptDatum Invoke(ScriptContext ctx, ScriptDatum arg1, ScriptDatum arg2, ScriptDatum arg3, ScriptDatum arg4, ScriptDatum arg5)
        {
            var target = Target;
            ScriptDatum result = default;
            DatumBuffer5 buf = default;
            buf[0] = arg1;
            buf[1] = arg2;
            buf[2] = arg3;
            buf[3] = arg4;
            buf[4] = arg5;
            DatumMethod.Invoke(ctx, target, buf, ref result);
            return result;
        }

        internal override ScriptDatum Invoke(ScriptContext ctx, ScriptDatum arg1, ScriptDatum arg2, ScriptDatum arg3, ScriptDatum arg4, ScriptDatum arg5, ScriptDatum arg6)
        {
            var target = Target;
            ScriptDatum result = default;
            DatumBuffer6 buf = default;
            buf[0] = arg1;
            buf[1] = arg2;
            buf[2] = arg3;
            buf[3] = arg4;
            buf[4] = arg5;
            buf[5] = arg6;
            DatumMethod.Invoke(ctx, target, buf, ref result);
            return result;
        }

        internal override ScriptDatum Invoke(ScriptContext ctx, ScriptDatum arg1, ScriptDatum arg2, ScriptDatum arg3, ScriptDatum arg4, ScriptDatum arg5, ScriptDatum arg6, ScriptDatum arg7)
        {
            var target = Target;
            ScriptDatum result = default;
            DatumBuffer7 buf = default;
            buf[0] = arg1;
            buf[1] = arg2;
            buf[2] = arg3;
            buf[3] = arg4;
            buf[4] = arg5;
            buf[5] = arg6;
            buf[6] = arg7;
            DatumMethod.Invoke(ctx, target, buf, ref result);
            return result;
        }

        internal override ScriptDatum Invoke(ScriptContext ctx, ScriptDatum arg1, ScriptDatum arg2, ScriptDatum arg3, ScriptDatum arg4, ScriptDatum arg5, ScriptDatum arg6, ScriptDatum arg7, ScriptDatum arg8)
        {
            var target = Target;
            ScriptDatum result = default;
            DatumBuffer8 buf = default;
            buf[0] = arg1;
            buf[1] = arg2;
            buf[2] = arg3;
            buf[3] = arg4;
            buf[4] = arg5;
            buf[5] = arg6;
            buf[6] = arg7;
            buf[7] = arg8;
            DatumMethod.Invoke(ctx, target, buf, ref result);
            return result;
        }


        private WeakReference<InternalBoundCache> _lastBound;

        // TODO 改为弱引用
        private class InternalBoundCache
        {
            public ScriptObject Target;
            public BondingFunction Function;
        }

        /// <summary> Binds the function to a specific target object, creating a new bonded function. </summary>
        public BondingFunction Bind(ScriptObject target)
        {
            if (_lastBound != null && _lastBound.TryGetTarget(out var bound) && ReferenceEquals(bound.Target, target))
            {
                return bound.Function;
            }
            var bind = new BondingFunction(DatumMethod, target, Prototype);
            _lastBound = new WeakReference<InternalBoundCache>(new InternalBoundCache() { Target = target, Function = bind });
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
