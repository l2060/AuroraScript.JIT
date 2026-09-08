```

BenchmarkDotNet v0.15.8, Windows 11 (10.0.28000.2704/26H1/2026Update)
13th Gen Intel Core i7-13700KF 3.40GHz, 1 CPU, 24 logical and 16 physical cores
.NET SDK 10.0.400
  [Host]     : .NET 10.0.11 (10.0.11, 10.0.1126.37416), X64 RyuJIT x86-64-v3
  Job-BZNPUE : .NET 10.0.11 (10.0.11, 10.0.1126.37416), X64 RyuJIT x86-64-v3

IterationCount=5  IterationTime=200ms  LaunchCount=1  
WarmupCount=3  

```
| Method             | Mean       | Error     | StdDev    | Code Size | Gen0   | Allocated |
|------------------- |-----------:|----------:|----------:|----------:|-------:|----------:|
| ReadDynamicArray   |  4.0579 ns | 0.0079 ns | 0.0020 ns |   3,747 B |      - |         - |
| WriteDynamicArray  |  5.0805 ns | 0.0083 ns | 0.0013 ns |   7,941 B |      - |         - |
| ReadNumberProperty | 71.5899 ns | 1.3470 ns | 0.3498 ns |  22,719 B | 0.0018 |      32 B |
| WriteIntProperty   | 34.2508 ns | 0.0745 ns | 0.0193 ns |  14,323 B |      - |         - |
| ArraySpread        |  0.6114 ns | 0.0012 ns | 0.0002 ns |   2,182 B |      - |         - |
| ArgumentSpread     |  0.3716 ns | 0.0014 ns | 0.0004 ns |   3,708 B |      - |         - |
| CheckUInt32Number  |  1.8757 ns | 0.0087 ns | 0.0023 ns |     164 B |      - |         - |
| IsUInt32           |  1.2175 ns | 0.0039 ns | 0.0006 ns |     115 B |      - |         - |
| CheckInt64Datum    |  2.8962 ns | 0.0045 ns | 0.0012 ns |     660 B |      - |         - |
| CheckUInt64Datum   |  3.3616 ns | 0.0069 ns | 0.0018 ns |     630 B |      - |         - |
