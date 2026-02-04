```

BenchmarkDotNet v0.15.8, Windows 11 (10.0.26200.7623/25H2/2025Update/HudsonValley2)
13th Gen Intel Core i7-1355U 1.70GHz, 1 CPU, 12 logical and 10 physical cores
.NET SDK 10.0.102
  [Host] : .NET 10.0.2 (10.0.2, 10.0.225.61305), X64 RyuJIT x86-64-v3

Job=InProcess  Toolchain=InProcessEmitToolchain  

```
| Method         | FileName          | Mean        | Error     | StdDev    | Ratio | RatioSD | Gen0      | Gen1     | Gen2     | Allocated  | Alloc Ratio |
|--------------- |------------------ |------------:|----------:|----------:|------:|--------:|----------:|---------:|---------:|-----------:|------------:|
| **SystemTextJson** | **citm_catalog.json** |  **1,965.6 μs** |  **38.43 μs** |  **35.94 μs** |  **1.00** |    **0.02** |  **246.0938** | **246.0938** | **246.0938** | **1024.56 KB** |        **1.00** |
| MyParser       | citm_catalog.json |  9,278.0 μs | 184.84 μs | 233.76 μs |  4.72 |    0.14 |  859.3750 | 468.7500 |  78.1250 | 4874.98 KB |        4.76 |
| LegacyParser   | citm_catalog.json | 11,781.5 μs |  93.77 μs |  78.30 μs |  6.00 |    0.11 | 1218.7500 | 734.3750 | 234.3750 | 6057.63 KB |        5.91 |
|                |                   |             |           |           |       |         |           |          |          |            |             |
| **SystemTextJson** | **twitter.json**      |    **745.3 μs** |  **14.64 μs** |  **31.51 μs** |  **1.00** |    **0.06** |  **140.6250** | **140.6250** | **140.6250** |  **513.18 KB** |        **1.00** |
| MyParser       | twitter.json      |  6,008.6 μs | 117.23 μs | 115.14 μs |  8.08 |    0.38 |  453.1250 | 257.8125 |  70.3125 | 2385.29 KB |        4.65 |
| LegacyParser   | twitter.json      |  3,879.1 μs |  61.04 μs |  59.95 μs |  5.21 |    0.24 |  445.3125 | 441.4063 |        - | 2746.21 KB |        5.35 |
