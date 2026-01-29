using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;
using System.Text.Json;
using JsonParserLogic;
using JsonParserLogic.Types;

BenchmarkRunner.Run<ParserBenchmarks>();

[MemoryDiagnoser]
[InProcess]
public class ParserBenchmarks
{
    [Params("twitter.json", "citm_catalog.json")]
    public string FileName { get; set; } = string.Empty;

    private byte[] _jsonBytes = null!;

    [GlobalSetup]
    public void Setup()
    {
        string projectRoot = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "..");
        string path = Path.Combine(projectRoot, "JsonFiles", FileName);

        if (!File.Exists(path))
        {
            string existingFiles = string.Join(", ", Directory.GetFiles(AppDomain.CurrentDomain.BaseDirectory)
                .Select(Path.GetFileName));

            throw new FileNotFoundException($"File missing: {path}. \nFound in folder: {existingFiles}");
        }

        byte[] rawBytes = File.ReadAllBytes(path);

        ReadOnlySpan<byte> preamble = [0xEF, 0xBB, 0xBF];

        _jsonBytes = rawBytes.AsSpan().StartsWith(preamble)
            ? rawBytes.AsSpan(preamble.Length).ToArray()
            : rawBytes;
    }

    /*
    [Benchmark(Baseline = true)]
    public JsonDocument SystemTextJson()
    {
        return JsonDocument.Parse(_jsonBytes);
    }
    */

    [Benchmark]
    public JsonResult<JsonValue> MyParser()
    {
        return JsonParser.Parse(_jsonBytes);
    }
}