using AuroraScript.Compiler.Ast;
using AuroraScript.Runtime;
using System;
using System.Reflection;
using System.Reflection.Emit;

namespace AuroraScript.Compiler.Backend.Builders
{
    internal sealed class OnlyRunBuilder : AbstractCILBuilder
    {
        private readonly AssemblyBuilder _assemblyBuilder;
        private readonly ModuleBuilder _moduleBuilder;
        private TypeBuilder _typeBuilder;
        private int _moduleTypeCount;

        public OnlyRunBuilder(EngineOptions options) : base(options)
        {
            var assemblyName = new AssemblyName(InternalConstant.AssemblyName);
            assemblyName.Version = new Version(1, 0, 0, 0);
            var optimizeOption = IsDebugMode ? InternalConstant.Debug : InternalConstant.Release;
            _assemblyBuilder = AssemblyBuilder.DefineDynamicAssembly(assemblyName, AssemblyBuilderAccess.RunAndCollect, [optimizeOption]);
            _moduleBuilder = _assemblyBuilder.DefineDynamicModule(InternalConstant.AssemblyName);
        }

        public sealed override (MethodInfo Method, ILGenerator IL) DefineModuleInitMethod(ModuleDeclaration module)
        {
            var typeBuilder = _moduleBuilder.DefineType(ConfuseTypeName("AuroraModule_" + _moduleTypeCount++, ConfuseTarget.Class), TypeAttributes.Public | TypeAttributes.Class);
            var methodBuilder = typeBuilder.DefineMethod(ConfuseTypeName("Initialize", ConfuseTarget.Method), MethodAttributes.Public | MethodAttributes.Static, typeof(void), [typeof(ScriptContext), typeof(Span<ScriptDatum>)]);
            RegisterType(module.Source.FullPath, typeBuilder);
            return (methodBuilder, methodBuilder.GetILGenerator());
        }

        public sealed override (MethodInfo Method, ILGenerator IL) DefineDomainInitMethod()
        {
            _typeBuilder = _moduleBuilder.DefineType(EntryPointTypeName, TypeAttributes.Public | TypeAttributes.Class);
            var methodBuilder = _typeBuilder.DefineMethod(EntryPointMethodName, MethodAttributes.Public | MethodAttributes.Static, typeof(ScriptDatum), [typeof(ScriptContext), typeof(Span<ScriptDatum>)]);
            RegisterType(EntryPointTypeName, _typeBuilder);
            return (methodBuilder, methodBuilder.GetILGenerator());
        }



        public sealed override (MethodInfo Method, ILGenerator IL) DefineMethod(
            string moduleKey,
            string methodName,
            Type returnType,
            Type[] parameterTypes,
            bool aggressiveInlining = false)
        {
            if (!TryResolveType(moduleKey, out var typeBuilder))
            {
                throw new Exception($"Module source '{moduleKey}' is not defined");
            }
            var method = typeBuilder.DefineMethod(ConfuseTypeName(methodName, ConfuseTarget.Method), MethodAttributes.Public | MethodAttributes.Static, returnType, parameterTypes);
            if (aggressiveInlining)
            {
                method.SetImplementationFlags(
                    MethodImplAttributes.IL |
                    MethodImplAttributes.Managed |
                    MethodImplAttributes.AggressiveInlining);
            }
            return (method, method.GetILGenerator());
        }

        public sealed override MethodInfo GetRuntimeEntryPoint()
        {
            if (_typeBuilder == null) return null;
            var runtimeType = _typeBuilder.UnderlyingSystemType;
            if (_typeBuilder.IsCreated())
            {
                var initializeMethod = _typeBuilder.GetMethod("InitializeDomain");
                return initializeMethod;
            }
            return null;
        }

    }
}
