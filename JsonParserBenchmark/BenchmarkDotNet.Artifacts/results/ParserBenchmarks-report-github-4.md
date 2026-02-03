```

BenchmarkDotNet v0.15.8, Windows 11 (10.0.26200.7623/25H2/2025Update/HudsonValley2)
13th Gen Intel Core i7-1355U 1.70GHz, 1 CPU, 12 logical and 10 physical cores
.NET SDK 10.0.102
  [Host] : .NET 10.0.2 (10.0.2, 10.0.225.61305), X64 RyuJIT x86-64-v3

Job=InProcess  Toolchain=InProcessEmitToolchain  

```
| Method         | FileName          | Mean        | Error     | StdDev    | Ratio | RatioSD | Gen0      | Gen1     | Gen2     | Allocated  | Alloc Ratio |
|--------------- |------------------ |------------:|----------:|----------:|------:|--------:|----------:|---------:|---------:|-----------:|------------:|
| **SystemTextJson** | **citm_catalog.json** |  **2,064.5 μs** |  **14.54 μs** |  **12.14 μs** |  **1.00** |    **0.01** |  **246.0938** | **246.0938** | **246.0938** | **1024.62 KB** |        **1.00** |
| MyParser       | citm_catalog.json |  9,716.2 μs | 153.29 μs | 128.01 μs |  4.71 |    0.07 |  859.3750 | 468.7500 |  78.1250 | 4874.95 KB |        4.76 |
| LegacyParser   | citm_catalog.json | 11,915.1 μs | 215.10 μs | 201.21 μs |  5.77 |    0.10 | 1218.7500 | 734.3750 | 234.3750 | 6057.63 KB |        5.91 |
|                |                   |             |           |           |       |         |           |          |          |            |             |
| **SystemTextJson** | **twitter.json**      |    **722.1 μs** |   **4.22 μs** |   **3.74 μs** |  **1.00** |    **0.01** |  **141.6016** | **141.6016** | **141.6016** |   **513.4 KB** |        **1.00** |
| MyParser       | twitter.json      |  5,943.3 μs | 113.99 μs | 106.62 μs |  8.23 |    0.15 |  445.3125 | 250.0000 |  62.5000 | 2385.27 KB |        4.65 |
| LegacyParser   | twitter.json      |  3,691.9 μs |  21.89 μs |  18.28 μs |  5.11 |    0.04 |  445.3125 | 441.4063 |        - |  2746.2 KB |        5.35 |
