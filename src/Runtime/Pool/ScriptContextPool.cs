using AuroraScript.Runtime.Types;
using AuroraScript.Runtime;

namespace AuroraScript.Runtime.Pool
{
    internal sealed class ScriptContextPool
    {
        private readonly object _syncRoot = new();
        private ScriptContext _head;

        public ScriptContext Rent(ScriptDomain domain, ScriptObject userState, ScriptModule module, ClosureFunction closure)
        {
            ScriptContext ctx;
            lock (_syncRoot)
            {
                ctx = _head;
                if (ctx != null)
                {
                    _head = ctx.PoolNext;
                    ctx.PoolNext = null;
                }
            }

            if (ctx == null)
            {
                return new ScriptContext(domain, userState, module, closure);
            }

            ctx.Reset(domain, userState, module, closure);
            return ctx;
        }

        public void Return(ScriptContext ctx)
        {
            lock (_syncRoot)
            {
                ctx.PoolNext = _head;
                _head = ctx;
            }
        }
    }
}
