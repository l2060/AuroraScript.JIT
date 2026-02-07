using AuroraScript.Compiler.Ast;
using AuroraScript.Runtime;
using System;
using System.Reflection;
using System.Reflection.Emit;

namespace AuroraScript.Compiler.Emits.Builders
{
    internal class DynamicBuilder : AbstractCILBuilder
    {
        private MethodInfo _domainInitMethod;

        public DynamicBuilder(EngineOptions options) : base(options)
        {
        }

        public override (MethodInfo Method, ILGenerator IL) DefineDynamicMethod(ModuleDeclaration module)
        {
            var dynamicMethod = new DynamicMethod(
                module.ModuleName,
                typeof(ScriptDatum),
                [typeof(ScriptContext), typeof(ScriptDatum[])],
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
                [typeof(ScriptContext), typeof(ScriptDatum[])],
                typeof(DynamicBuilder).Module,
                true
            );

            return (dynamicMethod, dynamicMethod.GetILGenerator());
        }


        public override (MethodInfo Method, ILGenerator IL) DefineDomainInitMethod()
        {
            var dynamicMethod = new DynamicMethod(
                EntryPointMethodName,
                typeof(void),
                [typeof(ScriptContext), typeof(ScriptDatum[])],
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

        public override void SetLocalSymInfo(LocalBuilder local, string name)
        {

        }

        public override void MarkSequencePoint(AstNode node, ILGenerator il)
        {

        }

        public override void MarkSequencePoint(SourceSpan range, ILGenerator il)
        {

        }
    }
}
