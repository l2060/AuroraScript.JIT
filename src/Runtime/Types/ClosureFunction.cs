using System;

namespace AuroraScript.Runtime.Types
{
    /// <summary>
    /// Represents a closure function, containing the function bytecode (as a delegate) and its captured environment.
    /// A closure function can access environment variables from the scope where it was defined, even if called outside that scope.
    /// It is the runtime representation of functions in AuroraScript, implementing function invocation and closure features.
    /// </summary>
    public sealed class ClosureFunction : ScriptObject
    {
        /// <summary>
        /// The script domain this function belongs to.
        /// </summary>
        internal readonly ScriptDomain Domain;

        /// <summary>
        /// The compiled CIL delegate pointing to the underlying method.
        /// </summary>
        public readonly ScriptFunctionDelegate TargetDelegate;

        /// <summary>
        /// The name of the function (optional).
        /// Used for debugging and error reporting. Empty for anonymous functions.
        /// </summary>
        public readonly string FuncName;

        /// <summary>
        /// The module object this closure belongs to.
        /// Stores module-level variables and functions, serving as the closure's context environment.
        /// </summary>
        public readonly ScriptModule Module;

        /// <summary>
        /// The set of upvalues (captured variables) for this closure.
        /// </summary>
        internal readonly Upvalue[] Upvalues;

        /// <summary>
        /// Initializes a new instance of the <see cref="ClosureFunction"/> class.
        /// </summary>
        /// <param name="domain">The script domain.</param>
        /// <param name="module">The module object this closure belongs to.</param>
        /// <param name="targetDelegate">The compiled CIL delegate.</param>
        /// <param name="upvalues">The set of captured upvalues.</param>
        /// <param name="funcName">The function name (optional, null for anonymous functions).</param>
        internal ClosureFunction(ScriptDomain domain, ScriptModule module, ScriptFunctionDelegate targetDelegate, Upvalue[] upvalues, string funcName = null)
        {
            Domain = domain;
            Module = module;
            TargetDelegate = targetDelegate;
            Upvalues = upvalues;
            FuncName = funcName;
        }

        /// <summary>
        /// Invokes the function within the specified script context.
        /// </summary>
        /// <param name="ctx">The execution context.</param>
        /// <param name="args">The arguments to pass to the function.</param>
        /// <returns>The result of the function execution.</returns>
        internal override ScriptDatum Invoke(ScriptContext ctx, Span<ScriptDatum> args)
        {
            var context = ctx.With(Module, this);
            try
            {
                return TargetDelegate.Invoke(context, args);
            }
            finally
            {
                context.Release();
            }
        }

        internal override ScriptDatum Invoke(ScriptContext ctx)
        {
            var context = ctx.With(Module, this);
            try
            {
                return TargetDelegate.Invoke(context, Span<ScriptDatum>.Empty);
            }
            finally
            {
                context.Release();
            }
        }

        internal override ScriptDatum Invoke(ScriptContext ctx, ScriptDatum arg1)
        {
            var context = ctx.With(Module, this);
            DatumBuffer1 buf = default;
            buf[0] = arg1;
            try
            {
                return TargetDelegate.Invoke(context, buf);
            }
            finally
            {
                context.Release();
            }
        }

        internal override ScriptDatum Invoke(ScriptContext ctx, ScriptDatum arg1, ScriptDatum arg2)
        {
            var context = ctx.With(Module, this);
            DatumBuffer2 buf = default;
            buf[0] = arg1;
            buf[1] = arg2;
            try
            {
                return TargetDelegate.Invoke(context, buf);
            }
            finally
            {
                context.Release();
            }
        }

        internal override ScriptDatum Invoke(ScriptContext ctx, ScriptDatum arg1, ScriptDatum arg2, ScriptDatum arg3)
        {
            var context = ctx.With(Module, this);
            DatumBuffer3 buf = default;
            buf[0] = arg1;
            buf[1] = arg2;
            buf[2] = arg3;
            try
            {
                return TargetDelegate.Invoke(context, buf);
            }
            finally
            {
                context.Release();
            }
        }

        internal override ScriptDatum Invoke(ScriptContext ctx, ScriptDatum arg1, ScriptDatum arg2, ScriptDatum arg3, ScriptDatum arg4)
        {
            var context = ctx.With(Module, this);
            DatumBuffer4 buf = default;
            buf[0] = arg1;
            buf[1] = arg2;
            buf[2] = arg3;
            buf[3] = arg4;
            try
            {
                return TargetDelegate.Invoke(context, buf);
            }
            finally
            {
                context.Release();
            }
        }

        internal override ScriptDatum Invoke(ScriptContext ctx, ScriptDatum arg1, ScriptDatum arg2, ScriptDatum arg3, ScriptDatum arg4, ScriptDatum arg5)
        {
            var context = ctx.With(Module, this);
            DatumBuffer5 buf = default;
            buf[0] = arg1;
            buf[1] = arg2;
            buf[2] = arg3;
            buf[3] = arg4;
            buf[4] = arg5;
            try
            {
                return TargetDelegate.Invoke(context, buf);
            }
            finally
            {
                context.Release();
            }
        }

        internal override ScriptDatum Invoke(ScriptContext ctx, ScriptDatum arg1, ScriptDatum arg2, ScriptDatum arg3, ScriptDatum arg4, ScriptDatum arg5, ScriptDatum arg6)
        {
            var context = ctx.With(Module, this);
            DatumBuffer6 buf = default;
            buf[0] = arg1;
            buf[1] = arg2;
            buf[2] = arg3;
            buf[3] = arg4;
            buf[4] = arg5;
            buf[5] = arg6;
            try
            {
                return TargetDelegate.Invoke(context, buf);
            }
            finally
            {
                context.Release();
            }
        }

        internal override ScriptDatum Invoke(ScriptContext ctx, ScriptDatum arg1, ScriptDatum arg2, ScriptDatum arg3, ScriptDatum arg4, ScriptDatum arg5, ScriptDatum arg6, ScriptDatum arg7)
        {
            var context = ctx.With(Module, this);
            DatumBuffer7 buf = default;
            buf[0] = arg1;
            buf[1] = arg2;
            buf[2] = arg3;
            buf[3] = arg4;
            buf[4] = arg5;
            buf[5] = arg6;
            buf[6] = arg7;
            try
            {
                return TargetDelegate.Invoke(context, buf);
            }
            finally
            {
                context.Release();
            }
        }

        internal override ScriptDatum Invoke(ScriptContext ctx, ScriptDatum arg1, ScriptDatum arg2, ScriptDatum arg3, ScriptDatum arg4, ScriptDatum arg5, ScriptDatum arg6, ScriptDatum arg7, ScriptDatum arg8)
        {
            var context = ctx.With(Module, this);
            DatumBuffer8 buf = default;
            buf[0] = arg1;
            buf[1] = arg2;
            buf[2] = arg3;
            buf[3] = arg4;
            buf[4] = arg5;
            buf[5] = arg6;
            buf[6] = arg7;
            buf[7] = arg8;
            try
            {
                return TargetDelegate.Invoke(context, buf);
            }
            finally
            {
                context.Release();
            }
        }

        /// <summary>
        /// Invokes the script closure method directly from the CLR.
        /// Handles exceptions and builds a script-side stack trace if an error occurs.
        /// </summary>
        /// <param name="ctx">The execution context.</param>
        /// <param name="args">The arguments to pass to the function.</param>
        /// <returns>The result of the function execution.</returns>
        /// <exception cref="AuroraRuntimeException">Thrown if an error occurs during script execution.</exception>
        public ScriptDatum InvokeClr(ScriptContext ctx, params ScriptDatum[] args)
        {
            var context = ctx.With(Module, this);
            try
            {
                return TargetDelegate.Invoke(context, args);
            }
            catch (Exception ex)
            {
                throw new AuroraRuntimeException(ex, ctx.StackTrace());
            }
            finally
            {
                context.Release();
            }
        }

        /// <summary>
        /// Returns a string representation of the closure.
        /// Used for debugging and error reporting.
        /// </summary>
        /// <returns>A string representation including the function name or "anonymous".</returns>
        public override string ToString()
        {
            return $"<function {(string.IsNullOrEmpty(FuncName) ? "anonymous" : FuncName)}>";
        }
    }
}
