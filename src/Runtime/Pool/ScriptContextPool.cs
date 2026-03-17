using AuroraScript.Runtime.Types;
using AuroraScript.Runtime;
using System.Collections.Concurrent;
using System.Runtime.CompilerServices;

namespace AuroraScript.Runtime.Pool
{
    internal sealed class ScriptContextPool
    {
        private readonly ConcurrentStack<ScriptContext> _pool = new();

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ScriptContext Rent(ScriptDomain domain, ScriptObject userState, ScriptModule module, ClosureFunction closure)
        {
            if (_pool.TryPop(out var ctx))
            {
                ctx.Reset(domain, userState, module, closure);
                return ctx;
            }

            return new ScriptContext(domain, userState, module, closure);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Return(ScriptContext ctx)
        {
            // keep minimal cleanup; Reset will be called on the next rent
            _pool.Push(ctx);
        }
    }
}
