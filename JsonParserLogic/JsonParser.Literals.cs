using System.Text;
using PracticeJsonParser.Types;
namespace PracticeJsonParser;

public static partial class JsonParser
{
    internal static JsonResult<JsonNull> ParseNull(ReadOnlySpan<byte> jsonText, int currentIndex)
    {
        const int nullLength = 4;

        if (currentIndex + nullLength > jsonText.Length)
        {
            return JsonResult<JsonNull>.Err(JsonErrorType.EndOfFile, currentIndex);
        }

        var slice = jsonText.Slice(currentIndex, nullLength);

        if (!slice.SequenceEqual("null"u8))
        {
            return JsonResult<JsonNull>.Err(
                JsonErrorType.InvalidSyntax,
                $"Expected 'null', received '{Encoding.UTF8.GetString(slice)}'",
                currentIndex
            );
        }

        int afterNullIndex = currentIndex + nullLength;

        if (afterNullIndex == jsonText.Length)
        {
            return JsonResult<JsonNull>.Ok(JsonNull.Instance, afterNullIndex);
        }

        return jsonText[afterNullIndex] switch
        {
            (byte)',' or (byte)']' or (byte)'}' or (byte)' ' or (byte)'\t' or (byte)'\n' or (byte)'\r' =>
                JsonResult<JsonNull>.Ok(JsonNull.Instance, afterNullIndex),
            _ => JsonResult<JsonNull>.Err(
                JsonErrorType.InvalidSyntax,
                "Bools are not allowed to be followed by trailing garbage.",
                afterNullIndex
            )
        };
    }

    internal static JsonResult<JsonBool> ParseBool(ReadOnlySpan<byte> jsonText, int currentIndex)
    {
        byte firstCharacter = jsonText[currentIndex];

        return firstCharacter switch
        {
            (byte)'t' => ParseTrue(jsonText, currentIndex),
            (byte)'f' => ParseFalse(jsonText, currentIndex),
            _ => JsonResult<JsonBool>.Err(
                JsonErrorType.InvalidSyntax,
                $"Expected 't' (true) or 'f' (false), received '{firstCharacter}'",
                currentIndex
            )
        };
    }

    private static JsonResult<JsonBool> ParseTrue(ReadOnlySpan<byte> jsonText, int currentIndex)
    {
        const int trueLength = 4;

        if (currentIndex + trueLength > jsonText.Length)
        {
            return JsonResult<JsonBool>.Err(JsonErrorType.EndOfFile, currentIndex);
        }

        var slice = jsonText.Slice(currentIndex, trueLength);

        if (!slice.SequenceEqual("true"u8))
        {
            return JsonResult<JsonBool>.Err(
                JsonErrorType.InvalidSyntax,
                $"Expected 'true', received '{Encoding.UTF8.GetString(slice)}'",
                currentIndex
            );
        }

        int afterTrueIndex = currentIndex + trueLength;

        if (afterTrueIndex == jsonText.Length)
        {
            return JsonResult<JsonBool>.Ok(JsonBool.True, afterTrueIndex);
        }

        return jsonText[afterTrueIndex] switch
        {
            (byte)',' or (byte)']' or (byte)'}' or (byte)' ' or (byte)'\t' or (byte)'\n' or (byte)'\r' =>
                JsonResult<JsonBool>.Ok(JsonBool.True, afterTrueIndex),
            _ => JsonResult<JsonBool>.Err(
                JsonErrorType.InvalidSyntax,
                "Bools are not allowed to be followed by trailing garbage.",
                afterTrueIndex
            )
        };
    }

    private static JsonResult<JsonBool> ParseFalse(ReadOnlySpan<byte> jsonText, int currentIndex)
    {
        const int falseLength = 5;

        if (currentIndex + falseLength > jsonText.Length)
        {
            return JsonResult<JsonBool>.Err(JsonErrorType.EndOfFile, currentIndex);
        }

        var slice = jsonText.Slice(currentIndex, falseLength);

        if (!slice.SequenceEqual("false"u8))
        {
            return JsonResult<JsonBool>.Err(
                JsonErrorType.InvalidSyntax,
                $"Expected 'false', received '{Encoding.UTF8.GetString(slice)}'",
                currentIndex
            );
        }

        int afterFalseIndex = currentIndex + falseLength;

        if (afterFalseIndex == jsonText.Length)
        {
            return JsonResult<JsonBool>.Ok(JsonBool.False, afterFalseIndex);
        }

        return jsonText[afterFalseIndex] switch
        {
            (byte)',' or (byte)']' or (byte)'}' or (byte)' ' or (byte)'\t' or (byte)'\n' or (byte)'\r' =>
                JsonResult<JsonBool>.Ok(JsonBool.False, afterFalseIndex),
            _ => JsonResult<JsonBool>.Err(
                JsonErrorType.InvalidSyntax,
                "Bools are not allowed to be followed by trailing garbage.",
                afterFalseIndex
            )
        };
    }
}