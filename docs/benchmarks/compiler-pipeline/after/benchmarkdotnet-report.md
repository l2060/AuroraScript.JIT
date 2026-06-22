```

BenchmarkDotNet v0.15.8, Windows 11 (10.0.26100.8457/24H2/2024Update/HudsonValley)
Intel Xeon W-2235 CPU 3.80GHz (Max: 3.79GHz), 1 CPU, 12 logical and 6 physical cores
.NET SDK 10.0.301
  [Host]   : .NET 10.0.9 (10.0.9, 10.0.926.27113), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.9 (10.0.9, 10.0.926.27113), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                          | Categories | Mean          | Error         | StdDev        | Min          | Max           | Median        | Rank | Gen0     | Gen1     | Allocated  |
|-------------------------------- |----------- |--------------:|--------------:|--------------:|-------------:|--------------:|--------------:|-----:|---------:|---------:|-----------:|
| CompileBlock                    | compile    |     39.410 μs |      5.010 μs |     0.2746 μs |    39.181 μs |     39.715 μs |     39.335 μs |    1 |   4.2725 |   4.2114 |    18.2 KB |
| FullCompile_MultiModule         | compile    |    720.874 μs |  1,159.932 μs |    63.5798 μs |   674.086 μs |    793.264 μs |    695.272 μs |    2 |   9.7656 |   2.9297 |   62.03 KB |
| FullCompile_SingleModule        | compile    | 10,930.828 μs | 25,730.182 μs | 1,410.3581 μs | 9,966.950 μs | 12,549.567 μs | 10,275.967 μs |    3 | 453.1250 | 156.2500 | 2814.08 KB |
|                                 |            |               |               |               |              |               |               |      |          |          |            |
| EmitOnly_ParsedLargeModule      | emitter    |  7,149.067 μs |  1,644.713 μs |    90.1523 μs | 7,047.633 μs |  7,220.052 μs |  7,179.516 μs |    1 | 203.1250 |  70.3125 | 1270.95 KB |
|                                 |            |               |               |               |              |               |               |      |          |          |            |
| LexerOnly_Small                 | lexer      |      4.688 μs |      5.855 μs |     0.3209 μs |     4.481 μs |      5.057 μs |      4.525 μs |    1 |   0.4425 |        - |    1.89 KB |
| LexerOnly_StringsTemplatesRegex | lexer      |    138.898 μs |     18.868 μs |     1.0342 μs |   137.939 μs |    139.994 μs |    138.762 μs |    2 |   5.8594 |   0.2441 |   25.24 KB |
| LexerOnly_UnicodeIdentifiers    | lexer      |    165.835 μs |    278.632 μs |    15.2728 μs |   154.619 μs |    183.229 μs |    159.657 μs |    2 |   9.2773 |   0.2441 |   39.45 KB |
| LexerOnly_CommentsWhitespace    | lexer      |    286.160 μs |    142.579 μs |     7.8152 μs |   277.677 μs |    293.067 μs |    287.737 μs |    3 |        - |        - |    1.97 KB |
| LexerOnly_Large                 | lexer      |    846.891 μs |    417.136 μs |    22.8646 μs |   824.899 μs |    870.538 μs |    845.235 μs |    4 |   4.8828 |        - |   21.38 KB |
|                                 |            |               |               |               |              |               |               |      |          |          |            |
| ParseOnly_Small                 | parser     |     16.040 μs |      4.894 μs |     0.2682 μs |    15.797 μs |     16.328 μs |     15.996 μs |    1 |   1.6785 |        - |    7.08 KB |
| ParseOnly_TemplateInterpolation | parser     |    488.871 μs |  1,109.142 μs |    60.7958 μs |   450.505 μs |    558.967 μs |    457.140 μs |    2 |  82.0313 |  41.9922 |  412.88 KB |
| ParseOnly_Large                 | parser     |  2,508.904 μs |  1,190.327 μs |    65.2459 μs | 2,445.796 μs |  2,576.096 μs |  2,504.822 μs |    3 | 250.0000 | 246.0938 | 1532.01 KB |
