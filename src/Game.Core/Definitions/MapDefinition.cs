using System.Text.Json.Serialization;
using Game.Core.Model;

namespace Game.Core.Definitions;

public sealed record MapDefinition
{
    public required string Id { get; init; }

    public required string Name { get; init; }

    public MapKind Kind { get; init; } = MapKind.Small;

    public double TravelSpeed { get; init; }

    public string? DefaultLocation { get; init; }

    public string? Description { get; init; }

    public string? Picture { get; init; }

    public IReadOnlyList<string> Musics { get; init; } = [];

    public IReadOnlyList<MapLocationDefinition> Locations { get; init; } = [];
}

public sealed record MapLocationDefinition
{
    public required string Id { get; init; }

    public string? Name { get; init; }

    public MapPosition? Position { get; init; }

    public string? Description { get; init; }

    public string? Picture { get; init; }

    public bool HideWhenNoEvent { get; init; }

    public string? NoEventImage { get; init; }

    public IReadOnlyList<MapEventDefinition> Events { get; init; } = [];
}

public sealed record MapEventDefinition
{
    public required string Id { get; init; }

    [JsonConverter(typeof(Game.Core.Serialization.ParsedCallJsonConverter))]
    public required ParsedCall Action { get; init; }

    [JsonConverter(typeof(Game.Core.Serialization.ParsedExpressionJsonConverter))]
    public ParsedExpression? When { get; init; }

    public RepeatMode RepeatMode { get; init; } = RepeatMode.Infinite;

    /// <summary>
    /// Maximum number of successful interactions for a once-marked event.
    /// Null means the traditional single occurrence; -1 means unlimited.
    /// </summary>
    public int? RepeatLimit { get; init; }

    public string? Image { get; init; }

    public string? Description { get; init; }

}

public enum RepeatMode
{
    [JsonStringEnumMemberName("once")]
    Once,
    [JsonStringEnumMemberName("infinite")]
    Infinite
}

public enum MapKind
{
    [JsonStringEnumMemberName("small")]
    Small,
    [JsonStringEnumMemberName("large")]
    Large,
}
