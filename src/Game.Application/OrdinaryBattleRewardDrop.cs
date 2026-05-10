using Game.Core.Affix;
using Game.Core.Definitions;

namespace Game.Application;

public abstract record OrdinaryBattleRewardDrop;

public sealed record OrdinaryBattleStackRewardDrop(
    ItemDefinition Item,
    int Quantity) : OrdinaryBattleRewardDrop;

public sealed record OrdinaryBattleEquipmentRewardDrop(
    EquipmentDefinition Equipment,
    IReadOnlyList<GeneratedEquipmentAffixRoll> Rolls) : OrdinaryBattleRewardDrop;

public sealed record GeneratedEquipmentAffixRoll(
    string Key,
    EquipmentRandomAffixKind Kind,
    IReadOnlyList<AffixDefinition> Affixes);
