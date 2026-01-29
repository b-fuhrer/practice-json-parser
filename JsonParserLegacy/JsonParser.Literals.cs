using System.Text;
using JsonParserLegacy.Types;

namespace JsonParserLegacy;

public static partial class JsonParser
{
    internal static JsonResult<JsonNull> ParseNull(ReadOnlySpan<byte> jsonText, int currentIndex)
    {
        return ParseLiteral(jsonText, currentIndex, "null"u8, JsonNull.Instance);
    }

    internal static JsonResult<JsonBool> ParseBool(ReadOnlySpan<byte> jsonText, int currentIndex)
    {
        byte firstCharacter = jsonText[currentIndex];

        return firstCharacter switch
        {
            (byte)'t' => ParseLiteral(jsonText, currentIndex,"true"u8, JsonBool.True),
            (byte)'f' => ParseLiteral(jsonText, currentIndex, "false"u8, JsonBool.False),
            _ => JsonResult<JsonBool>.Err(
                JsonErrorType.InvalidSyntax,
                $"Expected 't' (true) or 'f' (false), received '{firstCharacter}'",
                currentIndex
            )
        };
    }

    private static JsonResult<TLiteral> ParseLiteral<TLiteral>(
        ReadOnlySpan<byte> jsonText,
        int currentIndex,
        ReadOnlySpan<byte> expectedLiteral,
        TLiteral successReturnValue
    )
        where TLiteral : JsonValue
    {
        if (currentIndex + expectedLiteral.Length > jsonText.Length)
        {
            return JsonResult<TLiteral>.Err(JsonErrorType.EndOfFile, currentIndex);
        }

        var slice = jsonText.Slice(currentIndex, expectedLiteral.Length);
        if (!slice.SequenceEqual(expectedLiteral))
        {
            return JsonResult<TLiteral>.Err(
                JsonErrorType.InvalidSyntax,
                $"Expected '{Encoding.UTF8.GetString(expectedLiteral)}', received '{Encoding.UTF8.GetString(slice)}'",
                currentIndex
            );
        }

        int afterLiteralIndex = currentIndex + expectedLiteral.Length;
        if (afterLiteralIndex == jsonText.Length)
        {
            return JsonResult<TLiteral>.Ok(successReturnValue, afterLiteralIndex);
        }

        byte afterLiteralCharacter = jsonText[afterLiteralIndex];
        bool isSeparator = afterLiteralCharacter is (byte)',' or (byte)']' or (byte)'}' or (byte)' ' or (byte)'\t'
            or (byte)'\n' or (byte)'\r';

        return isSeparator
            ? JsonResult<TLiteral>.Ok(successReturnValue, afterLiteralIndex)
            : JsonResult<TLiteral>.Err(
                JsonErrorType.InvalidSyntax,
                $"{(successReturnValue == JsonNull.Instance ? "Null is" : "Bools are")} not allowed to be followed by trailing garbage",
                afterLiteralIndex
            );
    }
}