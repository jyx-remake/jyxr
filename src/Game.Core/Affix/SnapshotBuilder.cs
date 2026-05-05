using System.Collections.ObjectModel;
using Game.Core.Model;

namespace Game.Core.Affix;

public static class SnapshotBuilder
{
    public static CharacterAffixSnapshot Build(ResolvedAffixSet resolvedAffixSet)
    {
        ArgumentNullException.ThrowIfNull(resolvedAffixSet);

        var statBuckets = new Dictionary<StatType, ModifierBucket>();
        var skillBuckets = new Dictionary<string, ModifierBucket>();
        var weaponBuckets = new Dictionary<WeaponType, ModifierBucket>();
        var legendBuckets = new Dictionary<string, ModifierBucket>();
        var traits = new HashSet<TraitId>();
        var hooksByTiming = new Dictionary<HookTiming, List<HookAffix>>();
        GrantModelAffix? selectedModel = null;

        foreach (var affix in resolvedAffixSet.Affixes)
        {
            switch (affix)
            {
                case StatModifierAffix statModifierAffix:
                    statBuckets[statModifierAffix.Stat] = GetOrEmpty(statBuckets, statModifierAffix.Stat).Apply(statModifierAffix.Value);
                    break;

                case SkillBonusModifierAffix skillModifierAffix:
                    skillBuckets[skillModifierAffix.SkillId] = GetOrEmpty(skillBuckets, skillModifierAffix.SkillId).Apply(skillModifierAffix.Value);
                    break;

                case WeaponBonusModifierAffix weaponModifierAffix:
                    weaponBuckets[weaponModifierAffix.WeaponType] = GetOrEmpty(weaponBuckets, weaponModifierAffix.WeaponType).Apply(weaponModifierAffix.Value);
                    break;

                case LegendSkillChanceModifierAffix legendModifierAffix:
                    legendBuckets[legendModifierAffix.SkillId] = GetOrEmpty(legendBuckets, legendModifierAffix.SkillId).Apply(legendModifierAffix.Value);
                    break;

                case TraitAffix traitAffix:
                    traits.Add(traitAffix.TraitId);
                    break;

                case HookAffix hookAffix:
                    GetOrCreate(hooksByTiming, hookAffix.Timing).Add(hookAffix);
                    break;

                case GrantModelAffix grantModelAffix when ShouldReplaceModel(selectedModel, grantModelAffix):
                    selectedModel = grantModelAffix;
                    break;
            }
        }

        return new CharacterAffixSnapshot(
            effectiveTalents: resolvedAffixSet.EffectiveTalents,
            traits: new ReadOnlySet<TraitId>(traits),
            hooksByTiming: BuildHooksByTiming(hooksByTiming),
            resolvedModelId: selectedModel?.ModelId,
            statModifierBuckets: new ReadOnlyDictionary<StatType, ModifierBucket>(statBuckets),
            skillModifierBuckets: new ReadOnlyDictionary<string, ModifierBucket>(skillBuckets),
            weaponModifierBuckets: new ReadOnlyDictionary<WeaponType, ModifierBucket>(weaponBuckets),
            legendChanceModifierBuckets: new ReadOnlyDictionary<string, ModifierBucket>(legendBuckets));
    }

    private static bool ShouldReplaceModel(GrantModelAffix? current, GrantModelAffix candidate)
    {
        if (current is null)
        {
            return true;
        }

        if (candidate.Priority != current.Priority)
        {
            return candidate.Priority > current.Priority;
        }

        return true;
    }

    private static TValue GetOrCreate<TKey, TValue>(IDictionary<TKey, TValue> dictionary, TKey key)
        where TKey : notnull
        where TValue : new()
    {
        if (dictionary.TryGetValue(key, out var existing))
        {
            return existing;
        }

        var created = new TValue();
        dictionary[key] = created;
        return created;
    }

    private static ModifierBucket GetOrEmpty<TKey>(IReadOnlyDictionary<TKey, ModifierBucket> dictionary, TKey key)
        where TKey : notnull =>
        dictionary.TryGetValue(key, out var bucket) ? bucket : ModifierBucket.Empty;

    private static IReadOnlyDictionary<HookTiming, IReadOnlyList<HookAffix>> BuildHooksByTiming(
        Dictionary<HookTiming, List<HookAffix>> hooksByTiming)
    {
        var result = new Dictionary<HookTiming, IReadOnlyList<HookAffix>>(hooksByTiming.Count);

        foreach (var (timing, hooks) in hooksByTiming)
        {
            result[timing] = new ReadOnlyCollection<HookAffix>(hooks);
        }

        return new ReadOnlyDictionary<HookTiming, IReadOnlyList<HookAffix>>(result);
    }
}
