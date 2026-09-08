```

BenchmarkDotNet v0.15.8, Windows 11 (10.0.28000.2704/26H1/2026Update)
13th Gen Intel Core i7-13700KF 3.40GHz, 1 CPU, 24 logical and 16 physical cores
.NET SDK 10.0.400
  [Host]     : .NET 10.0.11 (10.0.11, 10.0.1126.37416), X64 RyuJIT x86-64-v3
  Job-BZNPUE : .NET 10.0.11 (10.0.11, 10.0.1126.37416), X64 RyuJIT x86-64-v3

IterationCount=5  IterationTime=200ms  LaunchCount=1  
WarmupCount=3  

```
| Method         | Count | Mean          | Error         | StdDev      | Code Size | Gen0   | Allocated |
|--------------- |------ |--------------:|--------------:|------------:|----------:|-------:|----------:|
| **ArrayMiss**      | **32**    |   **451.9243 ns** |    **56.7241 ns** |  **14.7311 ns** |   **3,757 B** | **0.0046** |     **104 B** |
| PackedMiss     | 32    |   425.0516 ns |     7.5241 ns |   1.1644 ns |   3,859 B | 0.0062 |     104 B |
| CharacterMiss  | 32    |   501.4966 ns |   122.2036 ns |  18.9111 ns |   3,162 B | 0.0486 |     768 B |
| CheckedWrapper | 32    |     3.3751 ns |     0.1565 ns |   0.0406 ns |     843 B |      - |         - |
| NullWrapper    | 32    |     0.6524 ns |     0.3176 ns |   0.0825 ns |     605 B |      - |         - |
| **ArrayMiss**      | **256**   | **3,937.6170 ns** | **1,862.6078 ns** | **483.7133 ns** |   **3,745 B** |      **-** |     **104 B** |
| PackedMiss     | 256   | 3,274.7556 ns |    38.9584 ns |  10.1174 ns |   3,797 B |      - |     104 B |
| CharacterMiss  | 256   | 4,216.5528 ns |   173.5369 ns |  26.8550 ns |   3,180 B | 0.3802 |    6144 B |
| CheckedWrapper | 256   |     3.1524 ns |     0.2169 ns |   0.0563 ns |     843 B |      - |         - |
| NullWrapper    | 256   |     0.6587 ns |     0.9760 ns |   0.2535 ns |     605 B |      - |         - |
