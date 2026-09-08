```

BenchmarkDotNet v0.15.8, Windows 11 (10.0.28000.2704/26H1/2026Update)
13th Gen Intel Core i7-13700KF 3.40GHz, 1 CPU, 24 logical and 16 physical cores
.NET SDK 10.0.400
  [Host]     : .NET 10.0.11 (10.0.11, 10.0.1126.37416), X64 RyuJIT x86-64-v3
  Job-IPHOJU : .NET 10.0.11 (10.0.11, 10.0.1126.37416), X64 RyuJIT x86-64-v3

IterationCount=8  IterationTime=300ms  LaunchCount=2  
WarmupCount=5  

```
| Method       | Mean      | Error     | StdDev    | Gen0   | Allocated |
|------------- |----------:|----------:|----------:|-------:|----------:|
| Comparison   | 56.092 ns | 0.4015 ns | 0.3944 ns |      - |         - |
| CachedNumber |  1.844 ns | 0.0075 ns | 0.0074 ns | 0.0000 |         - |
| Iteration    |  5.177 ns | 2.6958 ns | 2.6476 ns | 0.0000 |         - |
