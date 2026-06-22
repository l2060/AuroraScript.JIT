# 编译管线重构性能对比

## 测量口径

- 环境：Windows 11、Intel Xeon W-2235、.NET SDK 10.0.301、.NET 10.0.9 x64。
- 主对比：`--compare`，2 次预热后测量 8 次平均值，before/after 使用相同源码集。
- 正式统计：BenchmarkDotNet `ShortRun`，3 次预热、3 次实际迭代，启用 `MemoryDiagnoser`。
- before：`before/manual-compare.csv`。
- after：`after/manual-compare.csv` 与 `after/benchmarkdotnet-report.*`。

## Before / After

| Case | Before ms | After ms | 加速比 | Before allocated | After allocated | 分配下降 |
|---|---:|---:|---:|---:|---:|---:|
| CompileBlock | 0.113 | 0.056 | 2.02x | 82,314 B | 18,434 B | 77.61% |
| FullCompile_MultiModule | 1.100 | 0.736 | 1.49x | 208,129 B | 63,582 B | 69.45% |
| FullCompile_SingleModule | 36.041 | 9.064 | 3.98x | 7,381,675 B | 2,884,214 B | 60.93% |
| LexerOnly_CommentsWhitespace | 1.518 | 0.290 | 5.23x | 1,002,397 B | 2,007 B | 99.80% |
| LexerOnly_Large | 5.711 | 0.822 | 6.95x | 2,187,549 B | 21,887 B | 99.00% |
| LexerOnly_Small | 0.047 | 0.009 | 5.22x | 65,861 B | 1,927 B | 97.07% |
| LexerOnly_StringsTemplatesRegex | 0.305 | 0.149 | 2.05x | 463,373 B | 25,839 B | 94.42% |
| LexerOnly_UnicodeIdentifiers | 0.453 | 0.166 | 2.73x | 477,277 B | 40,383 B | 91.54% |
| ParseOnly_Large | 10.861 | 2.789 | 3.89x | 4,023,845 B | 1,568,743 B | 61.01% |
| ParseOnly_Small | 0.095 | 0.017 | 5.59x | 71,637 B | 7,215 B | 89.93% |
| ParseOnly_TemplateInterpolation | 12.314 | 0.580 | 21.23x | 9,359,053 B | 422,751 B | 95.48% |

`EmitOnly_ParsedLargeModule` 未包含在原始 before 基线中。Emitter 专项优化前的中间测量为 9.063 ms / 3,814,726 B，最终手工结果为 5.784 ms / 1,311,969 B，分别下降 36.18% 和 65.61%。最终 BDN 均值为 7.149 ms / 1,270.95 KB。

## BDN 最终结果

| Case | Mean | Median | Allocated |
|---|---:|---:|---:|
| FullCompile_SingleModule | 10.931 ms | 10.276 ms | 2,814.08 KB |
| EmitOnly_ParsedLargeModule | 7.149 ms | 7.180 ms | 1,270.95 KB |
| LexerOnly_Large | 846.891 us | 845.235 us | 21.38 KB |
| LexerOnly_CommentsWhitespace | 286.160 us | 287.737 us | 1.97 KB |
| ParseOnly_Large | 2.509 ms | 2.505 ms | 1,532.01 KB |
| ParseOnly_TemplateInterpolation | 488.871 us | 457.140 us | 412.88 KB |

## 验收

- Lexer 分配下降至少 80%：通过，所有 Lexer case 下降 91.54% 到 99.80%。
- Parser 大样本分配下降至少 50%：通过，下降 61.01%。
- FullCompile 分配下降至少 30%：通过，单模块下降 60.93%。
- FullCompile CPU 不回退：通过，单模块约 3.98x 加速。
- 语义回归：Release 全解决方案构建通过；Examples 脚本测试 12/12 通过，覆盖闭包、递归、模板、正则、模块和 CompileBlock。
