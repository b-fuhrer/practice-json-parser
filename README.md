# PracticeJsonParser

A high-performance, zero-allocation JSON parser written in C# from scratch (targeting .NET 10).

This project explores advanced parsing techniques, specifically utilizing flat memory pools and struct-based topological mapping to achieve performance parity with, and in some cases exceed, the .NET standard library.

> **Note:** This project is for educational purposes and architectural exploration. For production environments, please use [System.Text.Json](https://learn.microsoft.com/en-us/dotnet/standard/serialization/system-text-json-overview).

## Design Philosophy

The architecture discards traditional object materialization in favor of a flat, continuous memory model. It leverages modern C# performance features to eliminate heap allocations during the parsing loop.

* **Flat Memory Pools:** Uses `JsonContext` backed by `ArrayPool<T>` to store parsed data linearly, completely bypassing the instantiation of traditional DOM objects (like `JsonArray` or `JsonObject`).
* **Topological Routing:** Collections are mapped using 8-byte `ArrayElement` and 12-byte `ObjectElement` structs. These structs utilize bit-packed `NextSiblingOffset` fields to enable $O(1)$ traversal and skipping of deeply nested structures.
* **Primitive Optimization:** Object properties holding primitive values (strings, numbers, booleans) route directly to data pools, bypassing the structural element arrays entirely.
* **Struct-Based State Management:** Execution state and error propagation are handled purely through `JsonResult` and `JsonNode` structs, avoiding the overhead of exceptions or null references.

## Installation & Example Usage

Ensure you are running the .NET 10 SDK.

```csharp
using PracticeJsonParser;

byte[] jsonBytes = ...; // specific byte source
ReadOnlySpan<byte> jsonSpan = jsonBytes;

// Initialize the rented memory context based on the payload size
using var context = new JsonContext(jsonSpan.Length);

// Parse returns a lightweight JsonNode struct facade
JsonNode root = JsonParser.Parse(context, jsonSpan);

if (root.IsSuccess)
{
    // Traverse the flat memory structure
    if (root.Type == JsonType.Object)
    {
        var enumerator = root.GetObject();
        while ((var prop = enumerator.GetNext()).Value.IsSuccess)
        {
            // Process properties...
        }
    }
}
else
{
    // Handle root.Error
}
```

## Performance & Benchmarks

The transition to a flat memory pool architecture has fundamentally shifted the performance profile. The parser now demonstrates competitive execution times and memory parity with the .NET standard library.

* **Execution Time:** Outperforms `System.Text.Json` by approximately **37%** on large, complex payloads (`citm_catalog.json`). For deeply nested, smaller payloads (`twitter.json`), it remains slower but shows massive improvements over the legacy design.
* **Memory Usage:** Allocation footprint has been reduced by over 80% compared to the legacy implementation, achieving near-parity with `System.Text.Json` (1.07x alloc ratio on `citm_catalog.json`).

### BenchmarkDotNet Results

```text
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
| **SystemTextJson** | **twitter.json** |   **498.2 μs** |  **5.07 μs** |  **4.75 μs** |  **1.00** |    **0.01** | **141.6016** | **141.6016** | **141.6016** |   **513.5 KB** |        **1.00** |
| MyParser       | twitter.json      |   833.3 μs |  8.58 μs |  8.03 μs |  1.67 |    0.02 |  73.2422 |  56.6406 |        - | 1198.39 KB |        2.33 |
| LegacyParser   | twitter.json      | 1,979.0 μs | 18.32 μs | 17.13 μs |  3.97 |    0.05 | 167.9688 | 164.0625 |        - | 2746.21 KB |        5.35 |

## Architecture Details

* **Separation of API and Execution:** The public API returns a `JsonNode` which acts as a window into the `JsonContext`. The internal recursive descent parser strictly returns `JsonResult` to track the byte index and pool insertion positions.
* **Forward-Only Enumeration:** Collections are read via custom `ArrayEnumerator` and `ObjectEnumerator` structs, preventing intermediate list allocations during traversal.
* **Deterministic Memory Lifecycle:** The parser relies on the `IDisposable` pattern within `JsonContext` to manage `ArrayPool<T>` rentals. This guarantees that all memory used during parsing is deterministically returned to the shared pools, eliminating garbage collector pressure.
* **Parallel Primitive Storage:** Raw data is isolated into dedicated, continuous array pools (`double[]` for numbers, `string[]` for strings). This improves CPU cache locality and completely eliminates boxing for value-types.
