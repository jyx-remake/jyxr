using Game.Application;
using Game.Core.Affix;
using Game.Core.Definitions;
using Game.Core.Model;
using Game.Core.Persistence;
using Game.Core.Story;
using Game.Expressions;

namespace Game.Tests;

public sealed class EquipmentRefinementServiceTests
{
    [Fact]
    public async Task Add_PreservesExistingGroupAndAddsWholeCombo()
    {
        var (session, definition) = CreateSession();
        var original = new StatModifierAffix(StatType.Speed, ModifierValue.Add(2));
        var equipment = session.State.EquipmentInstanceFactory.Create(definition, [original]);
        session.State.Inventory.AddEquipmentInstance(equipment);
        var host = new Host(1, 0, 3);

        await session.EquipmentRefinementService.RunAsync(host);

        Assert.Equal(2, EquipmentAffixGroups.Count(equipment.ExtraAffixes));
        Assert.Same(original, equipment.ExtraAffixes[0]);
        Assert.Equal(3, equipment.ExtraAffixes.Count);
        Assert.Equal(9, session.Profile.Yuanbao);
        Assert.Equal(9, host.Choices[1].Options.Count);
        Assert.Equal(1, host.Effects);
    }

    [Fact]
    public async Task Add_ToPlainStackSplitsExactlyOneItemAndPersistsAffixes()
    {
        var (session, definition) = CreateSession();
        session.State.Inventory.AddItem(definition, 2);

        await session.EquipmentRefinementService.RunAsync(new Host(0, 0, 2));

        Assert.Equal(1, session.State.Inventory.GetStack(definition).Quantity);
        var entry = Assert.Single(session.State.Inventory.Entries.OfType<EquipmentInstanceInventoryEntry>());
        Assert.Equal(1, EquipmentAffixGroups.Count(entry.Equipment.ExtraAffixes));
        var restored = InventoryMapper.FromRecord(InventoryMapper.ToRecord(session.State.Inventory), session.ContentRepository);
        var restoredEquipment = Assert.Single(restored.Entries.OfType<EquipmentInstanceInventoryEntry>()).Equipment;
        Assert.Equal(entry.Equipment.Id, restoredEquipment.Id);
        Assert.Equal(entry.Equipment.ExtraAffixes, restoredEquipment.ExtraAffixes);
    }

    [Fact]
    public async Task CancelCandidate_ChargesRollButDoesNotSplitPlainStack()
    {
        var (session, definition) = CreateSession();
        var stack = session.State.Inventory.AddItem(definition, 2);
        var nextNumber = session.State.EquipmentInstanceFactory.NextNumber;

        await session.EquipmentRefinementService.RunAsync(new Host(0, 8, 1));

        Assert.Same(stack, Assert.Single(session.State.Inventory.Entries));
        Assert.Equal(2, stack.Quantity);
        Assert.Equal(nextNumber, session.State.EquipmentInstanceFactory.NextNumber);
        Assert.Equal(9, session.Profile.Yuanbao);
    }

    [Fact]
    public async Task ExhaustedPool_DoesNotChargeOrOfferCandidates()
    {
        var (session, definition) = CreateSession();
        session.State.Inventory.AddItem(definition);
        var host = new Host(0, 0, 1);

        await session.EquipmentRefinementService.RunAsync(host);

        Assert.Equal(3, host.Choices.Count);
        Assert.Equal(9, session.Profile.Yuanbao);
        Assert.Equal(1, EquipmentAffixGroups.Count(Assert.Single(
            session.State.Inventory.Entries.OfType<EquipmentInstanceInventoryEntry>()).Equipment.ExtraAffixes));
    }

    [Fact]
    public async Task InsufficientCurrency_DoesNotChangeEquipment()
    {
        var (session, definition) = CreateSession();
        var stack = session.State.Inventory.AddItem(definition);
        session.Profile.SetYuanbao(0);

        var result = await session.EquipmentRefinementService.RunAsync(new Host(0));

        Assert.Equal("洗练元宝不够", result.JumpTarget);
        Assert.Same(stack, Assert.Single(session.State.Inventory.Entries));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    public async Task StateReplacedDuringChoice_DiscardsStaleResult(int choiceIndex)
    {
        var (session, definition) = CreateSession();
        var oldState = session.State;
        var stack = oldState.Inventory.AddItem(definition);
        var host = new Host(0, 0)
        {
            OnChoice = index => { if (index == choiceIndex) session.ReplaceState(new GameState()); },
        };

        await session.EquipmentRefinementService.RunAsync(host);

        Assert.Same(stack, Assert.Single(oldState.Inventory.Entries));
        Assert.Empty(session.State.Inventory.Entries);
        Assert.Equal(choiceIndex == 0 ? 10 : 9, session.Profile.Yuanbao);
        Assert.Equal(0, host.Effects);
    }

    [Fact]
    public async Task Replace_StillReplacesOneWholeGroup()
    {
        var (session, definition) = CreateSession();
        var equipment = session.State.EquipmentInstanceFactory.Create(definition,
            [new StatModifierAffix(StatType.Speed, ModifierValue.Add(2))]);
        session.State.Inventory.AddEquipmentInstance(equipment);

        await session.EquipmentRefinementService.RunAsync(new Host(0, 0, 2));

        Assert.Equal(1, EquipmentAffixGroups.Count(equipment.ExtraAffixes));
        Assert.Equal(2, equipment.ExtraAffixes.Count);
        Assert.Equal(9, session.Profile.Yuanbao);
    }

    [Fact]
    public async Task Add_FourGroupsBecomeFiveEvenWhenNewGroupContainsTwoAffixes()
    {
        var (session, definition) = CreateSession();
        var original = CreateFourGroups();
        var equipment = session.State.EquipmentInstanceFactory.Create(definition, original);
        session.State.Inventory.AddEquipmentInstance(equipment);
        var host = new Host(4, 0, 5);

        await session.EquipmentRefinementService.RunAsync(host);

        Assert.Equal(5, EquipmentAffixGroups.Count(equipment.ExtraAffixes));
        Assert.Equal(6, equipment.ExtraAffixes.Count);
        Assert.Equal(original, equipment.ExtraAffixes.Take(4));
        Assert.DoesNotContain(host.Choices[2].Options, option => option.Text.StartsWith("增加"));
        Assert.Equal("退出洗练", host.Choices[2].Options[5].Text);
        Assert.Equal(9, session.Profile.Yuanbao);
    }

    [Fact]
    public async Task AtFiveGroups_StillAllowsReplacementWithoutIncreasingCount()
    {
        var (session, definition) = CreateSession();
        var equipment = session.State.EquipmentInstanceFactory.Create(definition,
            [.. CreateFourGroups(), new StatModifierAffix(StatType.CritMult, ModifierValue.Add(0.1))]);
        session.State.Inventory.AddEquipmentInstance(equipment);
        var host = new Host(0, 0, 5);

        await session.EquipmentRefinementService.RunAsync(host);

        Assert.Equal(5, EquipmentAffixGroups.Count(equipment.ExtraAffixes));
        Assert.DoesNotContain(host.Choices[0].Options, option => option.Text.StartsWith("增加"));
        Assert.Equal(9, session.Profile.Yuanbao);
        Assert.Equal(1, host.Effects);
    }

    [Theory]
    [InlineData(5)]
    [InlineData(6)]
    public async Task AtOrAboveLimit_OnlyOffersReplacementAndExitWithoutCharging(int count)
    {
        var (session, definition) = CreateSession();
        var affixes = Enumerable.Range(0, count)
            .Select(index => (AffixDefinition)new StatModifierAffix(StatType.Speed, ModifierValue.Add(index + 1))).ToArray();
        var equipment = session.State.EquipmentInstanceFactory.Create(definition, affixes);
        session.State.Inventory.AddEquipmentInstance(equipment);
        var host = new Host(count);

        await session.EquipmentRefinementService.RunAsync(host);

        Assert.Equal(count + 1, Assert.Single(host.Choices).Options.Count);
        Assert.DoesNotContain(host.Choices[0].Options, option => option.Text.StartsWith("增加"));
        Assert.Equal(affixes, equipment.ExtraAffixes);
        Assert.Equal(10, session.Profile.Yuanbao);
    }

    [Theory]
    [InlineData("profile")]
    [InlineData("removed")]
    [InlineData("affixes")]
    public async Task PendingCandidate_DiscardsResultWhenSelectionIsInvalidated(string change)
    {
        var (session, definition) = CreateSession();
        var original = new StatModifierAffix(StatType.Speed, ModifierValue.Add(2));
        var equipment = session.State.EquipmentInstanceFactory.Create(definition, [original]);
        session.State.Inventory.AddEquipmentInstance(equipment);
        var pending = new TaskCompletionSource<int>(TaskCreationOptions.RunContinuationsAsynchronously);
        var host = new Host(1) { PendingCandidate = pending.Task };
        var run = session.EquipmentRefinementService.RunAsync(host).AsTask();
        Assert.False(run.IsCompleted);
        Assert.Equal(9, session.Profile.Yuanbao);

        var replacement = new StatModifierAffix(StatType.Accuracy, ModifierValue.Add(0.1));
        switch (change)
        {
            case "profile": session.ReplaceProfile(new GameProfile()); break;
            case "removed": session.State.Inventory.RemoveEquipmentInstance(equipment.Id); break;
            case "affixes": equipment.ReplaceExtraAffixes([replacement]); break;
        }
        pending.SetResult(0);
        await run;

        Assert.Same(change == "affixes" ? replacement : original, Assert.Single(equipment.ExtraAffixes));
        Assert.Equal(0, host.Effects);
        if (change == "profile") Assert.Equal(0, session.Profile.Yuanbao);
    }

    private static AffixDefinition[] CreateFourGroups() =>
    [
        new StatModifierAffix(StatType.Speed, ModifierValue.Add(2)),
        new StatModifierAffix(StatType.Accuracy, ModifierValue.Add(0.1)),
        new StatModifierAffix(StatType.Lifesteal, ModifierValue.Add(0.1)),
        new StatModifierAffix(StatType.AntiDebuff, ModifierValue.Add(0.1)),
    ];

    private static (GameSession Session, EquipmentDefinition Definition) CreateSession()
    {
        var definition = TestContentFactory.CreateEquipment("sword");
        var parser = new ExpressionParser();
        var range = new EquipmentRandomAffixRangeDefinition
        {
            Min = parser.ParseExpression("1"), Max = parser.ParseExpression("1"),
        };
        var repository = TestContentFactory.CreateRepository(equipmentRandomAffixTables:
        [
            new EquipmentRandomAffixTableDefinition
            {
                Id = "test", When = parser.ParseExpression("true"),
                Options = [new EquipmentRandomAffixOptionDefinition
                {
                    Kind = EquipmentRandomAffixKind.AttackCombo, Weight = 1, Ranges = [range, range],
                }],
            },
        ], equipment: [definition]);
        var session = new GameSession(new GameState(), repository);
        session.Profile.SetYuanbao(10);
        return (session, definition);
    }

    private sealed class Host(params int[] selections) : IRuntimeHost, IApplicationRuntimeHost
    {
        private readonly Queue<int> _selections = new(selections);
        public List<ChoiceContext> Choices { get; } = [];
        public Action<int>? OnChoice { get; init; }
        public Task<int>? PendingCandidate { get; init; }
        public int Effects { get; private set; }

        public ValueTask<InventoryEntry?> SelectRefinementEquipmentAsync(
            IReadOnlyList<InventoryEntry> entries, CancellationToken cancellationToken) =>
            ValueTask.FromResult<InventoryEntry?>(entries[0]);

        public ValueTask<int> ChooseOptionAsync(ChoiceContext choice, CancellationToken cancellationToken)
        {
            OnChoice?.Invoke(Choices.Count);
            Choices.Add(choice);
            if (Choices.Count == 2 && PendingCandidate is not null)
            {
                return new ValueTask<int>(PendingCandidate);
            }
            return ValueTask.FromResult(_selections.Dequeue());
        }

        public ValueTask PlayEffectAsync(string effectId, CancellationToken cancellationToken)
        {
            Effects++;
            return ValueTask.CompletedTask;
        }

        public ValueTask DialogueAsync(DialogueContext dialogue, CancellationToken cancellationToken) => throw new NotSupportedException();
        public ValueTask<BattleOutcome> ResolveBattleAsync(BattleContext battle, CancellationToken cancellationToken) => throw new NotSupportedException();
    }
}
