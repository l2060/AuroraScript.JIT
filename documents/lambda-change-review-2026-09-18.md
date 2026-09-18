**Lambda / callable 变更审查报告 · 2026-09-18**

**结论：方向有价值，当前实现不宜直接验收；存在局部过度设计和已验证的语义回归，但没有证据把整体定性为“病态优化”或“编译器代码爆炸”。**

建议保留 callable 声明、上下文参数信息、受保护的 native 入口和共享 thunk；先修复回退语义与边界缺陷，再收敛重复推断、重复发射路径和冗余状态。当前最需要的是减少机制数量，而不是继续增加覆盖特殊情形的分支。

**1. 审查范围与证据口径**

审查了最近提交记录，以 `af67748`（Declare callable types with type syntax and bind ambient contracts）及其后的当前工作区为主要范围。

- “改动前”：`af67748^`，即 `4fa6f67`。
- “HEAD”：`af67748`，已经包含第一版 callable 类型与调用优化。
- “工作区”：HEAD 加已暂存、未暂存、相关未跟踪源码的实际组合。没有只看 `git diff` 或只看暂存区。
- 排除 `.tools`、`.tmpdump`、`.wiki`、根目录 dump 文件和临时 DLL；工具目录不计入源码增长与设计评价。构建使用项目必需的宿主代码生成器，不评价该工具的内部实现。
- 源码规模只统计 `src/Compiler` 下 C# 物理行，包含空行、注释，不把文档、测试、工具、构建输出算作编译器膨胀。
- 对照验证在独立临时项目中进行，HEAD 源码通过 `git archive` 导出后单独构建；实际运行时明确切换 HEAD / 工作区 DLL。未修改被审查的源码或现有测试。

这里必须区分三个问题：语言能力是否改善、运行时是否更快、编译器是否更复杂。第一个不能替第二个作证明，第三个也不能只用新增行数裁决。

**2. 实际改变了什么**

lambda 在此次之前已经能作为运行时闭包值存在。此次主要提升的是：将函数值的调用契约显式表达出来，并利用契约选择参数表示、返回值处理和调用入口，而非从零实现函数作为一等值。

已提交的主要能力包括：

- `type Predicate(Number value) Boolean;` 等 callable 类型声明、名称解析、导出与全局声明接入。
- 将直接传入的 lambda 与接收参数的 callable 契约关联，补充参数及返回类型。
- 为合适的无捕获函数生成 native 入口，闭包仍保留动态调用入口。
- callable 调用先探测兼容 native delegate，不能命中时走原有 Datum 调用。

工作区进一步：

- 将反复出现在调用点的 guard、frame 管理及回退代码提取为共享 thunk。
- native callable 入口显式携带 `ScriptContext`，扩展可用的函数体与上下文处理。
- 用宿主包声明的 callable 类型替代 HTTP 回调原有的独立参数属性。
- 加入宿主回调的 nullable 参数处理和 detached native callback 入口。
- 增加跨模块 native 调用的模块状态依赖分析。

这是一组横跨语法、语义、ABI、宿主互操作的变更，不能仅作为“一项小型 lambda 性能优化”衡量。

**3. 已验证的正向收益**

**共享 thunk 有实际体积收益，不只是把 C# 代码拆文件。**

独立样例在一个 native 函数内，对同一个 `Supplier() Number` 参数重复调用；每次调用结果赋给独立局部变量，最后返回最后一个结果。HEAD 和工作区在 Dynamic、OnlyRun、Persistence 三种模式均能运行该组样例。

在 Persistence 模式，对生成程序集内声明的静态方法，累加 `GetMethodBody().GetILAsByteArray().Length`：

| 同签名调用点数 | HEAD 方法体 IL 字节 | 工作区方法体 IL 字节 | 工作区共享 thunk 数 | 体积变化 |
| --- | ---: | ---: | ---: | ---: |
| 1 | 593 | 593 | 1 | 0% |
| 10 | 1,715 | 808 | 1 | -52.9% |
| 100 | 17,619 | 2,968 | 1 | -83.2% |

该样例生成的静态方法数量从 7 增为 8，但增加的 thunk 数没有随调用点增加。增加一个辅助方法换取重复 guard 消除，是合理的空间交换。

此处测量的是方法体 IL 字节，不包括异常表、元数据、JIT 后机器码或整个 DLL；它证明生成代码减少，不证明执行速度同比提升。

还有一个实际正确性收益：将样例改为 `return f() + f() + ...;` 时，HEAD 在 10 / 100 次调用的样例中，三种模式均出现“Common Language Runtime detected an invalid program”；工作区均正常返回 10 / 100。源代码中旧路径把异常保护块内联在表达式求值中，新 thunk 将保护块移入独立方法，与消除该错误的方向一致。当前仍保留的旧内联路径不能因此视为全部安全。

**契约、动态回退和捕获限制本身是合理边界。**

类型信息只在满足条件时用于 native 路径，保留动态入口，且捕获闭包继续使用已有闭包机制，避免了立即引入按捕获环境组合生成的大量函数版本。默认参数、展开参数、未知类型等不能安全优化时应保守回退，这不是设计失败。

**宿主契约向脚本 callable 类型靠拢也是正确方向。**

删除独立的 `AuroraCallbackArgumentAttribute`，让包声明 `HttpCallback`，减少了两套完全独立的“回调参数是什么类型”描述。HTTP 传输失败时 response 为 null，nullable native object 转换有实际业务依据，不应简单视作无意义分支。

跨模块 native 入口不能直接观察调用者的模块状态，新增依赖限制及对应读写测试也是必要的正确性措施。不过，这项修正应与 lambda 性能收益分别评价。

**4. 已复现的问题：当前版本不能作为纯优化直接验收**

以下独立复现在 .NET 10 Release 下运行，涉及的行为差异在 Dynamic、OnlyRun、Persistence 三种模式一致。

**问题 A：共享 thunk 在确认 native 入口之前转换参数，改变了动态回退的输入。高优先级，工作区新增回归。**

复现一：

```as
@module(TEST);
type Reader(int64 x) String;
func raw(x) { return typeof x; }
func apply(Reader f) String { return f(1); }
export func run() { return apply(raw); }
```

| 版本 | 结果 |
| --- | --- |
| HEAD | `number` |
| 工作区 | `int64` |

复现二：

```as
@module(TEST);
type Reader(int32 x) Number;
func raw(x) { return x; }
func apply(Reader f) Number { return f(1.5); }
export func run() { return apply(raw); }
```

| 版本 | 结果 |
| --- | --- |
| HEAD | `1.5` |
| 工作区 | `Type check failed: expected int32, actual number.` |

两组样例均会报告契约不匹配警告，但编译器明确允许继续执行。原设计的动态回退因此仍然是需要保持的可观察语义；如果要改成强制契约，应单独定义语言变更，并使所有路径一致执行，不能以优化命中与否决定行为。

原因在 `TypedCilEmitter.CallableThunk.cs`：调用者先通过 `EmitDirectArgument` 将表达式转换成契约要求的 native 类型，再调用 thunk；thunk 内部才检查 `NativeTarget`。回退时只能将转换后的值重新包装，原始 Datum 已经丢失。

`CanRoundTripThroughDatum` 只判断“该参数类型本身可否往返表示”，没有证明“本次源表达式经过转换后与原始脚本值等价”。`Number → int64 → Datum` 明显不满足这一条件，`1.5 → int32` 甚至会提前抛异常。

建议先让这类需要规范化、窄化或改变脚本种类的调用走已有通用路径；或者让共享边界保留原始实参，确认入口后再转换。不要为每种数值类型新增一套专用 thunk。需要覆盖目标表达式及各实参只求值一次、求值次序、失败转换和异常路径。

**问题 B：8 参数 callable 在编译阶段越界。高优先级，HEAD 已存在。**

```as
@module(TEST);
type Sum(Number a, Number b, Number c, Number d,
         Number e, Number f, Number g, Number h) Number;
func apply(Sum fn) Number { return fn(1,2,3,4,5,6,7,8); }
export func run() { return apply((a,b,c,d,e,f,g,h) => a+h); }
```

HEAD 和工作区的三种模式均抛 `IndexOutOfRangeException`。

`TryPlanCallableCall` 允许最多 16 个参数；但 `TypedRuntimeMetadata.CallMethods` 创建的 Invoke 表长度为 8，仅覆盖 0～7 参数。thunk、动态 callable 路径和保留的内联路径仍直接按参数个数索引此表。即使预计运行时总会走 native 分支，发射 fallback 时也已经越界。

最小修复应限制此优化入口的适用 arity，让宽调用复用既有通用参数缓冲区调用机制。没有必要为了修复这个问题手写 `Invoke8` 到 `Invoke16` 的平行实现。

**问题 C：void 返回值依赖具体发射路径，语义尚未统一。工作区改善了部分路径，但仍有明显不一致。**

```as
@module(TEST);
type Action(Object x) void;
func raw(x) { return 42; }
func apply(Action f) { return f(1); }
export func run() { return typeof apply(raw); }
```

工作区返回 `number`。把契约参数 `Object x` 改成 `Number x`，使参数能够走共享 thunk，则返回 `null`。

这是同一类 void callable 的值位置调用：共享 thunk 丢弃动态函数结果并物化 null；`EmitCallableDynamicCall` 在 `materializeVoid == true` 时却保留了实际返回 Datum。HEAD 原先就存在 void 回退不统一的问题，新 thunk 修正了一部分，但也让路径差异更直接可见。

如果 void 契约意味着调用者不能观察返回值，就应所有路径统一丢弃后生成 null；如果契约不兼容时应保留完全动态行为，则共享 thunk 也必须遵守。两种政策可以讨论，当前混合状态不应验收。

以上问题说明风险来自调用路径之间语义分叉，而不是单纯“代码写得太多”。

**5. 编译器是否已经爆炸膨胀**

| 统计范围 | 改动前 | HEAD | 工作区 | 改动前到工作区 |
| --- | ---: | ---: | ---: | ---: |
| `src/Compiler` C# 文件数 | 142 | 144 | 146 | +4 |
| `src/Compiler` 物理行 | 40,809 | 42,517 | 43,194 | +2,385，+5.84% |
| 其中 Backend 物理行 | 27,767 | 28,982 | 29,653 | +1,886，+6.79% |

工作区相对 HEAD 的编译器净增长为 **677 行**。其中两个未跟踪文件为：

- `TypedCilEmitter.Callable.cs`：217 行。
- `TypedCilEmitter.CallableThunk.cs`：261 行。

它们合计 478 行。只看 tracked diff 会得出净增 199 行，显著低估此次实际新增量。

主要文件的变化：

| 文件 | 改动前 | HEAD | 工作区 |
| --- | ---: | ---: | ---: |
| `CallableContractAnalyzer.cs` | 0 | 372 | 499 |
| `TypedFunctionBuilder.cs` | 5,688 | 5,989 | 5,947 |
| `TypedCilEmitter.cs` | 8,664 | 8,996 | 8,987 |

主 emitter 在工作区减少 9 行，但新增 callable partial 文件共 478 行。按整个职责计算，不能把它描述成源码层面的瘦身。

另一方面，5.84% 的编译器源码增长包含一项新的类型语法、名称解析和宿主接入，不能仅凭这个数字判断“爆炸”。已检查的路径也未出现按各参数可能类型做笛卡尔积特化、按每个调用点复制整份函数体的算法。

目前更准确的判断是：**源码增长尚属有限；生成 IL 的重复膨胀得到明显抑制；语义机制的分叉增长已经需要治理。**

**6. 过度设计具体体现在哪里**

**两套上下文参数信息通路同时存在。**

`CallableContractAnalyzer` 提前扫描调用、关联函数，并修改 lambda 的 `DeclaredType` / 返回类型；`TypedFunctionBuilder.RecordCallableLambdaParameters` 又查找调用目标、lambda 和参数契约，写入 `ModulePlan` 的 contextual parameter 表；`TypedFunctionCode.ApplyContextualParameterPredictions` 再将其复制到表达式预测字典。

这些通路并非完全等价：声明约束与用于 runtime guard 的预测信息必须区分，缺少返回声明的部分契约也可能只适合提供预测。因此不建议简单删除第二条通路。但契约解析、函数定位和参数映射没有必要各做一遍。

建议由一个既有分析阶段记录已绑定的契约及其事实强度，后续按“确定约束 / 预测”使用同一份结果。不要为此再添加一个全新的跨阶段框架。

**调用发射维护了三份近似协议。**

当前包括普通 callable 动态调用、共享 thunk 的 fast/fallback、主 emitter 中仍保留的内联 fast/fallback。类型检查、返回值转换、void 物化、参数求值与 frame 恢复散落在多处。问题 A、C 正说明这些副本已经出现分歧。

共享 thunk 是应该保留的收敛方向。对 packed array 等不能安全复用 thunk 的场景，如果尚无实测收益，应先使用唯一通用动态调用路径，而不是保留另一套复杂内联协议以追求覆盖率。

**存在可以直接删去的状态，不需要讨论抽象架构。**

全仓库源码搜索发现：

- `PreparedDirectMethod.NativeParameters`、`NativeReturnType` 仅构造赋值和声明，没有读取使用。
- `CallablePlan.Slot` 被赋值、声明，但没有后续消费者。
- `ModulePlan.RecordContextualNativeParameter` 在旧 HTTP callback 记录逻辑删除后没有调用者。
- 当前 `NativeEntryTakesContext` 的赋值均为 true，值得检查是否仍需要维护该独立状态。

前三项可直接作为清理候选；第四项需要结合加载入口和未来明确支持的 ABI 再决定，不能只凭两个 true 赋值贸然删除兼容判断。

**宿主模型仍有局部特例，暂时不要把它包装成通用框架。**

`ExportAttribute` 以一个 callable 类型名、一个“从参数列表末尾计数”的位置、一个全回调级 nullable 布尔量描述回调；运行时新增的 `TryInvokeNativeDetached<T1,T2>` 则只服务二参数 void callback。

这对 HTTP error-first callback 是务实实现，但尚不是多 callback、逐参数 nullable、任意返回值的完整宿主委托系统。不要立即补齐所有 arity 和组合；应先证明另一个实际宿主的需求确实不能复用当前边界。

`AllowsNull` 被写进原本表示源类型引用的 AST，也把宿主调用约束与语法表示耦合。长期更合适的承载位置是现有已绑定契约或参数事实；这不意味着现在必须设计一套完整 nullable 类型系统。

**分析阶段还有可量化的复杂度风险。**

`RecordCallableLambdaParameters` 为查找被调函数及 lambda 多次线性扫描模块函数表。大规模同模块高阶调用可形成调用点数乘函数数的工作量，且它位于类型分析过程中。已有函数 ID / declaration 索引可复用，不需要另建复杂缓存体系。

新模块状态传播使用重复全表扫描的不动点算法。它有界且常见，不是天然错误；在不利声明顺序的调用链上，轮次可随函数数增长。若规模测试显示明显成本，再改为基于反向依赖的工作队列。当前没有编译时长测量支持把它直接判定为性能灾难。

**7. “一等公民”和“类型推断”的能力边界**

现在最强的优化形态是：已知本模块函数接收有类型的 callable 参数，调用点直接传入 lambda 或可识别模块函数；宿主侧是已导入包的直接成员调用。

`TryResolveCallableSlot`、`TryGetCallableType` 均主要从局部名称对应的 `ParameterDeclaration` 恢复 callable 契约。分析器的输入识别也依赖直接 lambda、模块函数名、import receiver/member 等 AST 形态。

因此，这还不是普遍的函数值类型流分析。通过局部别名、容器、属性、返回值、较长的高阶转发链传播契约，不能据此认为已经完整解决。独立 `var f = x => x+1; apply(f)` 样例能够正确执行，但这不等于局部别名获得了与内联 lambda 同样的 native 契约传播。

这不要求马上引入全局约束求解或全程序单态化。更合理的路线是先让现有作用域与绑定事实保持一致，再按实际热路径补充少量明确的函数值流转。

返回类型也需要准确命名：本次部分“推断”实质上是把预期契约返回类型施加到 lambda，而不是从函数体独立推导后证明所有返回都符合。预期类型、已验证类型、预测类型应在实现和文档里区分，避免把优化猜测升级成无条件 ABI 承诺。

**8. 验证结果及其限制**

| 验证 | 结果 |
| --- | --- |
| callable / HTTP / 模块初始化 / closure context 定向测试 | 80 / 80 通过 |
| 核心测试项目完整运行，net10.0 Release | 1,391 通过，1 失败，共 1,392 |
| 单独复跑完整测试中的失败项 | 同样失败 |
| 独立 HEAD / 工作区样例 | 已复现问题 A、B、C，并验证共享 thunk 体积收益 |

现有失败项为 `ClrDirectCompilationTests.UnknownMemberOnProvenClrTypeProducesNonFatalWarning`：警告集合相关断言通过后，测试要求错误输出包含 `warning:`，实际输出为空。这里没有把它归因于 lambda 修改，也不能把完整测试描述为全绿。

80 项定向测试属于完整测试的子集，不能相加得出更多独立覆盖。本次没有运行语言服务 / 语言服务器项目，也没有验证 net8.0 / net9.0。

本次没有提供稳定多进程运行时吞吐、JIT 时间、编译时长、分配量的 A/B 基准。因此可以确认“重复 IL 更少”和若干行为变化，不能宣称“整体性能显著提升”。尤其 HTTP 网络耗时通常远大于两参数 dispatch，不能仅因 HTTP callback 走 native 就推断端到端收益可观。

现有测试侧重成功调用、生成方法和 guard 的存在，仍缺少跨优化路径等价性，以及 0/1/7/8 参数、类型转换、void 值位置、单次求值等边界。新增的独立样例能够在大量既有测试通过时找到问题，说明需要改进测试目标，而非继续单纯增加成功案例数量。

**9. 建议的验收顺序与禁止膨胀的约束**

第一步，先定义并修复 A、C 的动态回退语义，修复 B 的 arity 越界。对不支持的情况复用通用入口，不扩写一系列特化路径。现有核心测试失败需解决或完成基线归因。

第二步，删除没有消费者的字段和包装方法；合并契约解析与函数定位；优先收敛 callable 调用发射协议。把模块状态依赖修复与 callable 性能收益分开审查，便于判断成本来源。

第三步，补充少量真正验证边界的测试与规模基准，而不是把当前实现细节写成固定断言：

- 同一个动态目标、同一实参，在 native 命中和回退时保持规定语义；覆盖 Number / int32 / int64、转换失败、null 和 void。
- 7 与 8 参数、默认参数、展开参数、捕获、跨模块与异常，验证不越界、不重复求值、不泄漏上下文。
- 同一签名 1 / 10 / 100 / 1,000 个调用点，统计 native 函数体数量、thunk 数量、IL 字节和编译分配。
- native 命中、native 未命中、混合命中分别测量吞吐；再用真实脚本确认收益，不能只测最有利的微基准。

推荐将以下规则作为防止膨胀的验收标准：

- 每个可优化函数至多新增一份 native 实现体，不因每个使用位置或每组实参复制整份函数。
- 同一模块、相同调用签名与转换语义共享 thunk；调用点增长不得导致同签名 guard 体线性复制。
- 不支持的组合优先走唯一通用 fallback；新增快路径必须有可测收益，并说明新增维护的语义责任。
- 无读取者的计划字段、无调用者的适配层不得长期保留；拆 partial 文件不算净简化。
- 新的宿主 callback 形态优先接入既有契约，禁止每增加一个包就新增一套独立推断与调用协议。
- 无性能证据时，不继续扩大特化覆盖，也不为未来假想组合先搭建大型类型推断框架。

**最终判断**

这批修改包含值得保留的实质收益，尤其是共享 thunk 对重复 IL 和表达式内调用正确性的改善。它也存在可复现的回退语义回归、未修复的宽调用崩溃，以及已出现语义分叉的重复机制。

因此应评价为：**有价值但尚未完成收敛的优化；局部复杂度已经超出当前证明的收益，应该修正并精简，而不是整体推倒，也不应继续以“lambda 一等公民”为理由无上限叠加编译器代码。**
