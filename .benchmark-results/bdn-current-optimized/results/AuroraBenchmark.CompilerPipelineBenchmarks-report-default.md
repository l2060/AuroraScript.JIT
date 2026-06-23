
BenchmarkDotNet v0.15.8, Windows 11 (10.0.26200.8655/25H2/2025Update/HudsonValley2)
13th Gen Intel Core i7-13700KF 3.40GHz, 1 CPU, 24 logical and 16 physical cores
.NET SDK 10.0.301
  [Host]   : .NET 10.0.9 (10.0.9, 10.0.926.27113), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.9 (10.0.9, 10.0.926.27113), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

 Method                          | Categories | Mean         | Error        | StdDev      | Min          | Max          | Median       | Rank | Gen0     | Gen1     | Allocated  |
-------------------------------- |----------- |-------------:|-------------:|------------:|-------------:|-------------:|-------------:|-----:|---------:|---------:|-----------:|
 CompileBlock                    | compile    |    46.641 μs |    13.663 μs |   0.7489 μs |    46.179 μs |    47.505 μs |    46.240 μs |    1 |   1.4648 |   0.3662 |    22.7 KB |
 FullCompile_MultiModule         | compile    |   568.033 μs |   682.125 μs |  37.3896 μs |   524.909 μs |   591.388 μs |   587.803 μs |    2 |   4.8828 |   0.9766 |   76.23 KB |
 FullCompile_SingleModule        | compile    | 8,323.745 μs | 1,994.796 μs | 109.3415 μs | 8,208.473 μs | 8,425.989 μs | 8,336.773 μs |    3 | 265.6250 | 125.0000 | 4206.85 KB |
                                 |            |              |              |             |              |              |              |      |          |          |            |
 EmitOnly_ParsedLargeModule      | emitter    | 7,307.486 μs | 8,308.755 μs | 455.4309 μs | 6,891.182 μs | 7,793.910 μs | 7,237.366 μs |    1 | 167.9688 |  82.0313 | 2631.47 KB |
                                 |            |              |              |             |              |              |              |      |          |          |            |
 LexerOnly_Small                 | lexer      |     3.657 μs |     1.024 μs |   0.0561 μs |     3.595 μs |     3.706 μs |     3.668 μs |    1 |   0.1144 |        - |    1.77 KB |
 LexerOnly_StringsTemplatesRegex | lexer      |   108.797 μs |    17.691 μs |   0.9697 μs |   107.682 μs |   109.442 μs |   109.269 μs |    2 |   1.5869 |   0.0610 |   25.13 KB |
 LexerOnly_UnicodeIdentifiers    | lexer      |   117.843 μs |    56.634 μs |   3.1043 μs |   115.443 μs |   121.349 μs |   116.738 μs |    2 |   2.6855 |   0.2441 |   41.28 KB |
 LexerOnly_CommentsWhitespace    | lexer      |   179.601 μs |   111.439 μs |   6.1083 μs |   172.677 μs |   184.230 μs |   181.895 μs |    3 |        - |        - |    1.85 KB |
 LexerOnly_Large                 | lexer      |   571.306 μs |   128.260 μs |   7.0304 μs |   563.477 μs |   577.079 μs |   573.361 μs |    4 |   0.9766 |        - |   21.26 KB |
                                 |            |              |              |             |              |              |              |      |          |          |            |
 ParseOnly_Small                 | parser     |     9.305 μs |    60.387 μs |   3.3100 μs |     7.278 μs |    13.125 μs |     7.513 μs |    1 |   0.4425 |   0.0076 |    6.79 KB |
 ParseOnly_TemplateInterpolation | parser     |   256.749 μs |    89.855 μs |   4.9253 μs |   253.038 μs |   262.337 μs |   254.873 μs |    2 |  26.8555 |   9.5215 |  412.59 KB |
 ParseOnly_Large                 | parser     | 1,355.280 μs |   266.720 μs |  14.6198 μs | 1,345.717 μs | 1,372.110 μs | 1,348.014 μs |    3 | 101.5625 |  56.6406 | 1562.48 KB |
