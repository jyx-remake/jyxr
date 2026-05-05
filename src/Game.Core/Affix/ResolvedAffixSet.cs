using Game.Core.Definitions;

namespace Game.Core.Affix;

public sealed class ResolvedAffixSet
{
    public ResolvedAffixSet(IReadOnlyList<AffixDefinition> affixes, IReadOnlySet<TalentDefinition> effectiveTalents)
    {
        ArgumentNullException.ThrowIfNull(affixes);
        ArgumentNullException.ThrowIfNull(effectiveTalents);

        Affixes = affixes;
        EffectiveTalents = effectiveTalents;
    }

    public IReadOnlyList<AffixDefinition> Affixes { get; }

    public IReadOnlySet<TalentDefinition> EffectiveTalents { get; }
}
