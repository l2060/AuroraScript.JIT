using AuroraScript.Compiler.Ast;
using AuroraScript.Runtime;
using System;
using System.Reflection;
using System.Reflection.Emit;

namespace AuroraScript.Compiler.Backend.Builders
{
    internal sealed class DynamicBuilder : AbstractCILBuilder
    {
        private static readonly Type[] s_standardParameters = [typeof(ScriptContext), typeof(Span<ScriptDatum>)];
        private MethodInfo _domainInitMethod;

        public DynamicBuilder(EngineOptions options) : base(options)
        {
        }

        public override (MethodInfo Method, ILGenerator IL) DefineDynamicMethod(ModuleDeclaration module)
        {
            var dynamicMethod = new DynamicMethod(
                module.ModuleName,
                typeof(ScriptDatum),
                s_standardParameters,
                typeof(DynamicBuilder).Module,
                true
            );

            return (dynamicMethod, dynamicMethod.GetILGenerator());
        }

        public override (MethodInfo Method, ILGenerator IL) DefineBlockMethod(string methodName)
        {
            var dynamicMethod = new DynamicMethod(
                methodName,
                typeof(ScriptDatum),
                s_standardParameters,
                typeof(DynamicBuilder).Module,
                true
            );

            return (dynamicMethod, dynamicMethod.GetILGenerator());
        }

        public override (MethodInfo Method, ILGenerator IL) DefineModuleInitMethod(ModuleDeclaration module)
        {
            var dynamicMethod = new DynamicMethod("Initialize",
                //$"Module_{module.ModuleName}_Initialize",
                typeof(void),
                s_standardParameters,
                typeof(DynamicBuilder).Module,
                true
            );

            return (dynamicMethod, dynamicMethod.GetILGenerator());
        }


        public override (MethodInfo Method, ILGenerator IL) DefineDomainInitMethod()
        {
            var dynamicMethod = new DynamicMethod(
                EntryPointMethodName,
                typeof(ScriptDatum),
                s_standardParameters,
                typeof(DynamicBuilder).Module,
                true
            );
            _domainInitMethod = dynamicMethod;
            return (dynamicMethod, dynamicMethod.GetILGenerator());
        }

        public override (MethodInfo Method, ILGenerator IL) DefineMethod(string moduleName, string methodName, Type returnType, Type[] parameterTypes)
        {
            var dynamicMethod = new DynamicMethod(methodName,
               //$"Module_{moduleName}_{}",
               returnType,
               parameterTypes,
               typeof(DynamicBuilder).Module,
               true
           );
            return (dynamicMethod, dynamicMethod.GetILGenerator());
        }


        public override MethodInfo GetRuntimeEntryPoint()
        {
            return _domainInitMethod;
        }

    }
}
