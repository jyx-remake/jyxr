using System.Collections.ObjectModel;
using Game.Core.Definitions;
using Game.Core.Model;

namespace Game.Core.Affix;

public sealed class CharacterAffixSnapshot
{
    public static CharacterAffixSnapshot Empty { get; } = new(
        effectiveTalents: new HashSet<TalentDefinition>(),
        traits: new HashSet<TraitId>(),
        hooksByTiming: new ReadOnlyDictionary<HookTiming, IReadOnlyList<HookAffix>>(
            new Dictionary<HookTiming, IReadOnlyList<HookAffix>>()),
        resolvedModelId: null,
        statModifierBuckets: new ReadOnlyDictionary<StatType, ModifierBucket>(new Dictionary<StatType, ModifierBucket>()),
        skillModifierBuckets: new ReadOnlyDictionary<string, ModifierBucket>(new Dictionary<string, ModifierBucket>()),
        weaponModifierBuckets: new ReadOnlyDictionary<WeaponType, ModifierBucket>(new Dictionary<WeaponType, ModifierBucket>()),
        legendChanceModifierBuckets: new ReadOnlyDictionary<string, ModifierBucket>(new Dictionary<string, ModifierBucket>()));

    public CharacterAffixSnapshot(
        IReadOnlySet<TalentDefinition> effectiveTalents,
        IReadOnlySet<TraitId> traits,
        IReadOnlyDictionary<HookTiming, IReadOnlyList<HookAffix>> hooksByTiming,
        string? resolvedModelId,
        IReadOnlyDictionary<StatType, ModifierBucket> statModifierBuckets,
        IReadOnlyDictionary<string, ModifierBucket> skillModifierBuckets,
        IReadOnlyDictionary<WeaponType, ModifierBucket> weaponModifierBuckets,
        IReadOnlyDictionary<string, ModifierBucket> legendChanceModifierBuckets)
    {
        ArgumentNullException.ThrowIfNull(effectiveTalents);
        ArgumentNullException.ThrowIfNull(traits);
        ArgumentNullException.ThrowIfNull(hooksByTiming);
        ArgumentNullException.ThrowIfNull(statModifierBuckets);
        ArgumentNullException.ThrowIfNull(skillModifierBuckets);
        ArgumentNullException.ThrowIfNull(weaponModifierBuckets);
        ArgumentNullException.ThrowIfNull(legendChanceModifierBuckets);

        EffectiveTalents = effectiveTalents;
        Traits = traits;
        HooksByTiming = hooksByTiming;
        ResolvedModelId = resolvedModelId;
        StatModifierBuckets = statModifierBuckets;
        SkillModifierBuckets = skillModifierBuckets;
        WeaponModifierBuckets = weaponModifierBuckets;
        LegendChanceModifierBuckets = legendChanceModifierBuckets;
    }

    public IReadOnlySet<TalentDefinition> EffectiveTalents { get; }

    public IReadOnlySet<TraitId> Traits { get; }

    public IReadOnlyDictionary<HookTiming, IReadOnlyList<HookAffix>> HooksByTiming { get; }

    public string? ResolvedModelId { get; }

    public IReadOnlyDictionary<StatType, ModifierBucket> StatModifierBuckets { get; }

    public IReadOnlyDictionary<string, ModifierBucket> SkillModifierBuckets { get; }

    public IReadOnlyDictionary<WeaponType, ModifierBucket> WeaponModifierBuckets { get; }

    public IReadOnlyDictionary<string, ModifierBucket> LegendChanceModifierBuckets { get; }
}
