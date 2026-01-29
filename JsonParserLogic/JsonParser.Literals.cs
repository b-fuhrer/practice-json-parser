using System.Text;
namespace JsonParserLogic;

public static partial class JsonParser
{
    internal static JsonNode ParseNull(ReadOnlySpan<byte> jsonText, int currentIndex)
    {
        const int nullLength = 4;
        var nullLiteral = "null"u8;

        if (currentIndex + nullLength > jsonText.Length)
        {
            return JsonNode.Err(ErrorType.EndOfFile, currentIndex);
        }

        var slice = jsonText.Slice(currentIndex, nullLength);
        if (!slice.SequenceEqual(nullLiteral))
        {
            return JsonNode.Err(
                ErrorType.InvalidSyntax,
                $"Expected '{Encoding.UTF8.GetString(nullLiteral)}', received '{Encoding.UTF8.GetString(slice)}'.",
                currentIndex
            );
        }

        int afterLiteralIndex = currentIndex + nullLength;
        if (afterLiteralIndex == jsonText.Length)
        {
            return JsonNode.OkNull(afterLiteralIndex);
        }

        byte afterLiteralCharacter = jsonText[afterLiteralIndex];
        bool isSeparator = IsSeparator(afterLiteralCharacter);

        return isSeparator
            ? JsonNode.OkNull(afterLiteralIndex)
            : JsonNode.Err(
                ErrorType.InvalidSyntax,
                "Null is not allowed to be followed by trailing garbage.",
                afterLiteralIndex
            );
    }

    internal static JsonNode ParseBool(ReadOnlySpan<byte> jsonText, int currentIndex)
    {
        byte firstCharacter = jsonText[currentIndex];

        return firstCharacter switch
        {
            (byte)'t' => ParseBoolValue(jsonText, currentIndex, true, "true"u8),
            (byte)'f' => ParseBoolValue(jsonText, currentIndex, false, "false"u8),
            _ => JsonNode.Err(
                ErrorType.InvalidSyntax,
                $"Expected 't' (true) or 'f' (false), received '{firstCharacter}'.",
                currentIndex
            )
        };
    }

    private static JsonNode ParseBoolValue(
        ReadOnlySpan<byte> jsonText,
        int currentIndex,
        bool successReturnValue,
        ReadOnlySpan<byte> boolLiteral
    )
    {
        int literalLength = boolLiteral.Length;

        if (currentIndex + literalLength > jsonText.Length)
        {
            return JsonNode.Err(ErrorType.EndOfFile, currentIndex);
        }

        var slice = jsonText.Slice(currentIndex, literalLength);
        if (!slice.SequenceEqual(boolLiteral))
        {
            return JsonNode.Err(
                ErrorType.InvalidSyntax,
                $"Expected '{Encoding.UTF8.GetString(boolLiteral)}', received '{Encoding.UTF8.GetString(slice)}'.",
                currentIndex
            );
        }

        int afterLiteralIndex = currentIndex + literalLength;
        if (afterLiteralIndex == jsonText.Length)
        {
            return JsonNode.OkBool(successReturnValue, afterLiteralIndex);
        }

        byte afterLiteralCharacter = jsonText[afterLiteralIndex];
        bool isSeparator = IsSeparator(afterLiteralCharacter);

        return isSeparator
            ? JsonNode.OkBool(successReturnValue, afterLiteralIndex)
            : JsonNode.Err(
                ErrorType.InvalidSyntax,
                "Bools are not allowed to be followed by trailing garbage.",
                afterLiteralIndex
            );
    }
}