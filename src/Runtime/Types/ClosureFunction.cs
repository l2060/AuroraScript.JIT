using System;

namespace AuroraScript.Runtime.Types
{
    /// <summary>
    /// Represents a closure function, containing the function bytecode and its captured environment.
    /// </summary>
    public sealed class ClosureFunction : ScriptObject
    {
        internal readonly ScriptDomain Domain;
        private readonly Delegate targetDelegate;
        private readonly byte fastArity;

        /// <summary>The function name used for diagnostics.</summary>
        public readonly string FuncName;

        /// <summary>The module object this closure belongs to.</summary>
        public readonly ScriptModule Module;
        internal readonly Upvalue[] Upvalues;

        internal ClosureFunction(ScriptDomain domain, ScriptModule module, ScriptFunctionDelegate targetDelegate, Upvalue[] upvalues, string funcName = null)
            : this(domain, module, targetDelegate, 255, upvalues, funcName)
        {
        }

        internal ClosureFunction(ScriptDomain domain, ScriptModule module, ScriptFunctionDelegate0 targetDelegate, Upvalue[] upvalues, string funcName = null)
            : this(domain, module, targetDelegate, 0, upvalues, funcName)
        {
        }

        internal ClosureFunction(ScriptDomain domain, ScriptModule module, ScriptFunctionDelegate1 targetDelegate, Upvalue[] upvalues, string funcName = null)
            : this(domain, module, targetDelegate, 1, upvalues, funcName)
        {
        }

        internal ClosureFunction(ScriptDomain domain, ScriptModule module, ScriptFunctionDelegate2 targetDelegate, Upvalue[] upvalues, string funcName = null)
            : this(domain, module, targetDelegate, 2, upvalues, funcName)
        {
        }

        internal ClosureFunction(ScriptDomain domain, ScriptModule module, ScriptFunctionDelegate3 targetDelegate, Upvalue[] upvalues, string funcName = null)
            : this(domain, module, targetDelegate, 3, upvalues, funcName)
        {
        }

        internal ClosureFunction(ScriptDomain domain, ScriptModule module, ScriptFunctionDelegate4 targetDelegate, Upvalue[] upvalues, string funcName = null)
            : this(domain, module, targetDelegate, 4, upvalues, funcName)
        {
        }

        internal ClosureFunction(ScriptDomain domain, ScriptModule module, ScriptFunctionDelegate5 targetDelegate, Upvalue[] upvalues, string funcName = null)
            : this(domain, module, targetDelegate, 5, upvalues, funcName)
        {
        }

        internal ClosureFunction(ScriptDomain domain, ScriptModule module, ScriptFunctionDelegate6 targetDelegate, Upvalue[] upvalues, string funcName = null)
            : this(domain, module, targetDelegate, 6, upvalues, funcName)
        {
        }

        internal ClosureFunction(ScriptDomain domain, ScriptModule module, ScriptFunctionDelegate7 targetDelegate, Upvalue[] upvalues, string funcName = null)
            : this(domain, module, targetDelegate, 7, upvalues, funcName)
        {
        }

        private ClosureFunction(ScriptDomain domain, ScriptModule module, Delegate targetDelegate, byte fastArity, Upvalue[] upvalues, string funcName)
        {
            Domain = domain;
            Module = module;
            this.targetDelegate = targetDelegate ?? throw new ArgumentNullException(nameof(targetDelegate));
            this.fastArity = fastArity;
            Upvalues = upvalues;
            FuncName = funcName;
        }

        internal override ScriptDatum Invoke(ScriptContext ctx, Span<ScriptDatum> args)
        {
            return InvokeArray(ctx, args);
        }

        internal override ScriptDatum Invoke(ScriptContext ctx)
        {
            return Invoke0(ctx);
        }

        internal override ScriptDatum Invoke(ScriptContext ctx, ScriptDatum arg1)
        {
            return Invoke1(ctx, arg1);
        }

        internal override ScriptDatum Invoke(ScriptContext ctx, ScriptDatum arg1, ScriptDatum arg2)
        {
            return Invoke2(ctx, arg1, arg2);
        }

        internal override ScriptDatum Invoke(ScriptContext ctx, ScriptDatum arg1, ScriptDatum arg2, ScriptDatum arg3)
        {
            return Invoke3(ctx, arg1, arg2, arg3);
        }

        internal override ScriptDatum Invoke(ScriptContext ctx, ScriptDatum arg1, ScriptDatum arg2, ScriptDatum arg3, ScriptDatum arg4)
        {
            return Invoke4(ctx, arg1, arg2, arg3, arg4);
        }

        internal override ScriptDatum Invoke(ScriptContext ctx, ScriptDatum arg1, ScriptDatum arg2, ScriptDatum arg3, ScriptDatum arg4, ScriptDatum arg5)
        {
            return Invoke5(ctx, arg1, arg2, arg3, arg4, arg5);
        }

        internal override ScriptDatum Invoke(ScriptContext ctx, ScriptDatum arg1, ScriptDatum arg2, ScriptDatum arg3, ScriptDatum arg4, ScriptDatum arg5, ScriptDatum arg6)
        {
            return Invoke6(ctx, arg1, arg2, arg3, arg4, arg5, arg6);
        }

        internal override ScriptDatum Invoke(ScriptContext ctx, ScriptDatum arg1, ScriptDatum arg2, ScriptDatum arg3, ScriptDatum arg4, ScriptDatum arg5, ScriptDatum arg6, ScriptDatum arg7)
        {
            return Invoke7(ctx, arg1, arg2, arg3, arg4, arg5, arg6, arg7);
        }

        internal ScriptDatum Invoke0(ScriptContext ctx)
        {
            var context = ctx.With(Module, this);
            return fastArity switch
            {
                0 => ((ScriptFunctionDelegate0)targetDelegate).Invoke(context),
                1 => ((ScriptFunctionDelegate1)targetDelegate).Invoke(context, ScriptDatum.Null),
                2 => ((ScriptFunctionDelegate2)targetDelegate).Invoke(context, ScriptDatum.Null, ScriptDatum.Null),
                3 => ((ScriptFunctionDelegate3)targetDelegate).Invoke(context, ScriptDatum.Null, ScriptDatum.Null, ScriptDatum.Null),
                4 => ((ScriptFunctionDelegate4)targetDelegate).Invoke(context, ScriptDatum.Null, ScriptDatum.Null, ScriptDatum.Null, ScriptDatum.Null),
                5 => ((ScriptFunctionDelegate5)targetDelegate).Invoke(context, ScriptDatum.Null, ScriptDatum.Null, ScriptDatum.Null, ScriptDatum.Null, ScriptDatum.Null),
                6 => ((ScriptFunctionDelegate6)targetDelegate).Invoke(context, ScriptDatum.Null, ScriptDatum.Null, ScriptDatum.Null, ScriptDatum.Null, ScriptDatum.Null, ScriptDatum.Null),
                7 => ((ScriptFunctionDelegate7)targetDelegate).Invoke(context, ScriptDatum.Null, ScriptDatum.Null, ScriptDatum.Null, ScriptDatum.Null, ScriptDatum.Null, ScriptDatum.Null, ScriptDatum.Null),
                _ => ((ScriptFunctionDelegate)targetDelegate).Invoke(context, Array.Empty<ScriptDatum>())
            };
        }

        internal ScriptDatum Invoke1(ScriptContext ctx, ScriptDatum arg0)
        {
            var context = ctx.With(Module, this);
            return fastArity switch
            {
                0 => ((ScriptFunctionDelegate0)targetDelegate).Invoke(context),
                1 => ((ScriptFunctionDelegate1)targetDelegate).Invoke(context, arg0),
                2 => ((ScriptFunctionDelegate2)targetDelegate).Invoke(context, arg0, ScriptDatum.Null),
                3 => ((ScriptFunctionDelegate3)targetDelegate).Invoke(context, arg0, ScriptDatum.Null, ScriptDatum.Null),
                4 => ((ScriptFunctionDelegate4)targetDelegate).Invoke(context, arg0, ScriptDatum.Null, ScriptDatum.Null, ScriptDatum.Null),
                5 => ((ScriptFunctionDelegate5)targetDelegate).Invoke(context, arg0, ScriptDatum.Null, ScriptDatum.Null, ScriptDatum.Null, ScriptDatum.Null),
                6 => ((ScriptFunctionDelegate6)targetDelegate).Invoke(context, arg0, ScriptDatum.Null, ScriptDatum.Null, ScriptDatum.Null, ScriptDatum.Null, ScriptDatum.Null),
                7 => ((ScriptFunctionDelegate7)targetDelegate).Invoke(context, arg0, ScriptDatum.Null, ScriptDatum.Null, ScriptDatum.Null, ScriptDatum.Null, ScriptDatum.Null, ScriptDatum.Null),
                _ => ((ScriptFunctionDelegate)targetDelegate).Invoke(context, [arg0])
            };
        }

        internal ScriptDatum Invoke2(ScriptContext ctx, ScriptDatum arg0, ScriptDatum arg1)
        {
            var context = ctx.With(Module, this);
            return fastArity switch
            {
                0 => ((ScriptFunctionDelegate0)targetDelegate).Invoke(context),
                1 => ((ScriptFunctionDelegate1)targetDelegate).Invoke(context, arg0),
                2 => ((ScriptFunctionDelegate2)targetDelegate).Invoke(context, arg0, arg1),
                3 => ((ScriptFunctionDelegate3)targetDelegate).Invoke(context, arg0, arg1, ScriptDatum.Null),
                4 => ((ScriptFunctionDelegate4)targetDelegate).Invoke(context, arg0, arg1, ScriptDatum.Null, ScriptDatum.Null),
                5 => ((ScriptFunctionDelegate5)targetDelegate).Invoke(context, arg0, arg1, ScriptDatum.Null, ScriptDatum.Null, ScriptDatum.Null),
                6 => ((ScriptFunctionDelegate6)targetDelegate).Invoke(context, arg0, arg1, ScriptDatum.Null, ScriptDatum.Null, ScriptDatum.Null, ScriptDatum.Null),
                7 => ((ScriptFunctionDelegate7)targetDelegate).Invoke(context, arg0, arg1, ScriptDatum.Null, ScriptDatum.Null, ScriptDatum.Null, ScriptDatum.Null, ScriptDatum.Null),
                _ => ((ScriptFunctionDelegate)targetDelegate).Invoke(context, [arg0, arg1])
            };
        }

        internal ScriptDatum Invoke3(ScriptContext ctx, ScriptDatum arg0, ScriptDatum arg1, ScriptDatum arg2)
        {
            var context = ctx.With(Module, this);
            return fastArity switch
            {
                0 => ((ScriptFunctionDelegate0)targetDelegate).Invoke(context),
                1 => ((ScriptFunctionDelegate1)targetDelegate).Invoke(context, arg0),
                2 => ((ScriptFunctionDelegate2)targetDelegate).Invoke(context, arg0, arg1),
                3 => ((ScriptFunctionDelegate3)targetDelegate).Invoke(context, arg0, arg1, arg2),
                4 => ((ScriptFunctionDelegate4)targetDelegate).Invoke(context, arg0, arg1, arg2, ScriptDatum.Null),
                5 => ((ScriptFunctionDelegate5)targetDelegate).Invoke(context, arg0, arg1, arg2, ScriptDatum.Null, ScriptDatum.Null),
                6 => ((ScriptFunctionDelegate6)targetDelegate).Invoke(context, arg0, arg1, arg2, ScriptDatum.Null, ScriptDatum.Null, ScriptDatum.Null),
                7 => ((ScriptFunctionDelegate7)targetDelegate).Invoke(context, arg0, arg1, arg2, ScriptDatum.Null, ScriptDatum.Null, ScriptDatum.Null, ScriptDatum.Null),
                _ => ((ScriptFunctionDelegate)targetDelegate).Invoke(context, [arg0, arg1, arg2])
            };
        }

        internal ScriptDatum Invoke4(ScriptContext ctx, ScriptDatum arg0, ScriptDatum arg1, ScriptDatum arg2, ScriptDatum arg3)
        {
            var context = ctx.With(Module, this);
            return fastArity switch
            {
                0 => ((ScriptFunctionDelegate0)targetDelegate).Invoke(context),
                1 => ((ScriptFunctionDelegate1)targetDelegate).Invoke(context, arg0),
                2 => ((ScriptFunctionDelegate2)targetDelegate).Invoke(context, arg0, arg1),
                3 => ((ScriptFunctionDelegate3)targetDelegate).Invoke(context, arg0, arg1, arg2),
                4 => ((ScriptFunctionDelegate4)targetDelegate).Invoke(context, arg0, arg1, arg2, arg3),
                5 => ((ScriptFunctionDelegate5)targetDelegate).Invoke(context, arg0, arg1, arg2, arg3, ScriptDatum.Null),
                6 => ((ScriptFunctionDelegate6)targetDelegate).Invoke(context, arg0, arg1, arg2, arg3, ScriptDatum.Null, ScriptDatum.Null),
                7 => ((ScriptFunctionDelegate7)targetDelegate).Invoke(context, arg0, arg1, arg2, arg3, ScriptDatum.Null, ScriptDatum.Null, ScriptDatum.Null),
                _ => ((ScriptFunctionDelegate)targetDelegate).Invoke(context, [arg0, arg1, arg2, arg3])
            };
        }

        internal ScriptDatum Invoke5(ScriptContext ctx, ScriptDatum arg0, ScriptDatum arg1, ScriptDatum arg2, ScriptDatum arg3, ScriptDatum arg4)
        {
            var context = ctx.With(Module, this);
            return fastArity switch
            {
                0 => ((ScriptFunctionDelegate0)targetDelegate).Invoke(context),
                1 => ((ScriptFunctionDelegate1)targetDelegate).Invoke(context, arg0),
                2 => ((ScriptFunctionDelegate2)targetDelegate).Invoke(context, arg0, arg1),
                3 => ((ScriptFunctionDelegate3)targetDelegate).Invoke(context, arg0, arg1, arg2),
                4 => ((ScriptFunctionDelegate4)targetDelegate).Invoke(context, arg0, arg1, arg2, arg3),
                5 => ((ScriptFunctionDelegate5)targetDelegate).Invoke(context, arg0, arg1, arg2, arg3, arg4),
                6 => ((ScriptFunctionDelegate6)targetDelegate).Invoke(context, arg0, arg1, arg2, arg3, arg4, ScriptDatum.Null),
                7 => ((ScriptFunctionDelegate7)targetDelegate).Invoke(context, arg0, arg1, arg2, arg3, arg4, ScriptDatum.Null, ScriptDatum.Null),
                _ => ((ScriptFunctionDelegate)targetDelegate).Invoke(context, [arg0, arg1, arg2, arg3, arg4])
            };
        }

        internal ScriptDatum Invoke6(ScriptContext ctx, ScriptDatum arg0, ScriptDatum arg1, ScriptDatum arg2, ScriptDatum arg3, ScriptDatum arg4, ScriptDatum arg5)
        {
            var context = ctx.With(Module, this);
            return fastArity switch
            {
                0 => ((ScriptFunctionDelegate0)targetDelegate).Invoke(context),
                1 => ((ScriptFunctionDelegate1)targetDelegate).Invoke(context, arg0),
                2 => ((ScriptFunctionDelegate2)targetDelegate).Invoke(context, arg0, arg1),
                3 => ((ScriptFunctionDelegate3)targetDelegate).Invoke(context, arg0, arg1, arg2),
                4 => ((ScriptFunctionDelegate4)targetDelegate).Invoke(context, arg0, arg1, arg2, arg3),
                5 => ((ScriptFunctionDelegate5)targetDelegate).Invoke(context, arg0, arg1, arg2, arg3, arg4),
                6 => ((ScriptFunctionDelegate6)targetDelegate).Invoke(context, arg0, arg1, arg2, arg3, arg4, arg5),
                7 => ((ScriptFunctionDelegate7)targetDelegate).Invoke(context, arg0, arg1, arg2, arg3, arg4, arg5, ScriptDatum.Null),
                _ => ((ScriptFunctionDelegate)targetDelegate).Invoke(context, [arg0, arg1, arg2, arg3, arg4, arg5])
            };
        }

        internal ScriptDatum Invoke7(ScriptContext ctx, ScriptDatum arg0, ScriptDatum arg1, ScriptDatum arg2, ScriptDatum arg3, ScriptDatum arg4, ScriptDatum arg5, ScriptDatum arg6)
        {
            var context = ctx.With(Module, this);
            return fastArity switch
            {
                0 => ((ScriptFunctionDelegate0)targetDelegate).Invoke(context),
                1 => ((ScriptFunctionDelegate1)targetDelegate).Invoke(context, arg0),
                2 => ((ScriptFunctionDelegate2)targetDelegate).Invoke(context, arg0, arg1),
                3 => ((ScriptFunctionDelegate3)targetDelegate).Invoke(context, arg0, arg1, arg2),
                4 => ((ScriptFunctionDelegate4)targetDelegate).Invoke(context, arg0, arg1, arg2, arg3),
                5 => ((ScriptFunctionDelegate5)targetDelegate).Invoke(context, arg0, arg1, arg2, arg3, arg4),
                6 => ((ScriptFunctionDelegate6)targetDelegate).Invoke(context, arg0, arg1, arg2, arg3, arg4, arg5),
                7 => ((ScriptFunctionDelegate7)targetDelegate).Invoke(context, arg0, arg1, arg2, arg3, arg4, arg5, arg6),
                _ => ((ScriptFunctionDelegate)targetDelegate).Invoke(context, [arg0, arg1, arg2, arg3, arg4, arg5, arg6])
            };
        }

        /// <summary>Invokes the closure from CLR code and wraps script stack information on failure.</summary>
        public ScriptDatum InvokeClr(ScriptContext ctx, params ScriptDatum[] args)
        {
            try
            {
                return InvokeArray(ctx, args);
            }
            catch (Exception ex)
            {
                throw new AuroraRuntimeException(ex, ctx.StackTrace());
            }
        }

        private ScriptDatum InvokeArray(ScriptContext ctx, Span<ScriptDatum> args)
        {
            var context = ctx.With(Module, this);
            return fastArity switch
            {
                0 => ((ScriptFunctionDelegate0)targetDelegate).Invoke(context),
                1 => ((ScriptFunctionDelegate1)targetDelegate).Invoke(context, GetArg(args, 0)),
                2 => ((ScriptFunctionDelegate2)targetDelegate).Invoke(context, GetArg(args, 0), GetArg(args, 1)),
                3 => ((ScriptFunctionDelegate3)targetDelegate).Invoke(context, GetArg(args, 0), GetArg(args, 1), GetArg(args, 2)),
                4 => ((ScriptFunctionDelegate4)targetDelegate).Invoke(context, GetArg(args, 0), GetArg(args, 1), GetArg(args, 2), GetArg(args, 3)),
                5 => ((ScriptFunctionDelegate5)targetDelegate).Invoke(context, GetArg(args, 0), GetArg(args, 1), GetArg(args, 2), GetArg(args, 3), GetArg(args, 4)),
                6 => ((ScriptFunctionDelegate6)targetDelegate).Invoke(context, GetArg(args, 0), GetArg(args, 1), GetArg(args, 2), GetArg(args, 3), GetArg(args, 4), GetArg(args, 5)),
                7 => ((ScriptFunctionDelegate7)targetDelegate).Invoke(context, GetArg(args, 0), GetArg(args, 1), GetArg(args, 2), GetArg(args, 3), GetArg(args, 4), GetArg(args, 5), GetArg(args, 6)),
                _ => ((ScriptFunctionDelegate)targetDelegate).Invoke(context, args)
            };
        }

        private static ScriptDatum GetArg(Span<ScriptDatum> args, int index)
        {
            return index >= 0 && index < args.Length ? args[index] : ScriptDatum.Null;
        }

        /// <summary>Returns a readable function name for diagnostics.</summary>
        public override string ToString()
        {
            return $"<function {(string.IsNullOrEmpty(FuncName) ? "anonymous" : FuncName)}>";
        }
    }
}
