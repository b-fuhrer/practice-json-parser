using System.Text;

namespace JsonParserLogic;

public static partial class JsonParser
{
    private const int InvalidHex = -1;

    internal static JsonResult ParseString(JsonContext context, ReadOnlySpan<byte> jsonText, int currentIndex)
    {
        if (currentIndex + 1 > jsonText.Length)
        {
            return JsonResult.Err(JsonError.EndOfFile, JsonType.String, currentIndex);
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
                int stringIndex = context.AddString(parsedString);
                return JsonResult.Ok(JsonType.String, stringIndex, newIndex + 1);
            }

            if (currentCharacter == (byte)'\\')
            {
                return OnEscapedCharacter(context, jsonText, initialIndex, newIndex);
            }

            newIndex++;
        }

        return JsonResult.Err(JsonError.EndOfFile, JsonType.String, newIndex);
    }

    private static JsonResult OnEscapedCharacter(JsonContext context, ReadOnlySpan<byte> jsonText, int initialIndex, int currentIndex)
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

                int stringIndex = context.AddString(stringBuilder.ToString());
                return JsonResult.Ok(JsonType.String, stringIndex, newIndex + 1);
            }

            if (currentCharacter == (byte)'\\')
            {
                if (startOfSegment != newIndex)
                {
                    StringBuilderAppendUtf8(stringBuilder, jsonText.Slice(startOfSegment, newIndex - startOfSegment));
                }

                if (newIndex + 1 >= jsonText.Length)
                {
                    return JsonResult.Err(JsonError.EndOfFile, JsonType.String, newIndex);
                }

                (char? escapedCharacter, int nextIndex, JsonResult? escapedError) =
                    DecodeEscapedCharacter(jsonText, newIndex + 1);

                if (escapedError is { } escapedCharacterError)
                {
                    return escapedCharacterError;
                }

                stringBuilder.Append(escapedCharacter);
                startOfSegment = nextIndex;
                newIndex = nextIndex;
                continue;
            }

            newIndex++;
        }

        return JsonResult.Err(JsonError.EndOfFile, JsonType.String, newIndex);
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

    private static (char? value, int newIndex, JsonResult? error) DecodeEscapedCharacter(ReadOnlySpan<byte> jsonText,
        int escapedIndex)
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
            _ => (null, escapedIndex, JsonResult.Err(
                    JsonError.InvalidCharacter,
                    JsonType.String,
                    escapedIndex
                )
            )
        };
    }

    private static (char? value, int newIndex, JsonResult? error) DecodeUnicodeSequence(ReadOnlySpan<byte> jsonText,
        int escapedIndex)
    {
        // escapedIndex = index of the 'u'
        if (escapedIndex + 4 >= jsonText.Length)
        {
            return (null, escapedIndex, JsonResult.Err(JsonError.EndOfFile, JsonType.String, escapedIndex));
        }

        int leftByte = ParseHexByteIntoInt(jsonText[escapedIndex + 1]);
        int middleLeftByte = ParseHexByteIntoInt(jsonText[escapedIndex + 2]);
        int middleRightByte = ParseHexByteIntoInt(jsonText[escapedIndex + 3]);
        int rightByte = ParseHexByteIntoInt(jsonText[escapedIndex + 4]);

        // if any of the "bytes" is negative, their bit-wise OR is also negative
        if ((leftByte | middleLeftByte | middleRightByte | rightByte) < 0)
        {
            return (null, escapedIndex, JsonResult.Err(
                    JsonError.InvalidCharacter,
                    JsonType.String,
                    escapedIndex
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
            >= (byte)'A' and <= (byte)'F' => hexByte - (byte)'A' + 10,
            >= (byte)'a' and <= (byte)'f' => hexByte - (byte)'a' + 10,
            _ => InvalidHex
        };
    }
}