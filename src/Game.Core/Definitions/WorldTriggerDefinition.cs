namespace Game.Core.Definitions;

public sealed record WorldTriggerDefinition
{
    public required string Id { get; init; }

    public required string Type { get; init; }

    public string TargetId { get; init; } = "";

    public int Probability { get; init; } = 100;

    public RepeatMode RepeatMode { get; init; } = RepeatMode.Once;

    public string? Description { get; init; }

    public IReadOnlyList<MapEventConditionDefinition> Conditions { get; init; } = [];
}
