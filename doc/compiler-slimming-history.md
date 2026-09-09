**历史记录：以下内容按实施阶段保留，当前结论以 compiler-slimming-results.md 为准。**

# 编译器降膨胀实施记录

基线：`eed7b28a295c0e274cbcf9bb5629918decee0926`。方案：[compiler-slimming-plan.md](compiler-slimming-plan.md)。

## 当前状态（2026-09-09）

任务尚未完成；当前实施覆盖批次 A–D，已达到首批净减少 300 非空行的源码目标，已开始统一验收。三个框架全套回归通过，性能、确定性计数与 IL 对照尚未完成。按用户要求，实施期间没有每步测试；主要改动完成后集中验证。不得据此宣称整个方案已完成或编译性能已提升。

已落实：

- `ExampleCompilationProfile` 建立一次跨模块返回预测，单列 `return-prediction` 阶段，各模块使用同一实例。
- `EmitOnly_ParsedLargeModule` 的测量范围注明为排除解析的后端构建，包含规划、绑定、类型分析和 IL 发射。
- `TypedFunctionBuilder.TryGetHostExport` 删除手写重载候选、评分和歧义循环，使用现有 `HostExportArgumentFacts.TrySelectOverload`。
- 公共选择器统一拒绝 spread，并在兼容性检查中同时计算转换成本，删除第二次参数类型遍历。
- 分析器按需保留取类型委托，避免每次选择创建委托；`HostExportDescriptor` 复用构造时取得的反射参数数组，避免参数查询反复调用 `Method.GetParameters()`。
- `BindModule` 直接填充 session 预测持有的绑定数组，并用同一名称映射绑定初始化器；`TypedModuleCode` 消费相同绑定，不再创建第二份绑定或模块稀疏绑定数组。独立 Build 入口也走相同流程。
- 模块 direct 调度仅遍历候选函数；独立分析、参数需求分析和不收敛回退均跳过非候选 direct 副本。没有候选时跳过调用证据的正文扫描。
- `CapturedCellTypes` 在共享绑定后一次建立闭包根归属与稳定初始化表达式。各轮只读取初始化表达式的最新类型，不再重建模块索引、扫描写入或追溯根 cell；普通分析与预测复用同一份静态结果。
- 名称绑定同时记录保护区域、finally return 和各循环的 finally transfer；删除 emitter 三个递归 Contains 扫描器。循环标记区分循环自身与外围 finally，未改为函数级统一标记。
- 初始 null 观察结果和按局部变量计算的静态写入事实由 FunctionBinding 持有，跨分析轮次共享。
- 名称绑定收集 return 节点，预测直接读取列表；删除 NativeReturnSummary 类及其 AST 遍历器。
- 固定数组元数据合并到 `TypedRuntimeMetadata.PackedArray`：同一包装类型只声明一次，复用字段的 DeclaringType/FieldType 得到包装/存储类型；删除 emitter 的 GetPackedClrType、GetPackedStorageType、GetPackedItemsField、GetPackedConstructor 及其重复 switch，没有保留转发层。数组栈种类映射和操作流程仍待继续精简。
- 名称绑定收集调用节点；预测固定点比较实际消费的 callee 返回摘要（包括 native/structural）和本函数捕获类型，输入未变化则复用上一轮结果。保持原有同步固定点顺序和收敛上限，未引入队列框架。
- 模块固定点复用没有 direct/upvalue 依赖的 generic 分析；独立 direct 的参数未改变则复用结果；没有 direct 依赖的初始化器也不再逐轮重分析。相关函数的完整输入比较仍待补齐，不能将此描述为步骤 5 全面验收通过。
- 宿主调用把选定 descriptor（含无匹配结果）保存在 TypedFunctionCode；每次重新分析调用参数时失效，普通发射直接消费。guard overlay 不继承普通调用目标，按当前受保护事实独立选择并缓存宿主结果；native value 的普通目标也不再跨 guard 继承。native object/value 的剩余重复选择仍待收拢。
- 参数证据直接遍历绑定阶段收集的正文调用节点，删除 DirectCallCollector 类、其递归 visitor 及每次构造的扫描对象。单独记录默认参数调用与正文的分界，保持原来正文证据范围。
- 宿主返回类型改为使用已有 GetNativeFlowType，删除重复的类型 switch。
- 原生值和原生对象方法共用现有调用结果字典，普通发射不再重选无匹配方法；guard 结果按分支缓存。原生值 guard 选择直接读取类型事实，删除临时参数类型/native 类型字典及填充循环。
- 原生对象候选选择统一到 HostExportArgumentFacts.SelectNativeOverload，删除分析器与 emitter 的重复候选循环，保持 params 回退、context 偏移、额外/默认参数和同成本顺序规则。使用 descriptor 已缓存的反射参数元数据。
- 数组修改统一为 EmitPackedMutation / EmitPackedCompound，两者共享接收者和索引求值、元素读取及写回方法；删除 8 个按类型复制的 Numeric/UInt32/Int64/UInt64 Mutation/Compound 方法与重复分发分支。Number 的增减仍使用带符号的 double 常数加法；整数仍使用原来的 Add/Sub。保留 UInt32 除法回到 Number、Int64 无符号右移回转、有符号/无符号转浮点和 64 位写回检查。当前仅有源码对照，IL/locals/边界执行仍待统一验收。
- guard 在保存操作数和生成 IL 前比较成员调用目标；保护前后目标相同（含两边均无原生目标）则省略分支。guard 选择结果保留在同一个 overlay 供发射复用。原生值选择提取为已有 emitter 的小方法，选择与发射共用。
- 删除 typeof 的 guard 入口，因为其发射始终使用相同 Datum runtime 操作。普通元素访问在保护后仍非 packed/native receiver、索引类型也未改变时，不再因 receiver/value 的无效类型提示生成 guard。其余运算与属性访问的收益筛选仍需继续审计；W7 的 IL/locals、命中与回退性能尚待最终测量。
- 模块注册函数时确定 ModuleIndex，ModulePlan 持有唯一的 FunctionId→模块索引映射。TypedModuleCode 的 generic/direct/参数/需求数组以及 emitter 的方法/候选数组均按模块函数数分配；删除模块级 maxId 容量扫描。全 session 的绑定和预测数组仍使用全局 ID，不复制到各模块。
- 捕获类型读取区分全 session 预测数组与模块紧凑数组；native 参数/coercion 分析、发射调用查找和返回预测 Apply 均切换到同一模块索引。默认参数查询也通过共享映射访问，删除 builder/emitter 两处全函数扫描。W4 总槽位、非连续 ID、多模块及嵌套函数仍待统一验证。
- 返回预测收敛后只保留 PredictionFacts（返回摘要、表达式 flow/native 类型），完整 TypedFunctionCode 仅作为 Build 内的固定点临时数据；不再由 session 或 generic/direct 结果持有预测的 locals、写入、循环、结构类型和调用计划。
- Apply 时移除与 generic/direct 均相同的表达式事实；空字典不再保留，没有预测差异的函数直接跳过 guard 准备。复用现有字典并原地删除，不复制新字典。guard 对缺省条目读取当前证明类型，避免把删除的条目误解为 Null。
- 原有预测存储隔离测试改为检查 value 的读取表达式类型，继续检查普通存储保持 Dynamic；不再要求预测持有完整局部变量数组。按用户要求尚未运行。步骤 7 的先 generic 后预测、摘要复用仍待实施。
- 准备顺序现已改为：共享绑定/闭包静态结果 → 所有模块 generic/direct 分析 → 返回预测 → 统一 Apply → 正常发射。TypedModuleCode.Analyze 不再隐式启动预测；独立 Build、EmissionSession 与 benchmark 使用同一个 CallableReturnPredictions.Build 准备入口及模块结果。
- 无调用、无捕获/直接函数依赖的函数直接从 generic 得到返回摘要，额外 prediction Analyze 为 0；相关初始化器也省略重复预测。不依赖提示的 generic 结果只借用于固定点，未把其字典作为可裁剪预测持有，避免误删证明事实。
- ExampleCompilationProfile 随新入口改为报告 types-and-return-prediction 合计阶段；删除原来逐模块报告（新入口已提前分析全部模块，原报告将仅测字典读取）。独立诊断仍需在统一验证时拆分普通分析与预测的实际开销，不能拿此合计当纯预测时间。
- 模块固定点在现有顺序上记录反向调用依赖，用 generic/direct 的重算标记取代无条件相关函数 Analyze。参数事实变化立即通知调用者；返回摘要在本轮统一发布时通知；捕获类型变化只通知对应闭包。没有新增队列或通用调度器。
- 参数证据每轮从当前 generic/direct 调用结果重建，清除旧 exact conflict，允许过时的观察撤销；仍保留收敛上限和保守回退。此调整必须在最终递归/声明顺序/native ABI 测试中验证，当前不宣称已经保持推导能力。
- direct 分析与参数验证合并到 AnalyzeDirect，删除主固定点与独立路径中的重复分析/验证代码。紧凑 generic 候选数组使用 Array.Fill，删除逐函数初始化循环。

新增生产文件：0。没有新增服务、接口层级、调度框架或全局缓存。

| 指标 | 固定基线 | 当前 | 差值 |
| --- | ---: | ---: | ---: |
| Compiler 物理行 | 39,375 | 39,040 | -335 |
| Compiler 非空行 | 35,805 | 35,505 | -300 |
| Backend 物理行 | 26,362 | 26,027 | -335 |
| Backend 非空行 | 24,463 | 24,163 | -300 |

历史验证（仅覆盖最初的宿主选择器修改，不覆盖随后绑定/direct/静态扫描修改）：

- 修改前 net10.0 全套：1275/1275 通过。
- 生产修改后 net10.0 全套：1275/1275 通过。
- 新增选择器回归后 HostExportGeneratorTests：24/24 通过，覆盖单次参数事实读取、spread 拒绝、同成本歧义。
- Benchmark Release 构建：0 警告、0 错误。
- `git diff --check` 通过。

## 统一验收进度

- net10.0：1276/1276 通过。首次统一编译发现 Operator 不是常量枚举，已将两处增减模式匹配改回显式相等比较。
- net8.0：1118/1118 通过。机器原来只有 net10 运行时；在 `.git/slimming-validation/dotnet` 隔离安装 8.0.31 和 9.0.20，测试进程通过 DOTNET_ROOT/DOTNET_ROOT_X64 使用它们，不修改系统安装。
- net9.0：1276/1276 通过。首次运行有 1 项失败；在固定基线独立工作树上复现相同测试失败。测试辅助 GetCalls 对没有 IL 方法体的运行时方法空引用，改为返回空 IL 后全套通过。这是测试辅助修复，不能算编译器优化收益。
- Examples（基线和候选）、Benchmark Release 构建均为 0 警告、0 错误。
- TRX：`tests/AuroraScript.Tests/TestResults/slimming-net8.trx`、`slimming-net9.trx`、`slimming-net10.trx`。
- 固定基线隔离工作树：`D:/SourceCode/AuroraScript.JIT.slimming-baseline`，提交 `eed7b28`。完整 Examples 输出及依赖分别保存在 `.git/slimming-validation/baseline-examples` 和 `candidate-examples`；该目录也保存 candidate.patch、程序集 SHA-256 和环境记录。
- 新增可重复执行的 `benchmark/RunCompilerSlimmingComparison.ps1`，按五对进程交替 A/B、每进程 16 次构建执行，原始日志和 CSV 逐进程保存。首次 W0 会话 45153 已失败终止：第 2 对 candidate 出现 Unbound local declaration；单独复现又出现未绑定名称导致字符串常量为 null。`.git/slimming-validation/w0` 的不完整样本不得用于性能结论。
- 根因审计发现 FunctionBinder 的并行 BindFunctionBodies 仍可通过 EnsureNestedFunction 修改共享的非线程安全 FunctionId 计数器与 ScopeTable.List。嵌套函数注册已移入现有串行注册阶段，连同参数默认值中的函数一并预注册，并保存实际父函数 Scope；并行绑定只消费已注册函数，不增加锁或禁用并行分析。
- 修复后 3 个独立进程各 16 次 Examples 完整构建通过，日志 `.git/slimming-validation/stability-fixed/process-{1,2,3}.txt`。这是稳定性复现检查，不是正式 A/B 结果。旧 candidate-examples 快照尚未包含该修复，后续正式测量必须重新构建并保存新快照。
- 新增 NestedFunctionIdsAndScopesRemainUniqueAcrossParallelModules，检查 1/20 模块、多层嵌套、全局 ID 与 Scope 唯一性及父 Scope；每种规模重复规划 10 次。修复后的 net10 全套 1278/1278 通过，会话 52065 已完成；前面的 net8/net9 通过结果属于修复前版本，修复后仍需确认。
- 输入 SHA-256 检查发现首次快照的 tests/test.as 仅换行符不同（190/189 字符，去除 CR 后完全一致），其他脚本一致。正式 A/B 前须将仓库脚本文本原样同步到双方隔离输出，重新记录 input-manifest.csv，确保输入逐字节相同；首次不完整 W0 不作结论。

待完成：步骤 0 的独立计数补丁、W0/W10 五对进程 A/B、规范化 IL 对照；步骤 1 数组映射去重；步骤 4 调用点摘要和其余重复扫描审计；步骤 5–9；步骤 10 按 W9 实测决定。步骤 2–4 的当前实现仍需统一验证确定性计数、推导与 IL 等价，不能仅以源码改动宣称验收完成。最终仍需 net8/net9/net10 全套验证及逐项验收。当前的选择器单元测试不能代替整条管线的结构计数或性能测量。

## 修复后的 A/B 结果

修复后全套：net8.0 **1120/1120**、net9.0 **1278/1278**、net10.0 **1278/1278** 通过。net8/net9 的最终会话 71299/55627 已完成。

W0 会话 70322 与 W10 会话 21679 均已正常结束；每个样本各完成 5 对独立进程、每进程 16 次完整构建。下面是每进程 Run 8–15 中位数再取五进程中位数，原始数据及各进程中位数保存在 [slimming-validation](slimming-validation/)。没有同时运行构建、测试或另一项 benchmark。

| 样本 | 基线预热 ms | 候选预热 ms | 基线分配 bytes | 候选分配 bytes |
| --- | ---: | ---: | ---: | ---: |
| W0 Examples Persistence | 960.511 | 787.458 | 27,652,108 | 16,540,072 |
| W10 A* Dynamic | 233.4075 | 199.150 | 7,570,148 | 6,185,244 |
| W10 MD5 Dynamic | 192.6725 | 165.736 | 4,900,720 | 4,343,836 |

W0 预热时间 **-18.02%**、分配 **-40.19%**；首次完整构建的进程间中位数由 2811.895 ms 降为 2141.809 ms（-23.83%）。W0 双方 17 个磁盘脚本逐字节一致，见 w0-input-manifest.csv；overlay 仍为 Examples 原有同一文本。基线生产源码保持 eed7b28，候选快照为 `.git/slimming-validation/candidate-fixed-examples`，包含串行预注册修复。原 `.git/slimming-validation/w0` 是失败旧样本，不计入这些结果。

W10 使用现有 FullCompile_RealAstar/FullCompile_RealMd5，通过新增 `--slimming-probe` 入口运行。为使双方探针一致，只将 benchmark 的 CompilerPipelineBenchmarks.cs / Program.cs 同步到基线工作树，未修改其生产源码。新增受控入口还支持 W1、W2/native、W3/native、W4 和 W5；这些受控样本尚未测量，不能据此认定对应结构计数已通过。

上述 W0/W10 仅证明这些完整编译样本的当前收益；尚未证明所有步骤的确定性计数、W7/W8 运行性能或规范化 IL 等价。生产行数在并发修复后为 Compiler 39,043 / 35,507、Backend 26,030 / 24,165（物理/非空），净减少 332 / 298；距离首批 300 非空行目标仍差 2 行，后续应继续实质性精简，不能用删注释或压缩排版凑数。

## 受控样本与结构计数

删除串行预注册后已无必要的 EnsureNestedFunction 转发方法，在调用点读取注册结果并保留缺失检查。当前 Compiler **39,034 / 35,500**、Backend **26,021 / 24,158**（物理/非空），净减少 **341 / 305**，没有通过删除注释或压缩多语句凑数。

下列完整编译性能同样采用五对进程、每进程 16 次构建及每进程预热中位数再取中位数；CSV 已归档：

| 样本 | 基线预热 ms | 候选预热 ms | 基线分配 bytes | 候选分配 bytes |
| --- | ---: | ---: | ---: | ---: |
| W1 1000 个独立普通函数 | 104.927 | 65.3275 | 20,051,076 | 11,702,676 |
| W4 100 模块、1000 函数 | 105.608 | 62.894 | 28,787,996 | 13,697,956 |

计数来自独立诊断工作树 `AuroraScript.JIT.slimming-diag-baseline` / `AuroraScript.JIT.slimming-diag-candidate`，安装脚本为 `benchmark/InstallCompilerSlimmingDiagnostics.ps1`。生产工作树未加入计数代码，带计数版本的时间/分配不用作性能结论。解析脚本为 `SummarizeCompilerSlimmingDiagnostics.ps1`；完整原始日志保存在 `slimming-validation/structural-raw.zip`，逐构建计数在 `structural-counts.csv`。每个样本的 16 次构建结构计数均完全一致。

| 样本/指标 | 基线 | 候选 |
| --- | ---: | ---: |
| W1 Bind（含初始化器） | 2002 | 1001 |
| W1 generic / direct / prediction Analyze | 1001 / 1000 / 2001 | 1001 / 0 / 0 |
| W1 每函数绑定最大次数 | 2 | 1 |
| W4 Bind（含 100 个初始化器） | 2200 | 1100 |
| W4 generic / direct / prediction Analyze | 1100 / 1000 / 2100 | 1100 / 0 / 0 |
| W4 每类模块函数数组总槽位 | 50500 | 1000 |
| W5 Bind（225 个函数与初始化器） | 452 | 226 |
| W5 generic / direct / prediction Analyze | 678 / 450 / 676 | 326 / 0 / 200 |
| W5 静态闭包扫描 | 1350 | 225 |
| W5 模块索引构建 | 6 | 1 |
| W5 根 cell 解析 | 1500 | 125 |

W5 包含只读、写入、提前捕获、多层转捕获四种结构。StatementEntries/ExpressionEntries 记录 TypeAnalyzer 遍历方法入口（含空节点调用），不能称为去重后的 AST 节点数。重载候选计数补丁已安装，但这些三个样本没有宿主重载，不用于证明 W6/W7。

W3-native 首次受控样本未声明 native 返回类型，被基线语法检查拒绝；已修正双方同一生成器，独立的 500 个附加函数保持普通函数。旧 `W3-native-8` 数据作废，修正版五对进程会话 50161 已全部完成，输出 `.git/slimming-validation/W3-native-8-fixed`，尚待汇总。补充 W4/10 模块、W3-native/8、W2/50 的双方结构诊断正在会话 12366 运行，输出原 diagnostics 目录；继续工作先确认该会话状态，再更新归档和计数 CSV。

## 补充诊断、运行性能与 IL

会话 12366、95612 已结束，结构 CSV 与原始 ZIP 已更新。W2/50 调用链的 prediction Analyze 从 2551 降到 98；generic 分析仍为 51（含初始化器）。W3/8 互递归加 500 个普通独立函数，generic/direct/prediction 从 1527/1016/1017 降至 517/16/16；独立函数不再跟随递归轮次重算。W4/10 模块的每类数组槽位从 5500 降至 1000，与 W1 的单模块和 W4 的百模块一起验证固定总函数数的空间变化。

W6 包含固定宿主调用、原生字符串方法、额外参数和 spread：候选枚举从分析 80/发射 6 降到分析 24/发射 **0**。诊断按最近的 TypeAnalyzer 调用栈判定分析来源，避免因整个分析由 emitter.Prepare 调起而误分类。具体重载歧义和 CLR 对象兼容性另由全套语义测试覆盖。

W3 修正版五对进程完整编译：预热中位数 74.615 → 40.882 ms，分配 15,515,400 → 7,297,128 bytes。

运行测量使用 `--slimming-runtime`，在计时前完成 Persistence 编译、域初始化、100 次预热与结果核对；每个 Run 执行 1000 次脚本调用，每调用循环 512 次。仍按五对进程、每进程 16 个 Run 测量，计时内每次调用都检查结果。W7-miss 将原普通函数替换为返回字符串的闭包，检查 guard 失败后的数值转换；W8 同时覆盖 Int32/UInt32/Int64/UInt64/Float64 数组前后缀自增和复合赋值。会话 24437 正常完成。

| 样本 | 基线预热 ms | 候选预热 ms | 双方分配 bytes |
| --- | ---: | ---: | ---: |
| W7 guard 命中 | 25.623 | 25.676 | 120000 |
| W7 guard 回退 | 42.008 | 42.035 | 120000 |
| W7 typeof 无效保护 | 28.4695 | 27.768 | 120000 |
| W8 数组修改 | 110.9175 | 111.2585 | 120000 |

这些运行样本无可重复回退证据，前三个保留有效操作的样本差异不足 1%，不宣称运行加速。边界值、溢出、NaN、负零、求值副作用等由已有全套测试覆盖，而不是由此小型性能输入推断。

持久化 IL 通过新增 `--slimming-il` 导出方法签名、元数据 token 解析后的指令、局部变量与异常区域，JSON 和 runtime-il-comparison.csv 已归档。双方 9 个方法签名匹配；7 个方法指令/locals 完全一致，包含 **arrays$native 的 811 字节 IL**。所有异常区域一致。另两处差异逐项解释如下：

- noop$typed：151 → 108 字节、locals 7 → 5、ScriptDatum.get_Kind 检查 1 → 0，正是删除 typeof 的无效 guard。
- InitializeDomain：长度、locals、异常区域一致，仅 RegisterModule 之前的 PathHash 整数不同；ModulePlan 使用进程随机化的 FullPath.GetHashCode()，两个独立进程自然不同，字符串路径及注册目标完全一致。

总 IL 从 1672 降至 1629 字节，其余函数未隐藏增加 IL。以上是该受控 W7/W8 单元的完整方法清单，不等于已经证明全部 Examples 的 IL 对照。尚需最后的范围审计、可选 W9 处理结论及报告整理；不要仅凭这些局部门槛宣布全部方案完成。

## 最后范围审计的修正与可选 W9 结论

纯宿主调用并不消费普通函数返回预测。CanReuseGeneric 现已检查可解析的调用依赖，复用现有 callable 解析缓存，不再仅凭 Calls.Count 判断。W6 的额外 prediction Analyze 从 1 降为 **0**，分析候选枚举从 24 降至 **12**，发射仍为 **0**；结构 CSV 和原始 ZIP 已更新。最新源码的 net10 **1278/1278**、net8 **1120/1120**、net9 **1278/1278** 全套通过（8210、62646、49458 均已结束）。

当前 Compiler **39,039 / 35,505**、Backend **26,026 / 24,163**（物理/非空），净减少 **336 / 300**，生产文件数量不增加。

W9 的独立读取诊断在只修改 FileSource 的隔离工作树进行，补丁为 `slimming-validation/W9-read-instrumentation.patch`。20 个 importer 指向同一磁盘依赖：每轮该依赖有 20 次 import 读取、1 次 parse 读取；每个 importer 正文读取 1 次。分类及调用记录见 W9-read-calls.csv；读取耗时是并行调用耗时，不能直接当作端到端可节省时间。

另在隔离副本试验单次构建内 Lazy 文本缓存，原型补丁见 W9-cache-experiment.patch，没有应用到生产工作树。五对进程结果（W9-cache-trial-samples/process-summary）：无缓存预热 9.4655 ms / 3,792,304 bytes，试验缓存 8.806 ms / 3,757,288 bytes，时间约 -6.97%、分配约 -0.92%。该原型额外引入第二份缓存，净增长 4 非空行，且没有统一增量编译的重复路径。按方案第 10 步“若实现增长或收益不足，记录为已定位但延期”，本轮不纳入此可选改动；**不宣称所有 import 正文已做到单轮只读取一次**。

最新 Examples 候选与依赖已保存为 `.git/slimming-validation/candidate-final-examples`。最终 W0 五对进程正在会话 **90326** 中运行，输出 `.git/slimming-validation/W0-final`；继续工作先查询原会话。最终报告还需替换旧阶段数据描述、归档 TRX，并完成全 Examples 方法对照；当前不能标记整个目标完成。

## 全方法对照与最后的 guard 补查

90326、40761、78346 均已结束。对应快照的 W0 预热中位数 **578.3335 → 465.4485 ms（-19.52%）**，分配 **27,637,360 → 16,170,700 bytes（-41.49%）**；首次构建 **1853.643 → 1439.197 ms（-22.36%）**。这是相邻五对进程的结果，不与前面不同时间段的绝对耗时混算。W10 最后一次 A*/MD5 分别为 143.3495 → 132.1945 ms、127.884 → 117.047 ms，分配也下降，CSV 使用 `*-final-*` 文件名。受控样本的最新五对进程也已归档。

全 Examples IL 导出已补上构造函数。双方共 **242 个方法（含 19 个构造函数）**，签名和方法数量完全匹配；规范化隔离路径及 RegisterModule 专用进程 PathHash 后，**240 个方法的指令、locals、异常区域一致**。仅 formatValue$typed（835 → 766 字节、locals 21 → 18）和 testHotPatch$typed（344 → 175 字节、locals 13 → 1）删除冗余 guard，Kind 检查分别减少 1 和 2 次；总 IL **57,098 → 56,860**。所有方法异常区域一致。两个同名 makeCounter 方法按相同签名和 IL 长度对齐，无缺失方法。

比较脚本为 `benchmark/CompareCompilerSlimmingIl.ps1`；只规范化已知隔离脚本根路径及模块注册调用前的哈希常量，不屏蔽普通整数操作数。普通指令、字段/方法/类型调用目标、locals 和异常区域均保留在 JSON/CSV 中。

补查发现并修复了三类无效保护：packed/native receiver 使用非数值索引、索引变化但仍走同一动态索引 helper、没有直接 getter 的 primitive 属性，以及无 native setter/field 接收者的动态属性写入。新增 NonNumericElementAccessDoesNotGuardUnchangedDynamicHelpers 检查结果、每个 operand 的调用次数，以及不出现 Kind/packed 转换调用。相关 23 项返回推导测试通过，随后最新 net10 全套 **1280/1280** 通过（91895 已结束）。

本次还将三个紧凑索引 getter 改为直接查询表达式，保留无效 ID 返回 null 的行为。当前 Compiler **39,037 / 35,503**，Backend **26,024 / 24,161**，净减少 **338 / 302**（物理/非空）。上述性能及全 Examples IL 快照早于这次 guard 补查；最新代码仍需刷新对应快照并确认 net8/net9，不能把旧快照当成全部最终证据。
