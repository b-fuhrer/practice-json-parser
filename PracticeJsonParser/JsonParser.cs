using System.Collections.Immutable;

namespace PracticeJsonParser;
using System.Text;

public static class JsonParser
{
    private const int InvalidHex = -1;
    
    // not implemented yet
    public static JsonValue Parse(ReadOnlySpan<byte> jsonText)
    {
        int currentIndex = 0;
        
        return JsonNull.Instance;
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
        byte firstCharacter = jsonText[currentIndex];

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

    private static bool IsByteDigit(byte byteToCheck)
    {
        return byteToCheck is >= (byte)'0' and <= (byte)'9';
    }

    private static double CalculatePowerOf10(int exponent)
    {
        double result = 1.0;
        int absoluteExponent = Math.Abs(exponent);

        for (int i = 0; i < absoluteExponent; i++)
        {
            result *= 10.0;
        }

        return exponent < 0 ? 1.0 / result : result;
    }

    private static JsonResult<JsonNumber> ParseNumber(ReadOnlySpan<byte> jsonText, int currentIndex)
    {
        double accumulator = 0.0;
        (double numberSign, int newIndex) = jsonText[currentIndex] == (byte)'-'
            ? (-1.0, currentIndex + 1)
            : (1.0, currentIndex);
        int exponentAccumulator = 0;
        int exponentSign = 1;

        while (newIndex < jsonText.Length && IsByteDigit(jsonText[newIndex]))
        {
            int currentDigit = jsonText[newIndex] - (byte)'0';

            accumulator = accumulator * 10 + currentDigit;

            newIndex++;
        }

        if (newIndex < jsonText.Length && jsonText[newIndex] == (byte)'.')
        {
            newIndex++;
            int fractionStartIndex = newIndex;
            double multiplier = 0.1;

            while (newIndex < jsonText.Length && IsByteDigit(jsonText[newIndex]))
            {
                int currentDigit = jsonText[newIndex] - (byte)'0';

                accumulator += currentDigit * multiplier;

                multiplier *= 0.1;

                newIndex++;
            }

            if (newIndex == fractionStartIndex)
            {
                return JsonResult<JsonNumber>.Err(
                    JsonErrorType.InvalidCharacter,
                    "A number is not allowed to end with a decimal point.",
                    newIndex
                );
            }
        }

        if (newIndex < jsonText.Length && (jsonText[newIndex] == (byte)'e' || jsonText[newIndex] == (byte)'E'))
        {
            if (newIndex + 1 < jsonText.Length)
            {
                (exponentSign, newIndex) = jsonText[newIndex + 1] switch
                {
                    (byte)'-' => (-1, newIndex + 2),
                    (byte)'+' => (1, newIndex + 2),
                    _ => (1, newIndex + 1)
                };
            }
            else
            {
                return JsonResult<JsonNumber>.Err(JsonErrorType.EndOfFile, newIndex);
            }

            int exponentStartIndex = newIndex;

            while (newIndex < jsonText.Length && IsByteDigit(jsonText[newIndex]))
            {
                int currentDigit = jsonText[newIndex] - (byte)'0';

                exponentAccumulator = exponentAccumulator * 10 + currentDigit;

                newIndex++;
            }

            if (newIndex == exponentStartIndex)
            {
                return JsonResult<JsonNumber>.Err(
                    JsonErrorType.InvalidCharacter,
                    "A number in scientific notation is not allowed to end with an 'e'/'E'.",
                    newIndex
                );
            }
        }

        double resultNumber = numberSign * accumulator * CalculatePowerOf10(exponentSign * exponentAccumulator);

        if (newIndex < jsonText.Length)
        {
            byte lastCharacter = jsonText[newIndex];

            return lastCharacter switch
            {
                (byte)',' or (byte)']' or (byte)'}' or (byte)' ' or (byte)'\t' or (byte)'\n' or (byte)'\r' =>
                    JsonResult<JsonNumber>.Ok(
                        new JsonNumber(resultNumber),
                        newIndex
                    ),
                (byte)'.' => JsonResult<JsonNumber>.Err(
                    JsonErrorType.InvalidCharacter,
                    "A number is only allowed to have one decimal point.",
                    newIndex
                ),
                (byte)'e' or (byte)'E' => JsonResult<JsonNumber>.Err(
                    JsonErrorType.InvalidCharacter,
                    "A number is only allowed to have one exponent marker 'e'/'E'.",
                    newIndex
                ),
                _ => JsonResult<JsonNumber>.Err(
                    JsonErrorType.InvalidCharacter,
                    $"A number cannot contain the character '{(char)lastCharacter}'.",
                    newIndex
                )
            };
        }

        return JsonResult<JsonNumber>.Ok(new JsonNumber(resultNumber), newIndex); // numbers are allowed at end of file
    }
   
    private static int ParseHexByteIntoInt(byte hexByte)
    {
        return hexByte switch
        {
            >= (byte)'0' and <= (byte)'9' => hexByte - (byte)'0',
            >= (byte)'A' and <= (byte)'F' => hexByte - (byte)'A' + 10, // A = 10 in hex
            >= (byte)'a' and <= (byte)'f' => hexByte - (byte)'a' + 10, // a = 10 in hex
            _ => InvalidHex
        };
    }
    
    private static (char? value, int newIndex, JsonError? error) DecodeUnicodeSequence(ReadOnlySpan<byte> jsonText, int escapedIndex)
    {
        // escapedIndex = index of the 'u'
        if (escapedIndex + 4 >= jsonText.Length)
        {
            return (null, escapedIndex, new JsonError(JsonErrorType.EndOfFile, null));
        }
        
        int leftByte = ParseHexByteIntoInt(jsonText[escapedIndex + 1]);
        int middleLeftByte = ParseHexByteIntoInt(jsonText[escapedIndex + 2]);
        int middleRightByte = ParseHexByteIntoInt(jsonText[escapedIndex + 3]);
        int rightByte = ParseHexByteIntoInt(jsonText[escapedIndex + 4]);
        
        // if any of the "bytes" is negative, their bit-wise OR is also negative
        if ((leftByte | middleLeftByte | middleRightByte | rightByte) < 0)
        {
            return (null, escapedIndex, new JsonError(
                JsonErrorType.InvalidCharacter,
                $"Failed to decode invalid hexadecimal in unicode sequence '\\{Encoding.UTF8.GetString(jsonText.Slice(escapedIndex, 5))}'"
                )
            );
        }

        char parsedSequence = (char)(leftByte << 12 | middleLeftByte << 8 | middleRightByte << 4 | rightByte);

        return (parsedSequence, escapedIndex + 5, null);
    }
    
    private static (char? value, int newIndex, JsonError? error) DecodeEscapedCharacter(ReadOnlySpan<byte> jsonText, int escapedIndex)
    {
        byte escapedCharacter = jsonText[escapedIndex];
        
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
        int amountOfCharsWritten = Encoding.UTF8.GetChars(utf8Slice, stackBuffer);
        stringBuilder.Append(stackBuffer[..amountOfCharsWritten]);
    }
    
    private static JsonResult<JsonString> OnEscapedCharacter(ReadOnlySpan<byte> jsonText, int initialIndex, int currentIndex)
    {
        int newIndex = currentIndex;
        
        const int additionalStartCapacity = 32;
        int scannedLength = currentIndex - initialIndex;
        var scannedChunk = jsonText.Slice(initialIndex, scannedLength);
        var stringBuilder = new StringBuilder(capacity: scannedLength + additionalStartCapacity);
        StringBuilderAppendUtf8(stringBuilder, scannedChunk);
        
        int startOfSegment = currentIndex;
        
        while (newIndex < jsonText.Length)
        {
            byte currentCharacter = jsonText[newIndex];
            
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
                
                (char? escapedCharacter, int nextIndex, var escapedError) = DecodeEscapedCharacter(jsonText, newIndex + 1);

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
        int initialIndex = currentIndex + 1;
        int newIndex = initialIndex;
        
        while (newIndex < jsonText.Length)
        {
            byte currentCharacter = jsonText[newIndex];
            
            if (currentCharacter == (byte)'"')
            {
                string parsedString = Encoding.UTF8.GetString(jsonText.Slice(initialIndex, newIndex - initialIndex));
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
        ImmutableArray<JsonValue>.Builder arrayBuilder = ImmutableArray.CreateBuilder<JsonValue>();
        
        int newIndex = SkipWhitespace(jsonText, currentIndex);
        if (newIndex == jsonText.Length)
        {
            return JsonResult<JsonArray>.Err(JsonErrorType.EndOfFile, newIndex);
        }

        if (jsonText[newIndex] == (byte)']')
        {
            return JsonResult<JsonArray>.Ok(new JsonArray([]), newIndex);
        }
        
        while (newIndex < jsonText.Length)
        {

            var parsedValue = ParseIntoValue(jsonText, newIndex);

            if (!parsedValue.Success)
            {
                return JsonResult<JsonArray>.Err(
                    parsedValue.Error.Value.Type,
                    parsedValue.Error.Value.Message,
                    parsedValue.Index
                );
            }

            arrayBuilder.Add(parsedValue.Value);

            int skipIndex = SkipWhitespace(jsonText, parsedValue.Index);
            if (skipIndex == jsonText.Length)
            {
                return JsonResult<JsonArray>.Err(JsonErrorType.EndOfFile, skipIndex);
            }

            byte characterAfterWhitespace = jsonText[skipIndex];

            if (characterAfterWhitespace == (byte)']')
            {
                return JsonResult<JsonArray>.Ok(new JsonArray(arrayBuilder.ToImmutableArray()), skipIndex + 1);
            }

            if (characterAfterWhitespace != (byte)',')
            {
                return JsonResult<JsonArray>.Err(
                    JsonErrorType.InvalidSyntax,
                    $"Array elements must be separated by ',' character, found '{(char)characterAfterWhitespace}'.",
                    skipIndex
                );
            }

            newIndex = skipIndex + 1;
        }

        return JsonResult<JsonArray>.Err(JsonErrorType.EndOfFile, newIndex);
    }
    
    private static JsonResult<JsonObject> ParseObject(ReadOnlySpan<byte> jsonText, int currentIndex)
    {
        ImmutableDictionary<string, JsonValue>.Builder dictionaryBuilder =
            ImmutableDictionary.CreateBuilder<string, JsonValue>();

        // current index is the index of the opening curly brace '{'
        int newIndex = SkipWhitespace(jsonText, currentIndex + 1);
        if (newIndex == jsonText.Length)
        {
            return JsonResult<JsonObject>.Err(JsonErrorType.EndOfFile, newIndex);
        }

        if (jsonText[newIndex] == (byte)'}')
        {
            return JsonResult<JsonObject>.Ok(new JsonObject([]), newIndex);
        }

        while (newIndex < jsonText.Length)
        {
            if (jsonText[newIndex] != (byte)'"')
            {
                return JsonResult<JsonObject>.Err(
                    JsonErrorType.InvalidSyntax,
                    $"Object keys must be strings and therefore begin with '\"', found '{(char)jsonText[newIndex]}'.",
                    newIndex
                );
            }

            var parsedKey = ParseString(jsonText, newIndex);
            if (!parsedKey.Success)
            {
                return JsonResult<JsonObject>.Err(
                    parsedKey.Error.Value.Type,
                    parsedKey.Error.Value.Message,
                    parsedKey.Index
                );
            }

            int skipAfterKeyIndex = SkipWhitespace(jsonText, parsedKey.Index);
            if (skipAfterKeyIndex == jsonText.Length)
            {
                return JsonResult<JsonObject>.Err(JsonErrorType.EndOfFile, skipAfterKeyIndex);
            }

            if (jsonText[skipAfterKeyIndex] != (byte)':')
            {
                return JsonResult<JsonObject>.Err(
                    JsonErrorType.InvalidSyntax,
                    $"Object keys and values must be separated by ':', found '{(char)jsonText[skipAfterKeyIndex]}'.",
                    skipAfterKeyIndex
                );
            }

            var parsedValue = ParseIntoValue(jsonText, skipAfterKeyIndex + 1);
            if (!parsedValue.Success)
            {
                return JsonResult<JsonObject>.Err(
                    parsedValue.Error.Value.Type,
                    parsedValue.Error.Value.Message,
                    parsedValue.Index
                );
            }

            if (dictionaryBuilder.ContainsKey(parsedKey.Value.String))
            {
                return JsonResult<JsonObject>.Err(
                    JsonErrorType.InvalidSyntax,
                    $"Duplicate key '{parsedKey.Value.String}' found in object.",
                    parsedKey.Index
                );
            }
            dictionaryBuilder.Add(parsedKey.Value.String, parsedValue.Value);

            int skipAfterValueIndex = SkipWhitespace(jsonText, parsedValue.Index);
            if (skipAfterValueIndex == jsonText.Length)
            {
                return JsonResult<JsonObject>.Err(JsonErrorType.EndOfFile, skipAfterValueIndex);
            }

            byte characterAfterWhitespace = jsonText[skipAfterValueIndex];

            if (characterAfterWhitespace == (byte)'}')
            {
                return JsonResult<JsonObject>.Ok(
                    new JsonObject(dictionaryBuilder.ToImmutableDictionary()),
                    skipAfterValueIndex + 1
                );
            }

            if (characterAfterWhitespace != (byte)',')
            {
                return JsonResult<JsonObject>.Err(
                    JsonErrorType.InvalidSyntax,
                    $"Object key-value pairds must be separated by ',' character, found '{(char)characterAfterWhitespace}'.",
                    skipAfterValueIndex
                );
            }

            newIndex = skipAfterValueIndex + 1;
        }

        return JsonResult<JsonObject>.Err(JsonErrorType.EndOfFile, newIndex);
    }
}
