using System.Text;
using JsonParserLogic;
namespace JsonParserTests;

public class ObjectTests
{
    private static ReadOnlySpan<byte> ToBytes(string json) => Encoding.UTF8.GetBytes(json);

    private static JsonNode GetProperty(JsonNode node, string key)
    {
        var enumerator = node.GetObject();
        ObjectProperty prop;
        while ((prop = enumerator.GetNext()).Value.IsSuccess)
        {
            if (prop.Key == key) return prop.Value;
        }
        throw new KeyNotFoundException();
    }

    [Fact]
    public void ParseObject_Empty_ReturnsSuccess()
    {
        var bytes = ToBytes("{}");
        using var context = new JsonContext(bytes.Length);
        var result = JsonParser.ParseObject(context, bytes, 0);

        Assert.True(result.IsSuccess);

        var node = JsonNode.Ok(context, result.Type, result.ElementIndex);
        var enumerator = node.GetObject();
        Assert.False(enumerator.GetNext().Value.IsSuccess);
        Assert.Equal(2, result.JsonIndex);
    }

    [Fact]
    public void ParseObject_SimpleKeyValue_ReturnsSuccess()
    {
        var bytes = ToBytes("{\"key\": 123}");
        using var context = new JsonContext(bytes.Length);
        var result = JsonParser.ParseObject(context, bytes, 0);

        Assert.True(result.IsSuccess);

        var node = JsonNode.Ok(context, result.Type, result.ElementIndex);
        var val = GetProperty(node, "key");
        Assert.Equal(JsonType.Number, val.Type);
        Assert.Equal(123.0, val.GetNumber());
    }

    [Theory]
    [InlineData("{\"a\": 1, \"b\": 2}", 2)]
    [InlineData("{\"key\": true, \"nullVal\": null}", 2)]
    public void ParseObject_MultipleKeys_ReturnsSuccess(string input, int expectedCount)
    {
        var bytes = ToBytes(input);
        using var context = new JsonContext(bytes.Length);
        var result = JsonParser.ParseObject(context, bytes, 0);

        Assert.True(result.IsSuccess);

        var node = JsonNode.Ok(context, result.Type, result.ElementIndex);
        int count = 0;
        var enumerator = node.GetObject();
        while (enumerator.GetNext().Value.IsSuccess)
        {
            count++;
        }

        Assert.Equal(expectedCount, count);
    }

    [Fact]
    public void ParseObject_Nested_ReturnsSuccess()
    {
        var bytes = ToBytes("{\"parent\": {\"child\": 1}}");
        using var context = new JsonContext(bytes.Length);
        var result = JsonParser.ParseObject(context, bytes, 0);

        Assert.True(result.IsSuccess);

        var node = JsonNode.Ok(context, result.Type, result.ElementIndex);
        var parent = GetProperty(node, "parent");
        Assert.Equal(JsonType.Object, parent.Type);

        var childVal = GetProperty(parent, "child");
        Assert.Equal(1.0, childVal.GetNumber());
    }

    [Theory]
    [InlineData("{")]
    [InlineData("{\"key\"}")]
    [InlineData("{\"key\": }")]
    [InlineData("{1: 1}")]
    [InlineData("{\"a\": 1,}")]
    [InlineData("{\"a\": 1 2}")]
    public void ParseObject_InvalidSyntax_ReturnsError(string input)
    {
        var bytes = ToBytes(input);
        using var context = new JsonContext(bytes.Length);
        var result = JsonParser.ParseObject(context, bytes, 0);

        Assert.False(result.IsSuccess);
    }
}