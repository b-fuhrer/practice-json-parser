using System.Collections.Immutable;

namespace JsonParserLogic;

public static partial class JsonParser
{
    internal static JsonNode ParseObject(ReadOnlySpan<byte> jsonText, int currentIndex)
    {
        ImmutableDictionary<string, JsonNode>.Builder dictionaryBuilder =
            ImmutableDictionary.CreateBuilder<string, JsonNode>();

        // current index is the index of the opening curly brace '{'
        int newIndex = SkipWhitespace(jsonText, currentIndex + 1);
        if (newIndex == jsonText.Length)
        {
            return JsonNode.Err(JsonError.EndOfFile, newIndex);
        }

        if (jsonText[newIndex] == (byte)'}')
        {
            return JsonNode.OkObject(ImmutableDictionary<string, JsonNode>.Empty, newIndex + 1);
        }

        while (newIndex < jsonText.Length)
        {
            newIndex = SkipWhitespace(jsonText, newIndex);
            if (newIndex == jsonText.Length)
            {
                return JsonNode.Err(JsonError.EndOfFile, newIndex);
            }

            if (jsonText[newIndex] != (byte)'"')
            {
                return JsonNode.Err(
                    JsonError.InvalidSyntax,
                    $"Object keys must be strings and therefore begin with '\"', found '{(char)jsonText[newIndex]}'.",
                    newIndex
                );
            }

            var parsedKey = ParseString(jsonText, newIndex);
            if (parsedKey.IsError)
            {
                return JsonNode.Err(
                    parsedKey.JsonError,
                    parsedKey.ErrorMessage,
                    parsedKey.Index
                );
            }

            int skipAfterKeyIndex = SkipWhitespace(jsonText, parsedKey.Index);
            if (skipAfterKeyIndex == jsonText.Length)
            {
                return JsonNode.Err(JsonError.EndOfFile, skipAfterKeyIndex);
            }

            if (jsonText[skipAfterKeyIndex] != (byte)':')
            {
                return JsonNode.Err(
                    JsonError.InvalidSyntax,
                    $"Object keys and values must be separated by ':', found '{(char)jsonText[skipAfterKeyIndex]}'.",
                    skipAfterKeyIndex
                );
            }

            var parsedValue = ParseIntoValue(jsonText, skipAfterKeyIndex + 1);
            if (parsedValue.IsError)
            {
                return JsonNode.Err(
                    parsedValue.JsonError,
                    parsedValue.ErrorMessage,
                    parsedValue.Index
                );
            }

            if (dictionaryBuilder.ContainsKey(parsedKey.String))
            {
                return JsonNode.Err(
                    JsonError.InvalidSyntax,
                    $"Duplicate key '{parsedKey.String}' found in object.",
                    parsedKey.Index
                );
            }
            dictionaryBuilder.Add(parsedKey.String, parsedValue);

            int skipAfterValueIndex = SkipWhitespace(jsonText, parsedValue.Index);
            if (skipAfterValueIndex == jsonText.Length)
            {
                return JsonNode.Err(JsonError.EndOfFile, skipAfterValueIndex);
            }

            byte characterAfterWhitespace = jsonText[skipAfterValueIndex];

            if (characterAfterWhitespace == (byte)'}')
            {
                return JsonNode.OkObject(
                    dictionaryBuilder.ToImmutableDictionary(),
                    skipAfterValueIndex + 1
                );
            }

            if (characterAfterWhitespace != (byte)',')
            {
                return JsonNode.Err(
                    JsonError.InvalidSyntax,
                    $"Object key-value pairds must be separated by ',' character, found '{(char)characterAfterWhitespace}'.",
                    skipAfterValueIndex
                );
            }

            newIndex = skipAfterValueIndex + 1;
        }

        return JsonNode.Err(JsonError.EndOfFile, newIndex);
    }
}