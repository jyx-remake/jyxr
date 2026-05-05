using Game.Core.Model;

namespace Game.Core.Definitions;

public sealed record GrowTemplateDefinition
{
    public required string Id { get; init; }

    public required string Name { get; init; }

    public IReadOnlyDictionary<StatType, int> StatGrowth { get; init; } = new Dictionary<StatType, int>();
}
