using AuroraScript.Compiler.Backend.Plans;
using System;
using System.Linq;
using System.Reflection;

namespace AuroraScript.Compiler.Backend.Emission
{
    internal sealed class EmissionReport
    {
        public EmissionReport(ModuleEmissionResult[] modules)
        {
            Modules = modules ?? Array.Empty<ModuleEmissionResult>();
        }

        public ModuleEmissionResult[] Modules { get; }
        public int ModuleCount => Modules.Length;
        public int FunctionCount => Modules.Sum(module => module.FunctionCount);
    }

    internal readonly struct ModuleEmissionResult
    {
        public ModuleEmissionResult(ModuleId module, string name, FunctionEmissionResult[] functions, MethodInfo initializer)
        {
            Module = module;
            Name = name;
            Functions = functions ?? Array.Empty<FunctionEmissionResult>();
            Initializer = initializer;
        }

        public ModuleId Module { get; }
        public string Name { get; }
        public FunctionEmissionResult[] Functions { get; }
        public MethodInfo Initializer { get; }
        public bool HasExecutableInitializer => Initializer != null;
        public int FunctionCount => Functions.Length;
    }

    internal readonly struct FunctionEmissionResult
    {
        public FunctionEmissionResult(
            FunctionId function,
            string name,
            FunctionVisibility visibility,
            bool isDirectCallCandidate,
            bool requiresClosureObject,
            bool canCacheClosureObject,
            int statementCount,
            int expressionCount,
            int localSlotReferenceCount,
            int upvalueSlotReferenceCount,
            int moduleSymbolReferenceCount,
            int directCallCandidateReferenceCount,
            int nestedFunctionReferenceCount,
            int catchSlotReferenceCount,
            SourceSpan[] sequencePoints,
            MethodInfo method,
            int cilLocalCount)
        {
            Function = function;
            Name = name;
            Visibility = visibility;
            IsDirectCallCandidate = isDirectCallCandidate;
            RequiresClosureObject = requiresClosureObject;
            CanCacheClosureObject = canCacheClosureObject;
            StatementCount = statementCount;
            ExpressionCount = expressionCount;
            LocalSlotReferenceCount = localSlotReferenceCount;
            UpvalueSlotReferenceCount = upvalueSlotReferenceCount;
            ModuleSymbolReferenceCount = moduleSymbolReferenceCount;
            DirectCallCandidateReferenceCount = directCallCandidateReferenceCount;
            NestedFunctionReferenceCount = nestedFunctionReferenceCount;
            CatchSlotReferenceCount = catchSlotReferenceCount;
            SequencePoints = sequencePoints ?? Array.Empty<SourceSpan>();
            Method = method;
            CilLocalCount = cilLocalCount;
        }

        public FunctionId Function { get; }
        public string Name { get; }
        public FunctionVisibility Visibility { get; }
        public bool IsDirectCallCandidate { get; }
        public bool RequiresClosureObject { get; }
        public bool CanCacheClosureObject { get; }
        public int StatementCount { get; }
        public int ExpressionCount { get; }
        public int LocalSlotReferenceCount { get; }
        public int UpvalueSlotReferenceCount { get; }
        public int ModuleSymbolReferenceCount { get; }
        public int DirectCallCandidateReferenceCount { get; }
        public int NestedFunctionReferenceCount { get; }
        public int CatchSlotReferenceCount { get; }
        public SourceSpan[] SequencePoints { get; }
        public int SequencePointCount => SequencePoints.Length;
        public MethodInfo Method { get; }
        public bool HasExecutableSkeleton => Method != null;
        public int CilLocalCount { get; }
    }
}
