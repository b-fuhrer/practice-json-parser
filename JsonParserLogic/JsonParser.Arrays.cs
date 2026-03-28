namespace JsonParserLogic;

public static partial class JsonParser
{
    internal static JsonResult ParseArray(JsonContext context, ReadOnlySpan<byte> jsonText, int currentIndex)
    {
        int headerIndex = context.AddArrayElement(JsonType.Array, 0);

        // currentIndex is index of opening bracket '['
        int newIndex = SkipWhitespace(jsonText, currentIndex + 1);
        if (newIndex == jsonText.Length)
        {
            return JsonResult.Err(JsonError.EndOfFile, JsonType.Array, newIndex);
        }

        if (jsonText[newIndex] == (byte)']')
        {
            context.ArrayElements[headerIndex].SetNextSiblingOffset(1);
            return JsonResult.Ok(JsonType.Array, headerIndex, newIndex + 1);
        }

        while (newIndex < jsonText.Length)
        {
            JsonResult parsedValue = ParseIntoValue(context, jsonText, newIndex);
            if (parsedValue.IsError)
            {
                return parsedValue;
            }

            if (parsedValue.Type < JsonType.Array)
            {
                if (parsedValue.Type == JsonType.Bool)
                {
                    context.AddArrayElement(parsedValue.ElementIndex != 0);
                }
                else
                {
                    context.AddArrayElement(parsedValue.Type, parsedValue.ElementIndex);
                }
            }

            int skipIndex = SkipWhitespace(jsonText, parsedValue.JsonIndex);
            if (skipIndex == jsonText.Length)
            {
                return JsonResult.Err(JsonError.EndOfFile, JsonType.Array, skipIndex);
            }

            byte characterAfterWhitespace = jsonText[skipIndex];

            if (characterAfterWhitespace == (byte)']')
            {
                uint arrayOffset = (uint)(context.ArrayElementCount - headerIndex);
                context.ArrayElements[headerIndex].SetNextSiblingOffset(arrayOffset);
                return JsonResult.Ok(JsonType.Array, headerIndex, skipIndex + 1);
            }

            if (characterAfterWhitespace != (byte)',')
            {
                return JsonResult.Err(JsonError.InvalidSyntax, JsonType.Array, skipIndex);
            }

            newIndex = skipIndex + 1;
        }

        return JsonResult.Err(JsonError.EndOfFile, JsonType.Array, newIndex);
    }
}