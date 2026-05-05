using System.Collections.ObjectModel;
using Game.Core.Definitions;

namespace Game.Core.Affix;

public static class AffixResolver
{
    public static ResolvedAffixSet Resolve(
        IEnumerable<AffixDefinition> activeAffixes,
        IEnumerable<TalentDefinition> learnedTalents)
    {
        ArgumentNullException.ThrowIfNull(activeAffixes);
        ArgumentNullException.ThrowIfNull(learnedTalents);

        return TalentResolver.Resolve(learnedTalents, new ReadOnlyCollection<AffixDefinition>(activeAffixes.ToList()));
    }

    public static IReadOnlyList<AffixDefinition> ResolveProviderAffixes(
        IAffixProvider provider,
        ProviderKind sourceKind)
    {
        ArgumentNullException.ThrowIfNull(provider);

        var affixes = new List<AffixDefinition>();

        foreach (var affix in provider.Affixes)
        {
            ArgumentNullException.ThrowIfNull(affix);

            affixes.Add(affix with
            {
                SourceKind = sourceKind,
            });
        }

        return new ReadOnlyCollection<AffixDefinition>(affixes);
    }

    public static IReadOnlyList<AffixDefinition> ResolveSkillAffixes(
        IEnumerable<SkillAffixDefinition> skillAffixes,
        int skillLevel,
        bool isEquippedInternalSkill,
        ProviderKind sourceKind)
    {
        ArgumentNullException.ThrowIfNull(skillAffixes);
        ArgumentOutOfRangeException.ThrowIfLessThan(skillLevel, 1);

        var affixes = new List<AffixDefinition>();

        foreach (var affix in skillAffixes)
        {
            ArgumentNullException.ThrowIfNull(affix);
            ArgumentNullException.ThrowIfNull(affix.Effect);

            if (skillLevel < affix.MinimumLevel)
            {
                continue;
            }

            if (affix.RequiresEquippedInternalSkill && !isEquippedInternalSkill)
            {
                continue;
            }

            affixes.Add(affix.Effect with
            {
                SourceKind = sourceKind,
            });
        }

        return new ReadOnlyCollection<AffixDefinition>(affixes);
    }
}
