# AuroraScript Compiler Backend Ultimate Refactor Plan

## 0. 结论先行

本次重构目标不是整理 `CILEmitter.cs`，而是废弃现有 AST 到 CIL 后端的设计，重建一个面向性能、低分配、并行编译和长期可维护性的编译架构。

重构允许删除或替换以下内部实现：

- `CILEmitter.cs`
- `CodeScope`
- `ClosureAnalyzer`
- `ConstantHoister`
- `DeclarationVisitor`
- `CILStackManager`
- 现有 emitter 内部的模块状态、闭包状态、局部变量状态、直接调用优化状态
- 现有后端工具类中仅为旧 emitter 服务的 API

默认保留以下边界：

- 保留公开 API 行为：`AuroraEngine.BuildAsync`、`CompileBlock`、`ScriptDomain.DynamicPatch`、三种 `CompilationMode`
- 保留 Lexer / Parser / AST 主体结构
- AST 如存在阻碍高性能后端或语义正确性的设计问题，可以局部重构
- 保留测试用例和 Examples 的可观察运行结果
- 保留 Runtime 语义；Runtime 可以为低分配调用路径增补 helper，但不能改变脚本行为

推荐的新链路：

```text
ScriptSource
  -> Lexer / Parser
  -> Module graph linking and deterministic order
  -> Semantic binding
  -> Closure and lifetime analysis
  -> Compile plan lowering
  -> Optimization passes
  -> Backend method definition
  -> Module-level parallel backend stages where safe
  -> Builder finalization
  -> Runtime entry delegate / persisted assembly
```

## 1. 当前事实基线

### 1.1 编译入口

当前 `AuroraEngine.BuildAsync` 为每次构建创建：

- `AbstractCILBuilder`
- `CILEmitter`
- `ScriptCompiler`

`ScriptCompiler` 已经用 channel 并发解析多个 source，并能做：

- 依赖发现
- 并行 parse
- 错误聚合
- 模块路径排序
- 模块名冲突检测
- 拓扑排序

但进入后端后仍是：

```text
GenerateCreateDomain(modules)
foreach module: module.Accept(CILEmitter)
builder.FinalizeBuild()
```

### 1.2 当前后端核心问题

`CILEmitter.cs` 约 3300 行，当前是单个可变 visitor。以下状态混在同一个对象中：

- 当前 ILGenerator
- 当前模块
- 当前作用域
- locals
- upvalue map
- local capture map
- 常量池
- stack manager
- break / continue label stack
- hot patch state
- module direct-call state
- function method cache
- direct closure field cache
- source location state
- 多个 visitor/analyzer 实例

这导致：

- 后端不可并行
- 作用域和闭包逻辑难以证明正确性
- 为保存/恢复状态大量复制 Dictionary
- 分析逻辑和发射逻辑耦合，重复扫描 AST
- 调用路径和参数数组 fallback 分支重复
- 三种 Builder 模式的能力差异没有被显式建模
- 性能优化散落在发射细节里，无法独立测试

### 1.3 已知性能基线

README 当前编译器 pipeline 指标：

| Case | Mean | Allocated | 备注 |
|---|---:|---:|---|
| `CompileBlock` | 0.047 ms | 18.35 KB | 小段脚本编译 |
| `FullCompile_MultiModule` | 0.281 ms | 65.63 KB | 当前样例较小 |
| `FullCompile_SingleModule` | 10.413 ms | 2.81 MB | 大模块完整编译主要成本 |
| `EmitOnly_ParsedLargeModule` | 4.598 ms | 1.26 MB | Emitter 是热点 |
| `ParseOnly_Large` | 5.421 ms | 1.53 MB | AST 构建分配也高 |

本次后端重构的直接性能目标应优先覆盖：

- `EmitOnly_ParsedLargeModule`
- `FullCompile_SingleModule`
- `FullCompile_MultiModule`
- `CompileBlock`
- 多文档宽依赖图
- Dynamic 模式下的模块级并行后端编译

## 2. 不可破坏的业务语义

重构后的行为必须通过现有测试矩阵，特别是：

- Lexer / Parser 行为不变
- 表达式求值、短路、赋值、删除、模板字符串、正则、展开、解构语义不变
- 语句控制流、异常、循环、break/continue、return 不变
- 模块 import/include/export 行为不变
- 模块拓扑、循环依赖、缺失文件、重复 root、错误聚合不变
- `CompileBlock` 不进入模块系统，不支持模块级语句
- 闭包捕获、逃逸、递归、嵌套 block slot 不共享错误
- `$args`、默认参数、高 arity、spread call 行为不变
- Dynamic / OnlyRun / Persistence 三种编译模式可观察结果一致
- 最终 Dynamic / OnlyRun / Persistence 三种模式必须全部接入新后端
- Hot reload 和 incremental patch 语义不变
- `EnableHotReload=false` 且 `EnableModuleDirectCall=true` 时允许同模块内部 direct call 优化
- Debug 模式 source location / stack trace 行为不回退
- Examples 运行结果不变

## 3. 新架构目标

### 3.1 设计目标

1. 后端从“边访问 AST 边发 IL”改为“先生成不可变编译计划，再发 IL”。
2. 所有语义分析 pass 和优化 pass 显式化，可独立测试。
3. 模块、函数、block 编译上下文互相隔离，不再复制全局 Dictionary 恢复状态。
4. 在三种 Builder 能力约束下做模块级并行：
   - Dynamic：优先支持模块级 plan/lowering/emission 并行
   - OnlyRun：模块级分析和 lowering 可并行，IL 定义和写入按 Builder 能力保守控制
   - Persistence：模块级分析和 lowering 可并行，metadata/PDB 相关步骤先保证可序列化和调试信息正确
5. 所有高频容器使用 arena / pooled builder / compact id table，减少 Dictionary 和字符串 key 分配。
6. 直接调用、快速 arity、常量池、闭包布局、局部变量布局从 emitter 中抽成 plan 属性。
7. 新后端内部 API 不追求兼容旧工具，允许一次性替换。

### 3.2 非目标

- 不把引擎改成解释器
- 不替换 CIL/JIT 为表达式树或 Roslyn
- 不改变脚本语言语义
- 不为保留旧类名而牺牲架构
- 不为了短期小风险保留旧 emitter 的核心状态机

## 4. 新后端模块划分

建议新增命名空间：

```text
AuroraScript.Compiler.Backend
AuroraScript.Compiler.Backend.Binding
AuroraScript.Compiler.Backend.Analysis
AuroraScript.Compiler.Backend.IR
AuroraScript.Compiler.Backend.Optimization
AuroraScript.Compiler.Backend.Emit
AuroraScript.Compiler.Backend.Builders
AuroraScript.Compiler.Backend.Diagnostics
```

### 4.1 BackendCompiler

职责：

- 接收已排序 `ModuleDeclaration[]`
- 调度 binding / analysis / lowering / emit
- 根据 `CompilationMode` 选择并行策略
- 返回 runtime entry point 或 patch delegate

建议替换旧调用方式：

```csharp
var backend = new BackendCompiler(builder, options);
backend.CompileModules(modules);
```

### 4.2 CompileSession

一次编译的全局不可变/半不可变状态。

包含：

- `EngineOptions`
- `CompilationModeCapabilities`
- `ModulePlan[]`
- global symbol tables
- metadata handles
- diagnostics
- pooled arenas
- cancellation token
- stable deterministic comparer

原则：

- session 生命周期只覆盖一次 build 或一次 patch
- 不暴露给 Runtime
- 不被 AST 节点反向引用

### 4.3 ModulePlan

一个模块的后端计划。

包含：

- module name/path/hash
- import/include plan
- module-level symbols
- export/member definitions
- top-level statements
- function plans
- direct-call candidates
- module initializer method handle
- direct closure static fields
- debug source document

### 4.4 FunctionPlan

一个函数或 lambda 的后端计划。

包含：

- stable function id
- AST function node reference
- name
- arity shape
- parameter layout
- local layout
- captured local layout
- inherited upvalue layout
- nested function references
- return convention
- method handle
- closure construction recipe
- direct-call eligibility
- debug/source range

### 4.5 BlockPlan

用于普通 block、function body、CompileBlock body。

包含：

- declarations
- lexical lifetime
- captured variable ids
- statement sequence
- local slot requirements
- closure materialization points
- break/continue target metadata

### 4.6 Symbol Model

废弃 `CodeScope`，新建紧凑 symbol model：

```text
SymbolId = int
ScopeId = int
FunctionId = int
ModuleId = int
```

Symbol 数据使用数组或 pooled vectors 存储：

- name
- declaration kind: local/module property/import/parameter/function/const
- access
- owner scope
- declaration AST
- flags: assigned/captured/escaped/exported/imported/const

查找策略：

- binding 阶段用临时 `Dictionary<string, SymbolId>` per scope
- lowering 后只使用 `SymbolId`
- emission 阶段禁止字符串查找局部变量

## 5. 编译阶段设计

### 5.1 Phase 0: Source parse

保留现有 Lexer / Parser / AST。

可以优化但不作为第一批后端重构必需项：

- token 文本切片化
- AST 子节点枚举减少分配
- SourceSpan compact 化
- 模板字符串 AST 降低分配

### 5.2 Phase 1: Module graph

保留现有语义：

- path normalization
- duplicate root 去重
- import dependency discovery
- deterministic error aggregation
- topological sort
- circular dependency report

可以重构实现：

- 将 `ScriptCompiler` 拆为 `SourceGraphBuilder` 和 `FrontendCompiler`
- 保持 parse 并行
- 输出 `FrontendResult`

```text
FrontendResult
  ModuleDeclaration[] ModulesByTopologicalOrder
  Diagnostic[] Diagnostics
```

### 5.3 Phase 2: Binding

目标：一次 pass 建立全部模块和函数作用域。

输出：

- `ModulePlan`
- `ScopeTable`
- `SymbolTable`
- function nesting tree
- import alias binding
- include copy plan
- duplicate declaration diagnostics

关键规则：

- module-level `var/const/function/enum/import` 绑定为 module property
- function/block-level `var/const/function/parameter` 绑定为 local symbol
- named function 在函数内按现有行为可递归引用
- include 仍按当前行为复制 enumerable properties
- patch 模式可注入 existing properties

### 5.4 Phase 3: Escape and closure analysis

目标：不在 emit 时临时分析闭包。

输出：

- 每个 FunctionPlan 的 free variables
- captured locals
- inherited upvalues
- closure layout
- upvalue slot index
- 是否需要 master upvalue array
- 是否可 detached 安全调用

推荐算法：

1. binding 产生 function tree。
2. 自底向上计算每个函数的 free symbol set。
3. 对父函数本地 symbol 标记 escaped。
4. 为每个函数生成稳定 upvalue layout。
5. block 内部被 nested closure 捕获的变量提升到 boxed upvalue slot。

数据结构：

- 小函数使用 stackalloc / small inline set
- 大函数使用 pooled bitset 或 pooled int set
- 只在 analysis 阶段使用可变集合
- plan 固化后用 sorted int arrays

### 5.5 Phase 4: Call shape analysis

目标：统一旧的快速调用、直接调用和参数数组策略。

输出：

- function fast arity: 0..7 或 span
- callsite shape:
  - direct local call
  - same-module internal direct call
  - closure fast call
  - property fast call
  - generic invoke
  - spread invoke
- callsite argument materialization plan

规则：

- 使用 `$args` 的函数不可 fast arity
- 有默认参数的函数不可 fast arity，除非后续设计专门支持默认参数直连
- arity > 7 走 span
- spread 走 materialized argument path
- `EnableHotReload=true` 或 `EnableModuleDirectCall=false` 禁用静态 direct call
- direct call 仅允许同模块、未 export、未被赋值覆盖、不可作为值观察的内部函数
- 跨模块调用、import alias property call、export 函数和模块 public 成员禁止 direct call

### 5.6 Phase 5: Constant and literal plan

目标：移除 emit 时 `ConstantHoister.GetLiteralStats`。

输出：

- per module constant plan
- per function constant plan
- hot literals
- duplicate literals
- object literal shape cache candidates
- string/template concat strategy

策略：

- 循环内或重复使用的 number/string/bool/null 可 hoist 到 local
- 小整数/常用 bool/null 直接使用 Runtime static 或 inline
- object literal 3 属性快路径保留，并扩展为 shape plan
- template string 降低为 concat plan，减少中间 `ScriptDatum -> string` 重复转换

### 5.7 Phase 6: Lowering to compile plan IR

不建议引入复杂 SSA。推荐轻量级、接近 CIL 的树形/线性 IR。

原因：

- AuroraScript 是动态语言，运行时 helper 承载大量语义
- 目标是 CIL，不是多后端
- 低分配和可控 IL 更重要

IR 形态：

```text
EmitOp[]
BasicBlock[]
LocalSlot[]
LabelId[]
ExceptionRegion[]
DebugPoint[]
```

表达式可以不完全 flatten，但 callsite、local access、upvalue access、module property access 必须 lowering 成明确 op。

示例：

```text
LoadLocal SymbolId
StoreLocal SymbolId
LoadUpvalue SymbolId
StoreUpvalue SymbolId
LoadModuleProperty SymbolId
StoreModuleProperty SymbolId
CallStatic MethodHandle, ArgShape
CallRuntime HelperId, ArgShape
Branch LabelId
BranchIfFalse LabelId
Return
```

### 5.8 Phase 7: Optimization passes

优先实现低风险、高收益、可证明正确的优化：

1. Fast arity method signatures
2. Direct local call for proven internal-only functions
3. Same-module direct call for non-exported internal functions when hot reload disabled and module direct-call is enabled
4. Constant local hoisting
5. Avoid argument array for 0..7 args
6. Avoid closure allocation for non-exported internal functions that are direct-only and never exposed
7. Avoid repeated module/global loads by local caching in initializer
8. Collapse simple numeric local arithmetic when both sides statically raw double safe
9. Object literal shape fast path
10. Source location store deduplication

后续可选高级优化：

- simple type feedback 或 static primitive propagation
- loop-invariant literal plan
- specialized `CompiledBlock` path with typed parameters

### 5.9 Phase 8: Method definition

将“定义元数据句柄”和“写 IL”分开。

第一步统一定义：

- domain initializer
- module initializer
- function methods
- direct closure static fields
- source documents
- ModuleId / FunctionId / MethodHandle / DirectClosureField 的全局预定义

第二步发射 IL：

- domain initializer
- module initializers
- function bodies

好处：

- 所有 recursive / forward function references 都可提前解析
- 并行 emission 不需要抢着定义 method
- Builder 能力差异可以集中管理
- 模块级并行前已经固化 direct call 所需的只读元数据

### 5.10 Phase 9: IL emission

新 emitter 必须是短生命周期、上下文隔离对象。

建议对象：

- `MethodEmitter`
- `ModuleInitializerEmitter`
- `DomainInitializerEmitter`
- `PatchEmitter`
- `CompileBlockEmitter`

每个 emitter 只持有：

- 当前 `ILGenerator`
- 当前 `MethodPlan`
- 当前 `EmissionFrame`
- 当前 stack state
- label table
- local table
- source location tracker

禁止持有全局可变模块表。

## 6. 三种编译模式支持

### 6.1 Dynamic

优先优化模式。

特点：

- `DynamicMethod` 独立，适合并行定义和发射
- 无 PDB
- 可优先尝试模块级并行编译
- 需要处理 `DynamicMethod.CreateDelegate` 注册时机

推荐策略：

- serial define domain/module init
- module-level parallel plan/lowering/emission where verified safe
- module 内函数保持串行编译，避免方法级任务调度过度设计
- serial emit domain initializer，module initializer 保持拓扑注册和初始化顺序
- dynamic method delegate registry 使用 thread-safe id 分配

### 6.2 OnlyRun

特点：

- 使用 `AssemblyBuilder/ModuleBuilder/TypeBuilder`
- 当前没有 sequence point
- 类型创建必须 finalize

推荐策略：

- metadata define 阶段串行
- compile plan 和 module-level lowering 可并行
- IL 写入是否并行需要实测；默认先通过 capability flag 控制
- 如果 `ModuleBuilder/TypeBuilder/ILGenerator` 并行安全性不能保证，则写 IL 串行，但保留模块级并行 analysis/lowering

### 6.3 Persistence

特点：

- .NET 9+ `PersistedAssemblyBuilder`
- Debug 模式需要 PDB/sequence point
- `DefineDocument`、metadata、initialized data 共享状态更敏感

推荐策略：

- metadata define 串行
- source document map 串行
- const data field define 串行或 lock-protected
- module-level lowering/analysis 并行
- IL 写入先以 capability flag 保守控制
- Debug/PDB 验证作为单独验收项

## 7. Hot Patch 支持

现有 hot patch 入口在 `ScriptDomain.DynamicPatch` 和 `IncrementalCompiler`。

新设计建议：

- 将 patch 编译作为 `BackendCompiler.CompilePatch(...)`
- patch 使用同一 binding/analysis/lowering/emission pipeline
- patch mode 注入：
  - main module
  - dependency modules
  - patch type
  - existing properties
  - loaded module path map

Patch 特殊规则：

- `Replace` 删除/覆盖旧属性行为不变
- `Incremental` 只增加或覆盖声明行为不变
- `IgnoreDepends` 行为不变
- patch 主模块 initializer 生成 dynamic call method
- patch 不启用会破坏已有 module property 动态性的 direct call

## 8. CompileBlock 支持

CompileBlock 应走同一后端，但使用特殊 root：

```text
BlockCompilationUnit
  SourceName
  Parameters
  Body
  SyntheticFunctionPlan
```

优化方向：

- 参数 binding 使用 `SymbolId`
- local function direct call 优化保留并增强
- 不建立 ModulePlan
- 不参与热重载
- 默认 Dynamic + Release
- 可为无闭包 block 提供更短路径

需要保持：

- 参数名校验
- 不支持模块级语句
- source name 用于诊断
- `Invoke(domain)` 和 `Invoke()` 行为不变

## 9. 低分配策略

### 9.1 取消 emit 期 Dictionary 复制

旧代码热点：

- `CompileFunction` 保存/恢复 `_locals`
- 保存/恢复 `_upvalueMap`
- 保存/恢复 `_localScopeCaptureIndex`
- 保存/恢复 `_constantPool`
- `VisitBlock` 保存/恢复闭包相关 map

新策略：

- 每个 function/block 有自己的 plan
- emission frame 使用 parent pointer 或固定数组引用
- symbol 到 local slot 映射在 plan 中确定
- emission 阶段用 `LocalBuilder[]`，不用 Dictionary

### 9.2 Symbol/Scope 使用 int id

避免按字符串反复查找：

- name lookup 只在 binding 阶段发生
- 后端 plan 使用 `SymbolId`
- emission 阶段数组索引

### 9.3 Pooled builders

可使用：

- `ArrayPool<T>`
- 自定义 `PooledList<T>`
- `ValueListBuilder<T>` 风格结构
- pooled int set / bitset

限制：

- plan 固化后不能持有将归还池的数组
- 可变 pooled 对象只存在于 phase 内部

### 9.4 Call argument allocation

目标：

- 0..7 args 无数组
- native bound function 使用 `DatumBufferN`
- closure fast arity 直接调用 delegate
- spread 才 materialize
- high arity 尽可能使用 pooled array 或 stack-friendly helper

### 9.5 Delegate allocation

旧 DynamicMethod 在 `CompileFunction` 内即时 `CreateDelegate` 并注册。

新策略：

- method define/emit 完成后批量 materialize delegate
- module initializer 使用预注册 handle
- 未 export 的内部函数若只走 direct call 且不暴露为 first-class closure，可避免 closure allocation

需谨慎：

- 函数作为值、赋值、返回、传参、存 module property 时必须创建 `ClosureFunction`
- export 函数、模块 public 成员、可被脚本读取的函数必须创建 `ClosureFunction`
- recursive function 需要可引用自身
- hot reload 下不能静态冻结会被替换的函数

## 10. 并行编译策略

### 10.1 并行阶段

可以并行：

- parse 已有
- module-level binding 补全
- module-level closure/call/constant analysis
- module-level lowering
- Dynamic 模式下模块级 emission，前提是验证 Builder 写入安全
- benchmark source corpus generation 不相关

需要受控或串行：

- module graph linking/sort deterministic 输出
- global metadata handle definition
- TypeBuilder/ModuleBuilder mutation
- Persisted PDB document definition
- builder finalization
- domain initializer ordering
- module initializer registration order
- module 内函数编译顺序

### 10.2 调度模型

建议新增：

```text
CompilerScheduler
  MaxDegreeOfParallelism
  CancellationToken
  Diagnostics
  WorkQueue
```

任务粒度：

- module backend job
- module analysis job
- module lowering job
- module emit job

排序要求：

- diagnostics 按 path/source span deterministic 排序
- output method names deterministic unless confused mode enabled
- module registration order 使用拓扑排序结果
- module 内函数顺序 deterministic

## 11. AST 局部重构候选

默认不动 AST，但以下设计如果后端实现中确认有问题，可顺手修复：

1. `AstNode.ChildNodes` 如果每次枚举分配，应改为零分配遍历接口。
2. `AstNode.Length` + indexer 如果内部分配，应改为稳定数组/list。
3. `IdentifierToken.Value` 高频字符串可考虑 intern 或 symbolized name。
4. `ModuleDeclaration.Functions` 与 body statement 的双重关系需要明确，避免重复扫描。
5. `SourceSpan` mutation 当前存在修改副本/原对象风险，应确认是 struct 还是 class；若是 class，需修复 debug range 生成。
6. `LiteralExpression.Token` 到值的转换应集中，避免每个 pass switch。

AST 重构原则：

- 不改变 Parser 产生的语义
- 不改变测试快照
- 优先加只读派生属性或低分配遍历方法
- 大改 AST 需单独决策

## 12. Runtime 增补候选

允许新增 helper 以减少 emitted IL 复杂度和运行时分配。

候选：

- `CILHelper.InvokeN` 表驱动或 generated helper
- `CILHelper.CreateClosureFastN`
- `CILHelper.GetOrCreateDirectClosure`
- `CILHelper.MaterializeSpreadArguments`
- `CILHelper.GetArgOrDefault`
- `ScriptDatum` small buffer helpers
- `ClosureFunction` delegate storage优化

原则：

- Runtime helper 可以增补，不改变脚本语义
- 高频 helper 标记 `AggressiveInlining`
- 避免 helper 内部重新分配数组

## 13. 文件迁移方案

建议新增后端文件，而不是在旧 `CILEmitter.cs` 中分批改。

第一批新增：

```text
src/Compiler/Backend/BackendCompiler.cs
src/Compiler/Backend/CompileSession.cs
src/Compiler/Backend/CompilationModeCapabilities.cs
src/Compiler/Backend/Plans/ModulePlan.cs
src/Compiler/Backend/Plans/FunctionPlan.cs
src/Compiler/Backend/Plans/BlockPlan.cs
src/Compiler/Backend/Binding/SymbolTable.cs
src/Compiler/Backend/Binding/Binder.cs
src/Compiler/Backend/Analysis/ClosurePlanner.cs
src/Compiler/Backend/Analysis/CallShapePlanner.cs
src/Compiler/Backend/Analysis/ConstantPlanner.cs
src/Compiler/Backend/Lowering/PlanLowerer.cs
src/Compiler/Backend/Emit/MethodEmitter.cs
src/Compiler/Backend/Emit/ModuleInitializerEmitter.cs
src/Compiler/Backend/Emit/DomainInitializerEmitter.cs
src/Compiler/Backend/Emit/PatchEmitter.cs
src/Compiler/Backend/Emit/CompileBlockEmitter.cs
```

旧文件处理：

- `CILEmitter.cs` 最终删除或缩减为兼容 shim
- `ScriptCompiler` 改依赖 `BackendCompiler`
- `IncrementalCompiler` 改依赖 `BackendCompiler`
- benchmark 改用新后端
- 旧分析器和工具类在无引用后删除

## 14. 分阶段执行计划

### Phase A: 基线和护栏

1. 跑现有测试和 benchmark smoke，记录本地基线。
2. 增加后端专项 benchmark：
   - large module emit
   - many small functions
   - deep nested closures
   - wide module graph
   - CompileBlock hot path
3. 增加缺失回归测试：
   - 未 export 内部 direct-only function 不分配 closure 的可观察边界
   - hot reload 与 direct call 禁用边界
   - debug source location parity

验收：

- 当前 main 行为记录完整
- 后续每个 phase 可对比

### Phase B: 新 symbol/binding 层

1. 实现 `SymbolId/ScopeId/FunctionId/ModuleId`
2. 实现 `Binder`
3. 输出 `ModulePlan/FunctionPlan/BlockPlan` 初版
4. 保留旧 emitter，只对比 binding 结果和现有行为

验收：

- 所有模块/函数/局部声明能稳定绑定
- import/include/export 绑定正确
- diagnostics deterministic

### Phase C: Closure planner

1. 实现自底向上 free variable 分析
2. 生成 upvalue layout
3. 生成 escaped local layout
4. 针对闭包回归测试做 plan snapshot 或 internal assertion

验收：

- 现有 closure/recursive/default/$args/high arity 测试仍能被 plan 表达
- 不依赖旧 `ClosureAnalyzer`

### Phase D: Call/constant/layout planners

1. fast arity planner
2. direct local/module call planner
3. assigned name planner
4. constant planner
5. local slot planner

验收：

- plan 能覆盖现有 emitter 的优化能力
- 明确哪些优化在 hot reload/debug/persistence 下关闭

### Phase E: 新 IL emitter MVP

目标不是降级实现，而是完整语义的第一版后端。

实现顺序：

1. literals/name/local/module property
2. var/const/function/module initializer
3. return/block
4. binary/unary/logical
5. if/while/for/for-in
6. call/new/property/index
7. object/array/map/template/regex
8. assignment/compound/inc/dec/delete
9. destructuring
10. try/catch/finally/throw/debugger
11. closure/upvalue
12. import/include/enum

验收：

- Dynamic 模式主要测试通过
- CompileBlock 通过
- 不再使用旧 `CILEmitter`

### Phase F: 三模式补齐

1. OnlyRun builder capability 接入
2. Persistence builder capability 接入
3. Debug sequence point parity
4. persisted PE/PDB 验证

验收：

- `CompilationModeTests` 全通过
- net8 下 Persistence 仍按现有限制

### Phase G: Hot patch 接入

1. 替换 `IncrementalCompiler` 后端调用
2. patch binding 注入 existing properties
3. patch dynamic method emission
4. hot reload 禁用 direct call 的边界验证

验收：

- `HotReloadTests` 全通过
- Examples hot patch 路径运行结果不变

### Phase H: 并行化和低分配深化

1. Dynamic module-level backend parallelism
2. module-level analysis/lowering parallel
3. plan arena/pooling
4. remove remaining Dictionary in emit path
5. call argument allocation cleanup
6. delegate materialization batching

验收：

- benchmark 显著优于基线
- 无并发 nondeterministic failure

### Phase I: 删除旧实现

1. 删除 `CILEmitter.cs`
2. 删除旧 analyzer/tool
3. 删除旧 benchmark 入口
4. 更新 README benchmark 描述
5. 保留迁移文档

验收：

- 无旧后端引用
- 全测试通过
- benchmark compare 完成

## 15. 验收标准

### 15.1 正确性

必须通过：

```powershell
dotnet restore tests\AuroraScript.Tests.csproj --ignore-failed-sources
dotnet build tests\AuroraScript.Tests.csproj -c Release --no-restore
dotnet test tests\AuroraScript.Tests.csproj -c Release --no-build --logger "console;verbosity=normal"
dotnet run --project benchmark\Benchmark.csproj -c Release -- --smoke
```

Examples 至少运行主路径：

```powershell
dotnet run --project examples\Examples.csproj -c Release
```

### 15.2 性能目标

性能目标不是上限，而是持续压低编译耗时和内存分配。以下数值只作为第一版最低改善门槛，不能限制后续优化空间：

| Case | 目标 |
|---|---:|
| `EmitOnly_ParsedLargeModule` time | 降低 40%+ |
| `EmitOnly_ParsedLargeModule` allocation | 降低 60%+ |
| `FullCompile_SingleModule` time | 降低 25%+ |
| `FullCompile_SingleModule` allocation | 降低 35%+ |
| `FullCompile_MultiModule` wide graph | 随模块数获得并行收益 |
| `CompileBlock` allocation | 降低 30%+ |

最终目标以实际 benchmark 结果调整，但不得接受“只为了过测试”的降级后端。

### 15.3 工程质量

- 后端 phase 可单独测试
- emission 上下文无跨方法共享可变状态
- 三种编译模式 capability 显式建模
- hot reload 对优化的禁用条件集中建模
- Debug/PDB 逻辑不散落在表达式发射中
- 大部分 backend 数据结构基于 id/数组，不基于字符串 Dictionary

### 15.4 实现前基线

记录时间：2026-06-23。

正确性基线：

```text
dotnet test tests\AuroraScript.Tests.csproj -c Release
net8.0:  289 passed, 0 failed, 0 skipped
net9.0:  289 passed, 0 failed, 0 skipped
net10.0: 289 passed, 0 failed, 0 skipped
```

备注：

首次运行时发现 `tests\AuroraScript.Tests\obj` 嵌套生成目录污染源码 glob，导致重复 assembly attributes；清理测试项目生成目录后基线通过。清理范围仅限 `tests\AuroraScript.Tests`、`tests\bin`、`tests\obj` 生成目录。

Smoke 基线：

```text
dotnet run --project benchmark\Benchmark.csproj -c Release -- --smoke
CompilerRegressionSmoke: completed
RuntimeBenchmarks: completed
CompilerPipelineBenchmarks: completed
```

Compare 基线：

| Suite | Name | Category | SourceBytes | ElapsedMs | AllocatedBytes |
|---|---|---|---:|---:|---:|
| CompilerPipelineBenchmarks | CompileBlock | compile | 89 | 0.052 | 19,003 |
| CompilerPipelineBenchmarks | FullCompile_MultiModule | compile | 333 | 1.130 | 66,326 |
| CompilerPipelineBenchmarks | FullCompile_SingleModule | compile | 55,088 | 10.252 | 2,923,642 |
| CompilerPipelineBenchmarks | EmitOnly_ParsedLargeModule | emitter | 55,088 | 5.237 | 1,301,667 |
| CompilerPipelineBenchmarks | LexerOnly_Large | lexer | 55,088 | 0.859 | 22,003 |
| CompilerPipelineBenchmarks | ParseOnly_Large | parser | 55,088 | 3.916 | 1,600,579 |
| CompilerPipelineBenchmarks | ParseOnly_TemplateInterpolation | parser | 12,985 | 0.379 | 423,091 |

重构后的性能比较以同机同配置 compare 基线为准，重点压低 compiler pipeline 的 time 和 allocation。

## 16. 决策树和访谈顺序

后续访谈一次只问一个问题。推荐按依赖顺序确认：

1. 是否接受“废弃旧后端，新增 Backend 命名空间，最终删除旧 `CILEmitter`”。
2. 是否接受轻量 plan IR，而不是 SSA IR。
3. 是否允许修改 `ScriptCompiler/IncrementalCompiler/AuroraEngine` 的内部依赖。
4. Dynamic 模式是否作为第一性能优先级。
5. 是否采用模块级并行作为唯一编译并行粒度。
6. 是否允许 AST 增加零分配遍历 API。
7. 是否允许 Runtime 增补 fast helper。
8. direct-only function 是否只允许未 export 的内部方法。
9. Debug sequence point 是否要求逐行完全一致，还是 stack trace 行为一致即可。
10. benchmark 目标是否采用本文初始目标。
11. 是否先实现新后端并行存在，再一次性切换入口。
12. 旧后端删除的时间点：新后端全测试通过后立即删除，还是保留一个短期 fallback branch。

## 17. 第一项推荐决策

推荐答案：

接受新增 `AuroraScript.Compiler.Backend` 完整后端，最终删除旧 `CILEmitter` 和旧工具类。迁移过程中可以短期并存，但不能把旧 emitter 作为最终 fallback，也不能因为旧逻辑容易通过测试而保留其状态机。

理由：

- 当前瓶颈来自架构耦合，不是局部代码风格。
- 旧 emitter 的状态保存/恢复模型天然阻碍并行和低分配。
- 新后端需要用 plan 固化语义，才能把闭包、常量、调用、局部布局和 Builder 能力分开优化。
- 内部 API 无需对外兼容，重建成本低于长期维护旧状态机。

## 18. 决策日志

### D001: 后端重构边界

状态：已确认。

结论：

新增 `AuroraScript.Compiler.Backend` 完整后端，最终删除旧 `CILEmitter`、`CodeScope` 和旧分析器工具。迁移过程中可以短期并存，但最终不保留旧 emitter fallback。

### D002: IR 形态

状态：已确认。

结论：

采用轻量 Compile Plan IR，不采用完整 SSA IR 作为第一版后端核心。Compile Plan 使用 `SymbolId`、`LocalSlot`、`UpvalueSlot`、`LabelId`、`CallShape`、`EmitOp` 等接近 CIL 的低分配表示。

补充：

保留局部 `ValueFacts` / `PrimitiveFacts` 扩展点，用于后续数值路径、常量传播、简单类型事实等专项优化；不为全语言引入 CFG dominance、phi、use-def chain 等完整 SSA 基础设施。

### D003: 内部编译入口

状态：已确认。

结论：

允许修改内部编译入口，把 `AuroraEngine`、`ScriptCompiler`、`IncrementalCompiler` 从依赖 `CILEmitter` 改为依赖新的 `BackendCompiler`。公开 API 保持不变，内部链路改为 frontend 输出模块图和 AST，backend 负责编译 modules、patch 和 compile block。

### D004: 编译模式性能优先级

状态：已确认。

结论：

第一性能优先级放在 `CompilationMode.Dynamic`。Dynamic 模式必须作为极致性能、最低分配和模块级并行后端编译的主目标；OnlyRun 和 Persistence 必须保持行为一致，但第一阶段以复用新 plan/lowering 和正确发射为主，根据 Builder 能力逐步打开模块级并行。

### D005: 并行编译粒度

状态：已确认。

结论：

采用模块级并行作为唯一编译并行粒度。模块内部函数保持串行编译，不做方法级并行，也不做单方法内部并行。

补充：

模块级并行用于加速多文档/多模块项目；单个超大模块的性能提升主要依靠低分配后端、减少扫描、去除 emit 期 Dictionary、调用优化、常量计划和闭包布局优化。`EnableHotReload=false && EnableModuleDirectCall=true` 下的模块内部直接调用仍可保留，前提是先完成全局 ModulePlan、FunctionPlan、MethodHandle 和 DirectClosureField 预定义，再并行编译模块，最后按拓扑顺序串行生成 domain initializer 和模块初始化调用。

### D006: AST 遍历 API

状态：已确认。

结论：

允许为 AST 增加并直接替换现有遍历 API，目标是零分配或低分配遍历。Parser 输出语义、测试结果和脚本行为必须保持不变。

补充：

可替换 `ChildNodes` 等旧 API 为更适合后端多轮扫描的结构，例如 span-like 子节点访问、无枚举器 visitor、节点内稳定子节点数组/list 等。若发现 AST 节点结构本身导致重复扫描或额外分配，也允许局部重构。

### D007: Runtime fast helper

状态：已确认。

结论：

允许 Runtime 增补新的内部 fast helper，用来简化 emitted IL、减少运行时分配和集中高频调用模板。公开 API 和脚本语义必须保持不变。

补充：

可新增 `CILHelper.InvokeFastN`、`CILHelper.CreateClosureFastN`、`CILHelper.MaterializeSpreadArguments`、`CILHelper.GetArgOrDefault`、`CILHelper.LoadOrCreateDirectClosure`、`ScriptDatum` buffer helpers 等。Runtime helper 不能变成替代 CIL 后端的解释器循环。

### D008: 直接调用和 ClosureFunction 省略边界

状态：已确认。

结论：

直接调用方法只允许未 export 的内部方法。export 函数、模块 public 成员、可被脚本读取的函数必须按普通函数对象生成，不能跳过 `ClosureFunction` 分配。

补充：

未 export 的内部方法仍需满足 `EnableHotReload=false`、`EnableModuleDirectCall=true`、未被赋值覆盖、未作为值读取/传参/返回、未被闭包捕获为可观察函数对象等条件，才允许 direct call 或省略 `ClosureFunction`。这个规则优先保护脚本可观察行为。

### D009: Debug sequence point 兼容目标

状态：已确认。

结论：

要求在调试器下可以精准调试，保证异常栈追踪、断点、源码行列定位和 PDB 可用性正确；不要求逐 IL offset、逐 `nop` 或逐旧 emitter `MarkSequencePoint` 调用点完全一致。

补充：

新后端可以根据 lowering 和 emission 结构重新设计 sequence point 策略。验收标准是调试体验和源码定位准确，而不是复刻旧 emitter 的 IL 布局。

### D010: 性能目标口径

状态：已确认。

结论：

性能优化目标是尽可能降低编译耗时和内存分配，不被文档中的初始百分比限制。重构前需要记录同机同配置基线，后续所有优化以该基线对比，持续压低 `EmitOnly_ParsedLargeModule`、`FullCompile_SingleModule`、`FullCompile_MultiModule`、`CompileBlock` 等关键指标。

补充：

文档中的百分比只作为第一版最低改善门槛，不是最终目标或优化停止条件。

### D011: 新旧后端迁移方式

状态：已确认。

结论：

采用并存后切换。新增 Backend 命名空间和新 pipeline，旧 `CILEmitter` 在迁移期间暂时保留用于对照测试和分阶段验证；新后端通过 Dynamic、三模式、HotPatch、CompileBlock 和 benchmark 验收后，切换内部入口并最终删除旧后端。

补充：

并存是迁移手段，不是最终 fallback。最终代码不能保留旧 emitter 作为长期降级路径。

### D012: 旧后端删除时机

状态：已确认。

结论：

新后端完成全部验收后，在同一主线工作中立即删除旧后端。验收范围包括全测试、benchmark smoke、Examples 主路径、Dynamic / OnlyRun / Persistence、HotPatch、CompileBlock 和 Debug/PDB 验证。

补充：

删除范围包括 `CILEmitter.cs`、`CodeScope`、旧 analyzer/tool、旧 benchmark 入口以及相关注释和引用。不长期保留旧后端到独立 fallback 分支。

### D013: Global Predefine 阶段

状态：已确认。

结论：

模块级并行前增加串行 Global Predefine 阶段。该阶段只做全局句柄、符号和布局预定义，不写方法体 IL。

范围：

- `ModuleId` 分配
- `ModulePlan` 创建
- module name/path/hash 固化
- module initializer handle 定义
- module-level symbol 预声明
- 所有函数 `FunctionId` 分配
- 所有函数 `MethodHandle` 预定义
- 未 export 内部函数 direct-call 候选标记
- direct closure field 预定义
- import/include 绑定关系固化

补充：

Global Predefine 完成后，再进行模块级并行的 closure/call/constant analysis、lowering 和 module body emission。这样可以支持前向函数引用、未 export 内部 direct call 和确定性模块初始化顺序。

### D014: Direct call 作用域

状态：已确认。

结论：

direct call 只允许同模块内未 export 的内部方法，不做跨模块 direct call。

规则：

- 允许：当前模块内、未 export、未被赋值覆盖、未作为值读取/传参/返回、`EnableHotReload=false`、`EnableModuleDirectCall=true`、fast arity 可确定的内部函数。
- 禁止：跨模块调用、import alias property call、export 函数、module public 成员、可被脚本读取的函数。

补充：

import 后访问的是模块对象属性，属于脚本可观察动态属性读取；跨模块 direct call 会显著扩大 HotPatch、include、模块初始化顺序和可见性规则的复杂度，当前架构明确不做。

### D015: 模块级并行与 IL 写入能力

状态：已确认。

结论：

所有编译模式默认并行 module-level analysis 和 module-level lowering；IL 写入按 Builder 能力决定。

策略：

- Dynamic：模块级 IL 写入可以作为目标开启，但必须有 stress test 验证；如果验证不稳定，则保留并行 lowering，串行写 `DynamicMethod` IL。
- OnlyRun：默认串行写 Reflection.Emit metadata 和 IL，不假设 `ModuleBuilder` / `TypeBuilder` 线程安全。
- Persistence：默认串行写 metadata、PDB、initialized data 和 IL，不假设 `PersistedAssemblyBuilder` 相关对象线程安全。

补充：

模块级并行收益不依赖 Reflection.Emit 并行写入；analysis/lowering 可先并行，写入阶段由 `CompilationModeCapabilities` 显式控制。

### D016: Binding 数据结构

状态：已确认。

结论：

废弃 `CodeScope` 对象链，新 Binding 使用 `int id + array table` 模型：`ModuleId`、`FunctionId`、`ScopeId`、`SymbolId`。

设计：

- `ScopeTable` 存储 parent scope、owner function、scope kind、symbol range。
- `SymbolTable` 存储 name、kind、flags、owner scope/module/function、declaration node。
- Binding 阶段可以使用临时 `Dictionary<string, SymbolId>` 做 name lookup。
- Binding 结束后，analysis、lowering、emission 全部使用 `SymbolId` 和数组索引，不再使用字符串查找或 `CodeScope.Resolve`。

### D017: Module-level 声明模型

状态：已确认。

结论：

module-level 声明统一绑定为 module property symbol，而不是普通 local。Function/body 内部声明才绑定为 local symbol。

范围：

- `import` alias
- `include` copied member
- module-level `var`
- module-level `const`
- module-level `func`
- module-level `enum`

补充：

后端明确区分 module property access 和 local/upvalue access。未 export 内部函数可以增加 internal/direct-call 候选标记，但只要脚本能按名称读取它，就必须按可观察 module property/closure 处理。

### D018: 函数可见性模型

状态：已确认。

结论：

后端将函数可见性分为 `Exported`、`ModuleVisible`、`InternalOnly` 三类。

定义：

- `Exported`：对外模块 API，禁止 direct-only 优化，必须创建 `ClosureFunction`。
- `ModuleVisible`：未 export，但作为 module property 可被脚本按名称读取，默认必须创建 `ClosureFunction`。
- `InternalOnly`：编译器证明只在当前模块内部被直接调用，不作为可观察值出现，才允许 direct call 或省略 `ClosureFunction`。

补充：

未 export 不自动等于不可观察；只有通过 binding/escape/call shape 分析证明不可观察的函数才能进入 `InternalOnly`。

### D019: 未 export 顶层函数默认可见性

状态：已确认。

结论：

现有未 export 的 module-level function 默认仍定义到模块对象上，属于 `ModuleVisible`，不默认视为 `InternalOnly`。

补充：

只有新后端证明该函数不被脚本按名称读取、不被赋值、不被传递、不参与 HotPatch 可观察路径时，才能从 `ModuleVisible` 降级为 `InternalOnly` 优化。此规则保留当前模块对象可观察行为。

### D020: Module DirectCall 开关

状态：已确认。

结论：

新增 `EngineOptions.EnableModuleDirectCall`，只控制同模块未 export 内部方法 direct call。不引入优化等级 enum，不实现模块内部方法内联，不做跨模块 direct call。

规则：

- direct call 必须同时满足 `EnableHotReload=false` 和 `EnableModuleDirectCall=true`。
- 函数必须未 export，且引用用途分析证明所有引用都是同模块直接调用。
- 函数未被赋值覆盖，未作为值读取/传参/返回，未进入 object/array/map 等可观察容器。

补充：

`EnableHotReload=false` 只是必要条件，不自动开启 direct call；用户必须显式启用 `EnableModuleDirectCall`。

### D021: EnableModuleDirectCall 默认值

状态：已确认。

结论：

`EngineOptions.EnableModuleDirectCall` 默认值为 `false`。

补充：

默认行为保持现有可观察语义，用户必须显式开启同模块内部 direct call 优化。CI、Examples 和普通用户默认路径不因为 direct call 改变调试、HotPatch 或动态读取边界。

### D022: 闭包捕获布局

状态：已确认。

结论：

采用每个函数固定 `UpvalueLayout` + captured local box slot 的闭包模型。Analysis 阶段一次性确定函数的继承 upvalue、本地 captured local、普通 local slot 和 boxed slot。

设计：

- `FunctionPlan.Upvalues`：从父级继承的捕获变量。
- `FunctionPlan.CapturedLocals`：被子函数捕获的本地变量。
- `FunctionPlan.LocalSlots`：普通 IL local。
- `FunctionPlan.BoxSlots`：`Upvalue[]` 中的 boxed local slot。

补充：

Emission 阶段只按 slot index 读写，不再复制/恢复 `_upvalueMap`、`_localScopeCaptureIndex` 等可变字典。

### D023: HotPatch 与 direct call 边界

状态：已确认。

结论：

HotPatch 完全禁用 direct call 和 `InternalOnly` 降级。只要 `EnableHotReload=true` 或处于 patch 编译路径，就不做同模块 direct call、`InternalOnly` 降级、`ClosureFunction` 省略和静态冻结 module function。

补充：

HotPatch 的核心语义是替换/增量更新模块属性。direct call 可能绕过模块属性读取，导致调用旧方法或不可替换方法，因此 patch 路径统一禁用这些优化。

### D024: AST Visitor 形态

状态：已确认。

结论：

允许彻底替换旧 `IAstVisitor` 形态。旧继承式 visitor 如果只服务旧后端，可随旧后端删除。

约束：

- Parser 输出语义不变。
- 测试结果和 Examples 行为不变。
- 新后端 AST 遍历以零分配或低分配为目标。

补充：

AST 可提供 span-like 子节点访问、无枚举器遍历、静态/结构化 walker 等新机制；不需要保留旧 visitor API 兼容性。

### D025: 实现前基线验证

状态：已确认。

结论：

开始进入实现前基线验证，然后进入实现。基线至少包含 Release 测试套件、benchmark smoke 和 benchmark compare。

命令：

```powershell
dotnet test tests\AuroraScript.Tests.csproj -c Release
dotnet run --project benchmark\Benchmark.csproj -c Release -- --smoke
dotnet run --project benchmark\Benchmark.csproj -c Release -- --compare
```

补充：

如果 benchmark compare 耗时明显，先记录已完成的测试和 smoke 结果，再继续后续实现。

## 19. 实现日志

### I001: Backend 基础骨架

状态：已完成。

改动：

- 新增 `EngineOptions.EnableModuleDirectCall`，默认 `false`。
- 新增 `EngineOptions.WithEnableModuleDirectCall(bool)`。
- 更新 `EngineOptionsAndSourceTests` 覆盖新选项。
- 新增 `AuroraScript.Compiler.Backend` 基础类型：
  - `ModuleId`
  - `FunctionId`
  - `ScopeId`
  - `SymbolId`
  - `LocalSlotId`
  - `UpvalueSlotId`
  - `CompilationModeCapabilities`
  - `CompileSession`
  - `BackendCompiler`
- 新增 Binding/Plan 骨架：
  - `ScopeTable`
  - `SymbolTable`
  - `ScopeInfo`
  - `SymbolInfo`
  - `ModulePlan`
  - `FunctionPlan`
  - `CompileBlockPlan`
  - `LocalSlot`
  - `UpvalueSlot`
- `BackendCompiler.CreateModulePlans` 实现 Global Predefine 初版：
  - 创建 module scope
  - 预声明 import alias、module-level var/const、enum、function symbols
  - 为顶层函数创建 session-wide `FunctionId`
  - 标记 `Exported` / `ModuleVisible` 函数可见性
- 新增 `CompilerBackendPlanTests` 验证 plan 和 direct-call capability。

验证：

```text
dotnet test tests\AuroraScript.Tests.csproj -c Release
net8.0:  291 passed, 0 failed, 0 skipped
net9.0:  291 passed, 0 failed, 0 skipped
net10.0: 291 passed, 0 failed, 0 skipped

dotnet run --project benchmark\Benchmark.csproj -c Release -- --smoke
CompilerRegressionSmoke: completed
RuntimeBenchmarks: completed
CompilerPipelineBenchmarks: completed
```

备注：

本阶段不切换旧编译入口，只建立新后端的数据模型和显式 capability 边界。

### I002: Module direct-call 候选分析

状态：已完成。

改动：

- 新增 `Compiler.Backend.Analysis.ModuleUsageAnalyzer`。
- `BackendCompiler.CreateModulePlans` 在 Global Predefine 完成后运行模块用途分析。
- `EnableModuleDirectCall=true` 且 `EnableHotReload=false` 时，允许将满足条件的同模块非 export 函数标记为：
  - `FunctionPlan.IsDirectCallCandidate=true`
  - `FunctionPlan.Visibility=InternalOnly`
- 保守拒绝内部化的条件：
  - 函数名作为值读取，例如 `const exposed = helper`
  - 函数名被赋值或复合赋值
  - 函数名参与 `++` / `--`
  - 函数名被当前 function scope 的参数、局部变量或局部函数 shadow
  - 模块符号表中该名字不是该函数本身
  - 同名函数不唯一
  - HotReload 或 direct-call 开关关闭
- 更新 `CompilerBackendPlanTests` 覆盖：
  - direct-call opt-in
  - 可内部化的纯直接调用
  - 函数值读取拒绝内部化
  - 函数名赋值拒绝内部化
  - 参数/局部变量 shadow 拒绝误判 direct-call

验证：

```text
dotnet test tests\AuroraScript.Tests.csproj -c Release
net8.0:  299 passed, 0 failed, 0 skipped
net9.0:  299 passed, 0 failed, 0 skipped
net10.0: 299 passed, 0 failed, 0 skipped

dotnet run --project benchmark\Benchmark.csproj -c Release -- --smoke
CompilerRegressionSmoke: completed
RuntimeBenchmarks: completed
CompilerPipelineBenchmarks: completed
```

备注：

本阶段仍不切换旧编译入口。用途分析结果只进入新 backend plan，后续 emission 阶段会消费 `InternalOnly` 和 `IsDirectCallCandidate` 生成同模块直接调用路径。

### I003: 新 backend 独立测试项目与模块级并行分析

状态：已完成。

改动：

- 新增 `tests/CompilerBackend/AuroraScript.CompilerBackend.Tests.csproj`。
- 将新 backend 架构/plan 测试从主业务测试项目移入独立测试项目。
- `tests/AuroraScript.Tests.csproj` 排除 `CompilerBackend/**`，避免主测试项目递归包含新架构测试和子项目生成文件。
- `src/AuroraEngine.cs` 增加 `InternalsVisibleTo("AuroraScript.CompilerBackend.Tests")`。
- 将 `AuroraScript.CompilerBackend.Tests` 加入 `AuroraScript.sln`。
- `BackendCompiler.CreateModulePlans` 保持 Global Predefine 串行执行，随后按 capability 对模块分析阶段执行模块级并行调度。
- 新增多模块独立分析测试，验证：
  - 多模块 direct-call candidate 互不污染
  - session-wide `FunctionId` 仍唯一

新 backend 快速验证命令：

```powershell
dotnet test tests\CompilerBackend\AuroraScript.CompilerBackend.Tests.csproj -c Release
```

完整行为回归命令：

```powershell
dotnet test tests\AuroraScript.Tests.csproj -c Release
dotnet run --project benchmark\Benchmark.csproj -c Release -- --smoke
```

验证：

```text
dotnet test tests\CompilerBackend\AuroraScript.CompilerBackend.Tests.csproj -c Release
net8.0:  11 passed, 0 failed, 0 skipped
net9.0:  11 passed, 0 failed, 0 skipped
net10.0: 11 passed, 0 failed, 0 skipped

dotnet test tests\AuroraScript.Tests.csproj -c Release --no-restore
net8.0:  289 passed, 0 failed, 0 skipped
net9.0:  289 passed, 0 failed, 0 skipped
net10.0: 289 passed, 0 failed, 0 skipped

dotnet run --project benchmark\Benchmark.csproj -c Release -- --smoke
CompilerRegressionSmoke: completed
RuntimeBenchmarks: completed
CompilerPipelineBenchmarks: completed
```

备注：

后续新 backend 结构、binding、lowering、emission 单元测试优先进入 `AuroraScript.CompilerBackend.Tests`。主测试项目只保留业务行为、语言语义、runtime/API 和 Examples 等回归验证。

### I004: 函数级 BindingPlan 与低分配 local slot

状态：已完成。

改动：

- 新增 `Compiler.Backend.Binding.FunctionBinder`。
- `FunctionPlan` 增加 `IsModuleFunction`，区分模块顶层函数与嵌套函数/lambda。
- `LocalSlot` 改为函数私有数组记录：
  - `LocalSlotId`
  - `Name`
  - `BackendSymbolKind`
  - `BackendSymbolFlags`
  - `MemberAccess`
  - `AstNode Declaration`
  - `Type`
  - `IsParameter`
- 函数 binding 拆为两阶段：
  - 串行注册嵌套函数/lambda 的 `FunctionId` 和 `ScopeId`，保证确定性。
  - 模块级并行阶段填充各函数 `LocalSlots`、`NestedFunctions`、`HasDefaultParameters`、`UsesArgumentsObject`。
- direct-call 候选分析只处理 `IsModuleFunction=true` 的模块顶层函数，避免局部函数/lambda 被错误降级为模块内部 direct-call 对象。
- 函数局部 symbol 不写入 session 级 `SymbolTable`，避免模块并行 binding 时共享写入和非确定性 ID。

新增/更新测试：

- 参数 slot 与局部 slot 顺序。
- 默认参数标记。
- `arguments` 使用标记。
- const local flag。
- 嵌套函数/lambda plan 注册。
- 顶层模块函数与嵌套函数区分。

验证：

```text
dotnet test tests\CompilerBackend\AuroraScript.CompilerBackend.Tests.csproj -c Release --no-restore
net8.0:  13 passed, 0 failed, 0 skipped
net9.0:  13 passed, 0 failed, 0 skipped
net10.0: 13 passed, 0 failed, 0 skipped

dotnet test tests\AuroraScript.Tests.csproj -c Release --no-restore
net8.0:  289 passed, 0 failed, 0 skipped
net9.0:  289 passed, 0 failed, 0 skipped
net10.0: 289 passed, 0 failed, 0 skipped

dotnet run --project benchmark\Benchmark.csproj -c Release -- --smoke
CompilerRegressionSmoke: completed
RuntimeBenchmarks: completed
CompilerPipelineBenchmarks: completed
```

备注：

本阶段仍不切换旧 emitter。后续 closure/upvalue plan 将基于 `FunctionPlan.LocalSlots` 和 `NestedFunctions` 继续补全，不再复用旧 `ClosureAnalyzer` 或 `CodeScope`。

### I005: Closure/upvalue plan 与 lambda callback 优化边界

状态：已完成。

改动：

- 新增 `Compiler.Backend.Binding.ClosurePlanner`。
- `UpvalueSlot` 改为函数布局记录：
  - `UpvalueSlotId`
  - `Name`
  - `SourceFunction`
  - `SourceLocal`
  - `SourceUpvalue`
  - `IsInherited`
- `FunctionPlan` 增加：
  - `RequiresClosureObject`
  - `CanCacheClosureObject`
  - `IsLambda`
- Closure planner 基于 `FunctionPlan.LocalSlots` 与 `NestedFunctions` 计算：
  - 当前函数 `UpvalueSlots`
  - 父函数 `CapturedLocalSlots`
  - 跨多层捕获的 inherited upvalue 链
- `BackendCompiler` 在函数体 binding 后接入 closure/upvalue plan。

lambda 优化边界：

- `(a,b)=>a+b` 作为参数传入 `somecall` 时，是可观察函数对象，不允许无条件 direct-call。
- 非捕获 lambda 作为值传递时：
  - `RequiresClosureObject=true`
  - `CanCacheClosureObject=true`
  - 后续 emission 可生成 static method + cached singleton closure，避免每次执行处分配。
- 捕获 lambda 作为值传递时：
  - `RequiresClosureObject=true`
  - `CanCacheClosureObject=false`
  - 必须按固定 `UpvalueSlots` 创建新 closure。
- direct-call 仅适用于编译器证明不逃逸的 lambda，或白名单 intrinsic 证明 callback 同步调用且不保存的场景。

新增测试：

- 父函数 local 被子函数捕获。
- 孙函数跨两层捕获祖父函数 local。
- 非捕获 lambda 作为 callback 值传递时需要 closure object 但可缓存。
- 捕获 lambda 作为 callback 值传递时需要 fresh closure object。

验证：

```text
dotnet test tests\CompilerBackend\AuroraScript.CompilerBackend.Tests.csproj -c Release --no-restore
net8.0:  17 passed, 0 failed, 0 skipped
net9.0:  17 passed, 0 failed, 0 skipped
net10.0: 17 passed, 0 failed, 0 skipped

dotnet test tests\AuroraScript.Tests.csproj -c Release --no-restore
net8.0:  289 passed, 0 failed, 0 skipped
net9.0:  289 passed, 0 failed, 0 skipped
net10.0: 289 passed, 0 failed, 0 skipped

dotnet run --project benchmark\Benchmark.csproj -c Release -- --smoke
CompilerRegressionSmoke: completed
RuntimeBenchmarks: completed
CompilerPipelineBenchmarks: completed
```

备注：

本阶段仍不切换旧 emitter。后续 emission 需要消费 `RequiresClosureObject`、`CanCacheClosureObject`、`UpvalueSlots` 和 `CapturedLocalSlots`，替换旧 `ClosureAnalyzer`、`_upvalueMap`、`_localScopeCaptureIndex`。

### I006: 轻量 lowering plan 初版

状态：已完成。

改动：

- 新增 `Compiler.Backend.Lowering`。
- 新增 lowered 节点模型：
  - `LoweredStatementKind`
  - `LoweredExpressionKind`
  - `LoweredBlockStatement`
  - `LoweredReturnStatement`
  - `LoweredExpressionStatement`
  - `LoweredVariableDeclarationStatement`
  - `LoweredFunctionDeclarationStatement`
  - `LoweredIfStatement`
  - `LoweredWhileStatement`
  - `LoweredForStatement`
  - `LoweredBreakStatement`
  - `LoweredContinueStatement`
  - `LoweredLiteralExpression`
  - `LoweredNameExpression`
  - `LoweredBinaryExpression`
  - `LoweredAssignmentExpression`
  - `LoweredCompoundExpression`
  - `LoweredUnaryExpression`
  - `LoweredCallExpression`
  - `LoweredLambdaExpression`
  - `LoweredUnsupportedStatement`
  - `LoweredUnsupportedExpression`
  - `LoweredUnsupportedNode`
- `FunctionPlan.Body` 保存 lowered function body。
- `FunctionPlan.UnsupportedLoweredNodes` 保存未覆盖 AST 节点类型与源码范围；无 unsupported 时保持 `Array.Empty`。
- 新增 `FunctionLowerer`，当前覆盖高频骨架：
  - block
  - return
  - expression statement
  - simple variable declaration
  - nested function declaration
  - if/else
  - while
  - for
  - break/continue
  - literal
  - name
  - binary
  - assignment
  - compound assignment
  - unary
  - call
  - lambda
- name lowering 优先解析：
  - local slot
  - upvalue slot
  - module symbol
  - unresolved/global fallback
- `BackendCompiler` 在 closure/upvalue plan 后接入 lowering。

边界：

- 当前未完整覆盖所有语法，未覆盖节点显式生成 `Unsupported` lowered node。
- unsupported 覆盖缺口现在记录 statement/expression 计数、AST 节点类型与 `SourceSpan`，用于后续按语法分支补齐新 emitter 覆盖。
- 后续按语法种类逐步补齐 lowering，再让新 emitter 消费 lowered plan，而不是回到旧 `IAstVisitor`。

新增测试：

- lowered function body 已生成。
- nested function call target 解析为 local slot。
- 子函数读取父级变量解析为 upvalue slot。
- 模块常量读取解析为 module symbol。
- lambda expression lowering 为 `FunctionId`。
- if/while/for/break/continue lowered control-flow 结构已生成。
- assignment/compound/unary 高频表达式已生成。
- unsupported lowering 记录具体 AST 节点类型，并与计数保持一致。

验证：

```text
dotnet test tests\CompilerBackend\AuroraScript.CompilerBackend.Tests.csproj -c Release --no-restore
net8.0:  21 passed, 0 failed, 0 skipped
net9.0:  21 passed, 0 failed, 0 skipped
net10.0: 21 passed, 0 failed, 0 skipped

dotnet test tests\AuroraScript.Tests.csproj -c Release --no-restore
net8.0:  289 passed, 0 failed, 0 skipped
net9.0:  289 passed, 0 failed, 0 skipped
net10.0: 289 passed, 0 failed, 0 skipped

dotnet run --project benchmark\Benchmark.csproj -c Release -- --smoke
CompilerRegressionSmoke: completed
RuntimeBenchmarks: completed
CompilerPipelineBenchmarks: completed
```

备注：

lowering 初版仍不切换旧 emitter。下一阶段应优先补 statement/expression 覆盖率统计与 lowered plan 完整性测试，然后开始设计新 CIL emission 对 lowered plan 的消费接口。

### I007: 高频 statement/expression lowering 覆盖扩展

状态：已完成。

目标：

- 继续扩大新 backend lowered plan 的语法覆盖面。
- 让 examples、benchmark、主测试中高频出现的语句和对象访问表达式先脱离 `Unsupported`。
- 保持旧 emitter 不切换，旧业务逻辑行为不变。

改动：

- 新增 statement lowered 节点：
  - `LoweredForInStatement`
  - `LoweredTryStatement`
  - `LoweredThrowStatement`
  - `LoweredDeleteStatement`
  - `LoweredDebuggerStatement`
- 新增 expression lowered 节点：
  - `LoweredInExpression`
  - `LoweredGetPropertyExpression`
  - `LoweredGetElementExpression`
  - `LoweredSetPropertyExpression`
  - `LoweredSetElementExpression`
  - `LoweredArrayLiteralExpression`
  - `LoweredMapExpression`
  - `LoweredMapEntry`
  - `LoweredSpreadExpression`
  - `LoweredNewExpression`
- `FunctionLowerer` 新增 lowering：
  - for-in initializer/iterator/body
  - try/catch/finally body 与 catch variable
  - throw/delete/debugger
  - property/index get/set
  - array literal
  - object/map literal with keyed entries and spread entries
  - spread expression
  - new expression
- `UnsupportedCounter` 已递归遍历新增 lowered 节点，避免子表达式 unsupported 漏报。
- `LowerName` 返回类型收窄为 `LoweredNameExpression`，让 `InExpression.Left` 等调用点无需强转。
- `FunctionBinder` 现在为 `catch (name)` 声明函数局部槽位，`LoweredTryStatement` 保存 `CatchSlot`，新 emitter 不需要回退到旧 `CodeScope` 动态声明 catch 变量。

新增测试：

- `LoweringRepresentsForInAndExceptionStatements`
- `LoweringRepresentsObjectArrayMapAndConstructorExpressions`
- `FunctionBindingDeclaresCatchVariableSlot`
- `LoweringCountsUnsupportedNodes` 改为使用 destructuring variable declaration 作为当前仍未覆盖的缺口样例。


当前仍未覆盖：

- destructuring declaration lowering。
- template/interpolation/include/cast/group 等低频或需要独立语义决策的表达式。
- enum/import/module-meta 等模块级语句的 emitter 消费侧设计。

验证：

```text
dotnet test tests\CompilerBackend\AuroraScript.CompilerBackend.Tests.csproj -c Release --no-restore
net8.0:  24 passed, 0 failed, 0 skipped
net9.0:  24 passed, 0 failed, 0 skipped
net10.0: 24 passed, 0 failed, 0 skipped

dotnet test tests\AuroraScript.Tests.csproj -c Release --no-restore
net8.0:  289 passed, 0 failed, 0 skipped
net9.0:  289 passed, 0 failed, 0 skipped
net10.0: 289 passed, 0 failed, 0 skipped

dotnet run --project benchmark\Benchmark.csproj -c Release -- --smoke
CompilerRegressionSmoke: completed
RuntimeBenchmarks: completed
CompilerPipelineBenchmarks: completed
```

### I008: 新 CIL emission 骨架与 lowered plan 消费合同

状态：已完成。

目标：

- 建立新 emitter 的内部层次，不继承旧 `CILEmitter`，不回到 `IAstVisitor`。
- 先验证新 emitter 只消费 `FunctionPlan.Body` 和 lowered node。
- 为后续真实 IL 生成保留 Dynamic/Debuggable/Persisted 共用入口。
- 当前阶段不切换主编译入口，不生成可执行 IL。

新增架构：

- `Compiler.Backend.Emission`
- `EmissionSession`
- `ModuleEmitter`
- `FunctionEmitter`
- `LocalEmitter`
- `ControlFlowEmitter`
- `ExpressionEmitter`
- `FunctionEmissionContext`
- `EmissionReport`
- `ModuleEmissionResult`
- `FunctionEmissionResult`
- `UnsupportedEmissionException`

当前 emission pass 行为：

- `EmissionSession` 持有 `CompileSession` 与 `AbstractCILBuilder`。
- `ModuleEmitter` 顺序遍历 `ModulePlan.Functions`。
- `FunctionEmitter` 只从 `FunctionPlan.Body` 入口遍历 lowered statement。
- `ControlFlowEmitter` 遍历 lowered statement/control-flow。
- `ExpressionEmitter` 遍历 lowered expression。
- `LocalEmitter` 记录 local/upvalue/module symbol/function declaration/lambda 引用。
- 遇到 `LoweredUnsupportedStatement` 或 `LoweredUnsupportedExpression` 立即抛出 `UnsupportedEmissionException`。
- statement 级 `SourceSpan` 会进入 `FunctionEmissionResult.SequencePoints`，用于后续精确调试信息生成。
- `FunctionEmissionResult` 暴露：
  - statement/expression 计数
  - local/upvalue/module symbol 引用计数
  - direct-call candidate 引用计数
  - nested function/lambda 引用计数
  - catch slot 引用计数
  - closure/direct-call 相关函数属性快照

新增测试：

- `EmissionPassConsumesSupportedLoweredPlan`
  - 验证 supported lowered plan 可被 emission pass 消费。
  - 验证 sequence point、local slot、module symbol、direct-call candidate、catch slot 均可被 emitter 看见。
- `EmissionPassConsumesLoweredBodyInsteadOfAst`
  - 人为替换 `FunctionPlan.Body` 后 emission pass 成功，证明新 emitter 不回 AST。

验证：

```text
dotnet test tests\CompilerBackend\AuroraScript.CompilerBackend.Tests.csproj -c Release --no-restore
net8.0:  27 passed, 0 failed, 0 skipped
net9.0:  27 passed, 0 failed, 0 skipped
net10.0: 27 passed, 0 failed, 0 skipped

dotnet test tests\AuroraScript.Tests.csproj -c Release --no-restore
net8.0:  289 passed, 0 failed, 0 skipped
net9.0:  289 passed, 0 failed, 0 skipped
net10.0: 289 passed, 0 failed, 0 skipped

dotnet run --project benchmark\Benchmark.csproj -c Release -- --smoke
CompilerRegressionSmoke: completed
RuntimeBenchmarks: completed
CompilerPipelineBenchmarks: completed
```

下一阶段：

- I009 开始把 emission pass 从“遍历统计”推进到真实 IL skeleton：
  - 定义 function method signature 与 local slot layout。
  - 为 literal/return/block/local variable 生成最小 IL。
  - 仍不切主入口，先做 side-by-side emission 测试。

### I009: 新 emitter 可执行 IL skeleton 最小闭环

状态：已完成。

目标：

- 在不切换主编译入口的前提下，让新 emitter 生成可执行 DynamicMethod。
- 建立 function method signature、local slot layout 和最小表达式返回路径。
- 保持复杂 lowered plan 仍走 emission pass 消费统计，不因真实 IL 子集未完成而失败。

改动：

- `EmissionSession` 新增 `emitExecutableSkeletons` 显式开关，默认关闭。
- `FunctionEmissionResult` 新增：
  - `Method`
  - `HasExecutableSkeleton`
  - `CilLocalCount`
- `FunctionEmissionContext` 可写回 generated method，并同步 `FunctionPlan.Method`。
- 新增 `ExecutableSkeletonEmitter`。

当前可执行 IL 子集：

- 方法签名：
  - `ScriptDatum Method(ScriptContext ctx, Span<ScriptDatum> args)`
- local slot layout：
  - 每个 `FunctionPlan.LocalSlots` 声明一个 `ScriptDatum` CIL local。
  - parameter local 在方法入口通过 `CILHelper.GetArg(args, index)` 初始化。
- statement：
  - block
  - return
  - simple variable declaration
  - expression statement
- expression：
  - number/string/boolean/null literal
  - local name load
- 默认 fallthrough 返回 `ScriptDatum.Null`。

边界：

- 当前只对符合最小 IL 子集的函数生成 `Method`。
- if/loop/try/call/binary/property/module/upvalue/closure 等结构仍只由 emission pass 遍历统计，不生成真实 IL。
- 遇到 lowered unsupported 节点仍立即抛 `UnsupportedEmissionException`。
- 当前真实 IL skeleton 只在 side-by-side 专项测试中启用，不影响旧 `CILEmitter`。

新增测试：

- `EmissionSkeletonExecutesLiteralReturn`
- `EmissionSkeletonStoresAndLoadsLocal`
- `EmissionSkeletonInitializesParameterLocalsFromSpanArguments`

验证：

```text
dotnet test tests\CompilerBackend\AuroraScript.CompilerBackend.Tests.csproj -c Release --no-restore
net8.0:  30 passed, 0 failed, 0 skipped
net9.0:  30 passed, 0 failed, 0 skipped
net10.0: 30 passed, 0 failed, 0 skipped

dotnet test tests\AuroraScript.Tests.csproj -c Release --no-restore
net8.0:  289 passed, 0 failed, 0 skipped
net9.0:  289 passed, 0 failed, 0 skipped
net10.0: 289 passed, 0 failed, 0 skipped

dotnet run --project benchmark\Benchmark.csproj -c Release -- --smoke
CompilerRegressionSmoke: completed
RuntimeBenchmarks: completed
CompilerPipelineBenchmarks: completed
```

下一阶段：

- I010 扩展真实 IL skeleton：
  - binary arithmetic/comparison
  - assignment/local store
  - if/while/for label skeleton
  - direct local call 或 direct module call 的最小闭环

### I010: binary/assignment/control-flow 可执行 IL skeleton

状态：已完成。

目标：

- 扩展 I009 的真实 IL skeleton，使新 emitter side-by-side 路径能执行基础表达式和控制流。
- 继续保持主编译入口不切换，复杂语法仍只做 emission pass 消费统计。

改动：

- `ExecutableSkeletonEmitter` 新增 break/continue label 栈。
- 新增 statement IL：
  - `if/else`
  - `while`
  - `for`
  - `break`
  - `continue`
- 新增 expression IL：
  - binary arithmetic
  - binary comparison
  - simple local assignment
- 条件跳转统一通过 `CILHelper.ToBoolean(ScriptDatum)` 转换。

当前新增支持的 binary operator：

- `+`
- `-`
- `*`
- `/`
- `%`
- `==`
- `!=`
- `<`
- `<=`
- `>`
- `>=`

边界：

- assignment 仅支持 local name 左值。
- compound assignment、unary increment/decrement、logical &&/|| 暂未进入真实 IL skeleton。
- call/property/index/module/upvalue/closure/direct-call 暂未进入真实 IL skeleton。
- direct-call 最小闭环延后到 I011，避免和控制流 skeleton 同轮耦合。

新增测试：

- `EmissionSkeletonExecutesBinaryArithmeticAndComparison`
- `EmissionSkeletonExecutesLocalAssignment`
- `EmissionSkeletonExecutesWhileLoop`
- `EmissionSkeletonExecutesForLoopWithBreakAndContinue`

验证：

```text
dotnet test tests\CompilerBackend\AuroraScript.CompilerBackend.Tests.csproj -c Release --no-restore
net8.0:  34 passed, 0 failed, 0 skipped
net9.0:  34 passed, 0 failed, 0 skipped
net10.0: 34 passed, 0 failed, 0 skipped

dotnet test tests\AuroraScript.Tests.csproj -c Release --no-restore
net8.0:  289 passed, 0 failed, 0 skipped
net9.0:  289 passed, 0 failed, 0 skipped
net10.0: 289 passed, 0 failed, 0 skipped

dotnet run --project benchmark\Benchmark.csproj -c Release -- --smoke
CompilerRegressionSmoke: completed
RuntimeBenchmarks: completed
CompilerPipelineBenchmarks: completed
```

下一阶段：

- I011 做 direct-call 最小闭环：
  - direct local function call
  - module internal direct call
  - fast arity method signature 对齐
  - direct-call 与 closure fallback 的边界测试

### I011: module internal DirectCall 可执行 IL 最小闭环

状态：已完成。

目标：

- 让新 emitter 的 side-by-side executable skeleton 真正消费 DirectCall 分析结果。
- 对满足条件的同模块 `InternalOnly` 函数生成 fast arity 方法签名，避免 direct call 时分配 `ScriptDatum[]` 或构造 `Span<ScriptDatum>` 参数缓冲。
- 保持 DirectCall 边界严格：仅 `EnableHotReload=false && EnableModuleDirectCall=true` 且未 export、未作为函数值读取/赋值覆盖的模块内部函数可进入。

改动：

- `LoweredCallExpression` 新增 `DirectFunction`，由 lowering 阶段基于模块符号和 `FunctionPlan.IsDirectCallCandidate` 写入。
- `FunctionLowerer` 新增 direct function map，emit 阶段不再按 AST 名字二次推断 direct call 目标。
- `ExecutableSkeletonEmitter` 改为模块级两阶段：
  - `Prepare()` 先计算可执行函数集合。
  - 先为所有可执行函数定义 `MethodInfo/ILGenerator`。
  - 再逐个发射函数体，支持前向 direct call。
- `InternalOnly` direct-call 目标函数使用 fast arity 签名：
  - `Fast0` 到 `Fast7` 对应 `ScriptDatum Method(ScriptContext ctx, ScriptDatum arg0...)`。
  - export/非 direct 函数继续使用 `ScriptDatum Method(ScriptContext ctx, Span<ScriptDatum> args)`。
- direct call 发射规则：
  - 直接 `OpCodes.Call target.Method`。
  - 缺失参数补 `ScriptDatum.Null`。
  - 超出目标 arity 的实参仍按源码顺序求值并丢弃结果，保证副作用语义。
  - 不创建参数数组，不通过 `ClosureFunction`，不经过 `DynamicMethodRegistry`。

边界：

- 本阶段只实现模块内部 DirectCall，不实现嵌套 local function/lambda direct call。
- 含 upvalue/captured local/default parameter/`arguments` 对象的函数不进入 fast direct-call skeleton。
- arity 大于 7 的 direct candidate 暂不进入 fast direct-call skeleton。
- 普通非 direct call 仍不进入 executable skeleton，后续阶段补 runtime call fallback。
- 主编译入口仍未切换，旧 `CILEmitter` 行为不受影响。

新增测试：

- `LoweringMarksModuleDirectCallTarget`
- `EmissionSkeletonExecutesForwardModuleDirectCallWithFastArity`
- `EmissionSkeletonEvaluatesExtraDirectCallArgumentsInOrder`
- `EmissionSkeletonDoesNotDirectCallWhenModuleDirectCallIsDisabled`

验证：

```text
dotnet test tests\CompilerBackend\AuroraScript.CompilerBackend.Tests.csproj -c Release --no-restore
net8.0:  38 passed, 0 failed, 0 skipped
net9.0:  38 passed, 0 failed, 0 skipped
net10.0: 38 passed, 0 failed, 0 skipped

dotnet test tests\AuroraScript.Tests.csproj -c Release --no-restore
net8.0:  289 passed, 0 failed, 0 skipped
net9.0:  289 passed, 0 failed, 0 skipped
net10.0: 289 passed, 0 failed, 0 skipped

dotnet run --project benchmark\Benchmark.csproj -c Release -- --smoke
CompilerRegressionSmoke: completed
RuntimeBenchmarks: completed
CompilerPipelineBenchmarks: completed
```

下一阶段：

- I012 先补 executable skeleton 的表达式 fast path：
  - compound/local inc-dec/unary/logical/bitwise/shift。
  - 纯包装 `GroupExpression` 在 lowering 阶段剥离。
- I013 补普通调用 fallback 和函数对象 materialization 边界：
  - 非 DirectCall 的函数调用通过 runtime helper 路径执行。
  - export/module-visible 函数保持 `ClosureFunction` 可观察行为。
  - lambda/嵌套函数只在闭包布局可正确表达后接入。

### I012: expression fast path 扩展

状态：已完成。

目标：

- 在不引入函数对象/模块对象 materialization 的前提下，继续扩大 executable skeleton 可覆盖的低风险表达式集合。
- 优先支持不需要堆分配、不依赖闭包/upvalue/property/module 的节点。
- 修正 `GroupExpression` 被误判为 unsupported 的 lowering 问题。

改动：

- `FunctionLowerer` 对 `GroupExpression` 直接 lower 内部表达式。
- `ExecutableSkeletonEmitter` 新增：
  - `EmitBinary`
  - `EmitLogical`
  - `EmitCompound`
  - `EmitUnary`
- 新增可执行 expression：
  - logical `&&`
  - logical `||`
  - bitwise `&`
  - bitwise `|`
  - bitwise `^`
  - shift `<<`
  - signed shift `>>`
  - unsigned shift `>>>`
  - local compound assignment：`+= -= *= /= %=`
  - unary：`! ~ - typeof`
  - local prefix/postfix：`++ --`
- `&&/||` 使用 label 保留短路语义，返回脚本值本身，不退化为纯 boolean。
- local `++/--` 直接 `ldloca` 调用已有 `CILHelper.Increment*/Decrement*`，不创建临时对象。

边界：

- compound assignment 仅支持 local name 左值。
- `++/--` 仅支持 local name。
- property/index/upvalue/module symbol 的 compound 或 inc-dec 暂不进入 executable skeleton。
- 普通非 DirectCall 函数调用仍不进入 executable skeleton。

新增测试：

- `EmissionSkeletonExecutesCompoundAndUnaryLocalOperators`
- `EmissionSkeletonExecutesLogicalShortCircuitAndBitwiseOperators`

验证：

```text
dotnet test tests\CompilerBackend\AuroraScript.CompilerBackend.Tests.csproj -c Release --no-restore
net8.0:  40 passed, 0 failed, 0 skipped
net9.0:  40 passed, 0 failed, 0 skipped
net10.0: 40 passed, 0 failed, 0 skipped

dotnet test tests\AuroraScript.Tests.csproj -c Release --no-restore
net8.0:  289 passed, 0 failed, 0 skipped
net9.0:  289 passed, 0 failed, 0 skipped
net10.0: 289 passed, 0 failed, 0 skipped

dotnet run --project benchmark\Benchmark.csproj -c Release -- --smoke
CompilerRegressionSmoke: completed
RuntimeBenchmarks: completed
CompilerPipelineBenchmarks: completed
```

下一阶段：

- I013 普通函数对象调用 fallback：
  - 目标表达式为函数对象时，走 `CILHelper.Invoke0..7`。
  - 不创建参数数组，不走解释器循环。
- I014 函数对象 materialization：
  - module-visible/export 函数对象必须可观察。
  - DirectCall 继续绕开 `ClosureFunction`。
  - module initializer 中批量创建/写入函数对象。

### I013: ordinary function object call fallback

状态：已完成。

目标：

- executable skeleton 支持普通函数对象调用，不只依赖 DirectCall。
- 复用运行时已有 `CILHelper.Invoke0..7` fast path。
- 保持 `0-7` fast arity 上限，超过 7 先不进入 skeleton，后续统一走 materialized args fallback。

改动：

- `ExecutableSkeletonEmitter` 新增 `EmitRegularCall`。
- regular call 发射：
  - 先发射 target expression，得到 `ScriptDatum`。
  - 调用 `ScriptDatum.ToObject` 得到 `ScriptObject`。
  - 压入 `ScriptContext` 与 0-7 个实参。
  - 调用 `CILHelper.Invoke0..7`。
- `CanEmitRegularCall` 限制：
  - target expression 必须已经可由 skeleton 发射。
  - 实参数量必须 `<= 7`。
  - 所有实参表达式必须可由 skeleton 发射。
- DirectCall 优先级高于 regular call，满足 DirectCall 条件时仍直接 `call target.Method`。

边界：

- 本阶段不创建 module-visible/export `ClosureFunction`。
- 本阶段不处理 module symbol 函数名读取。
- 本阶段不处理 spread arguments 或 8+ 参数调用。
- 本阶段不接入 property call。

新增测试：

- `EmissionSkeletonExecutesRegularFunctionObjectCallsWithFastArity`

验证：

```text
dotnet test tests\CompilerBackend\AuroraScript.CompilerBackend.Tests.csproj -c Release --no-restore
net8.0:  41 passed, 0 failed, 0 skipped
net9.0:  41 passed, 0 failed, 0 skipped
net10.0: 41 passed, 0 failed, 0 skipped

dotnet test tests\AuroraScript.Tests.csproj -c Release --no-restore
net8.0:  289 passed, 0 failed, 0 skipped
net9.0:  289 passed, 0 failed, 0 skipped
net10.0: 289 passed, 0 failed, 0 skipped

dotnet run --project benchmark\Benchmark.csproj -c Release -- --smoke
CompilerRegressionSmoke: completed
RuntimeBenchmarks: completed
CompilerPipelineBenchmarks: completed
```

下一阶段：

- I014 module function object materialization：
  - 为 module-visible/export 函数创建 `ClosureFunction`。
  - 在 module initializer 中写入 module property。
  - 让非 DirectCall 的 `helper(...)` 可以通过 module symbol 读取函数对象再走 I013 regular call fallback。

### I014: module function object materialization

状态：已完成。

检查结论：

- I013 符合预期：
  - 普通函数对象调用已走 `CILHelper.Invoke0..7`。
  - `0-7` 实参不分配参数数组。
  - DirectCall 仍优先于 ordinary function object fallback。
- I014 原实现不完整：
  - module initializer 只写入函数对象，没有让 executable skeleton 读取 module function symbol。
  - `DefineModuleInitMethod` 发生在 `DefineMethod` 之后，对 `DebuggableBuilder` / `PersistedBuilder` 的模块类型注册顺序不正确。
  - dynamic mode 下直接 `ldftn DynamicMethod` 生成闭包不可靠，应沿用 `DynamicMethodRegistry`。

修复：

- `ModuleEmitter` 在 skeleton `Prepare()` 前先定义 module initializer，保证三种 builder 都先注册模块类型。
- `ModuleInitializerEmitter` 改为：
  - `Define()` 只提前定义 initializer method/IL。
  - `TryEmit()` 在函数 skeleton 全部生成后批量写入 module-visible/export 函数对象。
  - 没有可物化函数时仍补 `ret`，但 `ModuleEmissionResult` 不暴露空 initializer。
  - `DynamicMethod` 函数对象通过 `DynamicMethodRegistry.Register` + `CILHelper.ResolveDelegate*` 生成 delegate。
- `ExecutableSkeletonEmitter` 支持 module function symbol read：
  - local name 继续 `ldloc`。
  - 可物化模块函数名走 `ctx.Module.GetPropertyDatum(ctx, name)`。
  - 随后接 I013 ordinary call fallback。
- 可物化函数限定：
  - 同模块函数 symbol。
  - `RequiresClosureObject == true`。
  - 无 upvalue / captured local。
  - 已进入 executable skeleton 集合。
  - 函数名非空。

新增/调整测试：

- `EmissionSkeletonDoesNotDirectCallWhenModuleDirectCallIsDisabled`
  - DirectCall disabled 时，`call.DirectFunction` 保持无效。
  - `helper` 和 `run` 都生成 executable skeleton。
  - module initializer 写入 `helper/run` `ClosureFunction`。
  - `run` 通过 module function object fallback 调用 `helper(41)` 并返回 `42`。

验证：

```text
dotnet test tests\CompilerBackend\AuroraScript.CompilerBackend.Tests.csproj --no-restore
net8.0:  41 passed, 0 failed, 0 skipped
net9.0:  41 passed, 0 failed, 0 skipped
net10.0: 41 passed, 0 failed, 0 skipped
```

下一阶段：

- I015 ordinary call 的 8+ 实参 materialized args fallback：
  - `0-7` 保持当前无数组 fast path。
  - `8+` 实参用 `Span<ScriptDatum>`/数组 fallback 接入 `ScriptObject.Invoke(ctx, Span<ScriptDatum>)`。
  - 保持实参从左到右求值顺序。

### I015: ordinary call materialized arguments fallback

状态：已完成。

目标：

- ordinary function object call 不再因为 `8+` 实参数量退出 executable skeleton。
- `0-7` 实参继续走 I013 的 `CILHelper.Invoke0..7` 无数组 fast path。
- `8+` 实参 materialize 后走 `ScriptObject.Invoke(ctx, Span<ScriptDatum>)` 语义。
- 实参求值或调用抛错时也不能泄漏临时缓冲。

实现：

- `CILHelper` 新增 pooled argument helpers：
  - `RentArguments(int count)`
  - `InvokeMany(ScriptObject function, ScriptContext ctx, ScriptDatum[] args, int count)`
  - `ReturnArguments(ScriptDatum[] args)`
- `RuntimeMetadata` 缓存以上 helper `MethodInfo`。
- `ExecutableSkeletonEmitter.EmitRegularCall`：
  - `<= 7` 保持原 fast path。
  - `> 7`：
    - target 先求值并转为 `ScriptObject` 存本地。
    - 租借 `ScriptDatum[]`。
    - 按源码顺序填充实参。
    - 调用 `CILHelper.InvokeMany`。
    - 使用 IL `try/finally` 调用 `CILHelper.ReturnArguments`，覆盖实参求值异常与调用异常。
- 修正 DirectCall eligibility：
  - `ClosurePlanner` 在绑定和捕获布局完成后最终校正 `IsDirectCallCandidate`。
  - `8+` 参数、默认参数、`arguments`、upvalue/captured local 等不满足 fast direct signature 的模块函数，会从 `InternalOnly` 还原为 `ModuleVisible`，从而允许 I014 materialization。

新增测试：

- `EmissionSkeletonExecutesRegularFunctionObjectCallsWithMaterializedArguments`
  - 9 个实参 callback 调用进入 skeleton。
  - 验证左到右求值顺序。
  - 验证结果 `63`。
- `EmissionSkeletonMaterializesWideModuleCallsInsteadOfInvalidDirectCall`
  - DirectCall enabled 下，9 参数内部模块函数不再错误进入 DirectCall candidate。
  - 函数被 materialize 后通过 ordinary fallback 调用。
  - 验证结果 `10`。

验证：

```text
dotnet test tests\CompilerBackend\AuroraScript.CompilerBackend.Tests.csproj --no-restore
net8.0:  43 passed, 0 failed, 0 skipped
net9.0:  43 passed, 0 failed, 0 skipped
net10.0: 43 passed, 0 failed, 0 skipped
```

下一阶段：

- I016 property call fast path：
  - `obj.method(...)` 走 `CILHelper.InvokeProperty0..7`。
  - `8+` 实参复用 I015 materialized args fallback。
  - 为后续 examples 中常见对象方法调用减少 unsupported/skeleton 退出。

### I016: property call fast path

状态：已完成。

目标：

- executable skeleton 支持固定属性读取与属性方法调用。
- `obj.method(0..7 args)` 走 `CILHelper.InvokeProperty0..7`，避免创建 bound function/参数数组。
- `obj.method(8+ args)` 复用 pooled materialized args fallback。
- 保持 receiver 先于实参求值，实参从左到右求值。

实现：

- `CILHelper` 新增 `InvokePropertyMany(ScriptObject receiver, ScriptContext ctx, string name, ScriptDatum[] args, int count)`：
  - native `BondingFunction` 属性继续直接调用 `DatumMethod`。
  - 普通属性函数走 `function.Invoke(ctx, span)`。
  - helper 内部 `finally` 调用 `ReturnArguments`，调用异常时也归还缓冲。
- `RuntimeMetadata` 缓存 `CILHelper_InvokePropertyMany`。
- `ExecutableSkeletonEmitter` 新增：
  - `LoweredGetPropertyExpression` 固定属性读取。
  - `LoweredCallExpression` + `LoweredGetPropertyExpression` 属性调用 fast path。
  - `GetInvokePropertyMethod(0..7)` 分派。
  - 8+ property call materialized fallback。
- 修正 I015/I016 materialized fallback 的 IL 结构：
  - 不在表达式内部打开 IL exception block，避免外层求值栈非空时产生 invalid IL。
  - target/receiver 先求值并存 local。
  - 实参再按顺序求值并存 local。
  - 租借数组、填充、调用 helper；helper 内部负责 finally 归还。

新增测试：

- `EmissionSkeletonExecutesPropertyCallsWithFastAndMaterializedArguments`
  - 宿主对象挂 `sum` `BondingFunction`。
  - `obj.sum(1,2,3)` 覆盖 `InvokeProperty3`。
  - `obj.sum(1..9)` 覆盖 `InvokePropertyMany`。
  - 验证结果 `63`。

验证：

```text
dotnet test tests\CompilerBackend\AuroraScript.CompilerBackend.Tests.csproj --no-restore
net8.0:  44 passed, 0 failed, 0 skipped
net9.0:  44 passed, 0 failed, 0 skipped
net10.0: 44 passed, 0 failed, 0 skipped
```

下一阶段：

- I017 object/array literal executable fast path：
  - array literal 无 spread 使用容量构造并原位填充。
  - map/object literal 先支持固定 key/value，复用 `CreateObject3` fast path。
  - 为 examples 中常见数据结构构建减少 skeleton 退出。

### I017: object/array literal executable fast path

状态：已完成。

目标：

- executable skeleton 支持无 spread array literal。
- executable skeleton 支持固定 key object/map literal。
- 三属性且 key 不重复的 object literal 复用 `CILHelper.CreateObject3` hidden-class fast path。
- `array.length` 读取走 `CILHelper.GetLength` 专用路径。

实现：

- `ExecutableSkeletonEmitter` 新增：
  - `EmitArrayLiteral`
  - `EmitMap`
  - `EmitMapEntryForCreateObject`
  - `CanEmitArrayLiteral`
  - `CanEmitMap`
  - `TryGetFastObject3`
- array literal：
  - `new ScriptArray(capacity)`。
  - `SetElementValue(index, datum)` 原位填充。
  - 最后 `ScriptDatum.FromObject`。
- map/object literal：
  - 3 个固定 key 且不重复时走 `CILHelper.CreateObject3`。
  - 其他固定 key/value object 走 `new ScriptObject()` + `SetPropertyDatum`。
  - spread/shorthand fallback 尚不进入 skeleton。
- fixed property get：
  - `length` 特判为 `CILHelper.GetLength(ScriptDatum, ScriptContext)`。
  - 其他属性走 `CILHelper.GetProperty`。

新增测试：

- `EmissionSkeletonExecutesArrayAndMapLiteralFastPaths`
  - `[1,2,3,4]` 覆盖 array literal 原位填充。
  - `{ first: array.length, second: 5, third: 6 }` 覆盖 `CreateObject3`。
  - `map.first + map.second + map.third` 覆盖 fixed property get。
  - 验证结果 `15`。

验证：

```text
dotnet test tests\CompilerBackend\AuroraScript.CompilerBackend.Tests.csproj --no-restore
net8.0:  45 passed, 0 failed, 0 skipped
net9.0:  45 passed, 0 failed, 0 skipped
net10.0: 45 passed, 0 failed, 0 skipped
```

下一阶段：

- I018 element get/set executable fast path：
  - `obj[index]` 走 `CILHelper.GetElement`。
  - `obj[index] = value` 走 `CILHelper.SetElement`。
  - compound element assignment / increment 可后续拆分。

### I018: element get/set executable fast path

状态：已完成。

目标：

- executable skeleton 支持 `obj[index]`。
- executable skeleton 支持 `obj[index] = value`。
- set element 表达式保留返回被赋值 value 的语义。

实现：

- `ExecutableSkeletonEmitter` 新增：
  - `EmitGetElement`
  - `EmitSetElement`
  - `CanEmitGetElement`
  - `CanEmitSetElement`
- element get：
  - receiver 和 index 都按 `ScriptDatum` 发射。
  - 调用 `CILHelper.GetElement(ScriptDatum, ScriptDatum)`。
- element set：
  - receiver、index、value 按源码顺序发射。
  - `value` 复制到临时 local。
  - 调用 `CILHelper.SetElement(ScriptDatum, ScriptDatum, ScriptDatum)`。
  - 重新加载临时 local 作为表达式结果。

新增测试：

- `EmissionSkeletonExecutesElementGetAndSet`
  - `[1,2,3]` 创建数组。
  - `array[1] = 5` 覆盖 element set。
  - `array[0] + array[1] + array[2]` 覆盖 element get。
  - 验证结果 `9`。

验证：

```text
dotnet test tests\CompilerBackend\AuroraScript.CompilerBackend.Tests.csproj --no-restore
net8.0:  46 passed, 0 failed, 0 skipped
net9.0:  46 passed, 0 failed, 0 skipped
net10.0: 46 passed, 0 failed, 0 skipped
```

下一阶段：

- I019 fixed property set executable fast path：
  - `obj.name = value` 走 `ScriptObject.SetPropertyDatum`。
  - 返回 assigned value。
  - compound property assignment / increment 后续拆分。

### I019: fixed property set executable fast path

状态：已完成。

目标：

- executable skeleton 支持固定属性写入 `obj.name = value`。
- set property 表达式返回 assigned value。

实现：

- `ExecutableSkeletonEmitter` 新增：
  - `EmitSetProperty`
  - `CanEmitSetProperty`
  - `TryGetStaticPropertyName(LoweredSetPropertyExpression, out string)`
- 发射顺序：
  - receiver 先求值并转 `ScriptObject`。
  - 压入 `ScriptContext` 与固定属性名。
  - value 求值后复制到临时 local。
  - 调用 `ScriptObject.SetPropertyDatum`。
  - 重新加载临时 local 作为表达式结果。

新增测试：

- `EmissionSkeletonExecutesFixedPropertySet`
  - `{ value: 1, other: 2 }` 创建对象。
  - `obj.value = obj.other + 3` 覆盖 fixed property set。
  - `return obj.value` 验证写入结果。
  - 验证结果 `5`。

验证：

```text
dotnet test tests\CompilerBackend\AuroraScript.CompilerBackend.Tests.csproj --no-restore
net8.0:  47 passed, 0 failed, 0 skipped
net9.0:  47 passed, 0 failed, 0 skipped
net10.0: 47 passed, 0 failed, 0 skipped
```

下一阶段：

- I020 constructor call executable fast path：
  - `new Type(...)` 走 `CILHelper.New0..2` / `CILHelper.New`。
  - 8+ 参数复用 pooled argument fallback。

### I020: constructor call executable fast path

状态：已完成。

目标：

- executable skeleton 支持 `new Type(...)`。
- `0-2` 构造实参走 `CILHelper.New0..2` fast path。
- `3+` 构造实参走 pooled materialized args fallback。
- 保持 target 先于实参求值，实参从左到右求值。

实现：

- `CILHelper` 新增 `NewMany(ScriptObject type, ScriptContext ctx, ScriptDatum[] args, int count)`：
  - 内部调用现有 `New(type, ctx, span)`。
  - `finally` 归还 pooled argument buffer。
- `RuntimeMetadata` 缓存 `CILHelper_NewMany`。
- `ExecutableSkeletonEmitter` 新增：
  - `EmitNew`
  - `EmitNewMany`
  - `CanEmitNew`
  - `GetNewMethod`
- `3+` 构造调用沿用 I015 的安全结构：
  - target 先求值并存 local。
  - args 求值到 locals。
  - 租借数组并填充。
  - 调用 helper，helper 负责归还数组。

新增测试：

- `EmissionSkeletonExecutesConstructorFastAndMaterializedArguments`
  - 测试内 `CountingType : ScriptType` 构造器返回 `args.Length + sum(args)`。
  - `new Type(1,2)` 覆盖 `New2`。
  - `new Type(1,2,3,4)` 覆盖 `NewMany`。
  - 验证结果 `19`。

验证：

```text
dotnet test tests\CompilerBackend\AuroraScript.CompilerBackend.Tests.csproj --no-restore
net8.0:  48 passed, 0 failed, 0 skipped
net9.0:  48 passed, 0 failed, 0 skipped
net10.0: 48 passed, 0 failed, 0 skipped
```

下一阶段：

- I021 delete/throw/debugger/in operator statement-expression coverage：
  - `in` expression 可先接 `CILHelper.Included`。
  - `throw` / `delete` / `debugger` 保持语义后进入 skeleton。

### I021: in/throw/delete/debugger coverage

状态：已完成。

本轮完成：

- ordinary `a in obj` expression。
- `throw expr` statement。
- fixed property delete。
- element delete。
- `debugger` statement。

实现：

- lowering：
  - `IncludedExpression` 现在 lowering 为 `LoweredInExpression`。
  - `LoweredInExpression.Left` 从 `LoweredNameExpression` 放宽为 `LoweredExpression`，兼容普通 `in` 左侧表达式。
  - `ForInStatement.Iterator` 仍传入 name lowered expression，不影响 for-in 结构。
- executable skeleton：
  - `EmitIn`：
    - 先发射 right/collection 并转 `ScriptObject`。
    - 再发射 left/value。
    - 调用 `CILHelper.Included`。
  - `EmitThrow`：
    - 发射表达式。
    - 调用 `CILHelper.Throw`。
  - `EmitDelete`：
    - `delete obj.name` 走 `CILHelper.DeleteProperty`。
    - `delete obj[index]` 走 `CILHelper.DeleteElement`。
  - `EmitDebugger`：
    - 保留旧 emitter 条件。
    - `NET9_0_OR_GREATER` 下仅 `PersistedBuilder + Debug` 发 `OpCodes.Break`。
    - 其他目标框架沿用 `Debug` 下发 `OpCodes.Break`。
- `CanEmitExpression` / `CanEmitStatement` 同步加入 `LoweredInExpression` 和 `LoweredThrowStatement`。
  - `delete` 仅固定属性和元素访问进入 skeleton。

新增测试：

- `EmissionSkeletonExecutesInExpression`
  - `{ first: 1 }` + `"first" in obj`。
  - 验证结果 `true`。
- `EmissionSkeletonExecutesThrowStatement`
  - `throw "boom"`。
  - 验证 skeleton 生成并抛出 `AuroraRuntimeException`。
- `EmissionSkeletonExecutesDeleteStatement`
  - 验证 `delete obj.first` 和 `delete array[1]`。
- `EmissionSkeletonAcceptsDebuggerStatement`
  - 验证 `debugger; return 7;` 不阻断 dynamic skeleton。

验证：

```text
dotnet test tests\CompilerBackend\AuroraScript.CompilerBackend.Tests.csproj --no-restore
net8.0:  52 passed, 0 failed, 0 skipped
net9.0:  52 passed, 0 failed, 0 skipped
net10.0: 52 passed, 0 failed, 0 skipped
```

### I022: try/catch/finally executable coverage

状态：已完成。

本轮完成：

- `LoweredTryStatement` 进入 executable skeleton。
- 裸 `try { ... }` 支持：
  - 源语法允许省略 catch/finally。
  - parser 删除旧的 `try statement requires catch or finally.` 限制。
  - skeleton 将裸 try 按普通语句块发射，不创建 exception block。
- 有 handler 的 try 支持：
  - `try/catch`
  - `try/finally`
  - `try/catch/finally`
  - catch 变量通过 `CILHelper.ExceptionToError(Exception)` 转换后写入 binder 预分配的 `CatchSlot`。

实现：

- `ExecutableSkeletonEmitter.EmitTry`：
  - 无 catch/finally：直接 `EmitStatement(statement.Body)`。
  - 有 catch/finally：使用 `BeginExceptionBlock` / `BeginCatchBlock` / `BeginFinallyBlock` / `EndExceptionBlock`。
  - catch body 不存在但 catch 语法存在时，保持旧 emitter 语义：捕获异常并丢弃或写入 catch slot。
- `CanEmitTry`：
  - 裸 try 使用普通 `CanEmitStatement`，允许 return/break/continue 按原上下文发射。
  - 有 handler 的 try 使用 protected statement 子集，当前不允许 `return/break/continue` 跨异常保护区。
  - 该边界避免生成 invalid IL；后续若要支持 handler 内 return，需要引入统一 `leave + return slot` 控制流重写。
- `ParserSyntaxTests`：
  - `try { var value = 1; }` 从 invalid syntax 移入 supported grammar。

新增测试：

- `EmissionSkeletonExecutesBareTryStatement`
  - 验证裸 try 中的 `return` 按普通块执行。
- `EmissionSkeletonExecutesTryCatchFinallyStatement`
  - 验证 throw 被 catch 捕获，finally 后续执行。
- `EmissionSkeletonExecutesTryFinallyStatement`
  - 验证无 catch 的 finally 正常执行。

验证：

```text
dotnet test tests\CompilerBackend\AuroraScript.CompilerBackend.Tests.csproj --no-restore
net8.0:  55 passed, 0 failed, 0 skipped
net9.0:  55 passed, 0 failed, 0 skipped
net10.0: 55 passed, 0 failed, 0 skipped

dotnet test tests\AuroraScript.Tests.csproj --no-restore
net8.0:  289 passed, 0 failed, 0 skipped
net9.0:  289 passed, 0 failed, 0 skipped
net10.0: 289 passed, 0 failed, 0 skipped
```

### I026: array/object spread literal coverage

状态：已完成。

本轮完成：

- executable skeleton 支持 array literal spread：
  - `[1, ...items, 4]`
- executable skeleton 支持 object/map literal spread：
  - `{ ...source, key: value }`
- 同步支持 object shorthand：
  - `{ value }`

实现：

- `EmitArrayLiteral`：
  - 无 spread 保持原固定容量 + `SetElementValue` 快路径。
  - 有 spread 时改为容量 0 初始化 + 顺序 `Push` / `CILHelper.SpreadInto`。
  - spread operand 先发射为 `ScriptDatum`，再 `ScriptDatum.ToObject`。
- `EmitMap`：
  - fast `CreateObject3` 继续只处理三个固定 key/value 且无重复 key 的对象。
  - 普通路径支持：
    - explicit key/value：`ScriptObject.SetPropertyDatum`
    - spread：`ScriptObject.CopyPropertysFrom(source, force: false)`
    - shorthand：从 `LoweredNameExpression.Name` 推断 key。
- `CanEmitArrayLiteral`：
  - spread element 检查内部 expression 是否可发射。
- `CanEmitMap`：
  - spread entry 必须 keyless。
  - shorthand 只接受 keyless `LoweredNameExpression`。
  - 其它 keyless expression 继续拒绝，避免猜错对象字面量语义。

新增测试：

- `EmissionSkeletonExecutesArrayAndMapSpreadLiterals`
  - 验证 array spread、object spread、shorthand、后续属性覆盖。

验证：

```text
dotnet test tests\CompilerBackend\AuroraScript.CompilerBackend.Tests.csproj --no-restore
net8.0:  60 passed, 0 failed, 0 skipped
net9.0:  60 passed, 0 failed, 0 skipped
net10.0: 60 passed, 0 failed, 0 skipped

dotnet test tests\AuroraScript.Tests.csproj --no-restore
net8.0:  289 passed, 0 failed, 0 skipped
net9.0:  289 passed, 0 failed, 0 skipped
net10.0: 289 passed, 0 failed, 0 skipped
```

### I027: spread call and spread constructor coverage

状态：已完成。

本轮完成：

- executable skeleton 支持 ordinary function object spread call：
  - `callback(0, ...values, 5)`
- executable skeleton 支持 property spread call：
  - `obj.sum(1, ...values, 5)`
- executable skeleton 支持 constructor spread call：
  - `new Type(1, ...values, 5)`
- spread call 不复用旧 emitter 的 `List<ScriptDatum>` + `CollectionsMarshal.AsSpan` 路径。

实现：

- `CILHelper`：
  - 新增 `EnsureArgumentCapacity(ScriptDatum[] args, int count)`。
  - 新增 `AddArgument(ScriptDatum[] args, ref int count, ScriptDatum value)`。
  - 新增 `SpreadIntoArguments(ScriptDatum[] args, ref int count, ScriptObject value)`。
  - 使用 `ArrayPool<ScriptDatum>` 扩容，扩容时只复制已写入的 `count` 个元素。
  - 最终仍复用 `InvokeMany` / `InvokePropertyMany` / `NewMany` 的 `finally ReturnArguments` 归还路径。
- `RuntimeMetadata`：
  - 缓存 `CILHelper_AddArgument`。
  - 缓存 `CILHelper_SpreadIntoArguments`。
- `ExecutableSkeletonEmitter`：
  - `EmitRegularCall`：有 spread 时强制走 materialized pooled buffer path。
  - `EmitPropertyCall`：有 spread 时强制走 materialized pooled buffer path。
  - `EmitNew`：有 spread 时强制走 `EmitNewMany`。
  - 新增 `EmitArgumentsToBuffer`：
    - 初始按 argument expression 数量租用 buffer，至少 1。
    - 普通实参调用 `AddArgument`。
    - spread 实参先转 `ScriptObject`，再调用 `SpreadIntoArguments`。
    - 左到右发射表达式，保留求值顺序。
  - `CanEmitDirectCall` 明确拒绝 spread 参数，避免 direct-call fast path 把 spread 当普通实参。
  - ordinary/property/new call 的 eligibility 改为 `CanEmitArgument`，spread 时检查内部 expression 是否可发射。

新增测试：

- `EmissionSkeletonExecutesSpreadFunctionObjectCalls`
  - 验证 ordinary function object spread call。
- `EmissionSkeletonExecutesSpreadPropertyCalls`
  - 验证 property spread call。
- `EmissionSkeletonExecutesSpreadConstructorCalls`
  - 验证 constructor spread call。

验证：

```text
dotnet test tests\CompilerBackend\AuroraScript.CompilerBackend.Tests.csproj --no-restore -f net9.0
net9.0:  63 passed, 0 failed, 0 skipped

dotnet test tests\CompilerBackend\AuroraScript.CompilerBackend.Tests.csproj --no-restore -f net10.0
net10.0: 63 passed, 0 failed, 0 skipped

dotnet test tests\AuroraScript.Tests.csproj --no-restore -f net9.0
net9.0:  289 passed, 0 failed, 0 skipped

dotnet test tests\AuroraScript.Tests.csproj --no-restore -f net10.0
net10.0: 289 passed, 0 failed, 0 skipped
```

备注：

- 本机当前缺少 `Microsoft.NETCore.App 8.0.0` x64 runtime，net8 testhost 无法启动。
- net8 未出现断言失败，测试进程在启动阶段因 runtime 缺失中止。

### I028: uncaptured local function hoist and materialization

状态：已完成。

本轮完成：

- 修正 lowering 的 block function declaration 顺序：
  - 旧 emitter 在 block 内先访问 `block.Functions`，再执行普通语句。
  - 新 lowering 原先把 `block.Functions` 追加到普通语句之后，可能破坏声明前调用语义。
  - 现在 `LowerBlock` 先 lowering `block.Functions`，再 lowering `block` 普通语句。
- executable skeleton 支持无捕获局部函数声明：
  - `func helper(...) { ... }`
  - 声明被物化为 `ClosureFunction`，转为 `ScriptDatum` 后写入对应 local slot。
  - 后续 `helper(...)` 走既有 ordinary function object call path。

边界：

- 仅支持无 upvalue / 无 captured local 的局部函数。
- 捕获局部的函数仍不进入 skeleton，等待后续 upvalue materialization。
- 局部函数闭包物化需要有效 `ScriptContext`，测试中使用真实 domain/module context。

实现：

- `FunctionLowerer.LowerBlock`：
  - 函数声明 lowering 顺序改为先 `block.Functions`，再普通 statement。
- `ExecutableSkeletonEmitter`：
  - `EmitStatement` 新增 `LoweredFunctionDeclarationStatement`。
  - 新增 `EmitFunctionDeclaration`：
    - `ClosureMaterializer.EmitClosure`
    - `ScriptDatum.FromObject`
    - `stloc localSlot`
  - 新增 `CanEmitFunctionDeclaration`：
    - local slot 有效。
    - function id 有效且在 executable set 内。
    - 目标函数可无捕获物化。

新增测试：

- `EmissionSkeletonHoistsUncapturedLocalFunctionDeclarations`
  - 验证声明前调用 `helper(value)`。
  - 验证 `run` 和局部 `helper` 都生成 executable skeleton。

验证：

```text
dotnet test tests\CompilerBackend\AuroraScript.CompilerBackend.Tests.csproj --no-restore -f net9.0
net9.0:  64 passed, 0 failed, 0 skipped

dotnet test tests\CompilerBackend\AuroraScript.CompilerBackend.Tests.csproj --no-restore -f net10.0
net10.0: 64 passed, 0 failed, 0 skipped

dotnet test tests\AuroraScript.Tests.csproj --no-restore -f net9.0
net9.0:  289 passed, 0 failed, 0 skipped

dotnet test tests\AuroraScript.Tests.csproj --no-restore -f net10.0
net10.0: 289 passed, 0 failed, 0 skipped
```

备注：

- 本机当前仍缺少 `Microsoft.NETCore.App 8.0.0` x64 runtime，net8 testhost 无法启动。

### I025: default parameter lowering and skeleton initialization

状态：已完成。

背景：

- 风险核查发现：默认参数函数可能进入 executable skeleton，但旧实现只用 `CILHelper.GetArg` 初始化参数，会丢失默认值语义。
- 该问题必须修正，否则新架构会错误接管默认参数函数。

本轮完成：

- 默认参数 initializer lowering 到 `FunctionPlan.ParameterDefaults`。
- 默认参数表达式参与 unsupported 统计。
- executable skeleton 在 Span 调用约定下支持默认参数。
- 默认参数函数仍不会进入 fast direct-call：
  - `SelectCallConvention` 保持 `HasDefaultParameters => Span`。
  - module direct-call 判定继续排除默认参数函数。

实现：

- `FunctionPlan`：
  - 新增 `LoweredExpression[] ParameterDefaults`。
- `FunctionLowerer`：
  - lowering 每个 `ParameterDeclaration.Initializer`。
  - 默认值表达式复用函数本地 slot/upvalue/module symbol 解析。
  - unsupported counter 统计默认值表达式。
- `ExecutableSkeletonEmitter.InitializeParameters`：
  - 无默认值：保持 `CILHelper.GetArg(args, index)`。
  - 有默认值：先发射默认表达式，再调用 `CILHelper.TryGetArg(args, index, defaultValue)`。
  - 支持默认表达式读取前序参数 local，如 `b = a + 3`。
- `CanEmitParameterDefaults`：
  - 默认表达式不可 skeleton 发射时，函数不进入 executable skeleton。

新增测试：

- `EmissionSkeletonExecutesDefaultParameterFunctions`
  - `func add(a, b = a + 3)`。
  - 验证 `add(2) + add(2, 10) == 15`。
  - 验证默认参数函数走 Span convention 且仍能作为 materialized module function 被 `run` skeleton 调用。

验证：

```text
dotnet test tests\CompilerBackend\AuroraScript.CompilerBackend.Tests.csproj --no-restore
net8.0:  59 passed, 0 failed, 0 skipped
net9.0:  59 passed, 0 failed, 0 skipped
net10.0: 59 passed, 0 failed, 0 skipped

dotnet test tests\AuroraScript.Tests.csproj --no-restore
net8.0:  289 passed, 0 failed, 0 skipped
net9.0:  289 passed, 0 failed, 0 skipped
net10.0: 289 passed, 0 failed, 0 skipped
```

### I024: uncaptured lambda materialization

状态：已完成。

本轮完成：

- executable skeleton 支持无捕获 lambda 表达式。
- 覆盖形态：
  - `somecall((a, b) => a + b, 2, 3)`
  - lambda 作为普通调用实参传递。
- 捕获 lambda / upvalue lambda 仍不进入 skeleton，等待 upvalue materialization。
- 局部函数声明暂不接入 skeleton：
  - 当前 lowering 将 `block.Functions` 追加到普通语句之后，和旧 emitter 的 block-level hoist 不一致。
  - 直接发射会改变“声明前调用”的语义，因此需要后续单独做 block-level function initialization pass。

实现：

- 新增 `ClosureMaterializer`：
  - 统一发射 `ClosureFunction` 构造 IL。
  - module initializer 和 lambda expression 共用同一套 delegate/closure 生成路径。
  - 支持 `Fast0..Fast7` 与 `Span` 调用约定。
  - lambda 名称为空时向 `ClosureFunction` 传 `null`。
- `ModuleInitializerEmitter`：
  - 删除重复闭包发射逻辑。
  - 改为调用 `ClosureMaterializer.EmitClosure`。
- `ExecutableSkeletonEmitter`：
  - `LoweredLambdaExpression` 发射为 `ClosureFunction`，随后转成 `ScriptDatum`。
  - `CanEmitLambda` 要求目标 lambda function：
    - 在当前 executable set 内。
    - 无 upvalue / captured local。
    - 可物化 closure。
- `DynamicMethodRegistry` / `EmissionSession`：
  - 增加 dynamic delegate reserve/late-register。
  - 解决父函数发射 lambda 闭包时，lambda `DynamicMethod` 方法体尚未完成导致 `CreateDelegate` 失败的问题。
  - `EmissionSession` 以 `(DynamicMethod, FunctionCallConvention)` 缓存 delegate id，避免重复注册。
  - 所有模块发射完成后统一完成 `CreateDelegate` 和 registry 写入。

新增测试：

- `EmissionSkeletonMaterializesUncapturedLambdaArguments`
  - 验证父函数和 lambda 函数都生成 executable skeleton。
  - 使用宿主 `BondingFunction` 调回传入的 lambda，验证结果为 `5`。

验证：

```text
dotnet test tests\CompilerBackend\AuroraScript.CompilerBackend.Tests.csproj --no-restore
net8.0:  58 passed, 0 failed, 0 skipped
net9.0:  58 passed, 0 failed, 0 skipped
net10.0: 58 passed, 0 failed, 0 skipped

dotnet test tests\AuroraScript.Tests.csproj --no-restore
net8.0:  289 passed, 0 failed, 0 skipped
net9.0:  289 passed, 0 failed, 0 skipped
net10.0: 289 passed, 0 failed, 0 skipped
```

### I023: for-in executable coverage

状态：已完成。

本轮完成：

- `LoweredForInStatement` 进入 executable skeleton。
- 支持：
  - `for (var item in array)`
  - `for (var key in object)`
  - `for (var ch in string)`
  - body 内部 `break` / `continue`

实现：

- `ExecutableSkeletonEmitter.EmitForIn`：
  - 先发射 initializer，确保 `var item` 的 local slot 已初始化。
  - 发射 iterator right expression，转为 `ScriptObject`。
  - 调用 `ScriptObject.GetEnumerator()` 获取 `ScriptEnumerator`。
  - 循环条件使用 `ScriptEnumerator.NextValue(out ScriptDatum)`，直接写入 lowering/binder 分配的循环变量 local。
  - 复用 `_breakLabels` / `_continueLabels`，`continue` 跳到 increment label 后再回 condition。
- `CanEmitForIn`：
  - 仅允许 iterator left 为本地变量。
  - collection expression、initializer、body 都必须可被 skeleton 发射。

新增测试：

- `EmissionSkeletonExecutesForInAcrossArrayObjectAndString`
  - 验证 array/object/string 三类枚举路径。
- `EmissionSkeletonExecutesForInWithBreakAndContinue`
  - 验证 for-in 内部 break/continue 标签正确。

验证：

```text
dotnet test tests\CompilerBackend\AuroraScript.CompilerBackend.Tests.csproj --no-restore
net8.0:  57 passed, 0 failed, 0 skipped
net9.0:  57 passed, 0 failed, 0 skipped
net10.0: 57 passed, 0 failed, 0 skipped

dotnet test tests\AuroraScript.Tests.csproj --no-restore
net8.0:  289 passed, 0 failed, 0 skipped
net9.0:  289 passed, 0 failed, 0 skipped
net10.0: 289 passed, 0 failed, 0 skipped
```

### I029: `$args` object skeleton coverage

状态：已完成。

背景：

- 旧 emitter 支持函数体内读取 `$args`，通过当前调用的 `Span<ScriptDatum>` 构造 `ScriptArray`。
- 使用 `$args` 的函数不能进入 fast direct-call，否则会丢失真实实参数量；应保持 Span 调用约定。
- `arguments` 不是旧 emitter 的内置名，本轮不扩展语义，避免把普通标识符误判为参数对象并误伤 direct-call。

本轮完成：

- `FunctionBinder.SpecialUsageScanner`：
  - 扫描函数体内 `$args`。
  - 跳过嵌套函数/lambda，避免父函数误继承子函数的 `$args` 使用。
  - 只识别 `$args`，不再将 `arguments` 当成内置参数对象。
- `ExecutableSkeletonEmitter.EmitName`：
  - 对 `$args` 发射 `ldarg.1` + `new ScriptArray(Span<ScriptDatum>)`。
  - 转为 `ScriptDatum` 后参与普通表达式路径。
- `ExecutableSkeletonEmitter.CanEmitName`：
  - 将 `$args` 作为 skeleton 可发射内置名。
- direct-call 约束保持：
  - `UsesArgumentsObject == true` 时函数仍走 `FunctionCallConvention.Span`。
  - module direct-call 继续拒绝该函数，保留真实实参数组语义。

新增测试：

- `EmissionSkeletonExecutesArgsObjectFunctions`
  - `func count(a, b = 5) { return a + b + $args.length; }`
  - 验证 `count(2) + count(2, 10) == 22`。
  - 验证 `UsesArgumentsObject == true`、`IsDirectCallCandidate == false`、`CallConvention == Span`。

验证：

```text
dotnet test tests\CompilerBackend\AuroraScript.CompilerBackend.Tests.csproj --no-restore -f net9.0
net9.0:  65 passed, 0 failed, 0 skipped

dotnet test tests\CompilerBackend\AuroraScript.CompilerBackend.Tests.csproj --no-restore -f net10.0
net10.0: 65 passed, 0 failed, 0 skipped

dotnet test tests\AuroraScript.Tests.csproj --no-restore -f net9.0
net9.0:  289 passed, 0 failed, 0 skipped

dotnet test tests\AuroraScript.Tests.csproj --no-restore -f net10.0
net10.0: 289 passed, 0 failed, 0 skipped
```

备注：

- 本机当前缺少 `Microsoft.NETCore.App 8.0.0` x64 runtime，net8 testhost 无法启动；本轮只验证 net9.0 / net10.0。

### I030: destructuring declaration lowering and skeleton coverage

状态：已完成。

背景：

- 主测试和 Examples 已使用变量解构，如 `var [first, ...middle, last] = ...` 与 `var { name, age } = ...`。
- 旧 lowering 将 `VariableDeclaration` 解构直接标为 unsupported，导致新 executable skeleton 无法接管这类高频业务代码。
- Parser 当前可达语法范围：
  - 对象解构只支持属性简写 `{ a, b }`。
  - 数组解构支持标识符元素与 spread 标识符，支持 rest 后继续跟尾部元素。
  - 不支持对象别名、默认值、嵌套 pattern；本轮不为不可达 AST 设计额外分支。

本轮完成：

- Lowered model：
  - 新增 `LoweredObjectDestructuringDeclarationStatement`。
  - 新增 `LoweredArrayDestructuringDeclarationStatement`。
  - 新增 object/array destructuring binding 结构，直接保存目标 `LocalSlotId`。
- `FunctionLowerer`：
  - 普通变量声明保持 `LoweredVariableDeclarationStatement`。
  - `{ a, b }` lowering 为对象解构声明，initializer 只 lowering 一次。
  - `[first, ...middle, last]` lowering 为数组解构声明：
    - 普通前缀元素按固定 index 读取。
    - rest 使用 start index 与 trailing count。
    - rest 后尾部元素按 `array.Length - trailingCount` 读取。
  - unsupported counter 递归统计解构 initializer。
- 非执行型 emission 统计：
  - 记录解构目标 local slot 引用。
  - 记录 initializer 表达式引用。
- `ExecutableSkeletonEmitter`：
  - 对象解构：
    - initializer 转 `ScriptObject` 后存临时 local。
    - 逐属性调用 `ScriptObject.GetPropertyDatum(ctx, name)`。
  - 数组解构：
    - initializer 转 `ScriptArray` 后存临时 local。
    - 普通元素调用 `ScriptArray.GetElement(index)`。
    - rest 调用现有 `ScriptArray.SliceTo(start, end, ref target)`。
  - `CanEmit` / protected try block 判定都接入解构声明。

新增/调整测试：

- `LoweringCountsUnsupportedNodes`
  - 解构不再作为 unsupported 样例。
- `LoweringRepresentsDestructuringDeclarations`
  - 验证 object/array destructuring lowered 结构、rest trailing count。
- `EmissionSkeletonExecutesDestructuringDeclarations`
  - 验证 object/array destructuring 可生成 executable skeleton 并执行。
  - 覆盖 rest 与 rest 后尾部元素。

验证：

```text
dotnet test tests\CompilerBackend\AuroraScript.CompilerBackend.Tests.csproj --no-restore -f net9.0
net9.0:  67 passed, 0 failed, 0 skipped

dotnet test tests\CompilerBackend\AuroraScript.CompilerBackend.Tests.csproj --no-restore -f net10.0
net10.0: 67 passed, 0 failed, 0 skipped

dotnet test tests\AuroraScript.Tests.csproj --no-restore -f net9.0
net9.0:  289 passed, 0 failed, 0 skipped

dotnet test tests\AuroraScript.Tests.csproj --no-restore -f net10.0
net10.0: 289 passed, 0 failed, 0 skipped
```

备注：

- 本机当前缺少 `Microsoft.NETCore.App 8.0.0` x64 runtime，net8 testhost 无法启动；本轮只验证 net9.0 / net10.0。

### I031: regex literal and element compound-add skeleton coverage

状态：已完成。

背景：

- Parser 已将模板字符串 lowering 为普通 `BinaryExpression(Add)` 链，新后端无需专用模板节点。
- `typeof` 已通过 `UnaryExpression` + `CILHelper.TypeOf` 接入 skeleton。
- 剩余高价值表达式缺口包括：
  - Regex literal：旧 emitter 支持 `/pattern/flags`，新 skeleton literal 白名单未包含 `RegexToken`。
  - 元素 `+=`：Parser 对 `values[index++] += 2` 保留 `CompoundExpression`，旧 emitter 使用 helper 保证目标只求值一次；新 skeleton 只支持 local compound。

本轮完成：

- `ExecutableSkeletonEmitter.EmitLiteral`：
  - `RegexToken` 发射 `RegexManager.Resolve(pattern, flags)`。
  - 转为 `ScriptDatum.FromObject`，后续 property call 路径可直接调用 `.test(...)`。
- `ExecutableSkeletonEmitter.CanEmitExpression`：
  - literal 白名单加入 `RegexToken`。
- `ExecutableSkeletonEmitter.EmitCompound`：
  - 支持 `LoweredGetElementExpression` 左值的 `+=`。
  - 使用现有 `CILHelper.CompoundAddElement(ScriptDatum, ScriptDatum, ScriptDatum)`。
  - 保持 receiver/index/right 各求值一次。
- `CanEmitCompound`：
  - local compound 保持原 fast path。
  - element compound 仅允许 `+=`，与旧 helper 覆盖范围一致。

新增测试：

- `EmissionSkeletonExecutesElementCompoundAddOnce`
  - 验证 `values[index++] += 2` 结果正确且 index 只递增一次。
- `EmissionSkeletonExecutesRegexLiteralCalls`
  - 验证 `/aurora/i.test('AURORA Script')` 可生成 executable skeleton 并返回 `true`。

验证：

```text
dotnet test tests\CompilerBackend\AuroraScript.CompilerBackend.Tests.csproj --no-restore -f net9.0
net9.0:  69 passed, 0 failed, 0 skipped

dotnet test tests\CompilerBackend\AuroraScript.CompilerBackend.Tests.csproj --no-restore -f net10.0
net10.0: 69 passed, 0 failed, 0 skipped

dotnet test tests\AuroraScript.Tests.csproj --no-restore -f net9.0
net9.0:  289 passed, 0 failed, 0 skipped

dotnet test tests\AuroraScript.Tests.csproj --no-restore -f net10.0
net10.0: 289 passed, 0 failed, 0 skipped
```

备注：

- 本机当前缺少 `Microsoft.NETCore.App 8.0.0` x64 runtime，net8 testhost 无法启动；本轮只验证 net9.0 / net10.0。

### I032: property/element unary mutation skeleton coverage

状态：已完成。

背景：

- 旧 emitter 支持 `obj.value++`、`--obj.value`、`array[index]++`、`--array[index]`。
- 新 skeleton 此前只支持本地变量自增自减，导致包含属性/元素 mutation 的函数无法进入 skeleton。
- Runtime 已有 mutation helper，且能保证 receiver/index 求值一次。

本轮完成：

- `ExecutableSkeletonEmitter.EmitUnary`：
  - local mutation 保持 `ldloca + CILHelper.Increment/Decrement*`。
  - element mutation：
    - receiver 转 `ScriptObject`。
    - index 按表达式求值一次。
    - 调用 `CILHelper.IncrementElement*` / `DecrementElement*`。
  - property mutation：
    - 限制静态属性名。
    - receiver 转 `ScriptObject`。
    - 调用 `CILHelper.IncrementProperty*` / `DecrementProperty*`。
- `CanEmitUnary`：
  - 扩展允许本地变量、静态属性、元素三类 mutation target。
  - 动态属性名仍拒绝进入 skeleton。

新增测试：

- `EmissionSkeletonExecutesPropertyAndElementUnaryMutation`
  - 覆盖 `obj.value++`、`--obj.value`、`values[index++]++`。
  - 验证 postfix/prefix 返回值语义。
  - 验证元素索引表达式只求值一次。

验证：

```text
dotnet test tests\CompilerBackend\AuroraScript.CompilerBackend.Tests.csproj --no-restore -f net9.0
net9.0:  70 passed, 0 failed, 0 skipped

dotnet test tests\CompilerBackend\AuroraScript.CompilerBackend.Tests.csproj --no-restore -f net10.0
net10.0: 70 passed, 0 failed, 0 skipped

dotnet test tests\AuroraScript.Tests.csproj --no-restore -f net9.0
net9.0:  289 passed, 0 failed, 0 skipped

dotnet test tests\AuroraScript.Tests.csproj --no-restore -f net10.0
net10.0: 289 passed, 0 failed, 0 skipped
```

备注：

- 本机当前缺少 `Microsoft.NETCore.App 8.0.0` x64 runtime，net8 testhost 无法启动；本轮只验证 net9.0 / net10.0。
