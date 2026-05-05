namespace Game.Core.Persistence;

public sealed record ShopStateRecord(
    IReadOnlyDictionary<string, int> PurchasedQuantities);
