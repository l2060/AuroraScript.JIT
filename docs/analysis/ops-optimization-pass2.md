# Ops 第二批优化

本批在第一批基础上实施；`bc1e422` 是开始 Ops 优化前的基线快照。

后续进度见 [第三批优化记录](ops-optimization-pass3.md)。

## 已处理

| 审计项 | 实施情况 |
| --- | --- |
| A11 | 删除 GetElement 中无法成功的 indexedFallback 分支，保留最终的属性读取 |
| C08 | SetElement 先检查 Reference 中的原生索引器/packed 数组，只有属性 fallback 才调用 ToObject |
| C09 | 数值索引的属性 fallback 不再构造 Datum、重入通用 Get/SetElement；int 和 double 各自格式化键，保持文化和 Number 负零语义 |
| C11 | 普通 ScriptArray 的数组展开复用 AddRange/Span.CopyTo；参数展开一次确认容量后批量复制；目标长度加法使用 checked |
| C05（部分） | Int64/UInt64 的 Number 输入只解码一次，复用局部数值完成检查和转换；保留精确整数路径和显式上界 |
| B12（部分） | Int64/UInt64 Number 检查的失败构造放入 NoInlining 冷方法，保持异常类型和消息 |

普通数组自展开现在按开始时的长度复制一次，修复旧循环随 target.Length 不断增长、最终可能耗尽内存的问题。测试覆盖有无扩容及空数组。正常脚本中创建新数组的 spread 仍保持原有求值顺序。

## 没有保留的尝试

- UInt32 round-trip 检查虽然语义测试通过，但本机 CheckUInt32Number 从 1.876 ns 退步到 2.438 ns，IsUInt32 从 1.215 ns 退步到 1.452 ns；已恢复原范围/Math.Truncate 实现。
- 把数字属性 fallback 强制设为 NoInlining 导致属性读写退步；最终使用可内联的专用 helper，并保留 int 键的原生格式化。
- 不能仅凭方法更短或分支更少判断性能。

## 基准结果

BenchmarkDotNet 0.15.8，.NET 10.0.11 x64，i7-13700KF，Windows 11。
每个版本使用相同输入：一次 launch，3 次 warmup，5 次 200 ms 采样。
基线包含第一批优化，但不含本批 Runtime 改动。

| 用例 | 基线 ns/操作 | 最终 ns/操作 | 解读 |
| --- | ---: | ---: | --- |
| WriteDynamicArray | 6.655 | 5.081 | 耗时下降约 24% |
| ReadNumberProperty | 76.773 | 71.590 | 本次下降约 7%，字符串分配仍为 32 B/次 |
| WriteIntProperty | 86.157 | 34.251 | 本次下降约 60%，键 7 的分配由 24 B/次变为 0 |
| ArraySpread | 2.534 | 0.611 | 按元素折算，耗时下降约 76% |
| ArgumentSpread | 2.251 | 0.372 | 按元素折算，耗时下降约 83% |
| CheckInt64Datum | 3.088 | 2.896 | 本次下降约 6% |
| CheckUInt64Datum | 3.642 | 3.362 | 本次下降约 8% |
| CheckUInt32Number | 1.876 | 1.876 | 已恢复原实现，持平 |
| IsUInt32 | 1.215 | 1.218 | 已恢复原实现，持平 |

ReadDynamicArray 基线 8.769 ns，第一次候选 8.862 ns，最终 4.058 ns；读取实现未在两个候选之间直接修改却出现较大变化，因此不将此项作为稳定加速结论。需要多 launch 和更长采样另行验证。

Spread 每次处理 128 个元素，目标数组预留空间，ArraySpread 还包含重置目标长度和清空旧元素的成本。结果是批处理吞吐，不是单次独立元素操作延迟。

整数键的零分配仅针对本机 .NET 的小整数字符串缓存命中用例，不意味着任意 int 键都不分配。整个应用、不同 CPU、其他运行时或冷启动的收益不能据此直接推算。

原始结果与汇编：

- `benchmark/results/ops-pass2-baseline/results/`：本批开始前。
- `benchmark/results/ops-pass2-optimized/results/`：首次候选，包含后续撤回的退步方案。
- `benchmark/results/ops-pass2-final/results/`：最终保留方案。

```powershell
dotnet run -c Release --project benchmark/Benchmark.csproj -- --filter '*BoundaryOpsBenchmarks*' --job short --warmupCount 3 --iterationCount 5 --iterationTime 200 --launchCount 1 --artifacts benchmark/results/ops-pass2-final
```

## 语义验证范围

- ObjectOpsOptimizationTests 内保留旧索引实现作为 oracle，比较派发、返回值、异常类型/消息、getter/setter 次数和 context。
- 覆盖自定义原生索引器、普通/packed 数组、原始值 fallback、null/Boolean/字符串/精确 64 位索引、NaN/无穷/负零及越界值。
- 覆盖 invariant、fr-FR 和自定义符号文化，以及 512 个随机 Int32 键，验证 int 和旧 Number 格式化的一致性。
- TypeCheckOptimizationTests 覆盖特殊值、2^32/2^63/2^64 邻接值和随机 IEEE-754 位模式，检查原始谓词、归一化类型和错误消息。
- SpreadOptimizationTests 覆盖容量充足/扩容、空输入、引用身份、参数前缀、计数、自展开，以及三种编译模式下模块与函数的混合 spread、求值顺序和多参数动态调用。

本批新增 17 个测试用例。最终 .NET 10 Release、Debug 全量测试各 1187 项通过；net8.0、net9.0、net10.0 Release 编译均成功，零警告、零错误。本机仅安装 .NET 10 运行时，未执行 .NET 8/9 运行时测试。

## 仍保留的边界

- Number 索引的 `(int)double` 与 Datum 索引的先转 long 再转 int 并不被统一，极端输入的既有差异保留。
- packed 展开继续逐元素处理，不改变可能抛错时的已写入内容；参数池所有权和清理协议不变。
- TypeCheck 的公开入口、精确 64 位归一化规则、负零拒绝、NaN/无穷拒绝不变。
- C02/C03、C06/C07、C10/C12/C13 及剩余架构项目仍未实施；本批不视为全部审计项完成。
