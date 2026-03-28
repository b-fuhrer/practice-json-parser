using System.Text;
using JsonParserLogic;
namespace JsonParserTests;

public class NullTests
{
    private static ReadOnlySpan<byte> ToBytes(string json) => Encoding.UTF8.GetBytes(json);

    [Fact]
    public void ParseNull_ValidInput_ReturnsSuccess()
    {
        var bytes = ToBytes("null");
        using var context = new JsonContext(bytes.Length);
        var result = JsonParser.ParseNull(context, bytes, 0);

        Assert.True(result.IsSuccess);
        Assert.Equal(JsonType.Null, result.Type);
        Assert.Equal(4, result.JsonIndex);
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
        using var context = new JsonContext(bytes.Length);
        var result = JsonParser.ParseNull(context, bytes, 0);

        Assert.False(result.IsSuccess);
    }
}