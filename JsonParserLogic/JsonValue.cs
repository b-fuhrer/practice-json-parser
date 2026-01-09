using System.Collections.Immutable;
namespace PracticeJsonParser;

public abstract record JsonValue;

public sealed record JsonNull : JsonValue
{
    public static readonly JsonNull Instance = new();
    private JsonNull() {}
}

public sealed record JsonBool(bool Bool) : JsonValue
{
    public static readonly JsonBool True = new(true);
    public static readonly JsonBool False = new(false);
}

public sealed record JsonString(string String) : JsonValue;

public sealed record JsonNumber(double Number) : JsonValue;

public sealed record JsonArray(ImmutableArray<JsonValue> Elements) : JsonValue;

public sealed record JsonObject(ImmutableDictionary<string, JsonValue> Fields) : JsonValue;
