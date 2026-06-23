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
| CompileBlock                    | compile    |    13.323 μs |     0.8652 μs |   0.0474 μs |    13.289 μs |    13.377 μs |    13.302 μs |    1 |   1.1597 |   1.0986 |   18.56 KB |
| FullCompile_MultiModule         | compile    |   486.402 μs |   217.0591 μs |  11.8977 μs |   475.240 μs |   498.919 μs |   485.047 μs |    2 |   4.8828 |   1.4648 |   74.19 KB |
| FullCompile_SingleModule        | compile    | 7,577.394 μs |   820.7999 μs |  44.9908 μs | 7,530.687 μs | 7,620.445 μs | 7,581.049 μs |    3 | 265.6250 | 132.8125 | 4104.74 KB |
|                                 |            |              |               |             |              |              |              |      |          |          |            |
| EmitOnly_ParsedLargeModule      | emitter    | 6,101.443 μs | 4,899.3739 μs | 268.5512 μs | 5,796.427 μs | 6,302.363 μs | 6,205.541 μs |    1 | 164.0625 |  82.0313 | 2529.36 KB |
|                                 |            |              |               |             |              |              |              |      |          |          |            |
| LexerOnly_Small                 | lexer      |     3.709 μs |     0.6460 μs |   0.0354 μs |     3.678 μs |     3.748 μs |     3.702 μs |    1 |   0.1144 |        - |    1.77 KB |
| LexerOnly_StringsTemplatesRegex | lexer      |    59.254 μs |     0.6182 μs |   0.0339 μs |    59.219 μs |    59.287 μs |    59.258 μs |    2 |   1.5869 |   0.0610 |   25.13 KB |
| LexerOnly_UnicodeIdentifiers    | lexer      |    67.018 μs |     6.7903 μs |   0.3722 μs |    66.723 μs |    67.437 μs |    66.895 μs |    2 |   2.6855 |   0.2441 |   41.28 KB |
| LexerOnly_CommentsWhitespace    | lexer      |    96.661 μs |     6.8284 μs |   0.3743 μs |    96.432 μs |    97.092 μs |    96.457 μs |    3 |        - |        - |    1.85 KB |
| LexerOnly_Large                 | lexer      |   556.749 μs |   193.0395 μs |  10.5811 μs |   544.546 μs |   563.386 μs |   562.314 μs |    4 |   0.9766 |        - |   21.26 KB |
|                                 |            |              |               |             |              |              |              |      |          |          |            |
| ParseOnly_Small                 | parser     |     7.303 μs |     0.5086 μs |   0.0279 μs |     7.281 μs |     7.334 μs |     7.294 μs |    1 |   0.4425 |   0.0076 |    6.79 KB |
| ParseOnly_TemplateInterpolation | parser     |   152.201 μs |    20.0449 μs |   1.0987 μs |   151.553 μs |   153.469 μs |   151.580 μs |    2 |  26.8555 |   9.5215 |  412.59 KB |
| ParseOnly_Large                 | parser     |   810.549 μs |    57.3603 μs |   3.1441 μs |   807.574 μs |   813.838 μs |   810.233 μs |    3 | 101.5625 |  56.6406 | 1562.48 KB |
