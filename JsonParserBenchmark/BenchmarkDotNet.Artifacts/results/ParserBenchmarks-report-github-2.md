```

BenchmarkDotNet v0.15.8, Windows 11 (10.0.26100.7462/24H2/2024Update/HudsonValley)
13th Gen Intel Core i7-1355U 1.70GHz, 1 CPU, 12 logical and 10 physical cores
.NET SDK 10.0.101
  [Host] : .NET 10.0.1 (10.0.1, 10.0.125.57005), X64 RyuJIT x86-64-v3

Job=InProcess  Toolchain=InProcessEmitToolchain  

```
| Method         | FileName          | Mean      | Error     | StdDev    | Ratio | RatioSD | Gen0      | Gen1     | Gen2     | Allocated  | Alloc Ratio |
|--------------- |------------------ |----------:|----------:|----------:|------:|--------:|----------:|---------:|---------:|-----------:|------------:|
| **SystemTextJson** | **citm_catalog.json** |  **3.533 ms** | **0.0702 ms** | **0.1418 ms** |  **1.00** |    **0.06** |  **246.0938** | **246.0938** | **246.0938** | **1024.59 KB** |        **1.00** |
| MyParser       | citm_catalog.json | 18.766 ms | 0.3485 ms | 0.3090 ms |  5.32 |    0.22 | 1187.5000 | 718.7500 | 218.7500 | 6057.63 KB |        5.91 |
|                |                   |           |           |           |       |         |           |          |          |            |             |
| **SystemTextJson** | **twitter.json**      |  **1.256 ms** | **0.0214 ms** | **0.0270 ms** |  **1.00** |    **0.03** |  **140.6250** | **140.6250** | **140.6250** |  **513.48 KB** |        **1.00** |
| MyParser       | twitter.json      |  6.405 ms | 0.1164 ms | 0.1554 ms |  5.10 |    0.16 |  445.3125 | 437.5000 |        - | 2746.22 KB |        5.35 |
