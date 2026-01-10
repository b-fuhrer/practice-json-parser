# PracticeJsonParser

A high-performance oriented JSON parser written in C# from scratch (targeting .NET 10).

This project explores incorporating functional programming paradigms (such as pure functions, referential transparency, and explicit state passing) into a manual state-management system within the .NET ecosystem.

> **Note:** This project is for educational purposes only. It is **not** intended for production environments. For professional applications, please use [System.Text.Json](https://learn.microsoft.com/en-us/dotnet/standard/serialization/system-text-json-overview).

## Design Philosophy

The architecture is heavily influenced by functional programming principles to improve testability and predictability, while leveraging modern C# features for efficient data handling.

* **Zero External Dependencies:** Core components, including numeric decomposition and string unescaping, are implemented entirely from scratch without reliance on standard library helpers or external libraries.
* **Low-Allocation, Stack-Based Design:** Utilizes `ReadOnlySpan<byte>` and stack-allocated `record structs` to manage parsing state, avoiding heap allocations for the parser's internal logic.
* **Functional Influence:** Logic is contained in pure functions where state is passed via arguments and returned as transformed values, reducing reliance on mutable class-level state.
* **Full Grammar Support:** Implements the complete JSON specification, including nested objects, arrays, string escaping, and scientific notation for numbers.

## Installation & Example Usage

Ensure you are running the .NET 10 SDK.

```csharp
using PracticeJsonParser;

byte[] jsonBytes = ...; // specific byte source
ReadOnlySpan<byte> jsonSpan = jsonBytes;

JsonResult<JsonValue> result = JsonParser.Parse(jsonSpan);

if (result.IsSuccess)
{
    JsonValue value = result.Value;
    // Process value...
}
else
{
    // Handle result.Error
}
```

## Performance & Benchmarks

While the parser leverages zero-allocation techniques for its internal state, it currently lacks the highly specialized optimizations found in the .NET standard library. In comparative testing:

* **Execution Time:** Approximately **5x slower** than `System.Text.Json`.
* **Memory Usage:** Approximately **5x to 6x higher** memory footprint than the standard library implementation.

These results are expected as the project prioritizes structural clarity and manual parsing logic over the SIMD-intrinsics and internal JIT optimizations used by production-grade parsers.

### BenchmarkDotNet Results (`ParserBenchmarks-report-github-3.md`)

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


## Architecture Details

* **State Management:** The parser avoids `ref` parameters or mutable fields in favor of a pattern where helpers return a `(Value, NewIndex)` structure.
* **Error Handling:** Errors are propagated via a tagged union-like structure, ensuring that error states are handled explicitly as first-class values rather than through exceptions.
