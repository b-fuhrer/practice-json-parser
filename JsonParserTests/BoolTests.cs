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
        var result = JsonParser.ParseBool(bytes, 0);

        Assert.True(result.Success, $"Failed to parse: {input}");
        Assert.Equal(expected, result.Value.Bool);
        Assert.Equal(bytes.Length, result.Index);
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

        Assert.False(result.Success, $"Should have failed for input: {input}");
    }
}