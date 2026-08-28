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

        /// <inheritdoc />
        protected internal override ScriptDatum TypeOfValue => TypeNames.Type;

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

        internal override ScriptDatum Invoke(ScriptContext ctx, Span<ScriptDatum> args)
        {
            if (Callable)
            {
                ScriptDatum result = default;
                Construct(ctx, args, ref result);
                return result;
            }
            return base.Invoke(ctx, args);
        }

        internal override ScriptDatum Invoke(ScriptContext ctx)
        {
            if (Callable)
            {
                ScriptDatum result = default;
                Construct(ctx, Span<ScriptDatum>.Empty, ref result);
                return result;
            }
            return base.Invoke(ctx);
        }

        internal override ScriptDatum Invoke(ScriptContext ctx, ScriptDatum arg1)
        {
            if (Callable)
            {
                ScriptDatum result = default;
                DatumBuffer1 buf = default;
                buf[0] = arg1;
                Construct(ctx, buf, ref result);
                return result;
            }
            return base.Invoke(ctx, arg1);
        }

        internal override ScriptDatum Invoke(ScriptContext ctx, ScriptDatum arg1, ScriptDatum arg2)
        {
            if (Callable)
            {
                ScriptDatum result = default;
                DatumBuffer2 buf = default;
                buf[0] = arg1;
                buf[1] = arg2;
                Construct(ctx, buf, ref result);
                return result;
            }
            return base.Invoke(ctx, arg1, arg2);
        }

        internal override ScriptDatum Invoke(ScriptContext ctx, ScriptDatum arg1, ScriptDatum arg2, ScriptDatum arg3)
        {
            if (Callable)
            {
                ScriptDatum result = default;
                DatumBuffer3 buf = default;
                buf[0] = arg1;
                buf[1] = arg2;
                buf[2] = arg3;
                Construct(ctx, buf, ref result);
                return result;
            }
            return base.Invoke(ctx, arg1, arg2, arg3);
        }

        internal override ScriptDatum Invoke(ScriptContext ctx, ScriptDatum arg1, ScriptDatum arg2, ScriptDatum arg3, ScriptDatum arg4)
        {
            if (Callable)
            {
                ScriptDatum result = default;
                DatumBuffer4 buf = default;
                buf[0] = arg1;
                buf[1] = arg2;
                buf[2] = arg3;
                buf[3] = arg4;
                Construct(ctx, buf, ref result);
                return result;
            }
            return base.Invoke(ctx, arg1, arg2, arg3, arg4);
        }

        internal override ScriptDatum Invoke(ScriptContext ctx, ScriptDatum arg1, ScriptDatum arg2, ScriptDatum arg3, ScriptDatum arg4, ScriptDatum arg5)
        {
            if (Callable)
            {
                ScriptDatum result = default;
                DatumBuffer5 buf = default;
                buf[0] = arg1;
                buf[1] = arg2;
                buf[2] = arg3;
                buf[3] = arg4;
                buf[4] = arg5;
                Construct(ctx, buf, ref result);
                return result;
            }
            return base.Invoke(ctx, arg1, arg2, arg3, arg4, arg5);
        }

        internal override ScriptDatum Invoke(ScriptContext ctx, ScriptDatum arg1, ScriptDatum arg2, ScriptDatum arg3, ScriptDatum arg4, ScriptDatum arg5, ScriptDatum arg6)
        {
            if (Callable)
            {
                ScriptDatum result = default;
                DatumBuffer6 buf = default;
                buf[0] = arg1;
                buf[1] = arg2;
                buf[2] = arg3;
                buf[3] = arg4;
                buf[4] = arg5;
                buf[5] = arg6;
                Construct(ctx, buf, ref result);
                return result;
            }
            return base.Invoke(ctx, arg1, arg2, arg3, arg4, arg5, arg6);
        }

        internal override ScriptDatum Invoke(ScriptContext ctx, ScriptDatum arg1, ScriptDatum arg2, ScriptDatum arg3, ScriptDatum arg4, ScriptDatum arg5, ScriptDatum arg6, ScriptDatum arg7)
        {
            if (Callable)
            {
                ScriptDatum result = default;
                DatumBuffer7 buf = default;
                buf[0] = arg1;
                buf[1] = arg2;
                buf[2] = arg3;
                buf[3] = arg4;
                buf[4] = arg5;
                buf[5] = arg6;
                buf[6] = arg7;
                Construct(ctx, buf, ref result);
                return result;
            }
            return base.Invoke(ctx, arg1, arg2, arg3, arg4, arg5, arg6, arg7);
        }

        internal override ScriptDatum Invoke(ScriptContext ctx, ScriptDatum arg1, ScriptDatum arg2, ScriptDatum arg3, ScriptDatum arg4, ScriptDatum arg5, ScriptDatum arg6, ScriptDatum arg7, ScriptDatum arg8)
        {
            if (Callable)
            {
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
                Construct(ctx, buf, ref result);
                return result;
            }
            return base.Invoke(ctx, arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8);
        }

        /// <summary>
        /// Concrete types must implement this to handle instance construction.
        /// </summary>
        /// <param name="ctx">The execution context.</param>
        /// <param name="args">Arguments passed during construction.</param>
        /// <param name="result">The result of the construction.</param>
        public abstract void Construct(ScriptContext ctx, Span<ScriptDatum> args, ref ScriptDatum result);

        /// <summary> Returns a string representation of the script type. </summary>
        public override string ToString()
        {
            return "Type: " + Name;
        }
    }
}
