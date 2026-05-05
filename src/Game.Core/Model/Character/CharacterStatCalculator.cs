namespace Game.Core.Model.Character;

public sealed class CharacterStatCalculator
{
    public CharacterStatSheet Calculate(CharacterInstance character)
    {
        var baseStats = character.GetBaseStats();
        var finalStats = Enum.GetValues<StatType>()
            .ToDictionary(stat => stat, character.GetBaseStat, EqualityComparer<StatType>.Default);

        return new CharacterStatSheet(baseStats, finalStats);
    }

    public int GetFinalStat(CharacterInstance character, StatType statType) =>
        character.GetBaseStat(statType);
}
