
BenchmarkDotNet v0.15.8, Windows 11 (10.0.26200.8655/25H2/2025Update/HudsonValley2)
13th Gen Intel Core i7-13700KF 3.40GHz, 1 CPU, 24 logical and 16 physical cores
.NET SDK 10.0.301
  [Host]   : .NET 10.0.9 (10.0.9, 10.0.926.27113), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.9 (10.0.9, 10.0.926.27113), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

 Method                          | Categories | Mean         | Error         | StdDev      | Min          | Max          | Median       | Rank | Gen0     | Gen1     | Allocated  |
-------------------------------- |----------- |-------------:|--------------:|------------:|-------------:|-------------:|-------------:|-----:|---------:|---------:|-----------:|
 CompileBlock                    | compile    |    45.683 μs |    24.4194 μs |   1.3385 μs |    44.468 μs |    47.118 μs |    45.461 μs |    1 |   1.4954 |   0.3662 |    23.2 KB |
 FullCompile_MultiModule         | compile    |   547.264 μs |   611.6858 μs |  33.5286 μs |   510.583 μs |   576.330 μs |   554.881 μs |    2 |   4.8828 |   0.9766 |   77.55 KB |
 FullCompile_SingleModule        | compile    | 8,734.531 μs | 2,688.7788 μs | 147.3810 μs | 8,601.100 μs | 8,892.725 μs | 8,709.769 μs |    3 | 281.2500 | 125.0000 | 4380.24 KB |
                                 |            |              |               |             |              |              |              |      |          |          |            |
 EmitOnly_ParsedLargeModule      | emitter    | 7,304.519 μs | 1,580.9715 μs |  86.6584 μs | 7,239.366 μs | 7,402.868 μs | 7,271.324 μs |    1 | 179.6875 |  89.8438 | 2804.86 KB |
                                 |            |              |               |             |              |              |              |      |          |          |            |
 LexerOnly_Small                 | lexer      |     3.606 μs |     0.1078 μs |   0.0059 μs |     3.599 μs |     3.610 μs |     3.608 μs |    1 |   0.1144 |        - |    1.77 KB |
 LexerOnly_StringsTemplatesRegex | lexer      |   102.421 μs |    40.5670 μs |   2.2236 μs |    99.853 μs |   103.721 μs |   103.689 μs |    2 |   1.5869 |   0.0610 |   25.13 KB |
 LexerOnly_UnicodeIdentifiers    | lexer      |   120.450 μs |    30.7858 μs |   1.6875 μs |   118.532 μs |   121.703 μs |   121.116 μs |    2 |   2.6855 |   0.2441 |   41.28 KB |
 LexerOnly_CommentsWhitespace    | lexer      |   187.540 μs |    12.7691 μs |   0.6999 μs |   186.936 μs |   188.307 μs |   187.377 μs |    3 |        - |        - |    1.85 KB |
 LexerOnly_Large                 | lexer      |   544.268 μs |   356.1705 μs |  19.5229 μs |   521.923 μs |   558.020 μs |   552.862 μs |    4 |   0.9766 |        - |   21.26 KB |
                                 |            |              |               |             |              |              |              |      |          |          |            |
 ParseOnly_Small                 | parser     |    13.536 μs |     4.7789 μs |   0.2619 μs |    13.384 μs |    13.839 μs |    13.386 μs |    1 |   0.4425 |   0.0076 |    6.79 KB |
 ParseOnly_TemplateInterpolation | parser     |   268.605 μs |    33.8857 μs |   1.8574 μs |   266.465 μs |   269.806 μs |   269.543 μs |    2 |  26.8555 |   9.5215 |  412.59 KB |
 ParseOnly_Large                 | parser     | 1,388.077 μs |   304.1068 μs |  16.6691 μs | 1,369.168 μs | 1,400.647 μs | 1,394.416 μs |    3 | 101.5625 |  56.6406 | 1562.48 KB |
