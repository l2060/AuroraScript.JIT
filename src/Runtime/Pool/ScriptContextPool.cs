using AuroraScript.Runtime.Types;
using AuroraScript.Runtime;
using System.Runtime.CompilerServices;
using System.Threading;

namespace AuroraScript.Runtime.Pool
{
    internal sealed class ScriptContextPool
    {
        private ScriptContext _head;

        public ScriptContext Rent(ScriptDomain domain, ScriptObject userState, ScriptModule module, ClosureFunction closure)
        {
            while (true)
            {
                var head = _head;
                if (head == null)
                {
                    return new ScriptContext(domain, userState, module, closure);
                }

                var next = head.PoolNext;
                if (Interlocked.CompareExchange(ref _head, next, head) != head)
                {
                    continue;
                }

                head.PoolNext = null;
                var ctx = head;
                ctx.Reset(domain, userState, module, closure);
                return ctx;
            }
        }

        public void Return(ScriptContext ctx)
        {
            while (true)
            {
                var head = _head;
                ctx.PoolNext = head;
                if (Interlocked.CompareExchange(ref _head, ctx, head) == head)
                {
                    return;
                }
            }
        }
    }
}
