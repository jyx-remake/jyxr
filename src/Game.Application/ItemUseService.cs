using Game.Core.Definitions;
using Game.Core.Model;
using Game.Core.Model.Character;
using Game.Application.Formatters;

namespace Game.Application;

public sealed class ItemUseService
{
    private readonly GameSession _session;

    public ItemUseService(GameSession session)
    {
        ArgumentNullException.ThrowIfNull(session);
        _session = session;
    }

    private GameState State => _session.State;
    private GameConfig Config => _session.Config;
    private SkillMaxLevelPolicy SkillMaxLevelPolicy => _session.SkillMaxLevelPolicy;
    private CharacterResourceLimitPolicy CharacterResourceLimitPolicy => _session.CharacterResourceLimitPolicy;

    public ItemUseAnalysis Analyze(InventoryEntry entry)
    {
        ArgumentNullException.ThrowIfNull(entry);

        var support = ResolveSupport(entry);
        if (!support.IsSupported)
        {
            return new ItemUseAnalysis(false, support.Message, []);
        }

        var targets = State.Party.Members
            .Select(character => AnalyzeTarget(entry, character))
            .ToList();
        return new ItemUseAnalysis(true, support.Message, targets);
    }

    public ItemUseTargetCandidate AnalyzeTarget(InventoryEntry entry, CharacterInstance character)
    {
        ArgumentNullException.ThrowIfNull(entry);
        ArgumentNullException.ThrowIfNull(character);

        var support = ResolveSupport(entry);
        if (!support.IsSupported)
        {
            return ItemUseTargetCandidate.Disabled(character.Id, support.Message);
        }

        var requirementFailure = ValidateRequirements(entry.Definition, character);
        if (requirementFailure is not null)
        {
            return ItemUseTargetCandidate.Disabled(character.Id, requirementFailure);
        }

        var specificFailure = support.Kind == ItemUseKind.Effects
            ? ValidateEffectTargets(support.Effects, character)
            : null;
        if (specificFailure is not null)
        {
            return ItemUseTargetCandidate.Disabled(character.Id, specificFailure);
        }

        return ItemUseTargetCandidate.Enabled(character.Id);
    }

    public ItemUseResult Use(InventoryEntry entry, string targetCharacterId)
    {
        ArgumentNullException.ThrowIfNull(entry);
        ArgumentException.ThrowIfNullOrWhiteSpace(targetCharacterId);

        if (!State.Inventory.Entries.Any(candidate => ReferenceEquals(candidate, entry)))
        {
            return ItemUseResult.Failed("物品已不在背包中。");
        }

        var target = State.Party.GetMember(targetCharacterId);
        var candidate = AnalyzeTarget(entry, target);
        if (!candidate.CanUse)
        {
            return ItemUseResult.Failed(candidate.Reason);
        }

        var support = ResolveSupport(entry);
        if (!support.IsSupported)
        {
            return ItemUseResult.Failed(support.Message);
        }

        var result = support.Kind == ItemUseKind.Equipment
            ? UseEquipment(entry, target)
            : ApplyUseEffects(entry.Definition, target, support.Effects);

        if (result.Success)
        {
            CommitSuccessfulUse(entry);
        }

        return result;
    }

    public void CommitSuccessfulUse(InventoryEntry entry)
    {
        ArgumentNullException.ThrowIfNull(entry);
        if (entry.Definition.ConsumeOnUse)
        {
            _session.InventoryService.RemoveItem(entry.Definition);
        }
    }

    private ItemUseResult UseEquipment(InventoryEntry entry, CharacterInstance target)
    {
        switch (entry)
        {
            case StackInventoryEntry { Item: EquipmentDefinition equipmentDefinition }:
                _session.InventoryService.EquipFromStack(target, equipmentDefinition);
                return ItemUseResult.Succeeded($"【{target.Name}】装备【{equipmentDefinition.Name}】");

            case EquipmentInstanceInventoryEntry equipmentEntry:
                _session.InventoryService.EquipInstance(target, equipmentEntry.Equipment.Id);
                return ItemUseResult.Succeeded($"【{target.Name}】装备【{equipmentEntry.Equipment.Definition.Name}】");

            default:
                return ItemUseResult.Failed("该装备条目无效。");
        }
    }

    private ItemUseResult ApplyUseEffects(
        ItemDefinition item,
        CharacterInstance target,
        IReadOnlyList<ItemUseEffectDefinition> effects)
    {
        var resourceStatsChanged = false;
        var resultDetails = new List<string>();
        foreach (var effect in effects)
        {
            switch (effect)
            {
                case GrantExternalSkillItemUseEffectDefinition externalSkill:
                    target.SetExternalSkillState(
                        _session.ContentRepository.GetExternalSkill(externalSkill.SkillId),
                        ResolveExternalSkillBookMaxLevel(externalSkill),
                        0,
                        true);
                    break;
                case GrantInternalSkillItemUseEffectDefinition internalSkill:
                    target.SetInternalSkillState(
                        _session.ContentRepository.GetInternalSkill(internalSkill.SkillId),
                        ResolveInternalSkillBookMaxLevel(internalSkill),
                        0);
                    break;
                case GrantSpecialSkillItemUseEffectDefinition specialSkill:
                    target.LearnSpecialSkill(_session.ContentRepository.GetSpecialSkill(specialSkill.SkillId));
                    break;
                case GrantTalentItemUseEffectDefinition talent:
                    target.LearnTalent(_session.ContentRepository.GetTalent(talent.TalentId));
                    break;
                case AddMaxHpItemUseEffectDefinition maxHp:
                    target.AddBaseStat(StatType.MaxHp, maxHp.Value);
                    resourceStatsChanged = true;
                    break;
                case AddMaxMpItemUseEffectDefinition maxMp:
                    target.AddBaseStat(StatType.MaxMp, maxMp.Value);
                    resourceStatsChanged = true;
                    break;
                case SetGenderItemUseEffectDefinition setGender:
                    target.SetGender(setGender.Gender);
                    resultDetails.Add($"{target.Name}已经变成了{FormatterTextCn.GetGenderNameCn(setGender.Gender)}");
                    break;
                case ReduceMaxResourceRatioItemUseEffectDefinition reduction:
                {
                    var loss = target.ReduceBaseResourceStat(reduction.StatId, reduction.Ratio);
                    resultDetails.Add($"{FormatStatName(reduction.StatId)} -{loss}");
                    resourceStatsChanged = true;
                    break;
                }
                default:
                    throw new InvalidOperationException(
                        $"Unsupported out-of-battle item effect: {effect.GetType().Name}");
            }
        }

        if (resourceStatsChanged)
        {
            CharacterResourceLimitPolicy.ClampBaseResourceStats(target);
            target.ClampBattleResources();
        }

        _session.Events.Publish(new CharacterChangedEvent(target.Id));
        var message = $"【{target.Name}】使用【{item.Name}】";
        return ItemUseResult.Succeeded(resultDetails.Count == 0
            ? message
            : $"{message}：{string.Join("，", resultDetails)}");
    }

    private static ItemUseSupport ResolveSupport(InventoryEntry entry)
    {
        var item = entry.Definition;
        if (item is EquipmentDefinition)
        {
            return ItemUseSupport.Supported(ItemUseKind.Equipment, "请选择装备目标。", item.UseEffects);
        }

        if (item.Type == ItemType.Consumable)
        {
            return ItemUseSupport.Unsupported("消耗品只能在战斗中使用。");
        }
        if (item.Type == ItemType.QuestItem)
        {
            return ItemUseSupport.Unsupported("剧情物品暂不可主动使用。");
        }
        if (item.UseEffects.Count == 0)
        {
            return ItemUseSupport.Unsupported("该物品没有可用效果。");
        }
        if (!item.UseEffects.All(IsSupportedOutOfBattleEffect))
        {
            return ItemUseSupport.Unsupported("该物品包含尚未接入的场外效果。");
        }

        return ItemUseSupport.Supported(ItemUseKind.Effects, "请选择使用目标。", item.UseEffects);
    }

    private static bool IsSupportedOutOfBattleEffect(ItemUseEffectDefinition effect) =>
        effect is GrantExternalSkillItemUseEffectDefinition or
            GrantInternalSkillItemUseEffectDefinition or
            GrantSpecialSkillItemUseEffectDefinition or
            GrantTalentItemUseEffectDefinition or
            AddMaxHpItemUseEffectDefinition or
            AddMaxMpItemUseEffectDefinition or
            SetGenderItemUseEffectDefinition or
            ReduceMaxResourceRatioItemUseEffectDefinition;

    private string? ValidateRequirements(ItemDefinition item, CharacterInstance target)
    {
        foreach (var requirement in item.Requirements)
        {
            switch (requirement)
            {
                case StatItemRequirementDefinition stat:
                    if (ResolveRequirementStatValue(target, stat.StatId) < stat.Value)
                    {
                        return $"需要{FormatStatName(stat.StatId)}达到{stat.Value}";
                    }
                    break;
                case TalentItemRequirementDefinition talent:
                    if (!target.HasEffectiveTalent(talent.TalentId))
                    {
                        return $"需要天赋「{talent.TalentId}」";
                    }
                    break;
                case GenderItemRequirementDefinition gender:
                    if (!gender.Genders.Contains(target.Gender))
                    {
                        return $"仅限{string.Join("、", gender.Genders.Select(FormatterTextCn.GetGenderNameCn))}使用";
                    }
                    break;
            }
        }

        return null;
    }

    private double ResolveRequirementStatValue(CharacterInstance target, StatType statType) =>
        Config.ItemRequirementStatSource switch
        {
            ItemRequirementStatSource.Final => target.GetStat(statType),
            ItemRequirementStatSource.Base => target.GetBaseStat(statType),
            _ => throw new InvalidOperationException(
                $"Unsupported item requirement stat source: {Config.ItemRequirementStatSource}"),
        };

    private string? ValidateEffectTargets(
        IReadOnlyList<ItemUseEffectDefinition> effects,
        CharacterInstance target)
    {
        var newExternalSkillIds = new HashSet<string>(StringComparer.Ordinal);
        var newInternalSkillIds = new HashSet<string>(StringComparer.Ordinal);
        var specialSkillIds = new HashSet<string>(StringComparer.Ordinal);
        var talentIds = new HashSet<string>(StringComparer.Ordinal);
        var requiredTalentPoints = 0;

        foreach (var effect in effects)
        {
            switch (effect)
            {
                case GrantExternalSkillItemUseEffectDefinition externalSkill:
                {
                    var currentLevel = target.GetExternalSkillLevel(externalSkill.SkillId);
                    if (currentLevel is not null &&
                        currentLevel.Value >= ResolveExternalSkillBookMaxLevel(externalSkill))
                    {
                        return "该外功已达上限";
                    }

                    if (currentLevel is null && !newExternalSkillIds.Add(externalSkill.SkillId))
                    {
                        return "物品包含重复的外功学习效果";
                    }
                    break;
                }
                case GrantInternalSkillItemUseEffectDefinition internalSkill:
                {
                    var currentLevel = target.GetInternalSkillLevel(internalSkill.SkillId);
                    if (currentLevel is not null &&
                        currentLevel.Value >= ResolveInternalSkillBookMaxLevel(internalSkill))
                    {
                        return "该内功已达上限";
                    }

                    if (currentLevel is null && !newInternalSkillIds.Add(internalSkill.SkillId))
                    {
                        return "物品包含重复的内功学习效果";
                    }
                    break;
                }
                case GrantSpecialSkillItemUseEffectDefinition specialSkill:
                    if (!specialSkillIds.Add(specialSkill.SkillId))
                    {
                        return "物品包含重复的特技学习效果";
                    }
                    if (target.GetSpecialSkills().Any(skill =>
                            string.Equals(skill.Definition.Id, specialSkill.SkillId, StringComparison.Ordinal)))
                    {
                        return "已领悟该绝技";
                    }
                    break;
                case GrantTalentItemUseEffectDefinition talent:
                    if (!talentIds.Add(talent.TalentId))
                    {
                        return "物品包含重复的天赋学习效果";
                    }
                    if (target.HasTalent(talent.TalentId))
                    {
                        return "已习得该天赋";
                    }
                    requiredTalentPoints = checked(
                        requiredTalentPoints + _session.ContentRepository.GetTalent(talent.TalentId).Point);
                    break;
            }
        }

        if (target.GetExternalSkills().Count + newExternalSkillIds.Count > Config.MaxExternalSkillCount)
        {
            return "外功数量已达上限";
        }
        if (target.GetInternalSkills().Count + newInternalSkillIds.Count > Config.MaxInternalSkillCount)
        {
            return "内功数量已达上限";
        }
        if (requiredTalentPoints > 0)
        {
            var spentPoints = _session.CharacterService.GetSpentTalentPoints(target);
            var capacity = _session.CharacterService.GetTalentPointCapacity(target);
            if (spentPoints + requiredTalentPoints > capacity)
            {
                return $"武学常识不足，需要{requiredTalentPoints}";
            }
        }

        return null;
    }

    private int ResolveExternalSkillBookMaxLevel(GrantExternalSkillItemUseEffectDefinition effect) =>
        ResolveSkillBookMaxLevel(effect.Level, SkillMaxLevelPolicy.GetExternalSkillMaxLevel(effect.SkillId));

    private int ResolveInternalSkillBookMaxLevel(GrantInternalSkillItemUseEffectDefinition effect) =>
        ResolveSkillBookMaxLevel(effect.Level, SkillMaxLevelPolicy.GetInternalSkillMaxLevel(effect.SkillId));

    private int ResolveSkillBookMaxLevel(int? bookLevel, int currentMaxLevel)
    {
        if (Config.IgnoreSkillBookLevelLimit || bookLevel is null)
        {
            return currentMaxLevel;
        }

        return Math.Min(bookLevel.Value, currentMaxLevel);
    }

    private static string FormatStatName(StatType statType) => StatCatalog.GetDisplayNameCn(statType);

    private enum ItemUseKind
    {
        Equipment,
        Effects,
    }

    private sealed record ItemUseSupport(
        bool IsSupported,
        ItemUseKind Kind,
        string Message,
        IReadOnlyList<ItemUseEffectDefinition> Effects)
    {
        public static ItemUseSupport Supported(
            ItemUseKind kind,
            string message,
            IReadOnlyList<ItemUseEffectDefinition> effects) =>
            new(true, kind, message, effects);

        public static ItemUseSupport Unsupported(string message) =>
            new(false, default, message, []);
    }
}

public sealed record ItemUseAnalysis(
    bool IsSupported,
    string Message,
    IReadOnlyList<ItemUseTargetCandidate> Targets);

public sealed record ItemUseTargetCandidate(
    string CharacterId,
    bool CanUse,
    string Reason)
{
    public static ItemUseTargetCandidate Enabled(string characterId) =>
        new(characterId, true, string.Empty);

    public static ItemUseTargetCandidate Disabled(string characterId, string reason) =>
        new(characterId, false, reason);
}

public sealed record ItemUseResult(
    bool Success,
    string Message)
{
    public static ItemUseResult Succeeded(string message = "") => new(true, message);

    public static ItemUseResult Failed(string message) => new(false, message);
}
