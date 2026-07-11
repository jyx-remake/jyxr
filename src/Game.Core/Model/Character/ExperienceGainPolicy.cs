using Game.Core.Affix;
namespace Game.Core.Model.Character;

public static class ExperienceGainPolicy
{
    public static int Resolve(CharacterInstance character, int experience)
    {
        ArgumentNullException.ThrowIfNull(character);
        ArgumentOutOfRangeException.ThrowIfNegative(experience);

        return character.Traits.Contains(TraitId.DoubleExperienceGain)
            ? checked(experience * 2)
            : experience;
    }
}
