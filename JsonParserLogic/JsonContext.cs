using System.Buffers;
using System.Runtime.CompilerServices;

namespace JsonParserLogic;

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
