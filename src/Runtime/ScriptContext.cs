using AuroraScript.Core;
using AuroraScript.Runtime.Types;
using System;
using System.Collections.Generic;

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
    /// Represents the execution context for AuroraScript.
    /// This object maintains the state for a specific function execution, including 
    /// references to the domain, global variables, current module, and captured upvalues.
    /// It is passed to every compiled script function to provide access to the runtime environment.
    /// </summary>
    public class ScriptContext
    {
        /// <summary> The script domain associated with this context. </summary>
        public readonly ScriptDomain Domain;

        /// <summary> The engine instance that is executing the script. </summary>
        public readonly AuroraEngine Engine;

        /// <summary> The global variables and functions available in this context. </summary>
        public readonly ScriptGlobal Global;

        /// <summary> The current script module being executed. </summary>
        public readonly ScriptModule Module;

        /// <summary> A user-defined state object associated with this execution context. </summary>
        public readonly ScriptObject UserState;

        /// <summary> An array of captured values (upvalues) available to the current function. </summary>
        internal readonly Upvalue[] Upvalues;

        /// <summary> The closure function that is the target of this execution context. </summary>
        public readonly ClosureFunction Target;

        /// <summary> The next context in a linked list or stack of execution contexts (e.g., for call frames). </summary>
        public ScriptContext Next;

        /// <summary> The previous context in a linked list or stack of execution contexts (e.g., for call frames). </summary>
        public ScriptContext Previous;
        /// <summary> The current execution location or instruction pointer within the code. </summary>
        public Int64 Location;

        /// <summary>
        /// Initializes a new instance of the <see cref="ScriptContext"/> class with detailed execution parameters.
        /// </summary>
        internal ScriptContext(ScriptDomain domain, ScriptObject userState, ScriptModule module, ClosureFunction closure = null)
        {
            Domain = domain;
            Engine = domain.Engine;
            Global = domain.Global;
            UserState = userState;
            Module = module;
            Target = closure;
            Upvalues = closure?.Upvalues;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ScriptContext"/> class for a specific domain.
        /// </summary>
        public ScriptContext(ScriptDomain domain)
        {
            Domain = domain;
            Engine = domain.Engine;
            Global = domain.Global;
            UserState = domain.UserState;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ScriptContext"/> class for a domain and specific user state.
        /// </summary>
        public ScriptContext(ScriptDomain domain, ScriptObject userState)
        {
            Domain = domain;
            Engine = domain.Engine;
            Global = domain.Global;
            UserState = userState;
        }

        /// <summary>
        /// Creates a new child execution context with a specific module and optional closure.
        /// Sets the <see cref="Next"/> pointer to the newly created context.
        /// </summary>
        public ScriptContext With(ScriptModule module, ClosureFunction closure = null)
        {
            var next = new ScriptContext(Domain, UserState, module, closure);
            this.Next = next;
            next.Previous = this;
            return next;
        }

        /// <summary>
        /// Creates a new child execution context for the specified closure function.
        /// Automatically resolves the module from the closure.
        /// </summary>
        public ScriptContext With(ClosureFunction closure)
        {
            var next = new ScriptContext(Domain, UserState, closure.Module, closure);
            this.Next = next;
            next.Previous = this;
            return next;
        }

        /// <summary>
        /// Creates a new child execution context with a specific module, closure, and user state.
        /// </summary>
        public ScriptContext With(ScriptModule module, ClosureFunction closure, ScriptObject userState)
        {
            var next = new ScriptContext(Domain, userState ?? UserState, module, closure);
            this.Next = next;
            next.Previous = this;
            return next;
        }



        /// <summary>
        /// Retrieves the current call stack by traversing the chain of previous execution contexts.
        /// </summary>
        /// <returns>An array of <see cref="AuroraStackTrace"/> representing the call stack.</returns>
        public AuroraStackTrace[] CallStack()
        {
            List<AuroraStackTrace> stackTraces = new List<AuroraStackTrace>();
            var c = this;
            while (c != null && c.Location > 0)
            {
                UnionNumber m = new UnionNumber(c.Location);
                var moduleMeta = Global.modulePathHash[m.Int32ValueH];
                stackTraces.Add(new AuroraStackTrace(moduleMeta.ModulePath, c.Target?.FuncName, m.Int32ValueL));
                c = c.Previous;
            }
            return stackTraces.ToArray();
        }


        /// <summary>
        /// Retrieves the stack trace by traversing the chain of next execution contexts.
        /// Useful for capturing the stack at the point of an exception.
        /// </summary>
        /// <returns>A list of <see cref="AuroraStackTrace"/> representing the stack trace, in reverse order (most recent first).</returns>
        public List<AuroraStackTrace> StackTrace()
        {
            List<AuroraStackTrace> stackTraces = new List<AuroraStackTrace>();
            var c = this.Next;
            while (c != null)
            {
                UnionNumber m = new UnionNumber(c.Location);
                var moduleMeta = Global.modulePathHash[m.Int32ValueH];
                stackTraces.Add(new AuroraStackTrace(moduleMeta.ModulePath, c.Target?.FuncName, m.Int32ValueL));
                c = c.Next;
            }
            stackTraces.Reverse();
            return stackTraces;
        }


    }
}
