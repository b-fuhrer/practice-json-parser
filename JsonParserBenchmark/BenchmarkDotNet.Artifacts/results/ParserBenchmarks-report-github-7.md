```

BenchmarkDotNet v0.15.8, Windows 11 (10.0.26200.7840/25H2/2025Update/HudsonValley2)
AMD Ryzen 9 9950X 4.30GHz, 1 CPU, 32 logical and 16 physical cores
.NET SDK 10.0.102
  [Host] : .NET 10.0.2 (10.0.2, 10.0.225.61305), X64 RyuJIT x86-64-v4

Job=InProcess  Toolchain=InProcessEmitToolchain  

```
| Method         | FileName          | Mean       | Error    | StdDev   | Ratio | RatioSD | Gen0     | Gen1     | Gen2     | Allocated  | Alloc Ratio |
|--------------- |------------------ |-----------:|---------:|---------:|------:|--------:|---------:|---------:|---------:|-----------:|------------:|
| **SystemTextJson** | **citm_catalog.json** | **1,349.0 μs** | **11.80 μs** | **11.04 μs** |  **1.00** |    **0.01** | **248.0469** | **248.0469** | **248.0469** | **1024.57 KB** |        **1.00** |
| MyParser       | citm_catalog.json |   930.2 μs | 10.48 μs |  8.76 μs |  0.69 |    0.01 |  66.4063 |  52.7344 |        - | 1100.62 KB |        1.07 |
| LegacyParser   | citm_catalog.json | 4,209.4 μs | 63.15 μs | 59.07 μs |  3.12 |    0.05 | 367.1875 | 359.3750 |        - |    6057 KB |        5.91 |
|                |                   |            |          |          |       |         |          |          |          |            |             |
| **SystemTextJson** | **twitter.json**      |   **507.1 μs** |  **7.40 μs** |  **6.92 μs** |  **1.00** |    **0.02** | **141.6016** | **141.6016** | **141.6016** |  **513.66 KB** |        **1.00** |
| MyParser       | twitter.json      |   884.2 μs | 11.21 μs | 10.49 μs |  1.74 |    0.03 |  73.2422 |  56.6406 |        - | 1198.39 KB |        2.33 |
| LegacyParser   | twitter.json      | 2,009.3 μs | 23.61 μs | 20.93 μs |  3.96 |    0.07 | 167.9688 | 164.0625 |        - | 2746.21 KB |        5.35 |
