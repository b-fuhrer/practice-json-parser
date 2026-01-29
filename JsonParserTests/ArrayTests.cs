using System.Text;
using JsonParserLogic;
using JsonParserLogic.Types;
namespace JsonParserTests;

public class ArrayTests
{
    private static ReadOnlySpan<byte> ToBytes(string json) => Encoding.UTF8.GetBytes(json);

    [Fact]
    public void ParseArray_Empty_ReturnsSuccess()
    {
        var bytes = ToBytes("[]");
        var result = JsonParser.ParseArray(bytes, 0);

        Assert.True(result.Success);
        Assert.IsType<JsonArray>(result.Value);
        Assert.Empty(result.Value.Elements);
        Assert.Equal(2, result.Index); // consumed '[' and ']'
    }

    [Theory]
    [InlineData("[1, 2, 3]", 3)]
    [InlineData("[\"a\", \"b\"]", 2)]
    [InlineData("[true, false, null]", 3)]
    public void ParseArray_SimpleList_ReturnsSuccess(string input, int expectedCount)
    {
        var bytes = ToBytes(input);
        var result = JsonParser.ParseArray(bytes, 0);

        Assert.True(result.Success);
        Assert.Equal(expectedCount, result.Value.Elements.Length);
    }

    [Fact]
    public void ParseArray_Nested_ReturnsSuccess()
    {
        // matrix: [[1, 2], [3, 4]]
        var bytes = ToBytes("[[1, 2], [3, 4]]");
        var result = JsonParser.ParseArray(bytes, 0);

        Assert.True(result.Success);
        var outerArray = result.Value.Elements;

        Assert.Equal(2, outerArray.Length);
        // verify the first inner array contains 1 and 2
        var inner1 = (JsonArray)outerArray[0];
        Assert.Equal(1.0, ((JsonNumber)inner1.Elements[0]).Number);
    }

    [Fact]
    public void ParseArray_MixedTypes_ReturnsSuccess()
    {
        var bytes = ToBytes("[1, \"text\", true]");
        var result = JsonParser.ParseArray(bytes, 0);

        Assert.True(result.Success);
        Assert.IsType<JsonNumber>(result.Value.Elements[0]);
        Assert.IsType<JsonString>(result.Value.Elements[1]);
        Assert.IsType<JsonBool>(result.Value.Elements[2]);
    }

    [Theory]
    [InlineData("[")]         // EOF
    [InlineData("[1, 2")]    // missing closing bracket
    [InlineData("[1 2]")]    // missing comma
    [InlineData("[1, , 2]")] // double comma (invalid in standard JSON)
    [InlineData("[,]")]      // leading comma
    [InlineData("[1,]")]     // trailing comma (invalid in standard JSON)
    public void ParseArray_InvalidSyntax_ReturnsError(string input)
    {
        var bytes = ToBytes(input);
        var result = JsonParser.ParseArray(bytes, 0);

        Assert.False(result.Success, $"Should have failed: {input}");
    }
}