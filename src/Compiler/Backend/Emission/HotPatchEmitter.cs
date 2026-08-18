using AuroraScript.Compiler.Ast;
using AuroraScript.Compiler.Backend.Builders;
using AuroraScript.Compiler.Backend.Code;
using AuroraScript.Compiler.Backend.Plans;
using AuroraScript.Core;
using AuroraScript.Runtime;
using System;
using System.Reflection.Emit;

namespace AuroraScript.Compiler.Backend.Emission
{
    internal sealed class HotPatchEmitter
    {
        private readonly EmissionSession _session;
        private readonly AbstractCILBuilder _builder;
        private readonly HotPatchType _patchType;

        public HotPatchEmitter(EmissionSession session, HotPatchType patchType)
        {
            _session = session ?? throw new ArgumentNullException(nameof(session));
            _builder = session.Builder;
            _patchType = patchType;
        }

        public DynamicCallMethod Emit(ModulePlan mainModule)
        {
            ArgumentNullException.ThrowIfNull(mainModule);

            _session.EmitAll();

            var (patchMethod, il) = _builder.DefineDynamicMethod(mainModule.Declaration);
            var globalLocal = il.DeclareLocal(typeof(ScriptGlobal));
            _builder.SetLocalSymInfo(globalLocal, "global");

            il.Emit(OpCodes.Ldarg_0);
            il.Emit(OpCodes.Ldfld, TypedRuntimeMetadata.ContextGlobal);
            il.Emit(OpCodes.Stloc, globalLocal);

            var modules = _session.CompileSession.Modules;
            for (var i = 0; i < modules.Length; i++)
            {
                EnsureModule(il, modules[i], globalLocal);
            }

            for (var i = 0; i < modules.Length; i++)
            {
                InitializeModule(il, modules[i], globalLocal);
            }

            _builder.LoadNull(il);
            il.Emit(OpCodes.Ret);
            _builder.FinalizeBuild();
            return (DynamicCallMethod)patchMethod.CreateDelegate(typeof(DynamicCallMethod));
        }

        private void EnsureModule(ILGenerator il, ModulePlan module, LocalBuilder globalLocal)
        {
            il.Emit(OpCodes.Ldloc, globalLocal);
            _builder.LoadStringConstant(il, module.Name);
            _builder.LoadStringConstant(il, module.Path);
            _builder.LoadStringConstant(il, module.FullPath);
            il.Emit(OpCodes.Callvirt, TypedRuntimeMetadata.ScriptGlobalEnsureModule);
            if ((_patchType & HotPatchType.Replace) != 0)
            {
                il.Emit(OpCodes.Dup);
                il.Emit(OpCodes.Callvirt, TypedRuntimeMetadata.ScriptObjectClearProperties);
            }
            il.Emit(OpCodes.Pop);
        }

        private void InitializeModule(ILGenerator il, ModulePlan module, LocalBuilder globalLocal)
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
