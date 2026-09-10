using AuroraScript.Compiler.Ast;
using AuroraScript.Compiler.Ast.Expressions;
using AuroraScript.Compiler.Ast.Statements;
using AuroraScript.Compiler.Backend.Analysis;
using AuroraScript.Compiler.Backend.Code;
using AuroraScript.Compiler.Backend.Plans;
using AuroraScript.Runtime.Types;
using System;
using System.Reflection;
using System.Reflection.Emit;

namespace AuroraScript.Compiler.Backend.Emission
{
    internal sealed class ModuleInitializerEmitter
    {
        private readonly EmissionSession _session;
        private readonly ModulePlan _module;
        private readonly TypedCilEmitter _typed;
        private MethodInfo _initializer;
        private ILGenerator _il;
        private bool _defined;
        private bool _emitted;

        public ModuleInitializerEmitter(EmissionSession session, ModulePlan module, TypedCilEmitter typed)
        {
            _session = session ?? throw new ArgumentNullException(nameof(session));
            _module = module ?? throw new ArgumentNullException(nameof(module));
            _typed = typed ?? throw new ArgumentNullException(nameof(typed));
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
            _module.InitializerFunction.Method = _initializer;
            _defined = true;
        }

        public bool TryEmit(out MethodInfo initializer)
        {
            initializer = null;
            if (_emitted)
            {
                initializer = _initializer;
                return true;
            }

            Define();
            _typed.EmitInitializerBody(_il, EmitBody);
            _emitted = true;
            initializer = _initializer;
            return true;
        }

        private void EmitBody()
        {
            for (var i = 0; i < _module.Declaration.Imports.Count; i++)
            {
                var import = _module.Declaration.Imports[i];
                if (!import.Include)
                {
                    MarkSequencePoint(import);
                    EmitImportAlias(import);
                }
            }

            for (var i = 0; i < _module.Functions.Count; i++)
            {
                var function = _module.Functions[i];
                if (!CanMaterialize(function))
                {
                    continue;
                }

                EmitDefineFunction(_il, function);
            }

            for (var i = 0; i < _module.Declaration.Statements.Count; i++)
            {
                EmitModuleStatement(_module.Declaration.Statements[i]);
            }

            for (var i = 0; i < _module.Declaration.Imports.Count; i++)
            {
                var import = _module.Declaration.Imports[i];
                if (import.Include)
                {
                    MarkSequencePoint(import);
                    EmitInclude(import);
                }
            }
        }

        private static bool CanMaterialize(FunctionPlan function)
        {
            return function.IsModuleFunction &&
                function.UpvalueSlots.Length == 0 &&
                ClosureMaterializer.CanMaterialize(function, requireName: true);
        }

        private void EmitDefineFunction(ILGenerator il, FunctionPlan function)
        {
            il.Emit(OpCodes.Ldarg_0);
            il.Emit(OpCodes.Ldfld, TypedRuntimeMetadata.ContextModule);
            _session.Builder.LoadStringConstant(il, function.Name);
            ClosureMaterializer.EmitClosure(_session, il, function);
            il.Emit(OpCodes.Call, TypedRuntimeMetadata.DatumFromObject);
            il.Emit(OpCodes.Ldc_I4_1);
            il.Emit(function.IsNativeDeclared ? OpCodes.Ldc_I4_1 : OpCodes.Ldc_I4_0);
            il.Emit(_session.ForceModuleDefinitions ? OpCodes.Ldc_I4_1 : OpCodes.Ldc_I4_0);
            il.Emit(
                OpCodes.Callvirt,
                GetModuleDefineMethod(
                    function.Visibility == FunctionVisibility.Exported));
        }

        private void EmitImportAlias(ImportDeclaration import)
        {
            if (import.Name == null)
            {
                return;
            }

            _il.Emit(OpCodes.Ldarg_0);
            _il.Emit(OpCodes.Ldfld, TypedRuntimeMetadata.ContextModule);
            _session.Builder.LoadStringConstant(_il, import.Name.Value);
            _il.Emit(OpCodes.Ldarg_0);
            _il.Emit(OpCodes.Ldfld, TypedRuntimeMetadata.ContextGlobal);
            _session.Builder.LoadStringConstant(_il, import.Reference.FullPath);
            _il.Emit(OpCodes.Callvirt, TypedRuntimeMetadata.ScriptGlobalGetModuleByPath);
            _il.Emit(OpCodes.Call, TypedRuntimeMetadata.DatumFromObject);
            _il.Emit(OpCodes.Ldc_I4_0);
            _il.Emit(OpCodes.Ldc_I4_0);
            _il.Emit(_session.ForceModuleDefinitions ? OpCodes.Ldc_I4_1 : OpCodes.Ldc_I4_0);
            _il.Emit(
                OpCodes.Callvirt,
                TypedRuntimeMetadata.ScriptModuleDefineInternal);
        }

        private void EmitInclude(ImportDeclaration import)
        {
            _il.Emit(OpCodes.Ldarg_0);
            _il.Emit(OpCodes.Ldfld, TypedRuntimeMetadata.ContextModule);
            _il.Emit(OpCodes.Ldarg_0);
            _il.Emit(OpCodes.Ldfld, TypedRuntimeMetadata.ContextGlobal);
            _session.Builder.LoadStringConstant(_il, import.Reference.FullPath);
            _il.Emit(OpCodes.Callvirt, TypedRuntimeMetadata.ScriptGlobalGetModuleByPath);
            _il.Emit(_session.ForceModuleDefinitions ? OpCodes.Ldc_I4_1 : OpCodes.Ldc_I4_0);
            _il.Emit(OpCodes.Callvirt, TypedRuntimeMetadata.ScriptObjectCopyModuleExports);
        }

        private void EmitModuleStatement(AstNode node)
        {
            switch (node)
            {
                case null:
                case ModuleMetaStatement:
                case FunctionDeclaration:
                case ImportDeclaration:
                    return;
                case VariableDeclaration variable:
                    if (variable.IsDeclare)
                    {
                        return;
                    }
                    MarkSequencePoint(variable);
                    EmitVariableDeclaration(variable);
                    return;
                case EnumDeclaration enumDeclaration:
                    MarkSequencePoint(enumDeclaration);
                    EmitEnum(enumDeclaration);
                    return;
                case ExpressionStatement expressionStatement:
                    MarkSequencePoint(expressionStatement);
                    EmitExpressionDiscarded(expressionStatement.Expression);
                    return;
                default:
                    throw new NotSupportedException("Module initializer statement " + node.GetType().Name);
            }
        }

        private void MarkSequencePoint(AstNode node)
        {
            if (node == null)
            {
                return;
            }

            _session.Builder.MarkSequencePoint(node.Range, _il);
        }

        private void EmitVariableDeclaration(VariableDeclaration variable)
        {
            if (variable.Name != null)
            {
                EmitDefineDatum(
                    variable,
                    variable.Name.Value,
                    exported: variable.Access == MemberAccess.Export,
                    writable: !variable.IsConst);
                return;
            }

            throw new NotSupportedException("Module destructuring declaration");
        }

        private void EmitDefineDatum(
            VariableDeclaration declaration,
            string name,
            bool exported,
            bool writable)
        {
            _il.Emit(OpCodes.Ldarg_0);
            _il.Emit(OpCodes.Ldfld, TypedRuntimeMetadata.ContextModule);
            _session.Builder.LoadStringConstant(_il, name);
            if (_module.TryGetSymbol(name, out var symbolId) &&
                ReferenceEquals(_session.CompileSession.Symbols[symbolId].Declaration, declaration) &&
                _module.TryGetInlineConstantInfo(symbolId, out var constant))
            {
                EmitLiteral(ModuleConstInliningAnalyzer.CreateLiteralExpression(constant, SourceSpan.None));
            }
            else
            {
                _typed.EmitInitializerVariable(declaration);
            }
            _il.Emit(writable ? OpCodes.Ldc_I4_1 : OpCodes.Ldc_I4_0);
            _il.Emit(OpCodes.Ldc_I4_0);
            _il.Emit(_session.ForceModuleDefinitions ? OpCodes.Ldc_I4_1 : OpCodes.Ldc_I4_0);
            _il.Emit(OpCodes.Callvirt, GetModuleDefineMethod(exported));
        }

        private void EmitEnum(EnumDeclaration enumDeclaration)
        {
            if (enumDeclaration.Identifier == null)
            {
                return;
            }

            var enumLocal = _il.DeclareLocal(typeof(ScriptObject));
            _session.Builder.SetLocalSymInfo(enumLocal, enumDeclaration.Identifier.Value);
            _il.Emit(OpCodes.Newobj, TypedRuntimeMetadata.ScriptObjectConstructor);
            _il.Emit(OpCodes.Stloc, enumLocal);

            for (var i = 0; i < enumDeclaration.Elements.Count; i++)
            {
                var element = enumDeclaration.Elements[i];
                _il.Emit(OpCodes.Ldloc, enumLocal);
                _session.Builder.LoadStringConstant(_il, element.Name.Value);
                _il.Emit(OpCodes.Ldc_I4, element.Value);
                _il.Emit(OpCodes.Call, TypedRuntimeMetadata.DatumFromInt32);
                _il.Emit(OpCodes.Ldc_I4_0);
                _il.Emit(OpCodes.Ldc_I4_1);
                _il.Emit(OpCodes.Callvirt, TypedRuntimeMetadata.ScriptObjectDefineDatum);
            }

            _il.Emit(OpCodes.Ldarg_0);
            _il.Emit(OpCodes.Ldfld, TypedRuntimeMetadata.ContextModule);
            _session.Builder.LoadStringConstant(_il, enumDeclaration.Identifier.Value);
            _il.Emit(OpCodes.Ldloc, enumLocal);
            _il.Emit(OpCodes.Call, TypedRuntimeMetadata.DatumFromObject);
            _il.Emit(OpCodes.Ldc_I4_0);
            _il.Emit(OpCodes.Ldc_I4_0);
            _il.Emit(_session.ForceModuleDefinitions ? OpCodes.Ldc_I4_1 : OpCodes.Ldc_I4_0);
            _il.Emit(
                OpCodes.Callvirt,
                GetModuleDefineMethod(
                    enumDeclaration.Access == MemberAccess.Export));
        }

        private static MethodInfo GetModuleDefineMethod(bool exported)
        {
            return exported
                ? TypedRuntimeMetadata.ScriptModuleDefineExport
                : TypedRuntimeMetadata.ScriptModuleDefineInternal;
        }

        private void EmitLiteral(LiteralExpression expression) => _typed.EmitInitializerDatum(expression);
        private void EmitExpressionDiscarded(Expression expression) => _typed.EmitInitializerDiscarded(expression);
    }
}
