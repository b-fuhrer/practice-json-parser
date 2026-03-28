using System.Text;
using JsonParserLogic;
namespace JsonParserTests;

public class NumberTests
{
    private static ReadOnlySpan<byte> ToBytes(string json) => Encoding.UTF8.GetBytes(json);

    [Theory]
    [InlineData("0", 0)]
    [InlineData("123", 123)]
    [InlineData("-123", -123)]
    [InlineData("1.25", 1.25)]
    [InlineData("-0.5", -0.5)]
    [InlineData("1e3", 1000)]
    [InlineData("1.5e-2", 0.015)]
    public void ParseNumber_ValidInputs_ReturnCorrectValue(string input, double expected)
    {
        var bytes = ToBytes(input);
        using var context = new JsonContext(bytes.Length);
        var result = JsonParser.ParseNumber(context, bytes, 0);

        Assert.True(result.IsSuccess);

        var node = JsonNode.Ok(context, result.Type, result.ElementIndex);
        Assert.Equal(expected, node.GetNumber());
        Assert.Equal(bytes.Length, result.JsonIndex);
    }

    [Theory]
    [InlineData("1.2.3")]
    [InlineData("1e2e3")]
    [InlineData("e3")]
    [InlineData("-e10")]
    [InlineData("1e1.3")]
    [InlineData("0123")]
    [InlineData("--5")]
    [InlineData(".1")]
    [InlineData("+1")]
    public void ParseNumber_InvalidFormats_ReturnError(string input)
    {
        var bytes = ToBytes(input);
        using var context = new JsonContext(bytes.Length);
        var result = JsonParser.ParseNumber(context, bytes, 0);

        Assert.False(result.IsSuccess);
    }
}