```

BenchmarkDotNet v0.15.8, Windows 11 (10.0.26100.7462/24H2/2024Update/HudsonValley)
13th Gen Intel Core i7-1355U 1.70GHz, 1 CPU, 12 logical and 10 physical cores
.NET SDK 10.0.101
  [Host] : .NET 10.0.1 (10.0.1, 10.0.125.57005), X64 RyuJIT x86-64-v3

Job=InProcess  Toolchain=InProcessEmitToolchain  

```
| Method         | FileName          | Mean      | Error     | StdDev    | Median    | Ratio | RatioSD | Gen0      | Gen1     | Gen2     | Allocated  | Alloc Ratio |
|--------------- |------------------ |----------:|----------:|----------:|----------:|------:|--------:|----------:|---------:|---------:|-----------:|------------:|
| **SystemTextJson** | **citm_catalog.json** |  **3.908 ms** | **0.0898 ms** | **0.2633 ms** |  **3.857 ms** |  **1.00** |    **0.09** |  **242.1875** | **242.1875** | **242.1875** | **1024.55 KB** |        **1.00** |
| MyParser       | citm_catalog.json | 19.974 ms | 0.3979 ms | 1.0056 ms | 19.640 ms |  5.13 |    0.42 | 1187.5000 | 718.7500 | 218.7500 | 6057.63 KB |        5.91 |
|                |                   |           |           |           |           |       |         |           |          |          |            |             |
| **SystemTextJson** | **twitter.json**      |  **1.306 ms** | **0.0258 ms** | **0.0676 ms** |  **1.280 ms** |  **1.00** |    **0.07** |  **140.6250** | **140.6250** | **140.6250** |  **513.35 KB** |        **1.00** |
| MyParser       | twitter.json      |  6.489 ms | 0.1225 ms | 0.1023 ms |  6.516 ms |  4.98 |    0.26 |  445.3125 | 437.5000 |        - |  2746.2 KB |        5.35 |
