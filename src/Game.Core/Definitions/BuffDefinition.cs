using Game.Core.Affix;

namespace Game.Core.Definitions;

public sealed record BuffDefinition: IAffixProvider
{
    public required string Id { get; init; }
    public required string Name  { get; init; }
    public string Description { get; init; } = "";
    public required bool IsDebuff { get; init; }
    // public int DefaultDuration { get; init; } = 3;
    // public int DefaultLevel { get; init; } = 3;
    // public double DefaultChance { get; init; } = 1D;
    public ProviderKind ProviderKind => ProviderKind.Buff;
    public IReadOnlyList<AffixDefinition> Affixes { get; init; } = [];
}
