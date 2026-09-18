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
        private bool _usesModuleState;
        private List<FunctionId> _moduleStateCalls;
        private bool[] _directModuleState;
        private List<FunctionId>[] _directModuleStateCalls;

        private void BeginModuleStateTracking(int moduleIndex)
        {
            if (_directModuleState == null) return;
            _usesModuleState = false;
            _moduleStateCalls = _directModuleStateCalls[moduleIndex] ??= new();
        }

        private void EndModuleStateTracking(int moduleIndex)
        {
            if (_directModuleState == null) return;
            _directModuleState[moduleIndex] = _usesModuleState;
            _moduleStateCalls = null;
            _usesModuleState = false;
        }

        private void MarkModuleStateUse(BoundName binding)
        {
            if (IsModuleBinding(binding)) _usesModuleState = true;
        }

        /// <summary>
        /// Records a call a direct body performs on another function of this
        /// module, so module usage propagates along the direct call graph.
        /// </summary>
        private void RecordModuleStateCall(FunctionId callee)
        {
            _moduleStateCalls?.Add(callee);
        }

        /// <summary>
        /// Resolves module usage of every native entry of this module once all
        /// direct bodies are emitted. A caller of another module may only invoke
        /// entries proven independent of the active module.
        /// </summary>
        private void PublishNativeEntryModuleState()
        {
            if (_directModuleState == null) return;
            var changed = true;
            while (changed)
            {
                changed = false;
                for (var i = 0; i < _directModuleState.Length; i++)
                {
                    var calls = _directModuleStateCalls[i];
                    if (_directModuleState[i] || calls == null) continue;
                    for (var j = 0; j < calls.Count; j++)
                    {
                        var index = _module.GetFunctionIndex(calls[j]);
                        // A callee without an emitted direct body carries no
                        // proof, so its caller stays module dependent.
                        if (index >= 0 && !_directModuleState[index]) continue;
                        _directModuleState[i] = true;
                        changed = true;
                        break;
                    }
                }
            }

            for (var i = 0; i < _module.Functions.Count; i++)
            {
                var function = _module.Functions[i];
                if (function.NativeEntryMethod == null) continue;
                function.NativeEntryUsesModuleState =
                    _directModuleState[function.ModuleIndex];
                function.NativeEntryModuleStateResolved = true;
            }
        }

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
                _usesModuleState = true;
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
