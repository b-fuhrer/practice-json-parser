namespace PracticeJsonParser;
using System.Text;

public static class JsonParser
{
    // not implemented yet
    public static JsonValue Parse(ReadOnlySpan<byte> jsonText)
    {
        var currentIndex = 0;
        
        return JsonNull.Instance;
    }

    private static JsonResult<JsonValue> ParseIntoValue(ReadOnlySpan<byte> jsonText, int currentIndex)
    {
        var newIndex = SkipWhitespace(jsonText, currentIndex);
        
        if (newIndex == jsonText.Length)
        {
            return JsonResult<JsonValue>.Err(JsonErrorType.EndOfFile, newIndex);
        }
        
        var nextCharacter = jsonText[newIndex];

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
            var currentCharacter = jsonText[currentIndex];
            
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

    private static JsonResult<JsonNull> ParseNull(ReadOnlySpan<byte> jsonText, int currentIndex)
    {
        const int nullLength = 4;

        if (currentIndex + nullLength > jsonText.Length)
        {
            return JsonResult<JsonNull>.Err(JsonErrorType.EndOfFile, currentIndex);
        }

        var slice = jsonText.Slice(currentIndex, nullLength);

        return slice.SequenceEqual("null"u8)
            ? JsonResult<JsonNull>.Ok(JsonNull.Instance, currentIndex + nullLength)
            : JsonResult<JsonNull>.Err(
                JsonErrorType.InvalidSyntax,
                $"Expected 'null', received '{Encoding.UTF8.GetString(slice)}'",
                currentIndex
            );
    }

    private static JsonResult<JsonBool> ParseTrue(ReadOnlySpan<byte> jsonText, int currentIndex)
    {
        const int trueLength = 4;

        if (currentIndex + trueLength > jsonText.Length)
        {
            return JsonResult<JsonBool>.Err(JsonErrorType.EndOfFile, currentIndex);
        }

        var slice = jsonText.Slice(currentIndex, trueLength);

        return slice.SequenceEqual("true"u8)
            ? JsonResult<JsonBool>.Ok(JsonBool.True, currentIndex + trueLength)
            : JsonResult<JsonBool>.Err(
                JsonErrorType.InvalidSyntax,
                $"Expected 'true', received '{Encoding.UTF8.GetString(slice)}'",
                currentIndex
            );
    }

    private static JsonResult<JsonBool> ParseFalse(ReadOnlySpan<byte> jsonText, int currentIndex)
    {
        const int falseLength = 5;

        if (currentIndex + falseLength > jsonText.Length)
        {
            return JsonResult<JsonBool>.Err(JsonErrorType.EndOfFile, currentIndex);
        }

        var slice = jsonText.Slice(currentIndex, falseLength);

        return slice.SequenceEqual("false"u8)
            ? JsonResult<JsonBool>.Ok(JsonBool.False, currentIndex + falseLength)
            : JsonResult<JsonBool>.Err(
                JsonErrorType.InvalidSyntax,
                $"Expected 'false', received '{Encoding.UTF8.GetString(slice)}'",
                currentIndex
            );
    }

    private static JsonResult<JsonBool> ParseBool(ReadOnlySpan<byte> jsonText, int currentIndex)
    {
        var firstCharacter = jsonText[currentIndex];

        return firstCharacter switch
        {
            (byte)'t' => ParseTrue(jsonText, currentIndex),
            (byte)'f' => ParseFalse(jsonText, currentIndex),
            _ => JsonResult<JsonBool>.Err(
                JsonErrorType.InvalidSyntax,
                $"Expected 't' (true) or 'f' (false), received '{firstCharacter}'",
                currentIndex
            )
        };
    }
    
    private static JsonResult<JsonNumber> ParseNumber(ReadOnlySpan<byte> jsonText, int currentIndex)
    {
        return JsonResult<JsonNumber>.Ok(new JsonNumber(1), currentIndex);
    }

    private static (char? value, int newIndex) DecodeUnicodeSequence(ReadOnlySpan<byte> jsonText, int escapedIndex)
    {
        return (null, escapedIndex);
    }
    
    private static (char? value, int newIndex) DecodeEscapedCharacter(ReadOnlySpan<byte> jsonText, int escapedIndex)
    {
        var escapedCharacter = jsonText[escapedIndex];
        
        return escapedCharacter switch
        {
            (byte)'b' => ('\b', escapedIndex + 1),
            (byte)'f' => ('\f', escapedIndex + 1),
            (byte)'n' => ('\n', escapedIndex + 1),
            (byte)'r' => ('\r', escapedIndex + 1),
            (byte)'t' => ('\t', escapedIndex + 1),
            (byte)'\\' => ('\\', escapedIndex + 1),
            (byte)'/' => ('/', escapedIndex + 1),
            (byte)'"' => ('"', escapedIndex + 1),
            (byte)'u' => DecodeUnicodeSequence(jsonText, escapedIndex),
            _ => (null, escapedIndex)
        };
    }
    
    private static JsonResult<JsonString> OnEscapedCharacter(ReadOnlySpan<byte> jsonText, int initialIndex, int currentIndex)
    {
        var newIndex = currentIndex;
        
        const int additionalStartCapacity = 32;
        var scannedLength = currentIndex - initialIndex;
        var scannedChunk = Encoding.UTF8.GetString(jsonText.Slice(initialIndex, scannedLength));
        var stringBuilder = new StringBuilder(scannedChunk, scannedLength + additionalStartCapacity);

        while (newIndex < jsonText.Length)
        {
            var currentCharacter = jsonText[newIndex];
            
            if (currentCharacter == (byte)'"')
            {
                return JsonResult<JsonString>.Ok(new JsonString(stringBuilder.ToString()), newIndex + 1);
            }

            if (currentCharacter == (byte)'\\')
            {
                if (newIndex + 1 >= jsonText.Length)
                {
                    return JsonResult<JsonString>.Err(JsonErrorType.EndOfFile, newIndex);
                }
                
                var (nextEscapedCharacter, nextIndex) = DecodeEscapedCharacter(jsonText, newIndex + 1);
                
                if (nextEscapedCharacter is null)
                {
                    return JsonResult<JsonString>.Err(
                        JsonErrorType.InvalidCharacter,
                        $"Failed to decode escaped character '\\{(char)jsonText[newIndex + 1]}'",
                        newIndex
                    );
                }
                
                stringBuilder.Append(nextEscapedCharacter);
                newIndex = nextIndex;
                continue;
            }

            stringBuilder.Append((char)currentCharacter);
            newIndex++;
        }
        
        return JsonResult<JsonString>.Err(JsonErrorType.EndOfFile, newIndex);
    }
    
    private static JsonResult<JsonString> ParseString(ReadOnlySpan<byte> jsonText, int currentIndex)
    {
        var initialIndex = currentIndex + 1; // first character after the opening "
        var newIndex = initialIndex; 
        
        while (newIndex < jsonText.Length)
        {
            var currentCharacter = jsonText[newIndex];
            
            if (currentCharacter == (byte)'"')
            {
                var parsedString = Encoding.UTF8.GetString(jsonText.Slice(initialIndex, newIndex - initialIndex));
                return JsonResult<JsonString>.Ok(new JsonString(parsedString), newIndex + 1);
            }
            
            if (currentCharacter == (byte)'\\')
            {
                return OnEscapedCharacter(jsonText, initialIndex, newIndex);
            }
            
            newIndex++;
        }

        return JsonResult<JsonString>.Err(JsonErrorType.EndOfFile, newIndex);
    }
    
    private static JsonResult<JsonArray> ParseArray(ReadOnlySpan<byte> jsonText, int currentIndex)
    {
        return JsonResult<JsonArray>.Ok(new JsonArray([]), currentIndex);
    }
    
    private static JsonResult<JsonObject> ParseObject(ReadOnlySpan<byte> jsonText, int currentIndex)
    {
        return JsonResult<JsonObject>.Ok(new JsonObject([]), currentIndex);
    }
}
