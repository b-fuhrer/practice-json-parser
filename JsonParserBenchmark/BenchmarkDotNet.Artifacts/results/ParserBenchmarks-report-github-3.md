```

BenchmarkDotNet v0.15.8, Windows 11 (10.0.26100.7462/24H2/2024Update/HudsonValley)
13th Gen Intel Core i7-1355U 1.70GHz, 1 CPU, 12 logical and 10 physical cores
.NET SDK 10.0.101
  [Host] : .NET 10.0.1 (10.0.1, 10.0.125.57005), X64 RyuJIT x86-64-v3

Job=InProcess  Toolchain=InProcessEmitToolchain  

```
| Method         | FileName          | Mean      | Error     | StdDev    | Ratio | RatioSD | Gen0      | Gen1     | Gen2     | Allocated  | Alloc Ratio |
|--------------- |------------------ |----------:|----------:|----------:|------:|--------:|----------:|---------:|---------:|-----------:|------------:|
| **SystemTextJson** | **citm_catalog.json** |  **3.580 ms** | **0.0701 ms** | **0.1568 ms** |  **1.00** |    **0.06** |  **242.1875** | **242.1875** | **242.1875** | **1024.55 KB** |        **1.00** |
| MyParser       | citm_catalog.json | 18.961 ms | 0.3765 ms | 0.7691 ms |  5.31 |    0.31 | 1187.5000 | 718.7500 | 218.7500 | 6057.62 KB |        5.91 |
|                |                   |           |           |           |       |         |           |          |          |            |             |
| **SystemTextJson** | **twitter.json**      |  **1.259 ms** | **0.0248 ms** | **0.0517 ms** |  **1.00** |    **0.06** |  **140.6250** | **140.6250** | **140.6250** |  **513.39 KB** |        **1.00** |
| MyParser       | twitter.json      |  6.256 ms | 0.1193 ms | 0.1225 ms |  4.98 |    0.22 |  445.3125 | 437.5000 |        - | 2746.22 KB |        5.35 |
