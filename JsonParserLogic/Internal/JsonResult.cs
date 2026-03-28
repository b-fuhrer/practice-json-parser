namespace JsonParserLogic;

public readonly record struct JsonResult(
    int ElementIndex,
    int JsonIndex,
    JsonType Type,
    JsonError Error
)
{
    // accessors
    public bool IsSuccess => Error == JsonError.None;
    public bool IsError => Error != JsonError.None;

    // constructors
    public static JsonResult Ok(JsonType type, int elementIndex, int nextIndex)
    {
        return new JsonResult(elementIndex, nextIndex, type, JsonError.None);
    }

    public static JsonResult Err(JsonError error, JsonType type, int currentIndex)
    {
        return new JsonResult(0, currentIndex, type, error);
    }
}