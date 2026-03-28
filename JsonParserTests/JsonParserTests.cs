using System.Text;
using JsonParserLogic;
namespace JsonParserTests;

public class JsonParserTests
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

    [Theory]
    [InlineData("true")]
    [InlineData("null")]
    [InlineData("123.45")]
    [InlineData("\"hello world\"")]
    [InlineData("{}")]
    [InlineData("[]")]
    [InlineData("{\"key\": \"value\"}")]
    [InlineData("[1, 2, 3]")]
    [InlineData("  { \"a\": 1 }  ")]
    [InlineData("\t[ 1 , 2 ]\n")]
    public void Parse_ValidJson_ReturnsSuccess(string json)
    {
        var bytes = ToBytes(json);
        using var context = new JsonContext(bytes.Length);
        var rootNode = JsonParser.Parse(context, bytes);

        Assert.True(rootNode.IsSuccess);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("{\"a\": 1} garbage")]
    [InlineData("[1, 2] 3")]
    [InlineData("null x")]
    [InlineData("{")]
    [InlineData("[")]
    [InlineData("{\"a\": 1")]
    [InlineData("[1, 2")]
    [InlineData("{\"a\"}")]
    public void Parse_InvalidJson_ReturnsError(string json)
    {
        var bytes = ToBytes(json);
        using var context = new JsonContext(bytes.Length);
        var rootNode = JsonParser.Parse(context, bytes);

        Assert.False(rootNode.IsSuccess);

        if (json.Contains("garbage") || json.EndsWith('x') || json.EndsWith('3'))
        {
             Assert.Equal(JsonError.InvalidCharacter, rootNode.Error);
        }
    }

    [Fact]
    public void Parse_ComplexStructure_ReturnsCorrectData()
    {
        string json = @"
        {
            ""id"": 101,
            ""isActive"": true,
            ""tags"": [""admin"", ""editor""],
            ""metadata"": {
                ""lastLogin"": null,
                ""retryCount"": 3
            }
        }";

        var bytes = ToBytes(json);
        using var context = new JsonContext(bytes.Length);
        var rootNode = JsonParser.Parse(context, bytes);

        Assert.True(rootNode.IsSuccess);

        Assert.Equal(101.0, GetProperty(rootNode, "id").GetNumber());
        Assert.True(GetProperty(rootNode, "isActive").GetBoolean());

        var tags = GetProperty(rootNode, "tags").GetArray();
        Assert.Equal("admin", tags.GetNext().GetString());
        Assert.Equal("editor", tags.GetNext().GetString());
        Assert.False(tags.GetNext().IsSuccess);

        var meta = GetProperty(rootNode, "metadata");
        Assert.Equal(JsonType.Null, GetProperty(meta, "lastLogin").Type);
        Assert.Equal(3.0, GetProperty(meta, "retryCount").GetNumber());
    }
}