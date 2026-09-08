# Ops 第四批：保守收缩调用边界

本批接续前三批优化。只保留已经通过语义与生成 IL 验证的 B07、B10；B04、B06 的尝试已撤回。

## 最终保留

| 审计项 | 实施情况 | 收缩范围 |
| --- | --- | --- |
| B07 | 普通模块初始化和 HotPatch 初始化直接 callvirt ScriptContext.EnterModule/LeaveFrame | 不再经过 CallFrameOps 转发；仍调用完全相同的 frame 实现，保留原 finally 清理块 |
| B10 | 模块字符串加法调用 ConcatStringLeft/Right/Middle 后按需 FromString | 删除 3 个 AddString 元数据字段，新增 1 个 middle 核心字段，主表从 213 降到 211 |

新增的 ConcatStringMiddle 共享原 ToStringForConcat 规则；旧 AddStringMiddle 变为调用该核心的兼容壳。AddStringLeft/Right、CallFrameOps.EnterModule/Leave 等旧 Runtime 入口全部保留。

本批确定的收益是元数据和新生成调用链收缩，不宣称这些旧壳原本一定产生机器码调用开销，也不宣称整体吞吐提升。

## 已撤回的尝试

- B04：尝试直接调用 ScriptDatum.TryToNumber(in datum, out number)，参数缓存使用已有局部变量地址，部分原生比较增加临时 Datum 以保持求值顺序。语义测试通过，但基准未证明稳定收益，已恢复 ValueOps.TryToNumber 的原发射形式和原局部变量布局。
- B06：尝试直接调用 ScriptObject.GetEnumerator 和 ScriptEnumerator.NextValue。语义测试通过，但基准出现枚举性能风险，已恢复 IterationOps 原调用路径。
- 新增 IL 测试明确验证上述两个热路径仍绑定旧包装方法，防止只恢复元数据或只恢复调用指令。

## 基准与限制

GeneratedBoundaryBenchmarks 编译真实脚本并执行，每次函数调用内部进行 512 次比较/缓存运算/枚举。使用 .NET 10.0.11、i7-13700KF、Windows 11、BenchmarkDotNet 0.15.8。

| 阶段 | Comparison ns/迭代 | CachedNumber ns/迭代 | Iteration ns/元素 |
| --- | ---: | ---: | ---: |
| 本批前 | 56.774 | 1.466 | 2.575 |
| 包含 B04/B06 的候选 | 56.122 | 1.819 | 7.302 |
| 撤回 B04/B06 后 | 56.092 | 1.844 | 5.177 |

前两轮均为 2 个独立 launch、3 次 warmup、5 次 200 ms 采样。撤回后加长到 2 个 launch、5 次 warmup、8 次 300 ms 采样。

缓存基线和枚举复测出现明显多峰分布；撤回后枚举两组样本分别约 2.6 ns 和 7.8 ns，BenchmarkDotNet 也给出 MultimodalDistribution 警告。因此不能从均值认定 B06 是全部差异的原因，也不能将本批描述为性能提升。按照保守要求，未证明安全收益的热路径改动不保留。

分配按 512 次迭代归一化，报告中的小数舍入可能显示为 0；枚举仍保留原枚举器分配，不能据此声称整个函数零分配。

原始结果目录：

- `benchmark/results/ops-pass4-baseline/`：本批前。
- `benchmark/results/ops-pass4-final/`：初次候选，包含已撤回的 B04/B06；目录名不代表当前最终实现。
- `benchmark/results/ops-pass4-retained/`：当前保留实现。

```powershell
dotnet run -c Release --project benchmark/Benchmark.csproj -- --filter '*GeneratedBoundaryBenchmarks*' --job short --warmupCount 5 --iterationCount 8 --iterationTime 300 --launchCount 2 --artifacts benchmark/results/ops-pass4-retained
```

## 验证范围

新增 EmitterBoundaryProtocolTests，共 14 个用例：

- 三种编译模式下动态比较的 LR/RL 求值顺序，包括无法转数值时仍执行右操作数。
- 数值缓存的 Number、数字字符串、null、非法字符串更新，区分算术 null=0 与比较不可转换。
- 普通数组、packed 数组、字符串、自定义虚拟枚举器、null，以及循环中 continue/break/finally。
- 模块 prefix/suffix/middle 拼接、格式化副作用次序、Boolean 的原有 True 大小写。
- 三种模式的增量热补丁初始化。
- 持久化初始化成功/失败时恢复 Module、Target、UserState、Location，并释放兼容子 context。
- 生成 IL 验证直接 frame/concat 调用、数值和枚举热包装路径保留、旧 Runtime 入口仍存在。

最终 .NET 10 Release、Debug 全量测试各 1221 项通过；net8.0、net9.0、net10.0 Release 编译成功，零警告、零错误。本机没有 .NET 8/9 运行时，未执行对应运行时测试。

B04、B06 记为“尝试后撤回”，不记为完成；其他未实施项仍以原审计与前三批记录为准。
