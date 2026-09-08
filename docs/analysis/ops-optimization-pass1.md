# Ops 第一批优化

基线：`bc1e422`，已推送至 `origin/develop`。本批保留现有 Runtime 入口签名，不做破坏性的旧 DLL/API 删除。

后续进度见 [第二批优化记录](ops-optimization-pass2.md)。

## 已处理

| 审计项 | 实施情况 |
| --- | --- |
| A01 | 删除 11 个无消费者的元数据字段，保留其仍有效或可能被旧 DLL 引用的 Runtime 目标 |
| A02 | 删除 5 个不可达条件发射方法和 11 个关联字段；11 个 public Boolean 方法改为共享算子加 IsTrue 的兼容壳 |
| A05 | 删除无调用的私有 StaticMethod 反射工具 |
| A08 | 删除 ModuleInitializerEmitter 中无调用的 EmitTypedGlobalConstructor；TypedCilEmitter 同名方法不变 |
| A09 | 删除恒为 false 的 requireNativeLocal 参数及局部变量扫描 |
| A10 | 删除无调用的 CanPlanMaterialize、两个 ModulePlan 便利重载、最终 TypedFunctionCode 中无消费者的 LocalStructuralTypes 引用和访问器；保留分析器内部结构类型数组 |
| B01 | 合并两组重复 string.Concat 元数据字段 |
| B02 | ValueOps.ToBoolean(ScriptDatum) 统一转发 ScriptDatum.IsTrue；double/Object 重载语义不变 |
| C01 | Remember/Box 的捕获 lambda 移入 NoInlining 冷方法；null 和缓存命中路径不再创建捕获对象 |
| C04 | 使用类型、输入表示和转换方向组成的键缓存 packed MethodInfo；命中时不拼接方法名、不创建参数 Type[]、不反射查找 |

元数据主表从 243 个字段降到 219 个，55 个 packed 转换组合按需缓存。并发首次命中允许重复解析，但最终发布同一个缓存结果。

## 验证

- 新增 PackedBoundaryOptimizationTests：遍历全部 11 种 packed 类型、55 种边界签名，验证空值、共享存储、对象身份和附加属性。
- 全部 11 种类型的 wrapper/storage 预热命中和 null 路径均验证为零托管分配；首次创建 wrapper 的必要分配没有消除。
- 新增并发创建一致性、ConditionalWeakTable 弱引用回收、元数据缓存命中零分配测试。
- ValueOpsCompatibilityTests 覆盖 11 个 Boolean 入口在 null、Boolean、Number 特殊值、精确 64 位整数、字符串和对象组合上的返回值及异常。此测试在缩壳前后均运行通过。
- Release、Debug .NET 10 完整运行时测试：各 1170 项通过。
- net8.0、net9.0、net10.0 Release 编译成功，无警告、无错误；本机缺少 .NET 8/9 运行时，未执行对应运行时测试。

性能证据仅支持预热边界路径从审计时 24 B/次降到 0 B/次，以及缓存命中不再分配；本批没有宣称整体运行吞吐或冷编译时间的百分比提升。

## 明确保留

- ObjectOps 四个历史更新入口、ScopeOps.GetUserState、TypedDocumentBinder.BindInterpolation、ScriptPackedArray.ToExactNumber 链仍保留，只有无效元数据登记被删除。
- public ValueOps Boolean 名称、调用签名和异常行为保留；不删除任何有效的比较 Boolean 方法。
- CWT 的身份规则和并发发布策略不变，C02 的未命中优化未实施。
- A11 及其余 TypeCheck、对象属性、参数池、控制流、ABI 架构优化暂未实施；不将本批视为全部 42 项完成。
- 审计报告和原始 JSON 保留为基线快照，不覆盖历史证据。
