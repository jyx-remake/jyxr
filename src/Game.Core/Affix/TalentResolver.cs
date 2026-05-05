using System.Collections.ObjectModel;
using Game.Core.Definitions;

namespace Game.Core.Affix;

public static class TalentResolver
{
    public static ResolvedAffixSet Resolve(
        IEnumerable<TalentDefinition> learnedTalents,
        IReadOnlyList<AffixDefinition> flattenedAffixes)
    {
        ArgumentNullException.ThrowIfNull(learnedTalents);
        ArgumentNullException.ThrowIfNull(flattenedAffixes);

        // Avoid relying on the default equality semantics of the TalentDefinition record;
        // instead, perform stable deduplication directly by id.
        var candidateTalentsById = new Dictionary<string, TalentDefinition>(StringComparer.Ordinal);
        var resolvedAffixes = new List<AffixDefinition>(flattenedAffixes);

        foreach (var talent in learnedTalents)
        {
            candidateTalentsById[talent.Id] = talent;
        }

        foreach (var affix in flattenedAffixes)
        {
            if (affix is not GrantTalentAffix grantTalentAffix)
            {
                continue;
            }

            candidateTalentsById[grantTalentAffix.Talent.Id] = grantTalentAffix.Talent;
        }

        var effectiveTalents = candidateTalentsById.Values.ToHashSet();
        var replacedTalentIds = new HashSet<string>(StringComparer.Ordinal);

        foreach (var talent in effectiveTalents)
        {
            foreach (var replacedTalentId in talent.ReplaceTalentIds)
            {
                replacedTalentIds.Add(replacedTalentId);
            }
        }

        foreach (var talent in effectiveTalents)
        {
            if (replacedTalentIds.Contains(talent.Id))
            {
                continue;
            }

            foreach (var talentAffix in talent.Affixes)
            {
                resolvedAffixes.Add(talentAffix with
                {
                    SourceKind = ProviderKind.Talent,
                });
            }
        }

        return new ResolvedAffixSet(
            new ReadOnlyCollection<AffixDefinition>(resolvedAffixes),
            new ReadOnlySet<TalentDefinition>(effectiveTalents));
    }
}
