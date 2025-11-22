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
        const int nullLength = 4;

        if (currentIndex + nullLength > jsonText.Length)
        {
            return JsonResult<JsonNull>.Err(JsonErrorType.EndOfFile, currentIndex);
        }

        var possibleNullSlice = jsonText.Slice(currentIndex, nullLength);

        return possibleNullSlice switch
        {
            "null" => JsonResult<JsonNull>.Ok(JsonNull.Instance, currentIndex + nullLength),
            _ => JsonResult<JsonNull>.Err(
                JsonErrorType.InvalidSyntax, $"Expected 'null', received '{possibleNullSlice}'", currentIndex
            )
        };
    }

    private static JsonResult<JsonBool> ParseTrue(ReadOnlySpan<char> jsonText, int currentIndex)
    {
        const int trueLength = 4;

        if (currentIndex + trueLength > jsonText.Length)
        {
            return JsonResult<JsonBool>.Err(JsonErrorType.EndOfFile, currentIndex);
        }

        var possibleTrueSlice = jsonText.Slice(currentIndex, trueLength);

        return possibleTrueSlice switch
        {
            "true" => JsonResult<JsonBool>.Ok(new JsonBool(true), currentIndex + trueLength),
            _ => JsonResult<JsonBool>.Err(
                JsonErrorType.InvalidSyntax, $"Expected 'true', received '{possibleTrueSlice}'", currentIndex
            )
        };
    }
    
    private static JsonResult<JsonBool> ParseFalse(ReadOnlySpan<char> jsonText, int currentIndex)
    {
        const int falseLength = 5;

        if (currentIndex + falseLength > jsonText.Length)
        {
            return JsonResult<JsonBool>.Err(JsonErrorType.EndOfFile, currentIndex);
        }

        var possibleFalseSlice = jsonText.Slice(currentIndex, falseLength);

        return possibleFalseSlice switch
        {
            "false" => JsonResult<JsonBool>.Ok(new JsonBool(false), currentIndex + falseLength),
            _ => JsonResult<JsonBool>.Err(
                JsonErrorType.InvalidSyntax, $"Expected 'false', received '{possibleFalseSlice}'", currentIndex
            )
        };
    }

    /*
    // generic ParseBoolText -> lower performance but less code redundancy
    private static JsonResult<JsonBool> ParseBoolText(ReadOnlySpan<char> jsonText, int currentIndex, string boolText)
    {
        var boolTextLength = boolText.Length;
        
        if (currentIndex + boolTextLength > jsonText.Length)
        {
            return JsonResult<JsonBool>.Err(JsonErrorType.EndOfFile, currentIndex);
        }

        var possibleBoolSlice = jsonText.Slice(currentIndex, boolTextLength);

        return possibleBoolSlice switch
        {
            "true" => JsonResult<JsonBool>.Ok(new JsonBool(true), currentIndex + boolTextLength),
            "false" => JsonResult<JsonBool>.Ok(new JsonBool(false), currentIndex + boolTextLength),
            _ => JsonResult<JsonBool>.Err(
                JsonErrorType.InvalidSyntax, $"Expected '{boolText}', received '{possibleBoolSlice}'", currentIndex
            )
        };
    }
    */
    
    private static JsonResult<JsonBool> ParseBool(ReadOnlySpan<char> jsonText, int currentIndex)
    {
        var firstCharacter = jsonText[currentIndex];

        return firstCharacter switch
        {
            't' => ParseTrue(jsonText, currentIndex), // ParseBoolText(jsonText, currentIndex, "true")
            'f' => ParseFalse(jsonText, currentIndex),  // ParseBoolText(jsonText, currentIndex, "false")
            _ => JsonResult<JsonBool>.Err(
                JsonErrorType.InvalidSyntax, $"Expected 't' (true) or 'f' (false), received '{firstCharacter}'",
                currentIndex
            )
        };
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
