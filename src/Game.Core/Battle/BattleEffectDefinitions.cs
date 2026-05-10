using System.Text.Json.Serialization;
using Game.Core.Abstractions;
using Game.Core.Affix;
using Game.Core.Definitions;

namespace Game.Core.Battle;

[JsonPolymorphic(TypeDiscriminatorPropertyName = "type")]
[JsonDerivedType(typeof(SelfBattleTargetSelectorDefinition), "self")]
[JsonDerivedType(typeof(SourceBattleTargetSelectorDefinition), "source")]
[JsonDerivedType(typeof(TargetBattleTargetSelectorDefinition), "target")]
[JsonDerivedType(typeof(AllAlliesBattleTargetSelectorDefinition), "all_allies")]
[JsonDerivedType(typeof(AllEnemiesBattleTargetSelectorDefinition), "all_enemies")]
[JsonDerivedType(typeof(NearbyAlliesBattleTargetSelectorDefinition), "nearby_allies")]
public abstract record BattleTargetSelectorDefinition;

public sealed record SelfBattleTargetSelectorDefinition : BattleTargetSelectorDefinition;

public sealed record SourceBattleTargetSelectorDefinition : BattleTargetSelectorDefinition;

public sealed record TargetBattleTargetSelectorDefinition : BattleTargetSelectorDefinition;

public sealed record AllAlliesBattleTargetSelectorDefinition(
    bool IncludeSelf = true) : BattleTargetSelectorDefinition;

public sealed record AllEnemiesBattleTargetSelectorDefinition : BattleTargetSelectorDefinition;

public sealed record NearbyAlliesBattleTargetSelectorDefinition(
    int Radius,
    bool IncludeSelf = true) : BattleTargetSelectorDefinition;

[JsonPolymorphic(TypeDiscriminatorPropertyName = "type")]
[JsonDerivedType(typeof(ApplyBuffBattleEffectDefinition), "apply_buff")]
[JsonDerivedType(typeof(RemoveBuffBattleEffectDefinition), "remove_buff")]
[JsonDerivedType(typeof(RemoveNegativeBuffsBattleEffectDefinition), "remove_negative_buffs")]
[JsonDerivedType(typeof(RemovePositiveBuffsBattleEffectDefinition), "remove_positive_buffs")]
[JsonDerivedType(typeof(AddRageBattleEffectDefinition), "add_rage")]
[JsonDerivedType(typeof(SetRageBattleEffectDefinition), "set_rage")]
[JsonDerivedType(typeof(SetActionGaugeBattleEffectDefinition), "set_action_gauge")]
[JsonDerivedType(typeof(AddHpBattleEffectDefinition), "add_hp")]
[JsonDerivedType(typeof(AddMpBattleEffectDefinition), "add_mp")]
[JsonDerivedType(typeof(CancelHitBattleHookEffectDefinition), "cancel_hit")]
[JsonDerivedType(typeof(SetHitSuccessBattleHookEffectDefinition), "set_hit_success")]
[JsonDerivedType(typeof(ModifyDamageBattleHookEffectDefinition), "modify_damage")]
[JsonDerivedType(typeof(ModifyDamageContextBattleHookEffectDefinition), "modify_damage_context")]
[JsonDerivedType(typeof(ModifyMpCostBattleHookEffectDefinition), "modify_mp_cost")]
[JsonDerivedType(typeof(StrengthenContextBuffBattleHookEffectDefinition), "strengthen_context_buff")]
public abstract record BattleEffectDefinition
{
    public virtual void Resolve(IContentRepository contentRepository)
    {
    }
}

public sealed record ApplyBuffBattleEffectDefinition(
    BattleTargetSelectorDefinition Target,
    string BuffId,
    int Level,
    int Duration,
    int Chance = 100) : BattleEffectDefinition
{
    [JsonIgnore]
    public BuffDefinition Buff { get; private set; } = null!;

    public override void Resolve(IContentRepository contentRepository)
    {
        ArgumentNullException.ThrowIfNull(contentRepository);
        Buff = contentRepository.GetBuff(BuffId);
    }
}

public sealed record RemoveBuffBattleEffectDefinition(
    BattleTargetSelectorDefinition Target,
    string BuffId) : BattleEffectDefinition
{
    [JsonIgnore]
    public BuffDefinition Buff { get; private set; } = null!;

    public override void Resolve(IContentRepository contentRepository)
    {
        ArgumentNullException.ThrowIfNull(contentRepository);
        Buff = contentRepository.GetBuff(BuffId);
    }
}

public sealed record RemoveNegativeBuffsBattleEffectDefinition(
    BattleTargetSelectorDefinition Target) : BattleEffectDefinition;

public sealed record RemovePositiveBuffsBattleEffectDefinition(
    BattleTargetSelectorDefinition Target) : BattleEffectDefinition;

public sealed record AddRageBattleEffectDefinition(
    BattleTargetSelectorDefinition Target,
    int Value) : BattleEffectDefinition;

public sealed record SetRageBattleEffectDefinition(
    BattleTargetSelectorDefinition Target,
    int Value) : BattleEffectDefinition;

public sealed record SetActionGaugeBattleEffectDefinition(
    BattleTargetSelectorDefinition Target,
    int Value) : BattleEffectDefinition;

public sealed record AddHpBattleEffectDefinition(
    BattleTargetSelectorDefinition Target,
    int Value) : BattleEffectDefinition;

public sealed record AddMpBattleEffectDefinition(
    BattleTargetSelectorDefinition Target,
    int Value) : BattleEffectDefinition;

public sealed record CancelHitBattleHookEffectDefinition(
    bool SuppressHitEffects = true) : BattleEffectDefinition;
