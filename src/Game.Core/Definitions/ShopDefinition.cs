namespace Game.Core.Definitions;

public sealed record ShopDefinition
{
    public required string Id { get; init; }

    public required string Name { get; init; }

    public string? Music { get; init; }

    public string? Background { get; init; }

    public IReadOnlyList<ShopProductDefinition> Products { get; init; } = [];
}

public sealed record ShopProductDefinition
{
    public required string ContentId { get; init; }

    public int? PurchaseLimit { get; init; }

    public int? Price { get; init; }

    public int? PremiumPrice { get; init; }
}
