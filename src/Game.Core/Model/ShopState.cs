using Game.Core.Persistence;

namespace Game.Core.Model;

public sealed class ShopState
{
    private readonly Dictionary<string, int> _purchasedQuantities = new(StringComparer.Ordinal);

    public IReadOnlyDictionary<string, int> PurchasedQuantities => _purchasedQuantities;

    public static ShopState Restore(ShopStateRecord? record)
    {
        var state = new ShopState();
        if (record is null)
        {
            return state;
        }

        foreach (var (key, quantity) in record.PurchasedQuantities)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(key);
            ArgumentOutOfRangeException.ThrowIfNegative(quantity);
            if (quantity > 0)
            {
                state._purchasedQuantities.Add(key, quantity);
            }
        }

        return state;
    }

    public int GetPurchasedQuantity(string purchaseKey)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(purchaseKey);
        return _purchasedQuantities.GetValueOrDefault(purchaseKey);
    }

    public void AddPurchasedQuantity(string purchaseKey, int quantity)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(purchaseKey);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(quantity);
        _purchasedQuantities[purchaseKey] = checked(GetPurchasedQuantity(purchaseKey) + quantity);
    }

    public ShopStateRecord ToRecord() =>
        new(_purchasedQuantities
            .OrderBy(static entry => entry.Key, StringComparer.Ordinal)
            .ToDictionary(static entry => entry.Key, static entry => entry.Value, StringComparer.Ordinal));
}
