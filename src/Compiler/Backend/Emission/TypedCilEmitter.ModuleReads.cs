using AuroraScript.Compiler.Ast.Expressions;
using AuroraScript.Compiler.Ast.Statements;
using AuroraScript.Compiler.Backend.Code;
using AuroraScript.Compiler.Backend.Plans;
using AuroraScript.Runtime;
using System.Collections.Generic;
using System.Reflection.Emit;

namespace AuroraScript.Compiler.Backend.Emission
{
    internal sealed partial class TypedCilEmitter
    {
        private Dictionary<SymbolId, ModuleReadRegion.BindingValue> _stableModuleBindings;
        private bool _hasRepeatedModuleReads;
        private Dictionary<NameExpression, LocalBuilder> _moduleReadLocals;
        private HashSet<LocalBuilder> _loadedModuleReads;

        private void PrepareModuleReadRegions()
        {
            _hasRepeatedModuleReads = false;
            if (_function.IsModuleInitializer || !HasContextArgument) return;
            _stableModuleBindings ??= ModuleReadRegion.GetStableBindings(
                _module, _moduleCode.Initializer, _session.CallableReturns);
            if (_stableModuleBindings.Count == 0) return;
            var seen = new HashSet<SymbolId>();
            foreach (var binding in _session.CallableReturns.Bindings[_function.Id.Value].Names.Values)
                if (!binding.HasConstant && _stableModuleBindings.ContainsKey(binding.ModuleSymbol) && !seen.Add(binding.ModuleSymbol))
                {
                    _hasRepeatedModuleReads = true;
                    return;
                }
        }

        private bool TryEmitModuleReadRegion(IReadOnlyList<Statement> statements, ref int index)
        {
            if (!_hasRepeatedModuleReads) return false;
            var region = ModuleReadRegion.Analyze(statements, index, _code, _stableModuleBindings);
            if (region == null) return false;
            _moduleReadLocals = new();
            _loadedModuleReads = new();
            foreach (var names in region.Reads.Values)
            {
                if (names.Count < 2) continue;
                var local = DeclareLocal(typeof(ScriptDatum));
                foreach (var name in names) _moduleReadLocals[name] = local;
            }
            try
            {
                for (var i = 0; i < region.StatementCount; i++) EmitStatement(statements[index + i]);
            }
            finally
            {
                _moduleReadLocals = null;
                _loadedModuleReads = null;
            }
            index += region.StatementCount - 1;
            return true;
        }

        private bool TryEmitSharedModuleRead(NameExpression name)
        {
            if (_moduleReadLocals == null || !_moduleReadLocals.TryGetValue(name, out var local)) return false;
            // Load at the first actual use, preserving script evaluation order.
            // This set controls emission only; no runtime branch or type test is added.
            if (_loadedModuleReads.Add(local))
            {
                _il.Emit(OpCodes.Ldarg_0);
                _session.Builder.LoadStringConstant(_il, _code.GetName(name).Name);
                _il.Emit(OpCodes.Call, TypedRuntimeMetadata.GetModule);
                _il.Emit(OpCodes.Stloc, local);
            }
            _il.Emit(OpCodes.Ldloc, local);
            return true;
        }
    }
}
