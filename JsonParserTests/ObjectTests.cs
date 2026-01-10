using System.Text;
using PracticeJsonParser;
using PracticeJsonParser.Types;
namespace JsonParserTests;

public class ObjectTests
{
    private static ReadOnlySpan<byte> ToBytes(string json) => Encoding.UTF8.GetBytes(json);

    [Fact]
    public void ParseObject_Empty_ReturnsSuccess()
    {
        var bytes = ToBytes("{}");
        var result = JsonParser.ParseObject(bytes, 0);

        Assert.True(result.Success);
        Assert.Empty(result.Value.Fields);
        Assert.Equal(2, result.Index);
    }

    [Fact]
    public void ParseObject_SimpleKeyValue_ReturnsSuccess()
    {
        var bytes = ToBytes("{\"key\": 123}");
        var result = JsonParser.ParseObject(bytes, 0);

        Assert.True(result.Success);
        Assert.True(result.Value.Fields.ContainsKey("key"));
        
        JsonValue val = result.Value.Fields["key"];
        Assert.IsType<JsonNumber>(val);
        Assert.Equal(123.0, ((JsonNumber)val).Number);
    }

    [Theory]
    [InlineData("{\"a\": 1, \"b\": 2}", 2)]
    [InlineData("{\"key\": true, \"nullVal\": null}", 2)]
    public void ParseObject_MultipleKeys_ReturnsSuccess(string input, int expectedCount)
    {
        var bytes = ToBytes(input);
        var result = JsonParser.ParseObject(bytes, 0);

        Assert.True(result.Success);
        Assert.Equal(expectedCount, result.Value.Fields.Count);
    }

    [Fact]
    public void ParseObject_Nested_ReturnsSuccess()
    {
        var bytes = ToBytes("{\"parent\": {\"child\": 1}}");
        var result = JsonParser.ParseObject(bytes, 0);

        Assert.True(result.Success);
        
        JsonValue parent = result.Value.Fields["parent"];
        Assert.IsType<JsonObject>(parent);

        var childObj = ((JsonObject)parent).Fields;
        Assert.Equal(1.0, ((JsonNumber)childObj["child"]).Number);
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

        Assert.False(result.Success, $"Should have failed: {input}");
    }
}