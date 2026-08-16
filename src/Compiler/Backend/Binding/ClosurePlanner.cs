using AuroraScript.Compiler.Ast;
using AuroraScript.Compiler.Ast.Expressions;
using AuroraScript.Compiler.Ast.Statements;
using AuroraScript.Compiler.Backend.Plans;
using AuroraScript.Compiler.Backend.Traversal;
using System;
using System.Collections.Generic;

namespace AuroraScript.Compiler.Backend.Binding
{
    internal static class ClosurePlanner
    {
        public static void PlanModule(ModulePlan modulePlan)
        {
            ArgumentNullException.ThrowIfNull(modulePlan);

            if (modulePlan.Functions.Count == 0)
            {
                return;
            }

            if (modulePlan.Functions.Count == 1 &&
                modulePlan.Functions[0].NestedFunctions.Length == 0)
            {
                PlanSingleNonNestedFunction(modulePlan.Functions[0]);
                return;
            }

            if (!HasNestedFunctions(modulePlan))
            {
                PlanFlatModule(modulePlan);
                return;
            }

            var parentByIndex = new int[modulePlan.Functions.Count];
            Array.Fill(parentByIndex, -1);
            var maxFunctionId = -1;
            for (var i = 0; i < modulePlan.Functions.Count; i++)
            {
                var id = modulePlan.Functions[i].Id.Value;
                if (id > maxFunctionId)
                {
                    maxFunctionId = id;
                }
            }

            var indexById = maxFunctionId >= 0 ? new int[maxFunctionId + 1] : Array.Empty<int>();
            Array.Fill(indexById, -1);
            for (var i = 0; i < modulePlan.Functions.Count; i++)
            {
                indexById[modulePlan.Functions[i].Id.Value] = i;
            }

            for (var i = 0; i < modulePlan.Functions.Count; i++)
            {
                var parent = modulePlan.Functions[i];
                for (var childIndex = 0; childIndex < parent.NestedFunctions.Length; childIndex++)
                {
                    var child = parent.NestedFunctions[childIndex];
                    if (child.IsValid &&
                        (uint)child.Value < (uint)indexById.Length)
                    {
                        var nestedIndex = indexById[child.Value];
                        if (nestedIndex >= 0)
                        {
                            parentByIndex[nestedIndex] = i;
                        }
                    }
                }
            }

            var freeNames = CollectFreeNames(modulePlan);
            var builders = CreateLayoutBuilders(modulePlan);
            var resolver = new CaptureResolver(modulePlan, parentByIndex, builders);

            for (var i = 0; i < modulePlan.Functions.Count; i++)
            {
                for (var nameIndex = 0; nameIndex < freeNames[i].Count; nameIndex++)
                {
                    resolver.EnsureUpvalue(i, freeNames[i][nameIndex]);
                }
            }

            for (var i = 0; i < modulePlan.Functions.Count; i++)
            {
                var function = modulePlan.Functions[i];
                function.UpvalueSlots = builders.Upvalues[i]?.ToArray() ?? Array.Empty<UpvalueSlot>();
                function.CapturedLocalSlots = builders.CapturedLocals[i]?.ToArray() ?? Array.Empty<UpvalueSlot>();
                if (function.IsDirectCallCandidate && !CanUseModuleDirectCall(function))
                {
                    function.IsDirectCallCandidate = false;
                    if (function.IsModuleFunction && function.Visibility == FunctionVisibility.InternalOnly)
                    {
                        function.Visibility = FunctionVisibility.ModuleVisible;
                    }
                }

                function.RequiresClosureObject = RequiresClosureObject(function);
                function.CanCacheClosureObject = function.RequiresClosureObject &&
                    function.UpvalueSlots.Length == 0 &&
                    !function.IsModuleFunction &&
                    function.IsLambda;
            }
        }

        private static void PlanSingleNonNestedFunction(FunctionPlan function)
        {
            function.UpvalueSlots = Array.Empty<UpvalueSlot>();
            function.CapturedLocalSlots = Array.Empty<UpvalueSlot>();
            if (function.IsDirectCallCandidate && !CanUseModuleDirectCall(function))
            {
                function.IsDirectCallCandidate = false;
                if (function.IsModuleFunction && function.Visibility == FunctionVisibility.InternalOnly)
                {
                    function.Visibility = FunctionVisibility.ModuleVisible;
                }
            }

            function.RequiresClosureObject = RequiresClosureObject(function);
            function.CanCacheClosureObject = function.RequiresClosureObject &&
                function.UpvalueSlots.Length == 0 &&
                !function.IsModuleFunction &&
                function.IsLambda;
        }

        private static bool HasNestedFunctions(ModulePlan modulePlan)
        {
            for (var i = 0; i < modulePlan.Functions.Count; i++)
            {
                if (modulePlan.Functions[i].NestedFunctions.Length != 0)
                {
                    return true;
                }
            }

            return false;
        }

        private static void PlanFlatModule(ModulePlan modulePlan)
        {
            for (var i = 0; i < modulePlan.Functions.Count; i++)
            {
                PlanSingleNonNestedFunction(modulePlan.Functions[i]);
            }
        }

        private static bool CanUseModuleDirectCall(FunctionPlan function)
        {
            return function.IsModuleFunction &&
                !function.HasDefaultParameters &&
                !function.UsesArgumentsObject &&
                function.UpvalueSlots.Length == 0 &&
                function.CapturedLocalSlots.Length == 0 &&
                (GetParameterCount(function) <= 7 ||
                    function.DirectCallDirective == DirectCallDirective.PreserveClosure);
        }

        private static bool RequiresClosureObject(FunctionPlan function)
        {
            if (function.DirectCallDirective == DirectCallDirective.PreserveClosure)
            {
                return true;
            }

            return function.Visibility != FunctionVisibility.InternalOnly || !function.IsDirectCallCandidate;
        }

        private static int GetParameterCount(FunctionPlan function)
        {
            var count = 0;
            for (var i = 0; i < function.LocalSlots.Length; i++)
            {
                if (function.LocalSlots[i].IsParameter)
                {
                    count++;
                }
            }

            return count;
        }

        private static List<string>[] CollectFreeNames(ModulePlan modulePlan)
        {
            var result = new List<string>[modulePlan.Functions.Count];
            for (var i = 0; i < modulePlan.Functions.Count; i++)
            {
                var collector = new FreeNameCollector(modulePlan.Functions[i]);
                collector.Visit(modulePlan.Functions[i].Declaration?.Body);
                result[i] = collector.Names;
            }
            return result;
        }

        private static LayoutBuilders CreateLayoutBuilders(ModulePlan modulePlan)
        {
            var count = modulePlan.Functions.Count;
            return new LayoutBuilders(
                new List<UpvalueSlot>[count],
                new Dictionary<string, UpvalueSlotId>[count],
                new List<UpvalueSlot>[count]);
        }

        private sealed class CaptureResolver
        {
            private readonly ModulePlan _modulePlan;
            private readonly int[] _parentByIndex;
            private readonly LayoutBuilders _builders;

            public CaptureResolver(
                ModulePlan modulePlan,
                int[] parentByIndex,
                LayoutBuilders builders)
            {
                _modulePlan = modulePlan;
                _parentByIndex = parentByIndex;
                _builders = builders;
            }

            public UpvalueSlotId EnsureUpvalue(int functionIndex, string name)
            {
                var map = GetUpvalueMap(functionIndex);
                if (map.TryGetValue(name, out var existing))
                {
                    return existing;
                }

                var parentIndex = _parentByIndex[functionIndex];
                if (parentIndex < 0)
                {
                    return UpvalueSlotId.Invalid;
                }

                var parent = _modulePlan.Functions[parentIndex];
                var childParentScopeId = _modulePlan.Functions[functionIndex].ParentLocalScopeId;
                if (TryResolveLocal(parent, childParentScopeId, name, out var parentLocal))
                {
                    var upvalue = AddUpvalue(functionIndex,
                        name,
                        parent.Id,
                        parentLocal,
                        UpvalueSlotId.Invalid,
                        isInherited: false);
                    AddCapturedLocal(parentIndex, upvalue);
                    return upvalue.Id;
                }

                var parentUpvalue = EnsureUpvalue(parentIndex, name);
                if (!parentUpvalue.IsValid)
                {
                    return UpvalueSlotId.Invalid;
                }

                return AddUpvalue(functionIndex,
                    name,
                    parent.Id,
                    LocalSlotId.Invalid,
                    parentUpvalue,
                    isInherited: true).Id;
            }

            private static bool TryResolveLocal(FunctionPlan function, int startScopeId, string name, out LocalSlotId slot)
            {
                var scopeId = startScopeId;
                if (scopeId < 0)
                {
                    scopeId = 0;
                }

                var locals = function.LocalSlots;
                while (scopeId >= 0)
                {
                    for (var i = locals.Length - 1; i >= 0; i--)
                    {
                        if (locals[i].ScopeId == scopeId && locals[i].Name == name)
                        {
                            slot = locals[i].Id;
                            return true;
                        }
                    }

                    scopeId = GetParentScopeId(function, scopeId);
                }

                slot = LocalSlotId.Invalid;
                return false;
            }

            private static int GetParentScopeId(FunctionPlan function, int scopeId)
            {
                var scopes = function.LocalScopes;
                return (uint)scopeId < (uint)scopes.Length ? scopes[scopeId].ParentId : -1;
            }

            private UpvalueSlot AddUpvalue(
                int functionIndex,
                string name,
                FunctionId sourceFunction,
                LocalSlotId sourceLocal,
                UpvalueSlotId sourceUpvalue,
                bool isInherited)
            {
                var map = GetUpvalueMap(functionIndex);
                if (map.TryGetValue(name, out var existing))
                {
                    return _builders.Upvalues[functionIndex][existing.Value];
                }

                var upvalues = _builders.Upvalues[functionIndex] ??= new List<UpvalueSlot>();
                var id = new UpvalueSlotId(upvalues.Count);
                var upvalue = new UpvalueSlot(id, name, sourceFunction, sourceLocal, sourceUpvalue, isInherited);
                map.Add(name, id);
                upvalues.Add(upvalue);
                return upvalue;
            }

            private Dictionary<string, UpvalueSlotId> GetUpvalueMap(int functionIndex)
            {
                return _builders.UpvalueMaps[functionIndex] ??= new Dictionary<string, UpvalueSlotId>(StringComparer.Ordinal);
            }

            private void AddCapturedLocal(int functionIndex, UpvalueSlot childUpvalue)
            {
                var captured = _builders.CapturedLocals[functionIndex] ??= new List<UpvalueSlot>();
                for (var i = 0; i < captured.Count; i++)
                {
                    if (captured[i].Name == childUpvalue.Name && captured[i].SourceLocal.Equals(childUpvalue.SourceLocal))
                    {
                        return;
                    }
                }

                var id = new UpvalueSlotId(captured.Count);
                captured.Add(new UpvalueSlot(
                    id,
                    childUpvalue.Name,
                    childUpvalue.SourceFunction,
                    childUpvalue.SourceLocal,
                    UpvalueSlotId.Invalid,
                    isInherited: false));
            }
        }

        private sealed class FreeNameCollector
        {
            private readonly FunctionPlan _function;
            private readonly Stack<int> _scopeStack = new();

            public FreeNameCollector(FunctionPlan function)
            {
                _function = function;
                Names = new List<string>();
                _nameSet = new HashSet<string>(StringComparer.Ordinal);
                _scopeStack.Push(GetScopeId(function.Declaration?.Body ?? function.Declaration, 0));
            }

            private readonly HashSet<string> _nameSet;
            public List<string> Names { get; }

            public void Visit(AstNode node)
            {
                if (node == null)
                {
                    return;
                }

                switch (node)
                {
                    case FunctionDeclaration nested when !ReferenceEquals(nested, _function.Declaration):
                        return;
                    case LambdaExpression:
                        return;
                    case BlockStatement block:
                        VisitBlock(block);
                        return;
                    case VariableDeclaration variable:
                        Visit(variable.Initializer);
                        return;
                    case NameExpression name:
                        AddName(name);
                        return;
                    case GetPropertyExpression getProperty:
                        Visit(getProperty.Object);
                        return;
                    case SetPropertyExpression setProperty:
                        Visit(setProperty.Object);
                        Visit(setProperty.Value);
                        return;
                    case MapKeyValueExpression mapEntry:
                        Visit(mapEntry.Value);
                        return;
                }

                var visitor = new ChildVisitor(this);
                AstTraversal.VisitChildren(node, ref visitor);
            }

            private void AddName(NameExpression name)
            {
                if (name.Identifier == null)
                {
                    return;
                }

                var value = name.Identifier.Value;
                if (!IsLocalVisible(value) && _nameSet.Add(value))
                {
                    Names.Add(value);
                }
            }

            private void VisitBlock(BlockStatement block)
            {
                WithNodeScope(block, () =>
                {
                    for (var i = 0; i < block.Statements.Count; i++)
                    {
                        Visit(block.Statements[i]);
                    }
                    for (var i = 0; i < block.Functions.Count; i++)
                    {
                        Visit(block.Functions[i]);
                    }
                });
            }

            private bool IsLocalVisible(string name)
            {
                var scopeId = CurrentScopeId;
                var locals = _function.LocalSlots;
                while (scopeId >= 0)
                {
                    for (var i = locals.Length - 1; i >= 0; i--)
                    {
                        if (locals[i].ScopeId == scopeId && locals[i].Name == name)
                        {
                            return true;
                        }
                    }

                    scopeId = GetParentScopeId(scopeId);
                }

                return false;
            }

            private int CurrentScopeId => _scopeStack.Count == 0 ? 0 : _scopeStack.Peek();

            private int GetScopeId(AstNode node, int fallback)
            {
                return node != null &&
                    _function.LocalScopeByNode != null &&
                    _function.LocalScopeByNode.TryGetValue(node, out var scopeId)
                    ? scopeId
                    : fallback;
            }

            private int GetParentScopeId(int scopeId)
            {
                var scopes = _function.LocalScopes;
                return (uint)scopeId < (uint)scopes.Length ? scopes[scopeId].ParentId : -1;
            }

            private void WithNodeScope(AstNode node, Action action)
            {
                var scopeId = GetScopeId(node, CurrentScopeId);
                if (scopeId == CurrentScopeId)
                {
                    action();
                    return;
                }

                _scopeStack.Push(scopeId);
                try
                {
                    action();
                }
                finally
                {
                    _scopeStack.Pop();
                }
            }
        }

        private readonly struct LayoutBuilders
        {
            public LayoutBuilders(
                List<UpvalueSlot>[] upvalues,
                Dictionary<string, UpvalueSlotId>[] upvalueMaps,
                List<UpvalueSlot>[] capturedLocals)
            {
                Upvalues = upvalues;
                UpvalueMaps = upvalueMaps;
                CapturedLocals = capturedLocals;
            }

            public List<UpvalueSlot>[] Upvalues { get; }
            public Dictionary<string, UpvalueSlotId>[] UpvalueMaps { get; }
            public List<UpvalueSlot>[] CapturedLocals { get; }
        }

        private readonly struct ChildVisitor : IAstChildVisitor
        {
            private readonly FreeNameCollector _collector;

            public ChildVisitor(FreeNameCollector collector)
            {
                _collector = collector;
            }

            public void Visit(AstNode node)
            {
                _collector.Visit(node);
            }
        }
    }
}
