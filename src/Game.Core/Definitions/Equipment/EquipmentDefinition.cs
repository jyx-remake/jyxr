using Game.Core.Affix;
using Game.Core.Model;

namespace Game.Core.Definitions;

public sealed record EquipmentDefinition : ItemDefinition, IAffixProvider
{
    public required EquipmentSlotType SlotType { get; init; }
    public IReadOnlyList<AffixDefinition> Affixes { get; init; } = [];
    public ProviderKind ProviderKind { get; } = ProviderKind.Equipment;
}
