using System.Collections.Immutable;
using JsonParserLegacy.Types;
namespace JsonParserLegacy;

public static partial class JsonParser
{
    internal static JsonResult<JsonArray> ParseArray(ReadOnlySpan<byte> jsonText, int currentIndex)
    {
        ImmutableArray<JsonValue>.Builder arrayBuilder = ImmutableArray.CreateBuilder<JsonValue>();

        // currentIndex is index of opening bracket '['
        int newIndex = SkipWhitespace(jsonText, currentIndex + 1);
        if (newIndex == jsonText.Length)
        {
            return JsonResult<JsonArray>.Err(JsonErrorType.EndOfFile, newIndex);
        }

        if (jsonText[newIndex] == (byte)']')
        {
            return JsonResult<JsonArray>.Ok(new JsonArray([]), newIndex + 1);
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
}