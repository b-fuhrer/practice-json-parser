using System.Runtime.CompilerServices;
namespace JsonParserLogic;

public static partial class JsonParser
{
    public static JsonNode Parse(ReadOnlySpan<byte> jsonText)
    {
        if (jsonText.Length == 0)
        {
            return JsonNode.Err(ErrorType.EndOfFile, "JSON has no content", 0);
        }

        JsonNode parsedJson = ParseIntoValue(jsonText, 0);
        if (!parsedJson.IsSuccess)
        {
            return parsedJson;
        }

        int skipIndex = SkipWhitespace(jsonText, parsedJson.Index);

        return skipIndex == jsonText.Length
            ? parsedJson
            : JsonNode.Err(
                ErrorType.InvalidCharacter,
                "JSON contains garbage after the parsed content.",
                skipIndex
            );
    }

    private static JsonNode ParseIntoValue(ReadOnlySpan<byte> jsonText, int currentIndex)
    {
        int newIndex = SkipWhitespace(jsonText, currentIndex);
        
        if (newIndex == jsonText.Length)
        {
            return JsonNode.Err(ErrorType.EndOfFile, newIndex);
        }
        
        byte nextCharacter = jsonText[newIndex];

        var parsedValue = nextCharacter switch
        {
            (byte)'n' => ParseNull(jsonText, newIndex),
            (byte)'t' or (byte)'f' => ParseBool(jsonText, newIndex),
            (byte)'-' or >= (byte)'0' and <= (byte)'9' => ParseNumber(jsonText, newIndex),
            (byte)'"' => ParseString(jsonText, newIndex),
            (byte)'[' => ParseArray(jsonText, newIndex),
            (byte)'{' => ParseObject(jsonText, newIndex),
            _ => JsonNode.Err(
                ErrorType.InvalidCharacter,
                $"Invalid character: {nextCharacter}",
                newIndex
            )
        };

        return parsedValue;
    }

    private static int SkipWhitespace(ReadOnlySpan<byte> jsonText, int currentIndex)
    {
        while (currentIndex < jsonText.Length)
        {
            byte currentCharacter = jsonText[currentIndex];
            
            // treats all control characters (ASCII < 32) as whitespace
            // includes all JSON whitespace characters: '\t', '\n', '\r'
            if (currentCharacter > (byte)' ')
            {
                return currentIndex;
            }
            
            currentIndex++;
        }
        
        return currentIndex;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static bool IsSeparator(byte value)
    {
        return value is (byte)',' or (byte)']' or (byte)'}' or (byte)' ' or (byte)'\t' or (byte)'\n' or (byte)'\r';
    }
}