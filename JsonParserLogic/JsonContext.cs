using System.Buffers;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.JavaScript;

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
    InvalidSyntax
}

public class JsonContext : IDisposable
{
    internal double[] Numbers;
    internal string[] Strings;
    internal ArrayElement[] ArrayElements;
    internal ObjectElement[] ObjectElements;

    internal uint NumberCount = 0;
    internal uint StringCount = 0;
    internal uint ArrayElementCount = 0;
    internal uint ObjectElementCount = 0;

    public JsonContext(int jsonLength)
    {
        int initialSize = Math.Max(256, jsonLength / 8);

        Numbers = ArrayPool<double>.Shared.Rent(initialSize);
        Strings = ArrayPool<string>.Shared.Rent(initialSize);
        ArrayElements = ArrayPool<ArrayElement>.Shared.Rent(initialSize);
        ObjectElements = ArrayPool<ObjectElement>.Shared.Rent(initialSize);
    }

    public void Dispose()
    {
        if (Numbers.Length > 0)
        {
            ArrayPool<double>.Shared.Return(Numbers);
            Numbers = [];
        }

        if (ArrayElements.Length > 0)
        {
            ArrayPool<ArrayElement>.Shared.Return(ArrayElements);
            ArrayElements = [];
        }

        if (ObjectElements.Length > 0)
        {
            ArrayPool<ObjectElement>.Shared.Return(ObjectElements);
            ObjectElements = [];
        }

        if (Strings.Length > 0)
        {
            ArrayPool<string>.Shared.Return(Strings, clearArray: true);
            Strings = [];
        }
    }

    private static void Resize<T>(ref T[] array, uint elementCount)
    {
        int newSize = array.Length * 2;
        var newArray = ArrayPool<T>.Shared.Rent(newSize);

        Array.Copy(array, newArray, elementCount);

        bool clearArray = typeof(T) == typeof(string);
        ArrayPool<T>.Shared.Return(array, clearArray);

        array = newArray;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public uint AddNumber(double value)
    {
        if (NumberCount < Numbers.Length)
        {
            Numbers[NumberCount] = value;
            return NumberCount++;
        }

        return ResizeAndAddNumber(value);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public uint AddString(string value)
    {
        if (StringCount < Strings.Length)
        {
            Strings[StringCount] = value;
            return StringCount++;
        }
        return ResizeAndAddString(value);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public uint AddArrayElement(JsonType type, uint index)
    {
        var element = new ArrayElement(type, index);

        if (ArrayElementCount < ArrayElements.Length)
        {
            ArrayElements[ArrayElementCount] = element;
            return ArrayElementCount++;
        }
        return ResizeAndAddArrayElement(element);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public uint AddArrayElement(bool boolean)
    {
        var element = new ArrayElement(JsonType.Bool, boolean ? 1u : 0u);

        if (ArrayElementCount < ArrayElements.Length)
        {
            ArrayElements[ArrayElementCount] = element;
            return ArrayElementCount++;
        }
        return ResizeAndAddArrayElement(element);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public uint AddObjectElement(JsonType valueType, uint keyIndex, uint valueIndex)
    {
        var element = new ObjectElement(valueType, keyIndex, valueIndex);

        if (ObjectElementCount < ObjectElements.Length)
        {
            ObjectElements[ObjectElementCount] = element;
            return ObjectElementCount++;
        }
        return ResizeAndAddObjectElement(element);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public uint AddObjectElement(uint keyIndex, bool boolean)
    {
        var element = new ObjectElement(JsonType.Bool, keyIndex, boolean ? 1u : 0u);

        if (ObjectElementCount < ObjectElements.Length)
        {
            ObjectElements[ObjectElementCount] = element;
            return ObjectElementCount++;
        }
        return ResizeAndAddObjectElement(element);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private uint ResizeAndAddNumber(double value)
    {
        Resize(ref Numbers, NumberCount);
        Numbers[NumberCount] = value;
        return NumberCount++;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private uint ResizeAndAddString(string value)
    {
        Resize(ref Strings, StringCount);
        Strings[StringCount] = value;
        return StringCount++;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private uint ResizeAndAddArrayElement(ArrayElement element)
    {
        Resize(ref ArrayElements, ArrayElementCount);
        ArrayElements[ArrayElementCount] = element;
        return ArrayElementCount++;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private uint ResizeAndAddObjectElement(ObjectElement element)
    {
        Resize(ref ObjectElements, ObjectElementCount);
        ObjectElements[ObjectElementCount] = element;
        return ObjectElementCount++;
    }
}

[StructLayout(LayoutKind.Sequential, Size = 8)]
public struct ArrayElement
{
    internal uint MetaData; // first 3 bits = JsonType, latter 29 bits = next sibling index offset
    public uint Index;

    public JsonType Type => (JsonType)(MetaData >> 29); // access most significant 3 bits
    public uint NextSiblingOffset => MetaData & 0x1FFFFFFF; // access least significant 29 bits

    public ArrayElement(JsonType type, uint index)
    {
        Index = index;

        uint defaultOffset = type < JsonType.Array ? 1u : 0u;
        MetaData = ((uint)type << 29) | defaultOffset;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void SetNextSiblingOffset(uint offset)
    {
#if DEBUG
        if (offset > 0x1FFFFFFF) throw new OverflowException("JSON too complex (Offset > 29 bits)");
#endif
        MetaData = (MetaData & 0xE0000000) | (offset & 0x1FFFFFFF);
    }
}

[StructLayout(LayoutKind.Sequential, Size = 12)]
public struct ObjectElement
{
    internal uint MetaData; // first 3 bits = JsonType, latter 29 bits = next sibling index offset
    public uint KeyIndex;
    public uint ValueIndex;

    public JsonType Type => (JsonType)(MetaData >> 29); // access most significant 3 bits
    public uint NextSiblingOffset => MetaData & 0x1FFFFFFF; // access least significant 29 bits

    public ObjectElement(JsonType type, uint keyIndex, uint valueIndex)
    {
        KeyIndex = keyIndex;
        ValueIndex = valueIndex;

        uint defaultOffset = type < JsonType.Array ? 1u : 0u;
        MetaData = ((uint)type << 29) | defaultOffset;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void SetNextSiblingOffset(uint offset)
    {
#if DEBUG
        if (offset > 0x1FFFFFFF) throw new OverflowException("JSON too complex (Offset > 29 bits)");
#endif
        MetaData = (MetaData & 0xE0000000) | (offset & 0x1FFFFFFF);
    }
}

[StructLayout(LayoutKind.Sequential, Size = 16)]
public readonly record struct JsonNode(
    JsonContext Context,
    uint Index,
    JsonType Type, // success: result type, error: parsing type context
    JsonError Error
)
{
    // accessors
    public bool IsSuccess => Error == JsonError.None;
    public bool IsError => Error != JsonError.None;

    // constructors
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static JsonNode Ok(JsonContext context, JsonType type, uint index)
    {
        return new JsonNode(context, index, type, JsonError.None);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static JsonNode Err(JsonContext context, JsonError error, JsonType type)
    {
        return new JsonNode(context, 0, type, error);
    }
}

[StructLayout(LayoutKind.Sequential, Size = 12)]
public readonly record struct JsonResult(
    uint ElementIndex,
    int PositionIndex,
    JsonType Type,
    JsonError Error
)
{
    // accessors
    public bool IsSuccess => Error == JsonError.None;
    public bool IsError => Error != JsonError.None;

    // constructors
    public static JsonResult Ok(JsonType type, uint elementIndex, int nextIndex)
    {
        return new JsonResult(elementIndex, nextIndex, type, JsonError.None);
    }

    public static JsonResult Err(JsonError error, JsonType type, int currentIndex)
    {
        return new JsonResult(0, currentIndex, type, error);
    }
}