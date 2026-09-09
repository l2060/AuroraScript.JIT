# 编译器瘦身与重复分析优化实施方案

本方案以减少重复计算和重复生产代码为目标。每一步都必须交付可对比的数据、删除清单和语义验证结果；不能以新增一套管线、把代码移动到其他文件或降低推导能力作为优化结果。

基线提交：`eed7b28a295c0e274cbcf9bb5629918decee0926`。审计日期：2026-09-09。

本文是实施方案；本次只交付文档和测量数据，尚未实施下列编译器修改。文中的次数上限是验收要求，耗时百分比是筛选收益的门槛，均不代表已经取得的优化收益。

## 1. 已核实的基线和问题

### 1.1 生产源码体量

统计 `src/Compiler` 下的 C# 文件；物理行包含空行和注释，非空行排除空白行但包含注释。partial 文件合并统计，测试、文档和 benchmark 不计入生产源码。

| 范围 | 文件数 | 物理行 | 非空行 |
| --- | ---: | ---: | ---: |
| Compiler | 136 | 39,375 | 35,805 |
| Backend | 46 | 26,362 | 24,463 |
| TypedCilEmitter，含 partial | 4 | 9,858 | 9,296 |
| TypedFunctionBuilder，含 partial | 2 | 5,580 | 5,294 |

后两个类合计 15,438 物理行，占 Backend 的 58.6%。源码瘦身优先从这里删除重复规则和扫描入口。

### 1.2 完整编译实测

使用现有 [Examples 编译探针](../examples/Program.cs) 的 `--compile-benchmark`，Release、Persistence、HotReload 开启、ModuleConstInlining 开启、自动选择编译并行度。包含完整 BuildAsync、PE 序列化、DLL 写入和程序集加载，不执行脚本。每个新进程执行 16 次完整构建，没有以增量构建代替完整构建。

环境：Windows 11 企业版 10.0.26100，Xeon W-2235（6 核 / 12 逻辑处理器），SDK 10.0.400，.NET runtime 10.0.11，x64。磁盘输入为 17 个 `.as` 文件、63,596 字节；`seed.as` 实际使用宿主的 memory overlay。脚本及宿主文件的 SHA-256 见 [输入清单](compiler-slimming-baseline-manifest.csv)。overlay 的源码是：

```javascript
console.log('load from memory overlay'); export func go(){ console.log('seed from memory...');  }
```

5 个独立进程共 80 次完整构建；每进程 Run 8–15 提供 8 个预热样本，共 40 个。原始结果见 [完整编译 CSV](compiler-slimming-baseline.csv)。

| 进程 | Run 0 耗时 ms | 预热耗时中位数 ms | 预热分配中位数 bytes |
| --- | ---: | ---: | ---: |
| 1 | 2,386.459 | 1,419.924 | 27,827,092 |
| 2 | 2,981.148 | 1,532.852 | 27,822,816 |
| 3 | 2,404.649 | 1,557.803 | 27,828,316 |
| 4 | 2,835.022 | 1,425.856 | 27,825,736 |
| 5 | 2,987.331 | 1,443.325 | 27,825,492 |

主要基线采用“每进程预热中位数的中位数”：**1,443.325 ms / 27,825,736 bytes**。首次构建耗时的进程间中位数为 **2,835.022 ms**。作为辅助口径，合并 40 个预热样本的中位数是 1,483.633 ms / 27,826,984 bytes，两种口径不能混用。

预热进程中位数范围为 1,419.924–1,557.803 ms，全部预热样本范围为 1,250.876–1,972.257 ms。这次未固定 CPU affinity，未控制系统其他后台活动；没有并发运行本任务的构建、测试或其他 benchmark。该数据提供当前规模和分配基线，不能据此判断几个百分点的改进，更不能与仓库旧报告直接相减得出本轮回退。候选实现需要按第 3 节重新做相邻 A/B 对照。

本次 Examples Release 构建成功，0 警告、0 错误，80 次编译均成功；没有运行脚本语义测试或采集带诊断补丁的 AST/Analyze 次数。它们在后续步骤中仍是必需的验收项目。

### 1.3 重复工作的位置

| 现象 | 源码证据 | 可确定的结论 |
| --- | --- | --- |
| 名称绑定执行两遍 | [CallableReturnPredictions](../src/Compiler/Backend/Code/CallableReturnPredictions.cs) 构造函数；[TypedModuleCode](../src/Compiler/Backend/Code/TypedModuleCode.cs) 的 Build | 正常单次发射同时进入预测和模块分析；两者分别 BindModule 和 Bind 初始化函数 |
| 预测重新完整分析所有函数 | CallableReturnPredictions.Build | 每轮调用 TypedFunctionBuilder.Analyze，并再次扫描返回值和捕获变量；保存完整 TypedFunctionCode |
| 没有资格发射的 direct 副本仍被分析 | TypedModuleCode.Build / AnalyzeIndependentFunctions；[TypedCilEmitter](../src/Compiler/Backend/Emission/TypedCilEmitter.cs) 的 PrepareDirectMethods | 模块分析遍历所有函数，发射时才筛选 IsDirectCallCandidate |
| 多层固定点全量遍历 | TypedModuleCode.Build；[TypedFunctionBuilder](../src/Compiler/Backend/Code/TypedFunctionBuilder.cs) 的 Analyze / AnalyzeToFixedPoint | 模块每轮重新分析函数；函数内部反复扫描；存储类型改变后再跑类型固定点 |
| 闭包结构重复计算 | [CapturedCellTypes](../src/Compiler/Backend/Code/CapturedCellTypes.cs) 的 Analyze | 每轮重建函数索引、扫描写入和提前捕获、追溯根 cell |
| 重载选择实现和执行重复 | TypedFunctionBuilder.TryGetHostExport；TypedCilEmitter.TryGetHostExportCall；[HostExportArgumentFacts](../src/Compiler/Backend/Code/HostExportArgumentFacts.cs) | 已有公共选择器，但分析器仍自行遍历和评分；发射再次选择 |
| 数组元数据和操作流程重复 | TypedCilEmitter.GetPacked* / EmitPacked*Mutation / EmitPacked*Compound | 同一组数组类型在多个 switch 重复列举，读写操作也存在相同流程 |
| 模块数组含全局编号造成的空槽 | [CompileSession](../src/Compiler/Backend/CompileSession.cs) 的 AllocateFunctionId；TypedModuleCode.Build；TypedFunctionBuilder.BindModule；TypedCilEmitter 构造函数 | 每个模块按全局最大 FunctionId 分配，空间随模块排列及历史编号增大 |
| guard 缺少收益筛选 | [TypedCilEmitter.GuardedExpressions](../src/Compiler/Backend/Emission/TypedCilEmitter.GuardedExpressions.cs) 的 TryEmitGuardedExpression | 保存直接操作数，分别发射快速和回退操作；未先确认两条路径是否不同 |
| import 读取与解析读取重复 | [ScriptCompiler](../src/Compiler/ScriptCompiler.cs) 的 ResolveImportsAsync / BuildSyntaxTreeAsync；[IncrementalCompiler](../src/Compiler/IncrementalCompiler.cs) | 调用 ReadSource 和全局文件分类存在重复；底层是否实际重复磁盘 I/O 取决于 resolver |

解析、绑定、常量处理、闭包规划各有语义职责，不能仅因它们都遍历 AST 就认定可以删除。只有输入不变、计算相同或没有消费方的工作才属于本方案的去重对象。

## 2. 防止继续膨胀的交付规则

1. **每个可合并批次的生产代码净增长必须 ≤ 0 非空行。** 同时报告物理行和非空行，累计 Compiler 不得超过 39,375 / 35,805，Backend 不得超过 26,362 / 24,463。增加缓存字段或队列所需代码必须由同一批次删除的重复实现抵消，不能把“下一步再删”留作长期欠账。
2. 新增生产源码文件数默认是 0；优先修改现有 FunctionBinding、FunctionPlan、TypedFunctionCode、EmissionSession 和现有静态辅助类。新增嵌套数据结构只有在现有结构无法表达且能同时删除旧结构时才采用。
3. 不新增通用 Pass 框架、服务注册、策略接口层级、全新 IR、全局缓存系统或代码生成项目。按函数维护少量事实和待处理队列即可。
4. 文件移动、拆 partial、压缩多条语句到一行、删除解释语义的注释不计作瘦身。迁移到 Compiler 外的生产代码仍计入本任务净增长；不得把重复展开转移到生成器来绕过统计。
5. 每批都写明“新增什么、删除什么、哪些旧入口不再存在”。缓存引入后必须删除原来的重复计算入口，不能长期双写两套结果。
6. **最终源码减少量必须 > 0；第一批以净减至少 300 非空行为交付目标。** 300 是工程收敛目标，不是当前已证明可删除的行数。若实际删除不足，明确报告差额并继续精简；不能宣称“完成瘦身”。
7. 不以全部退回 Dynamic、禁用普通返回推导、去掉 native 特化、降低迭代上限、删除回退分支来通过性能门槛。

单步需要暂时增加代码时，可以在本地完成相互依赖的修改后一起验收；若对应删除无法完成，就缩小该步范围，不能单独合入增长版本。

## 3. 如何量化和判断收益

### 3.1 指标及口径

下表是诊断输出的指标定义，不要求为每个指标添加生产字段或新类型。

| 指标 | 如何采集 | 用途 |
| --- | --- | --- |
| BindCount | 在 TypedFunctionBuilder 实际创建 NameBinder 的入口计数，按函数/初始化器区分 | 验证一函数一绑定，不能只数 BindModule 调用 |
| AnalyzeCount | 在 TypedFunctionBuilder.Analyze 入口按函数及 generic/direct/prediction 来源计数 | 分清删掉哪一类完整分析 |
| BodyVisitCount | 在相关遍历器的节点访问入口计数，按分析类别分组 | 避免 Analyze 次数降低但内部扫描增多 |
| StaticScanCount | 分别记录闭包写入、根 cell、控制流结构和初始 null 观察扫描 | 验证静态摘要真正只计算一次 |
| OverloadSelectCount | 记录分析/普通发射/guard 分支的候选枚举次数 | 验证复用结果及失效条件 |
| InputRevision / SummaryChangeCount | 每个函数输入事实改变时递增修订号；记录输出摘要真正改变的次数 | 区分必要重算与无效重算 |
| FunctionSlots / LiveFunctions | 汇总相关数组槽位数与实际函数数 | 验证多模块空间复杂度 |
| AllocatedBytes | 完整构建前后 GC.GetTotalAllocatedBytes(true) 的差值 | 包含 worker 和 async 线程；不用 CurrentThread 代替 |
| WarmTime / FirstBuildTime | Stopwatch；预热样本和新进程首次构建分开 | 不能混用冷启动、预热和增量耗时 |
| ILBytes / LocalCount / GuardCount | Persistence 输出用 PEReader 读取方法体、局部签名及已记录 guard 点 | 对比生成代码体积，包含初始化器、generic、direct 和 wrapper |
| SourcePhysical / SourceNonblank | 按 1.1 的固定口径统计；另附 git diff --numstat | 防止只减少扫描却持续增加源码 |

准确计数放在一次性诊断补丁或现有 benchmark 内；诊断产物随报告保存，计数补丁不进入生产交付。诊断版本用于验证次数，无计数版本用于耗时和分配对比。不能把逐节点计数、并发原子操作的开销算作编译器性能。

### 3.2 对照方法

1. 固定基线及候选提交、SDK/runtime、宿主程序集、脚本内容、优化选项、并行度和 resolver 顺序。使用独立输出目录保存完整的运行依赖；内部 API 改动时两侧各自构建对应的 benchmark，不能混装 DLL。
2. 每轮先完成构建和正确性检查，再测性能；测量期间不并发运行其他构建、测试或另一个 benchmark。
3. 完整构建至少做 5 对独立进程，A/B 顺序交替。每进程执行 16 次，Run 0 单列为“引擎初始化后的首次构建”，Run 8–15 用于预热比较；它不包含进程启动时间。
4. 主结果报告每个进程预热中位数，再报告 5 个进程的中位数和范围；合并的 40 个预热样本可附列，不能当成 40 个独立进程。保留 CSV 原始值。
5. 相对变化使用 `(候选 - 基线) / 基线 × 100%`，时间、分配和 IL 均以负数为减少。阶段时间只用于归因，不能与完整构建时间相加。
6. 对无 IL 改动的批次，比较方法签名、调用目标、locals、异常区域及规范化后的 IL。不要要求 DLL 整体哈希相同：MVID、元数据 token 和路径散列可能改变。
7. 对改变 IL 的批次，另测脚本运行时间和分配；编译更快不能掩盖执行变慢。

### 3.3 验收门槛

| 项目 | 硬条件或处理方式 |
| --- | --- |
| 语义 | 相关回归全部通过；最终适用框架/编译模式全套通过；已有失败必须单列，不能默认为本次通过 |
| 源码 | 满足第 2 节净增长约束；目标函数不存在被替代的旧扫描/旧选择实现 |
| 次数 | 满足每一步的确定性计数目标；不能只报告平均耗时 |
| 无 IL 改动的批次 | 签名、规范化 IL、局部变量和异常处理保持等价；异常差异必须逐项解释 |
| 耗时回退 | 任一主要样本预热时间增加超过 5%，且在至少 4/5 对进程中同方向出现，视为回退；在独立重测中仍出现则该批不通过 |
| 分配回退 | 完整构建分配中位数增加超过 2%，先定位；重复可复现且无相应删除方案则不通过 |
| 新增复杂度的收益 | 引入队列、额外缓存等结构，需同时达到对应次数目标，并在目标样本获得至少 5% 时间减少或 10% 分配减少；达不到就采用更小的删除方案 |
| 纯删除重构 | 若源码和重复次数确定减少、性能无可重复回退，可保留；小于测量波动的变化标为“未证实加速” |

5%、10%、2% 是本项目用于取舍实现成本的门槛，不是收益承诺，也不是统计显著性的替代物。结果接近门槛或波动大时增加独立进程样本，不能挑选最快的一次作为结论。

## 4. 样本矩阵

优先复用 [CompilerPipelineBenchmarks](../benchmark/CompilerPipelineBenchmarks.cs)、[CompilerPerformanceRegressionTests](../tests/AuroraScript.Tests/CompilerPerformanceRegressionTests.cs) 和现有语义测试；必要的新样本在现有 benchmark 类内参数化生成，不新增测试框架或项目。

| 编号 | 输入 | 测量目的与预期结构性质 |
| --- | --- | --- |
| W0 | 当前 Examples 全部脚本及 memory overlay | 真实完整构建；本次已采集基线 |
| W1 | 同一模块 100/500/1000 个相互独立的普通函数，零 native，零捕获，初始化器不调用函数 | D=0；direct Analyze 为 0；每个最终输入下 generic 仅分析一次 |
| W2 | 单模块长度 10/50/200 的调用链；普通和 native 两组 | 区分预测和确定返回传播；改变函数声明顺序后结果和有效特化一致 |
| W3 | 2/8 个互递归函数，外加 500 个独立函数 | 只有递归相关函数继续迭代；外部 500 个函数不能每轮重算 |
| W4 | 总函数数固定 1000，分成 1/10/100 个模块 | 数组槽位总数随 F 而非 M×F 增长；函数分配顺序不影响结果 |
| W5 | 100 个闭包，覆盖只读、写入、提前捕获和多层转捕获 | 稳定性/根归属只计算一次；写入与初始化顺序语义不变 |
| W6 | 同一宿主对象的唯一匹配、重载歧义、spread、额外/默认参数 | 分析器和发射器选择一致；普通发射不重新枚举重载 |
| W7 | 普通函数返回 primitive/native object/packed array；含顶层调用、跨模块别名及运行时替换 | 保留返回推导；实际类型不符时回退；副作用只执行一次 |
| W8 | Int32/UInt32/Int64/UInt64/Number 数组复合赋值、自增、自减 | 验证整数边界、符号、截断、溢出、prefix/postfix 及求值顺序 |
| W9 | 20 个模块导入同一个依赖；memory 覆盖同名 file；下一轮修改源码 | 单轮执行模块文本读取去重，跨构建不缓存旧文本 |
| W10 | 现有 FullCompile_RealAstar / FullCompile_RealMd5 | 验证数值、数组及复杂控制流上的编译和执行性能 |

W1–W9 是待补充或组合的受控样本，不代表现有仓库已经提供同名 benchmark 方法。长度只限定输入规模；不预设真实耗时按某个百分比改善。

## 5. 分步实施

### 步骤 0：固定口径，补齐现有测量入口

**怎么做：**

1. 保存本基线及脚本清单，先运行已有编译/运行正确性样本。
2. 在独立诊断补丁中采集第 3 节次数；不得只从 passLimit 推算实际遍历次数。
3. 修正 ExampleCompilationProfile 的阶段口径：它目前对每模块单独调用 TypedModuleCode.Build，未传入整次编译共用的 CallableReturnPredictions。新增预测后，这个探针不能准确复现正式发射的跨模块预测流程。诊断应先按正式流程建立一次 session 预测，单列其开销，再把相同实例传给各模块。
4. EmitOnly_ParsedLargeModule 实际还包含绑定/计划和类型分析；报告中写成“排除解析的后端构建”，不能称作纯 IL 发射。

**验收：** 计数覆盖 generic/direct/prediction 三种来源和初始化器；同一输入两次诊断的结构计数一致，或能解释并行/调度导致的差异。生产提交不保留计数补丁。本步骤不宣称优化收益。

### 步骤 1：先删除重复的类型规则和重载选择

**修改位置：** TypedFunctionBuilder.TryGetHostExport、HostExportArgumentFacts、TypedCilEmitter.GetPacked*，必要时现有 TypedRuntimeMetadata。

**怎么做：**

1. 把分析器中的重载遍历、转换评分、歧义判定移到现有 TrySelectOverload 入口；先逐项对齐 spread、默认/额外参数、原生对象 CLR 类型及 context/this 参数偏移。
2. 分析器和发射器共同调用该入口；删除分析器的旧候选循环。此步先统一规则，结果缓存留到步骤 6。
3. 把数组包装类型、原始数组类型、栈类型和固定元数据集中维护。优先在现有类型辅助类中使用一份静态映射；避免为每种数组创建策略类或委托链。
4. 只合并真正相同的映射和流程；有符号除法、移位、64 位整数及浮点转换继续保留明确分支。结果必须保持生成 IL 等价。

**删除目标：** 手写宿主重载候选循环只剩 1 份；同一数组的固定元数据定义只剩 1 个来源。旧 switch 随替换一起删除，不保留兼容转发层。

**量化验收：** 非空行净减少；W6/W8 语义、调用目标和规范化 IL 等价；运行性能无回退。若通用映射引入查表成本而没有足够删除收益，缩小到最明显的重复项。

### 步骤 2：同一编译只绑定一次

**修改位置：** FunctionBinding、EmissionSession、TypedModuleCode.Build、CallableReturnPredictions 构造函数、TypedFunctionBuilder.BindModule。

**怎么做：**

1. 在常量处理、函数注册、名称/闭包规划和初始化器构造完成后，建立本次编译的绑定结果。不能跨 AST 改写阶段缓存。
2. 由现有 EmissionSession 持有并传入两条分析路径；使用现有 FunctionBinding，不新增 BindingService/缓存接口。
3. 每模块只建立一次 directFunctions 名称映射，同时供普通函数和初始化器绑定使用。
4. 单独的 TypedModuleCode.Build 测试入口在未收到绑定时局部创建一次，再传给其预测入口；避免另开一条隐式重复绑定路径。
5. 绑定字典只读共享；每次类型分析的可变局部状态仍由该次分析持有。一次构建结束即释放，不进入全局静态缓存。

**量化验收：** 令 F 为模块函数总数，M 为初始化器数，正常单 session 发射的绑定入口次数由 `2(F+M)` 降至 `F+M`，每个函数/初始化器恰好 1 次；directFunctions 映射构建由常规的 `4M` 降至 `M`。这是目标入口次数，不代表完整编译快一倍。

**删除目标：** 预测构造函数和模块 Build 不再各自建立同一绑定；新增传参及持有代码必须由旧重复逻辑抵消。W0/W2/W5/W7 通过，生成 IL 等价。

### 步骤 3：只分析有用途的 direct 副本

**修改位置：** TypedModuleCode.Build、AnalyzeIndependentFunctions、CollectNativeParameterDemands、CollectParameterEvidence。

**怎么做：**

1. 使用规划阶段已确定的 IsDirectCallCandidate 及不依赖类型结果的资格条件筛选。不能把“尚未推导出返回类型”当作永远不具备资格。
2. 对非候选函数不创建 direct 参数、direct TypedFunctionCode，不进入 direct 参数验证与重分析；无候选模块直接跳过相应 direct 调度。
3. 参数证据仍从所有实际可执行的 generic 调用点收集；只删除未执行 direct 副本贡献的证据。generic 与有效 direct 的证据不可混为一份，特别注意 coercion-only 与 exact 参数的区别。
4. 返回读取和 fallback 入口适配空 direct 结果；普通函数的返回提示继续走普通返回预测。

**量化验收：** `AnalyzeCount(direct, 非候选)=0`；D=0 的 W1 中 direct 分析和 direct 参数验证次数均为 0。对于其余模式输入真正相同的结果才允许复用，不能仅看 ReturnType 相同。

**删除目标：** 去掉非候选路径的创建、验证和兜底重分析。W1/W2/W6/W7 通过；有效 native 方法数和签名不得减少；已有普通返回、顶层和 void 行为不变。

### 步骤 4：把静态扫描移出类型固定点

**修改位置：** CapturedCellTypes、FunctionBinding/FunctionPlan、TypedFunctionBuilder.Analyze、TypedCilEmitter.EmitMethodBody。

**怎么做：**

1. 一次收集函数调用节点、return 节点、变量写入位置和局部声明信息；优先扩充现有绑定过程，不再增加同等规模的“摘要扫描器”。
2. CapturedCellTypes 将函数索引、根 cell 归属、写入/提前捕获稳定性分离出来保存一次。每轮只读取受影响 owner 的初始化表达式类型，并更新对应闭包类型。
3. 缓存静态控制流特征：是否含保护区域、finally return，以及各循环自己的 finally transfer。必须按循环节点区分，不能用一个函数级布尔值代替。
4. 初始 null 观察结果只有在绑定、声明顺序和捕获信息不变时共享。数值定义、coercion、参数缓存和参数缓冲区需求中依赖类型/调用方案的部分仍在必要时更新。
5. return 节点列表可复用，但顺序返回分析、隐式返回和 try/finally 的流语义继续保留。不能把两个名字相似但语义不同的扫描直接互相替换。

**量化验收：** 每函数静态闭包扫描至多 1 次、每个 upvalue 根归属至多解析 1 次；模块索引至多构建 1 次。固定点增加迭代轮数时，这些计数不再增长。每函数/循环的静态异常结构标记只计算 1 次。

**删除目标：** CapturedCellTypes.Analyze 中每轮重建索引和 CellScanner 的入口消失；发射中被摘要替代的递归 Contains* 实现删除。不能只在前面加缓存、把全部旧调用仍留着。

**回归：** W5、try/catch/finally、break/continue、隐式 null return、提前捕获；IL、locals 和异常区域等价。

### 步骤 5：只重算输入发生变化的函数

**前置条件：** 步骤 2–4 完成，计数能区分静态扫描和类型重算。

**修改位置：** TypedModuleCode 的模块循环、CallableReturnPredictions.Build 的预测循环及已有调用点摘要。

**怎么做：**

1. 从已收集调用点和捕获归属建立反向依赖：callee 返回变化通知 caller，caller 参数证据变化通知对应 callee，owner cell 类型变化通知闭包；初始化器作为同等分析单元。
2. 先做最小版本：保留现有固定点顺序，在输入未变化时跳过 Analyze。输入必须包括本函数相关参数/返回摘要、捕获类型和分析模式，不能只比较自身 ReturnType。
3. 若 W2/W3 仍显示无效整表遍历，再在现有调度处使用 `Queue<FunctionId>` 和去重标记；不建立通用调度器。generic/direct/prediction 事实保持各自语义，不能共用一个类型槽。
4. 只在输出摘要确实变化时通知依赖者。摘要包含 flow/native/structural 类型及会影响参数判定的证据，不能漏掉 CLR 对象类型改变。
5. 每调用点保留上一版参数贡献；重算时替换贡献并重新汇总受影响 callee。现有 transient Dynamic/coercion 证据可以撤销，不能通过永远 OR 合并旧类型实现“单调化”。
6. 静态可解析的别名/跨模块调用记录真实依赖；无法稳定解析的调用继续保守处理。重绑定后的运行值仍由 guard 检查，依赖图不是不可变函数身份的证明。
7. 保留收敛保护和保守回退。使用有界工作量防止队列不终止，但不能因调度方式改变而过早耗尽预算；须对比原管线的推导能力和 recursive native ABI。

**量化验收：** 每个 `(函数, 模式, 输入修订号)` 最多启动 1 次完整 Analyze，函数内部必要固定点另计。W3 中无依赖的 500 个函数不随递归轮数重算；W1 的无依赖 generic 每函数为 1 次。W2 声明顺序变化和 W3 递归均保持返回事实、有效特化和执行结果。

**删除目标：** 跳过缓存生效后删除无条件全量 Analyze 路径；队列完成替换后删除旧全量驱动循环。先只收拢重复调度代码，不强行一次合并所有分析状态；若净代码增长或目标收益不足，停留在“输入未变则跳过”的版本。

**限制：** 此步不引入基本块 IR，也不重写整个函数内部流分析。后者只有在实测仍占主要耗时且有独立方案时才处理。

### 步骤 6：发射消费最终调用结果，不重复选择成员

**修改位置：** TypedFunctionCode、TypedFunctionBuilder 的宿主/native 调用分析、TypedCilEmitter 及 NativeObjects partial。

**怎么做：**

1. 复用已有 `_nativeValueCalls` 的模式，记录最终调用目标以及“已分析但必须动态调用”的结果。目标为 null 与尚未计算要能区分，避免失败调用不断重试。
2. 类型事实改变时更新对应调用记录；稳定后冻结给发射使用。仅缓存 descriptor 已足够时不再创建完整 CallPlan 类型。
3. 参数缓存/缓冲区需求判断和发射读取同一结果，去掉各自重复解析 owner、枚举 overload 和检查参数的流程。
4. guard 下调用方案以该分支实际保护的事实计算一次；普通分支结果不能直接用于类型已经改变的 guard 分支。共享选择规则，不共享不成立的假设。

**量化验收：** 普通稳定发射阶段的 OverloadSelectCount=0；同一个 guard 分支和固定输入下至多选择 1 次；没有可用匹配时仍不重复枚举。W6/W7 的歧义、spread、额外参数及替换回退一致。

**删除目标：** emitter 中已由结果记录替代的候选查找逻辑删除；只保留加载接收者/参数、转换和发射的职责。净生产代码减少，不能追加一套 resolver。

### 步骤 7：缩小预测的计算范围和保留结果

**前置条件：** 步骤 2、4、5 完成，先消除机械重复，再考虑减少预测工作。

**怎么做：**

1. 先调整入口顺序：当前 TypedCilEmitter.Prepare 在模块 Build 前取用惰性 CallableReturns，此时还没有可复用的 generic 结果。改为在现有 session 中先建立各模块 generic/direct 结果，再计算预测并统一 Apply，最后发射函数体。复用现有“所有模块 Prepare 后再 Emit”的顺序，不增加另一套编译管线。
2. 删除 TypedModuleCode.Build 末尾自动启动全量预测的副作用，避免新顺序下再次预测。需要预测的独立测试入口显式调用同一准备逻辑；不能让测试路径和正式路径长期采用不同的预测算法。
3. 对没有依赖普通返回提示的函数，先尝试由已完成的 generic 分析提取返回摘要。Int32/UInt32 存储精化按普通 Datum ABI 恢复 Number；native/structural 摘要使用已有判定。
4. 从确实消费动态调用结果的操作反向标记所需函数及其传递依赖；跨模块别名、闭包和递归都属于依赖。仅凭没有显式 return 注解不能跳过函数；仅凭本函数没有 guard 也不能跳过其被 caller 使用的返回摘要。
5. 对需要预测的函数继续复用现有表达式/流推导，不新增一份轻量解释器。只在相关输入改变时执行预测；不依赖提示的函数不得再因全程序轮次重复预测。
6. 收敛后只保留消费端需要的表达式 flow/native 类型差异和返回摘要，释放完整预测 TypedFunctionCode 中不使用的 locals、循环及存储结果。先列出所有消费点再裁剪字段。
7. 预测不参与确定类型的存储、ABI 或无 guard 的直调证明；未收敛时保留原有动态行为。

**量化验收：** 不需要预测且可从 generic 得到摘要的函数，额外 prediction Analyze=0；无预测差异的函数不保留第二份完整 TypedFunctionCode。统计预测覆盖函数数、完整副本数及预测阶段分配，不能只比较 `_returns` 数组大小。

**删除目标：** 全函数无条件预测和不被消费的完整结果持有路径删除；不同时保留“旧预测”和“新预测”两套实现。若按需分析的依赖维护过于复杂或不能达成第 3 节收益门槛，先只做 generic 摘要复用与结果裁剪。

**回归：** 完整 DynamicFunctionReturnInferenceTests，尤其顶层消费、跨模块链、递归、局部别名、普通 void、隐式 return 和运行时替换。

### 步骤 8：消除模块数组里的全局编号空洞

**修改位置：** ModulePlan/FunctionPlan 的现有索引信息，TypedModuleCode、TypedFunctionBuilder.BindModule、TypedCilEmitter 的数组访问。

**怎么做：**

1. 保留全局 FunctionId 对外/跨模块身份，模块内部数组按 module.Functions 的紧凑下标分配。
2. 优先复用或扩充已有函数索引；若必须建立 ID→下标表，每模块只建 1 份，绑定/分析/发射共享。不能分别增加三张映射表。
3. initializer 继续独立保存。函数注册完成后固定索引；测试非连续 ID、嵌套函数以及模块顺序变化。
4. 全 session 的预测表若本来按全局 ID 稠密存储，可继续使用；不把所有数组都改成 Dictionary。

**量化验收：** 每张模块函数数组长度等于该模块 Fm；同一类数组跨模块总槽位 `ΣFm=F`，不再为 `Σ(maxIdm+1)`。W4 总函数数不变时，相关数组槽位不随模块数乘法增长；新增映射内存一并计入。

例如，100 个模块各 10 个函数、编号恰好连续分块且从 0 开始时，旧方案单类数组槽位是 `10×(1+…+100)=50,500`，紧凑方案是 1,000，减少 98.0%。这只是明确前提下的槽位算例，不是当前真实项目的内存降幅；实际 initializer/嵌套编号需用 W4 实测。

**删除目标：** 模块级 maxId 扫描及相应稀疏容量计算消失；不得引入新的通用 ID 容器。W4 与多模块/嵌套函数回归通过，IL 等价。

### 步骤 9：有明确收益才生成 guard，复用数组读写骨架

**修改位置：** TypedCilEmitter.GuardedExpressions、TypedCilEmitter 的调用、数组复合赋值和自增发射。

**怎么做：**

1. 在保存操作数和发射分支之前，利用步骤 6 的调用选择结果及已有运算分类判定：保护后是否真的去掉动态调用或转换。不能通过先发射两份 IL 再比较来判断收益。
2. 两条路径实际上相同则直接走现有路径，不生成检查、临时变量和分支。判定使用少量私有方法，不新增通用成本模型或 IL 回滚缓冲区。
3. 保留所有保证求值顺序及跨分支复用所必需的 operand 缓存。仅在可证明无副作用且重复加载等价时省略缓存；失败回退不能重新调用函数、getter 或参数表达式。
4. 复用数组接收者/索引只求值一次、保存旧值、写回和 prefix/postfix 选值的通用骨架；数值操作/转换仍按语义选择具体指令。
5. 先测量再决定是否还需函数级 guard 数量上限；本轮不默认增加配置项或任意截断有效特化。

**量化验收：** 人工构造“保护前后相同”的 W7 子例中 GuardCount=0，新增临时变量=0，IL 不大于无 guard 的动态路径。真实有收益子例保留特化，并分别验证命中/失败的运行耗时和分配。相对基线同一批 W7 的 IL 总量必须减少；不能通过增加其他函数 IL 隐藏体积增长。

**回归：** 副作用计数每个 operand 恰好 1；短路、重绑定、native object/数组替换、异常求值顺序一致。W8 覆盖 int/uint/long/ulong 边界、NaN/负零涉及的 Number 行为及写回转换。

### 步骤 10：按实测收益处理源码重复读取

**实施条件：** W9 显示重复 ReadSource 或分类确实存在，且其成本值得处理。

**怎么做：**

1. 在已有单次构建上下文中，复用 import 检查时取得的已解析 ScriptSource 文本及 global 分类结果；解析时传入同一文本。
2. 缓存键使用 resolver 已确定的源身份和本次构建边界，不自行强制小写路径或把不同 resolver 中的同名源码合并。memory overlay 的选择仍由 resolver 完成。
3. 多个 importer 指向相同已解析源时复用结果；并发任务先合并，再读取。采用当前图构建的任务/源登记机制，不另建一个缓存服务。
4. 下一轮完整或增量构建重新读取变更源。全项目 global 发现的预读与执行模块正文读取分开计数；不要宣称所有文件物理读取都能降到一次。

**量化验收：** 对已解析并进入模块图的同一源，单轮正文 ReadSource≤1；W9 跨轮修改立即可见，20 个 importer 不导致 20 次同源正文读取。现有 ExplicitSourceIsReadOncePerBuildWithoutCachingAcrossBuilds / IncrementalParsingReadsSourceOncePerBuild 继续通过。

**删除目标：** import/parse 两处正文重复读取和分类合为一个已登记结果的消费；若实现增长或收益不足，记录为已定位但延期，不能为此引入长期缓存层。

## 6. 批次顺序与停止条件

| 批次 | 包含步骤 | 交付重点 |
| --- | --- | --- |
| A | 0、1、2、3 | 统一规则、绑定一次、删除无用 direct；优先兑现源码净减少 |
| B | 4、5 | 静态摘要复用、输入变化才重算；严格控制缓存/队列代码成本 |
| C | 6、7 | 调用结果复用、预测按需计算及保留；普通返回推导能力不退化 |
| D | 8、9 | 多模块空间与生成 IL 瘦身；增加运行性能验收 |
| E | 10 | 只有 W9 数据支持时才做；不阻塞后端瘦身交付 |

每批独立提交并比较其父版本和固定基线。语义不符先修复；代码净增长先精简；性能回退先定位。若某个高级优化达不到收益门槛，保留已经完成的简单删除，不继续用新抽象补救它。

## 7. 可执行验证入口

以下命令均从仓库根目录执行，完成构建/测试后再单独运行性能测量。

```powershell
dotnet build examples/Examples.csproj -c Release --nologo
dotnet build benchmark/Benchmark.csproj -c Release --nologo
dotnet test tests/AuroraScript.Tests/AuroraScript.Tests.csproj -c Release -f net8.0
dotnet test tests/AuroraScript.Tests/AuroraScript.Tests.csproj -c Release -f net9.0
dotnet test tests/AuroraScript.Tests/AuroraScript.Tests.csproj -c Release -f net10.0
```

日常按修改范围筛选 DynamicFunctionReturnInferenceTests、CompilerBackendPlanTests、CompilerPerformanceRegressionTests、NativeFunctionTests、NativeObjectDirectCallTests、IntegerSpecializationTests、PackedArrayTests、VoidReturnTypeTests、CustomSourceResolverUsageTests。最终批次运行上面的全套；Persistence 只覆盖支持该模式的框架。LanguageServices/LanguageServer 仅在修改共享语法、类型契约或项目引用时增加对应套件。

现有 BenchmarkDotNet 入口可复测四个完整编译样本：

```powershell
dotnet benchmark/bin/Release/net10.0/Benchmark.dll --filter '*CompilerPipelineBenchmarks.FullCompile*' --warmupCount 6 --iterationCount 15 --iterationTime 300
```

该入口目前使用 Dynamic、内存/文件样本，不等同于 Examples 的 Persistence 基线；分别报告结果。`--compare` 是快速定位探针，不作为最终 A/B 结论。

复现本次 Examples 五进程测量，可使用以下脚本。输出 DLL 写入新临时目录，避免覆盖工作区已有 `123.dll`；CSV 写入该临时目录，验收后再选择归档位置。

```powershell
$probeAssembly = (Resolve-Path 'examples/bin/Release/net10.0/Examples.dll').Path
$probeDirectory = Join-Path ([IO.Path]::GetTempPath()) ('aurora-compile-' + [guid]::NewGuid().ToString('N'))
New-Item -ItemType Directory -Path $probeDirectory | Out-Null
$probeRows = [Collections.Generic.List[string]]::new()
$probeRows.Add('Process,Run,ElapsedMs,AllocatedBytes,Gen0,Gen1,Gen2')
Push-Location -LiteralPath $probeDirectory
try {
    for ($probeProcess = 1; $probeProcess -le 5; $probeProcess++) {
        $probeOutput = @(& dotnet $probeAssembly --compile-benchmark 2>&1)
        if ($LASTEXITCODE -ne 0) { throw ($probeOutput -join [Environment]::NewLine) }
        $probeSamples = @($probeOutput | ForEach-Object { $_.ToString() } |
            Where-Object { $_ -match '^\d+,\d+(\.\d+)?,\d+,\d+,\d+,\d+$' })
        if ($probeSamples.Count -ne 16) { throw 'Expected 16 complete build samples.' }
        foreach ($probeSample in $probeSamples) { $probeRows.Add("$probeProcess,$probeSample") }
    }
    [IO.File]::WriteAllLines((Join-Path $probeDirectory 'full-build.csv'),
        $probeRows, [Text.UTF8Encoding]::new($false))
} finally {
    Pop-Location
}
$probeDirectory
```

源码复核口径：

```powershell
$sourceRows = @(rg --files src/Compiler -g '*.cs' | ForEach-Object {
    $sourceLines = [IO.File]::ReadAllLines((Join-Path (Get-Location) $_))
    [pscustomobject]@{
        Path = $_
        Physical = $sourceLines.Length
        Nonblank = @($sourceLines | Where-Object { -not [string]::IsNullOrWhiteSpace($_) }).Count
    }
})
$sourceRows | Measure-Object Physical,Nonblank -Sum
$sourceRows | Where-Object Path -like '*Backend*' | Measure-Object Physical,Nonblank -Sum
git diff --numstat eed7b28a295c0e274cbcf9bb5629918decee0926 -- src language-tools
```

## 8. 每批必须填写的结果表

| 项目 | 父版本 | 本批 | 相对固定基线变化 | 证据 |
| --- | ---: | ---: | ---: | --- |
| Compiler / Backend 物理行、非空行 | 待测 | 待测 | 待测 | 固定口径统计、完整 diff |
| 删除的旧扫描器/选择循环/完整副本入口 | 列表 | 列表 | 累计列表 | 方法名与删除位置 |
| Bind / generic / direct / prediction Analyze 次数 | 待测 | 待测 | 待测 | W1–W7 诊断原始数据 |
| 静态扫描 / AST 节点访问 / 重载枚举次数 | 待测 | 待测 | 待测 | 同输入、同诊断补丁 |
| W0/W10 完整构建预热时间与分配 | 待测 | 待测 | 待测 | 五对进程 CSV |
| W4 数组槽位总量与映射分配 | 待测 | 待测 | 待测 | 固定 F、变化 M |
| 方法数 / ILBytes / LocalCount / GuardCount | 待测 | 待测 | 待测 | PE 方法清单与语义对比 |
| W7/W8 脚本执行时间与分配 | 待测 | 待测 | 待测 | 命中/失败与边界输入分列 |
| 语义回归 | 结果 | 结果 | 失败差异 | 框架、模式、测试输出 |

完成标准是生产代码减少、重复工作按目标消除、推导与动态语义保留，并有完整原始测量数据。某项未测就保留“待测”，不能把设计推算或旧版本报告写成当前优化成果。
