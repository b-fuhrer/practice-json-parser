using System.Text;
using PracticeJsonParser.Types;
namespace PracticeJsonParser;

public static partial class JsonParser
{
    private const int InvalidHex = -1;

    internal static JsonResult<JsonString> ParseString(ReadOnlySpan<byte> jsonText, int currentIndex)
    {
        if (currentIndex + 1 > jsonText.Length)
        {
            return JsonResult<JsonString>.Err(JsonErrorType.EndOfFile, currentIndex);
        }

        // initialIndex = first character after the opening "
        int initialIndex = currentIndex + 1;
        int newIndex = initialIndex;

        while (newIndex < jsonText.Length)
        {
            byte currentCharacter = jsonText[newIndex];

            if (currentCharacter == (byte)'"')
            {
                string parsedString = Encoding.UTF8.GetString(jsonText.Slice(initialIndex, newIndex - initialIndex));
                return JsonResult<JsonString>.Ok(new JsonString(parsedString), newIndex + 1);
            }

            if (currentCharacter == (byte)'\\')
            {
                return OnEscapedCharacter(jsonText, initialIndex, newIndex);
            }

            newIndex++;
        }

        return JsonResult<JsonString>.Err(JsonErrorType.EndOfFile, newIndex);
    }

    private static JsonResult<JsonString> OnEscapedCharacter(ReadOnlySpan<byte> jsonText, int initialIndex, int currentIndex)
    {
        int newIndex = currentIndex;

        const int additionalStartCapacity = 32;
        int scannedLength = currentIndex - initialIndex;
        var scannedChunk = jsonText.Slice(initialIndex, scannedLength);
        var stringBuilder = new StringBuilder(capacity: scannedLength + additionalStartCapacity);
        StringBuilderAppendUtf8(stringBuilder, scannedChunk);

        int startOfSegment = currentIndex;

        while (newIndex < jsonText.Length)
        {
            byte currentCharacter = jsonText[newIndex];

            if (currentCharacter == (byte)'"')
            {
                if (startOfSegment != newIndex)
                {
                    StringBuilderAppendUtf8(stringBuilder, jsonText.Slice(startOfSegment, newIndex - startOfSegment));
                }

                return JsonResult<JsonString>.Ok(new JsonString(stringBuilder.ToString()), newIndex + 1);
            }

            if (currentCharacter == (byte)'\\')
            {
                if (startOfSegment != newIndex)
                {
                    StringBuilderAppendUtf8(stringBuilder, jsonText.Slice(startOfSegment, newIndex - startOfSegment));
                }

                if (newIndex + 1 >= jsonText.Length)
                {
                    return JsonResult<JsonString>.Err(JsonErrorType.EndOfFile, newIndex);
                }

                (char? escapedCharacter, int nextIndex, var escapedError) = DecodeEscapedCharacter(jsonText, newIndex + 1);

                if (escapedError is not null)
                {
                    return JsonResult<JsonString>.Err(escapedError.Value, nextIndex);
                }

                stringBuilder.Append(escapedCharacter);
                startOfSegment = nextIndex;
                newIndex = nextIndex;
                continue;
            }

            newIndex++;
        }

        return JsonResult<JsonString>.Err(JsonErrorType.EndOfFile, newIndex);
    }

    private static void StringBuilderAppendUtf8(StringBuilder stringBuilder, ReadOnlySpan<byte> utf8Slice)
    {
        // only allocate maximum of 1KB on the stack here
        if (utf8Slice.Length > 512)
        {
            stringBuilder.Append(Encoding.UTF8.GetString(utf8Slice));
            return;
        }

        Span<char> stackBuffer = stackalloc char[utf8Slice.Length];
        int amountOfCharsWritten = Encoding.UTF8.GetChars(utf8Slice, stackBuffer);
        stringBuilder.Append(stackBuffer[..amountOfCharsWritten]);
    }

    private static (char? value, int newIndex, JsonError? error) DecodeEscapedCharacter(ReadOnlySpan<byte> jsonText, int escapedIndex)
    {
        byte escapedCharacter = jsonText[escapedIndex];

        return escapedCharacter switch
        {
            (byte)'b' => ('\b', escapedIndex + 1, null),
            (byte)'f' => ('\f', escapedIndex + 1, null),
            (byte)'n' => ('\n', escapedIndex + 1, null),
            (byte)'r' => ('\r', escapedIndex + 1, null),
            (byte)'t' => ('\t', escapedIndex + 1, null),
            (byte)'\\' => ('\\', escapedIndex + 1, null),
            (byte)'/' => ('/', escapedIndex + 1, null),
            (byte)'"' => ('"', escapedIndex + 1, null),
            (byte)'u' => DecodeUnicodeSequence(jsonText, escapedIndex),
            _ => (null, escapedIndex, new JsonError(
                        JsonErrorType.InvalidCharacter,
                        $"Failed to decode escaped character '\\{escapedCharacter}'"
                    )
                )
        };
    }

    private static (char? value, int newIndex, JsonError? error) DecodeUnicodeSequence(ReadOnlySpan<byte> jsonText, int escapedIndex)
    {
        // escapedIndex = index of the 'u'
        if (escapedIndex + 4 >= jsonText.Length)
        {
            return (null, escapedIndex, new JsonError(JsonErrorType.EndOfFile, null));
        }

        int leftByte = ParseHexByteIntoInt(jsonText[escapedIndex + 1]);
        int middleLeftByte = ParseHexByteIntoInt(jsonText[escapedIndex + 2]);
        int middleRightByte = ParseHexByteIntoInt(jsonText[escapedIndex + 3]);
        int rightByte = ParseHexByteIntoInt(jsonText[escapedIndex + 4]);

        // if any of the "bytes" is negative, their bit-wise OR is also negative
        if ((leftByte | middleLeftByte | middleRightByte | rightByte) < 0)
        {
            return (null, escapedIndex, new JsonError(
                        JsonErrorType.InvalidCharacter,
                        $"Failed to decode invalid hexadecimal in unicode sequence '\\{Encoding.UTF8.GetString(jsonText.Slice(escapedIndex, 5))}'"
                    )
                );
        }

        char parsedSequence = (char)(leftByte << 12 | middleLeftByte << 8 | middleRightByte << 4 | rightByte);

        return (parsedSequence, escapedIndex + 5, null);
    }

    private static int ParseHexByteIntoInt(byte hexByte)
    {
        return hexByte switch
        {
            >= (byte)'0' and <= (byte)'9' => hexByte - (byte)'0',
            >= (byte)'A' and <= (byte)'F' => hexByte - (byte)'A' + 10, // A = 10 in hex
            >= (byte)'a' and <= (byte)'f' => hexByte - (byte)'a' + 10, // a = 10 in hex
            _ => InvalidHex
        };
    }
}