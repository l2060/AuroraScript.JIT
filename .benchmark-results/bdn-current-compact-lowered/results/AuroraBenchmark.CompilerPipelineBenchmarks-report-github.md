```

BenchmarkDotNet v0.15.8, Windows 11 (10.0.26200.8655/25H2/2025Update/HudsonValley2)
13th Gen Intel Core i7-13700KF 3.40GHz, 1 CPU, 24 logical and 16 physical cores
.NET SDK 10.0.301
  [Host]   : .NET 10.0.9 (10.0.9, 10.0.926.27113), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.9 (10.0.9, 10.0.926.27113), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                          | Categories | Mean         | Error         | StdDev      | Min          | Max          | Median       | Rank | Gen0     | Gen1     | Allocated  |
|-------------------------------- |----------- |-------------:|--------------:|------------:|-------------:|-------------:|-------------:|-----:|---------:|---------:|-----------:|
| CompileBlock                    | compile    |    28.053 μs |     1.9501 μs |   0.1069 μs |    27.929 μs |    28.119 μs |    28.110 μs |    1 |   1.1292 |   1.1139 |   17.29 KB |
| FullCompile_MultiModule         | compile    |   516.656 μs |    98.4493 μs |   5.3963 μs |   511.263 μs |   522.056 μs |   516.650 μs |    2 |   4.3945 |   0.9766 |   67.63 KB |
| FullCompile_SingleModule        | compile    | 6,570.310 μs | 1,170.2690 μs |  64.1464 μs | 6,528.231 μs | 6,644.140 μs | 6,538.560 μs |    3 | 210.9375 | 101.5625 | 3239.04 KB |
|                                 |            |              |               |             |              |              |              |      |          |          |            |
| EmitOnly_ParsedLargeModule      | emitter    | 4,974.988 μs | 7,491.6943 μs | 410.6450 μs | 4,586.336 μs | 5,404.562 μs | 4,934.065 μs |    1 | 105.4688 |  50.7813 | 1663.66 KB |
|                                 |            |              |               |             |              |              |              |      |          |          |            |
| LexerOnly_Small                 | lexer      |     3.675 μs |     0.2331 μs |   0.0128 μs |     3.660 μs |     3.683 μs |     3.681 μs |    1 |   0.1144 |        - |    1.77 KB |
| LexerOnly_StringsTemplatesRegex | lexer      |   106.544 μs |     6.3349 μs |   0.3472 μs |   106.146 μs |   106.784 μs |   106.701 μs |    2 |   1.5869 |   0.0610 |   25.13 KB |
| LexerOnly_UnicodeIdentifiers    | lexer      |   118.646 μs |    35.9335 μs |   1.9696 μs |   116.373 μs |   119.852 μs |   119.714 μs |    2 |   2.6855 |   0.2441 |   41.28 KB |
| LexerOnly_CommentsWhitespace    | lexer      |   184.551 μs |    11.7968 μs |   0.6466 μs |   183.956 μs |   185.239 μs |   184.457 μs |    3 |        - |        - |    1.85 KB |
| LexerOnly_Large                 | lexer      |   557.634 μs |   104.3988 μs |   5.7225 μs |   551.513 μs |   562.850 μs |   558.539 μs |    4 |   0.9766 |        - |   21.26 KB |
|                                 |            |              |               |             |              |              |              |      |          |          |            |
| ParseOnly_Small                 | parser     |    13.389 μs |     2.5260 μs |   0.1385 μs |    13.285 μs |    13.546 μs |    13.335 μs |    1 |   0.4425 |   0.0076 |    6.79 KB |
| ParseOnly_TemplateInterpolation | parser     |   257.124 μs |    52.0006 μs |   2.8503 μs |   254.675 μs |   260.253 μs |   256.444 μs |    2 |  26.8555 |   9.5215 |  412.59 KB |
| ParseOnly_Large                 | parser     | 1,376.693 μs |   423.8042 μs |  23.2301 μs | 1,349.885 μs | 1,390.915 μs | 1,389.277 μs |    3 | 101.5625 |  56.6406 | 1562.48 KB |
