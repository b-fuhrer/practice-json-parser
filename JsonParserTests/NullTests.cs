using System.Text;
using PracticeJsonParser;
using PracticeJsonParser.Types;
namespace JsonParserTests;

public class NullTests
{
    private static ReadOnlySpan<byte> ToBytes(string json) => Encoding.UTF8.GetBytes(json);

    [Fact]
    public void ParseNull_ValidInput_ReturnsSuccess()
    {
        var bytes = ToBytes("null");
        var result = JsonParser.ParseNull(bytes, 0);

        Assert.True(result.Success);
        Assert.IsType<JsonNull>(result.Value);
        Assert.Equal(4, result.Index);
    }

    [Theory]
    [InlineData("Null")]
    [InlineData("NULL")]
    [InlineData("nul")]
    [InlineData("nil")]
    [InlineData("nothing")]
    [InlineData("nulll")]
    public void ParseNull_InvalidInputs_ReturnsError(string input)
    {
        var bytes = ToBytes(input);
        var result = JsonParser.ParseNull(bytes, 0);

        Assert.False(result.Success);
    }
}