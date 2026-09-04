using Game.Core.Affix;
using Game.Core.Model;

namespace Game.Core.Definitions;

public sealed record EquipmentDefinition : ItemDefinition, IAffixProvider
{
    public required EquipmentSlotType SlotType { get; init; }
    public IReadOnlyList<AffixDefinition> Affixes { get; init; } = [];
    public IReadOnlyList<EquipmentGrantedSkillDefinition> GrantedSkills { get; init; } = [];
    public IReadOnlyList<EquipmentGrantedSpecialSkillDefinition> GrantedSpecialSkills { get; init; } = [];
    public ProviderKind ProviderKind { get; } = ProviderKind.Equipment;
}

public sealed record EquipmentGrantedSkillDefinition(string SkillId, int Level);

public sealed record EquipmentGrantedSpecialSkillDefinition(string SkillId);
