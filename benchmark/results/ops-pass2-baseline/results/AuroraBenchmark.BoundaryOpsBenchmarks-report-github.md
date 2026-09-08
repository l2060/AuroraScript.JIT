```

BenchmarkDotNet v0.15.8, Windows 11 (10.0.28000.2704/26H1/2026Update)
13th Gen Intel Core i7-13700KF 3.40GHz, 1 CPU, 24 logical and 16 physical cores
.NET SDK 10.0.400
  [Host]     : .NET 10.0.11 (10.0.11, 10.0.1126.37416), X64 RyuJIT x86-64-v3
  Job-BZNPUE : .NET 10.0.11 (10.0.11, 10.0.1126.37416), X64 RyuJIT x86-64-v3

IterationCount=5  IterationTime=200ms  LaunchCount=1  
WarmupCount=3  

```
| Method             | Mean      | Error     | StdDev    | Code Size | Gen0   | Allocated |
|------------------- |----------:|----------:|----------:|----------:|-------:|----------:|
| ReadDynamicArray   |  8.769 ns | 0.0301 ns | 0.0047 ns |   3,879 B |      - |         - |
| WriteDynamicArray  |  6.655 ns | 0.0254 ns | 0.0066 ns |   7,713 B |      - |         - |
| ReadNumberProperty | 76.773 ns | 1.1155 ns | 0.1726 ns |  10,664 B | 0.0019 |      32 B |
| WriteIntProperty   | 86.157 ns | 1.4270 ns | 0.3706 ns |  23,842 B | 0.0013 |      24 B |
| ArraySpread        |  2.534 ns | 0.0242 ns | 0.0063 ns |   1,767 B |      - |         - |
| ArgumentSpread     |  2.251 ns | 0.0170 ns | 0.0044 ns |   3,652 B |      - |         - |
| CheckUInt32Number  |  1.876 ns | 0.0074 ns | 0.0019 ns |     164 B |      - |         - |
| IsUInt32           |  1.215 ns | 0.0093 ns | 0.0014 ns |     115 B |      - |         - |
| CheckInt64Datum    |  3.088 ns | 0.0115 ns | 0.0030 ns |     654 B |      - |         - |
| CheckUInt64Datum   |  3.642 ns | 0.0113 ns | 0.0018 ns |     681 B |      - |         - |
