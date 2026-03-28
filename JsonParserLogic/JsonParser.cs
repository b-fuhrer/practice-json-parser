using System.Runtime.CompilerServices;

namespace JsonParserLogic;

public static partial class JsonParser
{
    public static JsonNode Parse(JsonContext context, ReadOnlySpan<byte> jsonText)
    {
        if (jsonText.Length == 0)
        {
            return JsonNode.Err(context, JsonError.EndOfFile, JsonType.Null);
        }

        int startIndex = SkipWhitespace(jsonText, 0);
        if (startIndex == jsonText.Length)
        {
            return JsonNode.Err(context, JsonError.EndOfFile, JsonType.Null);
        }

        JsonResult parsedJson = ParseIntoValue(context, jsonText, startIndex);
        if (parsedJson.IsError)
        {
            return JsonNode.Err(context, parsedJson.Error, parsedJson.Type);
        }

        int skipIndex = SkipWhitespace(jsonText, parsedJson.JsonIndex);
        if (skipIndex != jsonText.Length)
        {
            return JsonNode.Err(context, JsonError.InvalidCharacter, parsedJson.Type);
        }

        if (parsedJson.Type < JsonType.Array)
        {
            int rootIndex = parsedJson.Type == JsonType.Bool
                ? context.AddArrayElement(parsedJson.ElementIndex != 0)
                : context.AddArrayElement(parsedJson.Type, parsedJson.ElementIndex);

            return JsonNode.Ok(context, parsedJson.Type, rootIndex, isRawValue: false);
        }

        return JsonNode.Ok(context, parsedJson.Type, parsedJson.ElementIndex, isRawValue: false);
    }

    internal static JsonResult ParseIntoValue(JsonContext context, ReadOnlySpan<byte> jsonText, int currentIndex)
    {
        int newIndex = SkipWhitespace(jsonText, currentIndex);

        if (newIndex == jsonText.Length)
        {
            return JsonResult.Err(JsonError.EndOfFile, JsonType.Null, newIndex);
        }

        byte nextCharacter = jsonText[newIndex];

        return nextCharacter switch
        {
            (byte)'n' => ParseNull(jsonText, newIndex),
            (byte)'t' or (byte)'f' => ParseBool(jsonText, newIndex),
            (byte)'-' or >= (byte)'0' and <= (byte)'9' => ParseNumber(context, jsonText, newIndex),
            (byte)'"' => ParseString(context, jsonText, newIndex),
            (byte)'[' => ParseArray(context, jsonText, newIndex),
            (byte)'{' => ParseObject(context, jsonText, newIndex),
            _ => JsonResult.Err(JsonError.InvalidCharacter, JsonType.Null, newIndex)
        };
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