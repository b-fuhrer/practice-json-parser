using System.Runtime.InteropServices;
using System.Runtime.CompilerServices;
using System.Collections.Frozen;
using System.Collections.Immutable;

namespace JsonParserLogic;

public enum JsonType : byte
{
    Null,
    Bool,
    Number,
    String,
    Array,
    Object
}

public enum ErrorType : byte
{
    None,
    EndOfFile,
    InvalidCharacter,
    InvalidSyntax
}

public readonly struct JsonNode
{
    public readonly JsonType Type;
    public readonly bool IsSuccess;
    public readonly ErrorType ErrorType;
    private readonly bool _bool;
    public readonly int Index;
    private readonly double _number;
    private readonly object? _reference; // for string, array and object

    public bool IsNull => IsSuccess && Type == JsonType.Null;
    public bool IsError => !IsSuccess;

    // success constructors
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static JsonNode OkNull(int index) => new JsonNode(JsonType.Null, index);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static JsonNode OkNumber(double value, int index) => new JsonNode(JsonType.Number, index, number: value);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static JsonNode OkBool(bool value, int index) => new JsonNode(JsonType.Bool, index, boolean: value);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static JsonNode OkString(string value, int index) => new JsonNode(JsonType.String, index, reference: value);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static JsonNode OkArray(JsonNode[] items, int index) =>
        new JsonNode(JsonType.Array, index, reference: items);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static JsonNode OkObject(ImmutableDictionary<string, JsonNode> items, int index) =>
        new JsonNode(JsonType.Object, index, reference: items);

    // error constructors
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static JsonNode Err(ErrorType type, string? message, int index) =>
        new JsonNode(JsonType.Null, index, success: false, errorType: type, reference: message);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static JsonNode Err(ErrorType type, string message) =>
        new JsonNode(JsonType.Null, 0, success: false, errorType: type, reference: message);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static JsonNode Err(ErrorType type, int index) =>
        new JsonNode(JsonType.Null, index, success: false, errorType: type, reference: null);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static JsonNode Err(ErrorType type) =>
        new JsonNode(JsonType.Null, 0, success: false, errorType: type, reference: null);

    // internal constuctor
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private JsonNode(
        JsonType type,
        int index,
        bool success = true,
        ErrorType errorType = ErrorType.None,
        bool boolean = false,
        double number = 0,
        object? reference = null)
    {
        Type = type;
        Index = index;
        IsSuccess = success;
        ErrorType = errorType;
        _number = number;
        _bool = boolean;
        _reference = reference;
    }

    // access helpers
    public double Number => IsSuccess && Type == JsonType.Number
        ? _number
        : throw ThrowInvalidAccess(JsonType.Number);

    public bool Bool => IsSuccess && Type == JsonType.Bool
        ? _bool
        : throw ThrowInvalidAccess(JsonType.Bool);

    public string String => IsSuccess && Type == JsonType.String && _reference != null
        ? Unsafe.As<object, string>(ref Unsafe.AsRef(in _reference))
        : throw ThrowInvalidAccess(JsonType.String);

    public string ErrorMessage => IsError && _reference != null
        ? Unsafe.As<object, string>(ref Unsafe.AsRef(in _reference))
        : string.Empty;

    public ImmutableArray<JsonNode> Array => IsSuccess && Type == JsonType.Array && _reference != null
        ? Unsafe.As<object, ImmutableArray<JsonNode>>(ref Unsafe.AsRef(in _reference))
        : throw ThrowInvalidAccess(JsonType.Array);

    public ImmutableDictionary<string, JsonNode> Object => IsSuccess && Type == JsonType.Object && _reference != null
        ? Unsafe.As<object, ImmutableDictionary<string, JsonNode>>(ref Unsafe.AsRef(in _reference))
        : throw ThrowInvalidAccess(JsonType.Object);

    [MethodImpl(MethodImplOptions.NoInlining)]
    private InvalidOperationException ThrowInvalidAccess(JsonType expected) => new InvalidOperationException(
        $"Invalid Access: Expected '{expected}', but node is '{(!IsSuccess ? "Error" : Type.ToString())}'."
    );
}