# 编译器与 Runtime Ops 清理审计

日期：2026-09-08。本文件及引用 JSON 记录 `bc1e422` 基线的审计结果；后续实施情况见 [第一批](ops-optimization-pass1.md)、[第二批](ops-optimization-pass2.md)、[第三批](ops-optimization-pass3.md) 和 [第四批优化记录](ops-optimization-pass4.md)。下文行号和可达性计数保留为审计时的快照。

## 结论

不能把 Ops 的行数直接等同于过度设计，但确实存在可连带清理的历史路径。

- 审计覆盖 `TypedRuntimeMetadata` 的全部 243 个静态字段、11 个方法，以及 Runtime 下全部 10 个 Ops 类的 249 个显式方法。
- 发现 **22 个没有有效消费者的元数据字段**：11 个直接没有读取，另 11 个只被不可达的编译器方法引用。
- 发现 **16 个当前生产编译链不再生成调用的 Ops 方法**：11 个算术/位运算真值版本、4 个专用元素更新方法、1 个 UserState 包装方法。
- 发现编译器中的 **5 方法互相引用但没有入口的条件发射子图**，以及其他无调用方法、恒定为 false 的选项和无效扫描。
- `PackedArrayBoundaryOps` 的 55 个公开转换方法全部有按类型/签名选择的编译器路径，不能因没有直接 C# 调用而删除。
- packed 边界的两个捕获 lambda 是实质性性能问题：本机预热后，缓存命中的 `ToInt32Storage(wrapper)` 和 `FromInt32Storage(storage)` 各测得 **24 B/次托管分配**。

“可删除”分为两种：不改变当前仓库新编译程序结果，以及不破坏仓库外调用/旧 DLL。前者可由本次分析支持，后者必须由版本兼容策略决定。public 方法、甚至旧生成 DLL 引用的 internal helper，都不能仅凭仓库无调用直接认定二进制兼容。

## 审计方法与证据边界

1. 阅读全部 Ops 和元数据实现，搜索 `src`、`tests`、`benchmark`、`language-tools` 中的引用。
2. 扫描 Release 编译产物的 IL 成员引用，排除元数据字段自身的初始化写入；解析 MethodInfo 字段和方法数组，保留间接发射关联。
3. 对编译器私有方法做从非私有方法/外部调用方出发的保守可达性传播，识别互相引用但没有入口的子图，再回查源码与反射构造方式。
4. 人工分析固定参数、模式门槛、StackValueKind/FlowValueType、类型检查和实际发射路径。映射表里有分支不等于分支有入口。
5. 通过临时脚本编译探针检查生成 IL。探针、审计工具均在临时目录，没有加入生产项目。
6. 分别加载 net8.0、net9.0、net10.0 编译产物，三者均得到 243 字段、249 Ops 方法、22 无有效消费者字段、7 个不可达私有编译器方法。这是 IL 审计，不是用 .NET 8/9 运行时执行测试。

[成员引用原始清单](ops-reference-audit.json) 包含全部字段与方法签名、直接 IL 调用方、元数据关联以及不可达清单。`Users: []` 本身不是删除结论，尤其是 packed 边界和按参数个数注册的方法。

以下完整候选表覆盖本次范围内发现的删除、合并、性能优化和架构收缩项目，不代表仓库外调用可被穷尽，也不把尚未测量的优化写成确定收益。

## A. 无效路径与可连带删除项

### A01：11 个直接没有消费者的元数据字段

位置：`src/Compiler/Backend/Code/TypedRuntimeMetadata.cs`。

| 字段 | 行号 | 删除字段后，对目标方法的处理 |
| --- | ---: | --- |
| `TryToInteger` | 77 | 仅删字段；`ScriptDatum.TryToInteger` 广泛使用，必须保留 |
| `CompoundAddElementNumber` | 144 | 可连同 ObjectOps 方法处理，见 A03 |
| `ChangeElementNumber` | 146 | 同上 |
| `CompoundAddElementIndex` | 149 | 同上 |
| `ChangeElementIndex` | 150 | 同上 |
| `GetUserState` | 178 | 可连同 ScopeOps 方法处理，见 A04 |
| `BindTypedDocument` | 195 | 旧无路径插值入口，见 A06 |
| `ScriptObjectGetProperty` | 210 | 仅删字段；`GetPropertyDatum` 是核心虚方法，不能删除 |
| `ScriptObjectCopyProperties` | 212 | 仅删字段；`CopyPropertysFrom` 仍由运行时使用 |
| `ToExactInt64Number` | 240 | 旧 64 位数组转 Number 链，见 A07 |
| `ToExactUInt64Number` | 241 | 同上 |

收益：减少首次元数据初始化的反射与维护负担；不要将其宣传为脚本循环加速。

### A02：不可达的条件发射子图及其 11 个 Ops

位置：`src/Compiler/Backend/Emission/ModuleInitializerEmitter.cs`。

| 私有方法 | 行号 | 调用关系 |
| --- | ---: | --- |
| `EmitCondition` | 448 | 调用 `TryEmitCondition`；自身仅被本组调用 |
| `TryEmitCondition` | 576 | 调用自身、`EmitCondition`、下面两个 Try 方法 |
| `TryEmitLiteralCondition` | 609 | 仅由 `TryEmitCondition` 调用 |
| `TryEmitBinaryCondition` | 630 | 调用 `EmitCondition` 和 `GetBinaryConditionMethod` |
| `GetBinaryConditionMethod` | 2100 | 仅由 `TryEmitBinaryCondition` 调用 |

这组方法没有从 `TryEmit`、`EmitModuleStatement`、`EmitExpression` 等实际入口进入的边。

当前 `EmitLogical`（858 行）调用的是 `EmitExpression` 加 `ToBooleanDatum`，保留逻辑运算的原始结果；一元 `!` 由普通一元发射处理。因此不能仅因为存在 `GetBinaryConditionMethod` 就认为其所有 Ops 仍可达。

可连带处理下列 **11 个 ValueOps 方法和同名 TypedRuntimeMetadata 字段**：

| 方法 | ValueOps.cs 行号 |
| --- | ---: |
| `AddBoolean` | 127 |
| `SubtractBoolean` | 247 |
| `MultiplyBoolean` | 283 |
| `DivideBoolean` | 319 |
| `ModuloBoolean` | 424 |
| `BitwiseAndBoolean` | 645 |
| `BitwiseOrBoolean` | 686 |
| `BitwiseXorBoolean` | 728 |
| `LeftShiftBoolean` | 764 |
| `RightShiftBoolean` | 799 |
| `UnsignedRightShiftBoolean` | 836 |

仓库中没有其他源调用，TypedCilEmitter 也不使用它们。临时探针的模块 `!(a+b)`、`!(a<<b)` 等生成的是 `Add/LeftShift` 加 `Not`，没有生成这些 Boolean 版本。

注意：**不能顺手删除 `EqualBoolean/NotEqualBoolean/LessBoolean/LessEqualBoolean/GreaterBoolean/GreaterEqualBoolean`**。它们还由 TypedCilEmitter 的 `GetComparison`（7751 行附近）使用，部分还有运行时调用。

风险：这些 ValueOps 方法是 public；删除源实现前需要决定旧 DLL/外部 C# 调用是否继续支持。也可以只停止登记并保留兼容壳。

### A03：4 个已经被展开式发射替代的元素更新 helper

位置：`src/Runtime/ObjectOps.cs:199,210,234,247`。

- `CompoundAddElementNumber(ScriptDatum,double,ScriptDatum)`
- `CompoundAddElementIndex(ScriptDatum,int,ScriptDatum)`
- `ChangeElementNumber(ScriptDatum,double,double,bool)`
- `ChangeElementIndex(ScriptDatum,int,double,bool)`

除了 A01 的元数据登记，没有当前调用。TypedCilEmitter 已自行生成读取、`ValueOps.Add/ChangeByOne`、写入和前后缀返回值选择。

探针确认：`updateInt$typed` 使用 `GetElementIndex/SetElementIndex`；`updateNumber$typed` 使用 `GetElementNumber/SetElementNumber`，两者都不调用以上 helper。

**保留** Datum 索引版本 `CompoundAddElement` 和 `ChangeElement`：ModuleInitializerEmitter 在 936、959、1005 行仍使用它们。

### A04：旧 UserState Datum 包装入口

位置：`src/Runtime/ScopeOps.cs:36`，`GetUserState(ScriptContext)`。

当前上下文读取使用 `GetUserStateObject` 或直接 `ContextUserState` 字段加 Datum 转换。只有 A01 的失效字段指向旧入口。可与该字段一起清理；不能删除 `GetUserStateObject`。

### A05：无调用的反射工具

位置：`TypedRuntimeMetadata.cs:330`，`StaticMethod(Type,string,params Type[])`。

整个源码与编译器 IL 均没有调用；当前分别使用 `Method`、`InstanceMethod`、`Constructor` 和 `Field`。可直接删除该私有方法。

### A06：旧 TDoc 插值入口

位置：`src/Runtime/Serialization/TypedDocumentBinder.cs:16`，`BindInterpolation(context,typeName,value)`。

仅有 A01 的失效字段登记。当前发射使用 `BindInterpolationAtPath`，包含根路径和嵌套元素路径信息。可删除旧入口及字段，不应删除共享 `Bind` 或带路径入口。

### A07：旧 Int64/UInt64 packed 元素转 Number 链

位置：`src/Runtime/Types/ScriptPackedArray.cs:249,264,276,280`。

- `ToExactInt64Number(long,int)`
- `ToExactUInt64Number(ulong,int)`
- `ToExactNumber(long,string,int)`
- `ToExactNumber(ulong,string,int)`

两个外层方法仅有失效元数据引用，两个内层重载只被它们调用。当前 Int64/UInt64 数组读取分别创建 `FromInt64/FromUInt64`（696、729 行），不再强制经过 double 精度检查。

这是比较明确的旧数值表示遗留。public 外层方法仍需兼容性决策；仅删除本链，不删除其他精确类型检查或真实的 Number 转换。

### A08：模块初始化器中无调用的旧构造发射方法

位置：`ModuleInitializerEmitter.cs:1541`，`EmitTypedGlobalConstructor(string,Expression)`。

方法无入口，可以删除。**TypedCilEmitter.cs:3203 的同名方法仍有调用，不能按方法名全局删除。** 本方法使用的 `GetGlobal/New1` 也仍被其他路径使用。

### A09：恒为 false 的选项及徒劳扫描

位置：`TypedCilEmitter.cs:8543`，`TypedSubsetValidator.CanEmit(..., requireNativeLocal, ...)`。

全部两个调用点（184、242 行附近）都传 `requireNativeLocal:false`。但方法仍先扫描所有局部变量、计算 `hasNativeLocal`，再判断一个永不成立的开关。

可删除该参数、扫描循环和拒绝分支。这既减少代码，也减少编译器候选固定点迭代中的工作。不是删除 `directMode` 或 `allowRuntimeBoundaryInDirectMode`，后两者有真实模式差异。

### A10：相邻编译器中的失效便利入口

| 位置 | 可清理项 | 不能连带删除的内容 |
| --- | --- | --- |
| `ClosureMaterializer.cs:21` | `CanPlanMaterialize(FunctionPlan,bool)` | `CanMaterialize` 仍用来实际创建闭包 |
| `ModuleEmitter.cs:16` | `Emit(ModulePlan)` 单阶段便利重载 | `Prepare` + `Emit(ModuleEmissionState)` 是真实两阶段链 |
| `ModuleEmitter.cs:46` | `EmitWithoutReport(ModulePlan)` 便利重载 | 状态参数重载仍在生产链使用 |
| `TypedFunctionCode.cs:541` | `GetLocalStructuralType(LocalSlotId)` | 分析器的结构类型推导仍有效 |

最后一项可进一步移除最终 `TypedFunctionCode.LocalStructuralTypes` 属性及构造参数中的无消费者引用。**不得删除 TypeAnalyzer 的 `_localStructuralTypes` 数组**：它在推导、分支快照和合流中仍有大量使用。这里只能减少结果对象保留的引用，不能声称省掉推导期间数组分配。

### A11：GetElement 的不可成功回退分支

位置：`ObjectOps.cs:81` 附近，`indexedFallback` 判断。

前面已经检查了 `receiver.Reference is IAuroraNativeIndexer` 并尝试同一个索引转换。若成功已返回，若失败，后面的同样转换不会凭空成功。`ScriptDatum.ToObject` 对已有 ScriptObject 返回相同对象，而新物化的 sealed 原始值包装类都不实现 IAuroraNativeIndexer。

因此按当前类型不变量，该 fallback 的索引器成功分支不可达，可移除重复接口检查和转换，直接进入属性读取。仍须保留最终属性读取；字符串和非整数索引不是无效用例。

## B. 可合并或可消除的转发层

这些项大多能减少维护面，但包装方法已经可能被 JIT 内联，删除不自动等于更快。

| ID | 项目与位置 | 方案 | 前提/风险 |
| --- | --- | --- | --- |
| B01 | `TypedRuntimeMetadata.StringConcat` / `StringConcat2`（82/281 行），`StringConcatThree` / `StringConcat3`（86/282 行） | 各保留一个规范字段，替换消费者，减少两个重复登记 | 绑定的是完全相同的 string.Concat 重载；纯编译器清理 |
| B02 | `ValueOps.ToBoolean(ScriptDatum)`（56 行）与 `ScriptDatum.IsTrue` | 统一到后者，或保留 public 壳并转发 | 两者当前 Datum 语义相同；还能让编译代码受益于前次编码真值优化。不要合并 object 重载 |
| B03 | `ValueOps.TypeOf`（929 行） | 元数据改绑 `ScriptDatum.TypeOf`，旧方法删除或留壳 | 参数、返回值相同；有实际消费者，不是死方法 |
| B04 | `ValueOps.TryToNumber`（37 行） | 可将编译器改为直接调用 ScriptDatum 版本 | 第一个参数从 by-value 变为 `in`，必须修改 `ldloca/ldarga` 发射，不能只换 MethodInfo |
| B05 | `ClosureOps.Resolve/Resolve0..7` | 直接登记 `DynamicMethodRegistry.Resolve/Resolve0..7` | 可以删除 9 个纯壳，但注册表生命周期与错误处理不能删除 |
| B06 | `IterationOps.MoveNext/GetEnumerator` | 分别直接调用 `NextValue`，以及发射 `ToObject + GetEnumerator` | 调用指令和接收者栈需调整；枚举语义不能删除 |
| B07 | `CallFrameOps.EnterModule/Leave` | 编译器直接调用 ScriptContext 对应方法 | 目标方法为 internal；处理发射访问权限和旧二进制，不能删除 frame 状态管理 |
| B08 | `ScopeOps.GetModule/SetModule/GetGlobal/SetGlobal/GetGlobalObject/GetUserStateObject` | 用统一发射模板生成字段读取、属性方法调用和返回值 | 是当前有效边界。赋值返回原值、context 和 UserState null 语义必须保留；主要收益是减少壳，不一定减少 IL |
| B09 | `ValueOps.Equal/NotEqual/Less/LessEqual/Greater/GreaterEqual` 的 Datum 返回壳 | 模块发射器调用 Boolean 版本后统一 FromBoolean | 六者当前都有模块发射消费者。可转移表示转换，不可直接删除，也不能改成 ScriptDatum.Equals |
| B10 | `ValueOps.AddStringLeft/AddStringRight/AddStringMiddle` | 统一 string 返回的 concat 核心，按需要在发射层打包 | 保留操作数顺序、ToStringForConcat 语义和三段拼接优化，避免删除后退回多次分配 |
| B11 | TypeCheck 的 CheckX、CheckXValue、CheckXNumber 系列 | 共享最小的值验证核心，保留不同返回表示入口 | 三类不是重复 API：返回 Datum、原生整数、接收原生 double 各有作用；重建 Datum 可能丢失负零或类型信息 |
| B12 | `TypeCheckOps.Mismatch`、`MismatchNumber/MismatchUInt32Number`，packed `Reject` | 统一明确不返回的冷错误入口，适当用 DoesNotReturn 消除假返回值 | 冷路径代码整洁收益为主；保持错误文本和参数，不把构造错误所需的 Datum 移到热路径 |
| B13 | packed 边界 11 类型 x 5 类转换的模板重复 | 用类型描述表/源生成维护重复方法 | 推荐减少手写重复而保留专用入口，不建议每次访问改成 enum 分派或动态反射 |

## C. 仍然可达的性能优化项

### C01：消除 packed 边界命中路径的捕获对象分配

位置：`PackedArrayBoundaryOps.cs:354` 的 `Remember<TStorage>`、367 行的 `Box<TStorage,TWrapper>`。

`_ => wrapper` 和 `_ => created` 的捕获对象在当前 Release IL 中均位于 `IL_0000 newobj`，早于 null 检查和缓存命中检查。源码中 lambda 位于慢路径，并不代表其捕获对象也只在慢路径创建。

本机 .NET 10 Release，先预热 30,000 次，再分别执行 100,000 次已登记 Int32 存储转换，以 `GC.GetAllocatedBytesForCurrentThread` 测量：

| 路径 | 实测托管分配 |
| --- | ---: |
| `ToInt32Storage(ScriptInt32Array)` 缓存命中 | 24 B/次 |
| `FromInt32Storage(int[])` 缓存命中 | 24 B/次 |

方案：让 null/命中路径保持无捕获，注册/创建移入独立冷方法；或采用合适的无捕获注册 API。需要再次测量吞吐、分配与并发行为。这是本次优先级最高的运行时优化，不是纯代码风格调整。

### C02：减少 CWT 查找和竞态下的无用包装对象

位置：同 C01。

`Remember` 未命中时先 `TryGetValue` 再 `GetValue`；`Box` 在 `GetValue` 前先构造 wrapper，竞争时可能构造后又丢弃。可优化登记策略或延迟真正的 factory 执行。

**不能删除 ConditionalWeakTable 或每次新建 wrapper**：当前用它保留对象身份和附加属性。替代设计要验证并发、可回收性、同一存储回到动态世界时仍是原对象，不能换成强引用全局字典。

### C03：减少 packed 断言和边界转换重复检查

位置：`PackedArrayBoundaryOps.cs:17,94,171` 起的三组方法，`TypedCilEmitter.cs:590,2294,6791,6839` 等调用点。

`ToXArray(ScriptDatum)` 先检查 null，再调用本身也检查 null/类型的 `CheckXArray`，随后经 `.Object` 又走兼容取对象逻辑和 cast。可以把“严格检查 + 提取精确 wrapper”统一成一次检查，已验证的路径直接读 `Reference`。

`ToXStorage(ScriptDatum)` 当前采用直接 cast，与 checked wrapper 路径的错误行为不完全相同。优化前区分“入口已检查”和“输入动态未知”，不能悄悄删除检查或改变错误类型。

### C04：缓存 packed MethodInfo，减少编译期间重复反射

位置：`TypedRuntimeMetadata.cs:268`；`TypedCilEmitter.cs:7565,7574,7583`。

每次取 packed 边界方法都拼接名称、创建参数类型数组并执行 `GetMethod`。11 类型和输入表示的组合是有限集合，可预建/按需缓存不可变表。

这是编译性能优化，不是每次脚本元素访问的运行时反射。与 B13 可以共用描述表，但无需为此引入复杂反射框架。

### C05：避免数值型 TypeCheck 的重复解码/转换

位置：`TypeCheckOps.CheckUInt32Value/CheckUInt32Number/CheckInt64/CheckInt64Value/CheckUInt64/CheckUInt64Value`。

缓存一次 Number 读取，验证时保留已转换值，减少验证通过后再次转换。`IsUInt32` 可评估与 Int32 类似的 round-trip 判断。

对于 Int64/UInt64，不能机械套用“转换回来相等”：double 的 2^63、2^64 边界和不同 .NET 版本的转换规则需要保留显式上界。负零、NaN、无穷、分数均需回归。没有汇编/基准之前，不声称 JIT 当前一定执行了全部重复表达式。

### C06：ValueOps 热路径的重复 Kind 检查与强制大内联

位置：`ValueOps.Add/Subtract/Multiply/Divide/Modulo`、位运算、`EqualBoolean/TryCompareInteger64`。

缓存操作数种类；将常见 Number 和同种精确整数作为小热路径，字符串拼接、对象值相等和错误转换留在慢路径。避免把完整动态分派复制到每个生成调用点。

`AggressiveInlining` 只是提示。应比较热代码尺寸、Tier1 汇编、Number/整数/字符串混合输入，而不是简单删除全部标记或认为所有大方法都会内联。

### C07：统一算术快转换，但保留不同 coercion 语义

位置：`ValueOps.TryToArithmeticNumber/ToArithmeticNumber`（17/30 行）、`AddToNumberLeft/Right`（156/169 行）。

当前算术转换先查 null，再进入 ScriptDatum 数值转换；混合加法退回时还可能创建 Datum，再 Add，再转换结果。可为常见已知 Number/Boolean/精确整数设计短路径。

不能把算术转换与比较转换简单合并：这里 null 在算术中是 0，但 `ScriptDatum.TryToNumber(null)` 失败。加法还可能拼接字符串或对象字符串，不能一律改为浮点加法。

### C08：动态写元素直接走 Reference 快路径

位置：`ObjectOps.SetElement`（133 行）。

读取已经先检查 `Reference`；动态写入却先完整 `ScriptDatum.ToObject`，之后才判断原生索引器/packed 数组。可对这两类直接分派，仅其他类型进入兼容转换。

保留非数字索引走属性、原生自定义索引器、返回原赋值值等行为。不要删除 Number/Index 专用读写入口，它们仍被实际发射。

### C09：避免数字索引 fallback 的 Datum 往返和重复分派

位置：`ObjectOps.GetElementNumber/GetElementIndex/SetElementNumber/SetElementIndex`。

非数组属性 fallback 目前重建数字 Datum 再进入通用 helper，重复判断种类/接收者。可以直接走共享属性 fallback，保留等价的键字符串转换。

注意 Number 路径采用 `(int)double`，Datum 路径可能先 `(long)double` 再 `(int)`，对极端索引并不自动等价；类型、负零、文化格式和越界语义要单独确认，不借“去重”静默修改行为。

### C10：属性调用失败快路径的双重查找和参数复制

位置：`CallOps.InvokeProperty0..7/InvokePropertyMany` 及 `TryInvokeNative`（153 行）。

先 `TryResolveProperty` 找 descriptor；不是无绑定原生函数时，再 `GetPropertyValue` 查一次。1..7 参数版本还先填内联缓冲，回退时又传一遍逐个参数。

可将解析结果作为共享调用结果继续使用，或先确定调用类型再组织参数。保持 getter 只执行一次、绑定 this、属性覆盖、原型链，以及派生类虚拟 `GetPropertyDatum` 行为；不能不经验证直接用 descriptor 替代所有虚调用。

### C11：数组展开的批量复制

位置：`ObjectOps.SpreadInto`（302 行）、`CallOps.AppendSpread`（177 行）。

普通 ScriptArray 已知连续存储和长度，可一次确认容量后用 Span/Array.Copy，避免循环 GetElement/Push 和重复计数更新。packed 元素仍需要逐个构造 Datum，可只合并容量与目标索引管理。

需保留展开顺序、非数组值 fallback、异常时参数池清理、溢出检查和别名行为。数组字面量初始化路径不能与动态 spread 混为一谈。

### C12：Includes 的数组快路径与字符路径

位置：`ObjectOps.Includes`（326 行附近）。

普通数组/packed 数组可直接遍历存储，避免 `ToObject + GetEnumerator`；单字符字符串可减少重复 Datum 构造。

这里使用的是 `ScriptDatum.Equals`，并包含数字字符串弱相等。不能直接替换为 `ValueOps.EqualBoolean`、纯位比较或简单 Contains(char)，否则可能改变 Date/Path/StringBuffer、数字字符串和空字符串结果。

### C13：元数据初始化按需化

位置：`TypedRuntimeMetadata` 静态初始化。

目前任何首次使用都初始化整张 243 字段表，包括不一定用到的 TDoc、packed、闭包等反射信息。可按子功能拆分嵌套静态表或使用有限的惰性缓存。

先做 A01/A02/B01，再测冷编译启动时间。不要把每次已缓存的字段读取换成 Dictionary/Lazy 调用，以免热编译路径更慢。

## D. 可收缩架构，但不建议当作直接删除

| ID | 项目 | 收缩方向 | 为什么不能直接删 |
| --- | --- | --- | --- |
| D01 | ModuleInitializerEmitter 与 TypedCilEmitter 两套表达式发射 | 让模块初始化复用有明确上下文的共同表达式后端，逐步减少 Datum 专用算子壳 | 模块导入/导出顺序、声明、上下文、TDoc 路径、参数池 finally、符号绑定均不同；不能一次替换 2161 行 |
| D02 | PackedArrayBoundaryOps 的 raw-storage/wrapper 双表示 | 选择规范边界表示，降低跨边界次数 | 当前原生 ABI 利用 CLR 数组，动态世界依赖 wrapper 身份和对象属性；55 个入口不是历史无效 API |
| D03 | ExceptionOps 的 finally 控制流信号 | 用控制转移状态和外围分派代替正常 return/break/continue 的异常分配/展开 | CLR finally 不能简单 ret/leave；嵌套 finally、覆盖 return、catch 不能吞控制信号，改动涉及整体异常降低 |
| D04 | public Ops/历史生成 DLL 的兼容面 | 明确“只支持重新编译”或保留小型版本兼容层，必要时停止公开新 helper | EditorBrowsable(Never) 不等于 internal；当前 public TypeCheckOps/ValueOps/CallFrameOps/PackedArrayBoundaryOps 可被仓库外调用 |
| D05 | packed 类型映射散落多个 switch | 用同一有限类型描述表生成元数据、类型映射和专用方法 | 是编译期维护优化；不要把脚本执行时的直接调用变为反射/通用装箱分派 |

## 明确不能按“兼容垃圾”删除的项目

| 项目 | 保留理由 |
| --- | --- |
| `TypeCheckOps.Check` 总分派 | `PackedArrayBoundaryOps.Reject` 有真实使用；外部也是 public。可改错误路径设计后再评估，而非现状无调用 |
| `CheckInt32/UInt32/Int64/UInt64` 的 Datum/Value/Number 形态 | 对应不同输入/输出 ABI；64 位 Check 还会归一化 ValueKind，不是纯验证 |
| packed `CheckXArray` 的 null 放行 | 原生 CLR 数组表示允许 null，现有边界身份/空值测试依赖它；与普通 `CheckArray` 拒绝 null 不矛盾 |
| `ValueOps.EqualBoolean` 与 `ScriptDatum.Equals` | 前者支持 ScriptObject.HasValueEquality/ValueEquals，后者对对象主要按引用相等；Date、Path、StringBuffer 有值相等实现 |
| 三种 `ToBoolean` | Datum 可合并到 IsTrue；double 必须处理 NaN；ScriptObject 走虚拟 IsTrue，不可一律用非 null 判断 |
| `ToStringForConcat` 与 `ScriptDatum.ToString` | 默认分支取 Reference 与 Object、null fallback 有差异，兼容属性构造出的值也可能触发不同结果 |
| `ModuloInt32/UInt32/Int64/UInt64`、`DivideInt64/UInt64` | 零除错误和 MinValue/-1 边界是语义要求，不可直接以裸 div/rem 替换 |
| `CallOps.Invoke0..7` / `InvokeProperty0..7` | metadata 数组动态按 arity 注册；用于避免参数数组分配，不能只看具名调用次数 |
| `RentArguments/AppendArgument/AppendSpread/ReturnArguments` | 多参数和 spread 仍需此协议；finally 清理防止池数组保留引用 |
| `CallFrameOps.GetArgument/GetArgumentOrDefault` | 缺参语义与默认值语义有效，不能换成无检查 arguments[index] |
| `ExceptionOps.PrepareCatch` | 阻止脚本 catch 吞掉 finally 的内部控制转移信号；信号机制未整体替换前必须保留 |
| `_directMode == false`、FunctionEmitter 的 `_typed == null` | 泛型/Dynamic 路径及只做诊断、不发射执行代码的模式存在，不能按主生产入口总开启优化推断不可达 |
| `EmissionSession.Emit()`、简化版 TypedFunctionBuilder/TypedModuleCode.Build | 生产主路径可能不直接调用，但大量 CompilerBackendPlanTests 使用；属于测试/诊断入口，不是仓库无人使用 |
| NativeDirectCallSignatureValidator 禁止部分 packed 操作 | 只限制自动原生 direct 候选；显式 native 和 generic/dynamic 路径仍可能使用相关 Ops，不能全局裁剪 |

## 全部 Ops 覆盖表

方法数含 public/internal/private 显式方法，不含编译器生成 lambda 方法和类型初始化器。

| 类 | 行数 | 方法数 | 当前链无调用可清理 | 其余处理 |
| --- | ---: | ---: | ---: | --- |
| CallFrameOps | 43 | 4 | 0 | B07；GetArgument 系列保留 |
| CallOps | 271 | 29 | 0 | C10/C11；调用与池协议保留 |
| ClosureOps | 44 | 9 | 0 | B05 可重定向后整体去壳 |
| ExceptionOps | 100 | 8 | 0 | D03；当前控制流均有效 |
| IterationOps | 16 | 2 | 0 | B06 可去壳，枚举协议保留 |
| ObjectOps | 426 | 28 | 4 | A03/A11、C08/C09/C11/C12 |
| PackedArrayBoundaryOps | 385 | 58 | 0 | 55 个有效专用入口 + Reject/Remember/Box；B13、C01-C04 |
| ScopeOps | 43 | 7 | 1 | A04/B08 |
| TypeCheckOps | 501 | 38 | 0 | B11/B12/C05；文件还含 CheckedType 枚举 |
| ValueOps | 935 | 66 | 11 | A02/B02-B04/B09/B10/C06/C07 |
| 合计 | 2764 | 249 | 16 | 其余 233 个有生产链、内部共享或动态发射使用依据 |

packed 的 55 个公开入口精确分成五组，每组覆盖 Int32、Int8、Float32、Float64、Boolean、UInt8、Int16、UInt16、UInt32、Int64、UInt64：

1. `ToXStorage(ScriptDatum)`：native 参数 shell、direct 表达式边界。
2. `ToXStorage(ScriptXArray)`：已知 wrapper 提取存储并登记身份。
3. `ToXArray(ScriptDatum)`：generic 表示/动态值转 wrapper。
4. `ToXArray(ScriptObject)`：Object 栈表示窄化。
5. `FromXStorage(T[])`：ConvertToDatum 的 11 个 buffer 分支。

`TypedRuntimeMetadata` 另有 420 行、243 字段和 11 方法；22 字段可随 A01/A02 清理，2 个重复字段可按 B01 合并，1 个私有方法可按 A05 删除。

## 建议审批顺序

1. **纯清理包**：A01-A10、B01。先删除编译器失效登记/不可达子图；public 或旧 DLL 目标按选定兼容策略留壳或删除。A09 有额外编译性能收益。
2. **高价值低扩散性能包**：C01、B02、C04，再评估 C02/C05/C08/C11。
3. **去壳包**：B03-B10。重点是缩小维护面，不承诺运行吞吐收益。
4. **需较多语义验证包**：A11、B11-B13、C03、C06/C07/C09/C10/C12/C13。
5. **架构包**：D01-D05，单独设计与基准，不与第一批清理混在一个提交。

删除/改动前的验证矩阵：三种编译模式、Debug/Release、模块初始化与函数体、原生/动态 ABI、常量内联、缺参与默认值、闭包/热更新、typed checks、packed null/身份/属性、动态与精确整数运算、NaN/负零、嵌套 finally、getter/setter 次数、参数池异常清理。是否增加“旧 DLL + 新 Runtime”测试取决于兼容性选择。

本轮仅进行了静态/IL 审计、临时编译探针和局部分配测量；没有因审计修改生产代码，也没有把上一轮完整测试结果当作未来删除实现后的验证。
