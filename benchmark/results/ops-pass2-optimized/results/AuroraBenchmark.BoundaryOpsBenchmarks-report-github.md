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
| ReadDynamicArray   |  8.8616 ns | 0.0610 ns | 0.0159 ns |   3,747 B |      - |         - |
| WriteDynamicArray  |  5.0839 ns | 0.0228 ns | 0.0035 ns |   8,301 B |      - |         - |
| ReadNumberProperty | 79.1666 ns | 0.8196 ns | 0.1268 ns |   9,263 B | 0.0020 |      32 B |
| WriteIntProperty   | 92.4982 ns | 0.9306 ns | 0.1440 ns |  13,226 B | 0.0014 |      24 B |
| ArraySpread        |  0.6114 ns | 0.0014 ns | 0.0004 ns |   2,182 B |      - |         - |
| ArgumentSpread     |  0.3718 ns | 0.0020 ns | 0.0005 ns |   3,708 B |      - |         - |
| CheckUInt32Number  |  2.4378 ns | 0.0700 ns | 0.0182 ns |     135 B |      - |         - |
| IsUInt32           |  1.4516 ns | 0.0101 ns | 0.0026 ns |     127 B |      - |         - |
| CheckInt64Datum    |  2.8974 ns | 0.0065 ns | 0.0010 ns |     603 B |      - |         - |
| CheckUInt64Datum   |  3.3611 ns | 0.0131 ns | 0.0020 ns |     630 B |      - |         - |
