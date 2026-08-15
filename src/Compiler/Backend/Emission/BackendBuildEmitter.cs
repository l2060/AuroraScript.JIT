using AuroraScript.Compiler.Ast;
using AuroraScript.Compiler.Backend.Builders;
using AuroraScript.Compiler.Backend.Code;
using AuroraScript.Compiler.Backend.Plans;
using AuroraScript.Runtime;
using System;
using System.Reflection.Emit;

namespace AuroraScript.Compiler.Backend.Emission
{
    internal sealed class BackendBuildEmitter
    {
        private readonly EmissionSession _session;
        private readonly AbstractCILBuilder _builder;

        public BackendBuildEmitter(EmissionSession session)
        {
            _session = session ?? throw new ArgumentNullException(nameof(session));
            _builder = session.Builder;
        }

        public void Emit()
        {
            _session.EmitAll();
            EnsureCompleteExecutableCoverage();
            EmitDomainInitializer();
            _builder.FinalizeBuild();
        }

        private void EnsureCompleteExecutableCoverage()
        {
            var modules = _session.CompileSession.Modules;
            for (var moduleIndex = 0; moduleIndex < modules.Length; moduleIndex++)
            {
                var module = modules[moduleIndex];
                for (var i = 0; i < module.Functions.Count; i++)
                {
                    var function = module.Functions[i];
                    if (function.Method != null)
                    {
                        continue;
                    }

                    throw new UnsupportedEmissionException(
                        function,
                        function.Declaration);
                }
            }
        }

        private void EmitDomainInitializer()
        {
            var modules = _session.CompileSession.Modules;
            var (_, il) = _builder.DefineDomainInitMethod();
            var globalLocal = il.DeclareLocal(typeof(ScriptGlobal));
            _builder.SetLocalSymInfo(globalLocal, "global");

            il.Emit(OpCodes.Ldarg_0);
            il.Emit(OpCodes.Ldfld, TypedRuntimeMetadata.ContextGlobal);
            il.Emit(OpCodes.Stloc, globalLocal);

            for (var i = 0; i < modules.Length; i++)
            {
                EmitRegisterModule(il, modules[i], globalLocal);
            }

            for (var i = 0; i < modules.Length; i++)
            {
                EmitInitializeModule(il, modules[i], globalLocal);
            }

            il.Emit(OpCodes.Ldsfld, TypedRuntimeMetadata.DatumNull);
            il.Emit(OpCodes.Ret);
        }

        private void EmitRegisterModule(ILGenerator il, ModulePlan module, LocalBuilder globalLocal)
        {
            il.Emit(OpCodes.Ldloc, globalLocal);
            _builder.LoadStringConstant(il, module.Name);
            il.Emit(OpCodes.Ldc_I4, module.PathHash);
            _builder.LoadStringConstant(il, module.Name);
            _builder.LoadStringConstant(il, module.Path);
            _builder.LoadStringConstant(il, module.FullPath);
            il.Emit(OpCodes.Newobj, TypedRuntimeMetadata.ScriptModuleConstructor);
            il.Emit(OpCodes.Callvirt, TypedRuntimeMetadata.ScriptGlobalRegisterModule);
        }

        private void EmitInitializeModule(ILGenerator il, ModulePlan module, LocalBuilder globalLocal)
        {
            if (module.Initializer == null)
            {
                return;
            }

            var moduleLocal = il.DeclareLocal(typeof(ScriptModule));
            var frameLocal = il.DeclareLocal(typeof(int));
            il.Emit(OpCodes.Ldloc, globalLocal);
            _builder.LoadStringConstant(il, module.Name);
            il.Emit(OpCodes.Callvirt, TypedRuntimeMetadata.ScriptGlobalGetModule);
            il.Emit(OpCodes.Stloc, moduleLocal);
            il.Emit(OpCodes.Ldarg_0);
            il.Emit(OpCodes.Ldloc, moduleLocal);
            il.Emit(OpCodes.Call, TypedRuntimeMetadata.EnterModuleFrame);
            il.Emit(OpCodes.Stloc, frameLocal);

            il.BeginExceptionBlock();
            il.Emit(OpCodes.Ldarg_0);
            il.Emit(OpCodes.Ldarg_1);
            il.Emit(OpCodes.Call, module.Initializer);
            il.BeginFinallyBlock();
            il.Emit(OpCodes.Ldarg_0);
            il.Emit(OpCodes.Ldloc, frameLocal);
            il.Emit(OpCodes.Call, TypedRuntimeMetadata.LeaveFrame);
            il.EndExceptionBlock();
        }
    }
}
