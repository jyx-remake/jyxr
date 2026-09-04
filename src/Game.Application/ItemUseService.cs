using Game.Core.Definitions;
using Game.Core.Model;
using Game.Core.Model.Character;
using Game.Application.Formatters;

namespace Game.Application;

public sealed class ItemUseService
{
    public const string ItemTargetCharacterIdVariable = "item_target";

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

        var effectAnalysis = support.Kind == ItemUseKind.Effects
            ? AnalyzeEffectTargets(support.Effects, character)
            : EffectTargetAnalysis.AllApplicable(support.Effects);
        if (effectAnalysis.Failure is not null)
        {
            return ItemUseTargetCandidate.Disabled(character.Id, effectAnalysis.Failure);
        }

        return ItemUseTargetCandidate.Enabled(character.Id, effectAnalysis.SkippedEffects);
    }

    public async Task<ItemUseResult> UseAsync(
        InventoryEntry entry,
        string targetCharacterId,
        CancellationToken cancellationToken = default) =>
        await UseAsyncCore(entry, targetCharacterId, false, cancellationToken);

    public async Task<ItemUseResult> UseAsync(
        InventoryEntry entry,
        string targetCharacterId,
        bool acceptPartialEffects,
        CancellationToken cancellationToken = default) =>
        await UseAsyncCore(entry, targetCharacterId, acceptPartialEffects, cancellationToken);

    private async Task<ItemUseResult> UseAsyncCore(
        InventoryEntry entry,
        string targetCharacterId,
        bool acceptPartialEffects,
        CancellationToken cancellationToken)
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
        if (candidate.RequiresConfirmation && !acceptPartialEffects)
        {
            return ItemUseResult.Failed("该物品有部分效果无法生效，请确认后使用。");
        }

        var support = ResolveSupport(entry);
        if (!support.IsSupported)
        {
            return ItemUseResult.Failed(support.Message);
        }

        if (support.Effects is [RunStoryItemUseEffectDefinition runStory])
        {
            // Legacy story#id#true payloads keep the item (the phone-style
            // reusable props); without the flag the use consumes it first.
            if (!runStory.KeepItem)
            {
                CommitSuccessfulUse(entry);
            }

            var storyService = _session.StoryService;
            var context = new StoryExecutionContext(new Dictionary<string, ExpressionValue>(StringComparer.Ordinal)
            {
                [ItemTargetCharacterIdVariable] = ExpressionValue.FromString(target.Id),
            });
            await storyService.ExecuteAsync(runStory.StoryId, context, cancellationToken);
            return ItemUseResult.Succeeded($"【{target.Name}】使用【{entry.Definition.Name}】");
        }

        var applicableEffects = support.Kind == ItemUseKind.Effects
            ? AnalyzeEffectTargets(support.Effects, target).ApplicableEffects
            : support.Effects;
        var result = support.Kind == ItemUseKind.Equipment
            ? UseEquipment(entry, target)
            : ApplyUseEffects(entry.Definition, target, applicableEffects);

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
                case GrantTitleItemUseEffectDefinition title:
                    _session.CharacterService.LearnTitle(target, title.TitleId);
                    break;
                case SetPortraitItemUseEffectDefinition setPortrait:
                    target.Portrait = setPortrait.PictureId;
                    resultDetails.Add($"{target.Name}改变了头像");
                    break;
                case RandomItemItemUseEffectDefinition randomItem:
                {
                    var selected = randomItem.Items[_session.RandomService.Next(0, randomItem.Items.Count)];
                    _session.InventoryService.AddItem(selected.ItemId, selected.Quantity);
                    resultDetails.Add($"获得了【{selected.ItemId}】×{selected.Quantity}");
                    break;
                }
                case AddStatsItemUseEffectDefinition addStats:
                    foreach (var (statType, value) in addStats.Values)
                    {
                        target.AddBaseStat(statType, value);
                        resourceStatsChanged |= CharacterResourceLimitPolicy.IsBaseResourceStat(statType);
                    }
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
        if (item.UseEffects.Count == 0)
        {
            // Quest items without effects stay inert; quest items WITH effects
            // are legacy-usable props (宝箱/称号/头像/剧情道具), so usability is
            // decided by the effects themselves instead of the legacy type.
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
            GrantTitleItemUseEffectDefinition or
            SetPortraitItemUseEffectDefinition or
            RandomItemItemUseEffectDefinition or
            AddStatsItemUseEffectDefinition or
            SetGenderItemUseEffectDefinition or
            ReduceMaxResourceRatioItemUseEffectDefinition or
            RunStoryItemUseEffectDefinition;

    /// <summary>
    /// Resolves the character an item is pinned to, letting the host skip
    /// target selection. Two cases auto-resolve: a <c>role_key</c> requirement
    /// names the only allowed user (legacy <c>require rolekey</c>), and an
    /// inventory-level effect (<c>random_item</c>) grants to the shared
    /// inventory instead of a character. Returns null when selection should
    /// proceed normally.
    /// </summary>
    public string? ResolveAutoTargetCharacterId(InventoryEntry entry)
    {
        ArgumentNullException.ThrowIfNull(entry);
        var item = entry.Definition;

        if (item.UseEffects is [RandomItemItemUseEffectDefinition])
        {
            return State.Party.Members.FirstOrDefault()?.Id;
        }

        var roleKey = item.Requirements
            .OfType<RoleKeyItemRequirementDefinition>()
            .FirstOrDefault();
        if (roleKey is null)
        {
            return null;
        }

        return State.Party.Members
            .FirstOrDefault(member => member.Id == roleKey.CharacterId ||
                string.Equals(member.Definition.Name, roleKey.CharacterId, StringComparison.Ordinal))
            ?.Id;
    }

    private string? ValidateRequirements(ItemDefinition item, CharacterInstance target)
    {
        foreach (var requirement in item.Requirements)
        {
            switch (requirement)
            {
                case StatItemRequirementDefinition stat:
                    var statValue = ResolveRequirementStatValue(target, stat.StatId, stat.Source);
                    if ((!stat.Negated && statValue < stat.Value) ||
                        (stat.Negated && statValue >= stat.Value))
                    {
                        return stat.Negated
                            ? $"{FormatStatName(stat.StatId)}需低于{stat.Value}"
                            : $"需要{FormatStatName(stat.StatId)}达到{stat.Value}";
                    }
                    break;
                case LevelItemRequirementDefinition level:
                    if (target.Level < level.Value)
                    {
                        return $"等级需达到{level.Value}";
                    }
                    break;
                case TalentItemRequirementDefinition talent:
                    if (!target.HasEffectiveTalent(talent.TalentId))
                    {
                        return $"需要天赋「{talent.TalentId}」";
                    }
                    break;
                case NotTalentItemRequirementDefinition notTalent:
                    if (target.HasEffectiveTalent(notTalent.TalentId))
                    {
                        return $"不能拥有天赋「{notTalent.TalentId}」";
                    }
                    break;
                case RoleKeyItemRequirementDefinition roleKey:
                    if (!string.Equals(target.Id, roleKey.CharacterId, StringComparison.Ordinal) &&
                        !string.Equals(target.Definition.Name, roleKey.CharacterId, StringComparison.Ordinal))
                    {
                        return $"仅限角色「{roleKey.CharacterId}」使用";
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

    private double ResolveRequirementStatValue(
        CharacterInstance target,
        StatType statType,
        ItemRequirementStatSource? source = null) =>
        (source ?? Config.ItemRequirementStatSource) switch
        {
            ItemRequirementStatSource.Final => target.GetStat(statType),
            ItemRequirementStatSource.Base => target.GetBaseStat(statType),
            _ => throw new InvalidOperationException(
                $"Unsupported item requirement stat source: {Config.ItemRequirementStatSource}"),
        };

    private EffectTargetAnalysis AnalyzeEffectTargets(
        IReadOnlyList<ItemUseEffectDefinition> effects,
        CharacterInstance target)
    {
        var externalSkillIds = new HashSet<string>(StringComparer.Ordinal);
        var internalSkillIds = new HashSet<string>(StringComparer.Ordinal);
        var newExternalSkillCount = 0;
        var newInternalSkillCount = 0;
        var specialSkillIds = new HashSet<string>(StringComparer.Ordinal);
        var talentIds = new HashSet<string>(StringComparer.Ordinal);
        var titleIds = new HashSet<string>(StringComparer.Ordinal);
        var simulatedBaseStats = new Dictionary<StatType, long>();
        var applicableEffects = new List<ItemUseEffectDefinition>();
        var skippedEffects = new List<ItemUseEffectDefinition>();
        var requiredTalentPoints = 0;

        foreach (var effect in effects)
        {
            switch (effect)
            {
                case GrantExternalSkillItemUseEffectDefinition externalSkill:
                {
                    if (!externalSkillIds.Add(externalSkill.SkillId))
                    {
                        return EffectTargetAnalysis.Failed("物品包含重复的外功学习效果");
                    }

                    var currentLevel = target.GetExternalSkillLevel(externalSkill.SkillId);
                    if (currentLevel is not null &&
                        currentLevel.Value >= ResolveExternalSkillBookMaxLevel(externalSkill))
                    {
                        skippedEffects.Add(effect);
                        break;
                    }

                    if (currentLevel is null)
                    {
                        newExternalSkillCount++;
                    }
                    applicableEffects.Add(effect);
                    break;
                }
                case GrantInternalSkillItemUseEffectDefinition internalSkill:
                {
                    if (!internalSkillIds.Add(internalSkill.SkillId))
                    {
                        return EffectTargetAnalysis.Failed("物品包含重复的内功学习效果");
                    }

                    var currentLevel = target.GetInternalSkillLevel(internalSkill.SkillId);
                    if (currentLevel is not null &&
                        currentLevel.Value >= ResolveInternalSkillBookMaxLevel(internalSkill))
                    {
                        skippedEffects.Add(effect);
                        break;
                    }

                    if (currentLevel is null)
                    {
                        newInternalSkillCount++;
                    }
                    applicableEffects.Add(effect);
                    break;
                }
                case GrantSpecialSkillItemUseEffectDefinition specialSkill:
                    if (!specialSkillIds.Add(specialSkill.SkillId))
                    {
                        return EffectTargetAnalysis.Failed("物品包含重复的特技学习效果");
                    }
                    if (target.GetSpecialSkills().Any(skill =>
                            string.Equals(skill.Definition.Id, specialSkill.SkillId, StringComparison.Ordinal)))
                    {
                        skippedEffects.Add(effect);
                        break;
                    }
                    applicableEffects.Add(effect);
                    break;
                case GrantTalentItemUseEffectDefinition talent:
                    if (!talentIds.Add(talent.TalentId))
                    {
                        return EffectTargetAnalysis.Failed("物品包含重复的天赋学习效果");
                    }
                    if (target.HasTalent(talent.TalentId))
                    {
                        skippedEffects.Add(effect);
                        break;
                    }
                    requiredTalentPoints = checked(
                        requiredTalentPoints + _session.ContentRepository.GetTalent(talent.TalentId).Point);
                    applicableEffects.Add(effect);
                    break;
                case GrantTitleItemUseEffectDefinition title:
                    if (!titleIds.Add(title.TitleId))
                    {
                        return EffectTargetAnalysis.Failed("物品包含重复的称号授予效果");
                    }
                    if (target.Titles.Any(owned =>
                            string.Equals(owned.Id, title.TitleId, StringComparison.Ordinal)))
                    {
                        skippedEffects.Add(effect);
                        break;
                    }
                    applicableEffects.Add(effect);
                    break;
                case AddStatsItemUseEffectDefinition addStats:
                    foreach (var (statType, value) in addStats.Values)
                    {
                        var currentValue = simulatedBaseStats.TryGetValue(statType, out var simulatedValue)
                            ? simulatedValue
                            : target.GetBaseStat(statType);
                        var result = currentValue + value;
                        if (result < 0)
                        {
                            return EffectTargetAnalysis.Failed($"{FormatStatName(statType)}不能低于0");
                        }
                        if (result > int.MaxValue)
                        {
                            return EffectTargetAnalysis.Failed($"{FormatStatName(statType)}超出有效范围");
                        }

                        simulatedBaseStats[statType] = result;
                    }
                    applicableEffects.Add(effect);
                    break;
                case ReduceMaxResourceRatioItemUseEffectDefinition reduction:
                {
                    var currentValue = simulatedBaseStats.TryGetValue(reduction.StatId, out var simulatedValue)
                        ? simulatedValue
                        : target.GetBaseStat(reduction.StatId);
                    simulatedBaseStats[reduction.StatId] = currentValue - (long)(currentValue * reduction.Ratio);
                    applicableEffects.Add(effect);
                    break;
                }
                default:
                    applicableEffects.Add(effect);
                    break;
            }
        }

        if (applicableEffects.Count == 0)
        {
            return EffectTargetAnalysis.Failed("该物品已无法带来新的效果");
        }
        if (newExternalSkillCount > 0 &&
            target.GetExternalSkills().Count + newExternalSkillCount > Config.MaxExternalSkillCount)
        {
            return EffectTargetAnalysis.Failed("外功数量已达上限");
        }
        if (newInternalSkillCount > 0 &&
            target.GetInternalSkills().Count + newInternalSkillCount > Config.MaxInternalSkillCount)
        {
            return EffectTargetAnalysis.Failed("内功数量已达上限");
        }
        if (requiredTalentPoints > 0)
        {
            var spentPoints = _session.CharacterService.GetSpentTalentPoints(target);
            var capacity = _session.CharacterService.GetTalentPointCapacity(target);
            if (spentPoints + requiredTalentPoints > capacity)
            {
                return EffectTargetAnalysis.Failed($"武学常识不足，需要{requiredTalentPoints}");
            }
        }

        return EffectTargetAnalysis.Succeeded(applicableEffects, skippedEffects);
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

    private sealed record EffectTargetAnalysis(
        string? Failure,
        IReadOnlyList<ItemUseEffectDefinition> ApplicableEffects,
        IReadOnlyList<ItemUseEffectDefinition> SkippedEffects)
    {
        public static EffectTargetAnalysis AllApplicable(IReadOnlyList<ItemUseEffectDefinition> effects) =>
            new(null, effects, []);

        public static EffectTargetAnalysis Succeeded(
            IReadOnlyList<ItemUseEffectDefinition> applicableEffects,
            IReadOnlyList<ItemUseEffectDefinition> skippedEffects) =>
            new(null, applicableEffects, skippedEffects);

        public static EffectTargetAnalysis Failed(string failure) =>
            new(failure, [], []);
    }
}

public sealed record ItemUseAnalysis(
    bool IsSupported,
    string Message,
    IReadOnlyList<ItemUseTargetCandidate> Targets);

public sealed record ItemUseTargetCandidate(
    string CharacterId,
    bool CanUse,
    string Reason,
    IReadOnlyList<ItemUseEffectDefinition> SkippedEffects)
{
    public bool RequiresConfirmation => CanUse && SkippedEffects.Count > 0;

    public static ItemUseTargetCandidate Enabled(
        string characterId,
        IReadOnlyList<ItemUseEffectDefinition>? skippedEffects = null) =>
        new(characterId, true, string.Empty, skippedEffects ?? []);

    public static ItemUseTargetCandidate Disabled(string characterId, string reason) =>
        new(characterId, false, reason, []);
}

public sealed record ItemUseResult(
    bool Success,
    string Message)
{
    public static ItemUseResult Succeeded(string message = "") => new(true, message);

    public static ItemUseResult Failed(string message) => new(false, message);
}
