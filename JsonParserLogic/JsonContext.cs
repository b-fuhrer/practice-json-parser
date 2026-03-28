using System.Buffers;
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

public struct ArrayEnumerator
{
    private readonly JsonContext _context;
    private int _currentIndex;
    private readonly int _endIndex;

    public ArrayEnumerator(JsonContext context, int headerIndex)
    {
        _context = context;
        _currentIndex = headerIndex + 1;

        uint offset = context.ArrayElements[headerIndex].NextSiblingOffset;
        _endIndex = headerIndex + (int)(offset > 0 ? offset : 1);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public JsonNode GetNext()
    {
        if (_currentIndex >= _endIndex)
        {
            return JsonNode.Err(_context, JsonError.EndOfEnumerator, JsonType.Null);
        }

        JsonNode node = JsonNode.Ok(_context, _context.ArrayElements[_currentIndex].Type, _currentIndex, false);
        _currentIndex += (int)_context.ArrayElements[_currentIndex].NextSiblingOffset;

        return node;
    }
}

public struct ObjectElement
{
    internal uint MetaData; // first 3 bits = JsonType, latter 29 bits = next sibling index offset
    public int KeyIndex;
    public int ValueIndex;

    public JsonType Type => (JsonType)(MetaData >> 29); // access most significant 3 bits
    public uint NextSiblingOffset => MetaData & 0x1FFFFFFF; // access least significant 29 bits

    public ObjectElement(JsonType type, int keyIndex, int valueIndex)
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
        if (offset > 0x1FFFFFFF)
        {
            throw new OverflowException("JSON too complex (Offset > 29 bits)");
        }
#endif
        MetaData = (MetaData & 0xE0000000) | (offset & 0x1FFFFFFF);
    }
}

public readonly record struct ObjectProperty(
    string Key,
    JsonNode Value
);

public struct ObjectEnumerator
{
    private readonly JsonContext _context;
    private int _currentIndex;

    public ObjectEnumerator(JsonContext context, int startIndex)
    {
        _context = context;
        _currentIndex = startIndex;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ObjectProperty GetNext()
    {
        if (_currentIndex == -1)
        {
            return new ObjectProperty(null!, JsonNode.Err(_context, JsonError.EndOfEnumerator, JsonType.Null));
        }

        ref ObjectElement element = ref _context.ObjectElements[_currentIndex];
        bool isDirect = element.Type < JsonType.Array;

        var property = new ObjectProperty(
            _context.Strings[element.KeyIndex],
            JsonNode.Ok(_context, element.Type, element.ValueIndex, isDirect)
        );

        uint offset = element.NextSiblingOffset;
        _currentIndex = offset == 0 ? -1 : _currentIndex + (int)offset;

        return property;
    }
}

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

        int startIndex = Context.ArrayElements[ValueIndex].NextSiblingOffset <= 1
            ? -1
            : Context.ArrayElements[ValueIndex].Index;

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
