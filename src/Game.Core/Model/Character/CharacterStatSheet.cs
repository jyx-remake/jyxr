namespace Game.Core.Model.Character;

public sealed record CharacterStatSheet(
    IReadOnlyDictionary<StatType, int> BaseStats,
    IReadOnlyDictionary<StatType, int> FinalStats);
