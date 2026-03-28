using System.Text;
using JsonParserLogic;
namespace JsonParserTests;

public class StringTests
{
    private static ReadOnlySpan<byte> ToBytes(string json) => Encoding.UTF8.GetBytes(json);

    [Theory]
    [InlineData("\"hello\"", "hello")]
    [InlineData("\"\"", "")]
    [InlineData("\"line1\\nline2\"", "line1\nline2")]
    [InlineData("\"tab\\tspace\"", "tab\tspace")]
    [InlineData("\"quote\\\"inside\"", "quote\"inside")]
    [InlineData("\"backslash\\\\test\"", "backslash\\test")]
    [InlineData("\"slash\\/test\"", "slash/test")]
    [InlineData("\"\\u0041\"", "A")]
    [InlineData("\"\\u2764\"", "❤")]
    public void ParseString_ValidInputs_ReturnCorrectValue(string input, string expected)
    {
        var bytes = ToBytes(input);
        using var context = new JsonContext(bytes.Length);
        var result = JsonParser.ParseString(context, bytes, 0);

        Assert.True(result.IsSuccess);

        var node = JsonNode.Ok(context, result.Type, result.ElementIndex);
        Assert.Equal(expected, node.GetString());
        Assert.Equal(bytes.Length, result.JsonIndex);
    }

    [Theory]
    [InlineData("\"unclosed string")]
    [InlineData("\"invalid\\escape\"")]
    [InlineData("\"\\uGGGG\"")]
    public void ParseString_InvalidFormats_ReturnError(string input)
    {
        var bytes = ToBytes(input);
        using var context = new JsonContext(bytes.Length);
        var result = JsonParser.ParseString(context, bytes, 0);

        Assert.False(result.IsSuccess);
    }
}