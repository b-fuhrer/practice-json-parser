namespace JsonParserLogic;

public static partial class JsonParser
{
    internal static JsonNode ParseArray(ReadOnlySpan<byte> jsonText, int currentIndex)
    {
        var arrayBuilder = new List<JsonNode>();

        // currentIndex is index of opening bracket '['
        int newIndex = SkipWhitespace(jsonText, currentIndex + 1);
        if (newIndex == jsonText.Length)
        {
            return JsonNode.Err(ErrorType.EndOfFile, newIndex);
        }

        if (jsonText[newIndex] == (byte)']')
        {
            return JsonNode.OkArray([], newIndex + 1);
        }

        while (newIndex < jsonText.Length)
        {
            JsonNode parsedValue = ParseIntoValue(jsonText, newIndex);
            if (parsedValue.IsError)
            {
                return JsonNode.Err(
                    parsedValue.ErrorType,
                    parsedValue.ErrorMessage,
                    parsedValue.Index
                );
            }

            arrayBuilder.Add(parsedValue);

            int skipIndex = SkipWhitespace(jsonText, parsedValue.Index);
            if (skipIndex == jsonText.Length)
            {
                return JsonNode.Err(ErrorType.EndOfFile, skipIndex);
            }

            byte characterAfterWhitespace = jsonText[skipIndex];

            if (characterAfterWhitespace == (byte)']')
            {
                return JsonNode.OkArray(arrayBuilder.ToArray(), skipIndex + 1);
            }

            if (characterAfterWhitespace != (byte)',')
            {
                return JsonNode.Err(
                    ErrorType.InvalidSyntax,
                    $"Array elements must be separated by ',' character, found '{(char)characterAfterWhitespace}'.",
                    skipIndex
                );
            }

            newIndex = skipIndex + 1;
        }

        return JsonNode.Err(ErrorType.EndOfFile, newIndex);
    }
}