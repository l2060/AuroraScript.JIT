```

BenchmarkDotNet v0.15.8, Windows 11 (10.0.28000.2704/26H1/2026Update)
13th Gen Intel Core i7-13700KF 3.40GHz, 1 CPU, 24 logical and 16 physical cores
.NET SDK 10.0.400
  [Host]     : .NET 10.0.11 (10.0.11, 10.0.1126.37416), X64 RyuJIT x86-64-v3
  Job-BZNPUE : .NET 10.0.11 (10.0.11, 10.0.1126.37416), X64 RyuJIT x86-64-v3

IterationCount=5  IterationTime=200ms  LaunchCount=1  
WarmupCount=3  

```
| Method               | Mean      | Error     | StdDev    | Code Size | Allocated |
|--------------------- |----------:|----------:|----------:|----------:|----------:|
| CreateInt32          | 0.6234 ns | 0.0060 ns | 0.0016 ns |     105 B |         - |
| CreateUInt32         | 0.6523 ns | 0.0104 ns | 0.0027 ns |     108 B |         - |
| CreateNumberInt64    | 0.5819 ns | 0.0012 ns | 0.0002 ns |     105 B |         - |
| CreateDouble         | 0.8931 ns | 0.0169 ns | 0.0044 ns |     122 B |         - |
| ReadNumber           | 0.5346 ns | 0.0026 ns | 0.0007 ns |     113 B |         - |
| Truthiness           | 1.0885 ns | 0.0102 ns | 0.0027 ns |     661 B |         - |
| ConvertNumber        | 0.6142 ns | 0.0187 ns | 0.0049 ns |     165 B |         - |
| ExactIntegerEquality | 2.3605 ns | 0.0453 ns | 0.0118 ns |   1,265 B |         - |
