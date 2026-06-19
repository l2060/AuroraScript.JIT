using AuroraScript.Runtime.Interop;
using AuroraScript.Runtime.Types;
using System;

namespace AuroraScript.Runtime
{
    /// <summary>
    /// Represents a compiled lightweight script block.
    /// </summary>
    public sealed class CompiledBlock
    {
        private readonly AuroraEngine _engine;
        private readonly ScriptFunctionDelegate _target;

        internal CompiledBlock(AuroraEngine engine, ScriptFunctionDelegate target)
        {
            _engine = engine;
            _target = target;
        }

        /// <summary>
        /// Invokes the compiled block in the specified domain with raw script arguments.
        /// </summary>
        public ScriptDatum Invoke(ScriptDomain domain, params ScriptDatum[] arguments)
        {
            var ctx = domain.ContextPool.Rent(domain, domain.UserState, null, null);
            try
            {
                return _target(ctx, arguments);
            }
            finally
            {
                ctx.Release();
            }
        }

        /// <summary>
        /// Invokes the compiled block in the specified domain with script object arguments.
        /// </summary>
        public ScriptDatum Invoke(ScriptDomain domain, params ScriptObject[] arguments)
        {
            return Invoke(domain, ClrMarshaller.ToDatums(arguments));
        }

        /// <summary>
        /// Invokes the compiled block in a new empty domain with raw script arguments.
        /// </summary>
        public ScriptDatum Invoke(params ScriptDatum[] arguments)
        {
            var domain = _engine.CreateEmptyDomain(null);
            return Invoke(domain, arguments);
        }

        /// <summary>
        /// Invokes the compiled block in a new empty domain with script object arguments.
        /// </summary>
        public ScriptDatum Invoke(params ScriptObject[] arguments)
        {
            return Invoke(ClrMarshaller.ToDatums(arguments));
        }

        /// <summary>
        /// Invokes the compiled block using an existing script context.
        /// </summary>
        public ScriptDatum Invoke(ScriptContext context, ReadOnlySpan<ScriptDatum> arguments)
        {
            var args = arguments.ToArray();
            return _target(context, args);
        }
    }
}
