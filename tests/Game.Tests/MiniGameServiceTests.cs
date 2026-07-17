using Game.Application;
using Game.Core.Definitions;
using Game.Core.Model;
using Game.Core.Model.Character;
using Game.Core.Story;

namespace Game.Tests;

public sealed class MiniGameServiceTests
{
    [Fact]
    public async Task StoryCommandDispatcher_GameLightnessTrainingScriptArgumentRunsLightnessTraining()
    {
        var session = CreateSession(heroShenfa: 30);
        var host = new LightnessTrainingHost(5);
        var dispatcher = new StoryCommandDispatcher(session, host);

        await dispatcher.ExecuteCommandAsync("game", [ExprValue.FromString("qinggong")], default);

        Assert.Equal(1, host.RunCount);
        Assert.Contains(session.State.Inventory.Entries.OfType<StackInventoryEntry>(), entry =>
            entry.Definition.Id == "特制鸡腿" &&
            entry.Quantity == 1);
        Assert.Contains(host.Dialogues, dialogue => dialogue.Text == "你坚持了5秒！");
    }

    [Fact]
    public async Task LightnessTraining_AccumulatesPracticeAndIncreasesShenfaWhenThresholdReached()
    {
        var session = CreateSession(heroShenfa: 30);
        var dispatcher = new StoryCommandDispatcher(session, new LightnessTrainingHost(14));

        await dispatcher.ExecuteCommandAsync("game", [ExprValue.FromString("qinggong")], default);

        var hero = session.State.Party.GetMember(Party.HeroCharacterId);
        Assert.Equal(30, hero.GetBaseStat(StatType.Shenfa));
        Assert.Equal(28, session.State.MiniGame.GetPracticePoints("lightness_training"));

        dispatcher = new StoryCommandDispatcher(session, new LightnessTrainingHost(1));
        await dispatcher.ExecuteCommandAsync("game", [ExprValue.FromString("qinggong")], default);

        Assert.Equal(35, hero.GetBaseStat(StatType.Shenfa));
        Assert.Equal(0, session.State.MiniGame.GetPracticePoints("lightness_training"));
    }

    [Fact]
    public async Task LightnessTraining_DoesNotIncreaseShenfaAtMiniGameCap()
    {
        var session = CreateSession(heroShenfa: 70);
        var dispatcher = new StoryCommandDispatcher(session, new LightnessTrainingHost(30));

        await dispatcher.ExecuteCommandAsync("game", [ExprValue.FromString("qinggong")], default);

        var hero = session.State.Party.GetMember(Party.HeroCharacterId);
        Assert.Equal(70, hero.GetBaseStat(StatType.Shenfa));
        Assert.Equal(0, session.State.MiniGame.GetPracticePoints("lightness_training"));
    }

    [Fact]
    public async Task LightnessTraining_ClampsShenfaIncreaseAtMiniGameCap()
    {
        var session = CreateSession(heroShenfa: 68);
        var dispatcher = new StoryCommandDispatcher(session, new LightnessTrainingHost(34));

        await dispatcher.ExecuteCommandAsync("game", [ExprValue.FromString("qinggong")], default);

        var hero = session.State.Party.GetMember(Party.HeroCharacterId);
        Assert.Equal(70, hero.GetBaseStat(StatType.Shenfa));
        Assert.Equal(0, session.State.MiniGame.GetPracticePoints("lightness_training"));
    }

    [Fact]
    public async Task LightnessTraining_SkipsClaimedUniqueRewards()
    {
        var session = CreateSession(heroShenfa: 80);
        foreach (var itemId in new[] { "凌波微步图谱", "天下轻功总决" })
        {
            session.State.MiniGame.MarkUniqueRewardClaimed(itemId);
        }

        for (var index = 0; index < 20; index += 1)
        {
            var dispatcher = new StoryCommandDispatcher(session, new LightnessTrainingHost(23));
            await dispatcher.ExecuteCommandAsync("game", [ExprValue.FromString("qinggong")], default);
        }

        Assert.DoesNotContain(session.State.Inventory.Entries.OfType<StackInventoryEntry>(), entry =>
            entry.Definition.Id is "凌波微步图谱" or "天下轻功总决");
    }

    [Fact]
    public async Task StoryCommandDispatcher_GameStrengthTrainingScriptArgumentRunsStrengthTraining()
    {
        var session = CreateSession(heroShenfa: 30, heroBili: 20);
        var host = new LightnessTrainingHost(0)
        {
            StrengthScore = 6,
            StrengthItemCounts = new Dictionary<string, int>
            {
                ["大还丹"] = 2,
                ["柳叶刀"] = 1,
            },
        };
        var dispatcher = new StoryCommandDispatcher(session, host);

        await dispatcher.ExecuteCommandAsync("game", [ExprValue.FromString("dianxue")], default);

        Assert.Equal(1, host.StrengthRunCount);
        Assert.Contains("大还丹", host.StrengthItemCandidates);
        Assert.Contains(session.State.Inventory.Entries.OfType<StackInventoryEntry>(), entry =>
            entry.Definition.Id == "大还丹" &&
            entry.Quantity == 2);
        Assert.Contains(session.State.Inventory.Entries.OfType<StackInventoryEntry>(), entry =>
            entry.Definition.Id == "柳叶刀" &&
            entry.Quantity == 1);
        Assert.True(session.State.MiniGame.IsUniqueRewardClaimed("柳叶刀"));
    }

    [Fact]
    public async Task StrengthTraining_AccumulatesPracticeAndIncreasesBiliWhenThresholdReached()
    {
        var session = CreateSession(heroShenfa: 30, heroBili: 10);
        var dispatcher = new StoryCommandDispatcher(session, new LightnessTrainingHost(0) { StrengthScore = 9 });

        await dispatcher.ExecuteCommandAsync("game", [ExprValue.FromString("dianxue")], default);

        var hero = session.State.Party.GetMember(Party.HeroCharacterId);
        Assert.Equal(10, hero.GetBaseStat(StatType.Bili));
        Assert.Equal(9, session.State.MiniGame.GetPracticePoints("strength_training"));

        dispatcher = new StoryCommandDispatcher(session, new LightnessTrainingHost(0) { StrengthScore = 1 });
        await dispatcher.ExecuteCommandAsync("game", [ExprValue.FromString("dianxue")], default);

        Assert.Equal(15, hero.GetBaseStat(StatType.Bili));
        Assert.Equal(0, session.State.MiniGame.GetPracticePoints("strength_training"));
    }

    [Fact]
    public async Task StrengthTraining_PreservesNegativePracticeFromPenaltyScore()
    {
        var session = CreateSession(heroShenfa: 30, heroBili: 20);
        var dispatcher = new StoryCommandDispatcher(session, new LightnessTrainingHost(0) { StrengthScore = -2 });

        await dispatcher.ExecuteCommandAsync("game", [ExprValue.FromString("dianxue")], default);

        Assert.Equal(-2, session.State.MiniGame.GetPracticePoints("strength_training"));
    }

    [Fact]
    public async Task StrengthTraining_DoesNotIncreaseBiliAtMiniGameCap()
    {
        var session = CreateSession(heroShenfa: 30, heroBili: 70);
        session.State.MiniGame.SetPracticePoints("strength_training", 12);
        var dispatcher = new StoryCommandDispatcher(session, new LightnessTrainingHost(0) { StrengthScore = 30 });

        await dispatcher.ExecuteCommandAsync("game", [ExprValue.FromString("dianxue")], default);

        var hero = session.State.Party.GetMember(Party.HeroCharacterId);
        Assert.Equal(70, hero.GetBaseStat(StatType.Bili));
        Assert.Equal(0, session.State.MiniGame.GetPracticePoints("strength_training"));
    }

    [Fact]
    public async Task StrengthTraining_ClampsBiliIncreaseAtMiniGameCap()
    {
        var session = CreateSession(heroShenfa: 30, heroBili: 68);
        var dispatcher = new StoryCommandDispatcher(session, new LightnessTrainingHost(0) { StrengthScore = 68 });

        await dispatcher.ExecuteCommandAsync("game", [ExprValue.FromString("dianxue")], default);

        var hero = session.State.Party.GetMember(Party.HeroCharacterId);
        Assert.Equal(70, hero.GetBaseStat(StatType.Bili));
        Assert.Equal(0, session.State.MiniGame.GetPracticePoints("strength_training"));
    }

    [Fact]
    public async Task StrengthTraining_SkipsClaimedUniqueItemCandidates()
    {
        var session = CreateSession(heroShenfa: 30, heroBili: 20);
        session.State.MiniGame.MarkUniqueRewardClaimed("柳叶刀");
        session.State.MiniGame.MarkUniqueRewardClaimed("金丝道袍");
        session.State.MiniGame.MarkUniqueRewardClaimed("黄金项链");
        session.State.MiniGame.MarkUniqueRewardClaimed("乌蚕衣");
        var host = new LightnessTrainingHost(0);
        var dispatcher = new StoryCommandDispatcher(session, host);

        await dispatcher.ExecuteCommandAsync("game", [ExprValue.FromString("dianxue")], default);

        Assert.DoesNotContain("柳叶刀", host.StrengthItemCandidates);
        Assert.DoesNotContain("金丝道袍", host.StrengthItemCandidates);
        Assert.DoesNotContain("黄金项链", host.StrengthItemCandidates);
        Assert.DoesNotContain("乌蚕衣", host.StrengthItemCandidates);
        Assert.Contains("大还丹", host.StrengthItemCandidates);
    }

    private static GameSession CreateSession(int heroShenfa)
        => CreateSession(heroShenfa, heroBili: 30);

    private static GameSession CreateSession(int heroShenfa, int heroBili)
    {
        var heroDefinition = TestContentFactory.CreateCharacterDefinition(
            Party.HeroCharacterId,
            new Dictionary<StatType, int>
            {
                [StatType.Shenfa] = heroShenfa,
                [StatType.Bili] = heroBili,
            });
        var hero = TestContentFactory.CreateCharacterInstance(Party.HeroCharacterId, heroDefinition);
        var party = new Party();
        party.AddMember(hero);
        var state = new GameState();
        state.SetParty(party);
        var repository = TestContentFactory.CreateRepository(
            characters: [heroDefinition],
            items: CreateRewardItems());
        return new GameSession(
            state,
            repository,
            config: new GameConfig { MiniGameMaxAttribute = 70 });
    }

    private static IReadOnlyList<NormalItemDefinition> CreateRewardItems()
    {
        var itemIds = new[]
        {
            "特制鸡腿",
            "冬虫夏草",
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
            "生生造化丹",
            "黑玉断续膏",
            "君子剑",
            "淑女剑",
            "天王保命丹",
            "乌蚕衣",
            "凌波微步图谱",
            "天下轻功总决",
            "大还丹",
            "大蟠桃",
            "九转熊蛇丸",
            "黄金项链",
            "血刀",
        };
        return itemIds
            .Select(static itemId => new NormalItemDefinition
            {
                Id = itemId,
                Name = itemId,
                Type = ItemType.Consumable,
                ConsumeOnUse = true,
            })
            .ToArray();
    }

    private sealed class LightnessTrainingHost : IRuntimeHost, IMiniGameRuntimeHost
    {
        private readonly int _survivedSeconds;

        public LightnessTrainingHost(int survivedSeconds)
        {
            _survivedSeconds = survivedSeconds;
        }

        public int RunCount { get; private set; }

        public int StrengthRunCount { get; private set; }

        public int StrengthScore { get; init; }

        public IReadOnlyDictionary<string, int> StrengthItemCounts { get; init; } =
            new Dictionary<string, int>();

        public IReadOnlyList<string> StrengthItemCandidates { get; private set; } = [];

        public List<DialogueContext> Dialogues { get; } = [];

        public ValueTask<int> RunLightnessTrainingAsync(CancellationToken cancellationToken)
        {
            RunCount++;
            return ValueTask.FromResult(_survivedSeconds);
        }

        public ValueTask<(int Score, IReadOnlyDictionary<string, int> ItemCounts)> RunStrengthTrainingAsync(
            IReadOnlyList<string> itemIds,
            CancellationToken cancellationToken)
        {
            StrengthRunCount++;
            StrengthItemCandidates = itemIds.ToArray();
            return ValueTask.FromResult((StrengthScore, StrengthItemCounts));
        }

        public ValueTask DialogueAsync(DialogueContext dialogue, CancellationToken cancellationToken)
        {
            Dialogues.Add(dialogue);
            return ValueTask.CompletedTask;
        }

        public ValueTask<ExprValue> GetVariableAsync(string name, CancellationToken cancellationToken) =>
            ValueTask.FromException<ExprValue>(new InvalidOperationException($"Unknown variable '{name}'."));

        public ValueTask<bool> EvaluatePredicateAsync(
            string name,
            IReadOnlyList<ExprValue> args,
            CancellationToken cancellationToken) =>
            ValueTask.FromException<bool>(new InvalidOperationException($"Unknown predicate '{name}'."));

        public ValueTask<StoryCommandResult> ExecuteCommandAsync(
            string name,
            IReadOnlyList<ExprValue> args,
            CancellationToken cancellationToken) =>
            ValueTask.FromException<StoryCommandResult>(new InvalidOperationException($"Host command '{name}' should not be invoked."));

        public ValueTask<int> ChooseOptionAsync(ChoiceContext choice, CancellationToken cancellationToken) =>
            ValueTask.FromException<int>(new InvalidOperationException("Choice UI should not be invoked."));

        public ValueTask<BattleOutcome> ResolveBattleAsync(BattleContext battle, CancellationToken cancellationToken) =>
            ValueTask.FromException<BattleOutcome>(new InvalidOperationException("Battle resolution should not be invoked."));
    }
}
