using Game.Core.Definitions;

namespace Game.Core.Model.Character;

public static class CharacterTalentPointCalculator
{
    public const int DefaultInnatePoints = 20;

    public static int CalculateCapacity(CharacterInstance character, GrowTemplateDefinition growTemplate)
    {
        ArgumentNullException.ThrowIfNull(character);
        ArgumentNullException.ThrowIfNull(growTemplate);

        var levelGrowth = growTemplate.StatGrowth.GetValueOrDefault(StatType.Wuxue);
        return checked(DefaultInnatePoints + character.Level * levelGrowth);
    }

    public static int CalculateSpentPoints(CharacterInstance character)
    {
        ArgumentNullException.ThrowIfNull(character);
        return character.UnlockedTalents.Sum(static talent => talent.Point);
    }
}
