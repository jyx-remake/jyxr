using Game.Core.Abstractions;
using Game.Core.Definitions;
using Game.Core.Definitions.Skills;
using Game.Core.Model;
using Game.Core.Story;

namespace Game.Application;

public sealed class StoryCommandDispatcher
{
    private const string AchievementResourceGroup = "nick";
    private const string AchievementResourcePrefix = AchievementResourceGroup + ".";
    private const string FemaleLeadCharacterId = "女主";
    private readonly GameSession _session;
    private readonly IRuntimeHost _host;
    private readonly StoryCommandBinder _binder;

    public StoryCommandDispatcher(GameSession session, IRuntimeHost host)
    {
        ArgumentNullException.ThrowIfNull(session);
        ArgumentNullException.ThrowIfNull(host);
        _session = session;
        _host = host;
        _binder = new StoryCommandBinder(this);
    }

    private GameState State => _session.State;
    private IContentRepository ContentRepository => _session.ContentRepository;

    public ValueTask<StoryCommandResult> ExecuteCommandAsync(
        string name,
        IReadOnlyList<ExprValue> args,
        CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentNullException.ThrowIfNull(args);

        if (_binder.TryExecute(name, args, cancellationToken, out var result))
        {
            return result;
        }

        return _host.ExecuteCommandAsync(name, args, cancellationToken);
    }

    [StoryCommand("item")]
    private ValueTask ExecuteItemAsync(string itemId, int quantity = 1)
    {
        _session.InventoryService.AddItem(itemId, quantity);
        return ValueTask.CompletedTask;
    }

    [StoryCommand("cost_item")]
    private ValueTask ExecuteCostItemAsync(string itemId, int quantity = 1)
    {
        _session.InventoryService.RemoveItem(itemId, quantity);
        return ValueTask.CompletedTask;
    }

    [StoryCommand("get_money")]
    private ValueTask ExecuteGetMoneyAsync(int amount) => ExecuteChangeSilverAsync(amount);

    [StoryCommand("cost_money")]
    private ValueTask ExecuteCostMoneyAsync(int amount) => ExecuteChangeSilverAsync(-amount);

    private ValueTask ExecuteChangeSilverAsync(int delta)
    {
        State.Currency.ChangeSilver(delta);
        _session.Events.Publish(new CurrencyChangedEvent());
        return ValueTask.CompletedTask;
    }

    [StoryCommand("yuanbao")]
    private ValueTask ExecuteChangeGoldAsync(int amount)
    {
        State.Currency.ChangeGold(amount);
        _session.Events.Publish(new CurrencyChangedEvent());
        return ValueTask.CompletedTask;
    }

    [StoryCommand("cost_day")]
    private ValueTask ExecuteCostDayAsync(int days)
    {
        State.Clock.AdvanceDays(days);
        _session.Events.Publish(new ClockChangedEvent());
        return ValueTask.CompletedTask;
    }

    [StoryCommand("set_round")]
    private ValueTask ExecuteSetRoundAsync(int round)
    {
        State.Adventure.SetRound(round);
        _session.Events.Publish(new AdventureStateChangedEvent());
        return ValueTask.CompletedTask;
    }

    [StoryCommand("set_game_mode")]
    private ValueTask ExecuteSetGameModeAsync(string mode)
    {
        State.Adventure.SetDifficulty(ParseGameDifficulty(mode));
        _session.Events.Publish(new AdventureStateChangedEvent());
        return ValueTask.CompletedTask;
    }

    [StoryCommand("log")]
    private ValueTask ExecuteLogAsync(string text)
    {
        State.Journal.Append(State.Clock, text);
        _session.Events.Publish(new JournalChangedEvent());
        return ValueTask.CompletedTask;
    }

    [StoryCommand("set_flag")]
    private ValueTask ExecuteSetStoryBooleanAsync(string variableName)
    {
        State.Story.SetVariable(variableName, ExprValue.FromBoolean(true));
        _session.Events.Publish(new StoryStateChangedEvent());
        return ValueTask.CompletedTask;
    }

    [StoryCommand("set_time_key")]
    private ValueTask ExecuteSetTimeKeyAsync(string key, int limitDays, string targetStoryId)
    {
        ContentRepository.GetStorySegment(targetStoryId);
        State.Story.SetTimeKey(key, State.Clock, limitDays, targetStoryId);
        _session.Events.Publish(new StoryStateChangedEvent());
        return ValueTask.CompletedTask;
    }

    [StoryCommand("clear_flag")]
    private ValueTask ExecuteClearFlagAsync(string variableName)
    {
        if (State.Story.RemoveVariable(variableName))
        {
            _session.Events.Publish(new StoryStateChangedEvent());
        }

        return ValueTask.CompletedTask;
    }

    [StoryCommand("world_trigger")]
    private ValueTask ExecuteWorldTriggerAsync(string mode)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(mode);

        switch (mode.Trim())
        {
            case "on":
                _session.WorldTriggerService.Unblock();
                break;
            case "off":
                _session.WorldTriggerService.Block();
                break;
            default:
                throw new InvalidOperationException($"Unsupported world_trigger mode '{mode}'.");
        }

        return ValueTask.CompletedTask;
    }

    [StoryCommand("daode")]
    private ValueTask ExecuteChangeDaodeAsync(int delta)
    {
        State.Adventure.ChangeMorality(delta);
        _session.Events.Publish(new AdventureStateChangedEvent());
        return ValueTask.CompletedTask;
    }

    [StoryCommand("clear_time_key")]
    private ValueTask ExecuteClearTimeKeyAsync(string key)
    {
        if (State.Story.RemoveTimeKey(key))
        {
            _session.Events.Publish(new StoryStateChangedEvent());
        }

        return ValueTask.CompletedTask;
    }

    [StoryCommand("haogan")]
    private ValueTask ExecuteChangeHaoganAsync(int delta)
    {
        State.Adventure.ChangeFavorability(delta);
        _session.Events.Publish(new AdventureStateChangedEvent());
        return ValueTask.CompletedTask;
    }

    [StoryCommand("rank")]
    private ValueTask IgnoreRankAsync(params ExprValue[] _) => ValueTask.CompletedTask;

    [StoryCommand("touch")]
    private ValueTask IgnoreTouchAsync(params ExprValue[] _) => ValueTask.CompletedTask;

    [StoryCommand("menpai")]
    private ValueTask ExecuteSetMenpaiAsync(string value)
    {
        State.Adventure.SetSect(value);
        _session.Events.Publish(new AdventureStateChangedEvent());
        return ValueTask.CompletedTask;
    }

    [StoryCommand("upgrade")]
    private ValueTask ExecuteUpgradeAsync(string target, string characterId, params ExprValue[] args)
    {
        if (StatCatalog.TryParse(target, out _))
        {
            var value = checked((int)args[0].AsNumber("Invocation 'upgrade' argument 'value'"));
            _session.CharacterService.AddBaseStat(characterId, target, value);
            return ValueTask.CompletedTask;
        }

        switch (target)
        {
            case "skill":
            {
                var skillId = args[0].AsString("Invocation 'upgrade' argument 'skillId'");
                var levels = checked((int)args[1].AsNumber("Invocation 'upgrade' argument 'levels'"));
                UpgradeSkillLevel(characterId, skillId, levels);
                return ValueTask.CompletedTask;
            }
            case "external":
            {
                var skillId = args[0].AsString("Invocation 'upgrade' argument 'skillId'");
                var levels = checked((int)args[1].AsNumber("Invocation 'upgrade' argument 'levels'"));
                _session.CharacterService.UpgradeExternalSkillLevel(characterId, skillId, levels);
                return ValueTask.CompletedTask;
            }
            case "internal":
            {
                var skillId = args[0].AsString("Invocation 'upgrade' argument 'skillId'");
                var levels = checked((int)args[1].AsNumber("Invocation 'upgrade' argument 'levels'"));
                _session.CharacterService.UpgradeInternalSkillLevel(characterId, skillId, levels);
                return ValueTask.CompletedTask;
            }
            default:
                throw new InvalidOperationException($"Unsupported upgrade target '{target}'.");
        }
    }

    private void UpgradeSkillLevel(string characterId, string skillId, int levels)
    {
        if (ContentRepository.TryGetExternalSkill(skillId, out _))
        {
            _session.CharacterService.UpgradeExternalSkillLevel(characterId, skillId, levels);
            return;
        }

        if (ContentRepository.TryGetInternalSkill(skillId, out _))
        {
            _session.CharacterService.UpgradeInternalSkillLevel(characterId, skillId, levels);
            return;
        }

        throw new InvalidOperationException($"Command 'upgrade skill' references unknown external/internal skill '{skillId}'.");
    }

    [StoryCommand("minus_maxpoints")]
    private ValueTask ExecuteMinusMaxPointsAsync(string characterId, int tenths)
    {
        _session.CharacterService.ScaleLegacyMinusMaxPoints(characterId, tenths);
        return ValueTask.CompletedTask;
    }

    [StoryCommand("growtemplate")]
    private ValueTask ExecuteGrowTemplateAsync(string characterId, string growTemplateId)
    {
        _session.CharacterService.SetGrowTemplate(characterId, growTemplateId);
        return ValueTask.CompletedTask;
    }

    [StoryCommand("grant_point", "get_point")]
    private ValueTask ExecuteGrantPointAsync(string characterId, int value)
    {
        _session.CharacterService.GrantStatPoints(characterId, value);
        return ValueTask.CompletedTask;
    }

    [StoryCommand("grant_exp", "get_exp")]
    private ValueTask ExecuteGrantExperienceAsync(string characterId, int value)
    {
        _session.CharacterService.GainExperience(characterId, value);
        return ValueTask.CompletedTask;
    }

    [StoryCommand("levelup")]
    private ValueTask ExecuteLevelUpAsync(string characterId, int levels = 1)
    {
        _session.CharacterService.LevelUp(characterId, levels);
        return ValueTask.CompletedTask;
    }

    [StoryCommand("change_female_name")]
    private ValueTask ExecuteChangeFemaleNameAsync(string name, string characterId = FemaleLeadCharacterId)
    {
        _session.PartyService.RenameOrCreateReserve(characterId, name);
        return ValueTask.CompletedTask;
    }

    [StoryCommand("join")]
    private ValueTask ExecuteJoinAsync(string characterId)
    {
        _session.PartyService.Join(characterId);
        return ValueTask.CompletedTask;
    }

    [StoryCommand("follow")]
    private ValueTask ExecuteFollowAsync(string characterId)
    {
        _session.PartyService.Follow(characterId);
        return ValueTask.CompletedTask;
    }

    [StoryCommand("leave")]
    private ValueTask ExecuteLeaveAsync(string characterIdOrName)
    {
        _session.PartyService.Leave(characterIdOrName);
        return ValueTask.CompletedTask;
    }

    [StoryCommand("leave_follow")]
    private ValueTask ExecuteLeaveFollowAsync(string characterIdOrName)
    {
        _session.PartyService.LeaveFollow(characterIdOrName);
        return ValueTask.CompletedTask;
    }

    [StoryCommand("leave_all")]
    private ValueTask ExecuteLeaveAllAsync()
    {
        _session.PartyService.LeaveAll();
        return ValueTask.CompletedTask;
    }

    [StoryCommand("learn")]
    private ValueTask ExecuteLearnAsync(string learnType, string characterId, string targetId, int level = 1)
    {
        _session.CharacterService.Learn(characterId, learnType, targetId, level);
        return ValueTask.CompletedTask;
    }

    [StoryCommand("remove")]
    private ValueTask ExecuteRemoveAsync(string removeType, string characterId, string targetId)
    {
        _session.CharacterService.Remove(characterId, removeType, targetId);
        return ValueTask.CompletedTask;
    }

    [StoryCommand("maxlevel")]
    private ValueTask ExecuteMaxLevelAsync(string skillId, int level)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(level);

        var skillName = ResolveMaxLevelSkillName(skillId);
        _session.Events.Publish(new ToastRequestedEvent($"武学精通【{skillName}】+ {level}"));
        return ValueTask.CompletedTask;
    }

    [StoryCommand("nick")]
    private ValueTask ExecuteUnlockAchievementAsync(string rawAchievementId)
    {
        var resourceId = ResolveAchievementResourceId(rawAchievementId);
        var achievement = ContentRepository.GetResource(resourceId);
        if (!string.Equals(achievement.Group, AchievementResourceGroup, StringComparison.Ordinal))
        {
            throw new InvalidOperationException($"Command 'nick' references non-achievement resource '{resourceId}'.");
        }

        _session.ProfileService.UnlockAchievement(rawAchievementId);
        return ValueTask.CompletedTask;
    }

    // TODO: 小游戏需要独立运行流，当前先给出占位提示。
    [StoryCommand("game")]
    private ValueTask ExecuteGamePlaceholderAsync(params ExprValue[] _) => ExecuteTodoCommandAsync("game");

    // TODO: 新手引导流程需要独立宿主接线，当前先给出占位提示。
    [StoryCommand("newbie")]
    private ValueTask ExecuteNewbiePlaceholderAsync(params ExprValue[] _) => ExecuteTodoCommandAsync("newbie");

    [StoryCommand("xilian")]
    private ValueTask<StoryCommandResult> ExecuteXilianAsync(
        CancellationToken cancellationToken,
        params ExprValue[] _) =>
        _session.EquipmentRefinementService.RunAsync(_host, cancellationToken);

    // TODO: 爬塔玩法需要独立流程接线，当前先给出占位提示。
    [StoryCommand("tower")]
    private ValueTask ExecuteTowerPlaceholderAsync(params ExprValue[] _) => ExecuteTodoCommandAsync("tower");

    // TODO: 华山论剑玩法需要独立流程接线，当前先给出占位提示。
    [StoryCommand("huashan")]
    private ValueTask ExecuteHuashanPlaceholderAsync(params ExprValue[] _) => ExecuteTodoCommandAsync("huashan");

    // TODO: 试炼玩法需要独立流程接线，当前先给出占位提示。
    [StoryCommand("trial")]
    private ValueTask ExecuteTrialPlaceholderAsync(params ExprValue[] _) => ExecuteTodoCommandAsync("trial");

    // TODO: 珍珑棋局玩法需要独立流程接线，当前先给出占位提示。
    [StoryCommand("zhenlongqiju")]
    private ValueTask ExecuteZhenlongqijuPlaceholderAsync(params ExprValue[] _) => ExecuteTodoCommandAsync("zhenlongqiju");

    // TODO: 擂台玩法需要独立流程接线，当前先给出占位提示。
    [StoryCommand("arena")]
    private ValueTask ExecuteArenaPlaceholderAsync(params ExprValue[] _) => ExecuteTodoCommandAsync("arena");

    private static string ResolveAchievementResourceId(string achievementId) =>
        AchievementResourcePrefix + achievementId;

    private ValueTask ExecuteTodoCommandAsync(string commandName)
    {
        _session.Events.Publish(new ToastRequestedEvent($"{commandName}指令暂未实现"));
        return ValueTask.CompletedTask;
    }

    private static GameDifficulty ParseGameDifficulty(string mode)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(mode);

        return mode.Trim() switch
        {
            "normal" => GameDifficulty.Normal,
            "hard" => GameDifficulty.Hard,
            "crazy" => GameDifficulty.Crazy,
            _ => throw new InvalidOperationException($"Unsupported game mode '{mode}'."),
        };
    }

    private string ResolveMaxLevelSkillName(string skillId)
    {
        if (ContentRepository.TryGetExternalSkill(skillId, out var externalSkill))
        {
            return externalSkill.Name;
        }

        if (ContentRepository.TryGetInternalSkill(skillId, out var internalSkill))
        {
            return internalSkill.Name;
        }

        throw new InvalidOperationException($"Command 'maxlevel' references unknown skill '{skillId}'.");
    }

}
