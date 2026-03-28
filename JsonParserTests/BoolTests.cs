using System.Text;
using JsonParserLogic;
namespace JsonParserTests;

public class BoolTests
{
    private static ReadOnlySpan<byte> ToBytes(string json) => Encoding.UTF8.GetBytes(json);

    [Theory]
    [InlineData("true", true)]
    [InlineData("false", false)]
    public void ParseBool_ValidInputs_ReturnCorrectValue(string input, bool expected)
    {
        var bytes = ToBytes(input);
        using var context = new JsonContext(bytes.Length);
        var result = JsonParser.ParseBool(bytes, 0);

        Assert.True(result.IsSuccess);

        var node = JsonNode.Ok(context, result.Type, result.ElementIndex, isRawValue: true);
        Assert.Equal(expected, node.GetBoolean());
        Assert.Equal(bytes.Length, result.JsonIndex);
    }

    [Theory]
    [InlineData("True")]
    [InlineData("FALSE")]
    [InlineData("tru")]
    [InlineData("fals")]
    [InlineData("truee")]
    [InlineData("notbool")]
    public void ParseBool_InvalidInputs_ReturnError(string input)
    {
        var bytes = ToBytes(input);
        var result = JsonParser.ParseBool(bytes, 0);

        Assert.False(result.IsSuccess);
    }
}