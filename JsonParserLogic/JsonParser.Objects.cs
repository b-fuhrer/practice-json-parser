namespace JsonParserLogic;

public static partial class JsonParser
{
    internal static JsonResult ParseObject(JsonContext context, ReadOnlySpan<byte> jsonText, int currentIndex)
    {
        int headerIndex = context.AddArrayElement(JsonType.Object, 0);

        // current index is the index of the opening curly brace '{'
        int newIndex = SkipWhitespace(jsonText, currentIndex + 1);
        if (newIndex == jsonText.Length)
        {
            return JsonResult.Err(JsonError.EndOfFile, JsonType.Object, newIndex);
        }

        if (jsonText[newIndex] == (byte)'}')
        {
            context.ArrayElements[headerIndex].SetNextSiblingOffset(1);
            return JsonResult.Ok(JsonType.Object, headerIndex, newIndex + 1);
        }

        int firstPropertyIndex = -1;
        int lastPropertyIndex = -1;

        while (newIndex < jsonText.Length)
        {
            newIndex = SkipWhitespace(jsonText, newIndex);
            if (newIndex == jsonText.Length)
            {
                return JsonResult.Err(JsonError.EndOfFile, JsonType.Object, newIndex);
            }

            if (jsonText[newIndex] != (byte)'"')
            {
                return JsonResult.Err(JsonError.InvalidSyntax, JsonType.Object, newIndex);
            }

            JsonResult parsedKey = ParseString(context, jsonText, newIndex);
            if (parsedKey.IsError)
            {
                return parsedKey;
            }

            if (ContainsObjectKey(context, firstPropertyIndex, parsedKey.ElementIndex))
            {
                return JsonResult.Err(JsonError.InvalidSyntax, JsonType.Object, parsedKey.JsonIndex);
            }

            int skipAfterKeyIndex = SkipWhitespace(jsonText, parsedKey.JsonIndex);
            if (skipAfterKeyIndex == jsonText.Length)
            {
                return JsonResult.Err(JsonError.EndOfFile, JsonType.Object, skipAfterKeyIndex);
            }

            if (jsonText[skipAfterKeyIndex] != (byte)':')
            {
                return JsonResult.Err(JsonError.InvalidSyntax, JsonType.Object, skipAfterKeyIndex);
            }

            JsonResult parsedValue = ParseIntoValue(context, jsonText, skipAfterKeyIndex + 1);
            if (parsedValue.IsError)
            {
                return parsedValue;
            }

            int propertyIndex = parsedValue.Type == JsonType.Bool
                ? context.AddObjectElement(parsedKey.ElementIndex, parsedValue.ElementIndex != 0)
                : context.AddObjectElement(parsedValue.Type, parsedKey.ElementIndex, parsedValue.ElementIndex);

            context.ObjectElements[propertyIndex].SetNextSiblingOffset(0);

            if (firstPropertyIndex == -1)
            {
                firstPropertyIndex = propertyIndex;
                context.ArrayElements[headerIndex].Index = firstPropertyIndex;
            }

            if (lastPropertyIndex != -1)
            {
                uint propertyOffset = (uint)(propertyIndex - lastPropertyIndex);
                context.ObjectElements[lastPropertyIndex].SetNextSiblingOffset(propertyOffset);
            }

            lastPropertyIndex = propertyIndex;

            if (parsedValue.Type == JsonType.Bool)
            {
                context.AddArrayElement(parsedValue.ElementIndex != 0);
            }
            else
            {
                context.AddArrayElement(parsedValue.Type, parsedValue.ElementIndex);
            }

            int skipAfterValueIndex = SkipWhitespace(jsonText, parsedValue.JsonIndex);
            if (skipAfterValueIndex == jsonText.Length)
            {
                return JsonResult.Err(JsonError.EndOfFile, JsonType.Object, skipAfterValueIndex);
            }

            byte characterAfterWhitespace = jsonText[skipAfterValueIndex];

            if (characterAfterWhitespace == (byte)'}')
            {
                uint objectOffset = (uint)(context.ArrayElementCount - headerIndex);
                context.ArrayElements[headerIndex].SetNextSiblingOffset(objectOffset);
                return JsonResult.Ok(JsonType.Object, headerIndex, skipAfterValueIndex + 1);
            }

            if (characterAfterWhitespace != (byte)',')
            {
                return JsonResult.Err(JsonError.InvalidSyntax, JsonType.Object, skipAfterValueIndex);
            }

            newIndex = skipAfterValueIndex + 1;
        }

        return JsonResult.Err(JsonError.EndOfFile, JsonType.Object, newIndex);
    }

    private static bool ContainsObjectKey(JsonContext context, int firstPropertyIndex, int keyIndex)
    {
        if (firstPropertyIndex == -1)
        {
            return false;
        }

        string key = context.Strings[keyIndex];
        int current = firstPropertyIndex;

        while (current != -1)
        {
            ref readonly ObjectElement element = ref context.ObjectElements[current];
            if (context.Strings[element.KeyIndex] == key)
            {
                return true;
            }

            uint offset = element.NextSiblingOffset;
            current = offset == 0 ? -1 : current + (int)offset;
        }

        return false;
    }
}