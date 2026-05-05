using Game.Core.Definitions;
using Game.Core.Model;

namespace Game.Application;

public sealed class ShopService
{
    private const decimal SellPriceRatio = 0.5m;
    private const string GoldProductContentId = "元宝";
    private readonly GameSession _session;

    public ShopService(GameSession session)
    {
        ArgumentNullException.ThrowIfNull(session);
        _session = session;
    }

    private GameState State => _session.State;

    public ShopView Open(string shopId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(shopId);

        var shop = _session.ContentRepository.GetShop(shopId);
        var products = shop.Products
            .Select((product, index) => (Product: product, Index: index))
            .Where(entry => !ShouldIgnoreProduct(entry.Product.ContentId))
            .Select(entry => CreateProductView(shop.Id, entry.Index, entry.Product))
            .ToList();

        return new ShopView(shop, products);
    }

    public ShopTransactionResult Buy(
        string shopId,
        int productIndex,
        int quantity = 1,
        ShopCurrencyKind? currencyKind = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(shopId);
        ArgumentOutOfRangeException.ThrowIfNegative(productIndex);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(quantity);

        var shop = _session.ContentRepository.GetShop(shopId);
        if (productIndex >= shop.Products.Count)
        {
            throw new InvalidOperationException($"Shop '{shopId}' has no product at index {productIndex}.");
        }

        var productDefinition = shop.Products[productIndex];
        if (ShouldIgnoreProduct(productDefinition.ContentId))
        {
            return ShopTransactionResult.Failed($"【{productDefinition.ContentId}】当前不作为商店商品处理。");
        }

        var product = CreateProductView(shop.Id, productIndex, productDefinition);
        var selectedCurrency = currencyKind ?? product.DefaultCurrencyKind;
        var unitPrice = product.GetUnitPrice(selectedCurrency);
        if (unitPrice is null)
        {
            throw new InvalidOperationException($"Shop product '{product.Item.Id}' cannot be bought with {selectedCurrency}.");
        }

        if (product.RemainingLimit is not null && quantity > product.RemainingLimit.Value)
        {
            return ShopTransactionResult.Failed($"【{product.Item.Name}】已达购买上限。");
        }

        var totalPrice = checked(unitPrice.Value * quantity);
        if (!CanSpend(selectedCurrency, totalPrice))
        {
            return ShopTransactionResult.Failed(selectedCurrency == ShopCurrencyKind.Silver ? "银两不足。" : "元宝不足。");
        }

        Spend(selectedCurrency, totalPrice);
        State.Shop.AddPurchasedQuantity(product.PurchaseKey, quantity);
        _session.Events.Publish(new CurrencyChangedEvent());
        _session.InventoryService.AddItem(product.Item, quantity);
        return ShopTransactionResult.Succeeded($"买入【{product.DisplayName}】 x{quantity}");
    }

    public ShopTransactionResult Sell(InventoryEntry entry, int quantity = 1)
    {
        ArgumentNullException.ThrowIfNull(entry);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(quantity);

        var unitPrice = GetSellPrice(entry.Definition);
        if (unitPrice <= 0 || !entry.Definition.CanDrop)
        {
            return ShopTransactionResult.Failed($"【{entry.Definition.Name}】不能出售。");
        }

        switch (entry)
        {
            case StackInventoryEntry stack:
                if (quantity > stack.Quantity)
                {
                    return ShopTransactionResult.Failed($"【{entry.Definition.Name}】数量不足。");
                }

                State.Inventory.RemoveItem(stack.Definition, quantity);
                break;

            case EquipmentInstanceInventoryEntry equipment:
                if (quantity != 1)
                {
                    return ShopTransactionResult.Failed("独立装备一次只能出售 1 件。");
                }

                State.Inventory.RemoveEquipmentInstance(equipment.Equipment.Id);
                break;

            default:
                throw new InvalidOperationException($"Unsupported inventory entry type '{entry.GetType().Name}'.");
        }

        var totalPrice = checked(unitPrice * quantity);
        State.Currency.AddSilver(totalPrice);
        _session.Events.Publish(new InventoryChangedEvent());
        _session.Events.Publish(new CurrencyChangedEvent());
        return ShopTransactionResult.Succeeded($"卖出【{entry.Definition.Name}】 x{quantity}");
    }

    public int GetSellPrice(ItemDefinition item)
    {
        ArgumentNullException.ThrowIfNull(item);
        return item.Price <= 0 ? 0 : Math.Max(1, (int)Math.Floor(item.Price * SellPriceRatio));
    }

    private ShopProductView CreateProductView(string shopId, int productIndex, ShopProductDefinition product)
    {
        var productItem = _session.ContentRepository.GetItem(product.ContentId);
        var purchaseKey = BuildPurchaseKey(shopId, productIndex, product.ContentId);
        var purchasedQuantity = State.Shop.GetPurchasedQuantity(purchaseKey);
        int? remainingLimit = product.PurchaseLimit is null
            ? null
            : Math.Max(0, product.PurchaseLimit.Value - purchasedQuantity);
        var price = product.Price ?? productItem.Price;

        return new ShopProductView(
            productIndex,
            product,
            productItem,
            productItem.Name,
            productItem.Type,
            productItem.Picture,
            purchaseKey,
            price,
            product.PremiumPrice,
            purchasedQuantity,
            remainingLimit);
    }

    public static bool ShouldIgnoreProduct(string contentId) =>
        string.Equals(contentId, GoldProductContentId, StringComparison.Ordinal) ||
        contentId.EndsWith("残章", StringComparison.Ordinal);

    private bool CanSpend(ShopCurrencyKind currencyKind, int amount) =>
        currencyKind switch
        {
            ShopCurrencyKind.Silver => State.Currency.CanSpendSilver(amount),
            ShopCurrencyKind.Gold => State.Currency.CanSpendGold(amount),
            _ => throw new ArgumentOutOfRangeException(nameof(currencyKind), currencyKind, null)
        };

    private void Spend(ShopCurrencyKind currencyKind, int amount)
    {
        switch (currencyKind)
        {
            case ShopCurrencyKind.Silver:
                State.Currency.SpendSilver(amount);
                return;
            case ShopCurrencyKind.Gold:
                State.Currency.SpendGold(amount);
                return;
            default:
                throw new ArgumentOutOfRangeException(nameof(currencyKind), currencyKind, null);
        }
    }

    private static string BuildPurchaseKey(string shopId, int productIndex, string contentId) =>
        $"{shopId}|{productIndex}|{contentId}";
}

public enum ShopCurrencyKind
{
    Silver,
    Gold,
}

public sealed record ShopView(
    ShopDefinition Definition,
    IReadOnlyList<ShopProductView> Products);

public sealed record ShopProductView(
    int ProductIndex,
    ShopProductDefinition Definition,
    ItemDefinition Item,
    string DisplayName,
    ItemType ItemType,
    string Picture,
    string PurchaseKey,
    int? Price,
    int? PremiumPrice,
    int PurchasedQuantity,
    int? RemainingLimit)
{
    public ShopCurrencyKind DefaultCurrencyKind =>
        Price is not null ? ShopCurrencyKind.Silver : ShopCurrencyKind.Gold;

    public int? GetUnitPrice(ShopCurrencyKind currencyKind) =>
        currencyKind switch
        {
            ShopCurrencyKind.Silver => Price,
            ShopCurrencyKind.Gold => PremiumPrice,
            _ => throw new ArgumentOutOfRangeException(nameof(currencyKind), currencyKind, null)
        };
}

public sealed record ShopTransactionResult(
    bool Success,
    string Message)
{
    public static ShopTransactionResult Succeeded(string message) => new(true, message);

    public static ShopTransactionResult Failed(string message) => new(false, message);
}
