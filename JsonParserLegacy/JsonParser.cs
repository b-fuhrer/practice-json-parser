using JsonParserLegacy.Types;
namespace JsonParserLegacy;

public static partial class JsonParser
{
    public static JsonResult<JsonValue> Parse(ReadOnlySpan<byte> jsonText)
    {
        if (jsonText.Length == 0)
        {
            return JsonResult<JsonValue>.Err(JsonErrorType.EndOfFile, "JSON has no content.", 0);
        }

        var parsedJson = ParseIntoValue(jsonText, 0);
        if (!parsedJson.Success)
        {
            return parsedJson;
        }

        int skipIndex = SkipWhitespace(jsonText, parsedJson.Index);

        return skipIndex == jsonText.Length
            ? parsedJson
            : JsonResult<JsonValue>.Err(
                JsonErrorType.InvalidCharacter,
                "JSON contains garbage after the parsed content.",
                skipIndex
            );
    }

    private static JsonResult<JsonValue> ParseIntoValue(ReadOnlySpan<byte> jsonText, int currentIndex)
    {
        int newIndex = SkipWhitespace(jsonText, currentIndex);
        
        if (newIndex == jsonText.Length)
        {
            return JsonResult<JsonValue>.Err(JsonErrorType.EndOfFile, newIndex);
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
            _ => JsonResult<JsonValue>.Err(
                JsonErrorType.InvalidCharacter,
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
}