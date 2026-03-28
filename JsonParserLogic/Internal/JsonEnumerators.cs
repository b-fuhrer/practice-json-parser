using System.Runtime.CompilerServices;

namespace JsonParserLogic;

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