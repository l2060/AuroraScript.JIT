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
| CreateInt32          | 0.8795 ns | 0.0019 ns | 0.0005 ns |     199 B |         - |
| CreateUInt32         | 0.9119 ns | 0.0170 ns | 0.0026 ns |     206 B |         - |
| CreateNumberInt64    | 0.8765 ns | 0.0100 ns | 0.0026 ns |     206 B |         - |
| CreateDouble         | 0.9032 ns | 0.0085 ns | 0.0022 ns |     202 B |         - |
| ReadNumber           | 0.5377 ns | 0.0084 ns | 0.0022 ns |     159 B |         - |
| Truthiness           | 1.7883 ns | 0.0352 ns | 0.0091 ns |     743 B |         - |
| ConvertNumber        | 0.8133 ns | 0.0085 ns | 0.0022 ns |   1,179 B |         - |
| ExactIntegerEquality | 4.6902 ns | 0.2942 ns | 0.0764 ns |   2,343 B |         - |
