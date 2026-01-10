using System.Diagnostics.CodeAnalysis;
namespace PracticeJsonParser.Types;

public enum JsonErrorType : byte
{
    EndOfFile,
    InvalidCharacter,
    InvalidSyntax
}

public readonly record struct JsonError(JsonErrorType Type, string? Message);

public readonly record struct JsonResult<T>(T? Value, int Index, JsonError? Error)
    where T : JsonValue
{
    [MemberNotNullWhen(true, nameof(Value))]
    [MemberNotNullWhen(false, nameof(Error))]
    public bool Success => Error is null;
    
    public static JsonResult<T> Ok(T value, int index)
        => new(value, index, null);
    
    public static JsonResult<T> Err(JsonError error, int index) 
        => new(null, index, error);

    public static JsonResult<T> Err(JsonErrorType type, string? message, int index)
        => new(null, index, new JsonError(type, message));
    
    public static JsonResult<T> Err(JsonErrorType type, int index)
        => new(null, index, new JsonError(type, null));
    
    // implicitly upcasts specific JsonValue result to base JsonValue result type
    // e.g. JsonResult<JsonString> can be used where JsonResult<JsonValue> is expected
    public static implicit operator JsonResult<JsonValue>(JsonResult<T> source)
    {
        var upcastedValue = source.Success 
            ? JsonResult<JsonValue>.Ok(source.Value, source.Index)
            : JsonResult<JsonValue>.Err(source.Error.Value, source.Index);

        return upcastedValue;
    }
}