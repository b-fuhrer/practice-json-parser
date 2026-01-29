using System.Text;
using JsonParserLogic;
namespace JsonParserTests;

public class StringTests
{
    private static ReadOnlySpan<byte> ToBytes(string json) => Encoding.UTF8.GetBytes(json);

    [Theory]
    // basic strings
    [InlineData("\"hello\"", "hello")]
    [InlineData("\"\"", "")]
    // escaped characters
    [InlineData("\"line1\\nline2\"", "line1\nline2")]
    [InlineData("\"tab\\tspace\"", "tab\tspace")]
    [InlineData("\"quote\\\"inside\"", "quote\"inside")]
    [InlineData("\"backslash\\\\test\"", "backslash\\test")]
    [InlineData("\"slash\\/test\"", "slash/test")]
    // unicode escapes
    [InlineData("\"\\u0041\"", "A")]
    [InlineData("\"\\u2764\"", "❤")]
    public void ParseString_ValidInputs_ReturnCorrectValue(string input, string expected)
    {
        var bytes = ToBytes(input);
        var result = JsonParser.ParseString(bytes, 0);

        Assert.True(result.Success, $"Failed to parse: {input}");
        Assert.Equal(expected, result.Value.String);
        Assert.Equal(bytes.Length, result.Index);
    }

    [Theory]
    // no missing opening quote test because parser design makes this impossible
    [InlineData("\"unclosed string")]
    [InlineData("\"invalid\\escape\"")]
    [InlineData("\"\\uGGGG\"")] // invalid hex in unicode escape
    public void ParseString_InvalidFormats_ReturnError(string input)
    {
        var bytes = ToBytes(input);
        var result = JsonParser.ParseString(bytes, 0);

        Assert.False(result.Success);
    }
}