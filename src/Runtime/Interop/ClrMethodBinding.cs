using AuroraScript.Runtime.Types;
using System;
using System.Collections.Generic;
using System.Reflection;

namespace AuroraScript.Runtime.Interop
{
    /// <summary>
    /// Represents a delegate for retrieving a property value from a .NET instance.
    /// </summary>
    /// <param name="instance">The .NET object instance.</param>
    /// <returns>The retrieved property value.</returns>
    public delegate object ClrGetterBinding(object instance);

    /// <summary>
    /// Represents a binding for setting a property value on a .NET instance.
    /// </summary>
    public sealed class ClrSetterBinding
    {
        /// <summary> Gets the action that performs the assignment on the .NET instance. </summary>
        public readonly Action<object, object> Setter;
        /// <summary> Gets the expected target .NET type for the property. </summary>
        public readonly Type Type;

        /// <summary>
        /// Initializes a new instance of the <see cref="ClrSetterBinding"/> class.
        /// </summary>
        public ClrSetterBinding(Action<object, object> setter, Type valueType)
        {
            Setter = setter;
            Type = valueType;
        }
    }

    /// <summary>
    /// Represents a bound .NET method (or set of overloads) that can be invoked from AuroraScript.
    /// This class handles the mapping of script arguments to .NET parameters and performs the actual invocation.
    /// </summary>
    public sealed class ClrMethodBinding : ScriptObject
    {
        internal readonly ClrTypeDescriptor _descriptor;
        internal readonly MethodInvoker[] _compiledInvokers;
        internal readonly ClrInstanceObject _instance;
        internal readonly bool _isStatic;


        private ClrMethodBinding(ClrTypeDescriptor descriptor, MethodInvoker[] compiledInvokers, ClrInstanceObject instance, bool isStatic)
        {
            _descriptor = descriptor;
            _compiledInvokers = compiledInvokers;
            _instance = instance;
            _isStatic = isStatic;

        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ClrMethodBinding"/> class with a set of candidate methods.
        /// </summary>
        internal ClrMethodBinding(ClrTypeDescriptor descriptor, MethodBase[] methods, bool isStatic) : base()
        {
            _descriptor = descriptor;
            _isStatic = isStatic;
            _compiledInvokers = CompileInvokers(methods);
        }

        /// <summary>
        /// Creates a new binding for this method that is associated with a specific .NET instance.
        /// </summary>
        /// <param name="instance">The .NET instance to bind to.</param>
        /// <returns>A new <see cref="ClrMethodBinding"/> instance bound to the target.</returns>
        internal ClrMethodBinding Bound(ClrInstanceObject instance)
        {
            return new ClrMethodBinding(_descriptor, _compiledInvokers, instance, _isStatic);
        }

        /// <summary> Gets the name of the type containing this method. </summary>
        public string Name => _descriptor.Type.Name;

        /// <summary>
        /// Invokes the bound .NET method using the provided script arguments.
        /// Performs overload resolution by trying each compiled invoker until one matches the arguments.
        /// </summary>
        /// <param name="ctx">The current script execution context.</param>
        /// <param name="args">The arguments passed to the method.</param>
        /// <returns>The result of the method invocation, converted to a <see cref="ScriptDatum"/>.</returns>
        internal override ScriptDatum Invoke(ScriptContext ctx, params ScriptDatum[] args)
        {
            var targetHolder = _instance;
            if (!_isStatic && targetHolder == null)
            {
                throw new InvalidOperationException($"Instance method '{_descriptor.Type.FullName}' requires a CLR target. Ensure the object is bound correctly before invoking.");
            }
            var targetInstance = _isStatic ? null : targetHolder.Instance;
            var invokers = _compiledInvokers;
            for (int i = 0; i < invokers.Length; i++)
            {
                ref readonly var invoker = ref invokers[i];
                if (_isStatic != invoker.IsStatic)
                {
                    continue;
                }
                ScriptDatum result = default;
                if (invoker.TryInvoke(targetInstance, args, ref result))
                {
                    return result;
                }
            }

            throw new InvalidOperationException($"No matching method overload found on '{_descriptor.Type.FullName}'.");
        }


        private static MethodInvoker[] CompileInvokers(MethodBase[] methods)
        {
            var invokers = new List<MethodInvoker>(methods.Length);
            foreach (var method in methods)
            {
                invokers.Add(MethodInvoker.Compiler.Compile(method));
            }
            return invokers.ToArray();
        }

        private delegate bool InvokeDelegate(object target, Span<ScriptDatum> args, ref ScriptDatum result);

        /// <summary>
        /// Represents an optimized invoker for a specific .NET method.
        /// </summary>
        internal readonly struct MethodInvoker
        {
            private readonly InvokeDelegate _invoke;
            private readonly int _expectedArgumentCount;

            /// <summary> Gets a value indicating whether the method is static. </summary>
            public bool IsStatic { get; }

            private MethodInvoker(bool isStatic, int expectedArgumentCount, InvokeDelegate invoke)
            {
                IsStatic = isStatic;
                _expectedArgumentCount = expectedArgumentCount;
                _invoke = invoke;
            }

            /// <summary>
            /// Attempts to invoke the .NET method using the provided arguments.
            /// </summary>
            /// <param name="target">The .NET instance (null for static methods).</param>
            /// <param name="args">The script arguments to pass to the method.</param>
            /// <param name="result">When this method returns, contains the result of the invocation.</param>
            /// <returns>True if the invocation was successful and arguments matched; otherwise, false.</returns>
            public bool TryInvoke(object target, Span<ScriptDatum> args, ref ScriptDatum result)
            {
                if (!IsStatic && target == null) return false;

                var effectiveArgs = args;
                if (_expectedArgumentCount >= 0 && effectiveArgs.Length != _expectedArgumentCount)
                {
                    return false;
                }

                return _invoke(target, effectiveArgs, ref result);
            }

            /// <summary>
            /// Provides functionality for compiling optimized invokers for .NET methods.
            /// </summary>
            public static class Compiler
            {
                /// <summary>
                /// Compiles an optimized <see cref="MethodInvoker"/> for the specified .NET method.
                /// </summary>
                /// <param name="method">The .NET method to compile an invoker for.</param>
                /// <returns>An optimized <see cref="MethodInvoker"/>.</returns>
                public static MethodInvoker Compile(MethodBase method)
                {
                    if (method is MethodInfo methodInfo)
                    {
                        return CompileMethod(methodInfo);
                    }

                    throw new NotSupportedException($"Unsupported method type '{method.GetType().FullName}'.");
                }

                /// <summary>
                /// Analyzes the method signature and selects the most efficient invocation strategy.
                /// </summary>
                private static MethodInvoker CompileMethod(MethodInfo method)
                {
                    var parameters = method.GetParameters();
                    var expectedArgs = parameters.Length;
                    InvokeDelegate invokeDelegate;

                    if (expectedArgs == 0)
                    {
                        invokeDelegate = CompileNoArgs(method);
                    }
                    else if (expectedArgs == 1)
                    {
                        invokeDelegate = CompileSingleArg(method, parameters[0]);
                    }
                    else
                    {
                        var invoker = new ReflectionInvoker(method);
                        invokeDelegate = invoker.Invoke;
                    }

                    return new MethodInvoker(method.IsStatic, expectedArgs, invokeDelegate);
                }

                /// <summary>
                /// Compiles an invoker for a method with no parameters.
                /// </summary>
                private static InvokeDelegate CompileNoArgs(MethodInfo method)
                {
                    if (method.ReturnType == typeof(void))
                    {
                        return (object target, Span<ScriptDatum> arguments, ref ScriptDatum result) =>
                        {
                            method.Invoke(target, Array.Empty<object>());
                            return true;
                        };
                    }

                    return (object target, Span<ScriptDatum> arguments, ref ScriptDatum result) =>
                    {
                        var invocationResult = method.Invoke(target, Array.Empty<object>());
                        ClrMarshaller.WriteToDatum(ref result, invocationResult);
                        return true;
                    };
                }

                /// <summary>
                /// Compiles an invoker for a method with exactly one parameter.
                /// </summary>
                private static InvokeDelegate CompileSingleArg(MethodInfo method, ParameterInfo parameter)
                {
                    var parameterType = parameter.ParameterType;
                    if (method.ReturnType == typeof(void))
                    {
                        return (object target, Span<ScriptDatum> args, ref ScriptDatum result) =>
                        {
                            if (!ClrMarshaller.TryConvertArgument(in args[0], parameterType, out var converted))
                            {
                                return false;
                            }

                            method.Invoke(target, new[] { converted });
                            return true;
                        };
                    }

                    return (object target, Span<ScriptDatum> args, ref ScriptDatum result) =>
                    {
                        if (!ClrMarshaller.TryConvertArgument(in args[0], parameterType, out var converted))
                        {
                            return false;
                        }

                        var invocationResult = method.Invoke(target, new[] { converted });
                        ClrMarshaller.WriteToDatum(ref result, invocationResult);
                        return true;
                    };
                }

                /// <summary>
                /// A fallback invoker that uses standard .NET reflection to call methods with multiple parameters.
                /// </summary>
                private sealed class ReflectionInvoker
                {
                    private readonly MethodInfo _method;
                    private readonly ParameterInfo[] _parameters;

                    /// <summary>
                    /// Initializes a new instance of the <see cref="ReflectionInvoker"/> class.
                    /// </summary>
                    public ReflectionInvoker(MethodInfo method)
                    {
                        _method = method;
                        _parameters = method.GetParameters();
                    }

                    /// <summary>
                    /// Invokes the method using reflection.
                    /// </summary>
                    public bool Invoke(object target, Span<ScriptDatum> args, ref ScriptDatum result)
                    {
                        var invokeArgs = new object[_parameters.Length];
                        for (int i = 0; i < _parameters.Length; i++)
                        {
                            if (!ClrMarshaller.TryConvertArgument(in args[i], _parameters[i].ParameterType, out var converted))
                            {
                                return false;
                            }
                            invokeArgs[i] = converted;
                        }

                        var invocationResult = _method.Invoke(target, invokeArgs);
                        if (_method.ReturnType == typeof(void))
                        {
                            return true;
                        }
                        ClrMarshaller.WriteToDatum(ref result, invocationResult);
                        return true;
                    }
                }
            }
        }

    }
}

