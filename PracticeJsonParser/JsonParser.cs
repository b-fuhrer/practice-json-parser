public static class JsonParser
{
    // not implemented yet
    public static JsonValue Parse(ReadOnlySpan<char> jsonText)
    {
        int currentPosition = 0;
        
        return JsonNull.Instance;
    }

    private static int SkipWhitespace(ReadOnlySpan<char> jsonText, int currentPosition)
    {
        while (currentPosition < jsonText.Length)
        {
            char currentCharacter = jsonText[currentPosition];
            
            // treats all control characters (ASCII < 32) as whitespace
            // includes JSON whitespace characters: '\t', '\n', '\r'
            if (currentCharacter > ' ') return currentPosition;
            
            currentPosition++;
        }
        
        return currentPosition;
    }
}
