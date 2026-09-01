using Game.Core.Model;
using Game.Core.Model.Character;
using Game.Core.Story;
using Game.Core.Definitions;

namespace Game.Application;

internal sealed class InventoryCurrencyStoryCommands
{
    private readonly GameSession _session;
    public InventoryCurrencyStoryCommands(GameSession session) => _session = session;

    [StoryCommand("change_item", "item")]
    public void ChangeItem(string itemId, int delta = 1, bool showToast = true)
    {
        var item = ResolveItem(itemId);
        if (delta > 0)
        {
            _session.InventoryService.AddItem(item, delta, showToast);
        }
        else if (delta < 0)
        {
            ArgumentOutOfRangeException.ThrowIfEqual(delta, int.MinValue);
            _session.InventoryService.RemoveItem(item, -delta);
            if (showToast)
            {
                var quantity = -delta;
                var quantitySuffix = quantity > 1 ? $" x{quantity}" : string.Empty;
                _session.Events.Publish(new ToastRequestedEvent($"失去物品【{item.Name}】{quantitySuffix}"));
            }
        }
    }

    [StoryCommand("remove_item", "cost_item")]
    public void RemoveItem(string itemId, int quantity = 1, bool showToast = true)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(quantity);
        var item = ResolveItem(itemId, out var isLegacyNumericAlias);
        if (isLegacyNumericAlias && !_session.State.Inventory.ContainsStack(item, quantity))
        {
            // Saves created while the base questionnaire was accidentally
            // published never received the shared vote token.  Consuming a
            // branch-suffixed alias from such a save is already satisfied;
            // do not turn that former publishing bug into a permanent crash.
            return;
        }
        ChangeItem(itemId, -quantity, showToast);
    }

    private ItemDefinition ResolveItem(string itemId) => ResolveItem(itemId, out _);

    private ItemDefinition ResolveItem(string itemId, out bool isLegacyNumericAlias)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(itemId);
        isLegacyNumericAlias = false;
        if (_session.ContentRepository.TryGetItem(itemId, out var item))
        {
            return item;
        }

        // A few legacy XMJH branches suffix the shared token name with the
        // random branch number (for example 队友表决令3), although rollrole.lua
        // only creates the base item 队友表决令. Preserve that legacy intent by
        // resolving a numeric suffix only when the base item is an actual
        // definition; genuinely unknown item ids still fail normally.
        var baseEnd = itemId.Length;
        while (baseEnd > 0 && char.IsDigit(itemId[baseEnd - 1]))
        {
            baseEnd--;
        }

        if (baseEnd > 0 && baseEnd < itemId.Length &&
            _session.ContentRepository.TryGetItem(itemId[..baseEnd], out item))
        {
            isLegacyNumericAlias = true;
            return item;
        }

        return _session.ContentRepository.GetItem(itemId);
    }

    [StoryCommand("add_random_item", "item_random")]
    public void AddRandomItem(IReadOnlyList<string> itemIds, int quantity = 1)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(quantity);
        if (itemIds.Count == 0) throw new InvalidOperationException("add_random_item requires at least one item id.");
        foreach (var id in itemIds) _session.ContentRepository.GetItem(id);
        _session.InventoryService.AddItem(itemIds[_session.RandomService.Next(0, itemIds.Count)], quantity);
    }

    [StoryCommand("add_random_item_options")]
    public void AddRandomItemOptions(IReadOnlyList<string> options)
    {
        if (options.Count == 0)
            throw new InvalidOperationException("add_random_item_options requires at least one item option.");

        var parsed = options.Select(ParseRandomItemOption).ToArray();
        foreach (var option in parsed) _session.ContentRepository.GetItem(option.ItemId);
        var selected = parsed[_session.RandomService.Next(0, parsed.Length)];
        _session.InventoryService.AddItem(selected.ItemId, selected.Quantity);
    }

    private static (string ItemId, int Quantity) ParseRandomItemOption(string option)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(option);
        var separator = option.LastIndexOf('#');
        if (separator <= 0 || separator >= option.Length - 1 ||
            !int.TryParse(option[(separator + 1)..], out var quantity) || quantity <= 0)
        {
            throw new InvalidOperationException(
                $"Random item option '{option}' must use the form item-id#positive-quantity.");
        }

        return (option[..separator], quantity);
    }

    [StoryCommand("change_silver", "get_money")]
    public void ChangeSilver(int delta)
    {
        _session.State.Currency.ChangeSilver(delta);
        _session.Events.Publish(new CurrencyChangedEvent());
    }

    [StoryCommand("change_yuanbao", "yuanbao")]
    public void ChangeYuanbao(int delta) => _session.ProfileService.ChangeYuanbao(delta);
}

internal sealed class AdventureStoryCommands
{
    private readonly GameSession _session;
    public AdventureStoryCommands(GameSession session) => _session = session;

    [StoryCommand("advance_days", "cost_day")]
    public void AdvanceDays(int days)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(days);
        _session.State.Clock.AdvanceDays(days);
        _session.Events.Publish(new ClockChangedEvent());
    }

    [StoryCommand("advance_time_slots", "cost_hour")]
    public void AdvanceTimeSlots(int timeSlots)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(timeSlots);
        _session.State.Clock.AdvanceTimeSlots(timeSlots);
        _session.Events.Publish(new ClockChangedEvent());
    }

    [StoryCommand("advance_to_time_slot", "to_chinesetime")]
    public void AdvanceToTimeSlot(string choices)
    {
        var candidates = ParseTimeSlotChoices(choices);
        var target = candidates[_session.RandomService.Next(0, candidates.Count)];
        _session.State.Clock.AdvanceToTimeSlot(target);
        _session.Events.Publish(new ClockChangedEvent());
    }

    [StoryCommand("show_cloud")]
    public void ShowCloud(bool visible)
    {
        _session.State.Adventure.SetCloudVisible(visible);
        _session.Events.Publish(new AdventureStateChangedEvent());
    }

    [StoryCommand("set_round")]
    public void SetRound(int round)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(round);
        _session.State.Adventure.SetRound(round);
        _session.ProfileService.RecordRoundReached(round);
        _session.Events.Publish(new AdventureStateChangedEvent());
    }

    [StoryCommand("set_difficulty", "set_game_mode")]
    public void SetDifficulty(string difficulty)
    {
        _session.State.Adventure.SetDifficulty(difficulty switch
        {
            "normal" => GameDifficulty.Normal,
            "hard" => GameDifficulty.Hard,
            "crazy" => GameDifficulty.Crazy,
            _ => throw new InvalidOperationException($"Unknown difficulty '{difficulty}'."),
        });
        _session.Events.Publish(new AdventureStateChangedEvent());
    }

    [StoryCommand("set_no_regret")]
    public void SetNoRegret(bool enabled)
    {
        _session.State.Adventure.SetNoRegret(enabled);
        _session.Events.Publish(new AdventureStateChangedEvent());
    }

    [StoryCommand("set_sect", "menpai")]
    public void SetSect(string sect)
    {
        _session.State.Adventure.SetSect(sect);
        _session.Events.Publish(new AdventureStateChangedEvent());
    }

    [StoryCommand("change_morality", "daode")]
    public void ChangeMorality(int delta)
    {
        _session.State.Adventure.ChangeMorality(delta);
        _session.Events.Publish(new AdventureStateChangedEvent());
    }

    [StoryCommand("change_favorability", "haogan")]
    public void ChangeFavorability(string characterId, int delta)
    {
        _session.State.Adventure.ChangeFavorability(characterId, delta);
        _session.Events.Publish(new AdventureStateChangedEvent());
    }

    [StoryCommand("set_rank")]
    public void SetRank(double rank)
    {
        _session.State.Adventure.SetRank(rank);
        _session.Events.Publish(new AdventureStateChangedEvent());
    }

    private static IReadOnlyList<TimeSlot> ParseTimeSlotChoices(string choices)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(choices);
        if (string.Equals(choices.Trim(), "random", StringComparison.OrdinalIgnoreCase))
        {
            return Enum.GetValues<TimeSlot>();
        }

        var candidates = choices
            .Where(static character => !char.IsWhiteSpace(character) && character is not ',' and not '，' and not '|' and not '#')
            .Select(ParseChineseTimeSlot)
            .ToArray();
        return candidates.Length > 0
            ? candidates
            : throw new InvalidOperationException($"No valid time slot was provided in '{choices}'.");
    }

    private static TimeSlot ParseChineseTimeSlot(char value) => value switch
    {
        '子' => TimeSlot.Zi,
        '丑' => TimeSlot.Chou,
        '寅' => TimeSlot.Yin,
        '卯' => TimeSlot.Mao,
        '辰' => TimeSlot.Chen,
        '巳' => TimeSlot.Si,
        '午' => TimeSlot.Wu,
        '未' => TimeSlot.Wei,
        '申' => TimeSlot.Shen,
        '酉' => TimeSlot.You,
        '戌' => TimeSlot.Xu,
        '亥' => TimeSlot.Hai,
        _ => throw new InvalidOperationException($"Unknown Chinese time slot '{value}'."),
    };
}

internal sealed class StoryStateCommands
{
    private readonly GameSession _session;
    private readonly StoryVariableMutationService _variableMutations;

    public StoryStateCommands(GameSession session, StoryVariableMutationService variableMutations)
    {
        _session = session;
        _variableMutations = variableMutations;
    }

    [StoryCommand("journal", "log")]
    public void Journal(string text)
    {
        _session.State.Journal.Append(_session.State.Clock, text);
        _session.Events.Publish(new JournalChangedEvent());
    }

    [StoryCommand("set_flag")]
    public void SetFlag(string name) => _variableMutations.Assign(name, ExpressionValue.FromBoolean(true));

    [StoryCommand("clear_flag")]
    public void ClearFlag(string name) => _variableMutations.Delete(name, "clear_flag");

    [StoryCommand("change_story_number")]
    public void ChangeStoryNumber(string name, double delta)
    {
        var current = _session.State.Story.TryGetVariable(name, out var value)
            ? value.AsNumber($"Story variable '{name}'")
            : 0;
        // Legacy XMJH used favorability as a non-negative counter. Its first
        // write often subtracts 50/100 only to cancel the old implicit value
        // of 50. Story numbers start at zero, so preserve the observable
        // counter semantics by keeping this compatibility mutation at zero.
        // Authors who need signed values can still use native DSL assignment.
        _variableMutations.Assign(name, ExpressionValue.FromNumber(Math.Max(0, current + delta)));
    }

    [StoryCommand("list_story_numbers")]
    public void ListStoryNumbers()
    {
        var entries = _session.State.Story.Variables
            .Where(static entry => entry.Value.Kind == ExpressionValueKind.Number)
            .OrderBy(static entry => entry.Key, StringComparer.Ordinal)
            .Select(static entry => $"{entry.Key}={entry.Value.AsNumber(entry.Key).ToString(System.Globalization.CultureInfo.InvariantCulture)}")
            .ToArray();
        var summary = entries.Length == 0
            ? "剧情数值变量：暂无"
            : $"剧情数值变量（共{entries.Length}个）：{string.Join("，", entries)}";
        _session.State.Journal.Append(_session.State.Clock, summary);
        _session.Events.Publish(new JournalChangedEvent());
    }

    [StoryCommand("set_time_key")]
    public void SetTimeKey(string key, int days, string storyId = "")
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(days);
        if (!string.IsNullOrWhiteSpace(storyId))
        {
            _session.ContentRepository.GetStorySegment(storyId);
        }

        _session.State.Story.SetTimeKey(key, _session.State.Clock, days, storyId);
        _session.Events.Publish(new StoryStateChangedEvent());
    }

    [StoryCommand("clear_time_key")]
    public void ClearTimeKey(string key)
    {
        if (!_session.State.Story.RemoveTimeKey(key))
        {
            _session.DiagnosticLogger.Warning($"Command 'clear_time_key' ignored missing story time key '{key}'.");
            return;
        }

        _session.Events.Publish(new StoryStateChangedEvent());
    }

    [StoryCommand("world_triggers")]
    public void SetWorldTriggersEnabled(bool enabled)
    {
        if (enabled) _session.WorldTriggerService.Unblock();
        else _session.WorldTriggerService.Block();
    }
}

internal sealed class CharacterGrowthStoryCommands
{
    private readonly GameSession _session;
    public CharacterGrowthStoryCommands(GameSession session) => _session = session;

    [StoryCommand("set_character_name")]
    public void SetCharacterName(string characterId, string name) =>
        _session.CharacterService.RenameCharacter(characterId, name);

    [StoryCommand("change_stat")]
    public void ChangeStat(string characterId, string stat, int delta) =>
        _session.CharacterService.AddBaseStat(characterId, stat, delta);

    [StoryCommand("set_growth", "growtemplate")]
    public void SetGrowth(string characterId, string growthId) =>
        _session.CharacterService.SetGrowTemplate(characterId, growthId);

    [StoryCommand("scale_stats")]
    public void ScaleStats(string characterId, double ratio) =>
        _session.CharacterService.ScaleStats(characterId, ratio);

    [StoryCommand("grant_points", "grant_point", "get_point")]
    public void GrantPoints(string characterId, int points)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(points);
        _session.CharacterService.GrantStatPoints(characterId, points);
    }

    [StoryCommand("grant_exp", "get_exp")]
    public void GrantExperience(string characterId, int experience)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(experience);
        _session.CharacterService.GainExperience(characterId, experience);
    }

    [StoryCommand("level_up", "levelup")]
    public void LevelUp(string characterId, int levels = 1)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(levels);
        _session.CharacterService.LevelUp(characterId, levels);
    }

    [StoryCommand("upgrade_external")]
    public void UpgradeExternal(string characterId, string skillId, int levels = 1) =>
        _session.CharacterService.UpgradeExternalSkillLevel(characterId, skillId, levels);

    [StoryCommand("upgrade_internal")]
    public void UpgradeInternal(string characterId, string skillId, int levels = 1) =>
        _session.CharacterService.UpgradeInternalSkillLevel(characterId, skillId, levels);

    [StoryCommand("upgrade_skill")]
    public void UpgradeSkill(string characterId, string skillId, int levels = 1) =>
        _session.CharacterService.UpgradeSkillLevel(characterId, skillId, levels);

    [StoryCommand("maxlevel", "max_skill_level")]
    public void MaxSkillLevel(string skillId, int levels = 1, string onceKey = "")
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(levels);
        var skillName = ResolveSkillName(skillId);
        if (!_session.ProfileService.TryAddSkillMaxLevelBonusOnce(skillId, levels, onceKey))
        {
            return;
        }

        _session.Events.Publish(new ProfileChangedEvent());
        _session.Events.Publish(new ToastRequestedEvent(
            $"武学精通【{skillName}】+ {levels}",
            ToastTone.Important));
    }

    private string ResolveSkillName(string skillId)
    {
        if (_session.ContentRepository.TryGetExternalSkill(skillId, out var externalSkill))
        {
            return externalSkill.Name;
        }

        if (_session.ContentRepository.TryGetInternalSkill(skillId, out var internalSkill))
        {
            return internalSkill.Name;
        }

        throw new InvalidOperationException($"Command 'maxlevel' references unknown skill '{skillId}'.");
    }
}

internal sealed class PartyLearningStoryCommands
{
    private readonly GameSession _session;
    public PartyLearningStoryCommands(GameSession session) => _session = session;

    [StoryCommand("join")]
    public void Join(string characterId, string? definitionId = null) =>
        _session.PartyService.Join(characterId, definitionId);

    [StoryCommand("join_random")]
    public void JoinRandom(IReadOnlyList<string> characterIds)
    {
        if (characterIds.Count == 0)
            throw new ArgumentException("join_random requires at least one candidate.", nameof(characterIds));

        foreach (var characterId in characterIds)
            _session.ContentRepository.GetCharacter(characterId);

        var selectedCharacterId = characterIds[_session.RandomService.Next(0, characterIds.Count)];
        _session.PartyService.Join(selectedCharacterId);
    }

    [StoryCommand("follow")]
    public void Follow(string characterId, string? definitionId = null) =>
        _session.PartyService.Follow(characterId, definitionId);

    [StoryCommand("leave")]
    public void Leave(string characterId) => _session.PartyService.Leave(characterId);

    [StoryCommand("leave_follower", "leave_follow")]
    public void LeaveFollower(string characterId) => _session.PartyService.LeaveFollow(characterId);

    [StoryCommand("leave_all")]
    public void LeaveAll() => _session.PartyService.LeaveAll();

    [StoryCommand("learn_external")]
    public void LearnExternal(string characterId, string skillId, int level = 1) =>
        _session.CharacterService.LearnExternalSkill(characterId, skillId, level);

    [StoryCommand("learn")]
    public void LearnAny(string characterId, string targetId, int level = 1) =>
        _session.CharacterService.LearnAny(characterId, targetId, level);

    [StoryCommand("learn_internal")]
    public void LearnInternal(string characterId, string skillId, int level = 1) =>
        _session.CharacterService.LearnInternalSkill(characterId, skillId, level);

    [StoryCommand("learn_special")]
    public void LearnSpecial(string characterId, string skillId) =>
        _session.CharacterService.LearnSpecialSkill(characterId, skillId);

    [StoryCommand("learn_talent")]
    public void LearnTalent(string characterId, string talentId) =>
        _session.CharacterService.LearnTalent(characterId, talentId);

    [StoryCommand("learn_title")]
    public void LearnTitle(string characterId, string titleId) =>
        _session.CharacterService.LearnTitle(characterId, titleId);

    [StoryCommand("equip_title")]
    public void EquipTitle(string characterId, string titleId) =>
        _session.CharacterService.EquipTitle(characterId, titleId);

    [StoryCommand("remove_external")]
    public void RemoveExternal(string characterId, string skillId) =>
        _session.CharacterService.RemoveExternalSkill(characterId, skillId);

    [StoryCommand("remove")]
    public void RemoveAny(string characterId, string targetId) =>
        _session.CharacterService.RemoveAny(characterId, targetId);

    [StoryCommand("remove_internal")]
    public void RemoveInternal(string characterId, string skillId) =>
        _session.CharacterService.RemoveInternalSkill(characterId, skillId);

    [StoryCommand("remove_special")]
    public void RemoveSpecial(string characterId, string skillId) =>
        _session.CharacterService.RemoveSpecialSkill(characterId, skillId);

    [StoryCommand("remove_talent")]
    public void RemoveTalent(string characterId, string talentId) =>
        _session.CharacterService.RemoveTalent(characterId, talentId);

    [StoryCommand("remove_title")]
    public void RemoveTitle(string characterId, string titleId) =>
        _session.CharacterService.RemoveTitle(characterId, titleId);

    [StoryCommand("unlock_achievement", "nick")]
    public void UnlockAchievement(string achievementId)
    {
        var resource = _session.ContentRepository.GetResource("nick." + achievementId);
        if (!string.Equals(resource.Group, "nick", StringComparison.Ordinal))
        {
            throw new InvalidOperationException(
                $"Command 'unlock_achievement' requires a resource in group 'nick', but 'nick.{achievementId}' belongs to '{resource.Group ?? "<none>"}'.");
        }

        _session.ProfileService.UnlockAchievement(achievementId);
    }

}

internal sealed class SpecialFlowStoryCommands
{
    private readonly GameSession _session;
    private readonly IRuntimeHost _host;

    public SpecialFlowStoryCommands(GameSession session, IRuntimeHost host)
    {
        _session = session;
        _host = host;
    }

    [StoryCommand("minigame", "game")]
    public ValueTask<StoryCommandResult> Minigame(string gameId, CancellationToken cancellationToken) =>
        _session.MiniGameService.RunAsync(_host, gameId, cancellationToken);

    [StoryCommand("refine", "xilian")]
    public ValueTask<StoryCommandResult> Refine(CancellationToken cancellationToken) =>
        _session.EquipmentRefinementService.RunAsync(_host, cancellationToken);

    [StoryCommand("tower")]
    public ValueTask<StoryCommandResult> Tower(CancellationToken cancellationToken) =>
        _session.SpecialBattleService.RunTowerAsync(_host, cancellationToken);

    [StoryCommand("huashan")]
    public ValueTask<StoryCommandResult> Huashan(CancellationToken cancellationToken) =>
        _session.SpecialBattleService.RunHuashanAsync(_host, cancellationToken);

    [StoryCommand("trial")]
    public ValueTask<StoryCommandResult> Trial(CancellationToken cancellationToken) =>
        _session.SpecialBattleService.RunTrialAsync(_host, cancellationToken);

    [StoryCommand("zhenlong", "zhenlongqiju")]
    public ValueTask<StoryCommandResult> Zhenlong(CancellationToken cancellationToken) =>
        _session.SpecialBattleService.RunZhenlongqijuAsync(_host, cancellationToken);

    [StoryCommand("arena")]
    public ValueTask<StoryCommandResult> Arena(string callbackStoryId = "", CancellationToken cancellationToken = default) =>
        _session.SpecialBattleService.RunArenaAsync(
            _host,
            string.IsNullOrWhiteSpace(callbackStoryId) ? null : callbackStoryId,
            cancellationToken);
}
