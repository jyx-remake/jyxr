using System.Text.Json;
using Game.Application;
using Game.Core.Definitions;
using Game.Core.Model;
using Game.Core.Persistence;
using Game.Core.Serialization;

namespace Game.Tests;

public sealed class ShopServiceTests
{
    [Fact]
    public void Buy_SpendsSilverAddsItemAndTracksPurchaseLimit()
    {
        var herb = CreateItem("herb", price: 30);
        var shop = CreateShop("village_shop", new ShopProductDefinition
        {
            ContentId = herb.Id,
            PurchaseLimit = 2,
            Price = 40,
        });
        var session = CreateSession([herb], [shop], silver: 100);
        var publishedEvents = CollectPublishedEvents(session);

        var result = session.ShopService.Buy(shop.Id, productIndex: 0);

        Assert.True(result.Success);
        Assert.Equal(60, session.State.Currency.Silver);
        Assert.True(session.State.Inventory.ContainsStack(herb));
        Assert.Equal(1, session.State.Shop.PurchasedQuantities.Single().Value);
        Assert.Contains(publishedEvents, static sessionEvent => sessionEvent is CurrencyChangedEvent);
        Assert.Contains(publishedEvents, static sessionEvent => sessionEvent is InventoryChangedEvent);
        Assert.Contains(
            publishedEvents,
            static sessionEvent => sessionEvent is ItemAcquiredEvent { ItemId: "herb", Quantity: 1 });
    }

    [Fact]
    public void Buy_RejectsInsufficientCurrencyWithoutChangingState()
    {
        var herb = CreateItem("herb", price: 30);
        var shop = CreateShop("village_shop", new ShopProductDefinition
        {
            ContentId = herb.Id,
            Price = 40,
        });
        var session = CreateSession([herb], [shop], silver: 20);

        var result = session.ShopService.Buy(shop.Id, productIndex: 0);

        Assert.False(result.Success);
        Assert.Equal("银两不足。", result.Message);
        Assert.Equal(20, session.State.Currency.Silver);
        Assert.Empty(session.State.Inventory.Entries);
        Assert.Empty(session.State.Shop.PurchasedQuantities);
    }

    [Fact]
    public void Buy_RejectsSoldOutLimitedProduct()
    {
        var herb = CreateItem("herb", price: 30);
        var shop = CreateShop("village_shop", new ShopProductDefinition
        {
            ContentId = herb.Id,
            PurchaseLimit = 1,
            Price = 40,
        });
        var session = CreateSession([herb], [shop], silver: 100);

        Assert.True(session.ShopService.Buy(shop.Id, productIndex: 0).Success);
        var result = session.ShopService.Buy(shop.Id, productIndex: 0);

        Assert.False(result.Success);
        Assert.Equal(60, session.State.Currency.Silver);
        Assert.Equal(1, session.State.Inventory.GetStack(herb).Quantity);
    }

    [Fact]
    public void Open_IgnoresGoldAndFragmentProducts()
    {
        var herb = CreateItem("herb", price: 30);
        var shop = CreateShop(
            "village_shop",
            new ShopProductDefinition
            {
                ContentId = "元宝",
                Price = 1000,
            },
            new ShopProductDefinition
            {
                ContentId = "降龙十八掌残章",
                PremiumPrice = 4,
            },
            new ShopProductDefinition
            {
                ContentId = herb.Id,
                Price = 40,
            });
        var session = CreateSession([herb], [shop], silver: 100);

        var view = session.ShopService.Open(shop.Id);

        var product = Assert.Single(view.Products);
        Assert.Equal(2, product.ProductIndex);
        Assert.Equal("herb", product.Item.Id);
        Assert.False(session.ShopService.Buy(shop.Id, productIndex: 0).Success);
        Assert.False(session.ShopService.Buy(shop.Id, productIndex: 1).Success);
    }

    [Fact]
    public void Sell_RemovesInventoryEntryAndAddsSilver()
    {
        var herb = CreateItem("herb", price: 30);
        var shop = CreateShop("village_shop", new ShopProductDefinition
        {
            ContentId = herb.Id,
            Price = 40,
        });
        var session = CreateSession([herb], [shop], silver: 0);
        var entry = session.State.Inventory.AddItem(herb, 2);
        var publishedEvents = CollectPublishedEvents(session);

        var result = session.ShopService.Sell(entry);

        Assert.True(result.Success);
        Assert.Equal(15, session.State.Currency.Silver);
        Assert.Equal(1, session.State.Inventory.GetStack(herb).Quantity);
        Assert.Contains(publishedEvents, static sessionEvent => sessionEvent is CurrencyChangedEvent);
        Assert.Contains(publishedEvents, static sessionEvent => sessionEvent is InventoryChangedEvent);
    }

    [Fact]
    public void SaveGame_RoundTripsShopPurchaseState()
    {
        var herb = CreateItem("herb", price: 30);
        var shop = CreateShop("village_shop", new ShopProductDefinition
        {
            ContentId = herb.Id,
            PurchaseLimit = 2,
            Price = 40,
        });
        var session = CreateSession([herb], [shop], silver: 100);
        session.ShopService.Buy(shop.Id, productIndex: 0);

        var saveGame = session.SaveGameService.CreateSave();
        var json = JsonSerializer.Serialize(saveGame, GameJson.Default);
        var roundTripped = JsonSerializer.Deserialize<SaveGame>(json, GameJson.Default);

        Assert.NotNull(roundTripped);
        var restoredShop = roundTripped!.RestoreShopState();
        Assert.Equal(1, restoredShop.PurchasedQuantities.Single().Value);
    }

    private static GameSession CreateSession(
        IReadOnlyList<ItemDefinition> items,
        IReadOnlyList<ShopDefinition> shops,
        int silver)
    {
        var repository = TestContentFactory.CreateRepository(items: items, shops: shops);
        var state = new GameState();
        state.Currency.AddSilver(silver);
        return new GameSession(state, repository);
    }

    private static NormalItemDefinition CreateItem(string id, int price) =>
        new()
        {
            Id = id,
            Name = id,
            Type = ItemType.Consumable,
            Price = price,
            CanDrop = true,
        };

    private static ShopDefinition CreateShop(string id, params ShopProductDefinition[] products) =>
        new()
        {
            Id = id,
            Name = id,
            Products = products,
        };

    private static List<object> CollectPublishedEvents(GameSession session)
    {
        var publishedEvents = new List<object>();
        session.Events.SubscribeAll(publishedEvents.Add);
        return publishedEvents;
    }
}
