using AuroraScript.Compiler.Ast;
using AuroraScript.Compiler.Backend.Plans;
using AuroraScript.Compiler.Emits;
using AuroraScript.Compiler.Emits.Builders;
using AuroraScript.Core;
using AuroraScript.Runtime;
using System;
using System.Collections.Generic;
using System.Reflection.Emit;

namespace AuroraScript.Compiler.Backend.Emission
{
    internal sealed class HotPatchEmitter
    {
        private readonly EmissionSession _session;
        private readonly AbstractCILBuilder _builder;
        private readonly HotPatchType _patchType;
        private readonly Dictionary<ModuleId, ModuleEmissionResult> _resultsByModule = new();

        public HotPatchEmitter(EmissionSession session, HotPatchType patchType)
        {
            _session = session ?? throw new ArgumentNullException(nameof(session));
            _builder = session.Builder;
            _patchType = patchType;
        }

        public DynamicCallMethod Emit(ModulePlan mainModule)
        {
            ArgumentNullException.ThrowIfNull(mainModule);

            var report = _session.Emit();
            for (var i = 0; i < report.Modules.Length; i++)
            {
                _resultsByModule[report.Modules[i].Module] = report.Modules[i];
            }

            var (patchMethod, il) = _builder.DefineDynamicMethod(mainModule.Declaration);
            var globalLocal = il.DeclareLocal(typeof(ScriptGlobal));
            _builder.SetLocalSymInfo(globalLocal, "global");

            il.Emit(OpCodes.Ldarg_0);
            il.Emit(OpCodes.Ldfld, RuntimeMetadata.CILContext_Global);
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
            il.Emit(OpCodes.Callvirt, RuntimeMetadata.ScriptGlobal_EnsureModule);
            if ((_patchType & HotPatchType.Replace) != 0)
            {
                il.Emit(OpCodes.Dup);
                il.Emit(OpCodes.Callvirt, RuntimeMetadata.ScriptObject_ClearProperties);
            }
            il.Emit(OpCodes.Pop);
        }

        private void InitializeModule(ILGenerator il, ModulePlan module, LocalBuilder globalLocal)
        {
            if (!_resultsByModule.TryGetValue(module.Id, out var result) || result.Initializer == null)
            {
                return;
            }

            il.Emit(OpCodes.Ldarg_0);
            il.Emit(OpCodes.Ldloc, globalLocal);
            _builder.LoadStringConstant(il, module.Name);
            il.Emit(OpCodes.Callvirt, RuntimeMetadata.ScriptGlobal_GetModule);
            il.Emit(OpCodes.Ldnull);
            il.Emit(OpCodes.Callvirt, RuntimeMetadata.CILContext_With);
            il.Emit(OpCodes.Ldarg_1);
            il.Emit(OpCodes.Call, result.Initializer);
        }
    }
}
