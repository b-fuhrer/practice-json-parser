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

    internal int NumberCount = 0;
    internal int StringCount = 0;
    internal int ArrayElementCount = 0;
    internal int ObjectElementCount = 0;

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

    private static void Resize<T>(ref T[] array, int elementCount)
    {
        int newSize = array.Length * 2;
        var newArray = ArrayPool<T>.Shared.Rent(newSize);

        Array.Copy(array, newArray, elementCount);

        bool clearArray = typeof(T) == typeof(string);
        ArrayPool<T>.Shared.Return(array, clearArray);

        array = newArray;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int AddNumber(double value)
    {
        if (NumberCount < Numbers.Length)
        {
            Numbers[NumberCount] = value;
            return NumberCount++;
        }

        return ResizeAndAddNumber(value);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int AddString(string value)
    {
        if (StringCount < Strings.Length)
        {
            Strings[StringCount] = value;
            return StringCount++;
        }

        return ResizeAndAddString(value);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int AddArrayElement(JsonType type, int index)
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
    public int AddArrayElement(bool boolean)
    {
        var element = new ArrayElement(JsonType.Bool, boolean ? 1 : 0);

        if (ArrayElementCount < ArrayElements.Length)
        {
            ArrayElements[ArrayElementCount] = element;
            return ArrayElementCount++;
        }

        return ResizeAndAddArrayElement(element);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int AddObjectElement(JsonType valueType, int keyIndex, int valueIndex)
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
    public int AddObjectElement(int keyIndex, bool boolean)
    {
        var element = new ObjectElement(JsonType.Bool, keyIndex, boolean ? 1 : 0);

        if (ObjectElementCount < ObjectElements.Length)
        {
            ObjectElements[ObjectElementCount] = element;
            return ObjectElementCount++;
        }

        return ResizeAndAddObjectElement(element);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private int ResizeAndAddNumber(double value)
    {
        Resize(ref Numbers, NumberCount);
        Numbers[NumberCount] = value;
        return NumberCount++;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private int ResizeAndAddString(string value)
    {
        Resize(ref Strings, StringCount);
        Strings[StringCount] = value;
        return StringCount++;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private int ResizeAndAddArrayElement(ArrayElement element)
    {
        Resize(ref ArrayElements, ArrayElementCount);
        ArrayElements[ArrayElementCount] = element;
        return ArrayElementCount++;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private int ResizeAndAddObjectElement(ObjectElement element)
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
    public int Index;

    public JsonType Type => (JsonType)(MetaData >> 29); // access most significant 3 bits
    public uint NextSiblingOffset => MetaData & 0x1FFFFFFF; // access least significant 29 bits

    public ArrayElement(JsonType type, int index)
    {
        Index = index;

        uint defaultOffset = type < JsonType.Array ? 1u : 0u;
        MetaData = ((uint)type << 29) | defaultOffset;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void SetNextSiblingOffset(uint offset)
    {
#if DEBUG
        if (offset > 0x1FFFFFFF)
        {
            throw new OverflowException("Next ArrayElement sibling too far away (Offset > 29 bits)");
        }
#endif
        MetaData = (MetaData & 0xE0000000) | (offset & 0x1FFFFFFF);
    }
}

[StructLayout(LayoutKind.Sequential, Size = 12)]
public struct ObjectElement
{
    internal uint MetaData; // first 3 bits = JsonType, latter 29 bits = next sibling index offset
    public int KeyIndex;
    public int _valueIndex;

    public JsonType Type => (JsonType)(MetaData >> 29); // access most significant 3 bits
    public uint NextSiblingOffset => MetaData & 0x1FFFFFFF; // access least significant 29 bits

    public ObjectElement(JsonType type, int keyIndex, int valueIndex)
    {
        KeyIndex = keyIndex;
        _valueIndex = valueIndex;

        uint defaultOffset = type < JsonType.Array ? 1u : 0u;
        MetaData = ((uint)type << 29) | defaultOffset;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void SetNextSiblingOffset(uint offset)
    {
#if DEBUG
        if (offset > 0x1FFFFFFF)
        {
            throw new OverflowException("JSON too complex (Offset > 29 bits)");
        }
#endif
        MetaData = (MetaData & 0xE0000000) | (offset & 0x1FFFFFFF);
    }
}

[StructLayout(LayoutKind.Sequential, Size = 12)]
public readonly record struct JsonResult(
    int ElementIndex,
    int JsonIndex,
    JsonType Type,
    JsonError Error
)
{
    // accessors
    public bool IsSuccess => Error == JsonError.None;
    public bool IsError => Error != JsonError.None;

    // constructors
    public static JsonResult Ok(JsonType type, int elementIndex, int nextIndex)
    {
        return new JsonResult(elementIndex, nextIndex, type, JsonError.None);
    }

    public static JsonResult Err(JsonError error, JsonType type, int currentIndex)
    {
        return new JsonResult(0, currentIndex, type, error);
    }
}

[StructLayout(LayoutKind.Sequential, Size = 16)]
public readonly struct JsonNode(JsonContext context, int valueIndex, JsonType type, JsonError error, bool isRawValue)
{
    public readonly JsonContext Context = context;
    public readonly JsonType Type = type; // success: result type, error: parsing type context
    public readonly JsonError Error = error;

    // accessors
    public bool IsSuccess => Error == JsonError.None;
    public bool IsError => Error != JsonError.None;

    // getters
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool GetBoolean()
    {
        if (Type != JsonType.Bool)
        {
            throw new InvalidOperationException();
        }

        return isRawValue
            ? valueIndex != 0
            : Context.ArrayElements[valueIndex].Index != 0;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public double GetNumber()
    {
        if (Type != JsonType.Number)
        {
            throw new InvalidOperationException();
        }

        return isRawValue
            ? Context.Numbers[valueIndex]
            : Context.Numbers[Context.ArrayElements[valueIndex].Index];
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public string GetString()
    {
        if (Type != JsonType.String)
        {
            throw new InvalidOperationException();
        }

        return isRawValue
            ? Context.Strings[valueIndex]
            : Context.Strings[Context.ArrayElements[valueIndex].Index];
    }

    /*
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ArrayEnumerator GetArray()
    {
        if (Type != JsonType.Array)
        {
            throw new InvalidOperationException();
        }

        return new ArrayEnumerator(Context, valueIndex);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ObjectEnumerator GetObject()
    {
        if (Type != JsonType.Object)
        {
            throw new InvalidOperationException();
        }

        int startIndex = Context.ArrayElements[valueIndex].NextSiblingOffset <= 1
            ? -1
            : Context.ArrayElements[valueIndex].Index;

        return new ObjectEnumerator(Context, startIndex);
    }
    */

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
