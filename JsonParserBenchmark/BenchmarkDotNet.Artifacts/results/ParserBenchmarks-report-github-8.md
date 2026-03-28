```

BenchmarkDotNet v0.15.8, Windows 11 (10.0.26200.7840/25H2/2025Update/HudsonValley2)
AMD Ryzen 9 9950X 4.30GHz, 1 CPU, 32 logical and 16 physical cores
.NET SDK 10.0.102
  [Host] : .NET 10.0.2 (10.0.2, 10.0.225.61305), X64 RyuJIT x86-64-v4

Job=InProcess  Toolchain=InProcessEmitToolchain  

```
| Method         | FileName          | Mean       | Error    | StdDev   | Ratio | RatioSD | Gen0     | Gen1     | Gen2     | Allocated  | Alloc Ratio |
|--------------- |------------------ |-----------:|---------:|---------:|------:|--------:|---------:|---------:|---------:|-----------:|------------:|
| **SystemTextJson** | **citm_catalog.json** | **1,367.2 μs** |  **9.32 μs** |  **8.72 μs** |  **1.00** |    **0.01** | **248.0469** | **248.0469** | **248.0469** | **1024.56 KB** |        **1.00** |
| MyParser       | citm_catalog.json |   863.3 μs |  7.67 μs |  6.80 μs |  0.63 |    0.01 |  66.4063 |  52.7344 |        - | 1100.62 KB |        1.07 |
| LegacyParser   | citm_catalog.json | 4,070.0 μs | 57.16 μs | 53.47 μs |  2.98 |    0.04 | 367.1875 | 359.3750 |        - |    6057 KB |        5.91 |
|                |                   |            |          |          |       |         |          |          |          |            |             |
| **SystemTextJson** | **twitter.json**      |   **498.2 μs** |  **5.07 μs** |  **4.75 μs** |  **1.00** |    **0.01** | **141.6016** | **141.6016** | **141.6016** |   **513.5 KB** |        **1.00** |
| MyParser       | twitter.json      |   833.3 μs |  8.58 μs |  8.03 μs |  1.67 |    0.02 |  73.2422 |  56.6406 |        - | 1198.39 KB |        2.33 |
| LegacyParser   | twitter.json      | 1,979.0 μs | 18.32 μs | 17.13 μs |  3.97 |    0.05 | 167.9688 | 164.0625 |        - | 2746.21 KB |        5.35 |
