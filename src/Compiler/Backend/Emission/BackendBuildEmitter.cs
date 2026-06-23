using AuroraScript.Compiler.Ast;
using AuroraScript.Compiler.Backend.Plans;
using AuroraScript.Compiler.Emits;
using AuroraScript.Compiler.Emits.Builders;
using AuroraScript.Runtime;
using System;
using System.Collections.Generic;
using System.Reflection.Emit;

namespace AuroraScript.Compiler.Backend.Emission
{
    internal sealed class BackendBuildEmitter
    {
        private readonly EmissionSession _session;
        private readonly AbstractCILBuilder _builder;
        private readonly Dictionary<ModuleId, ModuleEmissionResult> _resultsByModule = new();

        public BackendBuildEmitter(EmissionSession session)
        {
            _session = session ?? throw new ArgumentNullException(nameof(session));
            _builder = session.Builder;
        }

        public EmissionReport Emit()
        {
            var report = _session.Emit();
            for (var i = 0; i < report.Modules.Length; i++)
            {
                _resultsByModule[report.Modules[i].Module] = report.Modules[i];
            }

            EnsureCompleteExecutableCoverage();
            EmitDomainInitializer();
            _builder.FinalizeBuild();
            return report;
        }

        private void EnsureCompleteExecutableCoverage()
        {
            var modules = _session.CompileSession.Modules;
            for (var moduleIndex = 0; moduleIndex < modules.Length; moduleIndex++)
            {
                var module = modules[moduleIndex];
                if (!_resultsByModule.TryGetValue(module.Id, out var moduleResult))
                {
                    throw new InvalidOperationException("Missing emission result for module '" + module.Name + "'.");
                }

                for (var i = 0; i < module.Functions.Count; i++)
                {
                    var function = module.Functions[i];
                    if (function.Method != null)
                    {
                        continue;
                    }

                    throw new UnsupportedEmissionException(
                        function,
                        new Lowering.LoweredUnsupportedNode(
                            function.UnsupportedLoweredNodes.Length > 0 ? function.UnsupportedLoweredNodes[0].NodeType : "ExecutableSkeleton",
                            function.Declaration?.Range ?? SourceSpan.None,
                            isExpression: false));
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
            il.Emit(OpCodes.Ldfld, RuntimeMetadata.CILContext_Global);
            il.Emit(OpCodes.Stloc, globalLocal);

            for (var i = 0; i < modules.Length; i++)
            {
                EmitRegisterModule(il, modules[i], globalLocal);
            }

            for (var i = 0; i < modules.Length; i++)
            {
                EmitInitializeModule(il, modules[i], globalLocal);
            }

            il.Emit(OpCodes.Ldsfld, RuntimeMetadata.ScriptDatum_Null);
            il.Emit(OpCodes.Ret);
        }

        private void EmitRegisterModule(ILGenerator il, ModulePlan module, LocalBuilder globalLocal)
        {
            il.Emit(OpCodes.Ldloc, globalLocal);
            _builder.LoadStringConstant(il, module.Name);
            il.Emit(OpCodes.Ldc_I4, module.PathHash);
            _builder.LoadStringConstant(il, module.Name);
            _builder.LoadStringConstant(il, module.Path);
            il.Emit(OpCodes.Newobj, RuntimeMetadata.ScriptModule_Ctor);
            il.Emit(OpCodes.Callvirt, RuntimeMetadata.ScriptGlobal_RegisterModule);
        }

        private void EmitInitializeModule(ILGenerator il, ModulePlan module, LocalBuilder globalLocal)
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
