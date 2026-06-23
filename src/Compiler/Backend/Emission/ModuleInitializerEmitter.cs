using AuroraScript.Compiler.Backend.Plans;
using AuroraScript.Compiler.Emits;
using System;
using System.Reflection;
using System.Reflection.Emit;

namespace AuroraScript.Compiler.Backend.Emission
{
    internal sealed class ModuleInitializerEmitter
    {
        private readonly EmissionSession _session;
        private readonly ModulePlan _module;
        private MethodInfo _initializer;
        private ILGenerator _il;
        private bool _defined;
        private bool _emitted;

        public ModuleInitializerEmitter(EmissionSession session, ModulePlan module)
        {
            _session = session ?? throw new ArgumentNullException(nameof(session));
            _module = module ?? throw new ArgumentNullException(nameof(module));
        }

        public void Define()
        {
            if (_defined)
            {
                return;
            }

            var method = _session.Builder.DefineModuleInitMethod(_module.Declaration);
            _initializer = method.Method;
            _il = method.IL;
            _module.Initializer = _initializer;
            _defined = true;
        }

        public bool TryEmit(out MethodInfo initializer)
        {
            initializer = null;
            if (_emitted)
            {
                initializer = HasMaterializedFunctions(_module) ? _initializer : null;
                return initializer != null;
            }

            Define();
            var hasMaterializedFunctions = false;
            for (var i = 0; i < _module.Functions.Count; i++)
            {
                var function = _module.Functions[i];
                if (!CanMaterialize(function))
                {
                    continue;
                }

                hasMaterializedFunctions = true;
                EmitDefineFunction(_il, function);
            }

            _il.Emit(OpCodes.Ret);
            _emitted = true;
            if (!hasMaterializedFunctions)
            {
                return false;
            }

            initializer = _initializer;
            return true;
        }

        private static bool HasMaterializedFunctions(ModulePlan module)
        {
            for (var i = 0; i < module.Functions.Count; i++)
            {
                if (CanMaterialize(module.Functions[i]))
                {
                    return true;
                }
            }

            return false;
        }

        private static bool CanMaterialize(FunctionPlan function)
        {
            return ClosureMaterializer.CanMaterialize(function, requireName: true);
        }

        private void EmitDefineFunction(ILGenerator il, FunctionPlan function)
        {
            il.Emit(OpCodes.Ldarg_0);
            il.Emit(OpCodes.Ldfld, RuntimeMetadata.CILContext_Module);
            _session.Builder.LoadStringConstant(il, function.Name);
            ClosureMaterializer.EmitClosure(_session, il, function);
            il.Emit(OpCodes.Ldc_I4_1);
            il.Emit(OpCodes.Ldc_I4_1);
            il.Emit(OpCodes.Callvirt, RuntimeMetadata.ScriptObject_Define);
        }
    }
}
