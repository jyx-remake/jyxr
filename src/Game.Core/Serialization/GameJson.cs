using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Unicode;

namespace Game.Core.Serialization;

public static class GameJson
{
    public static JsonSerializerOptions Default { get; } = CreateDefaultOptions();

    private static JsonSerializerOptions CreateDefaultOptions() =>
        new()
        {
            PropertyNameCaseInsensitive = true,
            ReadCommentHandling = JsonCommentHandling.Skip,
            Encoder = JavaScriptEncoder.Create(UnicodeRanges.All),
            Converters = { new JsonStringEnumConverter() },
        };
}
