using AuroraScript.Compiler.Ast;
using AuroraScript.Compiler.Ast.Expressions;
using AuroraScript.Compiler.Ast.Statements;
using AuroraScript.Compiler.Emits.Builders;
using AuroraScript.Core;
using AuroraScript.Runtime;
using AuroraScript.Runtime.Types;
using AuroraScript.Tokens;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;

namespace AuroraScript.Compiler.Emits
{
    internal class CILEmitter(AbstractCILBuilder builder, EngineOptions Options) : IAstVisitor
    {
        private record ModuleState(String Name, int Hash, MethodInfo Init, ILGenerator IL, Dictionary<FunctionDeclaration, MethodInfo> Methods)
        {

        }

        internal struct LocalCaptureInfo
        {
            public LocalBuilder Array;
            public int Index;
        }

        private CodeScope _scope = new CodeScope(null, ScopeType.Global);
        private ILGenerator _il;
        private readonly Dictionary<string, ModuleState> _modules = new();
        private ModuleState _currentModule;
        private HotPatchType _patchType = 0;
        private bool IsPatching => _patchType != 0;

        private readonly Dictionary<DeclareObject, LocalBuilder> _locals = new();
        private readonly Dictionary<DeclareObject, int> _upvalueMap = new();
        private readonly Dictionary<DeclareObject, LocalCaptureInfo> _localScopeCaptureIndex = new();
        private LocalBuilder _scopeUpvaluesArray;
        private IEnumerable<string> _nextBlockParameters;
        private readonly CILStackManager _stackManager = new();
        private readonly Stack<Label> _breakLabels = new();
        private readonly Stack<Label> _continueLabels = new();
        private readonly Dictionary<object, LocalBuilder> _constantPool = new();
        private int ilOffset = -1;
        private void EmitNodeLocation(AstNode node)
        {
            UnionNumber union = new UnionNumber(_currentModule.Hash, node.LineNumber);
            if (ilOffset == _il.ILOffset) return;
            // load ctx 
            _il.Emit(OpCodes.Ldarg_0);
            _il.Emit(OpCodes.Ldc_I8, union.Int64Value);
            _il.Emit(OpCodes.Stfld, RuntimeMetadata.CILContext_Location);
            ilOffset = _il.ILOffset;
        }



        public DynamicCallMethod VisitHotPatch(ModuleDeclaration mainModule, ModuleDeclaration[] modules, HotPatchType patchType, IEnumerable<string> existingProperties)
        {
            _patchType = patchType;
            var (patchMethod, patchIL) = builder.DefineDynamicMethod(mainModule);
            var pathHash = mainModule.ModulePath.GetHashCode();
            _modules[mainModule.ModuleName] = new ModuleState(mainModule.ModuleName, pathHash, patchMethod, patchIL, new Dictionary<FunctionDeclaration, MethodInfo>());
            _il = patchIL;

            // 1. Dependency registration and initialization
            var globalLoc = _il.DeclareLocal(typeof(ScriptGlobal));
            _il.Emit(OpCodes.Ldarg_0);
            _il.Emit(OpCodes.Ldfld, RuntimeMetadata.CILContext_Global);
            _il.Emit(OpCodes.Stloc, globalLoc);

            foreach (var module in modules)
            {
                var moduleName = module.ModuleName;
                var (moduleInitMethod, moduleIL) = builder.DefineModuleInitMethod(module);
                var depPathHash = module.ModulePath.GetHashCode();
                _modules[moduleName] = new ModuleState(moduleName, depPathHash, moduleInitMethod, moduleIL, new Dictionary<FunctionDeclaration, MethodInfo>());

                // Register/Ensure
                _il.Emit(OpCodes.Ldloc, globalLoc);
                builder.LoadStringConstant(_il, moduleName);
                builder.LoadStringConstant(_il, module.ModulePath);
                _il.Emit(OpCodes.Callvirt, RuntimeMetadata.ScriptGlobal_EnsureModule);

                if ((_patchType & HotPatchType.Replace) != 0)
                {
                    _il.Emit(OpCodes.Dup);
                    _il.Emit(OpCodes.Callvirt, RuntimeMetadata.ScriptObject_ClearProperties);
                }
                _il.Emit(OpCodes.Pop);
            }

            // 2. Initialize dependencies
            foreach (var module in modules)
            {
                var state = _modules[module.ModuleName];
                _currentModule = state;
                _il = state.IL;
                _constantPool.Clear();
                var depHoister = new ConstantHoister();
                var depStats = depHoister.GetLiteralStats(module);
                InitializeConstantPool(depStats);

                _scope = _scope.Enter(ScopeType.Module);
                module.Accept(this); // Generate body of dependency
                _scope = _scope.Leave();

                _il = patchIL; // Switch back to patch IL for the initialization call
                _il.Emit(OpCodes.Ldarg_0);
                _il.Emit(OpCodes.Ldloc, globalLoc);
                builder.LoadStringConstant(_il, module.ModuleName);
                _il.Emit(OpCodes.Callvirt, RuntimeMetadata.ScriptGlobal_GetModule);
                _il.Emit(OpCodes.Ldnull);
                _il.Emit(OpCodes.Callvirt, RuntimeMetadata.CILContext_With);
                _il.Emit(OpCodes.Ldarg_1);
                _il.Emit(OpCodes.Call, state.Init);
            }

            // 3. Set up mainModule context
            _il.Emit(OpCodes.Ldarg_0);
            _il.Emit(OpCodes.Ldarg_0);
            _il.Emit(OpCodes.Ldfld, RuntimeMetadata.CILContext_Global);
            builder.LoadStringConstant(_il, mainModule.ModuleName);
            builder.LoadStringConstant(_il, mainModule.ModulePath);
            _il.Emit(OpCodes.Callvirt, RuntimeMetadata.ScriptGlobal_EnsureModule);

            if ((_patchType & HotPatchType.Replace) != 0)
            {
                _il.Emit(OpCodes.Dup);
                _il.Emit(OpCodes.Callvirt, RuntimeMetadata.ScriptObject_ClearProperties);
            }

            _il.Emit(OpCodes.Ldnull);
            _il.Emit(OpCodes.Callvirt, RuntimeMetadata.CILContext_With);
            _il.Emit(OpCodes.Starg_S, (byte)0);

            // 4. Generate implementation for main module
            _currentModule = _modules[mainModule.ModuleName];
            _il = _currentModule.IL;
            ilOffset = -1;
            _constantPool.Clear();
            var hoister = new ConstantHoister();
            var stats = hoister.GetLiteralStats(mainModule);
            InitializeConstantPool(stats);

            _scope = _scope.Enter(ScopeType.Module);
            foreach (var prop in existingProperties)
            {
                _scope.Declare(prop, DeclareType.Property, MemberAccess.Internal);
            }
            VisitBlock(mainModule);
            _scope = _scope.Leave();

            var lastnode = mainModule.ChildNodes.LastOrDefault();
            if (lastnode is not ReturnStatement)
            {
                // Mark sequence point for the implicit return at the end of the module
                var endRange = mainModule.Range;
                endRange.StartLine = endRange.EndLine;
                endRange.StartColumn = endRange.EndColumn;
                endRange.EndColumn++;
                builder.MarkSequencePoint(endRange, _il);

                _il.Emit(OpCodes.Ldsfld, RuntimeMetadata.ScriptDatum_Null);
                _il.Emit(OpCodes.Ret);
            }

            builder.FinalizeBuild();
            return (DynamicCallMethod)patchMethod.CreateDelegate(typeof(DynamicCallMethod));
        }


        public void Visit(ModuleDeclaration[] modules)
        {
            // 1. First pass: Define types and method headers for all modules

            // 2. Generate the main entry point: InitializeDomain(ScriptDomain)
            GenerateCreateDomain(modules);
            // 3. Second pass: Generate implementation for each module
            foreach (var module in modules)
            {
                module.Accept(this);
            }
            // 4. Finalize all types
            builder.FinalizeBuild();
        }




        private void WriteLocalSymbol(LocalBuilder local, String name)
        {
            builder.SetLocalSymInfo(local, name);
        }
        private void WriteLocalSymbol(LocalBuilder local, AstNode node)
        {
            switch (node)
            {
                case NameExpression name:
                    builder.SetLocalSymInfo(local, name.Identifier.Value);
                    break;
                case ParameterDeclaration parameter:
                    builder.SetLocalSymInfo(local, parameter.Name.Value);
                    break;
                case VariableDeclaration varDecl:
                    builder.SetLocalSymInfo(local, varDecl.Name.Value);
                    break;
                case FunctionDeclaration funcDecl:
                    builder.SetLocalSymInfo(local, funcDecl.Name.Value);
                    break;
                default:
                    throw new Exception();
            }
        }


        private void GenerateCreateDomain(ModuleDeclaration[] modules)
        {

            var (_, il) = builder.DefineDomainInitMethod();

            // Local 0: ScriptGlobal global
            var globalLoc = il.DeclareLocal(typeof(ScriptGlobal));
            WriteLocalSymbol(globalLoc, "global");
            // global = domain.Global
            il.Emit(OpCodes.Ldarg_0);
            il.Emit(OpCodes.Ldfld, RuntimeMetadata.CILContext_Global);
            il.Emit(OpCodes.Stloc, globalLoc);

            // Step 1: Register all modules

            for (int i = 0; i < modules.Length; i++)
            {
                // 1. Define Module Init Method
                var module = modules[i];
                var moduleName = module.ModuleName;
                var (moduleInitMethod, moduleIL) = builder.DefineModuleInitMethod(module);

                // ModuleState update
                var pathHash = module.ModulePath.GetHashCode();
                _modules[moduleName] = new ModuleState(moduleName, pathHash, moduleInitMethod, moduleIL, new Dictionary<FunctionDeclaration, MethodInfo>());

                // 2. Register Module
                il.Emit(OpCodes.Ldloc, globalLoc);
                builder.LoadStringConstant(il, moduleName);
                // moduleIdx
                il.Emit(OpCodes.Ldc_I4, pathHash);
                // new ScriptModule(name)
                builder.LoadStringConstant(il, moduleName);
                builder.LoadStringConstant(il, module.ModulePath);
                il.Emit(OpCodes.Newobj, RuntimeMetadata.ScriptModule_Ctor);
                // global.RegisterModule(name, idx, module)
                il.Emit(OpCodes.Callvirt, RuntimeMetadata.ScriptGlobal_RegisterModule);
            }



            // Step 2: Initialize each module
            foreach (var item in modules)
            {
                var moduleName = item.ModuleName;
                var state = _modules[moduleName];
                var init = state.Init;

                // CILContext
                il.Emit(OpCodes.Ldarg_0);
                // GetModule
                il.Emit(OpCodes.Ldloc, globalLoc);
                builder.LoadStringConstant(il, moduleName);
                il.Emit(OpCodes.Callvirt, RuntimeMetadata.ScriptGlobal_GetModule);
                // closure
                il.Emit(OpCodes.Ldnull);
                il.Emit(OpCodes.Callvirt, RuntimeMetadata.CILContext_With);
                // 
                il.Emit(OpCodes.Ldarg_1);
                // Call Initialize
                il.Emit(OpCodes.Call, init);
            }
            il.Emit(OpCodes.Ret);
        }

        protected override void BeforeVisitNode(AstNode node)
        {
            if (!node.IsIndependent)
            {
                return;
            }

            switch (node)
            {
                case ImportDeclaration:
                case FunctionDeclaration:
                case WhileStatement:
                case ForStatement:
                case ForInStatement:
                    return;
                default:
                    break;
            }
            EmitNodeLocation(node);
            builder.MarkSequencePoint(node, _il);
        }

        protected override void AfterVisitNode(AstNode node) { }

        /// <summary>
        /// 告诉栈顶压入的什么类型
        /// </summary>
        /// <param name="type"></param>
        private void PushType(Type type) => _stackManager.Push(type);

        /// <summary>
        /// 一条 IL 指令从真实栈中消费（弹出）了一个值
        /// </summary>
        /// <returns></returns>
        private Type PopType() => _stackManager.Pop();

        /// <summary>
        /// 接下来的指令需要栈顶是一个特定的类型，如果当前类型不匹配，自动生成转换指令
        /// </summary>
        /// <param name="targetType"></param>
        private void EnsureTop(Type targetType) => _stackManager.EnsureTop(_il, targetType);


        protected override void VisitModule(ModuleDeclaration node)
        {
            _scope = _scope.Enter(ScopeType.Module);
            var moduleName = node.ModuleName;
            _currentModule = _modules[moduleName];
            _il = _currentModule.IL;
            ilOffset = -1;
            _constantPool.Clear();
            var hoister = new ConstantHoister();
            var stats = hoister.GetLiteralStats(node);
            InitializeConstantPool(stats);

            VisitBlock(node);

            _il.Emit(OpCodes.Ret);
            _scope = _scope.Leave();
        }

        protected override void VisitImportDeclaration(ImportDeclaration node)
        {
            if (node.Include) return;
            var targetModuleName = node.ModuleName;
            var localAlias = node.Name.Value;
            // 1. Declare in scope
            _scope.Declare(localAlias, DeclareType.Property, MemberAccess.Internal);

            // 2. Emit CIL to find the module and define it on the current module
            // 3. currentModule.Define(localAlias, targetModule, writable: false, enumerable: true)
            _il.Emit(OpCodes.Ldarg_0); // current CILContext
            _il.Emit(OpCodes.Ldfld, RuntimeMetadata.CILContext_Module); // get Module Field
            // local alias
            builder.LoadStringConstant(_il, localAlias);
            {
                _il.Emit(OpCodes.Ldarg_0); // ScriptDomain
                _il.Emit(OpCodes.Ldfld, RuntimeMetadata.CILContext_Global); // get Global Field
                //// target module name
                builder.LoadStringConstant(_il, targetModuleName);
                _il.Emit(OpCodes.Callvirt, RuntimeMetadata.ScriptGlobal_GetModule); // Global.GetModule(targetModuleName)
            }
            _il.Emit(OpCodes.Ldc_I4_0); // writable
            _il.Emit(OpCodes.Ldc_I4_1); // enumerable
            _il.Emit(OpCodes.Callvirt, RuntimeMetadata.ScriptObject_Define);
        }

        protected override void VisitVarDeclaration(VariableDeclaration node)
        {
            if (node.Name == null && node.Pattern == null) return;
            if (node.IsConst && node.Initializer != null && node.Initializer is not LiteralExpression)
            {
                // fold const
                node.TryFolding(new Ast.EvaluationContext(_scope));
            }
            if (node.Pattern != null)
            {
                // Destructuring declaration: var { a, b } = initializer;
                if (node.Initializer == null) throw new AuroraEmitException((AstNode)null, "Destructuring declaration must have an initializer.");
                node.Initializer.Accept(this);
                node.Pattern.Accept(this);
                return;
            }
            _scope.Declare(node.Name.Value, _scope.ScopeType == Core.ScopeType.Module ? Core.DeclareType.Property : Core.DeclareType.Variable, node.Access, node);
            // If we are at the module level (root scope), register as module property
            if (_scope.ScopeType == ScopeType.Module) // Assuming global scope in emitter means module root
            {
                _il.Emit(OpCodes.Ldarg_0); // ScriptModule
                _il.Emit(OpCodes.Ldfld, RuntimeMetadata.CILContext_Module); // ScriptModule
                builder.LoadStringConstant(_il, node.Name.Value);
                if (node.Initializer != null)
                {
                    node.Initializer.Accept(this);
                    EnsureTop(typeof(ScriptObject));
                    PopType();
                }
                else
                {
                    _il.Emit(OpCodes.Ldsfld, RuntimeMetadata.NullValue_Instance);
                }
                _il.Emit(node.IsConst ? OpCodes.Ldc_I4_0 : OpCodes.Ldc_I4_1); // writable: isConst false
                _il.Emit(OpCodes.Ldc_I4_1); // enumerable: true
                if (IsPatching)
                {
                    _il.Emit(OpCodes.Callvirt, RuntimeMetadata.ScriptObject_Patch);
                }
                else
                {
                    _il.Emit(OpCodes.Callvirt, RuntimeMetadata.ScriptObject_Define);
                }
            }
            else
            {
                // Local variable handling
                var declare = _scope.Declare(node.Name.Value, Core.DeclareType.Variable, node.Access, node);
                if (_localScopeCaptureIndex.TryGetValue(declare, out var locCapture))
                {
                    if (node.Initializer != null)
                    {
                        _il.Emit(OpCodes.Ldloc, locCapture.Array);
                        _il.Emit(OpCodes.Ldc_I4, locCapture.Index);
                        _il.Emit(OpCodes.Ldelem, typeof(Upvalue));
                        // Value is pushed by right side
                        node.Initializer.Accept(this);
                        EnsureTop(typeof(ScriptDatum));
                        PopType(); // Pop the value that was just stored
                        _il.Emit(OpCodes.Stfld, RuntimeMetadata.Upvalue_Value);
                    }
                }
                else
                {
                    if (!_locals.TryGetValue(declare, out var local))
                    {
                        // Optimization: if it's a const and initialized with a literal that is already pooled, alias it.
                        if (node.IsConst && node.Initializer is LiteralExpression litExpr)
                        {
                            object val = litExpr.Token switch
                            {
                                NumberToken n => n.NumberValue,
                                StringToken s => s.Value,
                                BooleanToken b => b.BoolValue,
                                NullToken => ScriptObject.Null,
                                _ => null
                            };
                            if (val != null && _constantPool.TryGetValue(val, out var poolLocal))
                            {
                                _locals[declare] = poolLocal;
                                return;
                            }
                        }

                        // Restricted type inference: only infer types assignable to ScriptObject
                        Type localType = typeof(ScriptDatum);
                        if (node.Initializer != null)
                        {
                            node.Initializer.Accept(this);
                            var resultType = _stackManager.Peek();
                            if (typeof(ScriptObject).IsAssignableFrom(resultType))
                            {
                                localType = resultType;
                            }
                            local = _il.DeclareLocal(localType);
                            WriteLocalSymbol(local, node);
                            _locals[declare] = local;

                            EnsureTop(localType);
                            PopType();
                            _il.Emit(OpCodes.Stloc, local);
                        }
                        else
                        {
                            local = _il.DeclareLocal(localType);
                            WriteLocalSymbol(local, node);
                            _locals[declare] = local;
                        }
                    }
                    else
                    {
                        if (node.IsConst) throw new AuroraEmitException(node, $"Variables '{node.Name.Value}' cannot be redefined.");
                    }
                }
            }
        }


        protected override void VisitIncludedExpression(IncludedExpression node)
        {
            // Implementation of the 'in' operator (e.g., 'a' in obj)
            // high performance implementation using CILHelper.Included

            // 1. Evaluate collection (Right)
            node.Right.Accept(this);
            EnsureTop(typeof(ScriptObject));
            PopType();

            // 2. Evaluate element to search (Left)
            node.Left.Accept(this);
            EnsureTop(typeof(ScriptDatum));
            PopType();

            // 3. Call CILHelper.Included(ScriptObject collection, ScriptDatum value)
            _il.Emit(OpCodes.Call, RuntimeMetadata.CILHelper_Included);

            if (!node.NeedResult)
            {
                _il.Emit(OpCodes.Pop);
            }
            else
            {
                PushType(typeof(ScriptDatum));
            }
        }


        protected override void VisitLiteralExpression(LiteralExpression node)
        {
            object val = node.Token switch
            {
                NumberToken n => n.NumberValue,
                StringToken s => s.Value,
                BooleanToken b => b.BoolValue,
                NullToken n => ScriptObject.Null,
                _ => null
            };

            if (val != null && _constantPool.TryGetValue(val, out var local))
            {
                _il.Emit(OpCodes.Ldloc, local);
                PushType(typeof(ScriptDatum));
                return;
            }

            if (node.Token is StringToken stringToken)
            {
                var loaded = builder.LoadString(_il, stringToken.Value);
                if (loaded == LoadState.Constant)
                {
                    PushType(typeof(string));
                }
                else
                {
                    PushType(typeof(ScriptDatum));
                }
            }
            else if (node.Token is NumberToken numberToken)
            {
                var loaded = builder.LoadNumber(_il, numberToken.NumberValue);
                if (loaded == LoadState.Constant)
                {
                    PushType(typeof(double));
                }
                else
                {
                    PushType(typeof(ScriptDatum));
                }
            }
            else if (node.Token is BooleanToken booleanToken)
            {
                var loaded = builder.LoadBoolean(_il, booleanToken.BoolValue);
                if (loaded == LoadState.Constant)
                {
                    PushType(typeof(bool));
                }
                else
                {
                    PushType(typeof(ScriptDatum));
                }
            }

            else if (node.Token is RegexToken regex)
            {
                builder.LoadStringConstant(_il, regex.Pattern);
                builder.LoadStringConstant(_il, regex.Flags);
                _il.Emit(OpCodes.Call, RuntimeMetadata.RegexManager_LoadRegex);
                PushType(typeof(ScriptRegex));
            }
            else if (node.Token is NullToken)
            {
                builder.LoadNull(_il);
                PushType(typeof(ScriptDatum));
            }
            else
            {
                throw new Exception();
            }
        }

        protected override void VisitName(NameExpression node)
        {
            var name = node.Identifier.Value;
            if (name == "$args")
            {
                _il.Emit(OpCodes.Ldarg_1);
                _il.Emit(OpCodes.Newobj, RuntimeMetadata.ScriptArray_Ctor);
                PushType(typeof(ScriptArray));
                return;
            }
            if (name == "$state")
            {
                _il.Emit(OpCodes.Ldarg_0);
                _il.Emit(OpCodes.Ldfld, RuntimeMetadata.CILContext_UserState);
                PushType(typeof(ScriptObject));
                return;
            }
            if (name == "global")
            {
                _il.Emit(OpCodes.Ldarg_0);
                _il.Emit(OpCodes.Ldfld, RuntimeMetadata.CILContext_Global);
                PushType(typeof(ScriptObject));
                return;
            }

            _scope.Resolve(node.Identifier.Value, out var val);
            if (val.Type == DeclareType.Variable)
            {
                if (_localScopeCaptureIndex.TryGetValue(val, out var locCapture))
                {
                    // Load from local Upvalue array (including shared master array)
                    _il.Emit(OpCodes.Ldloc, locCapture.Array);
                    _il.Emit(OpCodes.Ldc_I4, locCapture.Index);
                    _il.Emit(OpCodes.Ldelem, typeof(Upvalue));
                    _il.Emit(OpCodes.Ldfld, RuntimeMetadata.Upvalue_Value);
                }
                else if (_upvalueMap.TryGetValue(val, out int upIdx))
                {
                    // Load from Upvalues in CILContext (original inherited array)
                    _il.Emit(OpCodes.Ldarg_0);
                    _il.Emit(OpCodes.Ldfld, RuntimeMetadata.CILContext_Upvalues);
                    _il.Emit(OpCodes.Ldc_I4, upIdx);
                    _il.Emit(OpCodes.Ldelem, typeof(Upvalue));
                    _il.Emit(OpCodes.Ldfld, RuntimeMetadata.Upvalue_Value);
                }
                else if (val != null && _locals.TryGetValue(val, out var local))
                {
                    _il.Emit(OpCodes.Ldloc, local);
                    PushType(local.LocalType);
                    return;
                }
                else
                {
                    throw new Exception();
                }
                PushType(typeof(ScriptDatum));
            }
            else if (val.Type == DeclareType.Property)
            {
                // ScriptModule
                _il.Emit(OpCodes.Ldarg_0);
                _il.Emit(OpCodes.Ldfld, RuntimeMetadata.CILContext_Module);
                _il.Emit(OpCodes.Ldarg_0);
                builder.LoadStringConstant(_il, node.Identifier.Value);
                _il.Emit(OpCodes.Callvirt, RuntimeMetadata.ScriptObject_GetPropertyValue);
                PushType(typeof(ScriptObject));
            }
            else if (val.Type == DeclareType.Global)
            {
                _il.Emit(OpCodes.Ldarg_0); // CILContext
                _il.Emit(OpCodes.Ldfld, RuntimeMetadata.CILContext_Global); // .Global
                _il.Emit(OpCodes.Ldarg_0);
                builder.LoadStringConstant(_il, node.Identifier.Value);
                _il.Emit(OpCodes.Callvirt, RuntimeMetadata.ScriptObject_GetPropertyValue);
                PushType(typeof(ScriptObject));
            }
        }

        protected override void VisitGetPropertyExpression(GetPropertyExpression node)
        {
            // 1. Visit Object
            node.Object.Accept(this);
            EnsureTop(typeof(ScriptObject));
            PopType(); // Receiver Object
            _il.Emit(OpCodes.Ldarg_0);
            // 2. Property name
            if (node.Property is NameExpression nameExp)
            {
                builder.LoadStringConstant(_il, nameExp.Identifier.Value);
                PushType(typeof(string));
                PopType(); // Name
            }
            else
            {
                node.Property.Accept(this);
                EnsureTop(typeof(string));
                PopType(); // Name
            }
            // 3. Call GetPropertyValue
            _il.Emit(OpCodes.Callvirt, RuntimeMetadata.ScriptObject_GetPropertyValue);
            PushType(typeof(ScriptObject));
        }

        protected override void VisitArrayExpression(ArrayLiteralExpression node)
        {
            var hasSpread = node.ChildNodes.Any(x => x is SpreadExpression);
            if (!hasSpread)
            {
                var count = node.ChildNodes.Count();

                // 1. Create ScriptDatum[] array
                _il.Emit(OpCodes.Ldc_I4, count);
                _il.Emit(OpCodes.Newarr, typeof(ScriptDatum));

                // 2. Set elements
                int index = 0;
                foreach (var item in node.ChildNodes)
                {
                    _il.Emit(OpCodes.Dup); // Duplicate array reference
                    _il.Emit(OpCodes.Ldc_I4, index);
                    if (item != null)
                    {
                        item.Accept(this);
                        EnsureTop(typeof(ScriptDatum));
                        PopType();
                    }
                    else
                    {
                        // Elided element
                        builder.LoadNull(_il);
                    }
                    _il.Emit(OpCodes.Stelem, typeof(ScriptDatum));
                    index++;
                }

                // 3. Create ScriptArray from ScriptDatum[]
                _il.Emit(OpCodes.Newobj, RuntimeMetadata.ScriptArray_Ctor);
            }
            else
            {
                // Logic for arrays with spreads
                _il.Emit(OpCodes.Ldc_I4, 0); // Initial 
                _il.Emit(OpCodes.Newobj, RuntimeMetadata.ScriptArray_CtorCapacity);
                foreach (var item in node.ChildNodes)
                {
                    if (item is SpreadExpression spread)
                    {
                        _il.Emit(OpCodes.Dup); // Duplicate ScriptArray
                        spread.Expression.Accept(this);
                        EnsureTop(typeof(ScriptObject));
                        PopType();
                        _il.Emit(OpCodes.Call, RuntimeMetadata.CILHelper_SpreadInto);
                    }
                    else
                    {
                        _il.Emit(OpCodes.Dup); // Duplicate ScriptArray
                        if (item != null)
                        {
                            item.Accept(this);
                            EnsureTop(typeof(ScriptDatum));
                            PopType();
                        }
                        else
                        {
                            builder.LoadNull(_il);
                        }
                        _il.Emit(OpCodes.Callvirt, RuntimeMetadata.ScriptArray_Push);
                    }
                }
            }
            PushType(typeof(ScriptArray));
        }

        protected override void VisitGetElementExpression(GetElementExpression node)
        {
            node.Object.Accept(this);
            EnsureTop(typeof(ScriptObject));
            PopType(); // Object
            node.Index.Accept(this);
            EnsureTop(typeof(ScriptDatum));
            PopType(); // Index
            _il.Emit(OpCodes.Call, RuntimeMetadata.CILHelper_GetElement);
            PushType(typeof(ScriptDatum));
        }

        protected override void VisitMapExpression(MapExpression node)
        {
            // 1. Create a new ScriptObject
            _il.Emit(OpCodes.Newobj, RuntimeMetadata.ScriptObject_Ctor);
            foreach (var entry in node.ChildNodes)
            {
                if (entry is MapKeyValueExpression property)
                {
                    _il.Emit(OpCodes.Dup); // Duplicate ScriptObject for SetPropertyValue
                    _il.Emit(OpCodes.Ldarg_0);
                    builder.LoadStringConstant(_il, property.Key.Value);
                    property.Value.Accept(this);
                    EnsureTop(typeof(ScriptObject));
                    PopType();
                    _il.Emit(OpCodes.Callvirt, RuntimeMetadata.ScriptObject_SetPropertyValue);
                }
                else if (entry is SpreadExpression spread)
                {
                    _il.Emit(OpCodes.Dup); // Duplicate ScriptObject for CopyPropertysFrom

                    spread.Expression.Accept(this);
                    EnsureTop(typeof(ScriptObject));
                    PopType();
                    _il.Emit(OpCodes.Ldc_I4_0); // force = false
                    _il.Emit(OpCodes.Callvirt, RuntimeMetadata.ScriptObject_CopyPropertysFrom);
                }
                else if (entry is NameExpression shorthand)
                {
                    _il.Emit(OpCodes.Dup); // Duplicate ScriptObject for SetPropertyValue
                    _il.Emit(OpCodes.Ldarg_0);
                    builder.LoadStringConstant(_il, shorthand.Identifier.Value);
                    shorthand.Accept(this);
                    EnsureTop(typeof(ScriptObject));
                    PopType();
                    _il.Emit(OpCodes.Callvirt, RuntimeMetadata.ScriptObject_SetPropertyValue);
                }
            }
            PushType(typeof(ScriptObject));
        }

        protected override void VisitSetElementExpression(SetElementExpression node)
        {
            node.Object.Accept(this);
            EnsureTop(typeof(ScriptObject));
            PopType(); // Object
            node.Index.Accept(this);
            EnsureTop(typeof(ScriptDatum));
            PopType(); // Index
            node.Value.Accept(this);
            EnsureTop(typeof(ScriptDatum));
            PopType(); // Value

            _il.Emit(OpCodes.Call, RuntimeMetadata.CILHelper_SetElement);
        }

        protected override void VisitBinaryExpression(BinaryExpression node)
        {
            if (node.Operator == Operator.LogicalAnd)
            {
                var labelEnd = _il.DefineLabel();
                node.Left.Accept(this);
                EnsureTop(typeof(ScriptDatum));
                PopType();
                _il.Emit(OpCodes.Dup);
                _il.Emit(OpCodes.Call, RuntimeMetadata.CILHelper_ToBoolean);
                _il.Emit(OpCodes.Brfalse, labelEnd);
                _il.Emit(OpCodes.Pop);
                node.Right.Accept(this);
                EnsureTop(typeof(ScriptDatum));
                _il.MarkLabel(labelEnd);

                if (!node.NeedResult)
                {
                    _il.Emit(OpCodes.Pop);
                    PopType();
                }
                return;
            }
            if (node.Operator == Operator.LogicalOr)
            {
                var labelEnd = _il.DefineLabel();
                node.Left.Accept(this);
                EnsureTop(typeof(ScriptDatum));
                PopType();
                _il.Emit(OpCodes.Dup);
                _il.Emit(OpCodes.Call, RuntimeMetadata.CILHelper_ToBoolean);
                _il.Emit(OpCodes.Brtrue, labelEnd);
                _il.Emit(OpCodes.Pop);
                node.Right.Accept(this);
                EnsureTop(typeof(ScriptDatum));
                _il.MarkLabel(labelEnd);

                if (!node.NeedResult)
                {
                    _il.Emit(OpCodes.Pop);
                    PopType();
                }
                return;
            }

            node.Left.Accept(this);
            EnsureTop(typeof(ScriptDatum));
            PopType(); // Left

            node.Right.Accept(this);
            EnsureTop(typeof(ScriptDatum));
            PopType(); // Right
            MethodInfo method = node.Operator switch
            {
                var op when op == Operator.Add => RuntimeMetadata.CILHelper_Add,
                var op when op == Operator.Subtract => RuntimeMetadata.CILHelper_Subtract,
                var op when op == Operator.Multiply => RuntimeMetadata.CILHelper_Multiply,
                var op when op == Operator.Divide => RuntimeMetadata.CILHelper_Divide,
                var op when op == Operator.Modulo => RuntimeMetadata.CILHelper_Modulo,
                var op when op == Operator.Equal => RuntimeMetadata.CILHelper_Equal,
                var op when op == Operator.NotEqual => RuntimeMetadata.CILHelper_NotEqual,
                var op when op == Operator.LessThan => RuntimeMetadata.CILHelper_Less,
                var op when op == Operator.LessThanOrEqual => RuntimeMetadata.CILHelper_LessEqual,
                var op when op == Operator.GreaterThan => RuntimeMetadata.CILHelper_Greater,
                var op when op == Operator.GreaterThanOrEqual => RuntimeMetadata.CILHelper_GreaterEqual,
                var op when op == Operator.BitwiseAnd => RuntimeMetadata.CILHelper_BitwiseAnd,
                var op when op == Operator.BitwiseOr => RuntimeMetadata.CILHelper_BitwiseOr,
                var op when op == Operator.BitwiseXor => RuntimeMetadata.CILHelper_BitwiseXor,
                var op when op == Operator.LeftShift => RuntimeMetadata.CILHelper_LeftShift,
                var op when op == Operator.SignedRightShift => RuntimeMetadata.CILHelper_RightShift,
                var op when op == Operator.UnSignedRightShift => RuntimeMetadata.CILHelper_UnsignedRightShift,
                _ => throw new NotImplementedException($"Binary operator {node.Operator} not implemented")
            };
            _il.Emit(OpCodes.Call, method);
            PushType(typeof(ScriptDatum));
        }

        protected override void VisitUnaryExpression(UnaryExpression node)
        {
            if (node.Operator == Operator.PreIncrement || node.Operator == Operator.PostIncrement ||
                node.Operator == Operator.PreDecrement || node.Operator == Operator.PostDecrement)
            {
                EmitIncrementDecrement(node);
                return;
            }
            node.Expression.Accept(this);
            EnsureTop(typeof(ScriptDatum));
            PopType();
            MethodInfo method = node.Operator switch
            {
                var op when op == Operator.LogicalNot => RuntimeMetadata.CILHelper_Not,
                var op when op == Operator.BitwiseNot => RuntimeMetadata.CILHelper_BitwiseNot,
                var op when op == Operator.Negate => RuntimeMetadata.CILHelper_Negate,
                var op when op == Operator.TypeOf => RuntimeMetadata.CILHelper_TypeOf,
                _ => throw new NotImplementedException($"Unary operator {node.Operator} not implemented")
            };
            _il.Emit(OpCodes.Call, method);
            PushType(typeof(ScriptDatum));
        }

        protected override void VisitAssignmentExpression(AssignmentExpression node)
        {
            EmitAssignment(node.Left, node.Right, null, node.NeedResult);
        }

        protected override void VisitCompoundExpression(CompoundExpression node)
        {
            EmitAssignment(node.Left, node.Right, node.Operator.SimplerOperator, node.NeedResult);
        }

        private void EmitAssignment(Expression left, Expression right, Operator op, bool resultNeeded)
        {
            // 属性数组访问已统一优化为set语句
            if (left is NameExpression nameExp)
            {
                _scope.Resolve(nameExp.Identifier.Value, out var val);

                if (op != null)
                {
                    nameExp.Accept(this);
                    EnsureTop(typeof(ScriptDatum));
                    PopType(); // Left (current val)
                    right.Accept(this);
                    EnsureTop(typeof(ScriptDatum));
                    PopType(); // Right
                    MethodInfo opMethod = op switch
                    {
                        var o when o == Operator.Add => RuntimeMetadata.CILHelper_Add,
                        var o when o == Operator.Subtract => RuntimeMetadata.CILHelper_Subtract,
                        var o when o == Operator.Multiply => RuntimeMetadata.CILHelper_Multiply,
                        var o when o == Operator.Divide => RuntimeMetadata.CILHelper_Divide,
                        var o when o == Operator.Modulo => RuntimeMetadata.CILHelper_Modulo,
                        _ => throw new NotImplementedException($"Compound operator {op} not implemented")
                    };
                    _il.Emit(OpCodes.Call, opMethod);
                    PushType(typeof(ScriptDatum));

                    if (resultNeeded)
                    {
                        _il.Emit(OpCodes.Dup);
                        PushType(typeof(ScriptDatum));
                    }
                    EmitStoreName(nameExp, val, null);
                }
                else
                {
                    EmitStoreName(nameExp, val, () =>
                    {
                        right.Accept(this);
                        EnsureTop(typeof(ScriptDatum));
                        if (resultNeeded)
                        {
                            _il.Emit(OpCodes.Dup);
                            PushType(typeof(ScriptDatum));
                        }
                    });
                }
            }
        }

        private void EmitIncrementDecrement(UnaryExpression node)
        {
            var isPost = node.Type == UnaryType.Post;
            var isIncrement = node.Operator == Operator.PreIncrement || node.Operator == Operator.PostIncrement;
            if (node.Expression is NameExpression nameExp)
            {
                _scope.Resolve(nameExp.Identifier.Value, out var val);
                MethodInfo opMethod;
                if (isIncrement) opMethod = isPost ? RuntimeMetadata.CILHelper_IncrementPostfix : RuntimeMetadata.CILHelper_IncrementPrefix;
                else opMethod = isPost ? RuntimeMetadata.CILHelper_DecrementPostfix : RuntimeMetadata.CILHelper_DecrementPrefix;

                if (val.Type == DeclareType.Variable)
                {
                    if (_upvalueMap.TryGetValue(val, out int upIdx))
                    {
                        _il.Emit(OpCodes.Ldarg_0);
                        _il.Emit(OpCodes.Ldfld, RuntimeMetadata.CILContext_Upvalues);
                        _il.Emit(OpCodes.Ldc_I4, upIdx);
                        _il.Emit(OpCodes.Ldelem, typeof(Upvalue));
                        _il.Emit(OpCodes.Ldflda, RuntimeMetadata.Upvalue_Value);
                        _il.Emit(OpCodes.Call, opMethod);
                    }
                    else if (_localScopeCaptureIndex.TryGetValue(val, out var locCapture))
                    {
                        _il.Emit(OpCodes.Ldloc, locCapture.Array);
                        _il.Emit(OpCodes.Ldc_I4, locCapture.Index);
                        _il.Emit(OpCodes.Ldelem, typeof(Upvalue));
                        _il.Emit(OpCodes.Ldflda, RuntimeMetadata.Upvalue_Value);
                        _il.Emit(OpCodes.Call, opMethod);
                    }
                    else
                    {
                        var local = _locals[val];
                        if (local.LocalType == typeof(ScriptDatum))
                        {
                            _il.Emit(OpCodes.Ldloca, local);
                            _il.Emit(OpCodes.Call, opMethod);
                        }
                        else
                        {
                            throw new Exception();
                        }
                    }
                }
                else
                {
                    // For Global/Property targets, use the specialized helpers
                    if (val.Type == DeclareType.Property)
                    {
                        _il.Emit(OpCodes.Ldarg_0); // CILContext
                        _il.Emit(OpCodes.Ldfld, RuntimeMetadata.CILContext_Module);

                    }
                    else
                    {
                        _il.Emit(OpCodes.Ldarg_0); // CILContext
                        _il.Emit(OpCodes.Ldfld, RuntimeMetadata.CILContext_Global);
                    }
                    MethodInfo opPropMethod;
                    if (isIncrement) opPropMethod = isPost ? RuntimeMetadata.CILHelper_IncrementPropertyPostfix : RuntimeMetadata.CILHelper_IncrementPropertyPrefix;
                    else opPropMethod = isPost ? RuntimeMetadata.CILHelper_DecrementPropertyPostfix : RuntimeMetadata.CILHelper_DecrementPropertyPrefix;
                    builder.LoadStringConstant(_il, nameExp.Identifier.Value);
                    _il.Emit(OpCodes.Call, opPropMethod);
                }

                if (node.NeedResult)
                {
                    PushType(typeof(ScriptDatum));
                }
                else
                {
                    _il.Emit(OpCodes.Pop);
                }
            }
            else if (node.Expression is GetElementExpression getElement)
            {
                getElement.Object.Accept(this);
                EnsureTop(typeof(ScriptObject));
                PopType(); // Object
                getElement.Index.Accept(this);
                EnsureTop(typeof(ScriptDatum));
                PopType(); // Index
                MethodInfo opElemMethod;
                if (isIncrement)
                    opElemMethod = isPost ? RuntimeMetadata.CILHelper_IncrementElementPostfix : RuntimeMetadata.CILHelper_IncrementElementPrefix;
                else
                    opElemMethod = isPost ? RuntimeMetadata.CILHelper_DecrementElementPostfix : RuntimeMetadata.CILHelper_DecrementElementPrefix;

                _il.Emit(OpCodes.Call, opElemMethod);

                if (node.NeedResult)
                {
                    PushType(typeof(ScriptDatum));
                }
                else
                {
                    _il.Emit(OpCodes.Pop);
                }
            }
            else if (node.Expression is GetPropertyExpression getPropMut)
            {
                getPropMut.Object.Accept(this);
                EnsureTop(typeof(ScriptObject));
                PopType(); // Object
                if (getPropMut.Property is NameExpression propNameExpMut)
                {
                    MethodInfo opPropMethod;
                    if (isIncrement) opPropMethod = isPost ? RuntimeMetadata.CILHelper_IncrementPropertyPostfix : RuntimeMetadata.CILHelper_IncrementPropertyPrefix;
                    else opPropMethod = isPost ? RuntimeMetadata.CILHelper_DecrementPropertyPostfix : RuntimeMetadata.CILHelper_DecrementPropertyPrefix;
                    builder.LoadStringConstant(_il, propNameExpMut.Identifier.Value);
                    _il.Emit(OpCodes.Call, opPropMethod);
                    if (node.NeedResult)
                    {
                        PushType(typeof(ScriptDatum));
                    }
                    else
                    {
                        _il.Emit(OpCodes.Pop);
                    }
                }
                else throw new NotImplementedException("Dynamic property mutation not implemented");
            }
        }

        protected override void VisitIfStatement(IfStatement node)
        {
            var labelElse = _il.DefineLabel();
            var labelEnd = _il.DefineLabel();
            // 1. Emit condition
            node.Condition.Accept(this);
            var topType = PopType();
            if (topType == typeof(ScriptDatum))
            {
                _il.Emit(OpCodes.Call, RuntimeMetadata.CILHelper_ToBoolean);
            }
            else
            {
                _il.Emit(OpCodes.Call, RuntimeMetadata.CILHelper_ToBoolean2);
            }
            // 2. Branch
            _il.Emit(OpCodes.Brfalse, labelElse);
            // 3. Body
            node.Body?.Accept(this);
            // 4. Jump to end if there's an else block
            if (node.Else != null)
            {
                _il.Emit(OpCodes.Br, labelEnd);
            }
            // 5. Else block
            _il.MarkLabel(labelElse);
            node.Else?.Accept(this);
            // 6. End label
            if (node.Else != null)
            {
                _il.MarkLabel(labelEnd);
            }
        }

        protected override void VisitTryStatement(TryStatement node)
        {
            _il.BeginExceptionBlock();
            node.Body?.Accept(this);
            _il.BeginCatchBlock(typeof(Exception));
            if (node.CatchBody == null || string.IsNullOrEmpty(node.CatchVariable))
            {
                _il.Emit(OpCodes.Pop);
            }
            if (node.CatchBody != null)
            {
                // catch value is on stack
                if (!string.IsNullOrEmpty(node.CatchVariable))
                {
                    _il.Emit(OpCodes.Call, RuntimeMetadata.CILHelper_ExceptionToError);
                    PushType(typeof(ScriptDatum));
                    // Declare and store catch variable in current function scope
                    var identifierToken = new IdentifierToken { Value = node.CatchVariable, Range = SourceSpan.None };
                    var nameExpr = new NameExpression(identifierToken);
                    var val = _scope.Declare(node.CatchVariable, DeclareType.Variable, MemberAccess.Internal);
                    EmitStoreName(nameExpr, val, null);
                }
                node.CatchBody.Accept(this);
            }
            if (node.FinallyBody != null)
            {
                _il.BeginFinallyBlock();
                node.FinallyBody.Accept(this);
            }
            _il.EndExceptionBlock();
        }

        protected override void VisitThrowStatement(ThrowStatement node)
        {
            if (node.Expression != null)
            {
                node.Expression.Accept(this);
                EnsureTop(typeof(ScriptDatum));
                _il.Emit(OpCodes.Call, RuntimeMetadata.CILHelper_Throw);
                PopType();
            }
            else
            {
                _il.Emit(OpCodes.Rethrow);
            }
        }

        protected override void VisitWhileStatement(WhileStatement node)
        {
            var labelBegin = _il.DefineLabel();
            var labelEnd = _il.DefineLabel();

            _continueLabels.Push(labelBegin);
            _breakLabels.Push(labelEnd);

            // Header sequence point
            var headerRange = node.Range;
            if (node.Condition != null)
            {
                headerRange.EndLine = node.Condition.Range.EndLine;
                headerRange.EndColumn = node.Condition.Range.EndColumn;
            }
            if (headerRange.EndLine == headerRange.StartLine && headerRange.EndColumn <= headerRange.StartColumn)
            {
                headerRange.EndColumn = headerRange.StartColumn + 1;
            }
            builder.MarkSequencePoint(headerRange, _il);

            _il.MarkLabel(labelBegin);

            // 1. Condition
            node.Condition.Accept(this);
            var topType = PopType();
            if (topType == typeof(ScriptDatum))
            {
                _il.Emit(OpCodes.Call, RuntimeMetadata.CILHelper_ToBoolean);
            }
            else
            {
                _il.Emit(OpCodes.Call, RuntimeMetadata.CILHelper_ToBoolean2);
            }

            // 2. Branch to end if false
            _il.Emit(OpCodes.Brfalse, labelEnd);

            // 3. Body
            node.Body?.Accept(this);

            // 4. Loop back
            _il.Emit(OpCodes.Br, labelBegin);

            // 5. Mark end
            _il.MarkLabel(labelEnd);

            _continueLabels.Pop();
            _breakLabels.Pop();
        }

        protected override void VisitForStatement(ForStatement node)
        {
            var labelCondition = _il.DefineLabel();
            var labelIncrement = _il.DefineLabel();
            var labelEnd = _il.DefineLabel();

            _continueLabels.Push(labelIncrement);
            _breakLabels.Push(labelEnd);

            // Header sequence point
            var headerRange = node.Range;
            var lastHeaderNode = (AstNode)node.Incrementor ?? node.Condition ?? node.Initializer;
            if (lastHeaderNode != null)
            {
                headerRange.EndLine = lastHeaderNode.Range.EndLine;
                headerRange.EndColumn = lastHeaderNode.Range.EndColumn;
            }
            if (headerRange.EndLine == headerRange.StartLine && headerRange.EndColumn <= headerRange.StartColumn)
            {
                headerRange.EndColumn = headerRange.StartColumn + 1;
            }
            builder.MarkSequencePoint(headerRange, _il);

            // 1. Initializer
            node.Initializer?.Accept(this);
            //if (node.Initializer != null) PopType();
            // 2. Mark condition start
            _il.MarkLabel(labelCondition);

            // 3. Condition
            if (node.Condition != null)
            {
                node.Condition.Accept(this);
                var topType = PopType();
                if (topType == typeof(ScriptDatum))
                {
                    _il.Emit(OpCodes.Call, RuntimeMetadata.CILHelper_ToBoolean);
                }
                else
                {
                    _il.Emit(OpCodes.Call, RuntimeMetadata.CILHelper_ToBoolean2);
                }

                // Branch to end if false
                _il.Emit(OpCodes.Brfalse, labelEnd);
            }

            // 4. Body
            node.Body?.Accept(this);

            // 5. Mark increment
            _il.MarkLabel(labelIncrement);

            // 6. Incrementor
            node.Incrementor?.Accept(this);

            // 7. Loop back to condition
            _il.Emit(OpCodes.Br, labelCondition);

            // 8. Mark end
            _il.MarkLabel(labelEnd);

            _continueLabels.Pop();
            _breakLabels.Pop();
        }

        protected override void VisitForInStatement(ForInStatement node)
        {
            var labelCondition = _il.DefineLabel();
            var labelIncrement = _il.DefineLabel();
            var labelEnd = _il.DefineLabel();

            _continueLabels.Push(labelIncrement);
            _breakLabels.Push(labelEnd);

            // Header sequence point
            var headerRange = node.Range;
            var iterRange = node.Iterator?.Range ?? default;
            if (iterRange.StartLine <= 0 && node.Iterator != null)
            {
                // Fallback to Right expression range if Iterator range is missing
                iterRange = node.Iterator.Right?.Range ?? default;
            }

            if (iterRange.StartLine > 0)
            {
                headerRange.EndLine = iterRange.EndLine;
                headerRange.EndColumn = iterRange.EndColumn;
            }
            else if (node.Initializer != null && node.Initializer.Range.StartLine > 0)
            {
                headerRange.EndLine = node.Initializer.Range.EndLine;
                headerRange.EndColumn = node.Initializer.Range.EndColumn;
            }

            if (headerRange.EndLine == headerRange.StartLine && headerRange.EndColumn <= headerRange.StartColumn)
            {
                headerRange.EndColumn = headerRange.StartColumn + 1;
            }
            builder.MarkSequencePoint(headerRange, _il);

            // 1. Initializer declaration (if any)
            // for(var n in array) -> declares n
            node.Initializer?.Accept(this);


            // 2. Evaluate collection and get iterator
            node.Iterator.Right.Accept(this);
            EnsureTop(typeof(ScriptObject));
            PopType();
            //_il.Emit(OpCodes.Call, RuntimeMetadata.ScriptDatum_ToObject);
            _il.Emit(OpCodes.Callvirt, RuntimeMetadata.ScriptObject_GetIterator);

            var localIterator = _il.DeclareLocal(typeof(ScriptEnumerator));
            WriteLocalSymbol(localIterator, name: null);
            _il.Emit(OpCodes.Stloc, localIterator);

            // 3. Mark condition label
            _il.MarkLabel(labelCondition);

            // 
            var itemVar = node.Iterator.Left;
            _scope.Resolve(itemVar.Identifier.Value, out var resolved);
            var itemLocal = EnsureLocal(itemVar, resolved);

            _il.Emit(OpCodes.Ldloc, localIterator);
            _il.Emit(OpCodes.Ldloca, itemLocal);
            _il.Emit(OpCodes.Callvirt, RuntimeMetadata.ScriptEnumerator_NextValue);
            _il.Emit(OpCodes.Brfalse, labelEnd);

            // 5. Body
            node.Body?.Accept(this);

            // 6. Mark increment label (continue comes here)
            _il.MarkLabel(labelIncrement);
            _il.Emit(OpCodes.Br, labelCondition);

            // 7. Mark end label
            _il.MarkLabel(labelEnd);

            _continueLabels.Pop();
            _breakLabels.Pop();
        }

        protected override void VisitBlock(BlockStatement node)
        {
            var oldUpvalueMap = new Dictionary<DeclareObject, int>(_upvalueMap);
            var oldLocalScopeCaptureIndex = new Dictionary<DeclareObject, LocalCaptureInfo>(_localScopeCaptureIndex);
            var oldScopeUpvaluesArray = _scopeUpvaluesArray;


            // 1. Pre-declare all locals in this block
            var declVisitor = new DeclarationVisitor(_scope);
            if (node is ModuleDeclaration mod)
            {
                foreach (var i in mod.Imports) i.Accept(declVisitor);
                foreach (var f in mod.Functions) f.Accept(declVisitor);
                foreach (var s in mod.ChildNodes) s.Accept(declVisitor);
            }
            else
            {
                foreach (var s in node.ChildNodes) s.Accept(declVisitor);
                foreach (var f in node.Functions) f.Accept(declVisitor);
            }


            // 2. Identify captured variables in this block
            var analyzer = new ClosureAnalyzer();
            var paramNames = _nextBlockParameters;
            _nextBlockParameters = null;

            // We analyze to find escaped locals. For function bodies, we also pass parameter names to see if they escape.
            analyzer.Analyze(node, _scope, paramNames);

            if (analyzer.EscapedLocals.Count > 0) // and  || analyzer.Upvalues.Any()
            {
                // We have new local variables that escape from this block.
                // We must create a new Master Upvalue Array that combines inherited upvalues and these new locals.
                var inheritedVars = analyzer.Upvalues.OrderBy(x => x).ToList();
                var localVars = analyzer.EscapedLocals.OrderBy(x => x).ToList();

                _scopeUpvaluesArray = _il.DeclareLocal(typeof(Upvalue[]));
                WriteLocalSymbol(_scopeUpvaluesArray, name: null);
                _il.Emit(OpCodes.Ldc_I4, inheritedVars.Count + localVars.Count);
                _il.Emit(OpCodes.Newarr, typeof(Upvalue));
                _il.Emit(OpCodes.Stloc, _scopeUpvaluesArray);

                // 1. Copy inherited upvalues (they might come from parent's master array or Context.Upvalues)
                for (int i = 0; i < inheritedVars.Count; i++)
                {
                    var varName = inheritedVars[i];
                    if (_scope.Resolve(varName, out var val) && val != null)
                    {
                        _il.Emit(OpCodes.Ldloc, _scopeUpvaluesArray);
                        _il.Emit(OpCodes.Ldc_I4, i);

                        if (oldUpvalueMap.TryGetValue(val, out int parentIdx))
                        {
                            // It's in the Inherited list of the current function
                            _il.Emit(OpCodes.Ldarg_0); // CILContext
                            _il.Emit(OpCodes.Ldfld, RuntimeMetadata.CILContext_Upvalues);
                            _il.Emit(OpCodes.Ldc_I4, parentIdx);
                            _il.Emit(OpCodes.Ldelem, typeof(Upvalue));
                        }
                        else if (oldLocalScopeCaptureIndex.TryGetValue(val, out var locCapture))
                        {
                            // It's in the Master list of an outer block in the same function
                            _il.Emit(OpCodes.Ldloc, locCapture.Array);
                            _il.Emit(OpCodes.Ldc_I4, locCapture.Index);
                            _il.Emit(OpCodes.Ldelem, typeof(Upvalue));
                        }
                        else
                        {
                            // Fallback (e.g. if it was just a local but now it escapes in a nested block)
                            // This might happen if the outer block didn't know it needed to escape.
                            // But analyzer.Analyze(node, scope) should have handled nested functions inside this block.
                            _il.Emit(OpCodes.Newobj, RuntimeMetadata.Upvalue_CtorEmpty);
                        }
                        _il.Emit(OpCodes.Stelem, typeof(Upvalue));
                        // Mark as master-captured for current function body
                        _upvalueMap.Remove(val);
                        _localScopeCaptureIndex[val] = new LocalCaptureInfo { Array = _scopeUpvaluesArray, Index = i };
                    }
                }

                // 2. Wrap local variables into Upvalues
                int offset = inheritedVars.Count;
                for (int i = 0; i < localVars.Count; i++)
                {
                    var varName = localVars[i];
                    if (_scope.Resolve(varName, out var val) && val != null)
                    {
                        var declare = val;
                        int masterIdx = offset + i;
                        _localScopeCaptureIndex[declare] = new LocalCaptureInfo { Array = _scopeUpvaluesArray, Index = masterIdx };

                        _il.Emit(OpCodes.Ldloc, _scopeUpvaluesArray);
                        _il.Emit(OpCodes.Ldc_I4, masterIdx);
                        _il.Emit(OpCodes.Newobj, RuntimeMetadata.Upvalue_CtorEmpty);
                        _il.Emit(OpCodes.Stelem, typeof(Upvalue));

                        // If parameter already has a value, sync it immediately
                        if (_locals.TryGetValue(declare, out var local))
                        {
                            _il.Emit(OpCodes.Ldloc, _scopeUpvaluesArray);
                            _il.Emit(OpCodes.Ldc_I4, masterIdx);
                            _il.Emit(OpCodes.Ldelem, typeof(Upvalue));
                            _il.Emit(OpCodes.Ldloc, local);
                            _il.Emit(OpCodes.Stfld, RuntimeMetadata.Upvalue_Value);
                        }
                    }
                }
            }
            else if (analyzer.Upvalues.Count > 0)
            {
                // Optimization: If no new locals escape, we can just use the parent's mapping.
                // But we must ensure the indices match.
                // Actually, if we just keep _upvalueMap and _localScopeCaptureIndex as they are,
                // children will correctly use the existing handles.
            }
            // 3. Visit statements
            if (node is ModuleDeclaration module)
            {
                foreach (var import in module.Imports) import.Accept(this);
            }

            // 4. Hoisting: Visit functions first
            foreach (var func in node.Functions)
            {
                if (func.Flags == FunctionFlags.Declare) continue;
                func.Accept(this);
            }

            foreach (var statement in node.ChildNodes)
            {
                statement.Accept(this);
            }

            // Mark sequence point for the closing brace '}'
            // Only if the last statement is not a return, to avoid nop after ret runtime error
            if (node.Parent is not FunctionDeclaration)
            {
                var endRange = node.Range;
                endRange.StartLine = endRange.EndLine;
                endRange.StartColumn = endRange.EndColumn;
                endRange.EndColumn++;
                builder.MarkSequencePoint(endRange, _il);
            }

            // Restore state
            _upvalueMap.Clear();
            foreach (var kv in oldUpvalueMap) _upvalueMap.Add(kv.Key, kv.Value);
            _localScopeCaptureIndex.Clear();
            foreach (var kv in oldLocalScopeCaptureIndex) _localScopeCaptureIndex.Add(kv.Key, kv.Value);
            _scopeUpvaluesArray = oldScopeUpvaluesArray;
        }

        protected override void VisitFunction(FunctionDeclaration node)
        {
            if (node.Flags == FunctionFlags.Declare) return;

            if (_scope.ScopeType == Core.ScopeType.Module)
            {
                _il.Emit(OpCodes.Ldarg_0);
                _il.Emit(OpCodes.Ldfld, RuntimeMetadata.CILContext_Module);
                builder.LoadStringConstant(_il, node.Name.Value);
                CompileFunction(node);
                _il.Emit(OpCodes.Ldc_I4_0);
                _il.Emit(OpCodes.Ldc_I4_1);
                if (IsPatching)
                {
                    _il.Emit(OpCodes.Callvirt, RuntimeMetadata.ScriptObject_Patch);
                }
                else
                {
                    _il.Emit(OpCodes.Callvirt, RuntimeMetadata.ScriptObject_Define);
                }
                PopType();
                return;
            }


            // If it has a name and is nested OR at module level, store it
            if (node.Name != null && (_scope.ScopeType == Core.ScopeType.Function || _scope.ScopeType == Core.ScopeType.Module))
            {
                _scope.Resolve(node.Name.Value, out var val);
                if (_scope.ScopeType == Core.ScopeType.Function)
                {
                    if (val != null &&
                        !_upvalueMap.ContainsKey(val) &&
                        !_localScopeCaptureIndex.ContainsKey(val) &&
                        !_locals.ContainsKey(val))
                    {
                        _locals[val] = _il.DeclareLocal(typeof(ClosureFunction));
                        WriteLocalSymbol(_locals[val], node);
                    }
                }

                EmitStoreName(new NameExpression(node.Name), val, () => CompileFunction(node));

            }
            else
            {
                CompileFunction(node);
                _il.Emit(OpCodes.Pop);
                PopType();
            }
        }


        protected override void VisitLambdaExpression(LambdaExpression node)
        {
            CompileFunction(node.Function);
        }


        private void CompileFunction(FunctionDeclaration node)
        {
            // Check if already compiled
            if (_currentModule.Methods.TryGetValue(node, out var method))
            {
                _il.Emit(OpCodes.Ldnull);
                _il.Emit(OpCodes.Ldftn, method);
                _il.Emit(OpCodes.Newobj, typeof(ScriptFunctionDelegate).GetConstructors()[0]);
                return;
            }
            var oldIl = _il;
            var oldScope = _scope;
            var oldLocals = new Dictionary<DeclareObject, LocalBuilder>(_locals);
            var oldUpvalueMap = new Dictionary<DeclareObject, int>(_upvalueMap);
            var oldLocalScopeCaptureIndex = new Dictionary<DeclareObject, LocalCaptureInfo>(_localScopeCaptureIndex);
            var oldScopeUpvaluesArray = _scopeUpvaluesArray;

            var funcName = node.Name?.Value;

            // Abstract ILGenerator retrieval
            (method, _il) = builder.DefineMethod(_currentModule.Name, funcName, typeof(ScriptDatum), [typeof(ScriptContext), typeof(ScriptDatum[])]);
            ilOffset = -1;
            _currentModule.Methods[node] = method;
            _scope = _scope.Enter(ScopeType.Function);

            var oldConstantPool = new Dictionary<object, LocalBuilder>(_constantPool);
            _constantPool.Clear();

            if (node.Body != null)
            {
                var hoister = new ConstantHoister();
                var stats = hoister.GetLiteralStats(node.Body);
                InitializeConstantPool(stats);
            }

            _locals.Clear();
            _upvalueMap.Clear();
            _localScopeCaptureIndex.Clear();
            _scopeUpvaluesArray = null;

            var oldStackState = _stackManager.GetState();
            _stackManager.Clear();

            // 1. Identify Upvalues needed from outer scope
            var analyzer = new ClosureAnalyzer();
            analyzer.Analyze(node, oldScope);

            foreach (var upvalueName in analyzer.Upvalues)
            {
                if (oldScope.Resolve(upvalueName, out var val) && val != null)
                {
                    // Inherit indexing from parent's context
                    if (oldUpvalueMap.TryGetValue(val, out int idx))
                    {
                        _upvalueMap[val] = idx;
                    }
                    else if (oldLocalScopeCaptureIndex.TryGetValue(val, out var locCapture))
                    {
                        _upvalueMap[val] = locCapture.Index;
                    }
                }
            }

            // 2. Pre-declare parameters
            foreach (var param in node.Parameters)
            {
                _scope.Declare(param.Name.Value, Core.DeclareType.Variable, MemberAccess.Internal);
            }

            // 3. Emit parameter initialization
            _nextBlockParameters = node.Parameters?.Select(p => p.Name.Value).ToList();

            // Parameters
            int argIdx = 0;
            if (node.Parameters != null)
            {
                foreach (var param in node.Parameters)
                {
                    if (param.Name == null) continue;
                    _scope.Resolve(param.Name.Value, out var val);
                    var declare = val;

                    var local = _il.DeclareLocal(typeof(ScriptDatum));
                    WriteLocalSymbol(local, param);
                    _locals[declare] = local;

                    _il.Emit(OpCodes.Ldarg_1); // args array
                    _il.Emit(OpCodes.Ldc_I4, argIdx); // index

                    if (param.Initializer != null)
                    {
                        // Optional parameter with default value
                        param.Initializer.Accept(this);
                        EnsureTop(typeof(ScriptDatum));
                        PopType();
                        _il.Emit(OpCodes.Call, RuntimeMetadata.CILHelper_TryGetArg);
                    }
                    else
                    {
                        // Required parameter
                        _il.Emit(OpCodes.Call, RuntimeMetadata.CILHelper_GetArg);
                    }

                    _il.Emit(OpCodes.Stloc, local);
                    argIdx++;
                }
            }

            node.Body?.Accept(this);

            if (node.Body == null || node.Body.ChildNodes.LastOrDefault() is not ReturnStatement)
            {
                // Mark sequence point for the implicit return at the end of the function
                var endRange = (node.Body != null) ? node.Body.Range : node.Range;
                endRange.StartLine = endRange.EndLine;
                endRange.StartColumn = 0;
                endRange.EndColumn++;
                builder.MarkSequencePoint(endRange, _il);

                builder.LoadNull(_il);
                _il.Emit(OpCodes.Ret);
            }

            _stackManager.RestoreState(oldStackState);

            // Restore state
            _il = oldIl;
            _scope = oldScope;
            _scopeUpvaluesArray = oldScopeUpvaluesArray;
            _locals.Clear();
            foreach (var kv in oldLocals) _locals.Add(kv.Key, kv.Value);
            _upvalueMap.Clear();
            foreach (var kv in oldUpvalueMap) _upvalueMap.Add(kv.Key, kv.Value);
            _localScopeCaptureIndex.Clear();
            foreach (var kv in oldLocalScopeCaptureIndex) _localScopeCaptureIndex.Add(kv.Key, kv.Value);

            _constantPool.Clear();
            foreach (var kv in oldConstantPool) _constantPool.Add(kv.Key, kv.Value);

            // Emit code to create ClosureFunction

            // ScriptDomain
            _il.Emit(OpCodes.Ldarg_0);
            _il.Emit(OpCodes.Ldfld, RuntimeMetadata.CILContext_Domain);
            // ScriptModule
            _il.Emit(OpCodes.Ldarg_0);
            _il.Emit(OpCodes.Ldfld, RuntimeMetadata.CILContext_Module);

            if (method is DynamicMethod dynamicMethod)
            {
                var del = (ScriptFunctionDelegate)dynamicMethod.CreateDelegate(typeof(ScriptFunctionDelegate));
                var delegateId = DynamicMethodRegistry.Register(dynamicMethod.Name, del);
                _il.Emit(OpCodes.Dup);
                _il.Emit(OpCodes.Ldc_I4, delegateId);
                _il.Emit(OpCodes.Call, RuntimeMetadata.CILHelper_ResolveDelegate);
            }
            else
            {
                // closure
                _il.Emit(OpCodes.Ldnull);
                _il.Emit(OpCodes.Ldftn, method);
                _il.Emit(OpCodes.Newobj, typeof(ScriptFunctionDelegate).GetConstructors()[0]);
            }

            // Share the current scope's Master Upvalue Array
            if (_scopeUpvaluesArray != null)
            {
                _il.Emit(OpCodes.Ldloc, _scopeUpvaluesArray);
            }
            else
            {
                // No master array in current scope. Check if we have inherited upvalues.
                // If this function captures something from outer scope, we must pass along 
                // the upvalue array we received as Arg0.
                if (analyzer.Upvalues.Count > 0)
                {
                    _il.Emit(OpCodes.Ldarg_0);
                    _il.Emit(OpCodes.Ldfld, RuntimeMetadata.CILContext_Upvalues);
                }
                else
                {
                    _il.Emit(OpCodes.Call, RuntimeMetadata.Array_Empty_Upvalue);
                }
            }
            // name
            if (node.Name != null)
                builder.LoadStringConstant(_il, node.Name.Value);
            else
                _il.Emit(OpCodes.Ldnull);

            _il.Emit(OpCodes.Newobj, RuntimeMetadata.ClosureFunction_Ctor);
            PushType(typeof(ClosureFunction));
        }


        private void EmitStoreName(NameExpression nameExp, DeclareObject val, Action valueEmitter)
        {
            if (val.Type == DeclareType.Variable)
            {
                if (_localScopeCaptureIndex.TryGetValue(val, out var locCapture))
                {
                    _il.Emit(OpCodes.Ldloc, locCapture.Array);
                    _il.Emit(OpCodes.Ldc_I4, locCapture.Index);
                    _il.Emit(OpCodes.Ldelem, typeof(Upvalue));

                    if (valueEmitter != null)
                    {
                        valueEmitter();
                        EnsureTop(typeof(ScriptDatum));
                        PopType(); // Value
                    }
                    else
                    {
                        throw new Exception();
                    }
                    _il.Emit(OpCodes.Stfld, RuntimeMetadata.Upvalue_Value);

                }
                else if (_upvalueMap.TryGetValue(val, out int upIdx))
                {
                    _il.Emit(OpCodes.Ldarg_0);
                    _il.Emit(OpCodes.Ldfld, RuntimeMetadata.CILContext_Upvalues);
                    _il.Emit(OpCodes.Ldc_I4, upIdx);
                    _il.Emit(OpCodes.Ldelem, typeof(Upvalue));

                    if (valueEmitter != null)
                    {
                        valueEmitter();
                        EnsureTop(typeof(ScriptDatum));
                        PopType(); // Value
                    }
                    else
                    {
                        throw new Exception();
                    }
                    _il.Emit(OpCodes.Stfld, RuntimeMetadata.Upvalue_Value);

                }
                else
                {
                    if (!_locals.TryGetValue(val, out var local))
                    {
                        // Auto-declare missing local variables (primarily for destructuring)
                        local = _il.DeclareLocal(typeof(ScriptDatum));
                        WriteLocalSymbol(local, nameExp);
                        _locals[val] = local;
                    }
                    if (val != null && val.VariableNode != null && val.VariableNode.IsConst)
                    {
                        throw new AuroraEmitException(nameExp, $"Assignment to constant variable '{val.VariableNode.Name.Value}'.");
                    }
                    if (valueEmitter != null)
                    {
                        valueEmitter();
                        EnsureTop(local.LocalType);
                        PopType();
                    }
                    else
                    {
                        EnsureTop(local.LocalType);
                        PopType();
                    }
                    _il.Emit(OpCodes.Stloc, local);

                }
            }
            else if (val.Type == DeclareType.Property || val.Type == DeclareType.Global)
            {
                if (val.Type == DeclareType.Property)
                {
                    if (_scope.ScopeType == Core.ScopeType.Function)
                    {
                        _il.Emit(OpCodes.Ldarg_0); // CILContext
                        _il.Emit(OpCodes.Ldfld, typeof(ScriptContext).GetField("Module"));
                    }
                    else
                    {
                        _il.Emit(OpCodes.Ldarg_1); // ScriptModule
                    }
                }
                else
                {
                    if (_scope.ScopeType == Core.ScopeType.Function)
                    {
                        _il.Emit(OpCodes.Ldarg_0); // CILContext
                        _il.Emit(OpCodes.Ldfld, typeof(ScriptContext).GetField("Global"));
                    }
                    else
                    {
                        _il.Emit(OpCodes.Ldarg_0); // ScriptDomain
                        _il.Emit(OpCodes.Ldfld, RuntimeMetadata.CILContext_Global);
                    }
                }
                builder.LoadStringConstant(_il, nameExp.Identifier.Value);
                PushType(typeof(string));

                if (valueEmitter != null)
                {
                    valueEmitter();
                    EnsureTop(typeof(ScriptObject));
                }
                else
                {
                    throw new Exception();
                }

                _il.Emit(val?.VariableNode?.IsConst == true ? OpCodes.Ldc_I4_0 : OpCodes.Ldc_I4_1);
                _il.Emit(OpCodes.Ldc_I4_1);
                _il.Emit(OpCodes.Callvirt, RuntimeMetadata.ScriptObject_Define);
                PopType(); // Value
                PopType(); // Name
                PopType(); // Target
            }
            else
            {
                throw new Exception();
            }
        }


        private LocalBuilder EnsureLocal(NameExpression nameExp, DeclareObject val)
        {
            if (!_locals.TryGetValue(val, out var local))
            {
                // Auto-declare missing local variables (primarily for destructuring)
                local = _il.DeclareLocal(typeof(ScriptDatum));
                WriteLocalSymbol(local, nameExp);
                _locals[val] = local;
            }
            return local;
        }
        protected override void VisitDeleteStatement(DeleteStatement node)
        {
            if (node.Expression is GetPropertyExpression getProp)
            {
                _il.Emit(OpCodes.Ldarg_0);
                getProp.Object.Accept(this);
                EnsureTop(typeof(ScriptObject));
                PopType(); // Object
                if (getProp.Property is NameExpression nameExp)
                {
                    builder.LoadStringConstant(_il, nameExp.Identifier.Value);
                    PushType(typeof(string));
                    PopType(); // Name
                    _il.Emit(OpCodes.Call, RuntimeMetadata.CILHelper_DeleteProperty);
                }
                else
                {
                    throw new AuroraEmitException(node.Expression, $"Invalid or unexpected token {getProp.Property}");
                }

            }
            else if (node.Expression is GetElementExpression getElem)
            {
                _il.Emit(OpCodes.Ldarg_0);
                getElem.Object.Accept(this);
                EnsureTop(typeof(ScriptObject));
                PopType(); // Object
                getElem.Index.Accept(this);
                EnsureTop(typeof(ScriptDatum));
                PopType(); // Index
                _il.Emit(OpCodes.Call, RuntimeMetadata.CILHelper_DeleteElement);
            }
        }

        protected override void VisitReturnStatement(ReturnStatement node)
        {
            if (node.Expression != null)
            {
                node.Expression.Accept(this);
                EnsureTop(typeof(ScriptDatum));
                PopType();
            }
            else
            {
                builder.LoadNull(_il);
            }
            _il.Emit(OpCodes.Ret);
        }





        protected override void VisitCallExpression(FunctionCallExpression node)
        {
            // 1. Target
            node.Target.Accept(this);
            EnsureTop(typeof(ScriptObject));
            PopType();

            // 2. Arguments (pushes Ctx and ArgsArray)
            EmitCallArguments(node);

            EmitNodeLocation(node);
            // 3. Invoke (returns ScriptDatum)
            _il.Emit(OpCodes.Callvirt, RuntimeMetadata.ScriptObject_Invoke);

            if (node.NeedResult)
            {
                PushType(typeof(ScriptDatum));
            }
            else
            {
                _il.Emit(OpCodes.Pop);
            }
        }

        protected override void VisitNewExpression(NewExpression node)
        {
            // 1. Target
            node.Expression.Target.Accept(this);
            EnsureTop(typeof(ScriptObject));
            PopType();

            // 2. Arguments (pushes Ctx and ArgsArray)
            EmitCallArguments(node.Expression);

            // 3. New (returns ScriptDatum)
            _il.Emit(OpCodes.Call, RuntimeMetadata.CILHelper_New);
            PushType(typeof(ScriptDatum));
        }

        private void EmitCallArguments(FunctionCallExpression node)
        {
            // 2. Arguments array
            var hasSpread = node.Arguments.Any(x => x is SpreadExpression);
            if (!hasSpread)
            {
                _il.Emit(OpCodes.Ldarg_0); // CILContext
                _il.Emit(OpCodes.Ldc_I4, node.Arguments.Count);
                _il.Emit(OpCodes.Newarr, typeof(ScriptDatum));
                for (int i = 0; i < node.Arguments.Count; i++)
                {
                    _il.Emit(OpCodes.Dup);
                    _il.Emit(OpCodes.Ldc_I4, i);
                    node.Arguments[i].Accept(this);
                    EnsureTop(typeof(ScriptDatum));
                    PopType();
                    _il.Emit(OpCodes.Stelem, typeof(ScriptDatum));
                }
            }
            else
            {
                // CILContext
                _il.Emit(OpCodes.Ldarg_0);

                // ScriptArray
                _il.Emit(OpCodes.Ldc_I4, 0);
                _il.Emit(OpCodes.Newobj, RuntimeMetadata.ScriptArray_CtorCapacity);

                foreach (var arg in node.Arguments)
                {
                    if (arg is SpreadExpression spread)
                    {
                        _il.Emit(OpCodes.Dup); // ScriptArray
                        spread.Expression.Accept(this);
                        EnsureTop(typeof(ScriptObject));
                        PopType();
                        _il.Emit(OpCodes.Call, RuntimeMetadata.CILHelper_SpreadInto);
                    }
                    else
                    {
                        _il.Emit(OpCodes.Dup); // ScriptArray
                        arg.Accept(this);
                        EnsureTop(typeof(ScriptDatum));
                        PopType();
                        _il.Emit(OpCodes.Callvirt, RuntimeMetadata.ScriptArray_Push);
                    }
                }
                // Convert to Array
                _il.Emit(OpCodes.Callvirt, RuntimeMetadata.ScriptArray_ToDatumArray);
            }
        }


        protected override void VisitSpreadExpression(SpreadExpression node)
        {
            node.Expression.Accept(this);
            EnsureTop(typeof(ScriptDatum));
        }

        protected override void VisitBreakExpression(BreakStatement node)
        {
            if (_breakLabels.Count > 0)
            {
                _il.Emit(OpCodes.Br, _breakLabels.Peek());
            }
            else
            {
                throw new InvalidOperationException("Break statement outside of loop");
            }
        }

        protected override void VisitContinueExpression(ContinueStatement node)
        {
            if (_continueLabels.Count > 0)
            {
                _il.Emit(OpCodes.Br, _continueLabels.Peek());
            }
            else
            {
                throw new InvalidOperationException("Continue statement outside of loop");
            }
        }

        protected override void VisitDebuggerExpression(DebuggerStatement node)
        {
            if (builder is PersistedBuilder && Options.OptimizeOption == OptimizeOptions.Debug)
            {
                _il.Emit(OpCodes.Break);
            }
        }

        protected override void VisitSetPropertyExpression(SetPropertyExpression node)
        {
            node.Object.Accept(this);
            EnsureTop(typeof(ScriptObject));
            PopType(); // Object
            _il.Emit(OpCodes.Ldarg_0);
            if (node.Property is NameExpression name)
            {
                builder.LoadStringConstant(_il, name.Identifier.Value);
                PushType(typeof(string));
                PopType(); // Name
            }
            node.Value.Accept(this);
            EnsureTop(typeof(ScriptObject));
            PopType(); // Value
            if (node.NeedResult)
            {
                var temp = _il.DeclareLocal(typeof(ScriptObject));
                WriteLocalSymbol(temp, name: null);
                _il.Emit(OpCodes.Dup);
                _il.Emit(OpCodes.Stloc, temp);
                _il.Emit(OpCodes.Callvirt, RuntimeMetadata.ScriptObject_SetPropertyValue);
                _il.Emit(OpCodes.Ldloc, temp);
                PushType(typeof(ScriptObject));
            }
            else
            {
                _il.Emit(OpCodes.Callvirt, RuntimeMetadata.ScriptObject_SetPropertyValue);
            }
        }

        protected override void VisitObjectDestructuringPattern(ObjectDestructuringPattern node)
        {
            // Initializer result (the object to destructure) is on stack top.
            // Requirement: ScriptObject
            EnsureTop(typeof(ScriptObject));
            PopType(); // Sync tracker for final pop
            foreach (var prop in node.Properties)
            {
                // Duplicate the object for each property access
                _il.Emit(OpCodes.Dup);
                PushType(typeof(ScriptObject)); // Manually sync tracker for Dup
                PopType(); // Duped Object
                _il.Emit(OpCodes.Ldarg_0);
                // Load property name
                builder.LoadStringConstant(_il, prop.Value);
                PushType(typeof(string));
                PopType(); // Name
                // Get property value
                _il.Emit(OpCodes.Callvirt, RuntimeMetadata.ScriptObject_GetPropertyValue);
                // Callvirt consumes duplicated TargetObject and PropertyName

                PushType(typeof(ScriptObject)); // Result is PropertyValue
                // Resolve where to store this property
                _scope.Resolve(prop.Value, out var resolved);
                // Let's use a name expression placeholder for EmitStoreName
                var nameExp = new NameExpression(prop);
                // EmitStoreName will consume the [PropertyValue] from stack and tracker.
                EmitStoreName(nameExp, resolved, null);
            }
            // Finally, pop the original object
            _il.Emit(OpCodes.Pop);
        }

        protected override void VisitArrayDestructuringPattern(ArrayDestructuringPattern node)
        {
            // Input: ScriptObject (expecting ScriptArray)
            EnsureTop(typeof(ScriptObject));
            PopType();
            _il.Emit(OpCodes.Castclass, typeof(ScriptArray));
            var localArray = _il.DeclareLocal(typeof(ScriptArray));
            WriteLocalSymbol(localArray, name: null);
            _il.Emit(OpCodes.Stloc, localArray);
            int restIndex = -1;
            for (int i = 0; i < node.Elements.Count; i++)
            {
                if (node.Elements[i] is SpreadExpression)
                {
                    restIndex = i;
                    break;
                }
            }

            int afterRestCount = restIndex >= 0 ? node.Elements.Count - restIndex - 1 : 0;

            for (int i = 0; i < node.Elements.Count; i++)
            {
                var element = node.Elements[i];
                if (element == null) continue;

                if (i == restIndex)
                {
                    // [...rest]
                    var spread = (SpreadExpression)element;

                    _il.Emit(OpCodes.Ldloc, localArray);
                    _il.Emit(OpCodes.Ldc_I4, i); // Start

                    _il.Emit(OpCodes.Ldloc, localArray);
                    _il.Emit(OpCodes.Callvirt, RuntimeMetadata.ScriptArray_get_Length);
                    _il.Emit(OpCodes.Ldc_I4, afterRestCount);
                    _il.Emit(OpCodes.Sub); // End (exclusive)

                    var localDatum = _il.DeclareLocal(typeof(ScriptDatum));
                    WriteLocalSymbol(localDatum, spread.Expression);
                    _il.Emit(OpCodes.Ldloca, localDatum);

                    _il.Emit(OpCodes.Callvirt, RuntimeMetadata.ScriptArray_SliceTo);

                    _il.Emit(OpCodes.Ldloc, localDatum);
                    PushType(typeof(ScriptDatum));

                    EmitDestructuringStore(spread.Expression);
                }
                else
                {
                    _il.Emit(OpCodes.Ldloc, localArray);
                    if (restIndex == -1 || i < restIndex)
                    {
                        // From start
                        _il.Emit(OpCodes.Ldc_I4, i);
                    }
                    else
                    {
                        // From end
                        int distFromEnd = node.Elements.Count - i;
                        _il.Emit(OpCodes.Ldloc, localArray);
                        _il.Emit(OpCodes.Callvirt, RuntimeMetadata.ScriptArray_get_Length);
                        _il.Emit(OpCodes.Ldc_I4, distFromEnd);
                        _il.Emit(OpCodes.Sub);
                    }

                    _il.Emit(OpCodes.Callvirt, RuntimeMetadata.ScriptArray_Get);
                    PushType(typeof(ScriptDatum));

                    EmitDestructuringStore(element);
                }
            }
        }

        private void EmitDestructuringStore(Expression target)
        {
            if (target is NameExpression name)
            {
                _scope.Resolve(name.Identifier.Value, out var resolved);
                EmitStoreName(name, resolved, null);
            }
            else if (target is ArrayDestructuringPattern || target is ObjectDestructuringPattern)
            {
                // Recursive destructuring
                _il.Emit(OpCodes.Call, RuntimeMetadata.ScriptDatum_ToObject);
                PopType();
                PushType(typeof(ScriptObject));
                target.Accept(this);
            }
            else if (target is SpreadExpression spread)
            {
                // Recursive destructuring for rest (unlikely in valid JS pattern but possible in AST)
                EmitDestructuringStore(spread.Expression);
            }
            else
            {
                // Fallback / Error
                _il.Emit(OpCodes.Pop);
                PopType();
            }
        }

        private void InitializeConstantPool(ConstantHoister.LiteralStats stats)
        {
            _constantPool.Clear();
            foreach (var val in stats.UsageCount.Keys)
            {
                // Only pool if it's hot (in loop) OR used multiple times
                if (stats.HotValues.Contains(val) || stats.UsageCount[val] > 1)
                {
                    var local = _il.DeclareLocal(typeof(ScriptDatum));
                    WriteLocalSymbol(local, name: null);
                    if (val is double d)
                    {
                        var loaded = builder.LoadNumber(_il, d);
                        if (loaded == LoadState.Constant)
                        {
                            _il.Emit(OpCodes.Call, RuntimeMetadata.ScriptDatum_FromNumber);
                        }
                    }
                    else if (val is string s)
                    {
                        var loaded = builder.LoadString(_il, s);
                        if (loaded == LoadState.Constant)
                        {
                            _il.Emit(OpCodes.Call, RuntimeMetadata.ScriptDatum_FromString);
                        }
                    }
                    else if (val is bool b)
                    {
                        var loaded = builder.LoadBoolean(_il, b);
                        if (loaded == LoadState.Constant)
                        {
                            _il.Emit(OpCodes.Call, RuntimeMetadata.ScriptDatum_FromBoolean);
                        }
                    }
                    else if (val == ScriptObject.Null)
                    {
                        builder.LoadNull(_il);
                    }

                    _il.Emit(OpCodes.Stloc, local);
                    _constantPool[val] = local;
                }
            }
        }


    }
}
