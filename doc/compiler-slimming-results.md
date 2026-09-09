# 编译器降膨胀验收结果

依据 [compiler-slimming-plan.md](compiler-slimming-plan.md)，固定生产基线 `eed7b28a295c0e274cbcf9bb5629918decee0926`。实施过程与失败样本说明保存在 [历史记录](compiler-slimming-history.md)，最终结论以本文件及下列验收产物为准。

## 结果

- Compiler：39,375 → **39,037 物理行**，35,805 → **35,503 非空行**；净减少 **338 / 302**。
- Backend：26,362 → **26,024 物理行**，24,463 → **24,161 非空行**；同样净减少 **338 / 302**。
- 生产 C# 文件仍为 Compiler 136 个、Backend 46 个。没有增加生产文件、通用 Pass 框架、策略接口、全局编译缓存或代码生成项目。
- 最新 Examples 完整编译预热中位数降低 **18.7%**，完整构建分配降低 **41.4%**。有效 native 方法和动态回退保留；没有通过降低推导迭代上限或禁用特化取得收益。
- 方案第 10 步的源码读取缓存经过隔离试验后未纳入：该步明确允许因实现增长延期。不能将此结果描述为所有源码单轮只读取一次。

## 实施与删除清单

| 方案步骤 | 最终实现 | 删除或取消的旧工作 |
| --- | --- | --- |
| 0 | 固定源码、输入、构建模式与比较口径；在隔离副本采集次数和阶段数据 | 修正旧 profile 各模块重复启动预测的测量方式；EmitOnly 注明包含规划和分析 |
| 1 | 共用宿主/原生对象重载选择规则，数组固定元数据集中维护 | 删除重复候选评分循环及 GetPackedClrType/StorageType/ItemsField/Constructor 四组 switch |
| 2 | 每个函数和初始化器绑定一次，共用名称映射与 FunctionBinding | 删除预测和模块分析各自绑定，以及模块稀疏绑定数组 |
| 3 | 仅分析实际 direct 候选 | 删除普通函数的 direct 分析、参数验证及回退副本 |
| 4 | 绑定收集调用/return；闭包根归属和稳定性、初始 null 观察及异常结构跨分析复用 | 删除 NativeReturnSummary、DirectCallCollector 遍历器和 emitter 三个 Contains 异常扫描器；闭包扫描退出固定点 |
| 5 | 保持固定点顺序，按 callee 返回、参数或捕获变化标记相关函数重算 | 取消无条件全函数 Analyze；合并 direct 分析/验证；过时参数证据可撤销 |
| 6 | TypedFunctionCode 保存调用目标及无匹配结果；guard 单独选择并缓存 | 普通发射不再重新枚举重载；删除 guard 临时参数类型字典 |
| 7 | 先完成各模块 generic/direct，再预测和 Apply；无预测依赖者复用 generic | 删除全函数无条件预测和完整预测 TypedFunctionCode 的长期持有；只保留表达式类型差异和摘要 |
| 8 | ModulePlan 一份 ID 映射，模块数组使用 ModuleIndex | 删除 maxId 容量扫描和全局编号空槽；删除默认参数查询的全函数扫描 |
| 9 | 过滤已证实无收益的调用、typeof、属性及索引保护；共用数组读取/修改/写回流程 | 删除八组复制的数组 Mutation/Compound 实现及对应重复分发 |

同一次编译中的可变流状态仍独立；绑定与静态事实只在本次构建共享。预测不参与无 guard 的 ABI/存储证明。高成本队列和独立轻量解释器未引入，采用方案允许的输入变化跳过与 generic 摘要复用方式。

还修复了原有并发缺陷：并行函数体绑定会给非线程安全的全局 ID 和 ScopeTable 分配条目。现在在原来的串行注册阶段注册全部嵌套函数和默认参数中的函数，并保留父 Scope；函数体分析继续并行。新增回归检查多模块重复规划的 ID/Scope 唯一性和父归属。

## 语义验证

| 框架 | 结果 |
| --- | ---: |
| net8.0 | **1121 / 1121 通过** |
| net9.0 | **1280 / 1280 通过** |
| net10.0 | **1280 / 1280 通过** |

测试按框架能力覆盖 Dynamic、OnlyRun、Persistence，以及普通返回、native、闭包、递归、热重载、异常控制流、数字/数组边界、序列化与源码解析。新增 guard 回归同时检查结果、每个 operand 的求值次数及无冗余 Kind/packed 转换调用。TRX 见 [final-test-results.zip](slimming-validation/final-test-results.zip)。

net9 初次发现的 GetCalls 空引用在固定基线也可复现：运行时方法可能没有 IL 方法体。测试辅助已按空 IL 处理，不计为编译器优化。机器原有 net10；net8/net9 运行时隔离安装在 `.git/slimming-validation/dotnet`，未改变系统安装。

Examples 和 Benchmark 的 Release 构建均为 0 警告、0 错误。实施期间按用户要求没有逐步跑测试，集中验收发现问题后修复并重验。

## 编译性能

每个样本五对独立进程、A/B 顺序交替、每进程 16 次完整构建。主指标为每进程 Run 8–15 中位数，再取五进程中位数。计时和分配使用 Stopwatch / GC.GetTotalAllocatedBytes(true)，测量期间未并发运行其他构建、测试或 benchmark。没有将诊断版计数开销算入性能结果。

| 最新快照样本 | 基线预热 ms | 候选预热 ms | 基线分配 bytes | 候选分配 bytes |
| --- | ---: | ---: | ---: | ---: |
| W0 Examples，Persistence | 592.411 | **481.626** | 27,631,624 | **16,184,868** |
| W10 A*，Dynamic | 151.8705 | **127.715** | 7,853,928 | **6,412,184** |
| W10 MD5，Dynamic | 132.7295 | **116.7565** | 5,168,920 | **4,590,244** |

W0 的五进程预热中位数范围：基线 573.3445–613.524 ms，候选 475.8405–495.447 ms；首次完整构建中位数 1948.094 → 1470.537 ms。首次构建不包含进程启动，不与预热混算。各时间段的绝对值受系统环境影响，只使用同轮相邻 A/B 作相对比较。

原始值与逐进程统计：

- [W0](slimming-validation/W0-verified-samples.csv) / [逐进程统计](slimming-validation/W0-verified-process-summary.csv)
- [A*](slimming-validation/W10-astar-verified-samples.csv) / [逐进程统计](slimming-validation/W10-astar-verified-process-summary.csv)
- [MD5](slimming-validation/W10-md5-verified-samples.csv) / [逐进程统计](slimming-validation/W10-md5-verified-process-summary.csv)

W0 双方 17 个磁盘脚本逐字节相同，见 [输入清单](slimming-validation/verified-input-manifest.csv)；memory overlay 使用 Examples 相同源码。每次完整构建包含 PE 序列化、独立目录 DLL 写入和加载，没有改成增量构建。

辅助受控样本也各完成五对进程：W1/1000 独立函数 39.8935 → 35.0345 ms、20,342,516 → 11,967,064 bytes；W4/100 模块共 1000 函数 63.4375 → 40.9915 ms、29,067,612 → 13,961,632 bytes；W3/8 互递归加 500 个普通函数 36.308 → 21.3785 ms、15,784,424 → 7,551,652 bytes。对应 `*-final-samples.csv` 与 `*-final-process-summary.csv` 已归档。这些样本在最后的无效属性 guard 补查前测量，其无提示函数/数值调用路径未改动；主 W0/W10 使用补查后的最新快照。

## 确定性计数

[structural-counts.csv](slimming-validation/structural-counts.csv) 保存每次构建计数；[structural-raw.zip](slimming-validation/structural-raw.zip) 保存原始日志。各样本 16 次构建的计数一致。计数补丁仅用于独立 worktree，不进入生产代码。

| 指标 | 基线 | 候选 |
| --- | ---: | ---: |
| W1/1000：Bind（含初始化器） | 2002 | **1001** |
| W1：generic/direct/prediction Analyze | 1001/1000/2001 | **1001/0/0** |
| W2/50：prediction Analyze | 2551 | **98** |
| W3/8 + 500：generic/direct/prediction Analyze | 1527/1016/1017 | **517/16/16** |
| W4/10 模块：每类模块数组槽位 | 5500 | **1000** |
| W4/100 模块：每类模块数组槽位 | 50500 | **1000** |
| W5：Bind | 452 | **226** |
| W5：静态闭包扫描 / 根 cell 解析 | 1350/1500 | **225/125** |
| W5：模块索引构建 | 6 | **1** |
| W6：分析 / 发射重载候选枚举 | 80/6 | **12/0** |
| W6：额外 prediction Analyze | 3 | **0** |

W1/W4 每函数和初始化器的最大绑定次数为 1；W1 每函数每模式的最大 Analyze 次数为 1。W3 的独立函数不随递归轮次重算；W5 只读、写入、提前捕获和多层捕获均纳入样本。StatementEntries/ExpressionEntries 是遍历入口次数（包含空节点调用），不是去重的 AST 节点数。输入修订由实际事实变化触发的重算标记实现，不新增生产修订号字段。

阶段诊断另用无节点计数的隔离副本分开记录 binding、types、prediction，见 [phase-profile.csv](slimming-validation/phase-profile.csv) 和 [补丁](slimming-validation/phase-instrumentation.patch)。它只用于归因，不替代完整构建 A/B，也不把阶段耗时与完整构建耗时相加。

## 运行性能与 IL

运行测量在编译、域初始化及预热后进行。每个 Run 为 1000 次调用，每调用循环 512 次，计时内逐次核对返回值；各样本同样五对进程、每进程 16 个 Run。

| 运行样本 | 基线预热 ms | 候选预热 ms | 双方分配 bytes |
| --- | ---: | ---: | ---: |
| guard 命中 | 25.623 | 25.676 | 120000 |
| 替换为返回字符串后的 guard 回退 | 42.008 | 42.035 | 120000 |
| typeof 无效 guard | 28.4695 | 27.768 | 120000 |
| 五种数值数组修改 | 110.9175 | 111.2585 | 120000 |

有效 guard 与数组样本的差异不足 1%，分配一致，不宣称运行加速或统计显著性。原始 `W7-*-runtime-samples.csv` / `W8-runtime-samples.csv` 与逐进程统计已归档。最后修正后重新核对结果并比较全部运行样本方法体，和运行测量时的候选代码等价，见 [final-runtime-equivalence.csv](slimming-validation/final-runtime-equivalence.csv)。边界、NaN、负零、溢出和异常求值顺序由完整语义回归覆盖。

完整 Examples 包含 **242 个方法（含 19 个构造函数）**，双方签名与数量一致。元数据 token 解析为成员/类型名称；只规范化隔离脚本目录与模块注册专用的进程 PathHash，不屏蔽普通整数常量。**240 个方法的规范化指令、locals 和异常区域一致**。所有方法 InitLocals 与异常区域一致。剩余差异：

- `formatValue$typed`：835 → 766 字节，locals 21 → 18，删除一个无效 Kind 检查。
- `testHotPatch$typed`：344 → 175 字节，locals 13 → 1，删除两个无效 Kind 检查。直接在求值栈传递操作数后 MaxStack 从 5 变为 6，没有改变调用目标或求值顺序。

总 IL **57,098 → 56,860 字节**，无方法或 native 签名被删除。见 [全方法比较](slimming-validation/examples-il-comparison.csv) 及 baseline/candidate-examples-il.json。运行样本中 arrays$native 的 811 字节 IL、locals 完全一致；noop$typed 从 151 缩到 108 字节、locals 7 → 5、Kind 检查 1 → 0。

## 可选步骤 10

20 个 importer 指向相同文件时，实测该依赖每轮 import 读取 20 次、parse 读取 1 次，见 [W9-read-calls.csv](slimming-validation/W9-read-calls.csv)。只修改 FileSource 的读取诊断与缓存试验补丁均已归档。

隔离缓存试验的五对进程：9.4655 → 8.806 ms，3,792,304 → 3,757,288 bytes。虽然耗时下降，但引入第二份读取缓存、净增长 4 非空行，且未统一增量路径。依据方案第 10 步“若实现增长或收益不足，记录为已定位但延期”，**本轮不采用**，生产 ScriptCompiler/IncrementalCompiler 不增加这层缓存。这是明确取舍，不是已实现 ReadSource≤1 的声明。

## 复现与产物

- 构建/回归：方案第 7 节的 Examples、Benchmark Release 构建及 net8/net9/net10 全套命令。
- 完整构建对照：[RunCompilerSlimmingComparison.ps1](../benchmark/RunCompilerSlimmingComparison.ps1)，默认 Examples `--compile-benchmark`；受控样本使用 `-ProbeArguments @('--slimming-probe','W1','1000')` 等。
- 运行对照：`-ProbeArguments @('--slimming-runtime','W7-hit','1000')`，同样支持 W7-miss、W7-noop、W8。
- 诊断安装/统计：[InstallCompilerSlimmingDiagnostics.ps1](../benchmark/InstallCompilerSlimmingDiagnostics.ps1)、[SummarizeCompilerSlimmingDiagnostics.ps1](../benchmark/SummarizeCompilerSlimmingDiagnostics.ps1)。安装脚本拒绝主工作树。
- IL 导出：Benchmark 的 `--slimming-il 输入.dll 输出.json [宿主程序集]`；比较脚本 [CompareCompilerSlimmingIl.ps1](../benchmark/CompareCompilerSlimmingIl.ps1)。

失败的早期 W0、缺少声明的早期 native 样本以及历史中间版本不计入最终结论。计数与时间分开采集，历史结果不混入 `*-verified-*` 主结果。用户原有的 `123.dll` 和 `dump.txt` 未纳入本次修改。