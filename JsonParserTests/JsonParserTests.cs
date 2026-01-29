using System.Text;
using JsonParserLogic;
namespace JsonParserTests;

public class JsonParserTests
{
    private static ReadOnlySpan<byte> ToBytes(string json) => Encoding.UTF8.GetBytes(json);

    [Theory]
    // basic types at root
    [InlineData("true")]
    [InlineData("null")]
    [InlineData("123.45")]
    [InlineData("\"hello world\"")]
    // containers at root
    [InlineData("{}")]
    [InlineData("[]")]
    [InlineData("{\"key\": \"value\"}")]
    [InlineData("[1, 2, 3]")]
    // whitespace handling
    [InlineData("  { \"a\": 1 }  ")]
    [InlineData("\t[ 1 , 2 ]\n")]
    public void Parse_ValidJson_ReturnsSuccess(string json)
    {
        var bytes = ToBytes(json);
        var result = JsonParser.Parse(bytes);

        Assert.True(result.IsSuccess, $"Failed to parse valid JSON: {json}");
    }

    [Theory]
    // empty
    [InlineData("")]
    [InlineData("   ")]
    // trailing garbage
    [InlineData("{\"a\": 1} garbage")]
    [InlineData("[1, 2] 3")]
    [InlineData("null x")]
    // structural errors
    [InlineData("{")]
    [InlineData("[")]
    [InlineData("{\"a\": 1")]
    [InlineData("[1, 2")]
    [InlineData("{\"a\"}")]
    public void Parse_InvalidJson_ReturnsError(string json)
    {
        var bytes = ToBytes(json);
        var result = JsonParser.Parse(bytes);

        Assert.False(result.IsSuccess, $"Should have failed for input: {json}");

        if (json.Contains("garbage") || json.EndsWith('x') || json.EndsWith('3'))
        {
             Assert.Equal(ErrorType.InvalidCharacter, result.ErrorType);
        }
    }

    [Fact]
    public void Parse_ComplexStructure_ReturnsCorrectData()
    {
        // A realistic scenario mixing all types
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
        var result = JsonParser.Parse(bytes);

        Assert.True(result.IsSuccess);

        var root = result.Object;

        // check simple properties
        Assert.Equal(101.0, root["id"].Number);
        Assert.True(root["isActive"].Bool);

        // check nested array
        var tags = root["tags"];
        Assert.Equal(2, tags.Array.Length);
        Assert.Equal("admin", tags.Array[0].String);

        // check nested object
        var meta = root["metadata"].Object;
        Assert.Equal(JsonType.Null, meta["lastLogin"].Type);
        Assert.Equal(3.0, meta["retryCount"].Number);
    }
}