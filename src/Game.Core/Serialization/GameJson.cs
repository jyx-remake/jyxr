using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Unicode;

namespace Game.Core.Serialization;

public static class GameJson
{
    public static JsonSerializerOptions Default { get; } = CreateDefaultOptions();

    private static JsonDocumentOptions DocumentOptions { get; } = new()
    {
        CommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true,
    };

    public static JsonDocument ParseDocument(string json) =>
        JsonDocument.Parse(json, DocumentOptions);

    private static JsonSerializerOptions CreateDefaultOptions() =>
        new()
        {
            PropertyNameCaseInsensitive = true,
            ReadCommentHandling = JsonCommentHandling.Skip,
            AllowTrailingCommas = true,
            Encoder = JavaScriptEncoder.Create(UnicodeRanges.All),
            Converters = { new JsonStringEnumConverter() },
        };
}
