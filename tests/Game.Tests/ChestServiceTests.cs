using Game.Application;
using Game.Core.Definitions;
using Game.Core.Model;

namespace Game.Tests;

public sealed class ChestServiceTests
{
    private static GameSession CreateSession()
    {
        var repository = TestContentFactory.CreateRepository();
        return new GameSession(new GameState(), repository);
    }

    private static NormalItemDefinition CreateItem(ItemType type, bool? allowChestStorage) => new()
    {
        Id = "item",
        Name = "item",
        Type = type,
        ConsumeOnUse = false,
        AllowChestStorage = allowChestStorage,
    };

    [Fact]
    public void CanStore_DefaultsToTypeRule()
    {
        var session = CreateSession();

        Assert.True(session.ChestService.CanStore(CreateItem(ItemType.Consumable, null)));
        Assert.False(session.ChestService.CanStore(CreateItem(ItemType.QuestItem, null)));
    }

    [Fact]
    public void CanStore_ChestOverrideWinsOverTypeRule()
    {
        var session = CreateSession();

        Assert.False(session.ChestService.CanStore(CreateItem(ItemType.Consumable, false)));
        Assert.True(session.ChestService.CanStore(CreateItem(ItemType.QuestItem, true)));
    }
}
