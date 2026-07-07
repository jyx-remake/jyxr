using Game.Core.Model;
using Game.Core.Story;

namespace Game.Application;

public sealed class MiniGameService
{
    private const string LightnessTrainingScriptId = "qinggong";
    private const string StrengthTrainingScriptId = "dianxue";
    private const string LightnessTrainingPracticeKey = "lightness_training";
    private const string StrengthTrainingPracticeKey = "strength_training";
    private const string HeroSpeaker = "主角";
    private const string TrainerSpeaker = "佟湘玉";
    private const string StrengthTrainerSpeaker = "白展堂";
    private const int LightnessTrainingStatIncrease = 5;
    private const int LightnessTrainingPracticeMultiplier = 2;
    private const int StrengthTrainingStatIncrease = 5;

    private static readonly HashSet<string> UniqueRewardIds = new(StringComparer.Ordinal)
    {
        "金丝道袍",
        "阔剑",
        "精钢拳套",
        "金刚杵",
        "柳叶刀",
        "罗汉拳谱",
        "天山掌法谱",
        "松风剑法秘籍",
        "华山剑法秘籍",
        "三分剑术",
        "雷震剑法秘籍",
        "南山刀法谱",
        "袖箭秘诀",
        "拂尘秘诀",
        "蛇鹤八打",
        "君子剑",
        "淑女剑",
        "黄金项链",
        "乌蚕衣",
        "凌波微步图谱",
        "天下轻功总决",
    };

    private static readonly string[] StrengthTrainingItemIds =
    [
        "大还丹",
        "大蟠桃",
        "冬虫夏草",
        "九转熊蛇丸",
        "生生造化丹",
        "柳叶刀",
        "金丝道袍",
        "黄金项链",
        "血刀",
        "乌蚕衣",
    ];

    private static readonly LightnessTrainingRewardTier[] LightnessTrainingRewardTiers =
    [
        new(5, 9, ["特制鸡腿"], "干得不错，奖你一点小礼物。"),
        new(10, 13, ["冬虫夏草", "金丝道袍", "阔剑", "精钢拳套", "金刚杵", "柳叶刀"], "太牛了，少侠我对你的敬意如滔滔江水不绝...一点小礼物，不成敬意。"),
        new(14, 16, ["生生造化丹", "冬虫夏草", "罗汉拳谱", "天山掌法谱", "松风剑法秘籍", "华山剑法秘籍", "三分剑术", "雷震剑法秘籍", "南山刀法谱", "袖箭秘诀", "拂尘秘诀", "蛇鹤八打"], "OMG...少侠我好崇拜你哦。"),
        new(17, 19, ["生生造化丹", "黑玉断续膏", "君子剑", "淑女剑"], "OMG少侠，你真的是人类么？"),
        new(20, 22, ["生生造化丹", "黑玉断续膏", "天王保命丹", "乌蚕衣"], "...你已经是God Like了。"),
        new(23, null, ["生生造化丹", "黑玉断续膏", "天王保命丹", "凌波微步图谱", "天下轻功总决"], "Oh, S**t！你已经超神了！"),
    ];

    private readonly GameSession _session;

    public MiniGameService(GameSession session)
    {
        ArgumentNullException.ThrowIfNull(session);
        _session = session;
    }

    private GameState State => _session.State;

    public async ValueTask<StoryCommandResult> RunAsync(
        IRuntimeHost host,
        string gameId,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(host);
        ArgumentException.ThrowIfNullOrWhiteSpace(gameId);

        return gameId.Trim() switch
        {
            LightnessTrainingScriptId => await RunLightnessTrainingAsync(host, cancellationToken),
            StrengthTrainingScriptId => await RunStrengthTrainingAsync(host, cancellationToken),
            "levelup" => ExecuteTodoCommand(),
            _ => ExecuteTodoCommand(),
        };
    }

    private async ValueTask<StoryCommandResult> RunLightnessTrainingAsync(
        IRuntimeHost host,
        CancellationToken cancellationToken)
    {
        if (host is not IMiniGameRuntimeHost miniGameHost)
        {
            throw new InvalidOperationException("Lightness training requires a mini-game runtime host.");
        }

        var survivedSeconds = Math.Max(0, await miniGameHost.RunLightnessTrainingAsync(cancellationToken));
        await ResolveLightnessTrainingAsync(host, survivedSeconds, cancellationToken);
        return StoryCommandResult.None;
    }

    private async ValueTask ResolveLightnessTrainingAsync(
        IRuntimeHost host,
        int survivedSeconds,
        CancellationToken cancellationToken)
    {
        await SayAsync(host, TrainerSpeaker, $"你坚持了{survivedSeconds}秒！", cancellationToken);

        var reward = ResolveLightnessTrainingReward(survivedSeconds);
        if (reward is not null)
        {
            await SayAsync(host, TrainerSpeaker, reward.Tier.Dialogue, cancellationToken);
            _session.InventoryService.AddItem(reward.ItemId);
            await SayAsync(host, HeroSpeaker, $"获得{reward.ItemId} x 1", cancellationToken);
            if (UniqueRewardIds.Contains(reward.ItemId))
            {
                State.MiniGame.MarkUniqueRewardClaimed(reward.ItemId);
            }
        }

        await ResolveLightnessTrainingGrowthAsync(host, survivedSeconds, cancellationToken);
    }

    private async ValueTask<StoryCommandResult> RunStrengthTrainingAsync(
        IRuntimeHost host,
        CancellationToken cancellationToken)
    {
        if (host is not IMiniGameRuntimeHost miniGameHost)
        {
            throw new InvalidOperationException("Strength training requires a mini-game runtime host.");
        }

        var availableItemIds = ResolveStrengthTrainingItemCandidates();
        var (score, itemCounts) = await miniGameHost.RunStrengthTrainingAsync(availableItemIds, cancellationToken);
        await ResolveStrengthTrainingAsync(host, score, itemCounts, cancellationToken);
        return StoryCommandResult.None;
    }

    private IReadOnlyList<string> ResolveStrengthTrainingItemCandidates()
    {
        var itemIds = StrengthTrainingItemIds
            .Where(itemId => !UniqueRewardIds.Contains(itemId) || !State.MiniGame.IsUniqueRewardClaimed(itemId))
            .ToArray();
        foreach (var itemId in itemIds)
        {
            _session.ContentRepository.GetItem(itemId);
        }

        if (itemIds.Length == 0)
        {
            throw new InvalidOperationException("Strength training has no available item candidates.");
        }

        return itemIds;
    }

    private async ValueTask ResolveStrengthTrainingAsync(
        IRuntimeHost host,
        int score,
        IReadOnlyDictionary<string, int> itemCounts,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(itemCounts);

        foreach (var (itemId, quantity) in itemCounts.OrderBy(static item => item.Key, StringComparer.Ordinal))
        {
            if (quantity <= 0)
            {
                continue;
            }

            _session.InventoryService.AddItem(itemId, quantity);
            await SayAsync(host, StrengthTrainerSpeaker, $"获得{itemId} x {quantity}", cancellationToken);
            if (UniqueRewardIds.Contains(itemId))
            {
                State.MiniGame.MarkUniqueRewardClaimed(itemId);
            }
        }

        await ResolveStrengthTrainingGrowthAsync(host, score, cancellationToken);
    }

    private LightnessTrainingReward? ResolveLightnessTrainingReward(int survivedSeconds)
    {
        var tier = LightnessTrainingRewardTiers.FirstOrDefault(tier => tier.Contains(survivedSeconds));
        if (tier is null)
        {
            return null;
        }

        var candidates = tier.ItemIds
            .Where(itemId => !UniqueRewardIds.Contains(itemId) || !State.MiniGame.IsUniqueRewardClaimed(itemId))
            .ToArray();
        if (candidates.Length == 0)
        {
            return null;
        }

        return new LightnessTrainingReward(tier, candidates[Random.Shared.Next(candidates.Length)]);
    }

    private async ValueTask ResolveLightnessTrainingGrowthAsync(
        IRuntimeHost host,
        int survivedSeconds,
        CancellationToken cancellationToken)
    {
        var hero = State.Party.GetMember(Party.HeroCharacterId);
        var currentLightness = hero.GetBaseStat(StatType.Shenfa);
        if (currentLightness >= _session.Config.MiniGameMaxAttribute)
        {
            await SayAsync(host, HeroSpeaker, "貌似练这个已经没什么长进了...", cancellationToken);
            return;
        }

        var practicePoints = checked(State.MiniGame.GetPracticePoints(LightnessTrainingPracticeKey) +
            survivedSeconds * LightnessTrainingPracticeMultiplier);
        if (practicePoints >= currentLightness)
        {
            State.MiniGame.SetPracticePoints(LightnessTrainingPracticeKey, 0);
            var increase = Math.Min(
                LightnessTrainingStatIncrease,
                _session.Config.MiniGameMaxAttribute - currentLightness);
            _session.CharacterService.AddBaseStat(Party.HeroCharacterId, "shenfa", increase);
            await SayAsync(
                host,
                HeroSpeaker,
                $"你的身法进步了！身法从【{currentLightness}】提高至【{currentLightness + increase}】！",
                cancellationToken);
            return;
        }

        State.MiniGame.SetPracticePoints(LightnessTrainingPracticeKey, practicePoints);
        await SayAsync(host, HeroSpeaker, "你练习了一会儿，对轻身功夫似乎有了一些心得...", cancellationToken);
    }

    private async ValueTask ResolveStrengthTrainingGrowthAsync(
        IRuntimeHost host,
        int score,
        CancellationToken cancellationToken)
    {
        var hero = State.Party.GetMember(Party.HeroCharacterId);
        var currentStrength = hero.GetBaseStat(StatType.Bili);
        if (currentStrength >= _session.Config.MiniGameMaxAttribute)
        {
            State.MiniGame.SetPracticePoints(StrengthTrainingPracticeKey, 0);
            await SayAsync(host, HeroSpeaker, "貌似现在练这个已经没法提高臂力了。。", cancellationToken);
            return;
        }

        var practicePoints = checked(State.MiniGame.GetPracticePoints(StrengthTrainingPracticeKey) + score);
        if (practicePoints >= currentStrength)
        {
            State.MiniGame.SetPracticePoints(StrengthTrainingPracticeKey, 0);
            var increase = Math.Min(
                StrengthTrainingStatIncrease,
                _session.Config.MiniGameMaxAttribute - currentStrength);
            _session.CharacterService.AddBaseStat(Party.HeroCharacterId, "bili", increase);
            await SayAsync(
                host,
                HeroSpeaker,
                $"你的臂力进步了！臂力从【{currentStrength}】提高至【{currentStrength + increase}】！",
                cancellationToken);
            return;
        }

        State.MiniGame.SetPracticePoints(StrengthTrainingPracticeKey, practicePoints);
        await SayAsync(host, HeroSpeaker, "你练习了一会儿，对臂力功夫似乎有了一些心得...", cancellationToken);
    }

    private StoryCommandResult ExecuteTodoCommand()
    {
        _session.Events.Publish(new ToastRequestedEvent("game指令暂未实现"));
        return StoryCommandResult.None;
    }

    private static ValueTask SayAsync(
        IRuntimeHost host,
        string speaker,
        string text,
        CancellationToken cancellationToken) =>
        host.DialogueAsync(new DialogueContext(speaker, text), cancellationToken);

    private sealed record LightnessTrainingRewardTier(
        int MinSeconds,
        int? MaxSeconds,
        IReadOnlyList<string> ItemIds,
        string Dialogue)
    {
        public bool Contains(int seconds) =>
            seconds >= MinSeconds &&
            (MaxSeconds is null || seconds <= MaxSeconds.Value);
    }

    private sealed record LightnessTrainingReward(
        LightnessTrainingRewardTier Tier,
        string ItemId);
}
