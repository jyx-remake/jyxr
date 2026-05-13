using Game.Application;
using Game.Core.Definitions;
using Game.Core.Model;
using Game.Core.Story;

namespace Game.Tests;

public sealed class SpecialBattleServiceTests
{
    [Fact]
    public async Task TowerRewardClaimsAreScopedByTowerStageAndReward()
    {
        var reward = CreateItem("rare_reward");
        var tower = CreateTwoStageTower(reward.Id);
        var session = CreateSession(tower, reward);
        var host = new TowerRuntimeHost(
            [["hero"], ["ally"]],
            [true, true]);

        await session.SpecialBattleService.RunTowerAsync(host);

        var entry = Assert.Single(session.State.Inventory.Entries.OfType<StackInventoryEntry>());
        Assert.Equal(reward.Id, entry.Item.Id);
        Assert.Equal(2, entry.Quantity);
        Assert.Equal(1, session.State.SpecialBattle.GetTowerRewardClaimCount("tower", "stage_a", "rare_reward"));
        Assert.Equal(1, session.State.SpecialBattle.GetTowerRewardClaimCount("tower", "stage_b", "rare_reward"));
    }

    [Fact]
    public async Task TowerRewardClaimsAreOnlyConsumedWhenRewardsAreGranted()
    {
        var reward = CreateItem("rare_reward");
        var tower = CreateTwoStageTower(reward.Id);
        var session = CreateSession(tower, reward);
        var host = new TowerRuntimeHost(
            [["hero"], ["ally"]],
            [true, false]);

        await session.SpecialBattleService.RunTowerAsync(host);

        Assert.Empty(session.State.Inventory.Entries);
        Assert.Empty(session.State.SpecialBattle.TowerRewardClaimCounts);
    }

    private static GameSession CreateSession(TowerDefinition tower, params ItemDefinition[] items)
    {
        var heroDefinition = TestContentFactory.CreateCharacterDefinition("hero");
        var allyDefinition = TestContentFactory.CreateCharacterDefinition("ally");
        var state = new GameState();
        state.Party.AddMember(TestContentFactory.CreateCharacterInstance("hero", heroDefinition));
        state.Party.AddMember(TestContentFactory.CreateCharacterInstance("ally", allyDefinition));

        var repository = TestContentFactory.CreateRepository(
            characters: [heroDefinition, allyDefinition],
            items: items,
            battles:
            [
                CreateBattle("battle_a"),
                CreateBattle("battle_b"),
            ],
            towers: [tower]);
        return new GameSession(state, repository);
    }

    private static TowerDefinition CreateTwoStageTower(string rewardId) =>
        new()
        {
            Id = "tower",
            Name = "tower",
            Stages =
            [
                CreateStage("stage_a", "battle_a", rewardId, 0),
                CreateStage("stage_b", "battle_b", rewardId, 1),
            ],
        };

    private static TowerStageDefinition CreateStage(
        string id,
        string battleId,
        string rewardId,
        int index) =>
        new()
        {
            Id = id,
            Name = id,
            BattleId = battleId,
            Index = index,
            Rewards =
            [
                new TowerRewardDefinition
                {
                    ContentId = rewardId,
                    Probability = 1d,
                    MaxClaims = 1,
                },
            ],
        };

    private static BattleDefinition CreateBattle(string id) =>
        new()
        {
            Id = id,
            Name = id,
            MapId = "map",
        };

    private static NormalItemDefinition CreateItem(string id) =>
        new()
        {
            Id = id,
            Name = id,
            Type = ItemType.Consumable,
            CanDrop = true,
        };

    private sealed class TowerRuntimeHost : IRuntimeHost, ISpecialBattleRuntimeHost
    {
        private readonly Queue<IReadOnlyList<string>> _selectedCharacterIds;
        private readonly Queue<bool> _battleResults;

        public TowerRuntimeHost(
            IEnumerable<IReadOnlyList<string>> selectedCharacterIds,
            IEnumerable<bool> battleResults)
        {
            _selectedCharacterIds = new Queue<IReadOnlyList<string>>(selectedCharacterIds);
            _battleResults = new Queue<bool>(battleResults);
        }

        public ValueTask DialogueAsync(DialogueContext dialogue, CancellationToken cancellationToken) =>
            ValueTask.CompletedTask;

        public ValueTask<int> ChooseOptionAsync(ChoiceContext choice, CancellationToken cancellationToken) =>
            ValueTask.FromResult(0);

        public ValueTask<BattleOutcome> ResolveBattleAsync(BattleContext battle, CancellationToken cancellationToken) =>
            ValueTask.FromResult(BattleOutcome.Win);

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
            ValueTask.FromResult(StoryCommandResult.None);

        public ValueTask<IReadOnlyList<string>> SelectCombatantsAsync(
            CombatantSelectionRequest request,
            CancellationToken cancellationToken) =>
            ValueTask.FromResult(_selectedCharacterIds.Dequeue());

        public ValueTask<bool> RunBattleAsync(
            SpecialBattleRequest request,
            CancellationToken cancellationToken) =>
            ValueTask.FromResult(_battleResults.Dequeue());
    }
}
