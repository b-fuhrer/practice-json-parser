using System.Collections.Immutable;
using JsonParserLegacy.Types;
namespace JsonParserLegacy;

public static partial class JsonParser
{
    internal static JsonResult<JsonObject> ParseObject(ReadOnlySpan<byte> jsonText, int currentIndex)
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
            return JsonResult<JsonObject>.Ok(new JsonObject([]), newIndex + 1);
        }

        while (newIndex < jsonText.Length)
        {
            newIndex = SkipWhitespace(jsonText, newIndex);
            if (newIndex == jsonText.Length)
            {
                return JsonResult<JsonObject>.Err(JsonErrorType.EndOfFile, newIndex);
            }

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