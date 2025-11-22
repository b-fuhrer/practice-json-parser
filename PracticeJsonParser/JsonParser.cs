namespace PracticeJsonParser;

public static class JsonParser
{
    // not implemented yet
    public static JsonValue Parse(ReadOnlySpan<char> jsonText)
    {
        var currentIndex = 0;
        
        return JsonNull.Instance;
    }

    private static JsonResult<JsonValue> ParseIntoValue(ReadOnlySpan<char> jsonText, int currentIndex)
    {
        var newIndex = SkipWhitespace(jsonText, currentIndex);
        
        if (newIndex == jsonText.Length) return JsonResult<JsonValue>.Err(JsonErrorType.EndOfFile, newIndex);
        
        var currentCharacter = jsonText[currentIndex];

        var parsedValue = currentCharacter switch
        {
            'n' => ParseNull(jsonText, currentIndex),
            't' or 'f' => ParseBool(jsonText, currentIndex),
            '-' or >= '0' and <= '9' => ParseNumber(jsonText, currentIndex),
            '"' => ParseString(jsonText, currentIndex),
            '[' => ParseArray(jsonText, currentIndex),
            '{' => ParseObject(jsonText, currentIndex),
            _ => JsonResult<JsonValue>.Err(
                    JsonErrorType.InvalidCharacter, $"Invalid character: {currentCharacter}", newIndex
                )
        };

        return parsedValue;
    }
    
    private static int SkipWhitespace(ReadOnlySpan<char> jsonText, int currentIndex)
    {
        while (currentIndex < jsonText.Length)
        {
            var currentCharacter = jsonText[currentIndex];
            
            // treats all control characters (ASCII < 32) as whitespace
            // includes JSON whitespace characters: '\t', '\n', '\r'
            if (currentCharacter > ' ') return currentIndex;
            
            currentIndex++;
        }
        
        return currentIndex;
    }
    
    private static JsonResult<JsonNull> ParseNull(ReadOnlySpan<char> jsonText, int currentIndex)
    {
        return JsonResult<JsonNull>.Ok(JsonNull.Instance, currentIndex);
    }
    
    private static JsonResult<JsonBool> ParseBool(ReadOnlySpan<char> jsonText, int currentIndex)
    {
        return JsonResult<JsonBool>.Ok(new JsonBool(true), currentIndex);
    }
    
    private static JsonResult<JsonNumber> ParseNumber(ReadOnlySpan<char> jsonText, int currentIndex)
    {
        return JsonResult<JsonNumber>.Ok(new JsonNumber(1), currentIndex);
    }
    
    private static JsonResult<JsonString> ParseString(ReadOnlySpan<char> jsonText, int currentIndex)
    {
        return JsonResult<JsonString>.Ok(new JsonString(""), currentIndex);
    }
    
    private static JsonResult<JsonArray> ParseArray(ReadOnlySpan<char> jsonText, int currentIndex)
    {
        return JsonResult<JsonArray>.Ok(new JsonArray([]), currentIndex);
    }
    
    private static JsonResult<JsonObject> ParseObject(ReadOnlySpan<char> jsonText, int currentIndex)
    {
        return JsonResult<JsonObject>.Ok(new JsonObject([]), currentIndex);
    }
}
