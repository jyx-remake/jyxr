namespace Game.Core.Definitions.Skills;

public sealed record ExternalSkillLevelDefinition(
    int Level,
    SkillTargetingDefinition? Targeting = null,
    double? PowerOverride = null,
    string? Animation = null,
    int? Cooldown = null);
