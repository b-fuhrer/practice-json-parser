using System.Runtime.CompilerServices;

namespace JsonParserLogic;

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