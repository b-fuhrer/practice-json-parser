using PracticeJsonParser.Types;
namespace PracticeJsonParser;

public static partial class JsonParser
{
    internal static JsonResult<JsonNumber> ParseNumber(ReadOnlySpan<byte> jsonText, int currentIndex)
    {
        double accumulator = 0.0;
        (double numberSign, int newIndex) = jsonText[currentIndex] == (byte)'-'
            ? (-1.0, currentIndex + 1)
            : (1.0, currentIndex);
        int exponentAccumulator = 0;
        int exponentSign = 1;

        if (newIndex < jsonText.Length && jsonText[newIndex] == (byte)'0')
        {
            if (newIndex + 1 < jsonText.Length && IsByteDigit(jsonText[newIndex + 1]))
            {
                return JsonResult<JsonNumber>.Err(
                    JsonErrorType.InvalidSyntax,
                    "Numbers are not allowed to have a leading zero.",
                    newIndex
                );
            }

            newIndex++;
        }
        else // if the integer part starts with zero, no further digits are allowed in the integer part
        {
            int integerLoopStartIndex = newIndex;
            while (newIndex < jsonText.Length && IsByteDigit(jsonText[newIndex]))
            {
                int currentDigit = jsonText[newIndex] - (byte)'0';

                accumulator = accumulator * 10 + currentDigit;

                newIndex++;
            }

            if (integerLoopStartIndex == newIndex)
            {
                return JsonResult<JsonNumber>.Err(
                    JsonErrorType.InvalidSyntax,
                    "Number does not contain any digits in the integer part.",
                    newIndex
                );
            }
        }

        if (newIndex < jsonText.Length && jsonText[newIndex] == (byte)'.')
        {
            newIndex++;
            int fractionStartIndex = newIndex;
            double multiplier = 0.1;

            while (newIndex < jsonText.Length && IsByteDigit(jsonText[newIndex]))
            {
                int currentDigit = jsonText[newIndex] - (byte)'0';

                accumulator += currentDigit * multiplier;

                multiplier *= 0.1;

                newIndex++;
            }

            if (newIndex == fractionStartIndex)
            {
                return JsonResult<JsonNumber>.Err(
                    JsonErrorType.InvalidCharacter,
                    "A number is not allowed to end with a decimal point.",
                    newIndex
                );
            }
        }

        if (newIndex < jsonText.Length && (jsonText[newIndex] == (byte)'e' || jsonText[newIndex] == (byte)'E'))
        {
            if (newIndex + 1 < jsonText.Length)
            {
                (exponentSign, newIndex) = jsonText[newIndex + 1] switch
                {
                    (byte)'-' => (-1, newIndex + 2),
                    (byte)'+' => (1, newIndex + 2),
                    _ => (1, newIndex + 1)
                };
            }
            else
            {
                return JsonResult<JsonNumber>.Err(JsonErrorType.EndOfFile, newIndex);
            }

            int exponentStartIndex = newIndex;

            while (newIndex < jsonText.Length && IsByteDigit(jsonText[newIndex]))
            {
                int currentDigit = jsonText[newIndex] - (byte)'0';

                exponentAccumulator = exponentAccumulator * 10 + currentDigit;

                newIndex++;
            }

            if (newIndex == exponentStartIndex)
            {
                return JsonResult<JsonNumber>.Err(
                    JsonErrorType.InvalidCharacter,
                    "A number in scientific notation is not allowed to end with an 'e'/'E'.",
                    newIndex
                );
            }
        }

        double resultNumber = numberSign * accumulator * CalculatePowerOf10(exponentSign * exponentAccumulator);

        if (newIndex < jsonText.Length)
        {
            byte lastCharacter = jsonText[newIndex];

            return lastCharacter switch
            {
                (byte)',' or (byte)']' or (byte)'}' or (byte)' ' or (byte)'\t' or (byte)'\n' or (byte)'\r' =>
                    JsonResult<JsonNumber>.Ok(
                        new JsonNumber(resultNumber),
                        newIndex
                    ),
                (byte)'.' => JsonResult<JsonNumber>.Err(
                    JsonErrorType.InvalidCharacter,
                    "A number is only allowed to have one decimal point.",
                    newIndex
                ),
                (byte)'e' or (byte)'E' => JsonResult<JsonNumber>.Err(
                    JsonErrorType.InvalidCharacter,
                    "A number is only allowed to have one exponent marker 'e'/'E'.",
                    newIndex
                ),
                _ => JsonResult<JsonNumber>.Err(
                    JsonErrorType.InvalidCharacter,
                    $"A number cannot contain the character '{(char)lastCharacter}'.",
                    newIndex
                )
            };
        }

        return JsonResult<JsonNumber>.Ok(new JsonNumber(resultNumber), newIndex); // numbers are allowed at end of file
    }

    private static bool IsByteDigit(byte byteToCheck)
    {
        return byteToCheck is >= (byte)'0' and <= (byte)'9';
    }

    private static double CalculatePowerOf10(int exponent)
    {
        double result = 1.0;
        int absoluteExponent = Math.Abs(exponent);

        for (int i = 0; i < absoluteExponent; i++)
        {
            result *= 10.0;
        }

        return exponent < 0 ? 1.0 / result : result;
    }
}