using System.Text;

namespace JsonParserLogic;

public static partial class JsonParser
{
    internal static JsonResult ParseNull(ReadOnlySpan<byte> jsonText, int currentIndex)
    {
        const int nullLength = 4;
        var nullLiteral = "null"u8;

        if (currentIndex + nullLength > jsonText.Length)
        {
            return JsonResult.Err(JsonError.EndOfFile, JsonType.Null, currentIndex);
        }

        var slice = jsonText.Slice(currentIndex, nullLength);
        if (!slice.SequenceEqual(nullLiteral))
        {
            return JsonResult.Err(JsonError.InvalidSyntax, JsonType.Null, currentIndex);
        }

        int afterLiteralIndex = currentIndex + nullLength;
        if (afterLiteralIndex == jsonText.Length)
        {
            return JsonResult.Ok(JsonType.Null, 0, afterLiteralIndex);
        }

        byte afterLiteralCharacter = jsonText[afterLiteralIndex];
        bool isSeparator = IsSeparator(afterLiteralCharacter);

        return isSeparator
            ? JsonResult.Ok(JsonType.Null, 0, afterLiteralIndex)
            : JsonResult.Err(JsonError.InvalidSyntax, JsonType.Null, afterLiteralIndex);
    }

    internal static JsonResult ParseBool(ReadOnlySpan<byte> jsonText, int currentIndex)
    {
        byte firstCharacter = jsonText[currentIndex];

        return firstCharacter switch
        {
            (byte)'t' => ParseBoolValue(jsonText, currentIndex, true, "true"u8),
            (byte)'f' => ParseBoolValue(jsonText, currentIndex, false, "false"u8),
            _ => JsonResult.Err(JsonError.InvalidSyntax, JsonType.Bool, currentIndex)
        };
    }

    private static JsonResult ParseBoolValue(
        ReadOnlySpan<byte> jsonText,
        int currentIndex,
        bool successReturnValue,
        ReadOnlySpan<byte> boolLiteral
    )
    {
        int literalLength = boolLiteral.Length;

        if (currentIndex + literalLength > jsonText.Length)
        {
            return JsonResult.Err(JsonError.EndOfFile, JsonType.Bool, currentIndex);
        }

        var slice = jsonText.Slice(currentIndex, literalLength);
        if (!slice.SequenceEqual(boolLiteral))
        {
            return JsonResult.Err(JsonError.InvalidSyntax, JsonType.Bool, currentIndex);
        }

        int afterLiteralIndex = currentIndex + literalLength;
        int encodedBool = successReturnValue ? 1 : 0;

        if (afterLiteralIndex == jsonText.Length)
        {
            return JsonResult.Ok(JsonType.Bool, encodedBool, afterLiteralIndex);
        }

        byte afterLiteralCharacter = jsonText[afterLiteralIndex];
        bool isSeparator = IsSeparator(afterLiteralCharacter);

        return isSeparator
            ? JsonResult.Ok(JsonType.Bool, encodedBool, afterLiteralIndex)
            : JsonResult.Err(JsonError.InvalidSyntax, JsonType.Bool, afterLiteralIndex);
    }
}