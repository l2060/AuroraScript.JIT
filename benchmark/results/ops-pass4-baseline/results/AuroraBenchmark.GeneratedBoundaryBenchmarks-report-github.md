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
| Comparison   | 56.774 ns | 1.9388 ns | 1.2824 ns |      - |         - |
| CachedNumber |  1.466 ns | 0.5784 ns | 0.3442 ns | 0.0000 |         - |
| Iteration    |  2.575 ns | 0.0228 ns | 0.0119 ns | 0.0000 |         - |
