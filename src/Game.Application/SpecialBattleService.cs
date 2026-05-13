using Game.Core.Definitions;
using Game.Core.Model;
using Game.Core.Story;

namespace Game.Application;

public sealed class SpecialBattleService
{
    private const string AchievementResourceGroup = "nick";
    private const string AchievementResourcePrefix = AchievementResourceGroup + ".";
    private const string TrialBattleId = "试炼之地_战斗";
    private const string TrialLoseStoryId = "original_试炼之地.失败";
    private const string TrialWinFallbackStoryId = "霹雳堂_胜利";
    private const string HuashanTowerId = "华山论剑";
    private const string HuashanWinStoryId = "original_华山论剑分枝判断";
    private const string ZhenlongqijuBattleId = "珍珑棋局_战斗";
    private const string ZhenlongqijuWinStoryId = "珍珑棋局_胜利";
    private const string ZhenlongqijuLoseStoryId = "珍珑棋局_失败";
    private const string DefaultTowerRewardId = "黑玉断续膏";

    private static readonly string[] ArenaHardLevelNames =
    [
        "江湖宵小",
        "小有名气",
        "成名高手",
        "威震四方",
        "惊世骇俗",
        "天人合一",
    ];

    private readonly GameSession _session;
    private readonly MapConditionEvaluator _towerConditionEvaluator;

    public SpecialBattleService(GameSession session)
    {
        ArgumentNullException.ThrowIfNull(session);
        _session = session;
        _towerConditionEvaluator = new MapConditionEvaluator(session);
    }

    private GameState State => _session.State;

    public async ValueTask<StoryCommandResult> RunTowerAsync(
        IRuntimeHost host,
        CancellationToken cancellationToken = default)
    {
        var tower = await ChooseTowerAsync(host, cancellationToken);
        if (tower is null)
        {
            return StoryCommandResult.None;
        }

        var stages = tower.Stages.OrderBy(static stage => stage.Index).ToArray();
        if (stages.Length == 0)
        {
            await SayAsync(host, "北丑", $"天关【{tower.Name}】没有可挑战关卡。", cancellationToken);
            return StoryCommandResult.None;
        }

        var forbiddenCharacterIds = new HashSet<string>(StringComparer.Ordinal);
        var pendingRewards = new List<string>();
        for (var index = 0; index < stages.Length; index++)
        {
            var stage = stages[index];
            var selected = await SelectCombatantsAsync(host, stage.BattleId, forbiddenCharacterIds, cancellationToken);
            AddSelectedToForbidden(forbiddenCharacterIds, selected);

            var isWin = await RunBattleAsync(
                host,
                new OrdinaryBattleRequest(stage.BattleId, selected),
                cancellationToken);
            if (!isWin)
            {
                pendingRewards.Clear();
                await SayAsync(host, "北丑", "哦！你挂了。于是你毛都没有得到。", cancellationToken);
                await SayAsync(host, "主角", "......", cancellationToken);
                return StoryCommandResult.None;
            }

            UnlockStageAchievements(stage);
            var rewardId = RollTowerReward(stage);
            pendingRewards.Add(rewardId);
            await SayAsync(host, "北丑", $"恭喜你取得了胜利！你本场战斗的奖励为【{rewardId}】！", cancellationToken);

            if (index == stages.Length - 1)
            {
                await SayAsync(host, "北丑", $"恭喜你挑战天关【{tower.Name}】成功！", cancellationToken);
                GrantTowerRewards(pendingRewards);
                return StoryCommandResult.None;
            }

            await SayAsync(host, "北丑", BuildPendingTowerRewardText(pendingRewards), cancellationToken);
            if (forbiddenCharacterIds.Count >= State.Party.Members.Count)
            {
                await SayAsync(host, "北丑", "你已经无人可以应战了，我只能强制结束本次天关挑战了！", cancellationToken);
                GrantTowerRewards(pendingRewards);
                return StoryCommandResult.None;
            }

            var continueIndex = await ChooseAsync(
                host,
                "北丑",
                "要继续挑战下一关吗？",
                [
                    "挑战！（注意：下一场战斗失败则失去所有奖励！）",
                    "算了，拿着现在的奖励走人吧。",
                ],
                cancellationToken);
            if (continueIndex != 0)
            {
                await SayAsync(host, "北丑", "知难而退，也是一种勇气！", cancellationToken);
                GrantTowerRewards(pendingRewards);
                return StoryCommandResult.None;
            }
        }

        return StoryCommandResult.None;
    }

    public async ValueTask<StoryCommandResult> RunHuashanAsync(
        IRuntimeHost host,
        CancellationToken cancellationToken = default)
    {
        var tower = _session.ContentRepository.GetTower(HuashanTowerId);
        var forbiddenCharacterIds = new HashSet<string>(StringComparer.Ordinal);
        foreach (var stage in tower.Stages.OrderBy(static stage => stage.Index))
        {
            var selected = await SelectCombatantsAsync(host, stage.BattleId, forbiddenCharacterIds, cancellationToken);
            AddSelectedToForbidden(forbiddenCharacterIds, selected);
            var isWin = await RunBattleAsync(
                host,
                new OrdinaryBattleRequest(stage.BattleId, selected),
                cancellationToken);
            if (!isWin)
            {
                await host.ExecuteCommandAsync("gameover", [], cancellationToken);
                return StoryCommandResult.None;
            }
        }

        return StoryCommandResult.Jump(HuashanWinStoryId);
    }

    public async ValueTask<StoryCommandResult> RunTrialAsync(
        IRuntimeHost host,
        CancellationToken cancellationToken = default)
    {
        var forbidden = State.SpecialBattle.TrialCompletedCharacterIds.ToHashSet(StringComparer.Ordinal);
        if (!State.Party.Members.Any(member => !forbidden.Contains(member.Id)))
        {
            await SayAsync(host, "孔八拉", "当前队伍中没有可以继续试炼的角色。", cancellationToken);
            return StoryCommandResult.None;
        }

        var selected = await SelectCombatantsAsync(host, TrialBattleId, forbidden, cancellationToken);
        var trialCharacterId = selected.FirstOrDefault()
            ?? throw new InvalidOperationException("Trial battle requires one selected character.");

        var isWin = await RunBattleAsync(
            host,
            new OrdinaryBattleRequest(TrialBattleId, selected),
            cancellationToken);
        if (!isWin)
        {
            return StoryCommandResult.Jump(TrialLoseStoryId);
        }

        State.SpecialBattle.MarkTrialCompleted(trialCharacterId);
        var roleStoryId = $"霹雳堂_{trialCharacterId}";
        return StoryCommandResult.Jump(
            _session.ContentRepository.TryGetStorySegment(roleStoryId, out _)
                ? roleStoryId
                : TrialWinFallbackStoryId);
    }

    public async ValueTask<StoryCommandResult> RunArenaAsync(
        IRuntimeHost host,
        string? callbackStoryId,
        CancellationToken cancellationToken = default)
    {
        var arenaBattles = _session.ContentRepository.GetBattles()
            .Where(static battle => battle.Id.StartsWith("arena_", StringComparison.Ordinal))
            .OrderBy(static battle => battle.Id, StringComparer.Ordinal)
            .ToArray();
        if (arenaBattles.Length == 0)
        {
            await SayAsync(host, "主角", "当前没有可用的竞技场战斗。", cancellationToken);
            return CreateArenaReturn(callbackStoryId);
        }

        var battleIndex = await ChooseAsync(
            host,
            "主角",
            "选择竞技场",
            arenaBattles.Select(static battle => battle.Name.Replace("arena_", string.Empty, StringComparison.Ordinal)).ToArray(),
            cancellationToken);
        var hardLevelIndex = await ChooseAsync(
            host,
            "主角",
            "选择难度",
            ArenaHardLevelNames,
            cancellationToken);

        var selected = await SelectCombatantsAsync(host, arenaBattles[battleIndex].Id, EmptyForbiddenSet, cancellationToken);
        await RunBattleAsync(
            host,
            new ArenaBattleRequest(
                arenaBattles[battleIndex].Id,
                selected,
                hardLevelIndex + 1),
            cancellationToken);
        return CreateArenaReturn(callbackStoryId);
    }

    public async ValueTask<StoryCommandResult> RunZhenlongqijuAsync(
        IRuntimeHost host,
        CancellationToken cancellationToken = default)
    {
        var level = _session.Profile.ZhenlongqijuLevel;
        var selected = await SelectCombatantsAsync(host, ZhenlongqijuBattleId, EmptyForbiddenSet, cancellationToken);
        var isWin = await RunBattleAsync(
            host,
            new ZhenlongqijuBattleRequest(
                ZhenlongqijuBattleId,
                selected,
                level),
            cancellationToken);
        if (!isWin)
        {
            return StoryCommandResult.Jump(ZhenlongqijuLoseStoryId);
        }

        _session.ProfileService.AdvanceZhenlongqijuLevel();
        return StoryCommandResult.Jump(ZhenlongqijuWinStoryId);
    }

    private async ValueTask<TowerDefinition?> ChooseTowerAsync(
        IRuntimeHost host,
        CancellationToken cancellationToken)
    {
        var towers = _session.ContentRepository.GetTowers()
            .Where(IsTowerUnlocked)
            .OrderBy(static tower => tower.Id, StringComparer.Ordinal)
            .ToArray();
        if (towers.Length == 0)
        {
            await SayAsync(host, "北丑", "当前没有可挑战的天关。", cancellationToken);
            return null;
        }

        var index = await ChooseAsync(
            host,
            "北丑",
            "选择要挑战的天关",
            towers.Select(static tower => tower.Name).ToArray(),
            cancellationToken);
        return towers[index];
    }

    private bool IsTowerUnlocked(TowerDefinition tower)
    {
        var conditions = tower.UnlockConditions
            .Select(static condition => new MapEventConditionDefinition
            {
                Type = condition.Type,
                Value = condition.Value,
            })
            .ToArray();
        return _towerConditionEvaluator.AreSatisfied(conditions);
    }

    private async ValueTask<IReadOnlyList<string>> SelectCombatantsAsync(
        IRuntimeHost host,
        string battleId,
        IReadOnlySet<string> forbiddenCharacterIds,
        CancellationToken cancellationToken)
    {
        var specialHost = GetSpecialHost(host);
        return await specialHost.SelectCombatantsAsync(
            new CombatantSelectionRequest(battleId, forbiddenCharacterIds),
            cancellationToken);
    }

    private static async ValueTask<bool> RunBattleAsync(
        IRuntimeHost host,
        SpecialBattleRequest request,
        CancellationToken cancellationToken)
    {
        var specialHost = GetSpecialHost(host);
        return await specialHost.RunBattleAsync(request, cancellationToken);
    }

    private static ISpecialBattleRuntimeHost GetSpecialHost(IRuntimeHost host) =>
        host as ISpecialBattleRuntimeHost
            ?? throw new InvalidOperationException("The active story runtime host does not support special battles.");

    private void UnlockStageAchievements(TowerStageDefinition stage)
    {
        foreach (var achievementId in stage.AchievementIds)
        {
            var resource = _session.ContentRepository.GetResource(AchievementResourcePrefix + achievementId);
            if (!string.Equals(resource.Group, AchievementResourceGroup, StringComparison.Ordinal))
            {
                throw new InvalidOperationException(
                    $"Tower achievement '{achievementId}' must resolve to a '{AchievementResourceGroup}' resource.");
            }

            _session.ProfileService.UnlockAchievement(achievementId);
        }
    }

    private string RollTowerReward(TowerStageDefinition stage)
    {
        for (var attempt = 0; attempt < 100; attempt++)
        {
            if (stage.Rewards.Count == 0)
            {
                break;
            }

            var reward = stage.Rewards[Random.Shared.Next(stage.Rewards.Count)];
            if (reward.MaxClaims is > 0 &&
                State.SpecialBattle.GetTowerRewardClaimCount(reward.ContentId) >= reward.MaxClaims.Value)
            {
                continue;
            }

            if (Random.Shared.NextDouble() > reward.Probability)
            {
                continue;
            }

            if (reward.MaxClaims is > 0)
            {
                State.SpecialBattle.AddTowerRewardClaim(reward.ContentId);
            }

            return reward.ContentId;
        }

        return DefaultTowerRewardId;
    }

    private void GrantTowerRewards(IReadOnlyList<string> rewardIds)
    {
        foreach (var rewardId in rewardIds)
        {
            if (string.Equals(rewardId, "元宝", StringComparison.Ordinal))
            {
                State.Currency.AddGold(1);
                _session.Events.Publish(new CurrencyChangedEvent());
                continue;
            }

            _session.InventoryService.AddItem(rewardId);
        }
    }

    private static string BuildPendingTowerRewardText(IReadOnlyList<string> pendingRewards) =>
        pendingRewards.Count == 0
            ? "截止目前，你还没有获得额外奖励。"
            : $"截止目前，你的奖励有：{string.Join("、", pendingRewards.Select(static reward => $"【{reward}】"))}！";

    private static void AddSelectedToForbidden(
        ISet<string> forbiddenCharacterIds,
        IReadOnlyList<string> selectedCharacterIds)
    {
        foreach (var characterId in selectedCharacterIds)
        {
            forbiddenCharacterIds.Add(characterId);
        }
    }

    private static async ValueTask<int> ChooseAsync(
        IRuntimeHost host,
        string speaker,
        string prompt,
        IReadOnlyList<string> options,
        CancellationToken cancellationToken) =>
        await host.ChooseOptionAsync(
            new ChoiceContext(
                speaker,
                prompt,
                options.Select(static (option, index) => new ChoiceOptionView(index, option)).ToArray()),
            cancellationToken);

    private static ValueTask SayAsync(
        IRuntimeHost host,
        string speaker,
        string text,
        CancellationToken cancellationToken) =>
        host.DialogueAsync(new DialogueContext(speaker, text), cancellationToken);

    private static StoryCommandResult CreateArenaReturn(string? callbackStoryId) =>
        string.IsNullOrWhiteSpace(callbackStoryId)
            ? StoryCommandResult.None
            : StoryCommandResult.Jump(callbackStoryId);

    private static IReadOnlySet<string> EmptyForbiddenSet { get; } = new HashSet<string>(StringComparer.Ordinal);
}
