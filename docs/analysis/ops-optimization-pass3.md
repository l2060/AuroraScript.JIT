# Ops 第三批：确定性收缩与已测收益

本批接续前两批优化。保留旧 Runtime 入口，不改变公开 ABI，不修改条件弱表并发策略或属性 getter 协议。

后续进度见 [第四批优化记录](ops-optimization-pass4.md)。

## 保留的改动

| 审计项 | 实施情况 | 确定的收益/收缩 |
| --- | --- | --- |
| B03 | typeof 元数据直接绑定 ScriptDatum.TypeOf | 新生成代码不再经过 ValueOps.TypeOf 转发；旧入口保留 |
| B05 | 9 个闭包解析元数据直接绑定 DynamicMethodRegistry | 新生成代码去掉 ClosureOps 转发边；注册、清理和旧入口不变 |
| B09 | 模块比较调用 bool 返回的核心方法，统一按需 FromBoolean | 删除 6 个重复元数据字段，主表从 219 降到 213；旧 Datum 比较入口保留 |
| C03（部分） | 11 个 ToXArray(ScriptDatum) 合并已检查 wrapper 的提取 | null 保持快路径；有效值减少重复类型检查和 Object 兼容取值；错误仍由原 TypeCheck 规则处理 |
| C12 | 普通/packed 数组直接遍历；字符串 needle 直接做 ordinal 查找 | 数组不创建枚举器；字符串查找不逐字符创建 Datum/字符串 |

B03/B05 的收益是确定地减少新调用链层级，不宣称已经内联的旧转发函数原本一定带来可测运行开销。

B09 的比较语义仍来自 ValueOps.EqualBoolean 等核心方法，不能换成 ScriptDatum.Equals。Date 等对象的值相等仍有效。

## 安全边界

- 数组 GetEnumerator 是 sealed；快路径仍只读取一次初始长度，并保留每个元素的访问顺序。
- Includes 仍使用 ScriptDatum.Equals，而不是 ValueOps 的对象值相等。数字字符串的弱相等保留。
- 空字符串 needle 仍返回 false；多字符 substring、单字符、UTF-16 代理项均保持 ordinal 语义。非字符串 needle 保留原逐字符比较路径。
- 自定义普通对象仍调用其虚拟 GetEnumerator。自定义 packed 元素访问的副作用、提前命中和异常顺序由旧实现 oracle 对照验证。
- C03 没有改动 ToXStorage(ScriptDatum) 或 ToXArray(ScriptObject)，避免改变它们的非法输入/异常行为。
- 冷 Reject(Datum) 使用原始 datum 做 TypeCheck，未先 ToObject，防止兼容构造值在归一化中改变错误信息。
- 未删除 ClosureOps、ValueOps.TypeOf 或六个 Datum 比较方法，旧生成 DLL 的入口仍在。

## 基准

BenchmarkDotNet 0.15.8，.NET 10.0.11 x64，i7-13700KF，Windows 11。
相同输入分别测量优化前后：单 launch、3 次 warmup、5 次 200 ms 采样。
以下是完整一次查找/转换的耗时，不是按元素折算。

| 用例 | 长度 | 基线 ns | 本批 ns | 分配变化 |
| --- | ---: | ---: | ---: | --- |
| ArrayMiss | 32 | 451.924 | 310.251 | 104 B -> 0 |
| ArrayMiss | 256 | 3937.617 | 2460.771 | 104 B -> 0 |
| PackedMiss | 32 | 425.052 | 349.282 | 104 B -> 0 |
| PackedMiss | 256 | 3274.756 | 2766.985 | 104 B -> 0 |
| CharacterMiss | 32 | 501.497 | 9.204 | 768 B -> 0 |
| CharacterMiss | 256 | 4216.553 | 16.314 | 6144 B -> 0 |
| CheckedWrapper | 32 参数组 | 3.375 | 2.076 | 均为 0 |
| CheckedWrapper | 256 参数组 | 3.152 | 2.041 | 均为 0 |

CharacterMiss 使用重复的非 ASCII 字符和不存在的单字符 needle，收益不能外推到所有字符或所有命中位置。CheckedWrapper 的参数组不改变转换输入，仅是同一基准类中的重复测量。

NullWrapper 接近空方法测量噪声，BenchmarkDotNet 给出 ZeroMeasurement 警告；只确认语义、零分配和 null-first 实现不变，不据此给出加速比例。基线部分耗时样本波动较大，因此本批主要依赖明确的分配消除及代码路径收缩，不宣传整体应用加速百分比。

原始报告和汇编位于 `benchmark/results/ops-pass3-baseline/results/` 与 `benchmark/results/ops-pass3-final/results/`。

```powershell
dotnet run -c Release --project benchmark/Benchmark.csproj -- --filter '*SafeOpsBenchmarks*' --job short --warmupCount 3 --iterationCount 5 --iterationTime 200 --launchCount 1 --artifacts benchmark/results/ops-pass3-final
```

## 验证

- IncludesOptimizationTests：旧实现 oracle、全部 11 种 packed 类型、引用/值相等差异、字符串弱相等、UTF-16、自定义枚举、packed 访问顺序与异常、快路径零分配。
- PackedBoundaryOptimizationTests：新增 11 个严格 wrapper 转换对照用例，覆盖正确类型、错误类型、null、原始值和兼容 Kind 构造值；错误类型、消息、对象身份不变。
- CompilerBoundarySimplificationTests：三种编译模式验证模块比较、复合赋值、短路、Date 值相等、typeof 与捕获闭包；持久化 IL 确认使用核心比较/typeof 入口。
- 元数据测试验证 9 个解析方法的目标与返回签名，并确认旧 Runtime 入口没有被删除。

本批新增 20 个测试用例。.NET 10 Release、Debug 全量测试各 1207 项通过；net8.0、net9.0、net10.0 Release 编译均成功，零警告、零错误。本机缺少 .NET 8/9 运行时，未执行对应运行时测试。

本批没有推进 B04/B06/B07/B08/B10/B11/B13、C02/C06/C07/C10/C13 或架构重构；C03 仅完成明确可等价替换的已检查 Datum-to-wrapper 路径。
