
BenchmarkDotNet v0.15.8, Windows 11 (10.0.26200.8655/25H2/2025Update/HudsonValley2)
13th Gen Intel Core i7-13700KF 3.40GHz, 1 CPU, 24 logical and 16 physical cores
.NET SDK 10.0.301
  [Host]   : .NET 10.0.9 (10.0.9, 10.0.926.27113), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.9 (10.0.9, 10.0.926.27113), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

 Method                          | Categories | Mean         | Error         | StdDev      | Min          | Max          | Median       | Rank | Gen0     | Gen1     | Allocated  |
-------------------------------- |----------- |-------------:|--------------:|------------:|-------------:|-------------:|-------------:|-----:|---------:|---------:|-----------:|
 CompileBlock                    | compile    |    28.596 μs |     6.4107 μs |   0.3514 μs |    28.323 μs |    28.992 μs |    28.472 μs |    1 |   1.1902 |   1.1749 |   18.35 KB |
 FullCompile_MultiModule         | compile    |   491.692 μs |   169.9803 μs |   9.3172 μs |   484.729 μs |   502.276 μs |   488.071 μs |    2 |   4.3945 |   0.9766 |   69.99 KB |
 FullCompile_SingleModule        | compile    | 7,043.578 μs | 3,035.9569 μs | 166.4110 μs | 6,942.688 μs | 7,235.652 μs | 6,952.395 μs |    3 | 234.3750 | 109.3750 | 3642.91 KB |
                                 |            |              |               |             |              |              |              |      |          |          |            |
 EmitOnly_ParsedLargeModule      | emitter    | 5,288.766 μs | 5,227.3863 μs | 286.5307 μs | 4,960.319 μs | 5,487.507 μs | 5,418.473 μs |    1 | 132.8125 |  66.4063 | 2067.53 KB |
                                 |            |              |               |             |              |              |              |      |          |          |            |
 LexerOnly_Small                 | lexer      |     3.604 μs |     0.5770 μs |   0.0316 μs |     3.568 μs |     3.625 μs |     3.620 μs |    1 |   0.1144 |        - |    1.77 KB |
 LexerOnly_StringsTemplatesRegex | lexer      |    60.270 μs |     3.2922 μs |   0.1805 μs |    60.114 μs |    60.468 μs |    60.227 μs |    2 |   1.5869 |   0.0610 |   25.13 KB |
 LexerOnly_UnicodeIdentifiers    | lexer      |    67.617 μs |     4.6826 μs |   0.2567 μs |    67.393 μs |    67.897 μs |    67.561 μs |    2 |   2.6855 |   0.2441 |   41.28 KB |
 LexerOnly_CommentsWhitespace    | lexer      |   107.117 μs |   317.9046 μs |  17.4254 μs |    96.899 μs |   127.238 μs |    97.215 μs |    3 |        - |        - |    1.85 KB |
 LexerOnly_Large                 | lexer      |   552.899 μs |   291.8215 μs |  15.9957 μs |   534.437 μs |   562.613 μs |   561.647 μs |    4 |   0.9766 |        - |   21.26 KB |
                                 |            |              |               |             |              |              |              |      |          |          |            |
 ParseOnly_Small                 | parser     |    13.664 μs |     2.3209 μs |   0.1272 μs |    13.520 μs |    13.760 μs |    13.713 μs |    1 |   0.4425 |   0.0076 |    6.79 KB |
 ParseOnly_TemplateInterpolation | parser     |   256.694 μs |    37.4135 μs |   2.0508 μs |   254.898 μs |   258.929 μs |   256.254 μs |    2 |  26.8555 |   9.5215 |  412.59 KB |
 ParseOnly_Large                 | parser     | 1,355.285 μs |   492.4985 μs |  26.9955 μs | 1,330.286 μs | 1,383.911 μs | 1,351.658 μs |    3 | 101.5625 |  56.6406 | 1562.48 KB |
