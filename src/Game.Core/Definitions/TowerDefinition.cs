namespace Game.Core.Definitions;

public sealed record TowerDefinition
{
    public required string Id { get; init; }
    public required string Name { get; init; }
    public string Description { get; init; } = "";
    public IReadOnlyList<TowerStageDefinition> Stages { get; init; } =  [];
    public IReadOnlyList<TowerUnlockConditionDefinition> UnlockConditions { get; init; } = [];
}

public sealed record TowerStageDefinition
{
    public required string Id { get; init; }
    public string Name { get; init; } = "";
    public string BattleId { get; init; } = "";
    public int Index { get; init; }
    public IReadOnlyList<TowerRewardDefinition> Rewards { get; init; } = [];
    public IReadOnlyList<string> AchievementIds { get; init; } = [];
}

public sealed record TowerRewardDefinition
{
    public required string ContentId { get; init; }

    public double Probability { get; init; }

    public int? MaxClaims { get; init; }
}

public sealed record TowerUnlockConditionDefinition
{
    public required string Type { get; init; }

    public string Value { get; init; } = "";
}
