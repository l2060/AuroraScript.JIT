```

BenchmarkDotNet v0.15.8, Windows 11 (10.0.28000.2704/26H1/2026Update)
13th Gen Intel Core i7-13700KF 3.40GHz, 1 CPU, 24 logical and 16 physical cores
.NET SDK 10.0.400
  [Host]     : .NET 10.0.11 (10.0.11, 10.0.1126.37416), X64 RyuJIT x86-64-v3
  Job-TVZQGE : .NET 10.0.11 (10.0.11, 10.0.1126.37416), X64 RyuJIT x86-64-v3

IterationCount=5  IterationTime=200ms  LaunchCount=2  
WarmupCount=3  

```
| Method       | Mean      | Error     | StdDev    | Gen0   | Allocated |
|------------- |----------:|----------:|----------:|-------:|----------:|
| Comparison   | 56.122 ns | 0.4194 ns | 0.2774 ns |      - |         - |
| CachedNumber |  1.819 ns | 0.1359 ns | 0.0809 ns | 0.0000 |         - |
| Iteration    |  7.302 ns | 4.0865 ns | 2.7030 ns |      - |         - |
