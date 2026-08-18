using AuroraScript.Core;
using AuroraScript.Runtime.Types;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace AuroraScript.Runtime
{
    /// <summary>
    /// A delegate representing a native or script-based function that can be invoked within the AuroraScript runtime.
    /// </summary>
    /// <param name="ctx">The execution context for the function call.</param>
    /// <param name="args">The arguments passed to the function.</param>
    /// <returns>A <see cref="ScriptDatum"/> representing the result of the function execution.</returns>
    public delegate ScriptDatum ScriptFunctionDelegate(ScriptContext ctx, Span<ScriptDatum> args);

    /// <summary>
    /// Represents a compiled script function that accepts no explicit arguments.
    /// </summary>
    /// <param name="ctx">The execution context for the function call.</param>
    /// <returns>A <see cref="ScriptDatum"/> representing the result of the function execution.</returns>
    public delegate ScriptDatum ScriptFunctionDelegate0(ScriptContext ctx);

    /// <summary>
    /// Represents a compiled script function that accepts one explicit argument.
    /// </summary>
    /// <param name="ctx">The execution context for the function call.</param>
    /// <param name="arg0">The first script argument.</param>
    /// <returns>A <see cref="ScriptDatum"/> representing the result of the function execution.</returns>
    public delegate ScriptDatum ScriptFunctionDelegate1(ScriptContext ctx, ScriptDatum arg0);

    /// <summary>
    /// Represents a compiled script function that accepts two explicit arguments.
    /// </summary>
    public delegate ScriptDatum ScriptFunctionDelegate2(ScriptContext ctx, ScriptDatum arg0, ScriptDatum arg1);

    /// <summary>
    /// Represents a compiled script function that accepts three explicit arguments.
    /// </summary>
    public delegate ScriptDatum ScriptFunctionDelegate3(ScriptContext ctx, ScriptDatum arg0, ScriptDatum arg1, ScriptDatum arg2);

    /// <summary>
    /// Represents a compiled script function that accepts four explicit arguments.
    /// </summary>
    public delegate ScriptDatum ScriptFunctionDelegate4(ScriptContext ctx, ScriptDatum arg0, ScriptDatum arg1, ScriptDatum arg2, ScriptDatum arg3);

    /// <summary>
    /// Represents a compiled script function that accepts five explicit arguments.
    /// </summary>
    public delegate ScriptDatum ScriptFunctionDelegate5(ScriptContext ctx, ScriptDatum arg0, ScriptDatum arg1, ScriptDatum arg2, ScriptDatum arg3, ScriptDatum arg4);

    /// <summary>
    /// Represents a compiled script function that accepts six explicit arguments.
    /// </summary>
    public delegate ScriptDatum ScriptFunctionDelegate6(ScriptContext ctx, ScriptDatum arg0, ScriptDatum arg1, ScriptDatum arg2, ScriptDatum arg3, ScriptDatum arg4, ScriptDatum arg5);

    /// <summary>
    /// Represents a compiled script function that accepts seven explicit arguments.
    /// </summary>
    public delegate ScriptDatum ScriptFunctionDelegate7(ScriptContext ctx, ScriptDatum arg0, ScriptDatum arg1, ScriptDatum arg2, ScriptDatum arg3, ScriptDatum arg4, ScriptDatum arg5, ScriptDatum arg6);

    /// <summary>
    /// Represents the execution context for AuroraScript.
    /// This object maintains the state for a specific function execution, including 
    /// references to the domain, global variables, current module, and captured upvalues.
    /// It is passed to every compiled script function to provide access to the runtime environment.
    /// </summary>
    public class ScriptContext
    {
        private CallFrame[] _frames;
        private int _frameCount;
        private AuroraStackTrace[] _exceptionStack;
        private bool _isActive;

        /// <summary> The script domain associated with this context. </summary>
        public ScriptDomain Domain;

        /// <summary> The engine instance that is executing the script. </summary>
        public AuroraEngine Engine;

        /// <summary> The global variables and functions available in this context. </summary>
        public ScriptGlobal Global;

        /// <summary> The current script module being executed. </summary>
        public ScriptModule Module;

        /// <summary> A user-defined state object associated with this execution context. </summary>
        public ScriptObject UserState;

        /// <summary> An array of captured values (upvalues) available to the current function. </summary>
        internal Upvalue[] Upvalues;

        /// <summary> The closure function that is the target of this execution context. </summary>
        public ClosureFunction Target;

        internal string DirectName;

        /// <summary> The next context in a linked list or stack of execution contexts (e.g., for call frames). </summary>
        public ScriptContext Next;

        /// <summary> The previous context in a linked list or stack of execution contexts (e.g., for call frames). </summary>
        public ScriptContext Previous;
        internal ScriptContext PoolNext;
        /// <summary> The current execution location or instruction pointer within the code. </summary>
        public Int64 Location;

        /// <summary>
        /// Initializes a new instance of the <see cref="ScriptContext"/> class with detailed execution parameters.
        /// </summary>
        internal ScriptContext(ScriptDomain domain, ScriptObject userState, ScriptModule module, ClosureFunction closure = null)
        {
            Reset(domain, userState, module, closure);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ScriptContext"/> class for a specific domain.
        /// </summary>
        public ScriptContext(ScriptDomain domain)
        {
            Reset(domain, domain.UserState, null, null);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ScriptContext"/> class for a domain and specific user state.
        /// </summary>
        public ScriptContext(ScriptDomain domain, ScriptObject userState)
        {
            Reset(domain, userState, null, null);
        }

        /// <summary>
        /// Creates a new child execution context with a specific module and optional closure.
        /// Sets the <see cref="Next"/> pointer to the newly created context.
        /// </summary>
        public ScriptContext With(ScriptModule module, ClosureFunction closure = null)
        {
            var next = Domain.ContextPool.Rent(Domain, UserState, module, closure);
            LinkNext(next);
            return next;
        }

        /// <summary>
        /// Creates a new child execution context for the specified closure function.
        /// Automatically resolves the module from the closure.
        /// </summary>
        public ScriptContext With(ClosureFunction closure)
        {
            var next = Domain.ContextPool.Rent(Domain, UserState, closure.Module, closure);
            LinkNext(next);
            return next;
        }

        /// <summary>
        /// Creates a new child execution context with a specific module, closure, and user state.
        /// </summary>
        public ScriptContext With(ScriptModule module, ClosureFunction closure, ScriptObject userState)
        {
            var next = Domain.ContextPool.Rent(Domain, userState ?? UserState, module, closure);
            LinkNext(next);
            return next;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void LinkNext(ScriptContext next)
        {
            if (ReferenceEquals(this, next))
            {
                // This can only happen when a context that has already been returned to
                // the pool is reused by its former owner (typically from a deferred CLR
                // callback). Restore the pool entry and fail before creating a self-cycle.
                Domain.ContextPool.Return(next);
                throw new InvalidOperationException(
                    "The ScriptContext is no longer active. Use ClosureFunction.InvokeClrDetached for deferred or asynchronous callbacks.");
            }

            this.Next = next;
            next.Previous = this;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal void Reset(ScriptDomain domain, ScriptObject userState, ScriptModule module, ClosureFunction closure)
        {
            Domain = domain;
            Engine = domain.Engine;
            Global = domain.Global;
            UserState = userState;
            Module = module;
            Target = closure;
            DirectName = null;
            Upvalues = closure?.Upvalues;
            Next = null;
            Previous = null;
            Location = 0;
            _frameCount = 0;
            _exceptionStack = null;
            _isActive = true;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal int EnterClosure(ClosureFunction closure)
        {
            ArgumentNullException.ThrowIfNull(closure);
            EnsureActive();
            var restoreDepth = PushCurrentFrame();
            Module = closure.Module;
            Target = closure;
            DirectName = null;
            Upvalues = closure.Upvalues;
            Location = 0;
            return restoreDepth;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal int EnterDirect(string name)
        {
            EnsureActive();
            var restoreDepth = PushCurrentFrame();
            Target = null;
            DirectName = name;
            Upvalues = null;
            Location = 0;
            return restoreDepth;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal int EnterModule(ScriptModule module)
        {
            EnsureActive();
            var restoreDepth = PushCurrentFrame();
            Module = module;
            Target = null;
            DirectName = null;
            Upvalues = null;
            Location = 0;
            return restoreDepth;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal void LeaveFrame(int restoreDepth)
        {
            if ((uint)restoreDepth > (uint)_frameCount)
            {
                throw new InvalidOperationException("Invalid script call frame depth.");
            }

            // Preserve the cleanup contract of the former child-context call path
            // for compatibility code that still creates linked contexts with With().
            if (Next != null)
            {
                Next.ReleaseLinked();
            }

            while (_frameCount > restoreDepth)
            {
                var index = --_frameCount;
                var frame = _frames[index];
                _frames[index] = default;
                Module = frame.Module;
                Target = frame.Target;
                DirectName = frame.DirectName;
                Upvalues = frame.Upvalues;
                UserState = frame.UserState;
                Location = frame.Location;
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal void CaptureExceptionStack()
        {
            _exceptionStack ??= SnapshotActiveFrames();
        }

        internal AuroraStackTrace[] TakeExceptionStack()
        {
            var trace = _exceptionStack ?? SnapshotActiveFrames();
            _exceptionStack = null;
            return trace;
        }

        internal void ClearExceptionStack()
        {
            _exceptionStack = null;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private int PushCurrentFrame()
        {
            var frames = _frames;
            if (frames == null)
            {
                frames = new CallFrame[4];
                _frames = frames;
            }
            else if (_frameCount == frames.Length)
            {
                Array.Resize(ref _frames, frames.Length * 2);
                frames = _frames;
            }

            var depth = _frameCount;
            frames[_frameCount++] = new CallFrame(Module, Target, DirectName, Upvalues, UserState, Location);
            return depth;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void EnsureActive()
        {
            if (!_isActive)
            {
                throw new InvalidOperationException(
                    "The ScriptContext is no longer active. Use ClosureFunction.InvokeClrDetached for deferred or asynchronous callbacks.");
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal void Release()
        {
            var prev = Previous;
            if (prev != null)
            {
                prev.Next = null;
                Previous = null;
            }
            Next = null;
            Location = 0;
            if (_frameCount != 0)
            {
                Array.Clear(_frames, 0, _frameCount);
                _frameCount = 0;
            }
            _exceptionStack = null;
            _isActive = false;
            Domain.ContextPool.Return(this);
        }

        internal void ReleaseLinked()
        {
            var current = this;
            while (current != null)
            {
                var next = current.Next;
                current.Next = null;

                if (ReferenceEquals(current, next))
                {
                    next = null;
                }
                else if (next != null)
                {
                    next.Previous = null;
                }

                current.Release();
                current = next;
            }
        }



        /// <summary>
        /// Retrieves the current call stack by traversing the chain of previous execution contexts.
        /// </summary>
        /// <returns>An array of <see cref="AuroraStackTrace"/> representing the call stack.</returns>
        public AuroraStackTrace[] CallStack()
        {
            if (_frameCount != 0)
            {
                return SnapshotActiveFrames();
            }

            var count = 0;
            for (var context = this; context != null; context = context.Previous)
            {
                if (context.Location > 0) count++;
            }
            if (count == 0) return Array.Empty<AuroraStackTrace>();

            var result = new AuroraStackTrace[count];
            var index = 0;
            for (var context = this; context != null; context = context.Previous)
            {
                if (TryCreateTrace(context.Location, context.Target?.FuncName ?? context.DirectName, out var trace))
                {
                    result[index++] = trace;
                }
            }
            return TrimTrace(result, index);
        }


        /// <summary>
        /// Retrieves the stack trace by traversing the chain of next execution contexts.
        /// Useful for capturing the stack at the point of an exception.
        /// </summary>
        /// <returns>A list of <see cref="AuroraStackTrace"/> representing the stack trace, in reverse order (most recent first).</returns>
        public List<AuroraStackTrace> StackTrace()
        {
            if (_exceptionStack != null)
            {
                return new List<AuroraStackTrace>(_exceptionStack);
            }

            if (_frameCount != 0)
            {
                return new List<AuroraStackTrace>(SnapshotActiveFrames());
            }

            List<AuroraStackTrace> stackTraces = new List<AuroraStackTrace>();
            var c = this.Next;
            while (c != null)
            {
                if (c.Location > 0)
                {
                    UnionNumber m = new UnionNumber(c.Location);
                    if (Global.modulePathHash.TryGetValue(m.Int32ValueH, out var moduleMeta))
                    {
                        stackTraces.Add(new AuroraStackTrace(moduleMeta.ModulePath, c.Target?.FuncName ?? c.DirectName, m.Int32ValueL));
                    }
                }
                c = c.Next;
            }
            stackTraces.Reverse();
            return stackTraces;
        }

        private AuroraStackTrace[] SnapshotActiveFrames()
        {
            var count = Location > 0 ? 1 : 0;
            for (var i = _frameCount - 1; i >= 0; i--)
            {
                if (_frames[i].Location > 0) count++;
            }
            if (count == 0) return Array.Empty<AuroraStackTrace>();

            var result = new AuroraStackTrace[count];
            var index = 0;
            if (TryCreateTrace(Location, Target?.FuncName ?? DirectName, out var current))
            {
                result[index++] = current;
            }
            for (var i = _frameCount - 1; i >= 0; i--)
            {
                ref readonly var frame = ref _frames[i];
                if (TryCreateTrace(frame.Location, frame.Target?.FuncName ?? frame.DirectName, out var trace))
                {
                    result[index++] = trace;
                }
            }
            return TrimTrace(result, index);
        }

        private bool TryCreateTrace(long location, string name, out AuroraStackTrace trace)
        {
            if (location > 0)
            {
                UnionNumber encoded = new UnionNumber(location);
                if (Global.modulePathHash.TryGetValue(encoded.Int32ValueH, out var moduleMeta))
                {
                    trace = new AuroraStackTrace(moduleMeta.ModulePath, name, encoded.Int32ValueL);
                    return true;
                }
            }

            trace = null;
            return false;
        }

        private static AuroraStackTrace[] TrimTrace(AuroraStackTrace[] trace, int count)
        {
            if (count == trace.Length) return trace;
            if (count == 0) return Array.Empty<AuroraStackTrace>();
            Array.Resize(ref trace, count);
            return trace;
        }

        private readonly struct CallFrame
        {
            public CallFrame(
                ScriptModule module,
                ClosureFunction target,
                string directName,
                Upvalue[] upvalues,
                ScriptObject userState,
                long location)
            {
                Module = module;
                Target = target;
                DirectName = directName;
                Upvalues = upvalues;
                UserState = userState;
                Location = location;
            }

            public ScriptModule Module { get; }
            public ClosureFunction Target { get; }
            public string DirectName { get; }
            public Upvalue[] Upvalues { get; }
            public ScriptObject UserState { get; }
            public long Location { get; }
        }


    }
}
