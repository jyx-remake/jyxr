using System.Text.Json.Serialization;
using Game.Core.Model;

namespace Game.Core.Definitions;

public sealed record MapDefinition
{
    public required string Id { get; init; }

    public required string Name { get; init; }

    public MapKind Kind { get; init; } = MapKind.Small;

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

    public IReadOnlyList<MapEventDefinition> Events { get; init; } = [];
}

public sealed record MapEventDefinition
{
    public required string Type { get; init; }

    public string TargetId { get; init; } = "";

    public int Probability { get; init; }

    public RepeatMode RepeatMode { get; init; } = RepeatMode.Infinite;

    public string? Image { get; init; }

    public string? Description { get; init; }

    public IReadOnlyList<MapEventConditionDefinition> Conditions { get; init; } = [];
}

public sealed record MapEventConditionDefinition
{
    public required string Type { get; init; }

    public string Value { get; init; } = "";
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
