using AuroraScript.Compiler.Ast;
using AuroraScript.Compiler.Ast.Expressions;
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

            var indexById = new Dictionary<FunctionId, int>();
            var parentByIndex = new int[modulePlan.Functions.Count];
            Array.Fill(parentByIndex, -1);
            for (var i = 0; i < modulePlan.Functions.Count; i++)
            {
                indexById[modulePlan.Functions[i].Id] = i;
            }

            for (var i = 0; i < modulePlan.Functions.Count; i++)
            {
                var parent = modulePlan.Functions[i];
                for (var childIndex = 0; childIndex < parent.NestedFunctions.Length; childIndex++)
                {
                    if (indexById.TryGetValue(parent.NestedFunctions[childIndex], out var nestedIndex))
                    {
                        parentByIndex[nestedIndex] = i;
                    }
                }
            }

            var localIndexByName = BuildLocalIndexes(modulePlan);
            var freeNames = CollectFreeNames(modulePlan);
            var builders = CreateLayoutBuilders(modulePlan);
            var resolver = new CaptureResolver(modulePlan, parentByIndex, localIndexByName, builders);

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

                function.RequiresClosureObject = function.Visibility != FunctionVisibility.InternalOnly || !function.IsDirectCallCandidate;
                function.CanCacheClosureObject = function.RequiresClosureObject &&
                    function.UpvalueSlots.Length == 0 &&
                    !function.IsModuleFunction &&
                    function.IsLambda;
            }
        }

        private static bool CanUseModuleDirectCall(FunctionPlan function)
        {
            return function.IsModuleFunction &&
                function.Visibility == FunctionVisibility.InternalOnly &&
                !function.HasDefaultParameters &&
                !function.UsesArgumentsObject &&
                function.UpvalueSlots.Length == 0 &&
                function.CapturedLocalSlots.Length == 0 &&
                GetParameterCount(function) <= 7;
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

        private static Dictionary<string, LocalSlotId>[] BuildLocalIndexes(ModulePlan modulePlan)
        {
            var result = new Dictionary<string, LocalSlotId>[modulePlan.Functions.Count];
            for (var i = 0; i < modulePlan.Functions.Count; i++)
            {
                var locals = modulePlan.Functions[i].LocalSlots;
                var map = new Dictionary<string, LocalSlotId>(StringComparer.Ordinal);
                for (var slotIndex = 0; slotIndex < locals.Length; slotIndex++)
                {
                    map.TryAdd(locals[slotIndex].Name, locals[slotIndex].Id);
                }
                result[i] = map;
            }
            return result;
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
            private readonly Dictionary<string, LocalSlotId>[] _localIndexByName;
            private readonly LayoutBuilders _builders;

            public CaptureResolver(
                ModulePlan modulePlan,
                int[] parentByIndex,
                Dictionary<string, LocalSlotId>[] localIndexByName,
                LayoutBuilders builders)
            {
                _modulePlan = modulePlan;
                _parentByIndex = parentByIndex;
                _localIndexByName = localIndexByName;
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
                if (_localIndexByName[parentIndex].TryGetValue(name, out var parentLocal))
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
            private readonly HashSet<string> _locals = new(StringComparer.Ordinal);

            public FreeNameCollector(FunctionPlan function)
            {
                _function = function;
                for (var i = 0; i < function.LocalSlots.Length; i++)
                {
                    _locals.Add(function.LocalSlots[i].Name);
                }

                Names = new List<string>();
                _nameSet = new HashSet<string>(StringComparer.Ordinal);
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
                if (!_locals.Contains(value) && _nameSet.Add(value))
                {
                    Names.Add(value);
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
