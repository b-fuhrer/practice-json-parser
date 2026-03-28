namespace JsonParserLogic;

public static partial class JsonParser
{
    internal static JsonResult ParseNumber(JsonContext context, ReadOnlySpan<byte> jsonText, int currentIndex)
    {
        (double numberSign, int newIndex) = jsonText[currentIndex] == (byte)'-'
            ? (-1.0, currentIndex + 1)
            : (1.0, currentIndex);

        DoubleResult integerPart = ParseIntegerPart(jsonText, newIndex);
        if (integerPart.Error is { } integerPartError)
        {
            return integerPartError;
        }

        double accumulator = integerPart.Value;
        newIndex = integerPart.NewIndex;

        if (newIndex < jsonText.Length && jsonText[newIndex] == (byte)'.')
        {
            DoubleResult decimalPart = ParseDecimalPart(jsonText, newIndex, integerPart.Value);

            if (decimalPart.Error is { } decimalPartError)
            {
                return decimalPartError;
            }

            accumulator = decimalPart.Value;
            newIndex = decimalPart.NewIndex;
        }

        int exponent = 0;
        if (newIndex < jsonText.Length && (jsonText[newIndex] == (byte)'e' || jsonText[newIndex] == (byte)'E'))
        {
            IntResult exponentPart = ParseExponentPart(jsonText, newIndex);

            if (exponentPart.Error is { } exponentPartError)
            {
                return exponentPartError;
            }

            exponent = exponentPart.Value;
            newIndex = exponentPart.NewIndex;
        }

        double resultNumber = exponent == 0
            ? numberSign * accumulator
            : numberSign * accumulator * CalculatePowerOf10(exponent);

        if (newIndex >= jsonText.Length)
        {
            int numberIndexAtEof = context.AddNumber(resultNumber);
            return JsonResult.Ok(JsonType.Number, numberIndexAtEof, newIndex);
        }

        byte lastCharacter = jsonText[newIndex];

        JsonResult result = lastCharacter switch
        {
            (byte)',' or (byte)']' or (byte)'}' or (byte)' ' or (byte)'\t' or (byte)'\n' or (byte)'\r' =>
                JsonResult.Ok(JsonType.Number, context.AddNumber(resultNumber), newIndex),
            (byte)'.' => JsonResult.Err(
                JsonError.InvalidCharacter,
                JsonType.Number,
                newIndex
            ),
            (byte)'e' or (byte)'E' => JsonResult.Err(
                JsonError.InvalidCharacter,
                JsonType.Number,
                newIndex
            ),
            _ => JsonResult.Err(
                JsonError.InvalidCharacter,
                JsonType.Number,
                newIndex
            )
        };

        return result;
    }

    private static DoubleResult ParseIntegerPart(ReadOnlySpan<byte> jsonText, int index)
    {
        double accumulator = 0.0;

        if (index < jsonText.Length && jsonText[index] == (byte)'0')
        {
            if (index + 1 < jsonText.Length && IsDigit(jsonText[index + 1]))
            {
                return DoubleResult.Err(
                    JsonResult.Err(
                        JsonError.InvalidSyntax,
                        JsonType.Number,
                        index
                    )
                );
            }

            return DoubleResult.Ok(0.0, index + 1);
        }

        int integerLoopStartIndex = index;
        while (index < jsonText.Length && IsDigit(jsonText[index]))
        {
            int currentDigit = jsonText[index] - (byte)'0';

            accumulator = accumulator * 10 + currentDigit;

            index++;
        }

        if (integerLoopStartIndex == index)
        {
            return DoubleResult.Err(
                JsonResult.Err(
                    JsonError.InvalidSyntax,
                    JsonType.Number,
                    index
                )
            );
        }

        return DoubleResult.Ok(accumulator, index);
    }

    private static DoubleResult ParseDecimalPart(ReadOnlySpan<byte> jsonText, int index, double accumulator)
    {
        int newIndex = index + 1;
        int fractionStartIndex = newIndex;
        double multiplier = 0.1;

        while (newIndex < jsonText.Length && IsDigit(jsonText[newIndex]))
        {
            int currentDigit = jsonText[newIndex] - (byte)'0';

            accumulator += currentDigit * multiplier;

            multiplier *= 0.1;

            newIndex++;
        }

        if (newIndex == fractionStartIndex)
        {
            return DoubleResult.Err(JsonResult.Err(
                    JsonError.InvalidCharacter,
                    JsonType.Number,
                    index
                )
            );
        }

        return DoubleResult.Ok(accumulator, newIndex);
    }

    private static IntResult ParseExponentPart(ReadOnlySpan<byte> jsonText, int index)
    {
        int exponentAccumulator = 0;
        int exponentSign = 1;

        if (index + 1 < jsonText.Length)
        {
            (exponentSign, index) = jsonText[index + 1] switch
            {
                (byte)'-' => (-1, index + 2),
                (byte)'+' => (1, index + 2),
                _ => (1, index + 1)
            };
        }
        else
        {
            return IntResult.Err(JsonResult.Err(JsonError.EndOfFile, JsonType.Number, index));
        }

        int exponentStartIndex = index;

        while (index < jsonText.Length && IsDigit(jsonText[index]))
        {
            int currentDigit = jsonText[index] - (byte)'0';

            exponentAccumulator = exponentAccumulator * 10 + currentDigit;

            index++;
        }

        if (index == exponentStartIndex)
        {
            return IntResult.Err(
                JsonResult.Err(
                    JsonError.InvalidCharacter,
                    JsonType.Number,
                    index
                )
            );
        }

        return IntResult.Ok(exponentSign * exponentAccumulator, index);
    }

    private static bool IsDigit(byte value)
    {
        return value is >= (byte)'0' and <= (byte)'9';
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

    private readonly record struct DoubleResult(double Value, int NewIndex, JsonResult? Error)
    {
        public static DoubleResult Ok(double value, int index) => new(value, index, null);
        public static DoubleResult Err(JsonResult error) => new(0.0, 0, error);
    }

    private readonly record struct IntResult(int Value, int NewIndex, JsonResult? Error)
    {
        public static IntResult Ok(int value, int index) => new(value, index, null);
        public static IntResult Err(JsonResult error) => new(0, 0, error);
    }
}