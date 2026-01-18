using System.Runtime.InteropServices;
using System.Runtime.CompilerServices;
using System.Collections.Frozen;
namespace PracticeJsonParser;

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

[StructLayout(LayoutKind.Explicit)]
public readonly struct JsonNode
{
    [FieldOffset(0)] public readonly JsonType Tag;
    [FieldOffset(1)] public readonly bool IsSuccess;
    [FieldOffset(2)] private readonly byte _flags;
    [FieldOffset(3)] public readonly ErrorType ErrorType;
    [FieldOffset(4)] public readonly int Index;
    [FieldOffset(8)] private readonly double _number;
    [FieldOffset(8)] private readonly bool _bool;
    [FieldOffset(16)] private readonly object? _reference; // for string, array and FrozenDictionary

    public bool IsNull => IsSuccess && Tag == JsonType.Null;
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
    public static JsonNode OkObject(FrozenDictionary<string, JsonNode> items, int index) =>
        new JsonNode(JsonType.Object, index, reference: items);

    // error constructors
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static JsonNode Err(ErrorType type, string message, int index) =>
        new JsonNode(JsonType.Null, index, success: false, errorType: type, reference: message);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static JsonNode Err(ErrorType type, int index) =>
        new JsonNode(JsonType.Null, index, success: false, errorType: type, reference: null);

    // internal constuctor
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private JsonNode(
        JsonType tag,
        int index,
        bool success = true,
        ErrorType errorType = ErrorType.None,
        double number = 0,
        bool boolean = false,
        object? reference = null)
    {
        this = default;

        Tag = tag;
        Index = index;
        IsSuccess = success;
        ErrorType = errorType;

        if (tag == JsonType.Number) _number = number;
        else if (tag == JsonType.Bool) _bool = boolean;

        _reference = reference;
    }

    // access helpers
    public double Number => IsSuccess && Tag == JsonType.Number
        ? _number
        : throw ThrowInvalidAccess(JsonType.Number);

    public bool Bool => IsSuccess && Tag == JsonType.Bool
        ? _bool
        : throw ThrowInvalidAccess(JsonType.Bool);

    public string String => IsSuccess && Tag == JsonType.String && _reference != null
        ? Unsafe.As<object, string>(ref Unsafe.AsRef(in _reference))
        : throw ThrowInvalidAccess(JsonType.String);

    public string ErrorMessage => IsError && _reference != null
        ? Unsafe.As<object, string>(ref Unsafe.AsRef(in _reference))
        : string.Empty;

    public JsonNode[] Array => IsSuccess && Tag == JsonType.Array && _reference != null
        ? Unsafe.As<object, JsonNode[]>(ref Unsafe.AsRef(in _reference))
        : throw ThrowInvalidAccess(JsonType.Array);

    public FrozenDictionary<string, JsonNode> Object => IsSuccess && Tag == JsonType.Object && _reference != null
        ? Unsafe.As<object, FrozenDictionary<string, JsonNode>>(ref Unsafe.AsRef(in _reference))
        : throw ThrowInvalidAccess(JsonType.Object);

    [MethodImpl(MethodImplOptions.NoInlining)]
    private InvalidOperationException ThrowInvalidAccess(JsonType expected) => new InvalidOperationException(
        $"Invalid Access: Expected '{expected}', but node is '{(!IsSuccess ? "Error" : Tag.ToString())}'."
    );
}