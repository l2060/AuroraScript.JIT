# AuroraEngine 并行编译链路最终优化计划

## 1. 文档状态

- 日期：2026-06-22
- 范围：`AuroraEngine`、源码管理、模块依赖、语义分析、CIL Emitter、三种 Builder、增量缓存和 Benchmark。
- 目标：在保持确定性与语义正确的前提下，提高多文件编译吞吐，降低编译总分配和重复构建成本。
- 兼容策略：允许调整公开 API 和内部模型，不为旧实现保留双轨适配层。

## 2. 已完成基础修复

以下内容已经实现，不属于后续待办：

1. `ScriptCompiler` 不再使用 `Task.Factory.StartNew(async ...)`，所有 Worker 都被完整等待。
2. 模块路径经过规范化，并使用平台适配的路径比较器去重。
3. 模块注册使用 `TryAdd` 后入队，不再在 `ConcurrentDictionary.GetOrAdd` 工厂中执行副作用。
4. Worker 遇到模块错误后继续排空队列并聚合诊断，不会因为 `break` 导致 pending 永远无法归零。
5. Worker 数量不再由初始入口文件数量限制，单入口发现宽依赖后可以自适应扩容。
6. `EngineOptions.MaxDegreeOfParallelism` 支持限制前端并行度，`0` 表示使用处理器数量。
7. 支持 `CancellationToken`，同一个 `AuroraEngine` 的 Build 使用异步锁串行发布。
8. 模块输出顺序确定化；重复依赖边去重；循环依赖明确报错，不再静默丢失模块。
9. Persistence 模式只生成一份 PE `byte[]`，同一数组用于文件输出和 `Assembly.Load`。
10. Build 完成时创建并缓存 `ScriptFunctionDelegate`，`CreateDomain` 不再重复反射创建委托。

已增加回归 Smoke：宽依赖、重复入口、多模块错误聚合、循环依赖、取消、Dynamic 编译以及 Persistence 完整脚本执行。

## 3. 当前剩余瓶颈

当前链路为：

```text
Source discovery
  -> parallel read + Lexer + Parser
  -> serial link/check/sort
  -> serial CILEmitter for every module
  -> serial Builder finalization
  -> optional PE serialize/load
```

现有基准中，大样本 Parser 约 2.5 ms，Emitter 约 7.1 ms，完整编译约 10.9 ms。仅并行 Parser 的整体理论收益约 1.3 倍，Emitter 和 Builder 必须继续拆分。

主要剩余问题：

1. `CILEmitter` 同时持有全局编译状态和模块局部状态，不能模块级并行。
2. `ModuleBuilder`、`TypeBuilder` 和 `ILGenerator` 不具备可依赖的并行安全保证。
3. Lexer 读取接口仍要求完整 `string`，并行文件越多，源文本峰值内存越高。
4. Full Build 与 Hot Patch 使用两套依赖发现实现，行为和优化策略会分叉。
5. 没有内容哈希、语法树或 Lowering 结果缓存，重复 Build 总是全量执行。
6. `ModulePath.GetHashCode()` 被写入运行时元数据，存在随机化和碰撞风险。
7. 当前多模块 Benchmark 只有两个很小的文件，不能衡量并行扩展能力。

## 4. 最终目标架构

```text
AuroraEngine
  -> CompilationSession
       -> SourceManager
       -> DependencyGraph
       -> Parallel Frontend
            Lexer -> Parser -> ModuleSyntax
       -> Global Symbol/Module Linking
       -> Parallel Module Lowering
            ModuleSyntax -> ModuleEmitPlan
       -> Backend
            DynamicBackend: parallel module emit
            ReflectionBackend: parallel plan + serial commit
            MetadataBackend: parallel method bodies + serial metadata merge
       -> CompilationResult
  -> atomic install of EntryPoint/Assembly
```

建议核心模型：

```csharp
internal readonly record struct ModuleId(int Value);

internal sealed record ModuleSyntax(
    ModuleId Id,
    SourceIdentity Source,
    ModuleDeclaration Root,
    ImmutableArray<ModuleId> Dependencies);

internal sealed record ModuleEmitPlan(
    ModuleId Id,
    ImmutableArray<MethodPlan> Methods,
    MethodPlan Initializer,
    ModuleAnalysis Analysis);

internal sealed record ModuleArtifact(
    ModuleId Id,
    MethodInfo Initializer,
    ImmutableArray<MethodInfo> Methods);

internal sealed record CompilationResult(
    ScriptFunctionDelegate EntryPoint,
    Assembly Assembly,
    byte[] PeImage,
    ImmutableArray<CompilationDiagnostic> Diagnostics);
```

AST 可以继续使用现有类模型，但进入缓存或跨线程阶段前必须冻结，不允许 Emitter 回写 AST。

## 5. 分阶段实施方案

### Phase 0：建立多文件基准和阶段计时

新增 Benchmark 数据集：

- `32 x 4 KB` 独立模块。
- `64 x 50 KB` 独立模块。
- 单入口导入 64 个模块的宽依赖图。
- 64 层深依赖链。
- 菱形依赖和大量重复 import。
- 8 个包含语法错误的并行模块。
- Dynamic、OnlyRun、Persistence 三种模式。
- DOP：1、2、4、物理核心数、逻辑核心数。
- 冷文件缓存、热文件缓存、编译缓存命中三种场景。

在 `CompilationSession` 中使用无分配阶段计时器记录：

```text
Discovery / Read / LexParse / Link / Analyze / Lower / Emit / Finalize / Serialize / Load
```

记录 Wall time、CPU time、Allocated bytes、GC、峰值 Working Set、模块吞吐、最终 PE 哈希。

### Phase 1：引入 CompilationSession 和 SourceManager

新增：

- `Compiler/CompilationSession.cs`
- `Compiler/Sources/SourceManager.cs`
- `Compiler/Sources/SourceIdentity.cs`
- `Compiler/Diagnostics/CompilationDiagnostic.cs`

职责：

1. `AuroraEngine` 只负责配置、创建 Session 和原子安装结果。
2. SourceManager 统一路径规范化、编码、文本所有权、内容哈希和文件版本。
3. Full Build 与 IncrementalCompiler 共用 SourceManager 和依赖图构建器。
4. 将 `SearchAllFileSource` 改为 `Directory.EnumerateFiles`，避免先生成完整路径数组。
5. 区分“入口模块编译”和“目录全量编译”，默认只编译入口可达模块。

公开 API 建议：

```csharp
public Task<CompilationResult> CompileAsync(
    IReadOnlyList<ScriptSource> roots,
    CompilationOptions options,
    CancellationToken cancellationToken = default);

public void Install(CompilationResult result);
```

`BuildAsync` 可作为 `CompileAsync + Install` 的薄封装。

### Phase 2：稳定模块图与全局预声明

新增 `DependencyGraph`：

1. 用密集、稳定的 `ModuleId` 替代 `string.GetHashCode()`。
2. 节点按照规范化相对路径排序后分配 ID，保证相同输入产生相同 ID。
3. 对模块名、路径、import alias 和重复边做一次统一验证。
4. 使用 Tarjan SCC 检测循环依赖。
5. 当前语言策略保持“循环依赖是编译错误”；未来若允许循环，只修改 SCC 初始化策略，不改 Parser。
6. 所有模块先完成符号和方法签名预声明，再进入并行 Lowering。

产物为只读 `GlobalCompilationModel`，供所有 Module Lowerer 并发读取。

### Phase 3：拆分 CILEmitter

将当前 `CILEmitter` 拆为：

```text
CILEmitterCoordinator
ModuleLowerer
ModuleEmitter
ModuleEmitContext
DomainInitializerEmitter
```

`ModuleEmitContext` 独占以下状态：

- `CodeScope`
- 当前 IL 或指令 Writer
- locals/upvalues/capture map
- stack manager
- break/continue labels
- constant pool
- closure/constant/declaration analyzers
- 当前模块方法表

Coordinator 只持有不可变模块表和后端接口，不再保存 `_scope`、`_il`、`_currentModule` 等模块局部字段。

Lowering 先生成紧凑 `ModuleEmitPlan`，计划中的操作数使用符号句柄，不直接引用未提交的 metadata token。

为控制内存，不允许同时无限保留 SourceText、完整 AST 和 EmitPlan。采用有界流水线：

```text
Parse queue -> Lower queue -> Emit/Commit queue
```

模块进入下一阶段后，释放上一阶段不再需要的数据。

### Phase 4：Dynamic 后端模块级并行发射

Dynamic 模式优先实现，因为每个 `DynamicMethod` 相互独立：

1. 为每个模块创建独立 `ModuleEmitter`。
2. 模块 initializer 和模块内部函数在独立上下文中生成。
3. 每个任务返回 `ModuleArtifact`，不修改共享 Dictionary。
4. 所有 ModuleArtifact 完成后，按 ModuleId 串行生成 Domain EntryPoint。
5. 运行时模块注册和顶层初始化仍严格按照依赖顺序串行执行。

不允许并行执行脚本模块初始化，因为顶层代码可能具有全局副作用。

### Phase 5：OnlyRun/Persistence 并行 Lowering、串行 Commit

短期可靠方案：

1. 主线程按稳定顺序预定义 TypeBuilder、方法签名和字段句柄。
2. ModuleLowerer 并行生成 `MethodPlan`。
3. 单一 `ReflectionEmitCommitter` 按 ModuleId 将计划写入 ILGenerator。
4. Commit 完成后统一 `CreateType`。

不要通过给 `ModuleBuilder` 加全局锁来实现“并行 Emitter”；这只会增加锁竞争，实际仍是串行。

Persistence 终极方案：

1. 使用 `System.Reflection.Metadata` 直接生成 metadata。
2. 预先分配 Type/Method/Field 逻辑句柄。
3. 各方法体并行编码为独立 Blob。
4. 主线程按确定顺序合并 metadata table 和 method body stream。
5. PDB sequence point 同样使用模块局部 Blob，最终统一合并。

该方案完成后，`PersistedAssemblyBuilder` 可以删除，Persistence 不再依赖 Reflection.Emit。

### Phase 6：增量编译缓存

缓存键：

```text
CompilerVersion
+ LanguageVersion
+ Relevant EngineOptions
+ CanonicalPath
+ SourceContentHash
+ DirectDependencyPublicHash
```

分两级缓存：

- `SyntaxCache`：SourceText -> frozen ModuleSyntax。
- `EmitPlanCache`：ModuleSyntax + dependency public hash -> ModuleEmitPlan。

缓存不直接保存 `ILGenerator`、`TypeBuilder`、`DynamicMethod` 或可变 CodeScope。

缓存策略：

- 内存 LRU，按估算字节数限制容量。
- 可选磁盘缓存，仅保存版本化的紧凑二进制 EmitPlan。
- 文件时间和长度只用于快速否定；命中前仍以内容哈希为准。
- 依赖实现变化但公开符号未变化时，不使调用者的 SyntaxCache 失效。

Hot Patch 改为复用同一缓存和依赖图，只选择受影响模块及反向依赖重新 Lower。

### Phase 7：源码读取与内存生命周期

SourceManager 提供 `SourceText` 所有权：

```csharp
internal sealed class SourceTextOwner : IDisposable
{
    public ReadOnlyMemory<char> Text { get; }
}
```

实施顺序：

1. FileSource 增加异步读取 API，但本地小文件仍允许同步快速路径。
2. 大文件使用 `FileStream`、`SequentialScan` 和池化缓冲区解码。
3. Parser 完成且诊断切片不再依赖全文后，立即释放 SourceTextOwner。
4. 并行度同时受 CPU 上限和 `CompilationMemoryBudget` 控制。
5. SourceText 大小超过阈值时减少并发，避免多个大字符串同时进入 LOH。

多线程优化目标是降低 wall time，不保证降低峰值内存。必须用内存预算限制并行，而不是无界增加 Worker。

### Phase 8：Engine 与全局状态清理

1. `StringValue.ConfigurePooling` 目前是静态全局配置，移动为 Engine/Domain 级策略，避免多个 Engine 相互覆盖。
2. `CompilationResult` 完整构造后再原子替换 Engine 当前版本。
3. Build 失败或取消时保留上一个可运行 EntryPoint。
4. 为安装后的 Assembly 和动态方法建立明确生命周期；可卸载模式使用独立 AssemblyLoadContext。
5. `CreateDomain` 只读取已安装的不可变 EngineImage，不接触编译器对象。

## 6. 并行度配置

最终拆分配置：

```csharp
public int FrontendDegreeOfParallelism { get; init; }
public int LoweringDegreeOfParallelism { get; init; }
public int EmitDegreeOfParallelism { get; init; }
public long CompilationMemoryBudgetBytes { get; init; }
```

默认策略：

- Frontend：逻辑处理器数量与内存预算共同限制。
- Lowering：物理核心数量附近。
- Dynamic Emit：物理核心数量附近。
- Reflection Commit：1。
- Metadata Merge：1。

模块数量少于并行阈值或总源码小于阈值时直接走串行快速路径，避免 Task/Channel 调度成本。

## 7. 确定性和错误模型

必须满足：

1. 相同输入、配置和编译器版本生成相同模块顺序和稳定 PE 哈希，混淆模式除外。
2. 并行诊断最终按规范化路径、行、列、错误码排序。
3. 一个模块失败不会造成队列卡死，也不会让其他已排队诊断丢失。
4. 取消后不发布半成品 CompilationResult。
5. 所有异常路径归还 Lexer token chunk、SourceText buffer 和临时 EmitPlan buffer。
6. 输出文件使用临时文件加原子替换，避免取消或崩溃留下损坏 DLL。

## 8. 验收指标

正确性：

- 现有 Examples 和脚本单元测试全部通过。
- 新增 100 次并行压力测试，无死锁、重复模块或随机缺失模块。
- 循环、重复路径、大小写路径、多错误、取消和输出失败均有测试。
- DOP=1 与 DOP>1 的模块表、诊断和执行结果一致。

性能：

- 单模块 DOP=1 相对当前版本 CPU 回退不超过 3%，分配回退不超过 2%。
- 64 个 50 KB 独立模块，Dynamic 模式在 6 个物理核心上相对 DOP=1 至少 3 倍加速。
- 前端 Lex+Parse 在同场景至少 3.5 倍加速。
- OnlyRun/Persistence 在完成并行 Lowering 后至少 1.8 倍加速。
- 无变更增量 Build 至少 8 倍加速，分配下降至少 70%。
- 并行编译峰值内存不超过配置预算的 110%。

## 9. 实施顺序

严格按以下顺序推进：

1. 多文件 Benchmark、阶段计时和确定性校验。
2. CompilationSession、SourceManager、CompilationResult。
3. 稳定 ModuleId、DependencyGraph、全局预声明。
4. ModuleLowerer、ModuleEmitPlan、ModuleEmitContext。
5. Dynamic 模块级并行发射。
6. OnlyRun/Persistence 并行 Lowering、串行 Commit。
7. 增量缓存和 Hot Patch 前端统一。
8. SourceText 生命周期与内存预算。
9. System.Reflection.Metadata Persistence 后端。
10. 删除旧 ScriptCompiler/IncrementalCompiler/CILEmitter 中被替代的路径和兼容代码。

每个阶段都必须先记录 before 数据，再实施，再运行功能回归、压力测试和 Benchmark。未达到验收指标时不能进入下一阶段。

