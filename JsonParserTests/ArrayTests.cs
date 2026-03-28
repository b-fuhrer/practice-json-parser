using System.Text;
using JsonParserLogic;
namespace JsonParserTests;

public class ArrayTests
{
    private static ReadOnlySpan<byte> ToBytes(string json) => Encoding.UTF8.GetBytes(json);

    [Fact]
    public void ParseArray_Empty_ReturnsSuccess()
    {
        var bytes = ToBytes("[]");
        using var context = new JsonContext(bytes.Length);
        var result = JsonParser.ParseArray(context, bytes, 0);

        Assert.True(result.IsSuccess);
        Assert.Equal(JsonType.Array, result.Type);

        var node = JsonNode.Ok(context, result.Type, result.ElementIndex);
        var enumerator = node.GetArray();
        Assert.False(enumerator.GetNext().IsSuccess);
        Assert.Equal(2, result.JsonIndex);
    }

    [Theory]
    [InlineData("[1, 2, 3]", 3)]
    [InlineData("[\"a\", \"b\"]", 2)]
    [InlineData("[true, false, null]", 3)]
    public void ParseArray_SimpleList_ReturnsSuccess(string input, int expectedCount)
    {
        var bytes = ToBytes(input);
        using var context = new JsonContext(bytes.Length);
        var result = JsonParser.ParseArray(context, bytes, 0);

        Assert.True(result.IsSuccess);

        var node = JsonNode.Ok(context, result.Type, result.ElementIndex);
        int count = 0;
        var enumerator = node.GetArray();
        while (enumerator.GetNext().IsSuccess)
        {
            count++;
        }
        Assert.Equal(expectedCount, count);
    }

    [Fact]
    public void ParseArray_Nested_ReturnsSuccess()
    {
        var bytes = ToBytes("[[1, 2], [3, 4]]");
        using var context = new JsonContext(bytes.Length);
        var result = JsonParser.ParseArray(context, bytes, 0);

        Assert.True(result.IsSuccess);

        var node = JsonNode.Ok(context, result.Type, result.ElementIndex);
        var outerArray = node.GetArray();

        var inner1 = outerArray.GetNext();
        var inner2 = outerArray.GetNext();

        Assert.True(inner1.IsSuccess);
        Assert.True(inner2.IsSuccess);
        Assert.False(outerArray.GetNext().IsSuccess);

        var inner1Array = inner1.GetArray();
        Assert.Equal(1.0, inner1Array.GetNext().GetNumber());
    }

    [Fact]
    public void ParseArray_MixedTypes_ReturnsSuccess()
    {
        var bytes = ToBytes("[1, \"text\", true]");
        using var context = new JsonContext(bytes.Length);
        var result = JsonParser.ParseArray(context, bytes, 0);

        Assert.True(result.IsSuccess);

        var node = JsonNode.Ok(context, result.Type, result.ElementIndex);
        var enumerator = node.GetArray();
        Assert.Equal(JsonType.Number, enumerator.GetNext().Type);
        Assert.Equal(JsonType.String, enumerator.GetNext().Type);
        Assert.Equal(JsonType.Bool, enumerator.GetNext().Type);
    }

    [Theory]
    [InlineData("[")]
    [InlineData("[1, 2")]
    [InlineData("[1 2]")]
    [InlineData("[1, , 2]")]
    [InlineData("[,]")]
    [InlineData("[1,]")]
    public void ParseArray_InvalidSyntax_ReturnsError(string input)
    {
        var bytes = ToBytes(input);
        using var context = new JsonContext(bytes.Length);
        var result = JsonParser.ParseArray(context, bytes, 0);

        Assert.False(result.IsSuccess);
    }

    [Fact]
    public void ParseArray_ObjectWithPrimitiveProperties_DoesNotBreakSiblingTraversal()
    {
        var bytes = ToBytes("[{\"a\": 1, \"b\": true}, 42]");
        using var context = new JsonContext(bytes.Length);
        var result = JsonParser.ParseArray(context, bytes, 0);

        Assert.True(result.IsSuccess);

        var node = JsonNode.Ok(context, result.Type, result.ElementIndex);
        var enumerator = node.GetArray();

        var objectNode = enumerator.GetNext();
        var numberNode = enumerator.GetNext();

        Assert.Equal(JsonType.Object, objectNode.Type);
        Assert.Equal(42.0, numberNode.GetNumber());
        Assert.False(enumerator.GetNext().IsSuccess);
    }
}