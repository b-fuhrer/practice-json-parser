using System.Runtime.CompilerServices;

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

public enum JsonError : byte
{
    None,
    EndOfFile,
    InvalidCharacter,
    InvalidSyntax,
    EndOfEnumerator
}

public readonly struct JsonNode
{
    public readonly JsonContext Context;
    public readonly int ValueIndex;
    public readonly JsonType Type;
    public readonly JsonError Error;
    public readonly bool IsRawValue;

    // accessors
    public bool IsSuccess => Error == JsonError.None;
    public bool IsError => Error != JsonError.None;

    // primary constructor
    public JsonNode(JsonContext context, int valueIndex, JsonType type, JsonError error, bool isRawValue)
    {
        Context = context;
        ValueIndex = valueIndex;
        Type = type;
        Error = error;
        IsRawValue = isRawValue;
    }

    // getters
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool GetBoolean()
    {
        if (Type != JsonType.Bool)
        {
            throw new InvalidOperationException();
        }

        return IsRawValue
            ? ValueIndex != 0
            : Context.ArrayElements[ValueIndex].Index != 0;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public double GetNumber()
    {
        if (Type != JsonType.Number)
        {
            throw new InvalidOperationException();
        }

        return IsRawValue
            ? Context.Numbers[ValueIndex]
            : Context.Numbers[Context.ArrayElements[ValueIndex].Index];
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public string GetString()
    {
        if (Type != JsonType.String)
        {
            throw new InvalidOperationException();
        }

        return IsRawValue
            ? Context.Strings[ValueIndex]
            : Context.Strings[Context.ArrayElements[ValueIndex].Index];
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ArrayEnumerator GetArray()
    {
        if (Type != JsonType.Array)
        {
            throw new InvalidOperationException();
        }

        return new ArrayEnumerator(Context, ValueIndex);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ObjectEnumerator GetObject()
    {
        if (Type != JsonType.Object)
        {
            throw new InvalidOperationException();
        }

        int startIndex = Context.ArrayElements[ValueIndex].Index;

        return new ObjectEnumerator(Context, startIndex);
    }

    // constructors
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static JsonNode Ok(JsonContext context, JsonType type, int index, bool isRawValue = false)
    {
        return new JsonNode(context, index, type, JsonError.None, isRawValue);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static JsonNode Err(JsonContext context, JsonError error, JsonType type)
    {
        return new JsonNode(context, 0, type, error, false);
    }
}
