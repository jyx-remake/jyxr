using Game.Core.Definitions;
using Game.Core.Model;
using Game.Core.Model.Character;

namespace Game.Application;

public sealed class ItemUseService
{
    private const int DefaultLearnedSkillLevel = 20;

    private readonly GameSession _session;

    public ItemUseService(GameSession session)
    {
        ArgumentNullException.ThrowIfNull(session);
        _session = session;
    }

    private GameState State => _session.State;

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

        var specificFailure = ValidateSpecificTarget(support.Kind, support.Effects, entry, character);
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

        return support.Kind switch
        {
            ItemUseKind.Equipment => UseEquipment(entry, target),
            ItemUseKind.SkillBook => UseSkillBook(entry, target, support.Effects),
            ItemUseKind.SpecialSkillBook => UseSpecialSkillBook(entry, target, support.Effects),
            ItemUseKind.TalentBook => UseTalentBook(entry, target, support.Effects),
            ItemUseKind.Booster => UseBooster(entry, target, support.Effects),
            _ => ItemUseResult.Failed("该物品暂不可使用。"),
        };
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

    private ItemUseResult UseSkillBook(
        InventoryEntry entry,
        CharacterInstance target,
        IReadOnlyList<ItemUseEffectDefinition> effects)
    {
        foreach (var effect in effects)
        {
            switch (effect)
            {
                case GrantExternalSkillItemUseEffectDefinition externalSkill:
                {
                    // var level = ResolveTargetSkillLevel(
                    //     target.GetExternalSkillLevel(externalSkill.SkillId),
                    //     externalSkill.Level);
                    const int level = 20;
                    _session.CharacterService.LearnExternalSkill(
                        target,
                        externalSkill.SkillId,
                        level);
                    break;
                }
                case GrantInternalSkillItemUseEffectDefinition internalSkill:
                {
                    // var level = ResolveTargetSkillLevel(
                    //     target.GetInternalSkillLevel(internalSkill.SkillId),
                    //     internalSkill.Level);
                    const int level = 20;
                    _session.CharacterService.LearnInternalSkill(
                        target,
                        internalSkill.SkillId,
                        level);
                    break;
                }
            }
        }

        return ItemUseResult.Succeeded("");
    }

    private ItemUseResult UseSpecialSkillBook(
        InventoryEntry entry,
        CharacterInstance target,
        IReadOnlyList<ItemUseEffectDefinition> effects)
    {
        foreach (var effect in effects.OfType<GrantSpecialSkillItemUseEffectDefinition>())
        {
            _session.CharacterService.LearnSpecialSkill(target, effect.SkillId);
        }

        return ItemUseResult.Succeeded("");
    }

    private ItemUseResult UseTalentBook(
        InventoryEntry entry,
        CharacterInstance target,
        IReadOnlyList<ItemUseEffectDefinition> effects)
    {
        foreach (var effect in effects.OfType<GrantTalentItemUseEffectDefinition>())
        {
            _session.CharacterService.LearnTalent(target, effect.TalentId);
        }

        _session.InventoryService.RemoveItem(entry.Definition);
        return ItemUseResult.Succeeded("");
    }

    private ItemUseResult UseBooster(
        InventoryEntry entry,
        CharacterInstance target,
        IReadOnlyList<ItemUseEffectDefinition> effects)
    {
        foreach (var effect in effects)
        {
            switch (effect)
            {
                case AddMaxHpItemUseEffectDefinition maxHp:
                    target.AddBaseStat(StatType.MaxHp, maxHp.Value);
                    break;
                case AddMaxMpItemUseEffectDefinition maxMp:
                    target.AddBaseStat(StatType.MaxMp, maxMp.Value);
                    break;
            }
        }

        target.RebuildSnapshot();
        _session.InventoryService.RemoveItem(entry.Definition);
        _session.Events.Publish(new CharacterChangedEvent(target.Id));
        return ItemUseResult.Succeeded($"【{target.Name}】使用【{entry.Definition.Name}】");
    }

    private static ItemUseSupport ResolveSupport(InventoryEntry entry)
    {
        var item = entry.Definition;
        if (item is EquipmentDefinition)
        {
            return ItemUseSupport.Supported(ItemUseKind.Equipment, "请选择装备目标。", item.UseEffects);
        }

        return item.Type switch
        {
            ItemType.Consumable => ItemUseSupport.Unsupported("消耗品使用尚未接入。"),
            ItemType.Utility => ItemUseSupport.Unsupported("该道具暂无可用效果。"),
            ItemType.QuestItem => ItemUseSupport.Unsupported("剧情物品暂不可主动使用。"),
            ItemType.SkillBook => ResolveSkillBookSupport(item),
            ItemType.SpecialSkillBook => item.UseEffects.OfType<GrantSpecialSkillItemUseEffectDefinition>().Any()
                ? ItemUseSupport.Supported(ItemUseKind.SpecialSkillBook, "请选择研习目标。", item.UseEffects)
                : ItemUseSupport.Unsupported("该绝技书没有可用学习效果。"),
            ItemType.TalentBook => item.UseEffects.OfType<GrantTalentItemUseEffectDefinition>().Any()
                ? ItemUseSupport.Supported(ItemUseKind.TalentBook, "请选择研习目标。", item.UseEffects)
                : ItemUseSupport.Unsupported("该天赋书没有可用学习效果。"),
            ItemType.Booster => IsSupportedBooster(item)
                ? ItemUseSupport.Supported(ItemUseKind.Booster, "请选择使用目标。", item.UseEffects)
                : ItemUseSupport.Unsupported("该强化道具暂无可用效果。"),
            _ => ItemUseSupport.Unsupported("该物品暂不可使用。"),
        };
    }

    private static ItemUseSupport ResolveSkillBookSupport(ItemDefinition item)
    {
        var hasSkillEffect = item.UseEffects.Any(effect =>
            effect is GrantExternalSkillItemUseEffectDefinition ||
            effect is GrantInternalSkillItemUseEffectDefinition);
        return hasSkillEffect
            ? ItemUseSupport.Supported(ItemUseKind.SkillBook, "请选择研习目标。", item.UseEffects)
            : ItemUseSupport.Unsupported("该武学书没有可用学习效果。");
    }

    private static bool IsSupportedBooster(ItemDefinition item) =>
        item.UseEffects.Count > 0 &&
        item.UseEffects.All(effect =>
            effect is AddMaxHpItemUseEffectDefinition ||
            effect is AddMaxMpItemUseEffectDefinition);

    private static string? ValidateRequirements(ItemDefinition item, CharacterInstance target)
    {
        foreach (var requirement in item.Requirements)
        {
            switch (requirement)
            {
                case StatItemRequirementDefinition stat:
                    if (target.GetStat(stat.StatId) < stat.Value)
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
            }
        }

        return null;
    }

    private static string? ValidateSpecificTarget(
        ItemUseKind kind,
        IReadOnlyList<ItemUseEffectDefinition> effects,
        InventoryEntry entry,
        CharacterInstance target) =>
        kind switch
        {
            ItemUseKind.Equipment => null,
            ItemUseKind.SkillBook => ValidateSkillBookTarget(effects, target),
            ItemUseKind.SpecialSkillBook => ValidateSpecialSkillTarget(effects, target),
            ItemUseKind.TalentBook => ValidateTalentBookTarget(effects, target),
            ItemUseKind.Booster => null,
            _ => "该物品暂不可使用。",
        };

    private static string? ValidateSkillBookTarget(
        IReadOnlyList<ItemUseEffectDefinition> effects,
        CharacterInstance target)
    {
        foreach (var effect in effects)
        {
            switch (effect)
            {
                case GrantExternalSkillItemUseEffectDefinition externalSkill:
                {
                    var currentLevel = target.GetExternalSkillLevel(externalSkill.SkillId);
                    if (currentLevel is not null && currentLevel.Value >= ResolveEffectiveMaxLevel(externalSkill.Level))
                    {
                        return "该外功已达上限";
                    }
                    break;
                }
                case GrantInternalSkillItemUseEffectDefinition internalSkill:
                {
                    var currentLevel = target.GetInternalSkillLevel(internalSkill.SkillId);
                    if (currentLevel is not null && currentLevel.Value >= ResolveEffectiveMaxLevel(internalSkill.Level))
                    {
                        return "该内功已达上限";
                    }
                    break;
                }
            }
        }

        return null;
    }

    private static string? ValidateSpecialSkillTarget(
        IReadOnlyList<ItemUseEffectDefinition> effects,
        CharacterInstance target)
    {
        foreach (var effect in effects.OfType<GrantSpecialSkillItemUseEffectDefinition>())
        {
            if (target.GetSpecialSkills().Any(skill => string.Equals(skill.Definition.Id, effect.SkillId, StringComparison.Ordinal)))
            {
                return "已领悟该绝技";
            }
        }

        return null;
    }

    private static string? ValidateTalentBookTarget(
        IReadOnlyList<ItemUseEffectDefinition> effects,
        CharacterInstance target)
    {
        foreach (var effect in effects.OfType<GrantTalentItemUseEffectDefinition>())
        {
            if (target.HasTalent(effect.TalentId))
            {
                return "已习得该天赋";
            }
        }

        return null;
    }

    private static string FormatStatName(StatType statType) => StatCatalog.GetDisplayNameCn(statType);

    private static int ResolveTargetSkillLevel(int? currentLevel, int? effectLevel)
    {
        var targetLevel = ResolveEffectiveMaxLevel(effectLevel);
        return currentLevel is null
            ? targetLevel
            : Math.Max(currentLevel.Value, targetLevel);
    }

    private static int ResolveEffectiveMaxLevel(int? effectLevel) =>
        effectLevel ?? DefaultLearnedSkillLevel;

    private enum ItemUseKind
    {
        Equipment,
        SkillBook,
        SpecialSkillBook,
        TalentBook,
        Booster,
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
