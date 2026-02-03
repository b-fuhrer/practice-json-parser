```

BenchmarkDotNet v0.15.8, Windows 11 (10.0.26200.7623/25H2/2025Update/HudsonValley2)
13th Gen Intel Core i7-1355U 1.70GHz, 1 CPU, 12 logical and 10 physical cores
.NET SDK 10.0.102
  [Host] : .NET 10.0.2 (10.0.2, 10.0.225.61305), X64 RyuJIT x86-64-v3

Job=InProcess  Toolchain=InProcessEmitToolchain  

```
| Method         | FileName          | Mean        | Error     | StdDev    | Ratio | RatioSD | Gen0      | Gen1     | Gen2     | Allocated  | Alloc Ratio |
|--------------- |------------------ |------------:|----------:|----------:|------:|--------:|----------:|---------:|---------:|-----------:|------------:|
| **SystemTextJson** | **citm_catalog.json** |  **2,009.9 μs** |  **16.79 μs** |  **15.71 μs** |  **1.00** |    **0.01** |  **246.0938** | **246.0938** | **246.0938** | **1024.56 KB** |        **1.00** |
| MyParser       | citm_catalog.json |  9,967.4 μs |  95.07 μs |  88.93 μs |  4.96 |    0.06 |  859.3750 | 468.7500 |  78.1250 | 4874.98 KB |        4.76 |
| LegacyParser   | citm_catalog.json | 11,922.5 μs | 234.74 μs | 241.07 μs |  5.93 |    0.12 | 1218.7500 | 734.3750 | 234.3750 | 6057.62 KB |        5.91 |
|                |                   |             |           |           |       |         |           |          |          |            |             |
| **SystemTextJson** | **twitter.json**      |    **712.7 μs** |   **4.76 μs** |   **4.45 μs** |  **1.00** |    **0.01** |  **141.6016** | **141.6016** | **141.6016** |  **513.49 KB** |        **1.00** |
| MyParser       | twitter.json      |  5,930.5 μs | 117.89 μs | 126.14 μs |  8.32 |    0.18 |  453.1250 | 257.8125 |  70.3125 | 2385.27 KB |        4.65 |
| LegacyParser   | twitter.json      |  3,773.2 μs |  73.25 μs |  81.41 μs |  5.29 |    0.12 |  445.3125 | 441.4063 |        - | 2746.21 KB |        5.35 |
