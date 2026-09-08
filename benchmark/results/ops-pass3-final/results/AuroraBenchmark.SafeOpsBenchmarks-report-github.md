```

BenchmarkDotNet v0.15.8, Windows 11 (10.0.28000.2704/26H1/2026Update)
13th Gen Intel Core i7-13700KF 3.40GHz, 1 CPU, 24 logical and 16 physical cores
.NET SDK 10.0.400
  [Host]     : .NET 10.0.11 (10.0.11, 10.0.1126.37416), X64 RyuJIT x86-64-v3
  Job-BZNPUE : .NET 10.0.11 (10.0.11, 10.0.1126.37416), X64 RyuJIT x86-64-v3

IterationCount=5  IterationTime=200ms  LaunchCount=1  
WarmupCount=3  

```
| Method         | Count | Mean          | Error      | StdDev     | Code Size | Allocated |
|--------------- |------ |--------------:|-----------:|-----------:|----------:|----------:|
| **ArrayMiss**      | **32**    |   **310.2511 ns** |  **2.4898 ns** |  **0.3853 ns** |   **3,523 B** |         **-** |
| PackedMiss     | 32    |   349.2819 ns |  3.9765 ns |  1.0327 ns |   3,763 B |         - |
| CharacterMiss  | 32    |     9.2038 ns |  0.1272 ns |  0.0330 ns |   4,289 B |         - |
| CheckedWrapper | 32    |     2.0762 ns |  0.1383 ns |  0.0359 ns |     391 B |         - |
| NullWrapper    | 32    |     0.4039 ns |  0.4720 ns |  0.1226 ns |     466 B |         - |
| **ArrayMiss**      | **256**   | **2,460.7713 ns** | **51.3724 ns** | **13.3413 ns** |   **3,506 B** |         **-** |
| PackedMiss     | 256   | 2,766.9852 ns | 46.1458 ns | 11.9839 ns |   3,780 B |         - |
| CharacterMiss  | 256   |    16.3141 ns |  0.2724 ns |  0.0422 ns |   4,300 B |         - |
| CheckedWrapper | 256   |     2.0410 ns |  0.2477 ns |  0.0643 ns |     391 B |         - |
| NullWrapper    | 256   |     0.4191 ns |  0.1870 ns |  0.0486 ns |     466 B |         - |
