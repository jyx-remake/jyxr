using Game.Core.Affix;
using Game.Core.Definitions;
using Game.Core.Definitions.Skills;
using Game.Core.Model.Skills;

namespace Game.Core.Model.Character;

public sealed class CharacterInstance
{
    public const int MaxBattleRage = 6;

    public required string Id { get; init; }
    public required CharacterDefinition Definition { get; init; }
    public required string Name { get; set; }
    public string? Portrait { get; set; }
    public string? Model { get; set; }
    public string? GrowTemplateId { get; set; }

    public int Level { get; private set; } = 1;
    public int Experience { get; private set; }
    public int UnspentStatPoints { get; private set; }
    public int? CurrentHp { get; private set; }
    public int? CurrentMp { get; private set; }
    public int CurrentRage { get; private set; }

    public CharacterAffixSnapshot Snapshot { get; private set; } = CharacterAffixSnapshot.Empty;
    public IReadOnlySet<TalentDefinition> EffectiveTalents => Snapshot.EffectiveTalents;
    public IReadOnlySet<TraitId> Traits => Snapshot.Traits;
    public string? ResolvedModelId => Snapshot.ResolvedModelId;
    public IReadOnlyDictionary<HookTiming, IReadOnlyList<HookAffix>> HooksByTiming => Snapshot.HooksByTiming;

    public Dictionary<StatType, int> BaseStats { get; } = new();
    public List<TalentDefinition> UnlockedTalents { get; } = [];
    public List<SpecialSkillInstance> SpecialSkills { get; } = [];
    public List<ExternalSkillInstance> ExternalSkills { get; } = [];
    public List<InternalSkillInstance> InternalSkills { get; } = [];
    public string? EquippedInternalSkillId { get; private set; }
    public Dictionary<EquipmentSlotType, EquipmentInstance> EquippedItems { get; } = [];

    public void GrantExperience(int experience)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(experience);
        Experience += experience;
    }

    public void GrantStatPoints(int points)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(points);
        UnspentStatPoints = checked(UnspentStatPoints + points);
    }

    public void SetUnspentStatPoints(int points)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(points);
        UnspentStatPoints = points;
    }

    public void SetLevel(int level)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(level, 1);
        Level = level;
    }

    public void AllocateStat(StatType statType, int points)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(points);

        if (UnspentStatPoints < points)
        {
            throw new InvalidOperationException("Not enough unspent stat points.");
        }

        AddBaseStat(statType, points);
        UnspentStatPoints -= points;
    }

    public void AddBaseStat(StatType statType, int delta)
    {
        var value = checked(BaseStats.GetValueOrDefault(statType) + delta);
        if (value < 0)
        {
            throw new InvalidOperationException($"Base stat '{statType}' cannot be less than zero.");
        }

        if (value == 0)
        {
            BaseStats.Remove(statType);
            return;
        }

        BaseStats[statType] = value;
    }

    public void SetExternalSkillState(ExternalSkillDefinition definition, int level, int exp, bool active)
    {
        ArgumentNullException.ThrowIfNull(definition);
        var skill = SkillListMutation.Find(ExternalSkills, definition.Id);
        if (skill is null)
        {
            ExternalSkills.Add(CreateExternalSkill(definition, level, exp, active));
        }
        else
        {
            skill.SetState(level, exp, active);
        }

        RebuildSnapshot();
    }

    public SkillLevelChange<ExternalSkillInstance> UpgradeExternalSkillLevel(
        ExternalSkillDefinition definition,
        int levels,
        int maxLevel)
    {
        ArgumentNullException.ThrowIfNull(definition);
        var skill = SkillListMutation.Find(ExternalSkills, definition.Id);
        if (skill is null)
        {
            ArgumentOutOfRangeException.ThrowIfNegativeOrZero(levels);
            var createdLevel = Math.Min(levels, maxLevel);
            skill = CreateExternalSkill(definition, createdLevel, 0, true);
            ExternalSkills.Add(skill);
            RebuildSnapshot();
            return new SkillLevelChange<ExternalSkillInstance>(skill, 0, createdLevel, true);
        }

        var change = skill.UpgradeLevel(levels, maxLevel);
        if (change.NewLevel != change.OldLevel)
        {
            RebuildSnapshot();
        }

        return change;
    }

    public bool SetExternalSkillActive(string skillId, bool isActive)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(skillId);

        return SkillListMutation.GetRequired(ExternalSkills, skillId, "External skill").SetActive(isActive);
    }

    public void LevelUpAllSkillsMaxLevel(
        Func<ExternalSkillInstance, int> externalSkillMaxLevelResolver,
        Func<InternalSkillInstance, int> internalSkillMaxLevelResolver)
    {
        ArgumentNullException.ThrowIfNull(externalSkillMaxLevelResolver);
        ArgumentNullException.ThrowIfNull(internalSkillMaxLevelResolver);

        foreach (var externalSkill in ExternalSkills)
        {
            externalSkill.Level = Math.Max(externalSkill.Level, externalSkillMaxLevelResolver(externalSkill));
        }
        foreach (var internalSkill in InternalSkills)
        {
            internalSkill.Level = Math.Max(internalSkill.Level, internalSkillMaxLevelResolver(internalSkill));
        }
        RebuildSnapshot();
    }

    public bool RemoveExternalSkill(string skillId)
    {
        return SkillListMutation.Remove(ExternalSkills, skillId, beforeRemove: null, onChanged: RebuildSnapshot);
    }

    public void SetInternalSkillState(InternalSkillDefinition definition, int level, int exp)
    {
        ArgumentNullException.ThrowIfNull(definition);
        var skill = SkillListMutation.Find(InternalSkills, definition.Id);
        if (skill is null)
        {
            InternalSkills.Add(CreateInternalSkill(definition, level, exp));
        }
        else
        {
            skill.SetState(level, exp);
        }

        RebuildSnapshot();
    }

    public SkillLevelChange<InternalSkillInstance> UpgradeInternalSkillLevel(
        InternalSkillDefinition definition,
        int levels,
        int maxLevel)
    {
        ArgumentNullException.ThrowIfNull(definition);
        var skill = SkillListMutation.Find(InternalSkills, definition.Id);
        if (skill is null)
        {
            ArgumentOutOfRangeException.ThrowIfNegativeOrZero(levels);
            var createdLevel = Math.Min(levels, maxLevel);
            skill = CreateInternalSkill(definition, createdLevel, 0);
            InternalSkills.Add(skill);
            RebuildSnapshot();
            return new SkillLevelChange<InternalSkillInstance>(skill, 0, createdLevel, true);
        }

        var change = skill.UpgradeLevel(levels, maxLevel);
        if (change.NewLevel != change.OldLevel)
        {
            RebuildSnapshot();
        }

        return change;
    }

    public bool EquipInternalSkill(string? internalSkillId)
    {
        if (internalSkillId is null)
        {
            if (EquippedInternalSkillId is null)
            {
                return false;
            }

            EquippedInternalSkillId = null;
            RebuildSnapshot();
            return true;
        }

        if (SkillListMutation.Find(InternalSkills, internalSkillId) is null)
        {
            throw new InvalidOperationException($"Internal skill '{internalSkillId}' is not unlocked.");
        }

        if (string.Equals(EquippedInternalSkillId, internalSkillId, StringComparison.Ordinal))
        {
            return false;
        }

        EquippedInternalSkillId = internalSkillId;
        RebuildSnapshot();
        return true;
    }

    public bool RemoveInternalSkill(string skillId)
    {
        return SkillListMutation.Remove(InternalSkills, skillId, removedSkill =>
        {
            if (string.Equals(EquippedInternalSkillId, removedSkill.Id, StringComparison.Ordinal))
            {
                EquippedInternalSkillId = null;
            }
        }, RebuildSnapshot);
    }

    public int? GetExternalSkillLevel(string externalSkillId) =>
        SkillListMutation.Find(ExternalSkills, externalSkillId)?.Level;

    public int? GetInternalSkillLevel(string internalSkillId) =>
        SkillListMutation.Find(InternalSkills, internalSkillId)?.Level;

    public IReadOnlyList<FormSkillInstance> GetFormSkills() =>
        ExternalSkills.SelectMany(skill => skill.GetFormSkills())
            .Concat(InternalSkills.SelectMany(skill =>
                skill.GetFormSkills()))
            .ToList();

    public bool HasTalent(string talentId) =>
        UnlockedTalents.Any(talent => string.Equals(talent.Id, talentId, StringComparison.Ordinal));

    public bool HasEffectiveTalent(string talentId) =>
        EffectiveTalents.Any(talent => string.Equals(talent.Id, talentId, StringComparison.Ordinal));

    public bool LearnTalent(TalentDefinition talent)
    {
        ArgumentNullException.ThrowIfNull(talent);
        if (HasTalent(talent.Id))
        {
            return false;
        }

        UnlockedTalents.Add(talent);
        RebuildSnapshot();
        return true;
    }

    public bool RemoveTalent(string talentId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(talentId);

        var index = UnlockedTalents.FindIndex(talent => string.Equals(talent.Id, talentId, StringComparison.Ordinal));
        if (index < 0)
        {
            return false;
        }

        UnlockedTalents.RemoveAt(index);
        RebuildSnapshot();
        return true;
    }

    public bool LearnSpecialSkill(SpecialSkillDefinition definition, bool isActive = true)
    {
        ArgumentNullException.ThrowIfNull(definition);
        if (SpecialSkills.Any(skill => string.Equals(skill.Definition.Id, definition.Id, StringComparison.Ordinal)))
        {
            return false;
        }

        SpecialSkills.Add(new SpecialSkillInstance(definition, this, isActive));
        return true;
    }

    public bool SetSpecialSkillActive(string skillId, bool isActive)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(skillId);

        return SkillListMutation.GetRequired(SpecialSkills, skillId, "Special skill").SetActive(isActive);
    }

    public bool RemoveSpecialSkill(string skillId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(skillId);

        return SkillListMutation.Remove(SpecialSkills, skillId, beforeRemove: null);
    }

    public void ApplyBattleCarryover(int hp, int mp, int rage)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(hp);
        ArgumentOutOfRangeException.ThrowIfNegative(mp);
        ArgumentOutOfRangeException.ThrowIfNegative(rage);

        CurrentHp = Math.Clamp(hp, 0, ResolveMaxHp());
        CurrentMp = Math.Clamp(mp, 0, ResolveMaxMp());
        CurrentRage = Math.Clamp(rage, 0, MaxBattleRage);
    }

    public void RestoreBattleResources()
    {
        CurrentHp = ResolveMaxHp();
        CurrentMp = ResolveMaxMp();
        CurrentRage = 0;
        ResetSkillCooldowns();
    }

    public void AddEquipmentInstance(EquipmentInstance equipmentInstance)
    {
        if (EquippedItems.Values.Any(instance => string.Equals(instance.Id, equipmentInstance.Id, StringComparison.Ordinal)))
        {
            throw new InvalidOperationException($"Equipment instance '{equipmentInstance.Id}' already exists.");
        }

        if (EquippedItems.ContainsKey(equipmentInstance.Definition.SlotType))
        {
            throw new InvalidOperationException($"Equipment slot '{equipmentInstance.Definition.SlotType}' is already occupied.");
        }

        EquippedItems[equipmentInstance.Definition.SlotType] = equipmentInstance;
    }

    public EquipmentInstance RemoveEquipment(EquipmentSlotType slotType)
    {
        if (!EquippedItems.Remove(slotType, out var equipment))
        {
            throw new InvalidOperationException($"Equipment slot '{slotType}' is empty.");
        }

        return equipment;
    }

    public EquipmentInstance? GetEquipment(EquipmentSlotType slotType) =>
        EquippedItems.GetValueOrDefault(slotType);

    public IReadOnlyList<ExternalSkillInstance> GetExternalSkills() => ExternalSkills;

    public IReadOnlyList<InternalSkillInstance> GetInternalSkills() => InternalSkills;

    public IReadOnlyList<SpecialSkillInstance> GetSpecialSkills() => SpecialSkills;


    public void RebuildSnapshot()
    {
        var resolvedAffixSet = AffixResolver.Resolve(CollectActiveAffixes(), UnlockedTalents);
        Snapshot = SnapshotBuilder.Build(resolvedAffixSet);
        ClampBattleResources();
    }

    private IReadOnlyList<AffixDefinition> CollectActiveAffixes()
    {
        var affixes = new List<AffixDefinition>();

        foreach (var equipment in EquippedItems.Values)
        {
            affixes.AddRange(AffixResolver.ResolveProviderAffixes(equipment, ProviderKind.Equipment));
        }

        // talent's affixes move to TalentResolver

        foreach (var skill in ExternalSkills)
        {
            affixes.AddRange(AffixResolver.ResolveSkillAffixes(
                skill.Definition.Affixes,
                skill.Level,
                false,
                ProviderKind.ExternalSkill));
        }

        foreach (var skill in InternalSkills)
        {
            affixes.AddRange(AffixResolver.ResolveSkillAffixes(
                skill.Definition.Affixes,
                skill.Level,
                skill.IsEquipped,
                ProviderKind.InternalSkill));
        }

        return affixes;
    }


    public IReadOnlyDictionary<StatType, int> GetBaseStats() =>
        Enum.GetValues<StatType>()
            .ToDictionary(stat => stat, GetBaseStat, EqualityComparer<StatType>.Default);

    public int GetBaseStat(StatType statType) => BaseStats.GetValueOrDefault(statType);
    public double GetStat(StatType statType) =>
        GetBucket(Snapshot.StatModifierBuckets, statType).Evaluate(GetBaseStat(statType));

    public double GetSkillBonusValue(string skillId, double baseValue = 0) =>
        GetBucket(Snapshot.SkillModifierBuckets, skillId).Evaluate(baseValue);

    public double GetWeaponBonusValue(WeaponType weaponType, double baseValue) =>
        GetBucket(Snapshot.WeaponModifierBuckets, weaponType).Evaluate(baseValue);

    public double GetLegendChanceValue(string skillId, double baseValue) =>
        GetBucket(Snapshot.LegendChanceModifierBuckets, skillId).Evaluate(baseValue);

    public IReadOnlyList<HookAffix> GetHooks(HookTiming timing) =>
        Snapshot.HooksByTiming.TryGetValue(timing, out var hooks) ? hooks : Array.Empty<HookAffix>();

    private static ModifierBucket GetBucket<TKey>(IReadOnlyDictionary<TKey, ModifierBucket> buckets, TKey key)
        where TKey : notnull =>
        buckets.TryGetValue(key, out var bucket) ? bucket : ModifierBucket.Empty;

    private void ResetSkillCooldowns()
    {
        foreach (var skill in ExternalSkills)
        {
            skill.ResetBattleCooldown();
        }

        foreach (var skill in InternalSkills)
        {
            skill.ResetBattleCooldown();
        }

        foreach (var skill in SpecialSkills)
        {
            skill.ResetBattleCooldown();
        }
    }

    public void ClampBattleResources()
    {
        if (CurrentHp is not null)
        {
            CurrentHp = Math.Clamp(CurrentHp.Value, 0, ResolveMaxHp());
        }

        if (CurrentMp is not null)
        {
            CurrentMp = Math.Clamp(CurrentMp.Value, 0, ResolveMaxMp());
        }

        CurrentRage = Math.Clamp(CurrentRage, 0, MaxBattleRage);
    }

    private int ResolveMaxHp() => Math.Max(1, (int)Math.Round(GetStat(StatType.MaxHp)));

    private int ResolveMaxMp() => Math.Max(0, (int)Math.Round(GetStat(StatType.MaxMp)));

    private ExternalSkillInstance CreateExternalSkill(ExternalSkillDefinition definition, int level, int exp, bool canUseInBattle) =>
        new(definition, this, canUseInBattle)
        {
            Level = level,
            Exp = exp,
        };

    private InternalSkillInstance CreateInternalSkill(InternalSkillDefinition definition, int level, int exp) =>
        new(definition, this)
        {
            Level = level,
            Exp = exp,
        };
}
