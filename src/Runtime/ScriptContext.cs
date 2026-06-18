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
    public delegate ScriptDatum ScriptFunctionDelegate(ScriptContext ctx, ScriptDatum[] args);

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
                if (c.Location > 0)
                {
                    UnionNumber m = new UnionNumber(c.Location);
                    if (Global.modulePathHash.TryGetValue(m.Int32ValueH, out var moduleMeta))
                    {
                        stackTraces.Add(new AuroraStackTrace(moduleMeta.ModulePath, c.Target?.FuncName, m.Int32ValueL));
                    }
                }
                c = c.Next;
            }
            stackTraces.Reverse();
            return stackTraces;
        }


    }
}
