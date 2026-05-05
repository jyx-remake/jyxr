using Game.Core.Model;

namespace Game.Core.Definitions;

public sealed record BattleDefinition
{
    public required string Id { get; init; }

    public required string Name { get; init; }

    public required string MapId { get; init; }

    public string? Music { get; init; }

    public IReadOnlyList<string> RequiredCharacterIds { get; init; } = [];

    public IReadOnlyList<BattleParticipantDefinition> Participants { get; init; } = [];

    public IReadOnlyList<BattleRandomParticipantDefinition> RandomParticipants { get; init; } = [];
}


public sealed record BattleParticipantDefinition
{
    public required GridPosition Position { get; init; }

    public int Team { get; init; }

    public int Facing { get; init; }

    public string? CharacterId { get; init; }

    public int? PartyIndex { get; init; }
}

public sealed record BattleRandomParticipantDefinition
{
    public required GridPosition Position { get; init; }

    public int Facing { get; init; }

    public required string CharacterId { get; init; }

    public int Level { get; init; }

    public string? Animation { get; init; }

    public bool Boss { get; init; }
}
