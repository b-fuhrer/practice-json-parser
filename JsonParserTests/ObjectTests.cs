using System.Text;
using JsonParserLogic;
namespace JsonParserTests;

public class ObjectTests
{
    private static ReadOnlySpan<byte> ToBytes(string json) => Encoding.UTF8.GetBytes(json);

    [Fact]
    public void ParseObject_Empty_ReturnsSuccess()
    {
        var bytes = ToBytes("{}");
        var result = JsonParser.ParseObject(bytes, 0);

        Assert.True(result.IsSuccess);
        Assert.Empty(result.Object);
        Assert.Equal(2, result.Index);
    }

    [Fact]
    public void ParseObject_SimpleKeyValue_ReturnsSuccess()
    {
        var bytes = ToBytes("{\"key\": 123}");
        var result = JsonParser.ParseObject(bytes, 0);

        Assert.True(result.IsSuccess);
        Assert.True(result.Object.ContainsKey("key"));
        
        var val = result.Object["key"];
        Assert.Equal(JsonType.Number, val.Type);
        Assert.Equal(123.0, val.Number);
    }

    [Theory]
    [InlineData("{\"a\": 1, \"b\": 2}", 2)]
    [InlineData("{\"key\": true, \"nullVal\": null}", 2)]
    public void ParseObject_MultipleKeys_ReturnsSuccess(string input, int expectedCount)
    {
        var bytes = ToBytes(input);
        var result = JsonParser.ParseObject(bytes, 0);

        Assert.True(result.IsSuccess);
        Assert.Equal(expectedCount, result.Object.Count);
    }

    [Fact]
    public void ParseObject_Nested_ReturnsSuccess()
    {
        var bytes = ToBytes("{\"parent\": {\"child\": 1}}");
        var result = JsonParser.ParseObject(bytes, 0);

        Assert.True(result.IsSuccess);
        
        var parent = result.Object["parent"];
        Assert.Equal(JsonType.Object, parent.Type);

        var childObj = parent.Object;
        Assert.Equal(1.0, childObj["child"].Number);
    }

    [Theory]
    [InlineData("{")]             // EOF
    [InlineData("{\"key\"}")]      // missing colon and value
    [InlineData("{\"key\": }")]    // missing value
    [InlineData("{1: 1}")]         // key is not a string (invalid in JSON)
    [InlineData("{\"a\": 1,}")]    // trailing comma
    [InlineData("{\"a\": 1 2}")]   // missing comma between pairs
    public void ParseObject_InvalidSyntax_ReturnsError(string input)
    {
        var bytes = ToBytes(input);
        var result = JsonParser.ParseObject(bytes, 0);

        Assert.False(result.IsSuccess, $"Should have failed: {input}");
    }
}