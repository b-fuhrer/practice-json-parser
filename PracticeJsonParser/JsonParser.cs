namespace PracticeJsonParser;
using System.Text;

public static class JsonParser
{
    private const int invalidHex = -1;
    
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
   
    private static int ParseHexByteIntoInt(byte hexByte)
    {
        return hexByte switch
        {
            >= (byte)'0' and <= (byte)'9' => hexByte - (byte)'0',
            >= (byte)'A' and <= (byte)'F' => hexByte - (byte)'A' + 10, // A = 10 in hex
            >= (byte)'a' and <= (byte)'f' => hexByte - (byte)'a' + 10, // a = 10 in hex
            _ => invalidHex
        };
    }
    
    private static (char? value, int newIndex, JsonError? error) DecodeUnicodeSequence(ReadOnlySpan<byte> jsonText, int escapedIndex)
    {
        // escapedIndex = index of the 'u'
        if (escapedIndex + 4 >= jsonText.Length)
        {
            return (null, escapedIndex, new JsonError(JsonErrorType.EndOfFile, null));
        }
        
        var leftByte = ParseHexByteIntoInt(jsonText[escapedIndex + 1]);
        var middleLeftByte = ParseHexByteIntoInt(jsonText[escapedIndex + 2]);
        var middleRightByte = ParseHexByteIntoInt(jsonText[escapedIndex + 3]);
        var rightByte = ParseHexByteIntoInt(jsonText[escapedIndex + 4]);
        
        // if any of the "bytes" is negative, their bit-wise OR is also negative
        if ((leftByte | middleLeftByte | middleRightByte | rightByte) < 0)
        {
            return (null, escapedIndex, new JsonError(
                JsonErrorType.InvalidCharacter,
                $"Failed to decode invalid hexadecimal in unicode sequence '\\{Encoding.UTF8.GetString(jsonText.Slice(escapedIndex, 5))}'"
                )
            );
        }

        var parsedSequence = (char)(leftByte << 12 | middleLeftByte << 8 | middleRightByte << 4 | rightByte);

        return (parsedSequence, escapedIndex + 5, null);
    }
    
    private static (char? value, int newIndex, JsonError? error) DecodeEscapedCharacter(ReadOnlySpan<byte> jsonText, int escapedIndex)
    {
        var escapedCharacter = jsonText[escapedIndex];
        
        return escapedCharacter switch
        {
            (byte)'b' => ('\b', escapedIndex + 1, null),
            (byte)'f' => ('\f', escapedIndex + 1, null),
            (byte)'n' => ('\n', escapedIndex + 1, null),
            (byte)'r' => ('\r', escapedIndex + 1, null),
            (byte)'t' => ('\t', escapedIndex + 1, null),
            (byte)'\\' => ('\\', escapedIndex + 1, null),
            (byte)'/' => ('/', escapedIndex + 1, null),
            (byte)'"' => ('"', escapedIndex + 1, null),
            (byte)'u' => DecodeUnicodeSequence(jsonText, escapedIndex),
            _ => (null, escapedIndex, new JsonError(
                JsonErrorType.InvalidCharacter, 
                $"Failed to decode escaped character '\\{escapedCharacter}'"
                )
            )
        };
    }

    private static void StringBuilderAppendUtf8(StringBuilder stringBuilder, ReadOnlySpan<byte> utf8Slice)
    {
        // only allocate maximum of 1KB on the stack here
        if (utf8Slice.Length > 512)
        {
            stringBuilder.Append(Encoding.UTF8.GetString(utf8Slice));
            return;
        }

        Span<char> stackBuffer = stackalloc char[utf8Slice.Length];
        var amountOfCharsWritten = Encoding.UTF8.GetChars(utf8Slice, stackBuffer);
        stringBuilder.Append(stackBuffer[..amountOfCharsWritten]);
    }
    
    private static JsonResult<JsonString> OnEscapedCharacter(ReadOnlySpan<byte> jsonText, int initialIndex, int currentIndex)
    {
        var newIndex = currentIndex;
        
        const int additionalStartCapacity = 32;
        var scannedLength = currentIndex - initialIndex;
        var scannedChunk = jsonText.Slice(initialIndex, scannedLength);
        var stringBuilder = new StringBuilder(capacity: scannedLength + additionalStartCapacity);
        StringBuilderAppendUtf8(stringBuilder, scannedChunk);
        
        var startOfSegment = currentIndex;
        
        while (newIndex < jsonText.Length)
        {
            var currentCharacter = jsonText[newIndex];
            
            if (currentCharacter == (byte)'"')
            {
                if (startOfSegment != newIndex)
                {
                    StringBuilderAppendUtf8(stringBuilder, jsonText.Slice(startOfSegment, newIndex - startOfSegment));
                }
                
                return JsonResult<JsonString>.Ok(new JsonString(stringBuilder.ToString()), newIndex + 1);
            }

            if (currentCharacter == (byte)'\\')
            {
                if (startOfSegment != newIndex)
                {
                    StringBuilderAppendUtf8(stringBuilder, jsonText.Slice(startOfSegment, newIndex - startOfSegment));
                }
                
                if (newIndex + 1 >= jsonText.Length)
                {
                    return JsonResult<JsonString>.Err(JsonErrorType.EndOfFile, newIndex);
                }
                
                var (escapedCharacter, nextIndex, escapedError) = DecodeEscapedCharacter(jsonText, newIndex + 1);

                if (escapedError is not null)
                {
                    return JsonResult<JsonString>.Err(escapedError.Value, nextIndex);
                }
                
                stringBuilder.Append(escapedCharacter);
                startOfSegment = nextIndex;
                newIndex = nextIndex;
                continue;
            }

            newIndex++;
        }
        
        return JsonResult<JsonString>.Err(JsonErrorType.EndOfFile, newIndex);
    }
    
    private static JsonResult<JsonString> ParseString(ReadOnlySpan<byte> jsonText, int currentIndex)
    {
        // initialIndex = first character after the opening "
        var initialIndex = currentIndex + 1;
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
