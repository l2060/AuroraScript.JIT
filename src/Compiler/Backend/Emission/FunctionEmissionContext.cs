using AuroraScript.Compiler.Backend.Binding;
using AuroraScript.Compiler.Backend.Lowering;
using AuroraScript.Compiler.Backend.Plans;
using System;
using System.Collections.Generic;
using System.Reflection;

namespace AuroraScript.Compiler.Backend.Emission
{
    internal sealed class FunctionEmissionContext
    {
        private List<SourceSpan> _sequencePoints;
        private readonly HashSet<SymbolId> _directCallCandidateSymbols;

        public FunctionEmissionContext(
            CompileSession session,
            ModulePlan module,
            FunctionPlan function,
            HashSet<SymbolId> directCallCandidateSymbols = null)
        {
            Session = session ?? throw new ArgumentNullException(nameof(session));
            Module = module ?? throw new ArgumentNullException(nameof(module));
            Function = function ?? throw new ArgumentNullException(nameof(function));
            _directCallCandidateSymbols = directCallCandidateSymbols;
        }

        public CompileSession Session { get; }
        public ModulePlan Module { get; }
        public FunctionPlan Function { get; }
        public int StatementCount { get; private set; }
        public int ExpressionCount { get; private set; }
        public int LocalSlotReferenceCount { get; private set; }
        public int UpvalueSlotReferenceCount { get; private set; }
        public int ModuleSymbolReferenceCount { get; private set; }
        public int DirectCallCandidateReferenceCount { get; private set; }
        public int NestedFunctionReferenceCount { get; private set; }
        public int CatchSlotReferenceCount { get; private set; }
        public MethodInfo Method { get; private set; }
        public int CilLocalCount { get; private set; }

        public void RecordStatement(LoweredStatement statement)
        {
            if (statement == null)
            {
                return;
            }

            StatementCount++;
            RecordSequencePoint(statement.Range);
        }

        public void RecordExpression(LoweredExpression expression)
        {
            if (expression != null)
            {
                ExpressionCount++;
            }
        }

        public void RecordLocal(LocalSlotId slot)
        {
            if (slot.IsValid)
            {
                LocalSlotReferenceCount++;
            }
        }

        public void RecordUpvalue(UpvalueSlotId slot)
        {
            if (slot.IsValid)
            {
                UpvalueSlotReferenceCount++;
            }
        }

        public void RecordModuleSymbol(SymbolId symbol)
        {
            if (!symbol.IsValid)
            {
                return;
            }

            ModuleSymbolReferenceCount++;
            if (IsDirectCallCandidate(symbol))
            {
                DirectCallCandidateReferenceCount++;
            }
        }

        public void RecordNestedFunction(FunctionId function)
        {
            if (function.IsValid)
            {
                NestedFunctionReferenceCount++;
            }
        }

        public void RecordCatchSlot(LocalSlotId slot)
        {
            if (!slot.IsValid)
            {
                return;
            }

            CatchSlotReferenceCount++;
            RecordLocal(slot);
        }

        public UnsupportedEmissionException Unsupported(LoweredUnsupportedStatement statement)
        {
            return new UnsupportedEmissionException(Function, new LoweredUnsupportedNode(
                statement.Source?.GetType().Name ?? "<null>",
                statement.Range,
                isExpression: false));
        }

        public UnsupportedEmissionException Unsupported(LoweredUnsupportedExpression expression)
        {
            return new UnsupportedEmissionException(Function, new LoweredUnsupportedNode(
                expression.Source?.GetType().Name ?? "<null>",
                expression.Range,
                isExpression: true));
        }

        public void SetExecutableSkeleton(MethodInfo method, int localCount)
        {
            Method = method;
            CilLocalCount = localCount;
            Function.Method = method;
        }

        public FunctionEmissionResult ToResult()
        {
            return new FunctionEmissionResult(
                Function.Id,
                Function.Name,
                Function.Visibility,
                Function.IsDirectCallCandidate,
                Function.RequiresClosureObject,
                Function.CanCacheClosureObject,
                StatementCount,
                ExpressionCount,
                LocalSlotReferenceCount,
                UpvalueSlotReferenceCount,
                ModuleSymbolReferenceCount,
                DirectCallCandidateReferenceCount,
                NestedFunctionReferenceCount,
                CatchSlotReferenceCount,
                _sequencePoints?.ToArray() ?? Array.Empty<SourceSpan>(),
                Method,
                CilLocalCount);
        }

        private void RecordSequencePoint(SourceSpan range)
        {
            if (range.StartLine <= 0)
            {
                return;
            }

            _sequencePoints ??= new List<SourceSpan>(8);
            _sequencePoints.Add(range);
        }

        private bool IsDirectCallCandidate(SymbolId symbolId)
        {
            return _directCallCandidateSymbols != null &&
                _directCallCandidateSymbols.Contains(symbolId);
        }
    }
}
